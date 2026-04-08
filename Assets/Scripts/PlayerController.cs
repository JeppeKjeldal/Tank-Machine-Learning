using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float bulletSpeed = 10.0f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;

    private PlayerControls playerControls;
    private Rigidbody2D rigidBody;

    private float moveInput;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Enable();

        playerControls.Movement.Rotate.performed += OnRotate;
        playerControls.Movement.Shoot.performed += OnShoot;
    }

    private void OnDisable()
    {
        playerControls.Movement.Rotate.performed -= OnRotate;
        playerControls.Movement.Shoot.performed -= OnShoot;

        playerControls.Disable();
    }

    private void Update()
    {
        moveInput = playerControls.Movement.ForwardBackward.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        Vector2 moveAmount = (Vector2)transform.up * moveInput * moveSpeed * Time.fixedDeltaTime;
        rigidBody.MovePosition(rigidBody.position + moveAmount);
    }

    // 🔄 Rotate exactly 90 degrees per key press
    private void OnRotate(InputAction.CallbackContext context)
    {
        float input = context.ReadValue<float>();

        if (input > 0)
        {
            Rotate90(-90f);
        }
        else if (input < 0)
        {
            Rotate90(90f);
        }
    }

    private void Rotate90(float angle)
    {
        float newRotation = rigidBody.rotation + angle;
        rigidBody.MoveRotation(newRotation);
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = bulletSpawnPoint.up * bulletSpeed;
    }
}