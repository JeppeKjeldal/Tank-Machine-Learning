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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collided with: " + collision.gameObject.name);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Called every frame while touching something
        // Debug.Log("Still touching: " + collision.gameObject.name);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        //Debug.Log("Stopped colliding with: " + collision.gameObject.name);
    }
}
