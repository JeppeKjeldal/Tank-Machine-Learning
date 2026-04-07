using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float moveSpeed = 5f;
    private PlayerControls playerControls;
    private Rigidbody2D rigidBody;
    private Vector2 movementDirection;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Update()
    {
        PlayerInput();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void PlayerInput()
    {
        movementDirection = playerControls.Movement.Move.ReadValue<Vector2>();
    }

    private void Move()
    {
        rigidBody.MovePosition(rigidBody.position + movementDirection * (moveSpeed * Time.fixedDeltaTime));
    }
}
