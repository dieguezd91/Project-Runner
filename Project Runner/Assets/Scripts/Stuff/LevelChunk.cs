using System.Collections.Generic;
using UnityEngine;

public class LevelChunk : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebug;

    private Vector2Int chunkCoordinate;
    private ObstaclePoolManager obstaclePoolManager;
    private DecorationPoolManager decorationPoolManager;

    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private List<GameObject> spawnedDecorations = new List<GameObject>();
    private List<GameObject> terrainInstances = new List<GameObject>();

    private MeshRenderer originalMeshRenderer;
    private MeshCollider originalMeshCollider;

    public Vector2Int Coordinate => chunkCoordinate;

    private void Awake()
    {
        originalMeshRenderer = GetComponent<MeshRenderer>();
        originalMeshCollider = GetComponent<MeshCollider>();
    }

    public void Setup(Vector2Int coordinate, ObstaclePoolManager obstaclePool, DecorationPoolManager decorationPool)
    {
        chunkCoordinate = coordinate;
        obstaclePoolManager = obstaclePool;
        decorationPoolManager = decorationPool;

        if (showDebug)
        {
            Debug.Log($"Chunk {coordinate} Setup complete");
        }
    }

    public void SetupWithTerrain(Vector2Int coordinate, ObstaclePoolManager obstaclePool,
                                 DecorationPoolManager decorationPool, TerrainConfigSO terrainConfig, float chunkSize)
    {
        Setup(coordinate, obstaclePool, decorationPool);
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
            CreateMultipleTerrainVariants(terrainConfig, chunkSize);
        }
    }

    private void CreateSingleTerrainVariant(TerrainConfigSO terrainConfig, float chunkSize)
    {
        GameObject terrainPrefab = terrainConfig.GetRandomTerrainPrefab();
        if (terrainPrefab == null) return;

        GameObject terrainInstance = Instantiate(terrainPrefab, transform.position, Quaternion.identity, transform);
        ScaleTerrainToChunkSize(terrainInstance, chunkSize);
        terrainInstances.Add(terrainInstance);
    }

    private void CreateMultipleTerrainVariants(TerrainConfigSO terrainConfig, float chunkSize)
    {
        int columns = terrainConfig.subdivisionsPerChunk;
        float subChunkSize = chunkSize / columns;
        float startOffset = -(chunkSize / 2f) + (subChunkSize / 2f);

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < columns; z++)
            {
                Vector3 position = transform.position + new Vector3(
                    startOffset + (x * subChunkSize),
                    0,
                    startOffset + (z * subChunkSize)
                );

                GameObject terrainPrefab = terrainConfig.GetRandomTerrainPrefab();
                if (terrainPrefab == null) continue;

                GameObject terrainInstance = Instantiate(terrainPrefab, position, Quaternion.identity, transform);
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
        foreach (var terrain in terrainInstances)
        {
            if (terrain != null)
            {
                Destroy(terrain);
            }
        }
        terrainInstances.Clear();
    }

    public void PopulateObstacles(float chunkSize, int count, float minDistance)
    {
        if (obstaclePoolManager == null || count <= 0) return;

        List<Vector3> spawnedPositions = new List<Vector3>();
        int attempts = 0;
        int maxAttempts = count * 20;

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
                    obstacle.transform.SetParent(this.transform);
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

    public void PopulateDecorations(float chunkSize, int count, float minDistance, List<Vector3> obstaclesToAvoid)
    {
        if (decorationPoolManager == null || count <= 0) return;

        List<Vector3> allExistingPositions = new List<Vector3>(obstaclesToAvoid);
        int attempts = 0;
        int maxAttempts = count * 15;

        while (spawnedDecorations.Count < count && attempts < maxAttempts)
        {
            attempts++;
            Vector3 randomPos = GetRandomPositionInChunk(chunkSize);

            if (IsPositionValid(randomPos, allExistingPositions, minDistance))
            {
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                GameObject decoration = decorationPoolManager.GetRandomDecoration(randomPos, randomRotation);
                if (decoration != null)
                {
                    decoration.transform.SetParent(this.transform);
                    spawnedDecorations.Add(decoration);
                    allExistingPositions.Add(randomPos);
                }
            }
        }

        if (showDebug)
        {
            Debug.Log($"Chunk {chunkCoordinate}: Spawned {spawnedDecorations.Count}/{count} decorations in {attempts} attempts");
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
        foreach (var obstacle in spawnedObstacles)
        {
            if (obstacle != null && obstaclePoolManager != null)
            {
                obstaclePoolManager.ReturnObstacle(obstacle);
            }
        }
        spawnedObstacles.Clear();

        foreach (var decoration in spawnedDecorations)
        {
            if (decoration != null && decorationPoolManager != null)
            {
                decorationPoolManager.ReturnDecoration(decoration);
            }
        }
        spawnedDecorations.Clear();

        ClearPreviousTerrainInstances();
        ShowOriginalMesh();

        if (showDebug)
        {
            Debug.Log($"Chunk {chunkCoordinate} recycled");
        }
    }

    public List<Vector3> GetObstaclePositions()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var obs in spawnedObstacles)
        {
            if (obs != null) positions.Add(obs.transform.position);
        }
        return positions;
    }
}