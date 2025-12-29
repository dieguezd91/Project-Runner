using UnityEngine;
using System;

[System.Serializable]
public struct RunStats
{
    public float distance;
    public float time;

    public RunStats(float distance, float time)
    {
        this.distance = distance;
        this.time = time;
    }
}

/// <summary>
/// Trackea estadísticas de la run actual: distancia y tiempo.
/// NO maneja UI, solo datos.
/// </summary>
public class RunStatsTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;

    private float distanceTraveled = 0f;
    private Vector3 lastPosition;
    private float startTime;
    private bool isTracking = false;

    public float DistanceTraveled => distanceTraveled;
    public float TimeAlive => isTracking ? Time.time - startTime : 0f;

    public event Action<float> OnDistanceChanged;
    public event Action<float> OnTimeChanged;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (playerTransform != null)
        {
            lastPosition = playerTransform.position;
            StartTracking();
        }
        else
        {
            Debug.LogError("RunStatsTracker: Player transform not found!");
        }
    }

    private void Update()
    {
        if (!isTracking || playerTransform == null) return;

        // Calcular distancia (solo en plano XZ)
        Vector3 currentPos = playerTransform.position;
        float delta = Vector3.Distance(
            new Vector3(lastPosition.x, 0, lastPosition.z),
            new Vector3(currentPos.x, 0, currentPos.z)
        );

        distanceTraveled += delta;
        lastPosition = currentPos;

        // Notificar cambios
        OnDistanceChanged?.Invoke(distanceTraveled);
        OnTimeChanged?.Invoke(TimeAlive);
    }

    public void StartTracking()
    {
        isTracking = true;
        startTime = Time.time;
        distanceTraveled = 0f;
    }

    public void StopTracking()
    {
        isTracking = false;
    }

    public RunStats GetFinalStats()
    {
        return new RunStats(distanceTraveled, TimeAlive);
    }
}