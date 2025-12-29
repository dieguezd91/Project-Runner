using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maneja el estado global del juego: Muerte, Restart, Tracking de distancia.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject gameOverPanel;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI finalDistanceText;
    [SerializeField] private TextMeshProUGUI timeAliveText;
    [SerializeField] private TextMeshProUGUI finalTimeAliveText;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private float distanceTraveled = 0f;
    private Vector3 lastPosition;
    private bool isGameOver = false;
    private float startTime;
    private float timeAlive;

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
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath += HandlePlayerDeath;
            lastPosition = playerHealth.transform.position;
        }
        else
        {
            Debug.LogError("GameManager: PlayerHealth not found!");
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        startTime = Time.time;
    }

    private void Update()
    {
        if (isGameOver) return;

        // Actualizar distancia recorrida
        if (playerHealth != null)
        {
            Vector3 currentPos = playerHealth.transform.position;
            float delta = Vector3.Distance(
                new Vector3(lastPosition.x, 0, lastPosition.z),
                new Vector3(currentPos.x, 0, currentPos.z)
            );

            distanceTraveled += delta;
            lastPosition = currentPos;

            // Actualizar tiempo vivo
            timeAlive = Time.time - startTime;

            // Actualizar UI en tiempo real
            if (distanceText != null)
            {
                distanceText.text = $"Distance: {distanceTraveled:F1}m";
            }

            if (timeAliveText != null)
            {
                int minutes = Mathf.FloorToInt(timeAlive / 60f);
                int seconds = Mathf.FloorToInt(timeAlive % 60f);
                timeAliveText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }
    }

    private void HandlePlayerDeath()
    {
        if (isGameOver) return;

        isGameOver = true;

        if (showDebug)
        {
            Debug.Log($"=== GAME OVER ===");
            Debug.Log($"Distance: {distanceTraveled:F1}m");
            Debug.Log($"Time Alive: {timeAlive:F1}s");
        }

        // Detener tiempo
        Time.timeScale = 0f;

        // Mostrar Game Over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (finalDistanceText != null)
            {
                finalDistanceText.text = $"Distance: {distanceTraveled:F1}m";
            }

            if (finalTimeAliveText != null)
            {
                int minutes = Mathf.FloorToInt(timeAlive / 60f);
                int seconds = Mathf.FloorToInt(timeAlive % 60f);
                finalTimeAliveText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }
    }

    public void RestartGame()
    {
        // Restaurar timeScale
        Time.timeScale = 1f;

        // Recargar escena
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