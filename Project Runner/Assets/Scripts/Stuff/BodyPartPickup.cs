using NUnit.Framework;
using UnityEngine;
using static UnityEditor.MaterialProperty;

public class BodyPartPickup : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private BodyPartData partData;

    [Header("Visual")]
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;

    private Vector3 startPosition;
    private MeshRenderer meshRenderer;

    public BodyPartType PartType => partData != null ? partData.partType : BodyPartType.Legs;

    private void Start()
    {
        startPosition = transform.position;

        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && partData != null)
        {
            // Crear material instanciado para evitar modificar el material compartido
            meshRenderer.material = new Material(meshRenderer.material);
            meshRenderer.material.color = partData.prototypeColor;
        }
    }

    public void SetPartData(BodyPartData data)
    {
        partData = data;

        if (meshRenderer != null && partData != null)
        {
            meshRenderer.material = new Material(meshRenderer.material);
            meshRenderer.material.color = partData.prototypeColor;
        }
    }

    private void Update()
    {
        // Rotación
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Bobbing (movimiento vertical sutil)
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (BodyPartManager.Instance != null)
            {
                bool collected = BodyPartManager.Instance.CollectPart(partData);
                if (collected)
                {
                    // TODO: Aquí puedes añadir feedback visual/sonoro
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogWarning("[BodyPartPickup] No se encontró BodyPartManager.Instance");
            }
        }
    }
}