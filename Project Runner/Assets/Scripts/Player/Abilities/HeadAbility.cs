using UnityEngine;

public class HeadAbility : BodyPartAbility
{
    [Header("Stomp Settings")]
    private bool isStomping;
    private bool stompActivated;
    private float stompStartHeight;

    private InputReader inputReader;

    [Header("Ground Impact")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool showImpactRadius = true;

    [Header("Synergy Requirements")]
    [Tooltip("El Stomp requiere tener Legs equipadas para funcionar")]
    [SerializeField] private bool requiresLegs = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;

    private BodyPartManager collectionManager;

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
            Debug.LogError("[HeadAbility] No se pudo obtener InputReader");
            return;
        }

        // Obtener referencia al BodyPartCollectionManager
        collectionManager = GetComponent<BodyPartManager>();

        // El stomp se activa con el mismo botón que el stomp del sistema de vida
        inputReader.OnStompPerformed += TryStomp;

        Debug.Log("[HeadAbility] Stomp habilitado - Presiona Ctrl en el aire para usarlo");

        if (requiresLegs)
        {
            Debug.Log("[HeadAbility] NOTA: El Stomp requiere tener Legs equipadas para destruir obstáculos");
        }
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.OnStompPerformed -= TryStomp;
        }
    }

    private void Update()
    {
        // Detectar cuando aterrizamos después de un stomp
        if (isStomping && playerLocomotion != null)
        {
            if (playerLocomotion.IsGrounded() && stompActivated)
            {
                ExecuteGroundImpact();
            }
        }
    }

    private void FixedUpdate()
    {
        if (isStomping && stompActivated)
        {
            // Aplicar fuerza hacia abajo constantemente durante el stomp
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x * 0.5f, // Reducir velocidad horizontal
                -partData.stompForce,
                rb.linearVelocity.z * 0.5f
            );
        }
    }

    private void TryStomp()
    {
        Debug.Log("[HeadAbility] TryStomp llamado");

        if (!CanUseAbility())
        {
            if (showDebugGUI && isOnCooldown)
            {
                Debug.Log($"[HeadAbility] Stomp en cooldown: {GetRemainingCooldown():F1}s");
            }
            return;
        }

        ExecuteStomp();
    }

    protected override bool CheckCustomConditions()
    {
        // Solo permitir stomp si estamos en el AIRE
        if (playerLocomotion != null && playerLocomotion.IsGrounded())
        {
            if (showDebugGUI)
            {
                Debug.Log("[HeadAbility] Stomp requiere estar en el aire");
            }
            return false;
        }

        // No permitir stomp si ya estamos cayendo muy rápido
        if (rb.linearVelocity.y < -20f)
        {
            if (showDebugGUI)
            {
                Debug.Log("[HeadAbility] Ya estás cayendo demasiado rápido");
            }
            return false;
        }

        return true;
    }

    private void ExecuteStomp()
    {
        isStomping = true;
        stompActivated = true;
        stompStartHeight = transform.position.y;

        // Cancelar velocidad vertical actual y empezar a caer
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        StartCooldown();

        Debug.Log($"[HeadAbility] Stomp activado desde altura: {stompStartHeight:F2}");
    }

    private void ExecuteGroundImpact()
    {
        float fallDistance = stompStartHeight - transform.position.y;

        Debug.Log($"[HeadAbility] Impacto en el suelo - Distancia caída: {fallDistance:F2}m");

        // Verificar si el jugador tiene Legs equipadas para la sinergia completa
        bool hasLegs = HasRequiredPart(BodyPartType.Legs);

        if (requiresLegs && !hasLegs)
        {
            Debug.LogWarning("[HeadAbility] Stomp requiere Legs equipadas para destruir obstáculos. Solo eliminará enemigos.");
        }

        // Detectar TODOS los colliders en el radio (enemigos + obstáculos)
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            partData.stompRadius
        );

        int enemiesKilled = 0;
        int obstaclesDestroyed = 0;

        foreach (Collider col in hitColliders)
        {
            // Verificar si es un enemigo
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                Debug.Log($"[HeadAbility] Enemigo eliminado: {enemy.name}");
                Destroy(enemy.gameObject);
                enemiesKilled++;
                continue;
            }

            // Verificar si es un obstáculo destruible
            // Solo destruir obstáculos si tiene Legs equipadas (sinergia Head + Legs)
            if (col.CompareTag("Obstacle"))
            {
                if (!requiresLegs || hasLegs)
                {
                    Debug.Log($"[HeadAbility] Obstáculo destruido: {col.name}");
                    Destroy(col.gameObject);
                    obstaclesDestroyed++;
                }
                else
                {
                    Debug.Log($"[HeadAbility] Obstáculo detectado pero no destruido (requiere Legs): {col.name}");
                }
            }
        }

        // Log de resultados
        if (enemiesKilled > 0 || obstaclesDestroyed > 0)
        {
            string synergyIndicator = (hasLegs && obstaclesDestroyed > 0) ? " [SINERGIA HEAD+LEGS]" : "";
            Debug.Log($"[HeadAbility] Stomp Impact → Enemigos: {enemiesKilled} | Obstáculos: {obstaclesDestroyed}{synergyIndicator}");
        }
        else
        {
            Debug.Log($"[HeadAbility] Stomp sin impactos en el área");
        }

        // TODO: Agregar feedback visual (partículas, shake de cámara, etc.)

        EndStomp();
    }

    private void EndStomp()
    {
        isStomping = false;
        stompActivated = false;

        Debug.Log("[HeadAbility] Stomp finalizado");
    }

    private bool HasRequiredPart(BodyPartType partType)
    {
        if (collectionManager == null)
        {
            Debug.LogWarning("[HeadAbility] No se encontró BodyPartCollectionManager");
            return false;
        }

        return collectionManager.HasPart(partType);
    }

    public bool IsStomping => isStomping;

    protected override float GetCooldownDuration()
    {
        return partData.cooldownDuration;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !showImpactRadius) return;

        // Dibujar radio de impacto del stomp
        Gizmos.color = isStomping ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, partData != null ? partData.stompRadius : 5f);

        // Si está en stomp, dibujar línea de caída
        if (isStomping)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                new Vector3(transform.position.x, stompStartHeight, transform.position.z),
                transform.position
            );
        }
    }
}