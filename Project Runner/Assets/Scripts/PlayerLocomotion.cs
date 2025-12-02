using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    [Header("Configuration")]
    public PlayerConfigSO config;

    private Rigidbody rb;
    private new Transform transform;

    private bool isGrounded;
    private bool wasGrounded;
    private float lastJumpTime;
    private float lastGroundedTime;
    private bool jumpRequested;
    private bool jumpCut;

    private float horizontalInput;
    private float verticalInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        transform = GetComponent<Transform>();
    }

    private void Update()
    {
        ReadInput();
        CheckGround();
        HandleJump();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyGravity();
        ApplyDrag();
    }

    private void ReadInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump"))
        {
            jumpRequested = true;
            lastJumpTime = Time.time;
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            jumpCut = true;
        }
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;

        float detectionDistance = 1.1f;
        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            detectionDistance,
            config.groundLayer
        );

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            jumpCut = false;
        }
    }

    private void HandleJump()
    {
        bool canUseCoyoteTime = Time.time - lastGroundedTime <= config.coyoteTime;
        bool jumpInBuffer = Time.time - lastJumpTime <= config.jumpBufferTime;

        if (jumpRequested && jumpInBuffer && (isGrounded || canUseCoyoteTime))
        {
            ExecuteJump();
            jumpRequested = false;
        }

        if (jumpCut && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                rb.linearVelocity.y * config.jumpCutMultiplier,
                rb.linearVelocity.z
            );
            jumpCut = false;
        }
    }

    private void ExecuteJump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * config.jumpForce, ForceMode.Impulse);
        lastGroundedTime = 0;
    }

    private void ApplyMovement()
    {
        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

        if (movementDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                config.rotationSpeed * Time.fixedDeltaTime
            );

            Vector3 targetVelocity = movementDirection * config.maxSpeed;
            Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 accelerationForce = (targetVelocity - currentVelocity) * config.acceleration;

            rb.AddForce(accelerationForce, ForceMode.Force);
        }
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * config.gravityMultiplier, ForceMode.Acceleration);
        }
    }

    private void ApplyDrag()
    {
        rb.linearDamping = isGrounded ? config.groundDrag : config.airDrag;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public bool WasGrounded()
    {
        return wasGrounded;
    }

    private void OnDrawGizmos()
    {
        if (transform == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1.1f);
    }
}