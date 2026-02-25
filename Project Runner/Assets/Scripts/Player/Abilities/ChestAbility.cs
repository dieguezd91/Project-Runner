using UnityEngine;

public class ChestAbility : BodyPartAbility
{
    [Header("Weight Assistance (Always Active)")]
    [Tooltip("Reducción de penalización por peso (0.5 = 50% menos penalización)")]
    [SerializeField] private float weightAssistanceMultiplier = 0.5f;

    [Header("Double Jump (Only with Legs)")]
    private int jumpsRemaining = 0;
    private int maxAirJumps = 1;
    private bool hasDoubleJumped = false;
    private bool doubleJumpEnabled = false; // Flag para saber si está habilitado

    private LegsAbility legsAbility;

    [Header("Air Control")]
    private float originalAirDrag = 0f;
    private bool airControlActive = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    private float lastDoubleJumpTime = -999f;

    protected override void OnInitialize()
    {
        // inputReader inyectado por BodyPartManager via base.Initialize() — sin reflection
        // playerLocomotion cacheado por la base class
        if (inputReader == null)
        {
            Debug.LogError("[ChestAbility] InputReader no fue inyectado. Asigna el InputReader en BodyPartManager.");
            return;
        }

        originalAirDrag = rb.linearDamping;

        // Detectar si Legs ya está equipada (puede haberse añadido antes que Chest)
        legsAbility = GetComponent<LegsAbility>();
        if (legsAbility != null)
        {
            EnableDoubleJump();
        }
        else
        {
            // Escuchar si Legs se recoge después — sin polling en Update
            BodyPartManager.Instance.OnPartCollected += OnPartCollected;
        }

        Debug.Log($"[ChestAbility] Air Propellers habilitado | " +
                 $"Weight Assistance: {weightAssistanceMultiplier * 100f}% | " +
                 $"Double Jump: {(doubleJumpEnabled ? "Enabled" : "Disabled")}");
    }

    private void EnableDoubleJump()
    {
        doubleJumpEnabled = true;
        inputReader.OnJumpPerformed += TryDoubleJump;
        Debug.Log("[ChestAbility] Double Jump HABILITADO");
    }

    private void OnPartCollected(BodyPartType type)
    {
        if (type != BodyPartType.Legs || doubleJumpEnabled) return;

        legsAbility = GetComponent<LegsAbility>();
        if (legsAbility != null)
        {
            EnableDoubleJump();
            // Ya no necesitamos escuchar más eventos
            BodyPartManager.Instance.OnPartCollected -= OnPartCollected;
        }
    }

    private void OnDestroy()
    {
        if (inputReader != null && doubleJumpEnabled)
            inputReader.OnJumpPerformed -= TryDoubleJump;

        // Limpiar suscripción al evento de partes si nunca llegamos a tener Legs
        if (!doubleJumpEnabled && BodyPartManager.Instance != null)
            BodyPartManager.Instance.OnPartCollected -= OnPartCollected;

        if (airControlActive)
            rb.linearDamping = originalAirDrag;
    }

    private void Update()
    {
        if (playerLocomotion == null) return;

        // Resetear saltos disponibles cuando tocamos el suelo (solo si double jump está habilitado)
        if (doubleJumpEnabled && playerLocomotion.IsGrounded())
        {
            jumpsRemaining = maxAirJumps;
            hasDoubleJumped = false;

            // Restaurar drag al tocar suelo
            if (airControlActive)
            {
                rb.linearDamping = originalAirDrag;
                airControlActive = false;
            }
        }
        else if (!playerLocomotion.IsGrounded())
        {
            // Bonus pasivo: reducir drag en aire para mantener velocidad horizontal
            if (!airControlActive)
            {
                rb.linearDamping = originalAirDrag * (1f - partData.airVelocityConservation * 0.5f);
                airControlActive = true;
            }
        }
        else if (playerLocomotion.IsGrounded() && airControlActive)
        {
            // Restaurar drag si estamos en suelo y no tenemos double jump
            rb.linearDamping = originalAirDrag;
            airControlActive = false;
        }
    }

    private void TryDoubleJump()
    {
        // Verificación crítica: solo funciona si double jump está habilitado
        if (!doubleJumpEnabled)
        {
            return;
        }

        if (playerLocomotion == null)
        {
            return;
        }

        // Solo intentar double jump si estamos en el aire
        if (playerLocomotion.IsGrounded())
        {
            return; // El salto base lo maneja LegsAbility
        }

        if (!CanUseAbility())
        {
            if (showDebugGUI)
            {
                if (isOnCooldown)
                {
                    Debug.Log($"[ChestAbility] Double Jump en cooldown: {GetRemainingCooldown():F1}s");
                }
                else if (jumpsRemaining <= 0)
                {
                    Debug.Log("[ChestAbility] Sin saltos disponibles");
                }
            }
            return;
        }

        ExecuteDoubleJump();
    }

    protected override bool CheckCustomConditions()
    {
        if (playerLocomotion == null)
        {
            return false;
        }

        // Verificar que double jump está habilitado
        if (!doubleJumpEnabled)
        {
            return false;
        }

        // Verificar que estamos en el aire
        if (playerLocomotion.IsGrounded())
        {
            return false;
        }

        // Verificar que tenemos saltos disponibles
        if (jumpsRemaining <= 0)
        {
            return false;
        }

        return true;
    }

    private void ExecuteDoubleJump()
    {
        // Guardar velocidad horizontal actual
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Cancelar velocidad vertical actual
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Aplicar impulso de double jump
        rb.AddForce(Vector3.up * partData.doubleJumpForce, ForceMode.Impulse);

        // Consumir salto
        jumpsRemaining--;
        hasDoubleJumped = true;
        lastDoubleJumpTime = Time.time;

        // Iniciar cooldown
        StartCooldown();

        Debug.Log($"[ChestAbility] Double Jump ejecutado | " +
                 $"Fuerza: {partData.doubleJumpForce:F1} | " +
                 $"Velocidad horizontal conservada: {horizontalVelocity.magnitude:F2} m/s | " +
                 $"Saltos restantes: {jumpsRemaining}");
    }

    /// <summary>
    /// Método público para que LegsAbility consulte el multiplicador de asistencia
    /// </summary>
    public float GetWeightAssistanceMultiplier()
    {
        return weightAssistanceMultiplier;
    }

    protected override float GetCooldownDuration()
    {
        return partData.doubleJumpCooldown;
    }
}