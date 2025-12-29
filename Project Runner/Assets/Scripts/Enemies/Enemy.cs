using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;

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

        GetComponentInChildren<Renderer>().material.color = data.debugColor;

        RegisterToNearestChunk();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isChasing = distanceToPlayer <= data.chaseRange;

        if (isChasing)
        {
            ChasePlayer();
        }
    }

    void ChasePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * data.moveSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, data.rotationSpeed * Time.deltaTime);
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
        Debug.Log($"Enemy {name} murió");

        UnregisterFromChunk();

        Destroy(gameObject);
    }

    private void RegisterToNearestChunk()
    {
        LevelChunk[] chunks = FindObjectsOfType<LevelChunk>();
        float minDistance = float.MaxValue;
        LevelChunk nearestChunk = null;

        foreach (var chunk in chunks)
        {
            float distance = Vector3.Distance(transform.position, chunk.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestChunk = chunk;
            }
        }

        if (nearestChunk != null)
        {
            currentChunk = nearestChunk;
            currentChunk.RegisterEnemy(this);
        }
    }

    private void UnregisterFromChunk()
    {
        if (currentChunk != null)
        {
            currentChunk.UnregisterEnemy(this);
            currentChunk = null;
        }
    }

    void OnDestroy()
    {
        UnregisterFromChunk();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.chaseRange);
    }
}