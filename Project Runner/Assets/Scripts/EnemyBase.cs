using UnityEngine;
using System.Collections.Generic;

public class EnemyBase : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyConfigSO config;

    [Header("Visual")]
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 3f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundSnapSpeed = 10f;
    private float halfHeight;

    // Estado
    public enum EnemyState
    {
        Sleeping,
        Pursuing
    }

    private EnemyState currentState = EnemyState.Sleeping;
    private Transform targetTransform;
    private Rigidbody rb;
    private Vector3 velocity;
    private float sqrDetectionRadius;

    // Boids
    private List<EnemyBase> neighbors = new List<EnemyBase>();
    private Vector3 cohesionForce, separationForce, alignmentForce, targetForce;
    private bool isGrounded;
    private float targetGroundHeight;

    // Propiedades públicas
    public EnemyState CurrentState => currentState;
    public Vector3 Position => transform.position;
    public Vector3 Velocity => velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            halfHeight = col.bounds.extents.y;
        else
            halfHeight = 1f;      

        if (config != null)
            sqrDetectionRadius = config.baseDetectionRadius * config.baseDetectionRadius;
    }

    public void Initialize(Vector3 position, Transform target, EnemyState initialState = EnemyState.Sleeping)
    {
        transform.position = position;
        targetTransform = target;
        currentState = initialState;
        velocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;

        CheckGround();
        UpdateVisualState();
    }

    public void Activate()
    {
        if (currentState == EnemyState.Sleeping)
        {
            currentState = EnemyState.Pursuing;
            UpdateVisualState();
        }
    }

    public void Deactivate()
    {
        currentState = EnemyState.Sleeping;
        velocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        UpdateVisualState();
    }

    private void Update()
    {
        if (currentState == EnemyState.Sleeping && targetTransform != null)
        {
            float sqrDistToTarget = (targetTransform.position - transform.position).sqrMagnitude;

            if (sqrDistToTarget < sqrDetectionRadius)
            {
                Activate();
            }
        }
    }

    private void FixedUpdate()
    {
        CheckGround();
        SnapToGround();

        if (currentState != EnemyState.Pursuing || targetTransform == null) return;

        CalculateBoidForces();
        ApplyMovement();
    }

    private void CheckGround()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;

        RaycastHit hit;

        if (Physics.SphereCast(rayOrigin, groundCheckRadius, Vector3.down, out hit, groundCheckDistance + 1.5f, groundLayer))
        {
            isGrounded = true;
            targetGroundHeight = hit.point.y + halfHeight;
        }
        else
        {
            isGrounded = false;
            targetGroundHeight = transform.position.y - (2f * Time.fixedDeltaTime);
        }
    }

    private void SnapToGround()
    {
        if (!isGrounded)
        {
            transform.position += Vector3.down * 5f * Time.fixedDeltaTime;
            return;
        }

        Vector3 currentPos = transform.position;

        if (Mathf.Abs(currentPos.y - targetGroundHeight) < 0.05f)
        {
            transform.position = new Vector3(currentPos.x, targetGroundHeight, currentPos.z);
        }
        else
        {
            float newY = Mathf.MoveTowards(currentPos.y, targetGroundHeight, groundSnapSpeed * Time.fixedDeltaTime);
            transform.position = new Vector3(currentPos.x, newY, currentPos.z);
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    private void CalculateBoidForces()
    {
        FindNeighbors();

        Vector3 directionToTarget = targetTransform.position - transform.position;
        directionToTarget.y = 0;
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
        toCenterOfMass.y = 0;
        cohesionForce = toCenterOfMass.normalized;

        // SEPARACIÓN
        separationForce = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            Vector3 toNeighbor = transform.position - neighbor.Position;
            toNeighbor.y = 0;

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
            neighborVel.y = 0;
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
            if (otherEnemy != null && otherEnemy.CurrentState == EnemyState.Pursuing)
            {
                neighbors.Add(otherEnemy);
            }
        }
    }

    private void ApplyMovement()
    {
        Vector3 totalForce = targetForce * config.targetWeight +
                             cohesionForce * config.cohesionWeight +
                             separationForce * config.separationWeight +
                             alignmentForce * config.alignmentWeight;

        Vector3 horizontalForce = new Vector3(totalForce.x, 0f, totalForce.z);
        velocity += horizontalForce.normalized * config.acceleration * Time.fixedDeltaTime;

        if (velocity.magnitude > config.baseSpeed)
            velocity = velocity.normalized * config.baseSpeed;

        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

        if (velocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(velocity.x, 0f, velocity.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, config.rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void UpdateVisualState()
    {
        if (meshRenderer == null) return;
        meshRenderer.material.color = currentState == EnemyState.Sleeping ? Color.gray : Color.red;
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
        if (currentState == EnemyState.Pursuing && targetTransform != null)
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