using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Rigidbody rb;
    public PlayerLocomotion locomotion;

    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int jumpHash = Animator.StringToHash("Jump");

    private void Update()
    {
        UpdateAnimationParameters();
        DetectJumpTrigger();
    }

    private void UpdateAnimationParameters()
    {
        float horizontalSpeed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
        animator.SetFloat(speedHash, horizontalSpeed);
        animator.SetBool(isGroundedHash, locomotion.IsGrounded());
    }

    private void DetectJumpTrigger()
    {
        if (locomotion.WasGrounded() && !locomotion.IsGrounded() && rb.linearVelocity.y > 0.1f)
        {
            animator.SetTrigger(jumpHash);
        }
    }
}