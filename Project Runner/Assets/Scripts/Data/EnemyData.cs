using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Project Runner/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Detección")]
    public float chaseRange = 50f; // Rango en el que empieza a perseguir

    [Header("Visual")]
    public Color debugColor = Color.red;
}