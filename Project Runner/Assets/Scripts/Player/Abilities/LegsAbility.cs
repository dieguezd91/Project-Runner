using UnityEngine;

public class LegsAbility : BodyPartAbility
{
    [Header("Dash Settings")]
    private bool isDashing;
    private float dashEndTime;
    private Vector3 dashDirection;

    private InputReader inputReader;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;

    protected override void OnInitialize()
    {
        // Obtener InputReader desde PlayerLocomotion
        PlayerLocomotion locomotion = GetComponent<PlayerLocomotion>();
        if (locomotion != null)
        {
            // Usar reflexión para acceder al campo privado
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

        inputReader.OnDashPerformed += TryDash;
        Debug.Log("[LegsAbility] Dash habilitado - Presiona Shift para usarlo");
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.OnDashPerformed -= TryDash;
        }
    }

    private void Update()
    {
        if (isDashing && Time.time >= dashEndTime)
        {
            EndDash();
        }
    }

    private void TryDash()
    {
        Debug.Log("[LegsAbility] TryDash llamado"); // DEBUG

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

        Vector3 dashVelocity = dashDirection * partData.dashForce;
        rb.linearVelocity = new Vector3(dashVelocity.x, rb.linearVelocity.y, dashVelocity.z);

        isDashing = true;
        dashEndTime = Time.time + partData.dashDuration;

        StartCooldown();

        Debug.Log($"[LegsAbility] Dash ejecutado en dirección: {dashDirection}");
    }

    private void EndDash()
    {
        isDashing = false;
        Debug.Log("[LegsAbility] Dash finalizado");
    }

    public bool IsDashing => isDashing;

    protected override float GetCooldownDuration()
    {
        return partData.cooldownDuration;
    }
}