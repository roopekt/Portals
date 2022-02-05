using UnityEngine;
using System;
using System.Linq;

public class Teleporter : MonoBehaviour
{
    //variables for editor script
    public bool[] ghost_compMask;
    public bool copyChilds = false;
    public static readonly Type[] defCompsToCopy = { typeof(MeshFilter), typeof(MeshRenderer)/*, typeof(Collider)*/ };//by default, components of these types will be copied to the ghost
    public int[] ghost_compIDs;

    static readonly Quaternion flipRotation = Quaternion.AngleAxis(180, Vector3.up);
    new Rigidbody rigidbody;//rigidbody of this gameObject
    GameObject ghost;
    Transform portal;
    Transform otherPortal;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
            Debug.LogError("GameObject with Teleporter component has no rigibody. Please add a kinematic rigidbody to detect trigger collisions.");
    }

    void OnTriggerStay(Collider collider)
    {
        if (ghost == null)
        {
            Portal portalComp = collider.gameObject.GetComponent<Portal>();

            if (portalComp)//if collider is portal
            {
                //find portal and otherPortal
                portal = collider.transform;
                otherPortal = portalComp.otherPortal;

                //create ghost
                ghost = Instantiate(transform.gameObject);
                ghost.name = "Ghost";
                Component[] components = ghost.GetComponents<Component>();
                for (int i = 1; i < components.Length; i++)//starting from 1 to not destroy Transform
                {
                    if (!ghost_compMask[i]) Destroy(components[i]);
                }
                if (!copyChilds)
                {
                    //destroy children
                    Transform ghostTranform = ghost.transform;
                    for (int i = 0; i < ghostTranform.childCount; i++)
                    {
                        Destroy(ghostTranform.GetChild(i).gameObject);
                    }
                }
            }
        }

        if (ghost != null)
        {
            Transform ghostTransform = ghost.transform;

            ghostTransform.position = otherPortal.rotation * flipRotation * (Quaternion.Inverse(portal.rotation) * (transform.position - portal.position)) + otherPortal.position;
            ghostTransform.rotation = otherPortal.rotation * flipRotation * (Quaternion.Inverse(portal.rotation) * transform.rotation);

            if (Vector3.Dot(portal.forward, (transform.position - portal.position).normalized) > 0)//if this object is on other side of the portal
            {
                //rotate velocity (of transform)
                if (rigidbody) rigidbody.velocity = otherPortal.rotation * flipRotation * (Quaternion.Inverse(portal.rotation) * rigidbody.velocity);

                //switch portal and otherPortal
                var oldPortal = portal;
                portal = otherPortal;
                otherPortal = oldPortal;

                //temporaly store original transform
                Transform originalTr = transform;

                //teleport transform
                transform.position = ghostTransform.position;
                transform.rotation = ghostTransform.rotation;

                //teleport ghostTransform
                ghostTransform.position = originalTr.position;
                ghostTransform.rotation = originalTr.rotation;
            }
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.GetComponent<Portal>() != null)//if collider is portal
            Destroy(ghost);
    }

    void Reset()
    {
        Component[] components = GetComponents<Component>();
        ghost_compMask = new bool[components.Length];
        for (int i = 0; i < components.Length; i++)
            ghost_compMask[i] = isDefault(components[i].GetType());
    }

    //returns true, if component is in defCompsToCopy
    public static bool isDefault(Type component) => defCompsToCopy.Contains(component) || defCompsToCopy.Contains(component.BaseType);
}
