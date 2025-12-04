using UnityEngine;

/// <summary>
/// Sistema de Drift MEJORADO - Detecta cuando el input intenta girar pero la velocidad mantiene dirección
/// </summary>
public class DriftController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerConfigSO config;
    [SerializeField] private Rigidbody rb;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // Estado del drift
    private bool isDrifting;
    private float driftCharge; // 0 a 1

    // Estado del boost
    private bool isBoostActive;
    private float boostEndTime;
    private float currentBoostMultiplier = 1f;

    // Para detección mejorada
    private float driftTimer; // Tiempo continuo drifteando

    public bool IsDrifting => isDrifting;
    public float DriftCharge => driftCharge;
    public bool IsBoostActive => isBoostActive;
    public float CurrentBoostMultiplier => currentBoostMultiplier;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        UpdateBoost();
    }

    /// <summary>
    /// Actualiza el estado del drift - llamar desde PlayerLocomotion.FixedUpdate
    /// NUEVA VERSION: Compara input direction vs velocity direction
    /// </summary>
    public void UpdateDrift(Vector3 currentVelocity, Vector3 inputDirection, bool isGrounded)
    {
        if (!isGrounded)
        {
            // No se puede driftear en el aire
            EndDrift();
            return;
        }

        float currentSpeed = currentVelocity.magnitude;

        // Verificar velocidad mínima
        if (currentSpeed < config.driftMinSpeed)
        {
            EndDrift();
            return;
        }

        // Si no hay input, no hay drift
        if (inputDirection.magnitude < 0.1f)
        {
            EndDrift();
            return;
        }

        // NUEVA LÓGICA: Comparar dirección del input vs dirección de la velocidad
        Vector3 velocityDirection = currentVelocity.normalized;
        Vector3 normalizedInput = inputDirection.normalized;

        // Calcular ángulo entre donde QUIERES ir (input) y donde VAS (velocidad)
        float angleDifference = Vector3.Angle(normalizedInput, velocityDirection);

        // Debug visual
        if (showDebug)
        {
            Debug.DrawRay(transform.position + Vector3.up * 1f, velocityDirection * 3f, Color.cyan); // Velocidad
            Debug.DrawRay(transform.position + Vector3.up * 1.2f, normalizedInput * 3f, Color.yellow); // Input
        }

        // Si el ángulo es mayor al threshold, estamos intentando girar = DRIFT
        if (angleDifference >= config.driftAngleThreshold)
        {
            // Comenzar o continuar drift
            if (!isDrifting)
            {
                StartDrift();
            }

            driftTimer += Time.fixedDeltaTime;
            ChargeDrift();
        }
        else
        {
            // El input y la velocidad están alineados = NO drift
            if (isDrifting)
            {
                // Acabamos de terminar un drift, activar boost
                if (driftCharge > 0.1f && !isBoostActive)
                {
                    ActivateBoost();
                }
                EndDrift();
            }
            else
            {
                // Solo descargar si no estamos drifteando
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
            Debug.Log($"Drift Ended! Duration: {driftTimer:F2}s, Charge: {driftCharge:F2}");
        }

        isDrifting = false;
        driftTimer = 0f;
        DischargeDrift();
    }

    private void ChargeDrift()
    {
        driftCharge += config.driftChargeRate * Time.fixedDeltaTime;
        driftCharge = Mathf.Clamp01(driftCharge);
    }

    private void DischargeDrift()
    {
        driftCharge -= config.driftDischargeRate * Time.fixedDeltaTime;
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

        if (showDebug)
        {
            Debug.Log($"<color=yellow>DRIFT BOOST ACTIVATED! {currentBoostMultiplier:F2}x for {config.driftBoostDuration}s</color>");
        }
    }

    private void UpdateBoost()
    {
        if (isBoostActive && Time.time >= boostEndTime)
        {
            isBoostActive = false;
            currentBoostMultiplier = 1f;

            if (showDebug)
            {
                Debug.Log("Boost Ended");
            }
        }
    }

    /// <summary>
    /// Obtiene el multiplicador de fricción actual
    /// </summary>
    public float GetFrictionMultiplier()
    {
        if (isDrifting)
        {
            return config.driftFrictionMultiplier;
        }
        return 1f;
    }

    /// <summary>
    /// Obtiene la velocidad máxima ajustada por el boost
    /// </summary>
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
        float y = 310f; // Debajo del panel de locomotion
        float w = 350f;
        float h = 25f;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 15;
        style.fontStyle = FontStyle.Bold;

        // Fondo
        GUI.Box(new Rect(x, y, w, 180f), "");

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

        // BARRA DE CARGA
        DrawSimpleBar(x + 10, y, w - 20, 22f, driftCharge, Color.magenta);
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
            style.normal.textColor = Color.gray;
            GUI.Label(new Rect(x + 10, y, w - 20, h), "Boost: Inactive", style);
        }
        y += h;

        // INSTRUCCIONES
        style.fontSize = 12;
        style.normal.textColor = new Color(1f, 1f, 0.5f);
        GUI.Label(new Rect(x + 10, y, w - 20, h * 2),
            "Tip: Corre rápido y gira bruscamente\npara acumular carga de drift!", style);
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

        // Indicador visual de drift
        if (isDrifting)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f + driftCharge * 0.5f);
        }

        // Indicador de boost
        if (isBoostActive)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.5f);
        }
    }
}