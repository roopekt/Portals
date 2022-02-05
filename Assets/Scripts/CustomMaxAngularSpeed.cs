using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomMaxAngularSpeed : MonoBehaviour
{
    [SerializeField] private float maxAngularVelocity = 7f;

    private void Start()
    {
        GetComponent<Rigidbody>().maxAngularVelocity = maxAngularVelocity;
    }
}
