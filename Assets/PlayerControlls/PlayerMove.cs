using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour {

    [SerializeField] InputActionAsset actions;
    private InputAction moveAction;
    [SerializeField] private float walkSpeed = 1f;
    private Vector2 moveInput;

    void Start(){
        moveAction = actions.FindActionMap("Player").FindAction("Move");
    }

    void Update() {
        moveInput = moveAction.ReadValue<Vector2>();
        transform.position += Vector3.Normalize(transform.GetChild(0).forward * moveInput.y + transform.GetChild(0).right * moveInput.x) * walkSpeed * Time.deltaTime;
    }

}
