using System.Collections.Generic;
using UnityEngine;

public class LevelChunk : MonoBehaviour
{
    public Vector2Int Coordinate { get; private set; }

    private EnemyPoolManager poolManager;
    private Transform playerTransform;

    private List<EnemyBase> spawnedEnemies = new List<EnemyBase>();

    public void Setup(Vector2Int coord, EnemyPoolManager pool, Transform player)
    {
        Coordinate = coord;
        poolManager = pool;
        playerTransform = player;
    }

    public void PopulateEnemies(int chunkSize, int count)
    {
        if (poolManager == null || count <= 0) return;

        SpawnProceduralEnemies(chunkSize, count);
    }

    private void SpawnProceduralEnemies(int chunkSize, int count)
    {
        if (poolManager == null) return;

        for (int i = 0; i < count; i++)
        {
            float randomX = Random.Range(-chunkSize / 2f, chunkSize / 2f);
            float randomZ = Random.Range(-chunkSize / 2f, chunkSize / 2f);

            Vector3 spawnPos = transform.position + new Vector3(randomX, 2f, randomZ);

            EnemyBase enemy = poolManager.GetEnemy(
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

    public void Recycle()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                poolManager.ReturnEnemy(enemy);
            }
        }
        spawnedEnemies.Clear();
    }
}