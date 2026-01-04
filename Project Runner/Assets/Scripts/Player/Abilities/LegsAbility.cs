using NUnit.Framework;
using UnityEngine;
using static UnityEngine.InputSystem.Controls.AxisControl;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class LegsAbility : BodyPartAbility
{
    [Header("Dash Settings")]
    private bool isDashing;
    private float dashEndTime;
    private Vector3 dashDirection;

    private InputReader inputReader;

    [Header("Collision")]
    private RigidbodyConstraints originalConstraints;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;

    protected override void OnInitialize()
    {
        PlayerLocomotion locomotion = GetComponent<PlayerLocomotion>();
        if (locomotion != null)
        {
            var field = typeof(PlayerLocomotion).GetField("inputReader",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                inputReader = field.GetValue(locomotion) as InputReader;
            }
        }

        if (inputReader == null)
        {
            Debug.LogError("[LegsAbility] No se pudo obtener InputReader");
            return;
        }

        // Guardar constraints originales
        originalConstraints = rb.constraints;

        inputReader.OnDashPerformed += TryDash;
        Debug.Log("[LegsAbility] Dash habilitado - Presiona Shift para usarlo");
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.OnDashPerformed -= TryDash;
        }

        // Restaurar constraints al destruir
        if (rb != null)
        {
            rb.constraints = originalConstraints;
        }
    }

    private void Update()
    {
        if (isDashing && Time.time >= dashEndTime)
        {
            EndDash();
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            // Mantener velocidad del dash constante
            Vector3 dashVelocity = dashDirection * partData.dashForce;

            // Mantener Y velocity solo para gravedad leve, no para colisiones
            float currentYVelocity = rb.linearVelocity.y;

            // Clampear Y para evitar que suba mucho por colisiones
            currentYVelocity = Mathf.Clamp(currentYVelocity, -10f, 2f);

            rb.linearVelocity = new Vector3(dashVelocity.x, currentYVelocity, dashVelocity.z);
        }
    }

    private void TryDash()
    {
        Debug.Log("[LegsAbility] TryDash llamado");

        if (!CanUseAbility())
        {
            if (showDebugGUI && isOnCooldown)
            {
                Debug.Log($"[LegsAbility] Dash en cooldown: {GetRemainingCooldown():F1}s");
            }
            return;
        }

        ExecuteDash();
    }

    protected override bool CheckCustomConditions()
    {
        if (playerLocomotion != null && !playerLocomotion.IsGrounded())
        {
            if (showDebugGUI)
            {
                Debug.Log("[LegsAbility] Dash requiere estar en el suelo");
            }
            return false;
        }

        return true;
    }

    private void ExecuteDash()
    {
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (currentVelocity.magnitude > 0.1f)
        {
            dashDirection = currentVelocity.normalized;
        }
        else
        {
            dashDirection = transform.forward;
        }

        isDashing = true;
        dashEndTime = Time.time + partData.dashDuration;

        // Reducir drag temporalmente para que el dash no se frene
        rb.linearDamping = 0f;

        StartCooldown();

        Debug.Log($"[LegsAbility] Dash ejecutado - Force: {partData.dashForce}, Dir: {dashDirection}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDashing)
        {
            // Detectar colisión con CUALQUIER objeto sólido, no solo "Obstacle"
            // Verificar si la colisión es frontal (en la dirección del dash)
            Vector3 collisionNormal = collision.contacts[0].normal;
            float dotProduct = Vector3.Dot(dashDirection, -collisionNormal);

            // Si la colisión es frontal (dot > 0.5), detener el dash
            if (dotProduct > 0.5f)
            {
                Debug.Log($"[LegsAbility] Dash interrumpido por colisión frontal con {collision.gameObject.name}");

                // Detener movimiento horizontal
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

                EndDash();
            }
        }
    }

    private void EndDash()
    {
        isDashing = false;

        // Restaurar drag original
        rb.linearDamping = 0f; // O el valor que tengas configurado en el Rigidbody

        Debug.Log("[LegsAbility] Dash finalizado");
    }

    public bool IsDashing => isDashing;

    protected override float GetCooldownDuration()
    {
        return partData.cooldownDuration;
    }
}