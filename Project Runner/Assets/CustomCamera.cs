using UnityEngine;

public class CustomCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0); // Apuntar al pecho/cabeza

    [Header("Configuración Orbital")]
    [SerializeField] private float mouseSensitivity = 3.0f;
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [SerializeField] private Vector2 pitchLimits = new Vector2(-40, 85); // Cuánto puedes mirar arriba/abajo

    [Header("Configuración Zoom")]
    [SerializeField] private float currentDistance = 6.0f;
    [SerializeField] private Vector2 zoomLimits = new Vector2(2.0f, 12.0f);
    [SerializeField] private float zoomSpeed = 2.0f;
    [SerializeField] private float zoomDamping = 5.0f;

    [Header("Colisión (Evitar Paredes)")]
    [SerializeField] private LayerMask collisionLayers; // Asigna "Default", "Ground", etc. (NO Player)
    [SerializeField] private float collisionRadius = 0.2f; // Radio de la esfera de colisión de la cámara
    [SerializeField] private float collisionOffset = 0.2f; // Pequeño margen para que no atraviese la pared

    // Variables internas de estado
    private Vector3 _currentRotation;
    private Vector3 _rotationVelocity; // Para el SmoothDamp
    private float _yaw, _pitch;
    private float _targetDistance;

    private void Start()
    {
        // Inicializar ángulos basados en la rotación actual o default
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;
        _currentRotation = angles;
        _targetDistance = currentDistance;
    }

    private void LateUpdate() // LateUpdate es CRÍTICO para seguir objetos que se mueven en Update
    {
        if (!target) return;

        HandleInput();
        CalculatePosition();
    }

    private void HandleInput()
    {
        // 1. Rotación Orbital (Mouse)
        _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, pitchLimits.x, pitchLimits.y);

        // 2. Zoom (Scroll) - Usamos targetDistance para suavizarlo después
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _targetDistance -= scroll * zoomSpeed;
            _targetDistance = Mathf.Clamp(_targetDistance, zoomLimits.x, zoomLimits.y);
        }
    }

    private void CalculatePosition()
    {
        // A. Suavizado de Rotación (SmoothDamp para sensación "premium")
        Vector3 targetRotationEuler = new Vector3(_pitch, _yaw, 0);
        _currentRotation = Vector3.SmoothDamp(_currentRotation, targetRotationEuler, ref _rotationVelocity, rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(_currentRotation);

        // B. Suavizado de Zoom
        currentDistance = Mathf.Lerp(currentDistance, _targetDistance, Time.deltaTime * zoomDamping);

        // C. Calcular posición deseada (Sin colisión aún)
        // La fórmula mágica: Posición = Target + (Rotación * Vector Hacia Atrás * Distancia)
        Vector3 finalTargetPos = target.position + targetOffset;
        Vector3 desiredPosition = finalTargetPos - (rotation * Vector3.forward * currentDistance);

        // D. Sistema de Colisión (Anti-Clipping)
        // Lanzamos un Rayo/Esfera desde el jugador hacia la cámara
        RaycastHit hit;
        Vector3 directionToCamera = (desiredPosition - finalTargetPos).normalized;
        float distToCamera = Vector3.Distance(finalTargetPos, desiredPosition);

        if (Physics.SphereCast(finalTargetPos, collisionRadius, directionToCamera, out hit, distToCamera, collisionLayers))
        {
            // Si chocamos con una pared, ponemos la cámara justo en el punto de impacto (menos un offset)
            float hitDistance = hit.distance - collisionOffset;
            // No permitir que se acerque más del mínimo zoom
            hitDistance = Mathf.Max(hitDistance, zoomLimits.x);
            desiredPosition = finalTargetPos + (directionToCamera * hitDistance);
        }

        // E. Aplicar Transformaciones
        transform.position = desiredPosition;
        transform.LookAt(finalTargetPos);
    }
}