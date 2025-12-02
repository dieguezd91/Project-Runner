using Unity.Cinemachine;
using UnityEngine;

public class CinemachineInputHandler : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Header("Configuración de Input")]
    [SerializeField] private string mouseXInputName = "Mouse X";
    [SerializeField] private string mouseYInputName = "Mouse Y";
    [SerializeField] private KeyCode rotateKey = KeyCode.Mouse1; // Click derecho
    [SerializeField] private bool alwaysRotate = false; // Si es true, rota siempre sin necesidad de mantener tecla

    [Header("Sensibilidad")]
    [SerializeField] private float horizontalSensitivity = 300f;
    [SerializeField] private float verticalSensitivity = 2f;

    [Header("Control de Cursor")]
    [SerializeField] private bool manageCursor = true;
    [SerializeField] private bool startWithLockedCursor = true;

    private CinemachineOrbitalFollow orbitalFollow;
    private bool isRotating = false;

    private void Start()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
            if (cinemachineCamera == null)
            {
                Debug.LogError("GTACinemachineInputHandler: No se encontró CinemachineCamera!");
                enabled = false;
                return;
            }
        }

        // Obtener el componente OrbitalFollow
        orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbitalFollow == null)
        {
            Debug.LogError("GTACinemachineInputHandler: No se encontró CinemachineOrbitalFollow en la cámara!");
            enabled = false;
            return;
        }

        // Configurar cursor inicial
        if (manageCursor && startWithLockedCursor)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        HandleCursorControl();
        HandleRotationInput();
    }

    private void HandleCursorControl()
    {
        if (!manageCursor) return;

        // ESC para desbloquear cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        // Click para volver a bloquear
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            LockCursor();
        }
    }

    private void HandleRotationInput()
    {
        bool shouldRotate = alwaysRotate || Input.GetKey(rotateKey);

        if (shouldRotate && orbitalFollow != null)
        {
            float mouseX = Input.GetAxis(mouseXInputName);
            float mouseY = Input.GetAxis(mouseYInputName);

            if (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f)
            {
                // Actualizar la rotación orbital directamente
                // En Cinemachine 3.x, OrbitStyle es un struct, hay que trabajar con sus campos internos

                // Horizontal (alrededor del objetivo)
                orbitalFollow.HorizontalAxis.Value += mouseX * horizontalSensitivity * Time.deltaTime;

                // Vertical (arriba/abajo)
                orbitalFollow.VerticalAxis.Value += mouseY * verticalSensitivity * Time.deltaTime;
                orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
                    orbitalFollow.VerticalAxis.Value,
                    orbitalFollow.VerticalAxis.Range.x,
                    orbitalFollow.VerticalAxis.Range.y
                );
            }

            isRotating = true;
        }
        else
        {
            isRotating = false;
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Métodos públicos para control externo
    public void SetAlwaysRotate(bool value)
    {
        alwaysRotate = value;
    }

    public void SetSensitivity(float horizontal, float vertical)
    {
        horizontalSensitivity = horizontal;
        verticalSensitivity = vertical;
    }

    public CinemachineCamera GetCinemachineCamera()
    {
        return cinemachineCamera;
    }

    public CinemachineOrbitalFollow GetOrbitalFollow()
    {
        return orbitalFollow;
    }
}