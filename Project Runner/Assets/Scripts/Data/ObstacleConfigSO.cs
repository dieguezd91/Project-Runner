using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleData", menuName = "Game/Obstacle Data")]
public class ObstacleConfigSO : ScriptableObject
{
    [Header("Prefab")]
    [Tooltip("Prefab del obstáculo de Polygon")]
    public GameObject prefab;

    [Header("Spawn Settings")]
    [Tooltip("Peso de probabilidad (mayor = más común)")]
    [Range(0f, 100f)]
    public float spawnWeight = 50f;

    [Tooltip("¿Puede rotar aleatoriamente en Y?")]
    public bool randomRotation = true;

    [Tooltip("¿Puede escalar aleatoriamente?")]
    public bool randomScale = false;

    [Range(0.5f, 2f)]
    public float minScale = 0.8f;

    [Range(0.5f, 2f)]
    public float maxScale = 1.2f;

    [Header("Placement")]
    [Tooltip("Distancia mínima entre este obstáculo y otros")]
    public float minDistanceBetweenObstacles = 3f;

    [Tooltip("Offset en Y desde el suelo")]
    public float heightOffset = 0f;

    [Header("Categories")]
    [Tooltip("Tipo de obstáculo (para filtrar spawns)")]
    public ObstacleType obstacleType = ObstacleType.Prop;
}

public enum ObstacleType
{
    Prop,       // Props decorativos (rocas, árboles pequeños)
    Structure,  // Estructuras grandes (edificios, muros)
    Hazard,     // Obstáculos peligrosos (trampas, pinchos)
    Cover       // Coberturas (para evitar The Swarm)
}