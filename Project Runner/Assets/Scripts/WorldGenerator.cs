using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class WorldGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelChunk chunkPrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private EnemyPoolManager enemyPoolManager;

    [Header("Settings")]
    [SerializeField] private int chunkSize = 50;
    [SerializeField] private int viewDistance = 2;

    [Header("Procedural Spawning")]
    [SerializeField] private bool spawnEnemiesOnGeneration = true;
    [SerializeField] private int enemiesPerChunk = 2;
    [Tooltip("Tiempo en segundos antes de que empiecen a aparecer enemigos")]
    [SerializeField] private float startSpawnDelay = 5f;

    [Header("Obstacles")]
    [SerializeField] private ObstaclePoolManager obstaclePoolManager;
    [SerializeField] private bool spawnObstaclesOnGeneration = true;
    [SerializeField] private int obstaclesPerChunk = 5;
    [SerializeField] private float minDistanceBetweenObstacles = 4f;

    // State
    private Vector2Int _currentChunkCoord;
    private Dictionary<Vector2Int, LevelChunk> _activeChunks = new Dictionary<Vector2Int, LevelChunk>();
    private ObjectPool<LevelChunk> _chunkPool;

    private float currentSpawnTimer;
    private bool isSpawningEnabled = false;

    public float TimeUntilSpawn => Mathf.Max(0f, currentSpawnTimer);
    public bool IsSpawningEnabled => isSpawningEnabled;

    private void Awake()
    {
        InitializePool();
    }

    private void Start()
    {
        currentSpawnTimer = startSpawnDelay;
        if (currentSpawnTimer <= 0) isSpawningEnabled = true;

        UpdateVisibleChunks(true);
    }

    private void Update()
    {
        HandleSpawnTimer();
        HandleChunkGeneration();
    }

    private void HandleSpawnTimer()
    {
        if (isSpawningEnabled) return;

        currentSpawnTimer -= Time.deltaTime;

        if (currentSpawnTimer <= 0)
        {
            currentSpawnTimer = 0;
            isSpawningEnabled = true;
            PopulateExistingChunks();
        }
    }

    private void HandleChunkGeneration()
    {
        int x = Mathf.RoundToInt(playerTransform.position.x / chunkSize);
        int z = Mathf.RoundToInt(playerTransform.position.z / chunkSize);
        Vector2Int playerChunkCoord = new Vector2Int(x, z);

        if (playerChunkCoord != _currentChunkCoord)
        {
            _currentChunkCoord = playerChunkCoord;
            UpdateVisibleChunks();
        }
    }

    private void InitializePool()
    {
        _chunkPool = new ObjectPool<LevelChunk>(
            createFunc: () => Instantiate(chunkPrefab, transform),
            actionOnGet: (chunk) => chunk.gameObject.SetActive(true),
            actionOnRelease: (chunk) => {
                chunk.Recycle();
                chunk.gameObject.SetActive(false);
            },
            actionOnDestroy: (chunk) => Destroy(chunk.gameObject),
            defaultCapacity: 25,
            maxSize: 50
        );
    }

    private void UpdateVisibleChunks(bool forceUpdate = false)
    {
        HashSet<Vector2Int> coordsToKeep = new HashSet<Vector2Int>();
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                coordsToKeep.Add(_currentChunkCoord + new Vector2Int(x, y));
            }
        }

        List<Vector2Int> coordsToRemove = new List<Vector2Int>();
        foreach (var kvp in _activeChunks)
        {
            if (!coordsToKeep.Contains(kvp.Key))
                coordsToRemove.Add(kvp.Key);
        }

        foreach (var coord in coordsToRemove)
        {
            LevelChunk chunkToRemove = _activeChunks[coord];
            _chunkPool.Release(chunkToRemove);
            _activeChunks.Remove(coord);
        }

        foreach (var coord in coordsToKeep)
        {
            if (!_activeChunks.ContainsKey(coord))
                SpawnChunk(coord);
        }
    }

    private void SpawnChunk(Vector2Int coord)
    {
        LevelChunk newChunk = _chunkPool.Get();

        Vector3 position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        newChunk.transform.position = position;

        newChunk.Setup(coord, enemyPoolManager, obstaclePoolManager, playerTransform);

        if (spawnObstaclesOnGeneration && obstaclePoolManager != null)
        {
            newChunk.PopulateObstacles(chunkSize, obstaclesPerChunk, minDistanceBetweenObstacles);
        }

        if (isSpawningEnabled)
        {
            TrySpawnEnemiesInChunk(newChunk, coord);
        }

        _activeChunks.Add(coord, newChunk);
    }

    private void PopulateExistingChunks()
    {
        Debug.Log("Survival Started! Spawning initial wave...");
        foreach (var kvp in _activeChunks)
        {
            TrySpawnEnemiesInChunk(kvp.Value, kvp.Key);
        }
    }

    private void TrySpawnEnemiesInChunk(LevelChunk chunk, Vector2Int coord)
    {
        if (!spawnEnemiesOnGeneration || enemiesPerChunk <= 0) return;

        Vector2Int distToPlayer = coord - _currentChunkCoord;

        bool isSafeZone = Mathf.Abs(distToPlayer.x) <= 1 && Mathf.Abs(distToPlayer.y) <= 1;

        if (!isSafeZone)
        {
            chunk.PopulateEnemies(chunkSize, enemiesPerChunk);
        }
    }

    private void OnGUI()
    {
        if (currentSpawnTimer > 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.red;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(Screen.width / 2 - 100, 50, 200, 50),
                $"SURVIVAL IN: {currentSpawnTimer:F1}", style);
        }
    }
}