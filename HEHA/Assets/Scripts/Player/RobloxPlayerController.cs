using HEHA.Obby.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HEHA.Obby.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class RobloxPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float walkSpeed = 13f;
        [SerializeField] float sprintSpeed = 20f;
        [SerializeField] float acceleration = 80f;
        [SerializeField] float jumpHeight = 3.5f;
        [SerializeField] float gravity = -50f;
        [SerializeField] float groundCheckDistance = 0.15f;

        [Header("Rotation")]
        [SerializeField] float turnSpeed = 18f;
        [SerializeField] Transform bodyPivot;

        CharacterController controller;
        RobloxCameraController cameraController;
        PlayerInput playerInput;
        InputSystem_Actions inputActions;
        LadderClimbState ladderState;
        PlayerDeathHandler deathHandler;

        Vector3 velocity;
        bool isSprinting;

        public bool IsControlEnabled { get; private set; } = true;
        public float BodyYaw => bodyPivot != null ? bodyPivot.eulerAngles.y : transform.eulerAngles.y;
        public float HorizontalSpeed => new Vector3(velocity.x, 0f, velocity.z).magnitude;
        public float VerticalSpeed => velocity.y;
        public bool IsGrounded => IsGroundedCheck();
        public bool IsSprinting => isSprinting;

        PlayerAnimationController animationController;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();
            ladderState = GetComponent<LadderClimbState>();
            deathHandler = GetComponent<PlayerDeathHandler>();
            animationController = GetComponent<PlayerAnimationController>();
            cameraController = GetComponent<RobloxCameraController>();
            inputActions = new InputSystem_Actions(playerInput.actions);

            if (bodyPivot == null)
            {
                Transform visualRoot = transform.Find("VisualRoot");
                if (visualRoot != null)
                    bodyPivot = visualRoot;
            }
        }

        void OnEnable() => inputActions.Enable();

        void OnDisable() => inputActions.Disable();

        void Update()
        {
            if (!IsControlEnabled || (deathHandler != null && deathHandler.IsDead))
                return;

            bool grounded = IsGroundedCheck();
            if (grounded && velocity.y < 0f)
                velocity.y = -2f;

            Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            isSprinting = inputActions.Player.Sprint.IsPressed();
            float speed = isSprinting ? sprintSpeed : walkSpeed;

            if (ladderState != null && ladderState.IsClimbing)
            {
                UpdateLadderMovement(moveInput, speed);
            }
            else
            {
                UpdateGroundAirMovement(moveInput, speed, grounded);
            }

            if (inputActions.Player.Jump.WasPressedThisFrame())
            {
                if (grounded)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    animationController?.TriggerJump();
                    GameAudioController.Instance?.PlayJump();
                }
                else if (ladderState != null && ladderState.IsClimbing)
                {
                    ladderState.ExitLadder();
                }
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        void UpdateGroundAirMovement(Vector2 moveInput, float speed, bool grounded)
        {
            Vector3 camForward = GetCameraForward();
            Vector3 camRight = Vector3.Cross(Vector3.up, camForward).normalized;

            Vector3 wishDir = camForward * moveInput.y + camRight * moveInput.x;
            if (wishDir.sqrMagnitude > 1f)
                wishDir.Normalize();

            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 target = wishDir * speed;
            horizontal = Vector3.MoveTowards(horizontal, target, acceleration * Time.deltaTime);
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            if (wishDir.sqrMagnitude > 0.01f)
                RotateToward(wishDir);
        }

        void UpdateLadderMovement(Vector2 moveInput, float speed)
        {
            Vector3 climb = ladderState.ClimbDirection * moveInput.y * ladderState.ClimbSpeed;
            Vector3 strafe = ladderState.LadderRight * moveInput.x * ladderState.StrafeSpeed;
            velocity = climb + strafe;
            velocity.y = 0f;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 face = climb.sqrMagnitude > 0.01f ? climb : strafe;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f)
                    RotateToward(face.normalized);
            }
        }

        void RotateToward(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                return;

            Transform rotateTarget = bodyPivot != null ? bodyPivot : transform;
            Quaternion target = Quaternion.LookRotation(direction);
            rotateTarget.rotation = Quaternion.Slerp(rotateTarget.rotation, target, turnSpeed * Time.deltaTime);
        }

        Vector3 GetCameraForward()
        {
            float yaw = cameraController != null ? cameraController.GetCameraYaw() : BodyYaw;
            return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        }

        bool IsGroundedCheck()
        {
            if (controller.isGrounded)
                return true;

            Vector3 origin = transform.position + Vector3.up * 0.1f;
            return Physics.SphereCast(origin, controller.radius * 0.9f, Vector3.down,
                out _, groundCheckDistance + 0.1f, ~0, QueryTriggerInteraction.Ignore);
        }

        public void SetControlEnabled(bool enabled) => IsControlEnabled = enabled;
    }
}
