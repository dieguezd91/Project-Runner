using UnityEngine;

public class CustomCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private CameraConfigSO config;
    [SerializeField] private InputReader inputReader;

    [Header("Auto-Center Settings")]
    [SerializeField] private float idleTimeBeforeCenter = 2.0f; // Segundos de inactividad antes de centrar
    [SerializeField] private float centerSpeed = 3.0f; // Velocidad de rotación del centrado

    [Header("State (Read Only)")]
    [SerializeField] private float currentDistance;
    [SerializeField] private float pitch;
    [SerializeField] private float yaw;

    private Vector3 targetOffset = new Vector3(0, 1.5f, 0);
    private Vector3 rotationVelocity;
    private float targetDistance;
    private Vector3 currentRotationEuler;

    // Control de tiempo para el auto-centrado
    private float lastInputTime;

    private void Start()
    {
        if (config == null || inputReader == null)
        {
            Debug.LogError("CustomCamera: Referencias faltantes.");
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        currentRotationEuler = angles;

        currentDistance = config.defaultDistance;
        targetDistance = currentDistance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        HandleInput();
        ApplyAutoCenter(); // Nueva lógica de centrado
        MoveCamera();
    }

    private void HandleInput()
    {
        Vector2 lookInput = inputReader.LookInput;

        // Si hay movimiento en el input, actualizamos el tiempo de última actividad
        if (lookInput.sqrMagnitude > 0.01f)
        {
            lastInputTime = Time.time;

            yaw += lookInput.x * config.mouseSensitivityX * Time.deltaTime;
            pitch -= lookInput.y * config.mouseSensitivityY * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, config.pitchLimits.x, config.pitchLimits.y);
        }

        // Zoom (Scroll)
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= scroll * config.zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, config.zoomLimits.x, config.zoomLimits.y);
        }
    }

    private void ApplyAutoCenter()
    {
        // Solo centramos si ha pasado el tiempo de inactividad y el jugador se está moviendo
        // Usamos la rotación del target (jugador) como referencia trasera
        if (Time.time - lastInputTime > idleTimeBeforeCenter)
        {
            float targetYaw = followTarget.eulerAngles.y;

            // Interpolación esférica del ángulo para evitar saltos y elegir el camino más corto
            yaw = Mathf.LerpAngle(yaw, targetYaw, centerSpeed * Time.deltaTime);

            // Opcional: Centrar también el pitch a una altura por defecto (ej. 15 grados)
            pitch = Mathf.LerpAngle(pitch, 15f, centerSpeed * Time.deltaTime);
        }
    }

    private void MoveCamera()
    {
        Vector3 targetEuler = new Vector3(pitch, yaw, 0);
        currentRotationEuler = Vector3.SmoothDamp(currentRotationEuler, targetEuler, ref rotationVelocity, config.rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(currentRotationEuler);

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * config.zoomDamping);

        Vector3 targetPos = followTarget.position + targetOffset;
        Vector3 direction = rotation * Vector3.back;
        Vector3 desiredPosition = targetPos + (direction * currentDistance);

        if (CheckCollision(targetPos, desiredPosition, out Vector3 correctedPosition))
        {
            transform.position = correctedPosition;
        }
        else
        {
            transform.position = desiredPosition;
        }

        transform.LookAt(targetPos);
    }

    private bool CheckCollision(Vector3 from, Vector3 to, out Vector3 hitPosition)
    {
        hitPosition = to;
        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(from, to);

        if (Physics.SphereCast(from, config.collisionRadius, direction, out RaycastHit hit, distance, config.collisionLayers))
        {
            float hitDist = Mathf.Max(hit.distance - config.collisionOffset, 0.5f);
            hitPosition = from + (direction * hitDist);
            return true;
        }
        return false;
    }
}