using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class ObstaclePoolManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Lista de tipos de obstáculos disponibles")]
    [SerializeField] private ObstacleData[] obstacleDatabase;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSizePerType = 10;
    [SerializeField] private int maxPoolSizePerType = 50;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Pools por cada tipo de obstáculo
    private Dictionary<ObstacleData, ObjectPool<GameObject>> obstaclePools =
        new Dictionary<ObstacleData, ObjectPool<GameObject>>();

    private int activeObstaclesCount = 0;

    public int ActiveObstaclesCount => activeObstaclesCount;

    private void Awake()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var obstacleData in obstacleDatabase)
        {
            if (obstacleData == null || obstacleData.prefab == null)
            {
                Debug.LogWarning($"ObstacleData nulo o sin prefab asignado");
                continue;
            }

            var pool = new ObjectPool<GameObject>(
                createFunc: () => CreateObstacle(obstacleData),
                actionOnGet: OnGetObstacle,
                actionOnRelease: OnReleaseObstacle,
                actionOnDestroy: OnDestroyObstacle,
                collectionCheck: true,
                defaultCapacity: initialPoolSizePerType,
                maxSize: maxPoolSizePerType
            );

            obstaclePools.Add(obstacleData, pool);

            // Prewarm pool
            GameObject[] prewarmed = new GameObject[initialPoolSizePerType];
            for (int i = 0; i < initialPoolSizePerType; i++)
            {
                prewarmed[i] = pool.Get();
            }
            for (int i = 0; i < initialPoolSizePerType; i++)
            {
                pool.Release(prewarmed[i]);
            }
        }

        if (showDebug)
        {
            Debug.Log($"Obstacle Pools initialized: {obstaclePools.Count} types");
        }
    }

    private GameObject CreateObstacle(ObstacleData data)
    {
        GameObject obstacle = Instantiate(data.prefab, transform);
        obstacle.SetActive(false);

        // Agregar componente para tracking
        var tracker = obstacle.AddComponent<PooledObstacle>();
        tracker.obstacleData = data;

        return obstacle;
    }

    private void OnGetObstacle(GameObject obstacle)
    {
        obstacle.SetActive(true);
        activeObstaclesCount++;
    }

    private void OnReleaseObstacle(GameObject obstacle)
    {
        obstacle.SetActive(false);
        activeObstaclesCount--;
    }

    private void OnDestroyObstacle(GameObject obstacle)
    {
        if (obstacle != null)
        {
            Destroy(obstacle);
        }
    }

    /// <summary>
    /// Obtiene un obstáculo aleatorio basado en pesos de probabilidad
    /// </summary>
    public GameObject GetRandomObstacle(Vector3 position, Quaternion rotation)
    {
        ObstacleData selected = SelectWeightedRandom();
        if (selected == null) return null;

        return GetObstacle(selected, position, rotation);
    }

    /// <summary>
    /// Obtiene un obstáculo específico del pool
    /// </summary>
    public GameObject GetObstacle(ObstacleData data, Vector3 position, Quaternion rotation)
    {
        if (!obstaclePools.ContainsKey(data))
        {
            Debug.LogWarning($"No pool found for {data.name}");
            return null;
        }

        GameObject obstacle = obstaclePools[data].Get();

        // Aplicar posición y rotación
        obstacle.transform.position = position + Vector3.up * data.heightOffset;

        // Rotación aleatoria si está permitido
        if (data.randomRotation)
        {
            float randomY = Random.Range(0f, 360f);
            obstacle.transform.rotation = Quaternion.Euler(0, randomY, 0);
        }
        else
        {
            obstacle.transform.rotation = rotation;
        }

        // Escala aleatoria si está permitido
        if (data.randomScale)
        {
            float randomScale = Random.Range(data.minScale, data.maxScale);
            obstacle.transform.localScale = Vector3.one * randomScale;
        }

        return obstacle;
    }

    /// <summary>
    /// Devuelve un obstáculo al pool
    /// </summary>
    public void ReturnObstacle(GameObject obstacle)
    {
        if (obstacle == null) return;

        var tracker = obstacle.GetComponent<PooledObstacle>();
        if (tracker == null || tracker.obstacleData == null)
        {
            Debug.LogWarning("Obstacle sin PooledObstacle component");
            Destroy(obstacle);
            return;
        }

        if (obstaclePools.ContainsKey(tracker.obstacleData))
        {
            obstaclePools[tracker.obstacleData].Release(obstacle);
        }
    }

    /// <summary>
    /// Selección aleatoria ponderada por peso
    /// </summary>
    private ObstacleData SelectWeightedRandom()
    {
        if (obstacleDatabase.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var data in obstacleDatabase)
        {
            if (data != null)
                totalWeight += data.spawnWeight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var data in obstacleDatabase)
        {
            if (data == null) continue;

            cumulativeWeight += data.spawnWeight;
            if (randomValue <= cumulativeWeight)
            {
                return data;
            }
        }

        return obstacleDatabase[0]; // Fallback
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        float x = 10f;
        float y = 600f;
        float w = 300f;
        float h = 25f;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;

        GUI.Box(new Rect(x, y, w, 80f), "");

        y += 10f;

        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(x + 10, y, w - 20, h), "OBSTACLE POOL", style);
        y += h;

        style.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Active: {activeObstaclesCount}", style);
        y += h;

        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Types: {obstaclePools.Count}", style);
    }
}

/// <summary>
/// Componente helper para trackear qué ObstacleData usó este objeto
/// </summary>
public class PooledObstacle : MonoBehaviour
{
    [HideInInspector]
    public ObstacleData obstacleData;
}