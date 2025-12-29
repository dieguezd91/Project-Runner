using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Project Runner/Enemy Data")]
public class EnemyConfigSO : ScriptableObject
{
    [Header("Prefab")]
    [Tooltip("Prefab del enemigo")]
    public GameObject prefab;

    [Header("Spawn Settings")]
    [Tooltip("Peso de spawn - mayor valor = mayor probabilidad")]
    [Range(1, 100)]
    public int spawnWeight = 10;

    [Header("Movement Stats")]
    [Tooltip("Velocidad de movimiento")]
    [Range(1f, 20f)]
    public float moveSpeed = 5f;

    [Tooltip("Velocidad de rotación")]
    [Range(1f, 20f)]
    public float rotationSpeed = 10f;

    [Header("Detection")]
    [Tooltip("Rango en el que empieza a perseguir al jugador")]
    [Range(10f, 100f)]
    public float chaseRange = 50f;

    [Header("Visual")]
    [Tooltip("Color de debug para este tipo de enemigo")]
    public Color debugColor = Color.red;

    [Header("Transform Randomization")]
    [Tooltip("¿Aplicar rotación aleatoria en Y al spawnear?")]
    public bool randomRotation = true;

    [Tooltip("¿Aplicar escala aleatoria?")]
    public bool randomScale = false;

    [Tooltip("Escala mínima (si randomScale = true)")]
    [Range(0.5f, 2f)]
    public float minScale = 0.8f;

    [Tooltip("Escala máxima (si randomScale = true)")]
    [Range(0.5f, 2f)]
    public float maxScale = 1.2f;

    [Header("Positioning")]
    [Tooltip("Offset en altura para ajustar posición Y")]
    public float heightOffset = 0f;

    [Header("Categorization")]
    [Tooltip("Tipo de enemigo para organización")]
    public EnemyType enemyType = EnemyType.Basic;

    [Tooltip("Nombre descriptivo para debug")]
    public string enemyName;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(enemyName) && prefab != null)
        {
            enemyName = prefab.name;
        }

        if (minScale > maxScale)
        {
            minScale = maxScale;
        }
    }
}

public enum EnemyType
{
    Basic,      // Enemigo básico perseguidor
    Fast,       // Enemigo rápido
    Tank,       // Enemigo lento pero resistente
    Swarm       // Parte del enjambre (muchos, débiles)
}