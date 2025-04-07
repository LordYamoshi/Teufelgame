using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed of movement
    public float rotationSpeed = 100f; // Speed of rotation

    void Update()
    {
        // Move the camera with WASD in local space
        float moveX = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow keys
        float moveZ = Input.GetAxis("Vertical");   // W/S or Up/Down Arrow keys

        Vector3 localMoveDirection = new Vector3(moveX, 0, moveZ);
        transform.Translate(localMoveDirection * moveSpeed * Time.deltaTime, Space.Self); // Use Space.Self for local space movement

        // Rotate the camera with Q/E in local space
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.Self); // Rotate left in local space
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self); // Rotate right in local space
        }
    }
}
