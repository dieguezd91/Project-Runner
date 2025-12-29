using TMPro;
using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Maneja la cuenta regresiva inicial del juego.
/// </summary>
public class CountdownUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Settings")]
    [SerializeField] private int countdownFrom = 3;

    [Header("Colors")]
    [SerializeField] private Color countdownColor = Color.yellow;
    [SerializeField] private Color goColor = Color.green;

    private bool isWaitingForInput = true;
    private bool isCountingDown = false;

    public bool IsWaitingForInput => isWaitingForInput;
    public bool IsCountingDown => isCountingDown;

    public event Action OnCountdownComplete;

    private void Start()
    {
        ShowPrompt();
    }

    private void Update()
    {
        // Solo detectar ENTER si estamos esperando input
        if (isWaitingForInput && !isCountingDown)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                StartCountdown();
            }
        }
    }

    private void ShowPrompt()
    {
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "PRESS ENTER TO START";
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void StartCountdown()
    {
        if (isCountingDown) return;

        isWaitingForInput = false;
        isCountingDown = true;

        // Desactivar prompt text
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }

        // Activar countdown text
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        // Countdown: 3... 2... 1...
        for (int i = countdownFrom; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                countdownText.color = countdownColor;
                countdownText.fontSize = 120;
            }

            // Animación de escala
            if (countdownText != null)
            {
                StartCoroutine(ScaleAnimation(countdownText.transform));
            }

            yield return new WaitForSecondsRealtime(1f);
        }

        // GO!
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            countdownText.color = goColor;
            countdownText.fontSize = 150;
        }

        // Animación final
        if (countdownText != null)
        {
            StartCoroutine(ScaleAnimation(countdownText.transform, 1.5f));
        }

        yield return new WaitForSecondsRealtime(0.8f);

        // Ocultar panel completo
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        isCountingDown = false;

        // Notificar que el countdown terminó
        OnCountdownComplete?.Invoke();
    }

    private IEnumerator ScaleAnimation(Transform target, float maxScale = 1.2f)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * maxScale;

        // Scale up
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            target.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Scale back
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            target.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }
}