using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maneja el estado global del juego: Game Over, Restart, Quit.
/// NO maneja UI ni tracking de stats.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private RunStatsTracker statsTracker;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private HUDManager hudManager;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private bool isGameOver = false;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Auto-find references si no están asignadas
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (statsTracker == null)
        {
            statsTracker = FindFirstObjectByType<RunStatsTracker>();
        }

        if (gameOverUI == null)
        {
            gameOverUI = FindFirstObjectByType<GameOverUI>();
        }

        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<HUDManager>();
        }

        // Suscribirse a muerte del jugador
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath += HandlePlayerDeath;
        }
        else
        {
            Debug.LogError("GameManager: PlayerHealth not found!");
        }
    }

    private void HandlePlayerDeath()
    {
        if (isGameOver) return;

        isGameOver = true;

        // Obtener stats finales
        RunStats stats = statsTracker != null
            ? statsTracker.GetFinalStats()
            : new RunStats(0f, 0f);

        if (showDebug)
        {
            Debug.Log($"=== GAME OVER ===");
            Debug.Log($"Distance: {stats.distance:F1}m");
            Debug.Log($"Time Alive: {stats.time:F1}s");
        }

        // Pausar juego
        Time.timeScale = 0f;

        // Ocultar HUD
        if (hudManager != null)
        {
            hudManager.Hide();
        }

        // Mostrar Game Over UI
        if (gameOverUI != null)
        {
            gameOverUI.Show(stats.distance, stats.time);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        if (showDebug)
        {
            Debug.Log("Quitting game...");
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= HandlePlayerDeath;
        }
    }
}