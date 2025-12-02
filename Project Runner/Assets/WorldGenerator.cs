using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class WorldGenerator : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private LevelChunk chunkPrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int chunkSize = 50;
    [SerializeField] private int viewDistance = 2; // Radio de chunks (2 = grilla de 5x5)

    // State
    private Vector2Int _currentChunkCoord;
    private Dictionary<Vector2Int, LevelChunk> _activeChunks = new Dictionary<Vector2Int, LevelChunk>();
    private ObjectPool<LevelChunk> _chunkPool;

    private void Awake()
    {
        InitializePool();
    }

    private void Start()
    {
        // Forzamos la primera generación
        UpdateVisibleChunks(true);
    }

    private void Update()
    {
        // Calculamos la coordenada del chunk donde está el player
        // Mathf.RoundToInt funciona mejor si el pivote del chunk está en el centro
        int x = Mathf.RoundToInt(playerTransform.position.x / chunkSize);
        int z = Mathf.RoundToInt(playerTransform.position.z / chunkSize);
        Vector2Int playerChunkCoord = new Vector2Int(x, z);

        // Solo actualizamos si cambiamos de chunk
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
            actionOnRelease: (chunk) => chunk.gameObject.SetActive(false),
            actionOnDestroy: (chunk) => Destroy(chunk.gameObject),
            defaultCapacity: 25,
            maxSize: 50
        );
    }

    private void UpdateVisibleChunks(bool forceUpdate = false)
    {
        // 1. Identificar coordenadas que DEBEN estar visibles
        HashSet<Vector2Int> coordsToKeep = new HashSet<Vector2Int>();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                Vector2Int offset = new Vector2Int(x, y);
                coordsToKeep.Add(_currentChunkCoord + offset);
            }
        }

        // 2. Limpiar chunks viejos (que ya no están en coordsToKeep)
        // Usamos una lista temporal para evitar modificar el Dictionary mientras iteramos
        List<Vector2Int> coordsToRemove = new List<Vector2Int>();

        foreach (var kvp in _activeChunks)
        {
            if (!coordsToKeep.Contains(kvp.Key))
            {
                coordsToRemove.Add(kvp.Key);
            }
        }

        foreach (var coord in coordsToRemove)
        {
            LevelChunk chunkToRemove = _activeChunks[coord];
            _chunkPool.Release(chunkToRemove); // Devuelve al pool
            _activeChunks.Remove(coord);
        }

        // 3. Spawneamos los nuevos
        foreach (var coord in coordsToKeep)
        {
            if (!_activeChunks.ContainsKey(coord))
            {
                SpawnChunk(coord);
            }
        }
    }

    private void SpawnChunk(Vector2Int coord)
    {
        LevelChunk newChunk = _chunkPool.Get();

        // Posicionamiento matemático: Coord * Tamaño
        Vector3 position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        newChunk.transform.position = position;
        newChunk.Setup(coord);

        _activeChunks.Add(coord, newChunk);
    }
}