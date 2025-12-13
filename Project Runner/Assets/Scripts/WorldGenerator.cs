using UnityEngine;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private EnemyPoolManager enemyPoolManager;
    [SerializeField] private ObstaclePoolManager obstaclePoolManager;
    [SerializeField] private DecorationPoolManager decorationPoolManager;

    [Header("World Settings")]
    [SerializeField] private float chunkSize = 50f;
    [SerializeField] private int viewDistance = 3;

    [Header("Terrain")]
    [SerializeField] private TerrainConfigSO terrainConfig;
    [SerializeField] private bool useProceduralTerrain = true;

    [Header("Difficulty System")]
    [Tooltip("Usar sistema de dificultad progresiva (recomendado)")]
    [SerializeField] private bool useDifficultyProgression = true;
    [Tooltip("Referencia al DifficultyManager (dejar en null para buscar automáticamente)")]
    [SerializeField] private DifficultyManager difficultyManager;

    [Header("Manual Spawning (Solo si useDifficultyProgression = false)")]
    [SerializeField] private bool spawnEnemiesOnGeneration = false;
    [SerializeField][Range(0, 10)] private int enemiesPerChunk = 3;
    [SerializeField] private float startSpawnDelay = 5f;
    [SerializeField] private bool spawnObstaclesOnGeneration = true;
    [SerializeField][Range(0, 5)] private int obstaclesPerChunk = 1;
    [SerializeField][Range(3f, 15f)] private float minDistanceBetweenObstacles = 5f;

    [Header("Decoration Spawning (SIN COLISIÓN - Visual)")]
    [SerializeField] private bool spawnDecorationsOnGeneration = true;
    [SerializeField][Range(5, 50)] private int decorationsPerChunk = 20;
    [SerializeField][Range(1f, 5f)] private float minDistanceBetweenDecorations = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private Dictionary<Vector2Int, LevelChunk> activeChunks = new Dictionary<Vector2Int, LevelChunk>();
    private Vector2Int currentPlayerChunk;
    private bool hasStarted = false;

    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform not assigned!");
            return;
        }

        if (useDifficultyProgression && difficultyManager == null)
        {
            difficultyManager = FindObjectOfType<DifficultyManager>();
            if (difficultyManager == null)
            {
                Debug.LogWarning("DifficultyManager not found! Falling back to manual spawning.");
                useDifficultyProgression = false;
            }
        }

        currentPlayerChunk = GetChunkCoordinate(playerTransform.position);
        GenerateInitialChunks();

        if (!useDifficultyProgression && spawnEnemiesOnGeneration)
        {
            Invoke(nameof(EnableEnemySpawning), startSpawnDelay);
        }

        hasStarted = true;
    }

    private void Update()
    {
        if (!hasStarted || playerTransform == null) return;

        Vector2Int playerChunk = GetChunkCoordinate(playerTransform.position);

        if (playerChunk != currentPlayerChunk)
        {
            currentPlayerChunk = playerChunk;
            UpdateChunks();
        }
    }

    private void GenerateInitialChunks()
    {
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int coordinate = currentPlayerChunk + new Vector2Int(x, z);
                SpawnChunk(coordinate);
            }
        }

        if (showDebug)
        {
            Debug.Log($"Generated {activeChunks.Count} initial chunks around player");
        }
    }

    private void UpdateChunks()
    {
        HashSet<Vector2Int> chunksToKeep = new HashSet<Vector2Int>();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int coordinate = currentPlayerChunk + new Vector2Int(x, z);
                chunksToKeep.Add(coordinate);

                if (!activeChunks.ContainsKey(coordinate))
                {
                    SpawnChunk(coordinate);
                }
            }
        }

        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var kvp in activeChunks)
        {
            if (!chunksToKeep.Contains(kvp.Key))
            {
                chunksToRemove.Add(kvp.Key);
            }
        }

        foreach (var coordinate in chunksToRemove)
        {
            RecycleChunk(coordinate);
        }

        if (showDebug && chunksToRemove.Count > 0)
        {
            Debug.Log($"Updated chunks: spawned new, recycled {chunksToRemove.Count}");
        }
    }

    private void SpawnChunk(Vector2Int coordinate)
    {
        Vector3 worldPosition = new Vector3(
            coordinate.x * chunkSize,
            -1f,
            coordinate.y * chunkSize
        );

        GameObject chunkObj = Instantiate(chunkPrefab, worldPosition, Quaternion.identity, transform);
        LevelChunk chunk = chunkObj.GetComponent<LevelChunk>();

        if (chunk == null)
        {
            Debug.LogError("Chunk prefab missing LevelChunk component!");
            Destroy(chunkObj);
            return;
        }

        if (useProceduralTerrain && terrainConfig != null)
        {
            chunk.SetupWithTerrain(coordinate, enemyPoolManager, obstaclePoolManager,
                                   decorationPoolManager, playerTransform, terrainConfig, chunkSize);
        }
        else
        {
            chunk.Setup(coordinate, enemyPoolManager, obstaclePoolManager,
                       decorationPoolManager, playerTransform);
        }

        bool shouldSpawnEnemies = false;
        int enemyCount = 0;
        bool shouldSpawnObstacles = false;
        int obstacleCount = 0;

        if (useDifficultyProgression && difficultyManager != null)
        {
            shouldSpawnEnemies = difficultyManager.ShouldSpawnEnemies();
            enemyCount = difficultyManager.GetEnemyCount();
            shouldSpawnObstacles = difficultyManager.ShouldSpawnObstacles();
            obstacleCount = difficultyManager.GetObstacleCount();
        }
        else
        {
            shouldSpawnEnemies = spawnEnemiesOnGeneration;
            enemyCount = enemiesPerChunk;
            shouldSpawnObstacles = spawnObstaclesOnGeneration;
            obstacleCount = obstaclesPerChunk;
        }

        if (shouldSpawnEnemies && enemyPoolManager != null)
        {
            chunk.PopulateEnemies(chunkSize, enemyCount);
        }

        List<Vector3> obstaclePositions = new List<Vector3>();

        if (shouldSpawnObstacles && obstaclePoolManager != null)
        {
            chunk.PopulateObstacles(chunkSize, obstacleCount, minDistanceBetweenObstacles);

            foreach (Transform child in chunk.transform)
            {
                if (child.gameObject.layer == 7)
                {
                    obstaclePositions.Add(child.position);
                }
            }
        }

        if (spawnDecorationsOnGeneration && decorationPoolManager != null)
        {
            chunk.PopulateDecorations(chunkSize, decorationsPerChunk,
                                      minDistanceBetweenDecorations, obstaclePositions);
        }

        activeChunks[coordinate] = chunk;

        if (showDebug)
        {
            string spawnInfo = useDifficultyProgression
                ? $"Difficulty Mode - Obstacles: {(shouldSpawnObstacles ? obstacleCount.ToString() : "0")}, Enemies: {(shouldSpawnEnemies ? enemyCount.ToString() : "0")}"
                : $"Manual Mode - Obstacles: {(shouldSpawnObstacles ? obstacleCount.ToString() : "0")}, Enemies: {(shouldSpawnEnemies ? enemyCount.ToString() : "0")}";

            Debug.Log($"Spawned chunk {coordinate}: {spawnInfo}");
        }
    }

    private void RecycleChunk(Vector2Int coordinate)
    {
        if (!activeChunks.ContainsKey(coordinate)) return;

        LevelChunk chunk = activeChunks[coordinate];
        chunk.Recycle();
        Destroy(chunk.gameObject);
        activeChunks.Remove(coordinate);
    }

    private Vector2Int GetChunkCoordinate(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int z = Mathf.FloorToInt(worldPosition.z / chunkSize);
        return new Vector2Int(x, z);
    }

    private void EnableEnemySpawning()
    {
        spawnEnemiesOnGeneration = true;

        if (showDebug)
        {
            Debug.Log("Enemy spawning enabled after delay");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || !Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        Vector3 playerChunkCenter = new Vector3(
            currentPlayerChunk.x * chunkSize,
            0,
            currentPlayerChunk.y * chunkSize
        );
        Gizmos.DrawWireCube(playerChunkCenter, new Vector3(chunkSize, 2f, chunkSize));

        Gizmos.color = Color.cyan;
        foreach (var kvp in activeChunks)
        {
            Vector3 center = new Vector3(
                kvp.Key.x * chunkSize,
                0,
                kvp.Key.y * chunkSize
            );
            Gizmos.DrawWireCube(center, new Vector3(chunkSize, 1f, chunkSize));
        }
    }
}