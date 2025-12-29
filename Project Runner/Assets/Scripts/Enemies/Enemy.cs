using UnityEngine;

public class Enemy : MonoBehaviour
{
    private EnemyConfigSO config;
    private Transform player;
    private bool isChasing;
    private LevelChunk currentChunk;

    // Sistema de attachment
    private bool isAttachedToPlayer = false;
    private PlayerHealth attachedPlayerHealth;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    public void Initialize(EnemyConfigSO enemyConfig)
    {
        config = enemyConfig;
        isAttachedToPlayer = false;

        if (config != null)
        {
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = config.debugColor;
            }

            if (config.randomScale)
            {
                float randomScale = Random.Range(config.minScale, config.maxScale);
                transform.localScale = Vector3.one * randomScale;
            }
        }
    }

    void Update()
    {
        // Si está pegado, no hacer nada (el player lo mueve)
        if (isAttachedToPlayer) return;

        if (player == null || config == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isChasing = distanceToPlayer <= config.chaseRange;

        if (isChasing)
        {
            ChasePlayer();
        }
    }

    void ChasePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * config.moveSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                              config.rotationSpeed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Detectar obstáculos
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Die();
            return;
        }

        // Detectar jugador
        if (collision.gameObject.CompareTag("Player") && !isAttachedToPlayer)
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Hacer daño
                playerHealth.TakeDamage(15f, "Enemy");

                // Pegarse al jugador
                AttachToPlayer(playerHealth);
            }
        }
    }

    private void AttachToPlayer(PlayerHealth playerHealth)
    {
        if (isAttachedToPlayer) return;

        isAttachedToPlayer = true;
        attachedPlayerHealth = playerHealth;

        // Cambiar parent al jugador
        transform.SetParent(player);

        // CRÍTICO: Desactivar física para que no empuje al player
        Rigidbody enemyRb = GetComponent<Rigidbody>();
        if (enemyRb != null)
        {
            enemyRb.isKinematic = true;
            enemyRb.linearVelocity = Vector3.zero;
        }

        // Opcional: desactivar collider completamente
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // Notificar al PlayerHealth
        playerHealth.AttachEnemy(this);

        // Cambiar color visual
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.magenta;
        }

        Debug.Log($"Enemy {config?.enemyName ?? name} pegado al jugador!");
    }

    public void DetachFromPlayer()
    {
        if (!isAttachedToPlayer) return;

        isAttachedToPlayer = false;
        transform.SetParent(null);

        // Reactivar física
        Rigidbody enemyRb = GetComponent<Rigidbody>();
        if (enemyRb != null)
        {
            enemyRb.isKinematic = false;
        }

        // Reactivar collider
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (attachedPlayerHealth != null)
        {
            attachedPlayerHealth.DetachEnemy(this);
            attachedPlayerHealth = null;
        }

        // Restaurar color
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null && config != null)
        {
            renderer.material.color = config.debugColor;
        }

        Debug.Log($"Enemy {config?.enemyName ?? name} despegado del jugador!");
    }

    void Die()
    {
        Debug.Log($"Enemy {config?.enemyName ?? name} murió");

        // IMPORTANTE: Despegarse si estaba attached
        if (isAttachedToPlayer)
        {
            DetachFromPlayer();
        }

        if (currentChunk != null)
        {
            currentChunk.UnregisterEnemy(this);
        }

        gameObject.SetActive(false);
    }

    public void SetCurrentChunk(LevelChunk chunk)
    {
        currentChunk = chunk;

        if (currentChunk != null)
        {
            currentChunk.RegisterEnemy(this);
        }
    }

    public bool IsAttachedToPlayer => isAttachedToPlayer;

    void OnDrawGizmosSelected()
    {
        if (config == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.chaseRange);
    }
}