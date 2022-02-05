using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class PortalRenderer : MonoBehaviour
{
    //parameters controlled by PortalRendererEditor
    public Camera portalCamera;
    public int maxRenderCount = 12;//max number of portals to be rendered in one frame
    public float portalCamFarClipDist = 1000f;
    public Vector2Int rendTexMaxRes = new Vector2Int(1024, 1024);
    public Portal[] portals;
    public bool autosearchPortals = true;//if true, search for portals on Awake with tag below
    public string portalTag = "Portal";
    public bool debug_visualizeClipping = false;

    public struct ViewParam
    {
        public Vector3 position;
        public Quaternion rotation;
        public Matrix4x4 projMatrix;
        public Vector2Int resolution;// x: width, y: height
        public bool isScreenSpacePortal;//when close to portal, it's rendered with screen space shader
      
        public ViewParam(Vector3 position, Quaternion rotation, Matrix4x4 projMatrix, Vector2Int resolution, bool isScreenSpacePortal)
        {
            this.position = position;
            this.rotation = rotation;
            this.projMatrix = projMatrix;
            this.resolution = resolution;
            this.isScreenSpacePortal = isScreenSpacePortal;
        }

        //this overload sets isScreenSpacePortal to false
        public ViewParam(Vector3 position, Quaternion rotation, Matrix4x4 projMatrix, Vector2Int resolution) : this(position, rotation, projMatrix, resolution, false) { }
    }

    private struct Command//one command puts material onto portal or renders to texture (if portIndex == rendCode)
    {
        public ushort textureIndex;//index of material or render texture
        public ushort portalIndex;

        public const ushort rendCode = ushort.MaxValue;
    }
    
    //readonly data (set on Start)
    Camera cam;//Camera of this gameObject
    Material[] portalMats;
    RenderTexture[] rendTextures;
    static readonly Quaternion flipRotation = Quaternion.AngleAxis(180, Vector3.up);
    bool setupDone = false;//Start sets this to true
    Shader defaultShader;
    Shader screenSpaceShader;

    Command[] commandBuffer;
    ViewParam[] viewParamBuffer;

    void Start()
    {
        //simple variable setup
        cam = GetComponent<Camera>();
        commandBuffer = new Command[2 * maxRenderCount];
        viewParamBuffer = new ViewParam[maxRenderCount];
        defaultShader = Shader.Find("Universal Render Pipeline/Unlit");
        screenSpaceShader = Shader.Find("Hidden/Portal/ScreenSpace");

        //portalCamera setup
        portalCamera.transform.gameObject.SetActive(true);
        portalCamera.enabled = false;

        //portalMats and rendTextures setup
        portalMats = new Material[maxRenderCount];
        rendTextures = new RenderTexture[maxRenderCount];
        var rendTexDescriptor = new RenderTextureDescriptor(rendTexMaxRes.x, rendTexMaxRes.y, cam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default, 16);
        rendTexDescriptor.useDynamicScale = true;
        rendTexDescriptor.msaaSamples = 4;
        for (int i = 0; i < maxRenderCount; i++)
        {
            rendTextures[i] = new RenderTexture(rendTexDescriptor);
            rendTextures[i].name = "Portal Rend Tex ID" + i.ToString();

            portalMats[i] = new Material(defaultShader);
            portalMats[i].SetTexture("_BaseMap", rendTextures[i]);
            portalMats[i].SetColor("_BaseColor", Color.white);
            portalMats[i].name = "Portal Mat ID" + i.ToString();
        }

        //setup preRender-callback
        RenderPipelineManager.beginCameraRendering += renderPortals;

        //find portals by tag if autosearch enabled
        if (autosearchPortals)
        {
            var portalsAsGameObjects = GameObject.FindGameObjectsWithTag(portalTag);

            //get Portal-components
            portals = new Portal[portalsAsGameObjects.Length];
            int portalIndex = 0;//counts, how many GameObjects in portalsAsGameObjects have Portal-script
            for (int i = 0; i < portals.Length; i++)
            {
                portals[portalIndex] = portalsAsGameObjects[i].GetComponent<Portal>();

                if (portals[portalIndex] != null) { portalIndex++; }
            }

            //if all GameObjects (in portalsAsGameObjects) didn't have Portal-script
            if (portalIndex != portals.Length)
            {
                Debug.LogWarning(transform.name + ": PortalRenderer is searching for Portals with tag \"" + portalTag + "\", but not all GameObjects with that tag have Portal-Component.");

                //slice out null's
                var temp = portals;
                portals = new Portal[portalIndex];
                System.Array.Copy(temp, 0, portals, 0, portalIndex);
            }
        }

        setupDone = true;
    }

    void OnDestroy()
    {
        if (setupDone)
        {
            //remove preRender-callback
            RenderPipelineManager.beginCameraRendering -= renderPortals;

            //release rendTextures and portalMats
            for (int i = 0; i < rendTextures.Length; i++) if (rendTextures[i].IsCreated()) rendTextures[i].Release();
            System.Array.ForEach(portalMats, mat => Destroy(mat));
        }
    }

    void renderPortals(ScriptableRenderContext context, Camera camera) {
        if (camera == cam)
        {
            //evaluate commandBuffer and viewParamBuffer
            int materialsUsed = 0;
            int texturesUsed = 0;
            int commandsSet = 0;
            bool firstIter = true;
            do
            {
                if (!firstIter)
                {
                    //set render command
                    commandBuffer[commandsSet].textureIndex = (ushort)texturesUsed;
                    commandBuffer[commandsSet].portalIndex = Command.rendCode;
                    texturesUsed++;
                    commandsSet++;
                }

                //check what portals observer (set below) can see
                ViewParam observer = firstIter ? new ViewParam(cam.transform.position, cam.transform.rotation, cam.projectionMatrix, new Vector2Int(Screen.width, Screen.height)) : viewParamBuffer[texturesUsed - 1];
                for (ushort portalIndex = 0; portalIndex < portals.Length; portalIndex++)
                {
                    if (materialsUsed == maxRenderCount)
                        break;

                    if (isPortalVisible(observer, portals[portalIndex].transform) || debug_visualizeClipping)
                    {
                        #region calculate view parameters
                        //in this region value for this is set
                        ref var viewParam = ref viewParamBuffer[materialsUsed];

                        //check if portal should use screen space shader (not implemented!)
                        viewParam.isScreenSpacePortal = false;

                        //make sure that material of this portal uses right shader
                        Shader correctShader = viewParam.isScreenSpacePortal ? screenSpaceShader : defaultShader;
                        if (portalMats[materialsUsed].shader != correctShader)
                        {
                            var mat = new Material(correctShader);
                            if (correctShader == defaultShader)
                            {
                                mat.SetColor("_BaseColor", Color.white);
                                mat.SetTexture("_BaseMap", rendTextures[materialsUsed]);
                            }
                            else//if screen space shader
                            {
                                mat.SetTexture("_MainTex", rendTextures[materialsUsed]);
                            }
                            Destroy(portalMats[materialsUsed]);//destroy old material
                            portalMats[materialsUsed] = mat;//put new material to the buffer
                        }

                        Transform portalA = portals[portalIndex].transform;//portal seen by observer
                        Transform portalB = portals[portalIndex].otherPortal.transform;//pair of portalA

                        //position of the observer relative to portalA
                        Vector3 localObsPos = Quaternion.Inverse(portalA.rotation) * (observer.position - portalA.position);

                        //calculate position and rotation
                        viewParam.position = portalB.rotation * flipRotation * localObsPos + portalB.position;
                        if (viewParam.isScreenSpacePortal)
                            viewParam.rotation = portalB.rotation * flipRotation * (Quaternion.Inverse(portalA.rotation) * observer.rotation);
                        else
                            viewParam.rotation = portalB.rotation * flipRotation;

                        //calculate projection matrix
                        if (viewParam.isScreenSpacePortal)
                            viewParam.projMatrix = observer.projMatrix;
                        else
                        {
                            Vector3 pos = -localObsPos;//note negate
                            Vector3 scale = portalB.lossyScale;
                            float far = portalCamFarClipDist;
                            viewParam.projMatrix = Matrix4x4.zero;
                            viewParam.projMatrix[0, 0] = 2 * pos.z / scale.x;
                            viewParam.projMatrix[0, 2] = 2 * pos.x / scale.x;
                            viewParam.projMatrix[1, 1] = 2 * pos.z / scale.y;
                            viewParam.projMatrix[1, 2] = 2 * pos.y / scale.y;
                            viewParam.projMatrix[2, 2] = -(far + pos.z) / (far - pos.z);
                            viewParam.projMatrix[2, 3] = -2 * far * pos.z / (far - pos.z);
                            viewParam.projMatrix[3, 2] = -1f;
                        }


                        //calculate resolution
                        Vector3 s = portalA.lossyScale / 2;
                        Vector3[] corners = new Vector3[] { new Vector3(s.x, s.y), new Vector3(s.x, -s.y), new Vector3(-s.x, s.y), new Vector3(-s.x, -s.y) };//corners of the portal in it's local space (topRight, bottomRight, topLeft, bottomLeft)
                        for (int i = 0; i < corners.Length; i++)//do some transformations for corners
                        {
                            corners[i] = portalA.position + portalA.rotation * corners[i];//transform into world space
                            corners[i] = Quaternion.Inverse(observer.rotation) * (corners[i] - observer.position);//transform into camera's local space
                            corners[i] = observer.projMatrix * corners[i] / -corners[i].z;//transform into NDC
                            corners[i] = Vector3.Scale(corners[i], new Vector3(observer.resolution.x / 2, observer.resolution.y / 2, .5f));//scale by half (NDC is 2x2x2) of resolution of the observer
                        }
                        viewParam.resolution.x = (int)Mathf.Max(Vector2.Distance(corners[0], corners[2]), Vector3.Distance(corners[1], corners[3]));//set width to viewParam
                        viewParam.resolution.y = (int)Mathf.Max(Vector2.Distance(corners[0], corners[1]), Vector3.Distance(corners[2], corners[3]));//set height to viewParam
                        #endregion

                        //set 'apply material' command
                        commandBuffer[commandsSet].portalIndex = portalIndex;
                        commandBuffer[commandsSet].textureIndex = (ushort)materialsUsed;
                        materialsUsed++;
                        commandsSet++;
                    }
                    
                    if (debug_visualizeClipping)
                    {
                        if (isPortalVisible(observer, portals[portalIndex].transform))
                            portalMats[materialsUsed - 1].SetColor("_BaseColor", Color.white);
                        else
                            portalMats[materialsUsed - 1].SetColor("_BaseColor", Color.red);
                    }
                }

                firstIter = false;

            } while (texturesUsed < materialsUsed);

            //render portals
            Vector2 originalScaleFactors = new Vector2(ScalableBufferManager.widthScaleFactor, ScalableBufferManager.heightScaleFactor);
            for (int viewParamIndex = texturesUsed - 1, i = commandsSet - 1; i > -1; i--)
            {
                if (commandBuffer[i].portalIndex == Command.rendCode)//if render command 
                {
                    ref var rendTex = ref rendTextures[commandBuffer[i].textureIndex];
                    ref var viewParam = ref viewParamBuffer[viewParamIndex];

                    portalCamera.targetTexture = rendTex;//set target texture of the camera

                    //set width and height of the render texture
                    //float widthFactor = Mathf.Min(viewParam.resolution.x, rendTexMaxRes.x) / (float)rendTexMaxRes.x;
                    //float heightFactor = Mathf.Min(viewParam.resolution.y, rendTexMaxRes.y) / (float)rendTexMaxRes.y;
                    //ScalableBufferManager.ResizeBuffers(widthFactor, heightFactor);

                    //apply view parameters to camera
                    portalCamera.projectionMatrix = viewParam.projMatrix;
                    portalCamera.transform.position = viewParam.position;
                    portalCamera.transform.rotation = viewParam.rotation;
                    viewParamIndex--;

                    UniversalRenderPipeline.RenderSingleCamera(context, portalCamera);//render
                }
                else//if 'apply material' command
                {
                    portals[commandBuffer[i].portalIndex].material = portalMats[commandBuffer[i].textureIndex];
                }
            }
            ScalableBufferManager.ResizeBuffers(originalScaleFactors.x, originalScaleFactors.y);//to not mess up with other systems using ScalableBufferManager
        }
    }

    static bool isPortalVisible(ViewParam observer, Transform portal)
    {
        //normal clipping
        if (Vector3.Dot(portal.forward, portal.position - observer.position) <= 0)
            return false;

        #region frustum clipping
        //calculate portal's bounds
        var bounds = new Bounds(Vector3.zero, new Vector3(portal.lossyScale.x, portal.lossyScale.y, 0));

        //calculate frustum planes
        Matrix4x4 viewMatrix = Matrix4x4.Inverse(Matrix4x4.TRS(Quaternion.Inverse(portal.rotation) * (observer.position - portal.position), Quaternion.Inverse(portal.rotation) * observer.rotation, new Vector3(1, 1, -1)));
        var planes = GeometryUtility.CalculateFrustumPlanes(observer.projMatrix * viewMatrix);

        //do clip test
        if (!GeometryUtility.TestPlanesAABB(planes, bounds)) return false;
        #endregion

        //if no clipping was succesfull
        return true;
    }
}