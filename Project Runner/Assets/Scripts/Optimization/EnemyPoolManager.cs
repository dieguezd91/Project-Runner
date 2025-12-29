using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EnemyPoolManager : MonoBehaviour
{
    [Header("Enemy Configurations")]
    [SerializeField] private List<EnemyConfigSO> enemyConfigs = new List<EnemyConfigSO>();

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 20;

    private Queue<GameObject> enemyPool = new Queue<GameObject>();
    private List<GameObject> activeEnemies = new List<GameObject>();
    private int totalSpawnWeight;

    void Awake()
    {
        CalculateTotalWeight();
        InitializePool();
    }

    private void CalculateTotalWeight()
    {
        totalSpawnWeight = 0;
        foreach (var config in enemyConfigs)
        {
            if (config != null && config.prefab != null)
            {
                totalSpawnWeight += config.spawnWeight;
            }
        }
    }

    private void InitializePool()
    {
        if (enemyConfigs.Count == 0)
        {
            Debug.LogWarning("EnemyPoolManager: No enemy configs assigned!");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject enemy = CreateNewEnemy();
            enemy.SetActive(false);
            enemyPool.Enqueue(enemy);
        }
    }

    private GameObject CreateNewEnemy()
    {
        EnemyConfigSO config = GetRandomEnemyConfig();
        if (config == null || config.prefab == null) return null;

        GameObject enemy = Instantiate(config.prefab, transform);

        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            enemyComponent.Initialize(config);
        }

        return enemy;
    }

    private EnemyConfigSO GetRandomEnemyConfig()
    {
        if (enemyConfigs.Count == 0) return null;
        if (totalSpawnWeight <= 0) return enemyConfigs[0];

        int randomValue = Random.Range(0, totalSpawnWeight);
        int currentWeight = 0;

        foreach (var config in enemyConfigs)
        {
            if (config != null && config.prefab != null)
            {
                currentWeight += config.spawnWeight;
                if (randomValue < currentWeight)
                {
                    return config;
                }
            }
        }

        return enemyConfigs[0];
    }

    public GameObject GetEnemy(Vector3 position, Quaternion rotation)
    {
        GameObject enemy;

        if (enemyPool.Count > 0)
        {
            enemy = enemyPool.Dequeue();
        }
        else
        {
            enemy = CreateNewEnemy();
        }

        if (enemy == null) return null;

        enemy.transform.position = position;
        enemy.transform.rotation = rotation;
        enemy.SetActive(true);
        activeEnemies.Add(enemy);

        return enemy;
    }

    public void ReturnEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        enemy.SetActive(false);
        enemy.transform.SetParent(transform);
        activeEnemies.Remove(enemy);
        enemyPool.Enqueue(enemy);
    }

    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }
}