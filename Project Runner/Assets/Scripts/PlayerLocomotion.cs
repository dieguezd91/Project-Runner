using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    [Header("Configuration")]
    public PlayerConfigSO config;

    [Header("Debug")]
    public bool showDebugGUI = true;

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
    private float currentSpeedPercent; // Porcentaje de velocidad actual respecto a la máxima
    private float accelerationRate; // Tasa de aceleración actual para debug

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
        ApplyMomentum();
        ApplyGravity();
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

    private void ApplyMomentum()
    {
        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

        // Calcular velocidad objetivo
        targetHorizontalVelocity = movementDirection * config.maxSpeed;

        // Rotar jugador hacia la dirección de movimiento
        if (movementDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                config.rotationSpeed * Time.fixedDeltaTime
            );
        }

        // Obtener velocidad horizontal actual
        currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Calcular diferencia de velocidad
        Vector3 velocityDifference = targetHorizontalVelocity - currentHorizontalVelocity;
        float velocityDifferenceLength = velocityDifference.magnitude;

        // Aplicar fricción cuando no hay input
        if (movementDirection.magnitude < 0.1f && isGrounded)
        {
            currentHorizontalVelocity = Vector3.Lerp(
                currentHorizontalVelocity,
                Vector3.zero,
                config.friction * Time.fixedDeltaTime
            );
            accelerationRate = 0f;
        }
        else if (velocityDifferenceLength > 0.01f)
        {
            // Sistema de aceleración mejorado - más perceptible
            float currentSpeed = currentHorizontalVelocity.magnitude;
            currentSpeedPercent = currentSpeed / config.maxSpeed;

            // Aceleración con curva personalizada más pronunciada
            float accelerationFactor = GetAccelerationFactor(currentSpeedPercent);

            // Calcular la tasa de aceleración
            accelerationRate = accelerationFactor * config.acceleration;

            // Aplicar aceleración directamente a la velocidad
            Vector3 accelerationVector = movementDirection * accelerationRate * Time.fixedDeltaTime;
            currentHorizontalVelocity += accelerationVector;

            // Limitar a velocidad máxima
            if (currentHorizontalVelocity.magnitude > config.maxSpeed)
            {
                currentHorizontalVelocity = currentHorizontalVelocity.normalized * config.maxSpeed;
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
        // Curva de aceleración más pronunciada
        // Usa una curva exponencial para que la aceleración inicial sea MUY rápida
        // y se vaya reduciendo conforme nos acercamos a la velocidad máxima

        // Invertimos el porcentaje para que 1 = inicio, 0 = velocidad máxima
        float invertedPercent = 1f - speedPercent;

        // Aplicamos una curva cuadrática: más aceleración al inicio
        float curveValue = invertedPercent * invertedPercent;

        // Interpolamos entre start y end usando la curva
        return Mathf.Lerp(config.accelerationCurveEnd, config.accelerationCurveStart, curveValue);
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * config.gravityMultiplier, ForceMode.Acceleration);
        }
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public bool WasGrounded()
    {
        return wasGrounded;
    }

    public float GetCurrentSpeed()
    {
        return currentHorizontalVelocity.magnitude;
    }

    private void OnGUI()
    {
        if (!showDebugGUI) return;

        // Configuración del estilo
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 16;
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontStyle = FontStyle.Bold;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));

        // Panel de debug
        float panelWidth = 350f;
        float panelHeight = 280f;
        float padding = 10f;

        GUILayout.BeginArea(new Rect(padding, padding, panelWidth, panelHeight), boxStyle);

        GUILayout.Label("=== PLAYER LOCOMOTION DEBUG ===", labelStyle);
        GUILayout.Space(10);

        // Velocidad
        float currentSpeed = currentHorizontalVelocity.magnitude;
        labelStyle.normal.textColor = GetSpeedColor(currentSpeed);
        GUILayout.Label($"Speed: {currentSpeed:F2} / {config.maxSpeed:F2} m/s", labelStyle);

        // Barra de velocidad
        DrawProgressBar(currentSpeed / config.maxSpeed, "Speed", Color.cyan);

        GUILayout.Space(5);

        // Porcentaje de velocidad
        labelStyle.normal.textColor = Color.white;
        GUILayout.Label($"Speed %: {(currentSpeedPercent * 100f):F1}%", labelStyle);

        // Aceleración actual
        labelStyle.normal.textColor = accelerationRate > 0 ? Color.green : Color.gray;
        GUILayout.Label($"Acceleration Rate: {accelerationRate:F2}", labelStyle);

        // Barra de aceleración
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
        if (percent < 0.9f) return new Color(0.5f, 1f, 0.5f); // Verde claro
        return Color.green;
    }

    private void DrawProgressBar(float percent, string label, Color barColor)
    {
        float barWidth = 300f;
        float barHeight = 20f;

        Rect backgroundRect = GUILayoutUtility.GetRect(barWidth, barHeight);

        // Fondo
        GUI.DrawTexture(backgroundRect, MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.8f)));

        // Barra de progreso
        Rect fillRect = new Rect(
            backgroundRect.x,
            backgroundRect.y,
            backgroundRect.width * Mathf.Clamp01(percent),
            backgroundRect.height
        );
        GUI.DrawTexture(fillRect, MakeTex(2, 2, barColor));

        // Texto del porcentaje
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
    }
}