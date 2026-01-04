using UnityEngine;

public class BodyPartVisual : MonoBehaviour
{
    private BodyPartData partData;
    private float currentAngle;
    private float angleOffset;
    private Transform parentTransform;

    public void Initialize(BodyPartData data, int partIndex)
    {
        partData = data;
        parentTransform = transform.parent;

        // Distribución uniforme en círculo basada en el índice de recolección
        angleOffset = partIndex * (360f / 5f);
        currentAngle = angleOffset;
    }

    private void Update()
    {
        if (partData == null || parentTransform == null)
            return;

        currentAngle += partData.orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f)
            currentAngle -= 360f;

        float radians = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * partData.orbitRadius,
            Mathf.Sin(radians * 2f) * 0.3f, // Órbita vertical sutil
            Mathf.Sin(radians) * partData.orbitRadius
        );

        transform.position = parentTransform.position + offset;

        // Rotación propia de la esfera
        transform.Rotate(Vector3.up, partData.orbitSpeed * Time.deltaTime);
    }
}