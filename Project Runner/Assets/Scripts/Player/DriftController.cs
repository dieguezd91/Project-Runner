using UnityEngine;

public class DriftController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerConfigSO config;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private InputReader inputReader;

    [Header("Visual Feedback")]
    [SerializeField] private MeshRenderer playerRenderer;
    [SerializeField] private TrailRenderer boostTrail;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color chargingColor = new Color(1f, 0.8f, 0f); // Amarillo/naranja
    [SerializeField] private Color boostColor = new Color(0f, 1f, 1f); // Cyan

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // Estado del drift
    private bool isDrifting;
    private float driftCharge; // 0 a 1

    // Estado del boost
    private bool isBoostActive;
    private float boostEndTime;
    private float currentBoostMultiplier = 1f;

    // Para detecci�n mejorada
    private float driftTimer; // Tiempo continuo drifteando

    // Material para cambiar color
    private Material playerMaterial;
    private Color currentColor;

    // Threshold m�nimo para poder activar boost
    private const float MIN_CHARGE_FOR_BOOST = 0.3f;

    public bool IsDrifting => isDrifting;
    public float DriftCharge => driftCharge;
    public bool IsBoostActive => isBoostActive;
    public float CurrentBoostMultiplier => currentBoostMultiplier;
    public bool CanActivateBoost => driftCharge >= MIN_CHARGE_FOR_BOOST && !isBoostActive;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // Configurar material para feedback visual
        if (playerRenderer != null)
        {
            // Crear instancia del material para no afectar el material original
            playerMaterial = playerRenderer.material;
            currentColor = normalColor;
            playerMaterial.color = normalColor; // Asegurar que empiece con color normal
        }

        // Configurar trail (FORZAR desactivado al inicio)
        if (boostTrail != null)
        {
            boostTrail.emitting = false;
            boostTrail.enabled = true; // Componente activo pero no emitiendo
        }
    }

    private void OnEnable()
    {
        if (inputReader != null)
            inputReader.OnDashPerformed += TryActivateBoost;
    }

    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.OnDashPerformed -= TryActivateBoost;
    }

    private void Update()
    {
        UpdateBoost();
        UpdateVisualFeedback();
    }

    /// <summary>
    /// Intentar activar el boost si hay suficiente carga
    /// </summary>
    public void TryActivateBoost()
    {
        if (CanActivateBoost)
        {
            ActivateBoost();
        }
        else if (showDebug)
        {
            if (isBoostActive)
            {
                Debug.Log("Boost ya est� activo!");
            }
            else
            {
                Debug.Log($"Carga insuficiente: {(driftCharge * 100f):F0}% (m�nimo {(MIN_CHARGE_FOR_BOOST * 100f):F0}%)");
            }
        }
    }

    /// <summary>
    /// Actualizar feedback visual basado en el estado
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (playerMaterial == null) return;

        Color targetColor;

        if (isBoostActive)
        {
            // Color cyan brillante durante el boost
            targetColor = boostColor;

            // ASEGURAR que el trail est� activo
            if (boostTrail != null && !boostTrail.emitting)
            {
                boostTrail.emitting = true;
            }
        }
        else
        {
            // ASEGURAR que el trail est� desactivado cuando no hay boost
            if (boostTrail != null && boostTrail.emitting)
            {
                boostTrail.emitting = false;
            }

            if (driftCharge >= MIN_CHARGE_FOR_BOOST)
            {
                // BOOST DISPONIBLE - Color amarillo/naranja brillante con efecto pulsante
                float pulseIntensity = Mathf.Lerp(0.8f, 1f, Mathf.PingPong(Time.time * 3f, 1f));
                targetColor = chargingColor * pulseIntensity;
            }
            else if (isDrifting && driftCharge > 0.05f)
            {
                // CARGANDO - Transici�n gradual de blanco a amarillo
                targetColor = Color.Lerp(normalColor, chargingColor, driftCharge / MIN_CHARGE_FOR_BOOST);
            }
            else
            {
                // NORMAL - Color blanco
                targetColor = normalColor;
            }
        }

        // Transici�n suave de color
        // M�s r�pido cuando entra/sale de boost, m�s lento en otros casos
        float lerpSpeed = isBoostActive || driftCharge >= MIN_CHARGE_FOR_BOOST ? 12f : 6f;
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * lerpSpeed);
        playerMaterial.color = currentColor;
    }

    public void UpdateDrift(Vector3 currentVelocity, Vector3 inputDirection, bool isGrounded)
    {
        if (!isGrounded)
        {
            EndDrift();
            return;
        }

        float currentSpeed = currentVelocity.magnitude;

        if (currentSpeed < config.driftMinSpeed)
        {
            EndDrift();
            return;
        }

        if (inputDirection.magnitude < 0.1f)
        {
            EndDrift();
            return;
        }

        Vector3 velocityDirection = currentVelocity.normalized;
        Vector3 normalizedInput = inputDirection.normalized;

        float angleDifference = Vector3.Angle(normalizedInput, velocityDirection);

        if (showDebug)
        {
            Debug.DrawRay(transform.position + Vector3.up * 1f, velocityDirection * 3f, Color.cyan);
            Debug.DrawRay(transform.position + Vector3.up * 1.2f, normalizedInput * 3f, Color.yellow);
        }

        if (angleDifference >= config.driftAngleThreshold)
        {
            if (!isDrifting)
            {
                StartDrift();
            }

            driftTimer += Time.fixedDeltaTime;
            ChargeDrift();
        }
        else
        {
            // YA NO activamos boost autom�ticamente aqu�
            // El jugador debe presionar Shift para activarlo

            if (isDrifting)
            {
                if (showDebug)
                {
                    Debug.Log($"Drift terminado. Carga: {(driftCharge * 100f):F0}%. Presiona [Dash] para boost!");
                }
                EndDrift();
            }
            else
            {
                DischargeDrift();
            }
        }
    }

    private void StartDrift()
    {
        isDrifting = true;
        driftTimer = 0f;

        if (showDebug)
        {
            Debug.Log("Drift Started!");
        }
    }

    private void EndDrift()
    {
        if (isDrifting && showDebug)
        {
            Debug.Log($"Drift Ended! Duration: {driftTimer:F2}s, Charge: {(driftCharge * 100f):F0}%");
        }

        isDrifting = false;
        driftTimer = 0f;

        // Ya NO descargamos inmediatamente - mantener la carga para que el jugador la use
        // DischargeDrift(); <- QUITADO
    }

    private void ChargeDrift()
    {
        driftCharge += config.driftChargeRate * Time.fixedDeltaTime;
        driftCharge = Mathf.Clamp01(driftCharge);
    }

    private void DischargeDrift()
    {
        // Descargar m�s lentamente para dar tiempo a activar el boost
        driftCharge -= config.driftDischargeRate * Time.fixedDeltaTime * 0.5f; // 50% m�s lento
        driftCharge = Mathf.Max(0f, driftCharge);
    }

    private void ActivateBoost()
    {
        isBoostActive = true;
        boostEndTime = Time.time + config.driftBoostDuration;

        // El boost es proporcional a la carga acumulada
        currentBoostMultiplier = Mathf.Lerp(1f, config.driftBoostMultiplier, driftCharge);

        // Resetear carga
        driftCharge = 0f;

        // ACTIVAR trail EXPL�CITAMENTE
        if (boostTrail != null)
        {
            boostTrail.Clear(); // Limpiar trail anterior
            boostTrail.emitting = true;
        }

        if (showDebug)
        {
            Debug.Log($"<color=yellow>BOOST ACTIVADO! {currentBoostMultiplier:F2}x por {config.driftBoostDuration}s</color>");
        }
    }

    private void UpdateBoost()
    {
        if (isBoostActive && Time.time >= boostEndTime)
        {
            isBoostActive = false;
            currentBoostMultiplier = 1f;

            // DESACTIVAR trail EXPL�CITAMENTE
            if (boostTrail != null)
            {
                boostTrail.emitting = false;
            }

            if (showDebug)
            {
                Debug.Log("Boost terminado");
            }
        }
    }

    public float GetFrictionMultiplier()
    {
        if (isDrifting)
        {
            return config.driftFrictionMultiplier;
        }
        return 1f;
    }

    public float GetBoostedMaxSpeed()
    {
        if (isBoostActive)
        {
            return config.maxSpeed * currentBoostMultiplier;
        }
        return config.maxSpeed;
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        float x = 10f;
        float y = 310f;
        float w = 350f;
        float h = 25f;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 15;
        style.fontStyle = FontStyle.Bold;

        GUI.Box(new Rect(x, y, w, 210f), "");

        y += 10f;

        // TITULO
        style.normal.textColor = Color.magenta;
        GUI.Label(new Rect(x + 10, y, w - 20, h), "DRIFT SYSTEM", style);
        y += h + 5;

        // ESTADO DRIFT
        style.normal.textColor = isDrifting ? Color.cyan : Color.gray;
        string driftStatus = isDrifting ? "DRIFTING!" : "Normal";
        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Status: {driftStatus}", style);
        y += h;

        // TIMER DE DRIFT
        if (isDrifting)
        {
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 10, y, w - 20, h), $"Drift Time: {driftTimer:F2}s", style);
            y += h;
        }

        // CARGA DRIFT
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Drift Charge: {(driftCharge * 100f):F0}%", style);
        y += h;

        // BARRA DE CARGA con indicador de m�nimo
        DrawSimpleBar(x + 10, y, w - 20, 22f, driftCharge,
            driftCharge >= MIN_CHARGE_FOR_BOOST ? Color.yellow : Color.magenta);

        // L�nea indicadora del m�nimo
        float minLineX = x + 10 + (w - 20) * MIN_CHARGE_FOR_BOOST;
        GUI.DrawTexture(new Rect(minLineX, y, 2f, 22f), Texture2D.whiteTexture,
            ScaleMode.StretchToFill, true, 0, Color.green, 0, 0);

        y += 30f;

        // BOOST ACTIVO
        if (isBoostActive)
        {
            style.normal.textColor = Color.yellow;
            float remaining = boostEndTime - Time.time;
            GUI.Label(new Rect(x + 10, y, w - 20, h),
                $"BOOST! {currentBoostMultiplier:F2}x ({remaining:F1}s)", style);
        }
        else
        {
            style.normal.textColor = CanActivateBoost ? Color.yellow : Color.gray;
            string boostMsg = CanActivateBoost ?
                "Press [Dash] to BOOST!" :
                $"Boost: {((MIN_CHARGE_FOR_BOOST - driftCharge) * 100f):F0}% needed";
            GUI.Label(new Rect(x + 10, y, w - 20, h), boostMsg, style);
        }
        y += h;

        // INSTRUCCIONES
        style.fontSize = 12;
        style.normal.textColor = new Color(1f, 1f, 0.5f);
        GUI.Label(new Rect(x + 10, y, w - 20, h * 2),
            "Tip: Gira bruscamente para cargar.\n¡Presiona [Dash] cuando esté listo!", style);
    }

    private void DrawSimpleBar(float x, float y, float w, float h, float fill, Color col)
    {
        Rect bg = new Rect(x, y, w, h);
        GUI.DrawTexture(bg, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0,
            new Color(0.1f, 0.1f, 0.1f, 0.9f), 0, 0);

        if (fill > 0)
        {
            Rect fg = new Rect(x, y, w * Mathf.Clamp01(fill), h);
            GUI.DrawTexture(fg, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0,
                col, 0, 0);
        }

        GUI.Box(bg, "");

        GUIStyle ts = new GUIStyle(GUI.skin.label);
        ts.alignment = TextAnchor.MiddleCenter;
        ts.fontStyle = FontStyle.Bold;
        ts.fontSize = 13;
        ts.normal.textColor = Color.white;
        GUI.Label(bg, $"{(fill * 100f):F0}%", ts);
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || !Application.isPlaying) return;

        if (isDrifting)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f + driftCharge * 0.5f);
        }

        if (isBoostActive)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.5f);
        }

        // Indicador visual de carga suficiente
        if (CanActivateBoost && !isBoostActive)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.4f);
        }
    }
}