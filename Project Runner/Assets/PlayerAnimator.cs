using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private PlayerLocomotion locomotion; // Para acceder a _isGrounded si es público, o usa una propiedad

    // Animator Hashes (Más rápido que usar strings en Update)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelHash = Animator.StringToHash("VerticalVelocity");

    private void Update()
    {
        // 1. Velocidad Horizontal (Para Blend Tree de correr/idle)
        // Usamos la magnitud horizontal para saber si corre
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        animator.SetFloat(SpeedHash, flatVel.magnitude, 0.15f, Time.deltaTime);
        // 2. Estado de Suelo (Para transiciones Jump/Land)
        // Necesitas exponer una propiedad pública 'IsGrounded' en tu PlayerLocomotion
        animator.SetBool(IsGroundedHash, locomotion.IsGrounded);

        // 3. Velocidad Vertical (Para saber si está subiendo o cayendo)
        animator.SetFloat(VerticalVelHash, rb.linearVelocity.y);
    }
}