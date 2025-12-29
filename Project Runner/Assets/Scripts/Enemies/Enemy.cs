using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;

    private Transform player;
    private bool isChasing;

    void Start()
    {
        // Buscar al jugador en la escena
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Color visual para debug
        GetComponentInChildren<Renderer>().material.color = data.debugColor;
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
        // Dirección hacia el jugador
        Vector3 direction = (player.position - transform.position).normalized;

        // Movimiento
        transform.position += direction * data.moveSpeed * Time.deltaTime;

        // Rotación suave hacia el jugador
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, data.rotationSpeed * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        // Visualizar rango de persecución en el editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.chaseRange);
    }
}