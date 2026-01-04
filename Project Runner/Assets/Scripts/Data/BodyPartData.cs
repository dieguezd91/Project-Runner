using UnityEngine;

[CreateAssetMenu(fileName = "New Body Part", menuName = "Project Runner/Body Parts/Body Part Data")]
public class BodyPartData : ScriptableObject
{
    [Header("Identificación")]
    public BodyPartType partType;
    public string partName;
    [TextArea(2, 4)]
    public string description;

    [Header("Visual (Prototipo)")]
    public Color prototypeColor = Color.white;
    public float orbitRadius = 1.5f;
    public float orbitSpeed = 30f;

    [Header("Habilidad - General")]
    public float cooldownDuration = 2f;
    public bool requiresInput = true;

    [Header("Habilidad - Legs (Dash)")]
    [Tooltip("Fuerza del dash")]
    public float dashForce = 25f;
    [Tooltip("Duración del dash en segundos")]
    public float dashDuration = 0.2f;
    [Tooltip("Multiplicador de velocidad durante el dash")]
    public float dashSpeedMultiplier = 2f;

    [Header("Habilidad - Arms (Stomp)")]
    [Tooltip("Fuerza del stomp hacia abajo")]
    public float stompForce = 30f;
    [Tooltip("Radio de detección de enemigos")]
    public float stompRadius = 5f;

    [Header("Habilidad - Tail (Grapple)")]
    [Tooltip("Distancia máxima del grapple")]
    public float grappleDistance = 20f;
    [Tooltip("Velocidad de atracción del grapple")]
    public float grappleSpeed = 30f;

    [Header("Habilidad - Head (Visión)")]
    [Tooltip("Radio de detección aumentado")]
    public float detectionRadius = 15f;

    [Header("Habilidad - Torso (Shield)")]
    [Tooltip("Duración del shield")]
    public float shieldDuration = 3f;
    [Tooltip("Cooldown del shield")]
    public float shieldCooldown = 10f;
}

public enum BodyPartType
{
    Legs,    // Patas - Dash, Wall Jump
    Arms,    // Brazos - Stomp, Punch
    Back,    // Cola - Grapple, Swing
    Head,    // Cabeza - Visión mejorada, detección
    Chest,    // Torso - Shield, resistencia
    Cannon
}