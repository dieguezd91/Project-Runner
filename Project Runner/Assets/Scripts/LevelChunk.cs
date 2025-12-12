using System.Collections.Generic;
using UnityEngine;

public class LevelChunk : MonoBehaviour
{
    public Vector2Int Coordinate { get; private set; }

    private EnemyPoolManager enemyPoolManager;
    private ObstaclePoolManager obstaclePoolManager;
    private Transform playerTransform;

    private List<EnemyBase> spawnedEnemies = new List<EnemyBase>();
    private List<GameObject> spawnedObstacles = new List<GameObject>();

    public void Setup(Vector2Int coord, EnemyPoolManager enemyPool, ObstaclePoolManager obstaclePool, Transform player)
    {
        Coordinate = coord;
        enemyPoolManager = enemyPool;
        obstaclePoolManager = obstaclePool;
        playerTransform = player;
    }

    public void PopulateEnemies(int chunkSize, int count)
    {
        if (enemyPoolManager == null || count <= 0) return;

        SpawnProceduralEnemies(chunkSize, count);
    }

    /// <summary>
    /// Spawna obstáculos proceduralmente en el chunk
    /// </summary>
    public void PopulateObstacles(int chunkSize, int count, float minDistance)
    {
        if (obstaclePoolManager == null || count <= 0) return;

        SpawnProceduralObstacles(chunkSize, count, minDistance);
    }

    private void SpawnProceduralEnemies(int chunkSize, int count)
    {
        if (enemyPoolManager == null) return;

        for (int i = 0; i < count; i++)
        {
            float randomX = Random.Range(-chunkSize / 2f, chunkSize / 2f);
            float randomZ = Random.Range(-chunkSize / 2f, chunkSize / 2f);

            Vector3 spawnPos = transform.position + new Vector3(randomX, 2f, randomZ);

            EnemyBase enemy = enemyPoolManager.GetEnemy(
                spawnPos,
                playerTransform,
                EnemyBase.EnemyState.Sleeping
            );

            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
            }
        }
    }

    private void SpawnProceduralObstacles(int chunkSize, int count, float minDistance)
    {
        if (obstaclePoolManager == null) return;

        List<Vector3> spawnedPositions = new List<Vector3>();

        int attempts = 0;
        int maxAttempts = count * 10; // Evitar loops infinitos

        while (spawnedObstacles.Count < count && attempts < maxAttempts)
        {
            attempts++;

            // Posición aleatoria dentro del chunk
            float randomX = Random.Range(-chunkSize / 2f, chunkSize / 2f);
            float randomZ = Random.Range(-chunkSize / 2f, chunkSize / 2f);

            Vector3 candidatePos = transform.position + new Vector3(randomX, 0f, randomZ);

            // Verificar distancia mínima con obstáculos ya spawneados
            bool tooClose = false;
            foreach (var pos in spawnedPositions)
            {
                if (Vector3.Distance(candidatePos, pos) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            // Spawnear obstáculo
            GameObject obstacle = obstaclePoolManager.GetRandomObstacle(
                candidatePos,
                Quaternion.identity
            );

            if (obstacle != null)
            {
                spawnedObstacles.Add(obstacle);
                spawnedPositions.Add(candidatePos);
            }
        }
    }

    public void Recycle()
    {
        // Reciclar enemigos
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemyPoolManager.ReturnEnemy(enemy);
            }
        }
        spawnedEnemies.Clear();

        // Reciclar obstáculos
        foreach (var obstacle in spawnedObstacles)
        {
            if (obstacle != null && obstacle.activeInHierarchy)
            {
                obstaclePoolManager.ReturnObstacle(obstacle);
            }
        }
        spawnedObstacles.Clear();
    }
}