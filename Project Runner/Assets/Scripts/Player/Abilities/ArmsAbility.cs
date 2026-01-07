using UnityEngine;

public class ArmsAbility : BodyPartAbility
{
    [Header("Auto Destroy Settings")]
    [Tooltip("Distancia a la que se activa la destrucción automática")]
    [SerializeField] private float autoDestroyDistance = 2f;

    private float lastAutoDestroyTime = -999f;

    private PlayerLocomotion locomotion;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    private int objectsDestroyedCount = 0;

    protected override void OnInitialize()
    {
        locomotion = GetComponent<PlayerLocomotion>();

        Debug.Log($"[ArmsAbility] Kinetic Arms habilitado | " +
                 $"Auto Destroy Distance: {autoDestroyDistance}m | " +
                 $"Collision Speed Retention: {partData.collisionSpeedRetention * 100f}%");
    }

    private void Update()
    {
        if (playerLocomotion == null)
        {
            return;
        }

        // Solo funciona en el suelo y si no estamos en cooldown
        if (!playerLocomotion.IsGrounded() || isOnCooldown)
        {
            return;
        }

        // Detectar obstáculos automáticamente
        TryAutoDestroy();
    }

    private void TryAutoDestroy()
    {
        // Detectar obstáculos en dirección de movimiento
        Vector3 destroyDirection = rb.linearVelocity.normalized;
        if (destroyDirection.magnitude < 0.1f)
        {
            destroyDirection = transform.forward;
        }

        // Punto de origen del SphereCast
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // Radio del SphereCast
        float sphereRadius = 0.5f;

        // Usar SphereCast para detectar obstáculos
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            sphereRadius,
            destroyDirection,
            autoDestroyDistance
        );

        // Procesar hits
        foreach (RaycastHit hit in hits)
        {
            // Verificar si es un obstáculo destruible
            if (hit.collider.CompareTag("Obstacle"))
            {
                ExecuteDestroy(hit.collider.gameObject, destroyDirection);
                break; // Solo destruir uno por vez
            }
        }
    }

    private void ExecuteDestroy(GameObject obstacle, Vector3 destroyDirection)
    {
        // Mantener porcentaje de velocidad del player
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 retainedVelocity = currentVelocity * partData.collisionSpeedRetention;
        rb.linearVelocity = retainedVelocity;

        // Destruir el obstáculo
        Destroy(obstacle);
        objectsDestroyedCount++;
        lastAutoDestroyTime = Time.time;

        // Iniciar cooldown
        StartCooldown();

        Debug.Log($"[ArmsAbility] ✓ Obstáculo AUTO-DESTRUIDO: {obstacle.name} | " +
                 $"Total destruidos: {objectsDestroyedCount} | " +
                 $"Velocidad retenida: {currentVelocity.magnitude:F2} → {retainedVelocity.magnitude:F2} m/s");
    }

    protected override bool CheckCustomConditions()
    {
        // Auto-destroy no necesita condiciones extra
        return true;
    }

    /// <summary>
    /// Método para aplicar mitigación de colisiones (bonus pasivo)
    /// </summary>
    public float GetCollisionSpeedRetention()
    {
        return partData.collisionSpeedRetention;
    }

    protected override float GetCooldownDuration()
    {
        return partData.destroyCooldown;
    }

    private void OnDrawGizmosSelected()
    {
        if (rb == null) return;

        // Detectar dirección de destrucción
        Vector3 destroyDirection = rb.linearVelocity.magnitude > 0.1f
            ? rb.linearVelocity.normalized
            : transform.forward;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float sphereRadius = 0.5f;

        // Dibujar el SphereCast
        Gizmos.color = isOnCooldown ? Color.gray : Color.red;

        // Esfera de inicio
        Gizmos.DrawWireSphere(origin, sphereRadius);

        // Línea de dirección
        Gizmos.DrawRay(origin, destroyDirection * autoDestroyDistance);

        // Esfera de final
        Gizmos.DrawWireSphere(origin + destroyDirection * autoDestroyDistance, sphereRadius);
    }
}