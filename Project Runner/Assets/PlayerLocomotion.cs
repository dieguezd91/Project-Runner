using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerConfigSO config;
    // [SerializeField] private Transform cameraTransform; // ELIMINADO del Inspector

    // State
    private Rigidbody _rb;
    private CapsuleCollider _collider;
    private Transform _mainCameraTransform; // Referencia interna
    private Vector2 _inputVector;
    private bool _isJumpRequested;
    private bool _isGrounded;
    private float _groundCheckRadius = 0.3f;

    // Cache variables
    private Vector3 _moveDirection;
    private float _jumpBufferCounter;
    private float _coyoteTimeCounter;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();

        // AUTO-REFERENCIA: Buscamos la cámara principal automáticamente
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("PlayerLocomotion: No se encontró MainCamera taggeada en la escena.");
        }

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.freezeRotation = true;
    }

    private void Update()
    {
        HandleInput();

        if (_isGrounded) _coyoteTimeCounter = config.coyoteTime;
        else _coyoteTimeCounter -= Time.deltaTime;

        if (Input.GetButtonDown("Jump")) _jumpBufferCounter = config.jumpBufferTime;
        else _jumpBufferCounter -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        CheckGround();
        ApplyMovement();
        ApplyGravity();
        HandleJump();
    }

    private void HandleInput()
    {
        // ... (Tu lógica de input igual)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _inputVector = new Vector2(h, v).normalized;

        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _isJumpRequested = true;
        }
    }

    private void ApplyMovement()
    {
        if (_mainCameraTransform == null) return;

        // Usamos la referencia interna _mainCameraTransform
        Vector3 cameraForward = _mainCameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = _mainCameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        _moveDirection = (cameraForward * _inputVector.y + cameraRight * _inputVector.x);

        // ... Resto de tu lógica de física (Velocity diff, etc) ...
        // (Copia y pega tu bloque de lógica P-Controller aquí)

        Vector3 targetVelocity = _moveDirection * config.maxSpeed;
        Vector3 currentVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        Vector3 velocityDiff = targetVelocity - currentVelocity;

        float controlMultiplier = _isGrounded ? 1f : 0.2f;
        _rb.AddForce(velocityDiff * config.acceleration * controlMultiplier, ForceMode.Acceleration);

        if (_isGrounded)
            _rb.linearDamping = (_moveDirection.magnitude < 0.1f) ? config.groundDrag * 2f : 0f;
        else
            _rb.linearDamping = config.airDrag;

        // Rotación (incluida aquí para simplificar FixedUpdate)
        if (_moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, config.rotationSpeed * Time.fixedDeltaTime);
        }
    }

    // ... Resto de métodos (HandleJump, ApplyGravity, CheckGround) se mantienen igual ...
    // Asegúrate de copiar HandleJump, CheckGround y Gizmos del script anterior.

    private void HandleJump()
    {
        if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * config.jumpForce, ForceMode.Impulse);
            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;
        }

        if (!Input.GetButton("Jump") && _rb.linearVelocity.y > 0f)
        {
            _rb.AddForce(Vector3.down * _rb.linearVelocity.y * (1 - config.jumpCutMultiplier), ForceMode.Impulse);
        }
    }

    private void ApplyGravity()
    {
        if (_rb.linearVelocity.y < 0)
        {
            _rb.AddForce(Vector3.down * (Physics.gravity.y * (config.gravityMultiplier - 1) * -1), ForceMode.Acceleration);
        }
    }

    private void CheckGround()
    {
        Vector3 feetPosition = new Vector3(transform.position.x, _collider.bounds.min.y, transform.position.z);
        _isGrounded = Physics.CheckSphere(feetPosition, _groundCheckRadius, config.groundLayer);
    }
}