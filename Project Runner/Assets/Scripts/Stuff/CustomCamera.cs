using UnityEngine;

public class CustomCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private CameraConfigSO config;

    [Header("State (Read Only)")]
    [SerializeField] private float currentDistance;
    [SerializeField] private float pitch;
    [SerializeField] private float yaw;

    private Vector3 targetOffset = new Vector3(0, 1.5f, 0); // Ajustar al centro de masa visual
    private Vector3 rotationVelocity; // Para SmoothDamp
    private float targetDistance;
    private Vector3 currentRotationEuler;

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("CameraSystem: Falta asignar CameraConfigSO");
            enabled = false;
            return;
        }

        // Inicializar rotación basada en la actual
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        currentRotationEuler = angles;

        currentDistance = config.defaultDistance;
        targetDistance = currentDistance;

        // Opcional: Ocultar cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        HandleInput();
        MoveCamera();
    }

    private void HandleInput()
    {
        // Rotación
        float mouseX = Input.GetAxis("Mouse X") * config.mouseSensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * config.mouseSensitivityY * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, config.pitchLimits.x, config.pitchLimits.y);

        // Zoom
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= scroll * config.zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, config.zoomLimits.x, config.zoomLimits.y);
        }
    }

    private void MoveCamera()
    {
        // 1. Calcular Rotación Suavizada
        Vector3 targetEuler = new Vector3(pitch, yaw, 0);
        currentRotationEuler = Vector3.SmoothDamp(currentRotationEuler, targetEuler, ref rotationVelocity, config.rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(currentRotationEuler);

        // 2. Calcular Posición Ideal (Sin colisión)
        // Interpolamos la distancia para un zoom suave
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * config.zoomDamping);

        Vector3 targetPos = followTarget.position + targetOffset;
        Vector3 direction = rotation * Vector3.back;
        Vector3 desiredPosition = targetPos + (direction * currentDistance);

        // 3. Manejo de Colisiones (SphereCast desde el Target hacia la Cámara)
        if (CheckCollision(targetPos, desiredPosition, out Vector3 correctedPosition))
        {
            transform.position = correctedPosition;
        }
        else
        {
            transform.position = desiredPosition;
        }

        // 4. Mirar al target
        transform.LookAt(targetPos);
    }

    private bool CheckCollision(Vector3 from, Vector3 to, out Vector3 hitPosition)
    {
        hitPosition = to;

        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(from, to);

        // Usamos SphereCast para evitar que la cámara atraviese paredes finas o esquinas
        if (Physics.SphereCast(from, config.collisionRadius, direction, out RaycastHit hit, distance, config.collisionLayers))
        {
            // Si golpeamos algo, colocamos la cámara en el punto de impacto + offset de seguridad
            // Clamp para no acercar la cámara más allá de un mínimo incómodo
            float hitDist = Mathf.Max(hit.distance - config.collisionOffset, 0.5f);
            hitPosition = from + (direction * hitDist);
            return true;
        }

        return false;
    }
}