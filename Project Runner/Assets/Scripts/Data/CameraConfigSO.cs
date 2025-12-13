using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "Game/Camera Config")]
public class CameraConfigSO : ScriptableObject
{
    [Header("Orbit Settings")]
    public float mouseSensitivityX = 150f;
    public float mouseSensitivityY = 150f;
    public float rotationSmoothTime = 0.1f;
    public Vector2 pitchLimits = new Vector2(-40, 85);

    [Header("Zoom Settings")]
    public float defaultDistance = 6.0f;
    public Vector2 zoomLimits = new Vector2(2.0f, 12.0f);
    public float zoomSpeed = 4.0f;
    public float zoomDamping = 5.0f;

    [Header("Collision")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.2f;
    public float collisionOffset = 0.2f;
}