using System.Collections.Generic;
using UnityEngine;

public class LevelChunk : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebug;

    // Referencias
    private Vector2Int chunkCoordinate;
    private EnemyPoolManager enemyPoolManager;
    private ObstaclePoolManager obstaclePoolManager;
    private Transform playerTransform;

    // Estado interno
    private List<EnemyBase> spawnedEnemies = new List<EnemyBase>();
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private List<GameObject> terrainInstances = new List<GameObject>();

    // Componentes originales del prefab (se ocultan al usar terrenos procedurales)
    private MeshRenderer originalMeshRenderer;
    private MeshCollider originalMeshCollider;

    public Vector2Int Coordinate => chunkCoordinate;

    private void Awake()
    {
        originalMeshRenderer = GetComponent<MeshRenderer>();
        originalMeshCollider = GetComponent<MeshCollider>();
    }

    public void Setup(Vector2Int coordinate, EnemyPoolManager enemyPool, ObstaclePoolManager obstaclePool, Transform player)
    {
        chunkCoordinate = coordinate;
        enemyPoolManager = enemyPool;
        obstaclePoolManager = obstaclePool;
        playerTransform = player;

        if (showDebug)
        {
            Debug.Log($"Chunk {coordinate} Setup complete");
        }
    }

    public void SetupWithTerrain(Vector2Int coordinate, EnemyPoolManager enemyPool, ObstaclePoolManager obstaclePool,
                                 Transform player, TerrainConfigSO terrainConfig, float chunkSize)
    {
        Setup(coordinate, enemyPool, obstaclePool, player);
        GenerateTerrainVariants(terrainConfig, chunkSize);
    }

    private void GenerateTerrainVariants(TerrainConfigSO terrainConfig, float chunkSize)
    {
        if (terrainConfig == null)
        {
            Debug.LogWarning("No terrain config provided, using default chunk mesh");
            return;
        }

        HideOriginalMesh();
        ClearPreviousTerrainInstances();

        if (terrainConfig.oneTerrainPerChunk)
        {
            CreateSingleTerrainVariant(terrainConfig, chunkSize);
        }
        else
        {
            CreateSubdividedTerrain(terrainConfig, chunkSize);
        }
    }

    private void HideOriginalMesh()
    {
        if (originalMeshRenderer != null) originalMeshRenderer.enabled = false;
        if (originalMeshCollider != null) originalMeshCollider.enabled = false;
    }

    private void ShowOriginalMesh()
    {
        if (originalMeshRenderer != null) originalMeshRenderer.enabled = true;
        if (originalMeshCollider != null) originalMeshCollider.enabled = true;
    }

    private void ClearPreviousTerrainInstances()
    {
        foreach (var instance in terrainInstances)
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }
        terrainInstances.Clear();
    }

    private void CreateSingleTerrainVariant(TerrainConfigSO terrainConfig, float chunkSize)
    {
        GameObject terrainPrefab = terrainConfig.GetRandomTerrainPrefab();
        if (terrainPrefab == null) return;

        GameObject terrainInstance = Instantiate(terrainPrefab, transform);
        terrainInstance.transform.localPosition = Vector3.zero;

        ScaleTerrainToChunkSize(terrainInstance, chunkSize);

        terrainInstances.Add(terrainInstance);
    }

    private void CreateSubdividedTerrain(TerrainConfigSO terrainConfig, float chunkSize)
    {
        int subdivisions = terrainConfig.subdivisionsPerChunk;
        float subChunkSize = chunkSize / subdivisions;

        for (int x = 0; x < subdivisions; x++)
        {
            for (int z = 0; z < subdivisions; z++)
            {
                GameObject terrainPrefab = terrainConfig.GetRandomTerrainPrefab();
                if (terrainPrefab == null) continue;

                GameObject terrainInstance = Instantiate(terrainPrefab, transform);

                Vector3 localPosition = new Vector3(
                    (x - subdivisions / 2f + 0.5f) * subChunkSize,
                    0f,
                    (z - subdivisions / 2f + 0.5f) * subChunkSize
                );

                terrainInstance.transform.localPosition = localPosition;

                ScaleTerrainToChunkSize(terrainInstance, subChunkSize);

                terrainInstances.Add(terrainInstance);
            }
        }
    }

    private void ScaleTerrainToChunkSize(GameObject terrainInstance, float targetSize)
    {
        MeshFilter meshFilter = terrainInstance.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"Terrain prefab {terrainInstance.name} missing MeshFilter/Mesh");
            return;
        }

        Bounds meshBounds = meshFilter.sharedMesh.bounds;
        float meshSizeX = meshBounds.size.x;
        float meshSizeZ = meshBounds.size.z;

        float scaleFactorX = targetSize / meshSizeX;
        float scaleFactorZ = targetSize / meshSizeZ;

        terrainInstance.transform.localScale = new Vector3(scaleFactorX, 1f, scaleFactorZ);
    }

    public void PopulateEnemies(float chunkSize, int count)
    {
        if (enemyPoolManager == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos = GetRandomPositionInChunk(chunkSize);

            EnemyBase enemy = enemyPoolManager.GetEnemy(
                randomPos,
                playerTransform,
                EnemyBase.EnemyState.Sleeping
            );

            spawnedEnemies.Add(enemy);
        }

        if (showDebug)
        {
            Debug.Log($"Chunk {chunkCoordinate}: Spawned {count} enemies");
        }
    }

    public void PopulateObstacles(float chunkSize, int count, float minDistance)
    {
        if (obstaclePoolManager == null || count <= 0) return;

        List<Vector3> spawnedPositions = new List<Vector3>();
        int attempts = 0;
        int maxAttempts = count * 10;

        while (spawnedObstacles.Count < count && attempts < maxAttempts)
        {
            attempts++;
            Vector3 randomPos = GetRandomPositionInChunk(chunkSize);

            if (IsPositionValid(randomPos, spawnedPositions, minDistance))
            {
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                GameObject obstacle = obstaclePoolManager.GetRandomObstacle(randomPos, randomRotation);
                if (obstacle != null)
                {
                    spawnedObstacles.Add(obstacle);
                    spawnedPositions.Add(randomPos);
                }
            }
        }

        if (showDebug)
        {
            Debug.Log($"Chunk {chunkCoordinate}: Spawned {spawnedObstacles.Count}/{count} obstacles in {attempts} attempts");
        }
    }

    private Vector3 GetRandomPositionInChunk(float chunkSize)
    {
        float halfSize = chunkSize / 2f;

        float randomX = Random.Range(-halfSize, halfSize);
        float randomZ = Random.Range(-halfSize, halfSize);

        return transform.position + new Vector3(randomX, 0, randomZ);
    }

    private bool IsPositionValid(Vector3 position, List<Vector3> existingPositions, float minDistance)
    {
        foreach (var existingPos in existingPositions)
        {
            float distance = Vector3.Distance(
                new Vector3(position.x, 0, position.z),
                new Vector3(existingPos.x, 0, existingPos.z)
            );

            if (distance < minDistance)
            {
                return false;
            }
        }

        return true;
    }

    public void Recycle()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                enemyPoolManager.ReturnEnemy(enemy);
            }
        }
        spawnedEnemies.Clear();

        foreach (var obstacle in spawnedObstacles)
        {
            if (obstacle != null && obstaclePoolManager != null)
            {
                obstaclePoolManager.ReturnObstacle(obstacle);
            }
        }
        spawnedObstacles.Clear();

        ClearPreviousTerrainInstances();
        ShowOriginalMesh();

        if (showDebug)
        {
            Debug.Log($"Chunk {chunkCoordinate} recycled");
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebug && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 50f);
        }
    }
}