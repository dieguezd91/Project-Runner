using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/EnemyConfig")]
public class EnemyConfigSO : ScriptableObject
{
    [Header("Movimiento Base")]
    [Tooltip("Velocidad base del enemigo (85% del player aprox)")]
    public float baseSpeed = 8.5f;

    [Tooltip("Aceleración del enemigo")]
    public float acceleration = 15f;

    [Tooltip("Rotación hacia el objetivo")]
    public float rotationSpeed = 5f;

    [Header("Detección")]
    [Tooltip("Radio base de detección cuando hay pocos enemigos")]
    public float baseDetectionRadius = 10f;

    [Header("Boids Weights (Comportamiento de Enjambre)")]
    [Range(0f, 1f)]
    [Tooltip("Peso de perseguir al jugador")]
    public float targetWeight = 0.6f;

    [Range(0f, 1f)]
    [Tooltip("Peso de cohesión (mantenerse juntos)")]
    public float cohesionWeight = 0.2f;

    [Range(0f, 1f)]
    [Tooltip("Peso de separación (no apilarse)")]
    public float separationWeight = 0.15f;

    [Range(0f, 1f)]
    [Tooltip("Peso de alineación (seguir dirección del grupo)")]
    public float alignmentWeight = 0.05f;

    [Header("Boids Parameters")]
    [Tooltip("Radio para detectar vecinos cercanos")]
    public float neighborRadius = 5f;

    [Tooltip("Distancia mínima entre enemigos")]
    public float separationDistance = 1.5f;

    [Header("Física")]
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer;
}