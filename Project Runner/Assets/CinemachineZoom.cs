using Unity.Cinemachine;
using UnityEngine;

public class CinemachineZoom : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Header("Configuración de Zoom")]
    [SerializeField] private string scrollInputName = "Mouse ScrollWheel";
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minRadius = 2f;
    [SerializeField] private float maxRadius = 10f;
    [SerializeField] private float zoomSmoothness = 5f;

    private CinemachineOrbitalFollow orbitalFollow;
    private float targetTopRadius;
    private float targetMiddleRadius;
    private float targetBottomRadius;

    private void Start()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
            if (cinemachineCamera == null)
            {
                Debug.LogError("GTACinemachineZoom: No se encontró CinemachineCamera!");
                enabled = false;
                return;
            }
        }

        // Obtener el componente OrbitalFollow
        orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbitalFollow == null)
        {
            Debug.LogError("GTACinemachineZoom: No se encontró CinemachineOrbitalFollow en la cámara!");
            enabled = false;
            return;
        }

        // Inicializar radios objetivo con valores actuales
        targetTopRadius = orbitalFollow.Orbits.Top.Radius;
        targetMiddleRadius = orbitalFollow.Orbits.Center.Radius;
        targetBottomRadius = orbitalFollow.Orbits.Bottom.Radius;
    }

    private void Update()
    {
        HandleZoomInput();
        ApplyZoomSmooth();
    }

    private void HandleZoomInput()
    {
        float scroll = Input.GetAxis(scrollInputName);

        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Calcular nuevo radio basado en el scroll
            float zoomAmount = scroll * zoomSpeed;

            targetTopRadius = Mathf.Clamp(targetTopRadius - zoomAmount, minRadius, maxRadius);
            targetMiddleRadius = Mathf.Clamp(targetMiddleRadius - zoomAmount, minRadius, maxRadius);
            targetBottomRadius = Mathf.Clamp(targetBottomRadius - zoomAmount, minRadius, maxRadius);
        }
    }

    private void ApplyZoomSmooth()
    {
        if (orbitalFollow == null) return;

        // Acceder a las órbitas
        var orbits = orbitalFollow.Orbits;

        // Aplicar zoom suavemente a cada órbita
        var topOrbit = orbits.Top;
        topOrbit.Radius = Mathf.Lerp(topOrbit.Radius, targetTopRadius, Time.deltaTime * zoomSmoothness);
        orbits.Top = topOrbit;

        var centerOrbit = orbits.Center;
        centerOrbit.Radius = Mathf.Lerp(centerOrbit.Radius, targetMiddleRadius, Time.deltaTime * zoomSmoothness);
        orbits.Center = centerOrbit;

        var bottomOrbit = orbits.Bottom;
        bottomOrbit.Radius = Mathf.Lerp(bottomOrbit.Radius, targetBottomRadius, Time.deltaTime * zoomSmoothness);
        orbits.Bottom = bottomOrbit;

        // Asignar las órbitas modificadas de vuelta
        orbitalFollow.Orbits = orbits;
    }

    // Método público para establecer el zoom programáticamente
    public void SetZoom(float radius)
    {
        radius = Mathf.Clamp(radius, minRadius, maxRadius);
        targetTopRadius = radius;
        targetMiddleRadius = radius;
        targetBottomRadius = radius;
    }

    // Método público para establecer los límites de zoom
    public void SetZoomLimits(float min, float max)
    {
        minRadius = min;
        maxRadius = max;

        // Clampear radios actuales
        targetTopRadius = Mathf.Clamp(targetTopRadius, minRadius, maxRadius);
        targetMiddleRadius = Mathf.Clamp(targetMiddleRadius, minRadius, maxRadius);
        targetBottomRadius = Mathf.Clamp(targetBottomRadius, minRadius, maxRadius);
    }

    // Obtener el radio actual (radio del centro)
    public float GetCurrentRadius()
    {
        if (orbitalFollow != null)
        {
            return orbitalFollow.Orbits.Center.Radius;
        }
        return 0f;
    }
}