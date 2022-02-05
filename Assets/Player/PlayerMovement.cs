using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float jumpVelocity = 5f;

    private Rigidbody rb;
    private Vector3 movement;
    private bool grounded = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        movement = transform.forward * Input.GetAxis("Vertical") * movementSpeed +
                   transform.right * Input.GetAxis("Horizontal") * movementSpeed;

    }
    private void FixedUpdate()
    {
        Vector3 vel = rb.velocity;

        vel.x = movement.x;//move
        vel.z = movement.z;

        if (grounded)//jump
        {
            if (Input.GetKey("space"))//normal jump
            {
                vel.y = jumpVelocity;
            }
        }

        rb.velocity = vel;
    }
    private void OnCollisionStay() => grounded = true;
    private void OnCollisionExit() => grounded = false;
}
