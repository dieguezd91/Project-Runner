using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Clase base para enemigos de The Swarm
/// Implementa comportamiento Boids (cohesión, separación, alineación)
/// v2: Con detección de suelo y constraints correctos
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyConfigSO config;

    [Header("Visual")]
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 3f; // Aumentado para seguridad
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundSnapSpeed = 10f;
    private float halfHeight;

    // Estado
    public enum EnemyState
    {
        Dormido,
        Persiguiendo
    }

    private EnemyState currentState = EnemyState.Dormido;
    private Transform targetTransform;
    private Rigidbody rb;
    private Vector3 velocity;

    // Ground state
    private bool isGrounded;
    private float targetGroundHeight;

    // Boids
    private List<EnemyBase> neighbors = new List<EnemyBase>();
    private Vector3 cohesionForce;
    private Vector3 separationForce;
    private Vector3 alignmentForce;
    private Vector3 targetForce;

    // Propiedades públicas
    public EnemyState CurrentState => currentState;
    public Vector3 Position => transform.position;
    public Vector3 Velocity => velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // CONFIGURACIÓN CRÍTICA DEL RIGIDBODY
        rb.useGravity = false; // Controlamos la altura manualmente
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ; // Solo permitir rotación en Y
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Suavizado de movimiento

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            halfHeight = col.bounds.extents.y;
        }
        else
        {
            halfHeight = 1f; // Valor fallback por defecto
        }
    }

    public void Initialize(Vector3 position, Transform target, EnemyState initialState = EnemyState.Dormido)
    {
        transform.position = position;
        targetTransform = target;
        currentState = initialState;
        velocity = Vector3.zero;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Detectar altura inicial del suelo
        CheckGround();

        UpdateVisualState();
    }

    public void Activate()
    {
        if (currentState == EnemyState.Dormido)
        {
            currentState = EnemyState.Persiguiendo;
            UpdateVisualState();
        }
    }

    public void Deactivate()
    {
        currentState = EnemyState.Dormido;
        velocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        UpdateVisualState();
    }

    private void FixedUpdate()
    {
        // SIEMPRE verificar y ajustar altura
        CheckGround();
        SnapToGround();

        if (currentState != EnemyState.Persiguiendo || targetTransform == null)
            return;

        // Calcular fuerzas Boids
        CalculateBoidForces();

        // Combinar todas las fuerzas
        Vector3 totalForce =
            targetForce * config.targetWeight +
            cohesionForce * config.cohesionWeight +
            separationForce * config.separationWeight +
            alignmentForce * config.alignmentWeight;

        // Aplicar aceleración (SOLO EN PLANO XZ)
        Vector3 horizontalForce = new Vector3(totalForce.x, 0f, totalForce.z);
        velocity += horizontalForce.normalized * config.acceleration * Time.fixedDeltaTime;

        // Limitar velocidad
        if (velocity.magnitude > config.baseSpeed)
        {
            velocity = velocity.normalized * config.baseSpeed;
        }

        // Aplicar velocidad al rigidbody (MANTENER Y actual)
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

        // Rotar hacia la dirección de movimiento (solo en Y)
        if (velocity.magnitude > 0.1f)
        {
            Vector3 lookDirection = new Vector3(velocity.x, 0f, velocity.z);
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                config.rotationSpeed * Time.fixedDeltaTime
            );
        }
    }

    /// <summary>
    /// Detecta el suelo debajo del enemigo usando SphereCast
    /// </summary>
    private void CheckGround()
    {
        // CORRECCIÓN 1: Levantar el origen del raycast para que empiece ARRIBA del enemigo
        // y no falle si el enemigo spawnea parcialmente hundido.
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;

        RaycastHit hit;

        // SphereCast hacia abajo
        // Aumentamos la distancia para compensar que subimos el origen
        if (Physics.SphereCast(rayOrigin, groundCheckRadius, Vector3.down, out hit, groundCheckDistance + 1.5f, groundLayer))
        {
            isGrounded = true;
            // CORRECCIÓN 2: Sumar halfHeight. Ahora hoverHeight es el espacio de aire real bajo los pies.
            targetGroundHeight = hit.point.y + halfHeight;
        }
        else
        {
            isGrounded = false;
            // Fallback suave
            targetGroundHeight = transform.position.y - (2f * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Ajusta suavemente la posición Y para mantener altura sobre el suelo
    /// </summary>
    private void SnapToGround()
    {
        if (!isGrounded)
        {
            // Gravedad manual suave si está en el aire
            transform.position += Vector3.down * 5f * Time.fixedDeltaTime;
            return;
        }

        Vector3 currentPos = transform.position;

        // CORRECCIÓN: Usar un umbral pequeño. Si está muy cerca, hacemos snap directo.
        if (Mathf.Abs(currentPos.y - targetGroundHeight) < 0.05f)
        {
            transform.position = new Vector3(currentPos.x, targetGroundHeight, currentPos.z);
        }
        else
        {
            // CORRECCIÓN: Usar MoveTowards en lugar de Lerp para movimiento lineal y estable
            float newY = Mathf.MoveTowards(currentPos.y, targetGroundHeight, groundSnapSpeed * Time.fixedDeltaTime);
            transform.position = new Vector3(currentPos.x, newY, currentPos.z);
        }

        // Asegurar que no haya fuerzas físicas verticales residuales
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    private void CalculateBoidForces()
    {
        FindNeighbors();

        // Fuerza hacia el objetivo (SOLO EN PLANO XZ)
        Vector3 directionToTarget = targetTransform.position - transform.position;
        directionToTarget.y = 0; // Ignorar diferencia de altura
        targetForce = directionToTarget.normalized;

        if (neighbors.Count == 0)
        {
            cohesionForce = Vector3.zero;
            separationForce = Vector3.zero;
            alignmentForce = Vector3.zero;
            return;
        }

        // COHESIÓN
        Vector3 centerOfMass = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            centerOfMass += neighbor.Position;
        }
        centerOfMass /= neighbors.Count;

        Vector3 toCenterOfMass = centerOfMass - transform.position;
        toCenterOfMass.y = 0; // Solo plano XZ
        cohesionForce = toCenterOfMass.normalized;

        // SEPARACIÓN
        separationForce = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            Vector3 toNeighbor = transform.position - neighbor.Position;
            toNeighbor.y = 0; // Solo plano XZ

            float distance = toNeighbor.magnitude;
            if (distance < config.separationDistance && distance > 0)
            {
                separationForce += toNeighbor.normalized / distance;
            }
        }
        if (neighbors.Count > 0)
        {
            separationForce /= neighbors.Count;
        }
        separationForce = separationForce.normalized;

        // ALINEACIÓN
        Vector3 averageVelocity = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            Vector3 neighborVel = neighbor.Velocity;
            neighborVel.y = 0; // Solo plano XZ
            averageVelocity += neighborVel;
        }
        if (neighbors.Count > 0)
        {
            averageVelocity /= neighbors.Count;
            alignmentForce = averageVelocity.normalized;
        }
        else
        {
            alignmentForce = Vector3.zero;
        }
    }

    private void FindNeighbors()
    {
        neighbors.Clear();

        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            config.neighborRadius,
            config.enemyLayer
        );

        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject == gameObject)
                continue;

            EnemyBase otherEnemy = collider.GetComponent<EnemyBase>();
            if (otherEnemy != null && otherEnemy.CurrentState == EnemyState.Persiguiendo)
            {
                neighbors.Add(otherEnemy);
            }
        }
    }

    private void UpdateVisualState()
    {
        if (meshRenderer == null) return;

        switch (currentState)
        {
            case EnemyState.Dormido:
                meshRenderer.material.color = Color.gray;
                break;
            case EnemyState.Persiguiendo:
                meshRenderer.material.color = Color.red;
                break;
        }
    }

    public void ReturnToPool()
    {
        Deactivate();
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Radio de vecinos
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.neighborRadius);

        // Distancia de separación
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.separationDistance);

        // Ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawRay(rayStart, Vector3.down * groundCheckDistance);

        if (isGrounded)
        {
            // Mostrar altura objetivo
            Gizmos.color = Color.cyan;
            Vector3 targetPos = new Vector3(transform.position.x, targetGroundHeight, transform.position.z);
            Gizmos.DrawWireSphere(targetPos, 0.3f);
            Gizmos.DrawLine(transform.position, targetPos);
        }

        // Vectores de fuerza (solo si está activo)
        if (currentState == EnemyState.Persiguiendo && targetTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, targetForce * 2f);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, cohesionForce * 2f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, separationForce * 2f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, alignmentForce * 2f);
        }
    }
}