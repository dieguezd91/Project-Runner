using UnityEngine;
using Unity.Cinemachine; // Necesario para acceder a CinemachineCamera

public class CameraOrbitControl : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float sensitivityX = 200f;
    [SerializeField] private float sensitivityY = 150f;
    [SerializeField] private float yMinLimit = -20f; // No mirar muy abajo
    [SerializeField] private float yMaxLimit = 80f;  // No mirar muy arriba

    // State
    private float _yaw;
    private float _pitch;

    private void Start()
    {
        // Inicializar rotación con la actual
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;

        //// Ocultar cursor
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    private void LateUpdate() // LateUpdate para ir después del movimiento del player
    {
        // Leer Input (Mouse o Stick Derecho)
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Calcular rotación
        _yaw += mouseX * sensitivityX * Time.deltaTime;
        _pitch -= mouseY * sensitivityY * Time.deltaTime;

        // Limitar ángulo vertical (Clamp)
        _pitch = Mathf.Clamp(_pitch, yMinLimit, yMaxLimit);

        // Aplicar rotación al objeto cámara
        // Cinemachine ThirdPersonFollow leerá esta rotación y ajustará la posición automáticamente
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}