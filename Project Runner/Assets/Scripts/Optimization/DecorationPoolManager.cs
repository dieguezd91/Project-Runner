using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class DecorationPoolManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Lista de tipos de decoraciones disponibles")]
    [SerializeField] private DecorationConfigSO[] decorationConfigSO;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSizePerType = 50;
    [SerializeField] private int maxPoolSizePerType = 200;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private Dictionary<DecorationConfigSO, ObjectPool<GameObject>> decorationPools =
        new Dictionary<DecorationConfigSO, ObjectPool<GameObject>>();

    private int activeDecorationsCount = 0;

    public int ActiveDecorationsCount => activeDecorationsCount;

    private void Awake()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        if (decorationConfigSO == null || decorationConfigSO.Length == 0)
        {
            Debug.LogWarning("No decoration data configured!");
            return;
        }

        foreach (var decorationData in decorationConfigSO)
        {
            if (decorationData == null || decorationData.prefab == null)
            {
                Debug.LogWarning($"DecorationData nulo o sin prefab asignado");
                continue;
            }

            var pool = new ObjectPool<GameObject>(
                createFunc: () => CreateDecoration(decorationData),
                actionOnGet: OnGetDecoration,
                actionOnRelease: OnReleaseDecoration,
                actionOnDestroy: OnDestroyDecoration,
                collectionCheck: true,
                defaultCapacity: initialPoolSizePerType,
                maxSize: maxPoolSizePerType
            );

            decorationPools.Add(decorationData, pool);

            GameObject[] prewarmed = new GameObject[initialPoolSizePerType];
            for (int i = 0; i < initialPoolSizePerType; i++)
            {
                prewarmed[i] = pool.Get();
            }
            for (int i = 0; i < initialPoolSizePerType; i++)
            {
                pool.Release(prewarmed[i]);
            }
        }

        if (showDebug)
        {
            Debug.Log($"Decoration Pools initialized: {decorationPools.Count} types");
        }
    }

    private GameObject CreateDecoration(DecorationConfigSO data)
    {
        GameObject decoration = Instantiate(data.prefab, transform);
        decoration.SetActive(false);

        RemoveAllColliders(decoration);

        var tracker = decoration.AddComponent<PooledDecoration>();
        tracker.decorationData = data;

        return decoration;
    }

    private void RemoveAllColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
        {
            Destroy(collider);
        }

        if (showDebug && colliders.Length > 0)
        {
            Debug.Log($"Removed {colliders.Length} colliders from decoration: {obj.name}");
        }
    }

    private void OnGetDecoration(GameObject decoration)
    {
        decoration.SetActive(true);
        activeDecorationsCount++;
    }

    private void OnReleaseDecoration(GameObject decoration)
    {
        decoration.SetActive(false);
        activeDecorationsCount--;
    }

    private void OnDestroyDecoration(GameObject decoration)
    {
        if (decoration != null)
        {
            Destroy(decoration);
        }
    }

    public GameObject GetRandomDecoration(Vector3 position, Quaternion rotation)
    {
        DecorationConfigSO selected = SelectWeightedRandom();
        if (selected == null) return null;

        return GetDecoration(selected, position, rotation);
    }

    public GameObject GetDecoration(DecorationConfigSO data, Vector3 position, Quaternion rotation)
    {
        if (!decorationPools.ContainsKey(data))
        {
            Debug.LogWarning($"No pool found for {data.name}");
            return null;
        }

        GameObject decoration = decorationPools[data].Get();

        decoration.transform.position = position + Vector3.up * data.heightOffset;

        if (data.randomRotation)
        {
            float randomY = Random.Range(0f, 360f);
            decoration.transform.rotation = Quaternion.Euler(0, randomY, 0);
        }
        else
        {
            decoration.transform.rotation = rotation;
        }

        if (data.randomScale)
        {
            float randomScale = Random.Range(data.minScale, data.maxScale);
            decoration.transform.localScale = Vector3.one * randomScale;
        }

        return decoration;
    }

    public void ReturnDecoration(GameObject decoration)
    {
        if (decoration == null) return;

        var tracker = decoration.GetComponent<PooledDecoration>();
        if (tracker == null || tracker.decorationData == null)
        {
            Debug.LogWarning("Decoration sin PooledDecoration component");
            Destroy(decoration);
            return;
        }

        if (decorationPools.ContainsKey(tracker.decorationData))
        {
            decoration.transform.SetParent(this.transform);

            decorationPools[tracker.decorationData].Release(decoration);
        }
    }

    private DecorationConfigSO SelectWeightedRandom()
    {
        if (decorationConfigSO.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var data in decorationConfigSO)
        {
            if (data != null)
                totalWeight += data.spawnWeight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var data in decorationConfigSO)
        {
            if (data == null) continue;

            cumulativeWeight += data.spawnWeight;
            if (randomValue <= cumulativeWeight)
            {
                return data;
            }
        }

        return decorationConfigSO[0];
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        float x = 320f;
        float y = 10f;
        float w = 350f;
        float h = 25f;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;

        int totalBoxHeight = 80 + (decorationPools.Count * 25);
        GUI.Box(new Rect(x, y, w, totalBoxHeight), "");

        y += 10f;

        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(x + 10, y, w - 20, h), "DECORATION POOL", style);
        y += h;

        style.fontSize = 12;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Active: {activeDecorationsCount}", style);
        y += h;

        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Types: {decorationPools.Count}", style);
        y += h + 5;

        style.fontSize = 11;
        foreach (var kvp in decorationPools)
        {
            string name = kvp.Key.decorationName;
            int active = kvp.Value.CountActive;
            int inactive = kvp.Value.CountInactive;

            GUI.Label(new Rect(x + 10, y, w - 20, h),
                $"{name}: {active} / {inactive}", style);
            y += h;
        }
    }
}

public class PooledDecoration : MonoBehaviour
{
    [HideInInspector]
    public DecorationConfigSO decorationData;
}