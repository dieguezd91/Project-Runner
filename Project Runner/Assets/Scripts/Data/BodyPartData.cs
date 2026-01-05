using UnityEngine;

[CreateAssetMenu(fileName = "New Body Part", menuName = "Project Runner/Body Parts/Body Part Data")]
public class BodyPartData : ScriptableObject
{
    [Header("Identificacion")]
    public BodyPartType partType;
    public string partName;
    [TextArea(2, 4)]
    public string description;

    [Header("Visual")]
    public Color prototypeColor = Color.white;
    public float orbitRadius = 1.5f;
    public float orbitSpeed = 30f;

    [Header("Ability - General")]
    public float cooldownDuration = 2f;
    public bool requiresInput = true;

    [Header("Ability - Back (Dash)")]
    [Tooltip("Fuerza del dash")]
    public float dashForce = 25f;
    [Tooltip("Duracion del dash en segundos")]
    public float dashDuration = 0.2f;
    [Tooltip("Multiplicador de velocidad durante el dash")]
    public float dashSpeedMultiplier = 2f;

    [Header("Ability - Head (Stomp)")]
    [Tooltip("Fuerza del stomp hacia abajo")]
    public float stompForce = 30f;
    [Tooltip("Radio de deteccion de enemigos")]
    public float stompRadius = 5f;

    [Header("Ability - Cannon")]
    [Tooltip("Distancia maxima del grapple")]
    public float grappleDistance = 20f;
    [Tooltip("Velocidad de atracci�n del grapple")]
    public float grappleSpeed = 30f;

    [Header("Ability - Arms")]
    [Tooltip("Radio de deteccion aumentado")]
    public float detectionRadius = 15f;

    [Header("Habilidad - Chest (Double Jump)")]
    [Tooltip("Fuerza del segundo salto")]
    public float doubleJumpForce = 12f;
    [Tooltip("Costo de energía del double jump")]
    public float doubleJumpEnergyCost = 20f;
    [Tooltip("Cooldown del double jump")]
    public float doubleJumpCooldown = 3f;
    [Tooltip("Multiplicador de conservación de velocidad horizontal en aire")]
    public float airVelocityConservation = 1.0f; // 100% = sin pérdida

    [Header("Ability - Legs (Jump)")]
    [Tooltip("Fuerza del salto mejorado")]
    public float jumpForce = 15f;
    [Tooltip("Altura objetivo del salto en metros")]
    public float jumpHeight = 3f;
    [Tooltip("Multiplicador de velocidad en suelo (1.2 = +20%)")]
    public float speedBonus = 1.2f;                 
}

public enum BodyPartType
{
    Legs,
    Arms,
    Back,
    Head,
    Chest,
    Cannon
}