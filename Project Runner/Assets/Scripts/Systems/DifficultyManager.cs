using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyTier
    {
        [Tooltip("Tiempo en segundos cuando inicia este tier")]
        public float startTime;

        [Tooltip("Probabilidad de spawn de obstáculos (0-1)")]
        [Range(0f, 1f)]
        public float obstacleSpawnChance = 0.5f;

        [Tooltip("Rango de obstáculos por chunk cuando spawneamos")]
        public Vector2Int obstaclesPerChunkRange = new Vector2Int(1, 2);

        [Tooltip("Probabilidad de spawn de enemigos (0-1)")]
        [Range(0f, 1f)]
        public float enemySpawnChance = 0.3f;

        [Tooltip("Rango de enemigos por chunk cuando spawneamos")]
        public Vector2Int enemiesPerChunkRange = new Vector2Int(1, 3);

        [Tooltip("Nombre descriptivo del tier")]
        public string tierName = "Easy";
    }

    [Header("Difficulty Tiers")]
    [SerializeField]
    private DifficultyTier[] difficultyTiers = new DifficultyTier[]
    {
        new DifficultyTier
        {
            startTime = 0f,
            obstacleSpawnChance = 0.3f,
            obstaclesPerChunkRange = new Vector2Int(0, 1),
            enemySpawnChance = 0f,
            enemiesPerChunkRange = new Vector2Int(0, 0),
            tierName = "Tutorial (0-30s)"
        },
        new DifficultyTier
        {
            startTime = 30f,
            obstacleSpawnChance = 0.5f,
            obstaclesPerChunkRange = new Vector2Int(1, 1),
            enemySpawnChance = 0.2f,
            enemiesPerChunkRange = new Vector2Int(0, 1),
            tierName = "Easy (30s-1min)"
        },
        new DifficultyTier
        {
            startTime = 60f,
            obstacleSpawnChance = 0.7f,
            obstaclesPerChunkRange = new Vector2Int(1, 2),
            enemySpawnChance = 0.4f,
            enemiesPerChunkRange = new Vector2Int(1, 2),
            tierName = "Medium (1-2min)"
        },
        new DifficultyTier
        {
            startTime = 120f,
            obstacleSpawnChance = 0.9f,
            obstaclesPerChunkRange = new Vector2Int(1, 3),
            enemySpawnChance = 0.6f,
            enemiesPerChunkRange = new Vector2Int(2, 4),
            tierName = "Hard (2min+)"
        }
    };

    [Header("Settings")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool showDebug = true;

    private float gameTime = 0f;
    private DifficultyTier currentTier;
    private int currentTierIndex = 0;
    private bool isRunning = false;

    public static DifficultyManager Instance { get; private set; }

    public float GameTime => gameTime;
    public DifficultyTier CurrentTier => currentTier;
    public int CurrentTierIndex => currentTierIndex;
    public bool IsRunning => isRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (difficultyTiers.Length > 0)
        {
            currentTier = difficultyTiers[0];
        }
    }

    private void Start()
    {
        if (startOnAwake)
        {
            StartGame();
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        gameTime += Time.deltaTime;
        UpdateDifficultyTier();
    }

    public void StartGame()
    {
        gameTime = 0f;
        currentTierIndex = 0;
        if (difficultyTiers.Length > 0)
        {
            currentTier = difficultyTiers[0];
        }
        isRunning = true;

        if (showDebug)
        {
            Debug.Log($"Difficulty Manager Started - Initial Tier: {currentTier.tierName}");
        }
    }

    public void PauseGame()
    {
        isRunning = false;
    }

    public void ResumeGame()
    {
        isRunning = true;
    }

    public void ResetGame()
    {
        gameTime = 0f;
        currentTierIndex = 0;
        if (difficultyTiers.Length > 0)
        {
            currentTier = difficultyTiers[0];
        }
    }

    private void UpdateDifficultyTier()
    {
        for (int i = difficultyTiers.Length - 1; i >= 0; i--)
        {
            if (gameTime >= difficultyTiers[i].startTime)
            {
                if (currentTierIndex != i)
                {
                    currentTierIndex = i;
                    currentTier = difficultyTiers[i];

                    if (showDebug)
                    {
                        Debug.Log($"Difficulty Tier Changed: {currentTier.tierName} at {gameTime:F1}s");
                    }
                }
                break;
            }
        }
    }

    public bool ShouldSpawnObstacles()
    {
        if (currentTier == null) return false;
        return Random.value <= currentTier.obstacleSpawnChance;
    }

    public int GetObstacleCount()
    {
        if (currentTier == null) return 0;
        return Random.Range(
            currentTier.obstaclesPerChunkRange.x,
            currentTier.obstaclesPerChunkRange.y + 1
        );
    }

    public bool ShouldSpawnEnemies()
    {
        if (currentTier == null) return false;
        return Random.value <= currentTier.enemySpawnChance;
    }

    public int GetEnemyCount()
    {
        if (currentTier == null) return 0;
        return Random.Range(
            currentTier.enemiesPerChunkRange.x,
            currentTier.enemiesPerChunkRange.y + 1
        );
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private void OnGUI()
    {
        if (!showDebug || !isRunning) return;

        float x = 10f;
        float y = 10f;
        float w = 300f;
        float h = 25f;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;

        GUI.Box(new Rect(x, y, w, 150f), "");

        y += 10f;

        style.normal.textColor = Color.yellow;
        GUI.Label(new Rect(x + 10, y, w - 20, h), "DIFFICULTY MANAGER", style);
        y += h + 5;

        style.fontSize = 12;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Time: {GetFormattedTime()}", style);
        y += h;

        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Tier: {currentTier?.tierName ?? "None"}", style);
        y += h;

        GUI.Label(new Rect(x + 10, y, w - 20, h),
            $"Obstacle Chance: {(currentTier?.obstacleSpawnChance ?? 0) * 100:F0}%", style);
        y += h;

        GUI.Label(new Rect(x + 10, y, w - 20, h),
            $"Enemy Chance: {(currentTier?.enemySpawnChance ?? 0) * 100:F0}%", style);
    }
}