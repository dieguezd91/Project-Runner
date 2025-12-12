using UnityEngine;

[CreateAssetMenu(fileName = "TerrainConfig", menuName = "Project Runner/Terrain Config")]
public class TerrainConfigSO : ScriptableObject
{
    [System.Serializable]
    public class TerrainVariant
    {
        [Tooltip("Prefab del suelo (debe tener MeshFilter, MeshRenderer, MeshCollider)")]
        public GameObject terrainPrefab;

        [Tooltip("Peso de spawn - mayor valor = mayor probabilidad")]
        [Range(1, 100)]
        public int spawnWeight = 10;

        [Tooltip("Nombre descriptivo para debug")]
        public string variantName;
    }

    [Header("Variantes de Terreno")]
    [Tooltip("Lista de todos los prefabs de suelo disponibles")]
    public TerrainVariant[] terrainVariants;

    [Header("Configuración")]
    [Tooltip("Si es true, cada chunk usa un solo tipo de terreno. Si es false, se subdivide el chunk")]
    public bool oneTerrainPerChunk = true;

    [Tooltip("Si oneTerrainPerChunk = false, cantidad de subdivisiones por chunk")]
    [Range(1, 5)]
    public int subdivisionsPerChunk = 2;

    public GameObject GetRandomTerrainPrefab()
    {
        if (terrainVariants == null || terrainVariants.Length == 0)
        {
            Debug.LogError("No terrain variants configured!");
            return null;
        }

        int totalWeight = 0;
        foreach (var variant in terrainVariants)
        {
            totalWeight += variant.spawnWeight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var variant in terrainVariants)
        {
            currentWeight += variant.spawnWeight;
            if (randomValue < currentWeight)
            {
                return variant.terrainPrefab;
            }
        }

        return terrainVariants[0].terrainPrefab;
    }
}