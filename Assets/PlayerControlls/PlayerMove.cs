using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour {

    [SerializeField] InputActionAsset actions;
    private InputAction moveAction;
    private InputAction sprintAction;
    [SerializeField] private float walkSpeed = 1f;
    private Vector2 moveInput;

    void Start(){
        moveAction = actions.FindActionMap("Player").FindAction("Move");
        sprintAction = actions.FindActionMap("Player").FindAction("Sprint");
    }

    void Update() {
        moveInput = moveAction.ReadValue<Vector2>();
        float curWalkSpeed = walkSpeed;
        if (sprintAction.IsPressed()){
            curWalkSpeed *= 10;
        }
        transform.position += Vector3.Normalize(transform.GetChild(0).forward * moveInput.y + transform.GetChild(0).right * moveInput.x) * curWalkSpeed * Time.deltaTime;
    }

}
