using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Portal : MonoBehaviour
{
    //controlled by editor script
    public Transform otherPortal;

    private MeshRenderer meshRenderer;

    public Material material
    {
        set => meshRenderer.material = value;
        get => meshRenderer.material;
    }

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnValidate()//called, when value changes in inspector
    {
        //when otherPortal field for this Portal is set, also the one for the other (value of this Portal's otherPortal) is set
        if (otherPortal)
        {
            var otherPortalAsportal = otherPortal.GetComponent<Portal>();
            if (otherPortalAsportal) otherPortalAsportal.otherPortal = transform;
        }
    }
}