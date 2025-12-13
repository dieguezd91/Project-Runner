using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Configs/PlayerConfig")]
public class PlayerConfigSO : ScriptableObject
{
    [Header("Movimiento Base")]
    public float maxSpeed = 10f;
    public float acceleration = 90f;
    public float friction = 8f;

    [Header("Curva de Aceleración")]
    [Range(0.1f, 3f)]
    public float accelerationCurveStart = 1.5f; // Multiplicador inicial (acelera más rápido)
    [Range(0.1f, 2f)]
    public float accelerationCurveEnd = 0.3f;   // Multiplicador final (acelera más lento)

    [Header("Rotación")]
    public float rotationSpeed = 11f;

    [Header("Salto")]
    public float jumpForce = 15f;
    public float jumpCutMultiplier = 0.5f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;

    [Header("Gravedad")]
    public float gravityMultiplier = 3f;

    [Header("Drift System")]
    [Tooltip("Velocidad mínima para empezar a driftear")]
    public float driftMinSpeed = 5f;

    [Tooltip("Ángulo mínimo de giro para activar drift (en grados)")]
    [Range(30f, 90f)]
    public float driftAngleThreshold = 45f;

    [Tooltip("Velocidad de acumulación de carga de drift (0-1 por segundo)")]
    public float driftChargeRate = 0.8f;

    [Tooltip("Velocidad de descarga cuando no hay drift activo")]
    public float driftDischargeRate = 2f;

    [Tooltip("Multiplicador de velocidad del boost")]
    [Range(1.0f, 2.0f)]
    public float driftBoostMultiplier = 1.3f; // +30%

    [Tooltip("Duración del boost en segundos")]
    public float driftBoostDuration = 1.5f;

    [Tooltip("Reducción de fricción durante el drift (más bajo = más deslizamiento)")]
    [Range(0.1f, 1f)]
    public float driftFrictionMultiplier = 0.6f;

    [Header("Física")]
    public LayerMask groundLayer;
}