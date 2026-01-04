using System;
using System.Collections.Generic;
using UnityEngine;

public class BodyPartManager : MonoBehaviour
{
    public static BodyPartManager Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private Transform visualParent;
    [SerializeField] private GameObject prototypeSpherePrefab;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;

    private HashSet<BodyPartType> collectedParts = new HashSet<BodyPartType>();
    private Dictionary<BodyPartType, GameObject> visualParts = new Dictionary<BodyPartType, GameObject>();

    public event Action<BodyPartType> OnPartCollected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (visualParent == null)
            visualParent = transform;
    }

    public bool HasPart(BodyPartType type)
    {
        return collectedParts.Contains(type);
    }

    public bool CollectPart(BodyPartData partData)
    {
        if (collectedParts.Contains(partData.partType))
        {
            Debug.Log($"[BodyPartManager] Ya tienes la parte: {partData.partName}");
            return false;
        }

        collectedParts.Add(partData.partType);
        CreateVisualPart(partData);
        ActivateAbility(partData); // <- NUEVA LÍNEA

        OnPartCollected?.Invoke(partData.partType);

        Debug.Log($"[BodyPartManager] Parte recogida: {partData.partName} ({collectedParts.Count}/5)");
        return true;
    }

    private void ActivateAbility(BodyPartData partData)
    {
        BodyPartAbility ability = null;

        switch (partData.partType)
        {
            case BodyPartType.Legs:
                ability = gameObject.AddComponent<LegsAbility>();
                break;
            case BodyPartType.Arms:
                ability = gameObject.AddComponent<ArmsAbility>();
                break;
            case BodyPartType.Tail:
                // ability = gameObject.AddComponent<TailAbility>();
                Debug.Log("[BodyPartManager] Tail ability - Pendiente de implementación");
                break;
            case BodyPartType.Head:
                // ability = gameObject.AddComponent<HeadAbility>();
                Debug.Log("[BodyPartManager] Head ability - Pendiente de implementación");
                break;
            case BodyPartType.Torso:
                // ability = gameObject.AddComponent<TorsoAbility>();
                Debug.Log("[BodyPartManager] Torso ability - Pendiente de implementación");
                break;
        }

        if (ability != null)
        {
            ability.Initialize(partData);
            Debug.Log($"[BodyPartManager] Habilidad activada: {partData.partType}");
        }
    }

    private void CreateVisualPart(BodyPartData partData)
    {
        if (prototypeSpherePrefab == null)
        {
            Debug.LogWarning("[BodyPartManager] No hay prefab de esfera asignado");
            return;
        }

        GameObject visualSphere = Instantiate(prototypeSpherePrefab, visualParent);
        visualSphere.name = $"Visual_{partData.partType}";

        Renderer renderer = visualSphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = partData.prototypeColor;
        }

        BodyPartVisual visualComponent = visualSphere.AddComponent<BodyPartVisual>();
        visualComponent.Initialize(partData, collectedParts.Count - 1);

        visualParts[partData.partType] = visualSphere;
    }

    public int GetCollectedPartsCount()
    {
        return collectedParts.Count;
    }

    public bool HasAllParts()
    {
        return collectedParts.Count == 5;
    }

    public void ResetParts()
    {
        foreach (var visual in visualParts.Values)
        {
            if (visual != null)
                Destroy(visual);
        }

        collectedParts.Clear();
        visualParts.Clear();

        Debug.Log("[BodyPartManager] Partes reseteadas");
    }

    private void OnGUI()
    {
        if (!showDebugGUI) return;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontStyle = FontStyle.Bold;

        float panelWidth = 250f;
        float panelHeight = 150f;
        float padding = 10f;

        GUILayout.BeginArea(new Rect(Screen.width - panelWidth - padding, padding, panelWidth, panelHeight), boxStyle);

        GUILayout.Label("=== BODY PARTS ===", labelStyle);
        GUILayout.Space(5);

        labelStyle.normal.textColor = HasAllParts() ? Color.green : Color.yellow;
        GUILayout.Label($"Collected: {collectedParts.Count} / 5", labelStyle);

        GUILayout.Space(5);
        labelStyle.fontSize = 12;

        foreach (BodyPartType type in System.Enum.GetValues(typeof(BodyPartType)))
        {
            labelStyle.normal.textColor = HasPart(type) ? Color.green : Color.gray;
            string icon = HasPart(type) ? "✓" : "✗";
            GUILayout.Label($"{icon} {type}", labelStyle);
        }

        GUILayout.EndArea();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}