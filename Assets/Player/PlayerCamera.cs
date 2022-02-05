using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 150;
    [SerializeField] private bool lockMouse = true;

    private Transform playerBody;
    private float yMouseRotate = 0f;

    void Start()
    {
        playerBody = transform.parent.transform;

        if (lockMouse)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yMouseRotate += mouseY;

        if (yMouseRotate > 90f)
        {
            yMouseRotate = 90f;
            mouseY = 0f;
            rotate(270f);
        }
        else if (yMouseRotate < -90f)
        {
            yMouseRotate = -90f;
            mouseY = 0f;
            rotate(90f);
        }
        transform.Rotate(-Vector3.right * mouseY);
        playerBody.Rotate(Vector3.up * mouseX);
    }
    void rotate(float input)
    {
        Vector3 Rotation = transform.eulerAngles;
        Rotation.x = input;
        transform.eulerAngles = Rotation;
    }
}
