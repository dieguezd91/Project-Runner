using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLocomotion : MonoBehaviour
{
    [Header("Configuration")]
    public PlayerConfigSO config;

    [Header("Components")]
    [SerializeField] private DriftController driftController;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Debug")]
    public bool showDebugGUI = true;

    // Caches de abilities dinámicas — se refrescan via OnPartCollected, nunca en Update/FixedUpdate
    private BackAbility  _backAbility;
    private ChestAbility _chestAbility;
    private LegsAbility  _legsAbility;

    // Caches de componentes estáticos — cacheados en Awake
    private EnemyAttachmentManager _attachmentManager;

    [Header("Enemy Weight Penalty")]
    [Tooltip("Penalización de velocidad por cada enemigo pegado (0.15 = 15% por enemigo)")]
    [SerializeField] private float speedPenaltyPerEnemy = 0.15f;

    private Rigidbody rb;
    private new Transform transform;

    private bool isGrounded;
    private bool wasGrounded;
    private float lastJumpTime;
    private float lastGroundedTime;
    private bool jumpRequested;
    private bool jumpCut;
    // Buffers para ejecutar física SIEMPRE en FixedUpdate — nunca desde Update
    private bool _pendingJump;

    private float horizontalInput;
    private float verticalInput;

    // Sistema de momentum mejorado
    private Vector3 currentHorizontalVelocity;
    private Vector3 targetHorizontalVelocity;
    private float currentSpeedPercent;
    private float accelerationRate;

    [SerializeField] private InputReader inputReader;

    private void Awake()
    {
        rb                 = GetComponent<Rigidbody>();
        transform          = GetComponent<Transform>();
        _attachmentManager = GetComponent<EnemyAttachmentManager>();

        if (driftController == null)
            driftController = GetComponent<DriftController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        // Las abilities se añaden con AddComponent en runtime — refrescar cache por evento
        if (BodyPartManager.Instance != null)
            BodyPartManager.Instance.OnPartCollected += OnAbilityPartCollected;
    }

    private void OnEnable()
    {
        inputReader.OnJumpPerformed += HandleJumpPerformed;
        inputReader.OnJumpCanceled  += HandleJumpCanceled;
    }

    private void OnDisable()
    {
        inputReader.OnJumpPerformed -= HandleJumpPerformed;
        inputReader.OnJumpCanceled  -= HandleJumpCanceled;
    }

    private void OnDestroy()
    {
        if (BodyPartManager.Instance != null)
            BodyPartManager.Instance.OnPartCollected -= OnAbilityPartCollected;
    }

    // Llamado una sola vez cuando el jugador recoge una parte — no polling
    private void OnAbilityPartCollected(BodyPartType type)
    {
        switch (type)
        {
            case BodyPartType.Back:  _backAbility  = GetComponent<BackAbility>();  break;
            case BodyPartType.Chest: _chestAbility = GetComponent<ChestAbility>(); break;
            case BodyPartType.Legs:  _legsAbility  = GetComponent<LegsAbility>();  break;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        ReadInput();
        CheckGround();
        HandleJump();
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;

        if (_pendingJump) { ExecuteJump(); _pendingJump = false; }
        ApplyJumpCut();
        ApplyMomentum();
        ApplyGravity();
    }

    private void ReadInput()
    {
        horizontalInput = inputReader.MoveInput.x;
        verticalInput = inputReader.MoveInput.y;
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
            _pendingJump = true;
            jumpRequested = false;
        }
    }

    private void ExecuteJump()
    {
        // LegsAbility toma control del salto cuando está equipada
        if (_legsAbility != null)
        {
            Debug.Log("[PlayerLocomotion] LegsAbility activa — salto base cancelado");
            return;
        }

        Debug.Log($"[PlayerLocomotion] Ejecutando salto base - Fuerza: {config.jumpForce}");
        rb.AddForce(new Vector3(0f, -rb.linearVelocity.y, 0f), ForceMode.VelocityChange);
        rb.AddForce(Vector3.up * config.jumpForce, ForceMode.Impulse);
        lastGroundedTime = 0;
    }

    private void ApplyMomentum()
    {
        // No aplicar momentum si BackAbility está ejecutando un dash
        if (_backAbility != null && _backAbility.IsDashing)
            return;

        Vector3 movementDirection = Vector3.zero;

        if (cameraTransform != null)
        {
            // Proyectar vectores de la c�mara en el plano XZ (ignorar inclinaci�n Y)
            Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;

            movementDirection = (camForward * verticalInput + camRight * horizontalInput).normalized;
        }
        else
        {
            // Fallback a coordenadas globales si no hay c�mara
            movementDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
        }

        float baseMaxSpeed = GetAdjustedMaxSpeed(); // NUEVO: considera enemigos y chest

        // Obtener velocidad m�xima ajustada por boost de drift
        float effectiveMaxSpeed = baseMaxSpeed;
        if (driftController != null)
        {
            // GetBoostedMaxSpeed() devuelve config.maxSpeed * boostMultiplier
            // Necesitamos aplicar el mismo multiplier pero sobre baseMaxSpeed
            float boostMultiplier = driftController.GetBoostedMaxSpeed() / config.maxSpeed;
            effectiveMaxSpeed = baseMaxSpeed * boostMultiplier;
        }

        // Calcular velocidad objetivo
        targetHorizontalVelocity = movementDirection * effectiveMaxSpeed;

        // Obtener velocidad horizontal actual
        currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // NUEVA L�GICA DE ROTACI�N: Rotar hacia donde realmente nos movemos
        // Solo rotar si hay velocidad significativa
        if (currentHorizontalVelocity.magnitude > 1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentHorizontalVelocity);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                config.rotationSpeed * Time.fixedDeltaTime
            );
        }
        else if (movementDirection.magnitude >= 0.1f)
        {
            // Si estamos parados pero hay input, rotar hacia el input
            // Esto ayuda a empezar a moverse en la direcci�n correcta
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                config.rotationSpeed * Time.fixedDeltaTime
            );
        }

        // Actualizar sistema de drift
        if (driftController != null)
        {
            driftController.UpdateDrift(currentHorizontalVelocity, movementDirection, isGrounded);
        }

        // Obtener multiplicador de friccion del drift
        float frictionMultiplier = driftController != null ? driftController.GetFrictionMultiplier() : 1f;

        if (movementDirection.magnitude < 0.1f && isGrounded)
        {
            // Friccion: fuerza opuesta a la velocidad horizontal actual
            float effectiveFriction = config.friction * frictionMultiplier;
            rb.AddForce(-currentHorizontalVelocity * effectiveFriction, ForceMode.Acceleration);
            accelerationRate = 0f;
        }
        else if (movementDirection.magnitude >= 0.1f)
        {
            // Proyeccion de velocidad: acumular solo en la direccion deseada
            float speedInDesiredDir = Vector3.Dot(currentHorizontalVelocity, movementDirection);
            float remainingSpeed = effectiveMaxSpeed - speedInDesiredDir;

            if (remainingSpeed > 0f)
            {
                float currentSpeed = currentHorizontalVelocity.magnitude;
                currentSpeedPercent = effectiveMaxSpeed > 0f ? currentSpeed / effectiveMaxSpeed : 0f;

                float accelerationFactor = GetAccelerationFactor(currentSpeedPercent);
                accelerationRate = accelerationFactor * config.acceleration;

                if (driftController != null && driftController.IsDrifting)
                    accelerationRate *= 0.8f;

                // Limitar la fuerza para no superar la velocidad restante en este frame
                float forceToApply = Mathf.Min(accelerationRate, remainingSpeed / Time.fixedDeltaTime);
                rb.AddForce(movementDirection * forceToApply, ForceMode.Acceleration);
            }
            else
            {
                accelerationRate = 0f;
            }
        }

        // Cap suave: frena el exceso sin cortar knockbacks bruscamente
        ApplySoftSpeedCap(effectiveMaxSpeed);
    }

    private void ApplyJumpCut()
    {
        if (!jumpCut || rb.linearVelocity.y <= 0f) return;

        float targetY = rb.linearVelocity.y * config.jumpCutMultiplier;
        rb.AddForce(new Vector3(0f, targetY - rb.linearVelocity.y, 0f), ForceMode.VelocityChange);
        jumpCut = false;
    }

    private void ApplySoftSpeedCap(float maxSpeed)
    {
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float horizontalSpeed = horizontalVel.magnitude;
        if (horizontalSpeed > maxSpeed * 1.05f)
        {
            float excess = horizontalSpeed - maxSpeed;
            rb.AddForce(-horizontalVel.normalized * excess * config.friction, ForceMode.Acceleration);
        }
    }

    private float GetAccelerationFactor(float speedPercent)
    {
        // Curva de aceleraci�n m�s pronunciada
        float invertedPercent = 1f - speedPercent;
        float curveValue = invertedPercent * invertedPercent;
        return Mathf.Lerp(config.accelerationCurveEnd, config.accelerationCurveStart, curveValue);
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * config.gravityMultiplier, ForceMode.Acceleration);
        }
    }

    public bool IsGrounded() => isGrounded;
    public bool WasGrounded() => wasGrounded;
    public float GetCurrentSpeed() => currentHorizontalVelocity.magnitude;

    private void OnGUI()
    {
        if (!showDebugGUI) return;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 16;
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontStyle = FontStyle.Bold;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));

        float panelWidth = 350f;
        float panelHeight = 280f;
        float padding = 10f;

        GUILayout.BeginArea(new Rect(padding, padding, panelWidth, panelHeight), boxStyle);

        GUILayout.Label("=== PLAYER LOCOMOTION DEBUG ===", labelStyle);
        GUILayout.Space(10);

        // Velocidad
        float currentSpeed = currentHorizontalVelocity.magnitude;

        // Mostrar velocidad efectiva con boost
        float effectiveMaxSpeed = config.maxSpeed;
        if (driftController != null && driftController.IsBoostActive)
        {
            effectiveMaxSpeed = driftController.GetBoostedMaxSpeed();
            labelStyle.normal.textColor = Color.yellow;
        }
        else
        {
            labelStyle.normal.textColor = GetSpeedColor(currentSpeed);
        }

        GUILayout.Label($"Speed: {currentSpeed:F2} / {effectiveMaxSpeed:F2} m/s", labelStyle);

        if (_attachmentManager != null && _attachmentManager.AttachedCount > 0)
        {
            float adjustedMaxSpeed = GetAdjustedMaxSpeed();
            float speedReduction = ((config.maxSpeed - adjustedMaxSpeed) / config.maxSpeed) * 100f;

            labelStyle.normal.textColor = Color.red;
            GUILayout.Label($"Speed Penalty: -{speedReduction:F0}%", labelStyle);

            labelStyle.normal.textColor = Color.yellow;
            GUILayout.Label($"Max Speed: {adjustedMaxSpeed:F1} m/s", labelStyle);
        }

        // Barra de velocidad
        DrawProgressBar(currentSpeed / effectiveMaxSpeed, "Speed",
            driftController != null && driftController.IsBoostActive ? Color.yellow : Color.cyan);

        GUILayout.Space(5);

        // Porcentaje de velocidad
        labelStyle.normal.textColor = Color.white;
        GUILayout.Label($"Speed %: {(currentSpeedPercent * 100f):F1}%", labelStyle);

        // Aceleraci�n actual
        labelStyle.normal.textColor = accelerationRate > 0 ? Color.green : Color.gray;
        GUILayout.Label($"Acceleration Rate: {accelerationRate:F2}", labelStyle);

        // Barra de aceleraci�n
        float accelPercent = Mathf.Clamp01(accelerationRate / (config.acceleration * config.accelerationCurveStart));
        DrawProgressBar(accelPercent, "Accel", Color.green);

        GUILayout.Space(5);

        // Input
        labelStyle.normal.textColor = Color.yellow;
        GUILayout.Label($"Input: H={horizontalInput:F2} V={verticalInput:F2}", labelStyle);

        // Estado
        labelStyle.normal.textColor = isGrounded ? Color.green : Color.red;
        GUILayout.Label($"Grounded: {(isGrounded ? "YES" : "NO")}", labelStyle);

        labelStyle.normal.textColor = Color.white;
        GUILayout.Label($"Velocity Y: {rb.linearVelocity.y:F2}", labelStyle);

        GUILayout.EndArea();
    }

    private Color GetSpeedColor(float speed)
    {
        float percent = speed / config.maxSpeed;

        if (percent < 0.3f) return Color.red;
        if (percent < 0.6f) return Color.yellow;
        if (percent < 0.9f) return new Color(0.5f, 1f, 0.5f);
        return Color.green;
    }

    private void DrawProgressBar(float percent, string label, Color barColor)
    {
        float barWidth = 300f;
        float barHeight = 20f;

        Rect backgroundRect = GUILayoutUtility.GetRect(barWidth, barHeight);

        GUI.DrawTexture(backgroundRect, MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.8f)));

        Rect fillRect = new Rect(
            backgroundRect.x,
            backgroundRect.y,
            backgroundRect.width * Mathf.Clamp01(percent),
            backgroundRect.height
        );
        GUI.DrawTexture(fillRect, MakeTex(2, 2, barColor));

        GUIStyle percentStyle = new GUIStyle(GUI.skin.label);
        percentStyle.alignment = TextAnchor.MiddleCenter;
        percentStyle.fontStyle = FontStyle.Bold;
        percentStyle.normal.textColor = Color.white;
        GUI.Label(backgroundRect, $"{label}: {(percent * 100f):F0}%", percentStyle);
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    public void HandleJumpPerformed()
    {
        jumpRequested = true;
        lastJumpTime = Time.time;
    }

    public void HandleJumpCanceled()
    {
        if (rb.linearVelocity.y > 0)
        {
            jumpCut = true;
        }
    }

    /// <summary>
    /// Calcula la velocidad máxima ajustada considerando enemigos pegados y ChestAbility
    /// </summary>
    /// <summary>
    /// Calcula la velocidad máxima ajustada considerando enemigos pegados y ChestAbility
    /// </summary>
    private float GetAdjustedMaxSpeed()
    {
        float targetMaxSpeed = config.maxSpeed;

        if (_attachmentManager != null && _attachmentManager.AttachedCount > 0)
        {
            float speedPenalty = _attachmentManager.AttachedCount * speedPenaltyPerEnemy;

            if (_chestAbility != null)
                speedPenalty *= (1f - _chestAbility.GetWeightAssistanceMultiplier());

            targetMaxSpeed *= (1f - speedPenalty);
        }

        return targetMaxSpeed;
    }

    private void OnDrawGizmos()
    {
        if (transform == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1.1f);

        // Dibujar vector de velocidad
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, currentHorizontalVelocity);

        // Dibujar velocidad objetivo
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, targetHorizontalVelocity);

        if (driftController != null && driftController.IsDrifting)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
    }
}