using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Stampede/Player Config")]
public class PlayerConfigSO : ScriptableObject
{
    [Header("Locomotion")]
    public float maxSpeed = 15f;
    public float acceleration = 60f;
    public float groundDrag = 4f;     // Alta para frenar rápido, baja para "hielo/drift"
    public float airDrag = 1f;        // Menor control en el aire
    public float rotationSpeed = 12f; // Que tan rápido gira el modelo

    [Header("Jumping")]
    public float jumpForce = 15f;
    public float gravityMultiplier = 2.5f; // Para caídas rápidas (snappy jumps)
    public LayerMask groundLayer;

    [Header("Advanced Jump")]
    public float jumpCutMultiplier = 0.5f; // Cuanto se corta el salto al soltar el botón
    public float coyoteTime = 0.15f;       // Tiempo de gracia al caer de un borde
    public float jumpBufferTime = 0.1f;    // Tiempo de gracia al pulsar saltar antes de tocar suelo

    [Header("Procedural Animation")]
    public float tiltAngle = 10f;          // Grados de inclinación al correr
    public float tiltSpeed = 8f;
    public Vector3 jumpSquash = new Vector3(0.8f, 1.2f, 0.8f); // Estirarse al saltar
    public Vector3 landSquash = new Vector3(1.2f, 0.8f, 1.2f); // Aplastarse al caer
    public float deformationSpeed = 10f;
}