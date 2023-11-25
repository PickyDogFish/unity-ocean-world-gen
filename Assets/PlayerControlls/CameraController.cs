using UnityEngine;
using UnityEngine.Animations;

public class CameraController : MonoBehaviour {
    [SerializeField] float minXRotation = 25f;
    public float mouseSensivity = 20f;

    float xRotation = 0f;


    // Start is called before the first frame update
    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensivity * 100 * Time.smoothDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensivity * 100 *Time.smoothDeltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, minXRotation);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.parent.Rotate(Vector3.up * mouseX);
    }
}