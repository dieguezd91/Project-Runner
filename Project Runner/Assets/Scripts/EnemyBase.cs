using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Clase base para enemigos de The Swarm
/// Implementa comportamiento Boids (cohesión, separación, alineación)
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyConfigSO config;

    [Header("Visual")]
    [SerializeField] private MeshRenderer meshRenderer;

    // Estado
    public enum EnemyState
    {
        Dormido,      // Inactivo, esperando detección
        Persiguiendo  // Activo, persiguiendo al jugador
    }

    private EnemyState currentState = EnemyState.Dormido;
    private Transform targetTransform;
    private Rigidbody rb;
    private Vector3 velocity;

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
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    /// <summary>
    /// Inicializa el enemigo desde el pool
    /// </summary>
    public void Initialize(Vector3 position, Transform target, EnemyState initialState = EnemyState.Dormido)
    {
        transform.position = position;
        targetTransform = target;
        currentState = initialState;
        velocity = Vector3.zero;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Color visual según estado
        UpdateVisualState();
    }

    /// <summary>
    /// Activa el enemigo para que empiece a perseguir
    /// </summary>
    public void Activate()
    {
        if (currentState == EnemyState.Dormido)
        {
            currentState = EnemyState.Persiguiendo;
            UpdateVisualState();
        }
    }

    /// <summary>
    /// Desactiva el enemigo (volver a dormido)
    /// </summary>
    public void Deactivate()
    {
        currentState = EnemyState.Dormido;
        velocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        UpdateVisualState();
    }

    private void FixedUpdate()
    {
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

        // Aplicar aceleración
        velocity += totalForce.normalized * config.acceleration * Time.fixedDeltaTime;

        // Limitar velocidad
        if (velocity.magnitude > config.baseSpeed)
        {
            velocity = velocity.normalized * config.baseSpeed;
        }

        // Aplicar velocidad al rigidbody
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

        // Rotar hacia la dirección de movimiento
        if (velocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                config.rotationSpeed * Time.fixedDeltaTime
            );
        }
    }

    private void CalculateBoidForces()
    {
        // Encontrar vecinos cercanos
        FindNeighbors();

        // Calcular fuerza hacia el objetivo (jugador)
        targetForce = (targetTransform.position - transform.position).normalized;

        // Si no hay vecinos, solo perseguir
        if (neighbors.Count == 0)
        {
            cohesionForce = Vector3.zero;
            separationForce = Vector3.zero;
            alignmentForce = Vector3.zero;
            return;
        }

        // COHESIÓN: Moverse hacia el centro de masa de los vecinos
        Vector3 centerOfMass = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            centerOfMass += neighbor.Position;
        }
        centerOfMass /= neighbors.Count;
        cohesionForce = (centerOfMass - transform.position).normalized;

        // SEPARACIÓN: Alejarse de vecinos muy cercanos
        separationForce = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            float distance = Vector3.Distance(transform.position, neighbor.Position);
            if (distance < config.separationDistance && distance > 0)
            {
                Vector3 awayFromNeighbor = (transform.position - neighbor.Position).normalized;
                separationForce += awayFromNeighbor / distance; // Más fuerte cuanto más cerca
            }
        }
        if (neighbors.Count > 0)
        {
            separationForce /= neighbors.Count;
        }
        separationForce = separationForce.normalized;

        // ALINEACIÓN: Moverse en la dirección promedio de los vecinos
        Vector3 averageVelocity = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            averageVelocity += neighbor.Velocity;
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

        // Color según estado
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

    /// <summary>
    /// Devuelve el enemigo al pool
    /// </summary>
    public void ReturnToPool()
    {
        Deactivate();
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Dibujar radio de vecinos
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.neighborRadius);

        // Dibujar distancia de separación
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.separationDistance);

        // Dibujar vectores de fuerza
        if (currentState == EnemyState.Persiguiendo)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, targetForce * 2f);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, cohesionForce * 2f);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, separationForce * 2f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, alignmentForce * 2f);
        }
    }
}