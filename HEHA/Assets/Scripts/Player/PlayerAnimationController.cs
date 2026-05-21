using UnityEngine;

namespace HEHA.Obby.Player
{
    [RequireComponent(typeof(RobloxPlayerController))]
    public class PlayerAnimationController : MonoBehaviour
    {
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        static readonly int JumpHash = Animator.StringToHash("Jump");
        static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");

        RobloxPlayerController movement;
        PlayerDeathHandler deathHandler;
        Animator animator;

        void Awake()
        {
            movement = GetComponent<RobloxPlayerController>();
            deathHandler = GetComponent<PlayerDeathHandler>();
            animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            if (animator == null || movement == null || !movement.IsControlEnabled)
                return;

            if (deathHandler != null && deathHandler.IsDead)
                return;

            animator.SetFloat(SpeedHash, movement.HorizontalSpeed);
            animator.SetBool(IsGroundedHash, movement.IsGrounded);
            animator.SetBool(IsSprintingHash, movement.IsSprinting);
            animator.SetFloat(VerticalSpeedHash, movement.VerticalSpeed);
        }

        public void TriggerJump()
        {
            if (animator != null)
                animator.SetTrigger(JumpHash);
        }

        public void SetAnimatorEnabled(bool enabled)
        {
            if (animator != null)
                animator.enabled = enabled;
        }
    }
}
