using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float bulletSpeed = 10.0f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;

    private PlayerControls playerControls;
    private Rigidbody2D rigidBody;

    private float moveInput;
    private float rotateInput;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Enable();
        playerControls.Movement.Shoot.performed += OnShoot;
    }

    private void OnDisable()
    {
        playerControls.Movement.Shoot.performed -= OnShoot;
        playerControls.Disable();
    }

    private void Update()
    {
        moveInput = playerControls.Movement.ForwardBackward.ReadValue<float>();
        rotateInput = playerControls.Movement.Rotate.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        Move();
        Rotate();
    }

    private void Move()
    {
        Vector2 moveAmount = (Vector2)transform.up * moveInput * moveSpeed * Time.fixedDeltaTime;
        rigidBody.MovePosition(rigidBody.position + moveAmount);
    }

    private void Rotate()
    {
        float rotationAmount = -rotateInput * rotateSpeed * Time.fixedDeltaTime;
        rigidBody.MoveRotation(rigidBody.rotation + rotationAmount);
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = bulletSpawnPoint.up * bulletSpeed;
    }
}