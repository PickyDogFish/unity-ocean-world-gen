using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour {

    [SerializeField] InputActionAsset actions;
    private InputAction moveAction;
    [SerializeField] private float walkSpeed = 10f;
    // Start is called before the first frame update
    private Vector2 moveInput;

    void Start(){
        moveAction = actions.FindActionMap("Player").FindAction("Move");
    }

    // Update is called once per frame
    void Update() {
        moveInput = moveAction.ReadValue<Vector2>();
        transform.position += transform.GetChild(0).forward * moveInput.y + transform.GetChild(0).right * moveInput.x;
    }

}
