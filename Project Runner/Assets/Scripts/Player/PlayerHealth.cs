using UnityEngine;
using System;

/// <summary>
/// Maneja la vida del jugador: Shield, Energy, Damage, Muerte.
/// NO maneja attachment de enemigos (eso es EnemyAttachmentManager).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Resources")]
    [SerializeField] private float maxShield = 100f;
    [SerializeField] private float maxEnergy = 100f;

    [Header("Shield Regeneration by Speed")]
    [SerializeField] private float shieldDecaySlow = -20f;
    [SerializeField] private float shieldRegenSlow = 5f;
    [SerializeField] private float shieldRegenMedium = 15f;
    [SerializeField] private float shieldRegenFast = 25f;

    [Header("Damage Settings")]
    [SerializeField] private float frontalImpactDamage = 30f;
    [SerializeField] private float lateralImpactDamage = 10f;
    [SerializeField] private float impactAngleThreshold = 45f;

    [Header("Ballast Settings")]
    [SerializeField] private float shieldDecayPerEnemy = 5f;

    [Header("Grace Period")]
    [SerializeField] private float gracePeriodDuration = 2f;

    private PlayerLocomotion locomotion;
    private EnemyAttachmentManager attachmentManager;

    public float CurrentShield { get; private set; }
    public float CurrentEnergy { get; private set; }
    public bool IsInGracePeriod { get; private set; }
    public bool IsDead { get; private set; }

    private float gracePeriodTimer;

    public event Action OnPlayerDeath;
    public event Action<float> OnShieldChanged;
    public event Action<float> OnEnergyChanged;

    private void Awake()
    {
        locomotion = GetComponent<PlayerLocomotion>();
        attachmentManager = GetComponent<EnemyAttachmentManager>();

        CurrentShield = maxShield;
        CurrentEnergy = maxEnergy;
        IsDead = false;
    }

    private void Update()
    {
        if (IsDead) return;

        float currentSpeed = locomotion.GetCurrentSpeed();

        RegenerateShield(currentSpeed);
        HandleGracePeriod();
        ApplyBallastShieldDecay();
    }

    private void RegenerateShield(float speed)
    {
        float regenRate = GetShieldRegenRate(speed);

        if (IsInGracePeriod) regenRate = 0f;

        CurrentShield = Mathf.Clamp(CurrentShield + regenRate * Time.deltaTime, 0, maxShield);
        OnShieldChanged?.Invoke(CurrentShield);

        if (CurrentShield <= 0 && !IsInGracePeriod)
        {
            StartGracePeriod();
        }
    }

    private float GetShieldRegenRate(float speed)
    {
        if (speed < 10f) return shieldDecaySlow;
        if (speed < 20f) return shieldRegenSlow;
        if (speed < 30f) return shieldRegenMedium;
        return shieldRegenFast;
    }

    private void HandleGracePeriod()
    {
        if (!IsInGracePeriod) return;

        gracePeriodTimer -= Time.deltaTime;

        if (gracePeriodTimer <= 0)
        {
            IsInGracePeriod = false;
            CurrentShield = 10f;
            Debug.Log("Grace period terminado - Shield recuperado a 10%");
        }
    }

    private void StartGracePeriod()
    {
        IsInGracePeriod = true;
        gracePeriodTimer = gracePeriodDuration;
        Debug.Log("¡GRACE PERIOD ACTIVADO! 2 segundos");
    }

    private void ApplyBallastShieldDecay()
    {
        if (attachmentManager == null || !attachmentManager.HasAttachedEnemies) return;

        float decayRate = attachmentManager.GetShieldDecayRate(shieldDecayPerEnemy);
        CurrentShield -= decayRate * Time.deltaTime;

        if (CurrentShield < 0) CurrentShield = 0;
    }

    public void TakeDamage(float amount, string source = "Unknown")
    {
        if (IsDead) return;

        if (IsInGracePeriod)
        {
            Die($"Daño durante grace period ({source})");
            return;
        }

        CurrentShield -= amount;
        Debug.Log($"Daño: {amount:F1} de {source}. Shield: {CurrentShield:F1}%");

        OnShieldChanged?.Invoke(CurrentShield);
    }

    public void Die(string cause = "Unknown")
    {
        if (IsDead) return;

        IsDead = true;
        Debug.Log($"¡GAME OVER! Causa: {cause}");

        OnPlayerDeath?.Invoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsDead) return;

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Vector3 impactDirection = collision.contacts[0].normal;
            Vector3 movementDirection = transform.forward;
            float impactAngle = Vector3.Angle(-movementDirection, impactDirection);

            float damage = impactAngle < impactAngleThreshold ? frontalImpactDamage : lateralImpactDamage;
            TakeDamage(damage, "Obstáculo");
        }
    }

    private void OnGUI()
    {
        if (!locomotion.showDebugGUI) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));

        float x = 370f;
        float y = 10f;
        float w = 350f;
        float h = 320f;

        GUILayout.BeginArea(new Rect(x, y, w, h), boxStyle);

        style.normal.textColor = Color.white;
        GUILayout.Label("=== PLAYER HEALTH DEBUG ===", style);
        GUILayout.Space(10);

        // SHIELD
        style.normal.textColor = GetShieldColor(CurrentShield);
        GUILayout.Label($"Kinetic Shield: {CurrentShield:F1}%", style);
        DrawProgressBar(CurrentShield / maxShield, "Shield", style.normal.textColor);

        GUILayout.Space(5);

        // ENERGY
        style.normal.textColor = Color.cyan;
        GUILayout.Label($"Kinetic Energy: {CurrentEnergy:F1}", style);
        DrawProgressBar(CurrentEnergy / maxEnergy, "Energy", Color.cyan);

        GUILayout.Space(5);

        // GRACE PERIOD
        if (IsInGracePeriod)
        {
            style.normal.textColor = Color.red;
            GUILayout.Label($"⚠ GRACE PERIOD: {gracePeriodTimer:F2}s ⚠", style);
            GUILayout.Space(5);
        }

        // LASTRE (obtenido del AttachmentManager)
        if (attachmentManager != null)
        {
            var debugInfo = attachmentManager.GetDebugInfo();

            style.normal.textColor = Color.white;
            GUILayout.Label($"Enemigos Pegados: {debugInfo.count}/10", style);

            if (debugInfo.count > 0)
            {
                style.normal.textColor = Color.magenta;
                GUILayout.Label($"Masa Extra: +{debugInfo.mass:F1} kg", style);
                GUILayout.Label($"Drag Extra: +{debugInfo.drag:F2}", style);

                float currentSpeed = locomotion.GetCurrentSpeed();
                if (currentSpeed < 0.5f)
                {
                    style.normal.textColor = Color.red;
                    GUILayout.Label($"⚠ STATIONARY: {debugInfo.stationaryTime:F2}s / 1.0s", style);
                }

                float decayRate = attachmentManager.GetShieldDecayRate(shieldDecayPerEnemy);
                style.normal.textColor = Color.yellow;
                GUILayout.Label($"Shield Decay: -{decayRate:F1}%/s", style);
            }
        }

        GUILayout.Space(5);

        // ESTADO
        if (IsDead)
        {
            style.normal.textColor = Color.red;
            style.fontSize = 20;
            GUILayout.Label("☠ DEAD ☠", style);
        }

        GUILayout.EndArea();
    }

    private Color GetShieldColor(float shield)
    {
        if (shield < 25f) return Color.red;
        if (shield < 50f) return Color.yellow;
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
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}