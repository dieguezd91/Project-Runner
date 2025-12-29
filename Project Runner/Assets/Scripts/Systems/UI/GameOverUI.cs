using TMPro;
using UnityEngine;

/// <summary>
/// Maneja la UI de Game Over.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI finalDistanceText;
    [SerializeField] private TextMeshProUGUI finalTimeText;

    private void Start()
    {
        Hide();
    }

    public void Show(float distance, float timeAlive)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (finalDistanceText != null)
        {
            finalDistanceText.text = $"Distance: {distance:F1}m";
        }

        if (finalTimeText != null)
        {
            int minutes = Mathf.FloorToInt(timeAlive / 60f);
            int seconds = Mathf.FloorToInt(timeAlive % 60f);
            finalTimeText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}