using TMPro;
using UnityEngine;

/// <summary>
/// Maneja la UI del HUD en tiempo real: distancia, tiempo.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("References")]
    [SerializeField] private RunStatsTracker statsTracker;

    private void Start()
    {
        if (statsTracker == null)
        {
            statsTracker = FindFirstObjectByType<RunStatsTracker>();
        }

        if (statsTracker == null)
        {
            Debug.LogError("HUDManager: RunStatsTracker not found!");
        }
    }

    private void Update()
    {
        if (statsTracker == null) return;

        UpdateDistanceText(statsTracker.DistanceTraveled);
        UpdateTimeText(statsTracker.TimeAlive);
    }

    private void UpdateDistanceText(float distance)
    {
        if (distanceText != null)
        {
            distanceText.text = $"Distance: {distance:F1}m";
        }
    }

    private void UpdateTimeText(float time)
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}