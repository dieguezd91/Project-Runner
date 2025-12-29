using UnityEngine;

public class Enemy : MonoBehaviour
{
    private EnemyConfigSO config;
    private Transform player;
    private bool isChasing;
    private LevelChunk currentChunk;

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
        }
    }

    void Die()
    {
        Debug.Log($"Enemy {config?.enemyName ?? name} murió");

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

    void OnDrawGizmosSelected()
    {
        if (config == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.chaseRange);
    }
}