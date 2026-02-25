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

        ApplyMomentum();
        ApplyGravity();
    }

    //private void ReadInput()
    //{
    //    horizontalInput = Input.GetAxisRaw("Horizontal");
    //    verticalInput = Input.GetAxisRaw("Vertical");

    //    if (Input.GetButtonDown("Jump"))
    //    {
    //        jumpRequested = true;
    //        lastJumpTime = Time.time;
    //    }

    //    if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
    //    {
    //        jumpCut = true;
    //    }
    //}

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
        // LegsAbility toma control del salto cuando está equipada
        if (_legsAbility != null)
        {
            Debug.Log("[PlayerLocomotion] LegsAbility activa — salto base cancelado");
            return;
        }

        Debug.Log($"[PlayerLocomotion] Ejecutando salto base - Fuerza: {config.jumpForce}");
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
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

        // Calcular diferencia de velocidad
        Vector3 velocityDifference = targetHorizontalVelocity - currentHorizontalVelocity;
        float velocityDifferenceLength = velocityDifference.magnitude;

        // Obtener multiplicador de fricci�n del drift
        float frictionMultiplier = 1f;
        if (driftController != null)
        {
            frictionMultiplier = driftController.GetFrictionMultiplier();
        }

        // Aplicar fricci�n cuando no hay input
        if (movementDirection.magnitude < 0.1f && isGrounded)
        {
            // Fricci�n ajustada por drift
            float effectiveFriction = config.friction * frictionMultiplier;

            currentHorizontalVelocity = Vector3.Lerp(
                currentHorizontalVelocity,
                Vector3.zero,
                effectiveFriction * Time.fixedDeltaTime
            );
            accelerationRate = 0f;
        }
        else if (velocityDifferenceLength > 0.01f)
        {
            // Sistema de aceleraci�n mejorado
            float currentSpeed = currentHorizontalVelocity.magnitude;
            currentSpeedPercent = currentSpeed / effectiveMaxSpeed;

            // Aceleraci�n con curva personalizada
            float accelerationFactor = GetAccelerationFactor(currentSpeedPercent);

            // Calcular la tasa de aceleraci�n
            accelerationRate = accelerationFactor * config.acceleration;

            // Durante drift, reducir ligeramente la aceleraci�n para mantener el slide
            if (driftController != null && driftController.IsDrifting)
            {
                accelerationRate *= 0.8f; // 20% menos aceleraci�n durante drift
            }

            // Aplicar aceleraci�n directamente a la velocidad
            Vector3 accelerationVector = movementDirection * accelerationRate * Time.fixedDeltaTime;
            currentHorizontalVelocity += accelerationVector;

            // Limitar a velocidad m�xima efectiva
            if (currentHorizontalVelocity.magnitude > effectiveMaxSpeed)
            {
                currentHorizontalVelocity = currentHorizontalVelocity.normalized * effectiveMaxSpeed;
            }
        }

        // Aplicar velocidad horizontal al rigidbody
        rb.linearVelocity = new Vector3(
            currentHorizontalVelocity.x,
            rb.linearVelocity.y,
            currentHorizontalVelocity.z
        );
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