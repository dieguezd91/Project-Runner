using UnityEngine;

public class ArmsAbility : BodyPartAbility
{
    [Header("Stomp Settings")]
    private bool isStomping;
    private bool stompActivated;
    private float stompStartHeight;

    private InputReader inputReader;

    [Header("Ground Impact")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool showImpactRadius = true;

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
            Debug.LogError("[ArmsAbility] No se pudo obtener InputReader");
            return;
        }

        // El stomp se activa con el mismo botón que el stomp del sistema de vida
        // Podemos usar una tecla diferente, por ahora usaremos Ctrl
        inputReader.OnStompPerformed += TryStomp;

        Debug.Log("[ArmsAbility] Stomp habilitado - Presiona Ctrl en el aire para usarlo");
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
        Debug.Log("[ArmsAbility] TryStomp llamado");

        if (!CanUseAbility())
        {
            if (showDebugGUI && isOnCooldown)
            {
                Debug.Log($"[ArmsAbility] Stomp en cooldown: {GetRemainingCooldown():F1}s");
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
                Debug.Log("[ArmsAbility] Stomp requiere estar en el aire");
            }
            return false;
        }

        // No permitir stomp si ya estamos cayendo muy rápido
        if (rb.linearVelocity.y < -20f)
        {
            if (showDebugGUI)
            {
                Debug.Log("[ArmsAbility] Ya estás cayendo demasiado rápido");
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

        Debug.Log($"[ArmsAbility] Stomp activado desde altura: {stompStartHeight:F2}");
    }

    private void ExecuteGroundImpact()
    {
        float fallDistance = stompStartHeight - transform.position.y;

        Debug.Log($"[ArmsAbility] Impacto en el suelo - Distancia caída: {fallDistance:F2}m");

        // Detectar enemigos en radio
        Collider[] hitEnemies = Physics.OverlapSphere(
            transform.position,
            partData.stompRadius,
            enemyLayer
        );

        int enemiesKilled = 0;

        foreach (Collider enemyCollider in hitEnemies)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Matar enemigo (necesitarás implementar un método Kill en Enemy)
                Debug.Log($"[ArmsAbility] Enemigo eliminado: {enemy.name}");

                // Temporal: destruir directamente
                Destroy(enemy.gameObject);
                enemiesKilled++;
            }
        }

        if (enemiesKilled > 0)
        {
            Debug.Log($"[ArmsAbility] Stomp eliminó {enemiesKilled} enemigos");
        }

        // TODO: Agregar feedback visual (partículas, shake de cámara, etc.)

        EndStomp();
    }

    private void EndStomp()
    {
        isStomping = false;
        stompActivated = false;

        Debug.Log("[ArmsAbility] Stomp finalizado");
    }

    public bool IsStomping => isStomping;

    protected override float GetCooldownDuration()
    {
        return partData.cooldownDuration;
    }
}