using UnityEngine;

[CreateAssetMenu(fileName = "DecorationData", menuName = "Project Runner/Decoration Data")]
public class DecorationConfigSO : ScriptableObject
{
    [Header("Prefab")]
    [Tooltip("Prefab de decoración (sin colisión)")]
    public GameObject prefab;

    [Header("Spawn Settings")]
    [Tooltip("Peso de spawn - mayor valor = mayor probabilidad")]
    [Range(1, 100)]
    public int spawnWeight = 10;

    [Header("Transform Randomization")]
    [Tooltip("¿Aplicar rotación aleatoria en Y?")]
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
    [Tooltip("Categoría para organización (ej: Rocks, Plants, Flowers)")]
    public string category = "General";

    [Tooltip("Nombre descriptivo para debug")]
    public string decorationName;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(decorationName) && prefab != null)
        {
            decorationName = prefab.name;
        }

        if (minScale > maxScale)
        {
            minScale = maxScale;
        }
    }
}