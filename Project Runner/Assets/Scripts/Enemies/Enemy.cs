using UnityEngine;

/// <summary>
/// Comportamiento del enemigo: Chase, Muerte.
/// NO maneja attachment (eso lo delega a EnemyAttachmentManager).
/// </summary>
public class Enemy : MonoBehaviour
{
    private EnemyConfigSO config;
    private Transform player;
    private bool isChasing;
    private LevelChunk currentChunk;

    // Sistema de attachment (solo tracking de estado)
    private bool isAttachedToPlayer = false;
    public bool IsAttachedToPlayer => isAttachedToPlayer;

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
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Die();
            return;
        }

        if (collision.gameObject.CompareTag("Player") && !isAttachedToPlayer)
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(15f, "Enemy");
            }

            // Delegar el attachment al manager
            EnemyAttachmentManager attachmentManager = collision.gameObject.GetComponent<EnemyAttachmentManager>();
            if (attachmentManager != null)
            {
                if (attachmentManager.TryAttachEnemy(this))
                {
                    OnAttached();
                }
            }
        }
    }

    /// <summary>
    /// Llamado cuando el enemigo se pega exitosamente.
    /// </summary>
    public void OnAttached()
    {
        isAttachedToPlayer = true;

        // Desregistrarse del chunk
        if (currentChunk != null)
        {
            currentChunk.UnregisterEnemy(this);
            currentChunk = null;
        }
    }

    /// <summary>
    /// Llamado cuando el enemigo se despega.
    /// </summary>
    public void OnDetached()
    {
        isAttachedToPlayer = false;

        // Restaurar color
        if (config != null)
        {
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = config.debugColor;
            }
        }
    }

    void Die()
    {
        Debug.Log($"Enemy {config?.enemyName ?? name} muri�");

        if (currentChunk != null)
        {
            currentChunk.UnregisterEnemy(this);
            currentChunk = null;
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

    void OnDrawGizmosSelected()
    {
        if (config == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.chaseRange);
    }
}