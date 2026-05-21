using UnityEngine;
using UnityEngine.InputSystem;

namespace HEHA.Obby.Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class RobloxCameraController : MonoBehaviour
    {
        [SerializeField] Transform yawPivot;
        [SerializeField] Transform pitchPivot;
        [SerializeField] Camera playerCamera;
        [SerializeField] float mouseSensitivity = 2f;
        [SerializeField] float minPitch = -80f;
        [SerializeField] float maxPitch = 80f;
        [SerializeField] Vector3 cameraOffset = new Vector3(0f, 1.5f, -8f);
        [SerializeField] float zoomDistance = 8f;
        [SerializeField] float minZoom = 3f;
        [SerializeField] float maxZoom = 24f;
        [SerializeField] float zoomStep = 1.25f;
        [SerializeField] float collisionRadius = 0.25f;
        [SerializeField] float minDistance = 1.5f;
        [SerializeField] LayerMask collisionMask = ~0;

        PlayerInput playerInput;
        InputSystem_Actions inputActions;
        RobloxPlayerController playerController;
        float pitch;
        bool cursorLocked;

        public bool IsCursorLocked => cursorLocked;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            playerController = GetComponent<RobloxPlayerController>();
            inputActions = new InputSystem_Actions(playerInput.actions);

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

        }

        void OnEnable()
        {
            inputActions.Enable();
            zoomDistance = Mathf.Abs(cameraOffset.z) > 0.01f ? Mathf.Abs(cameraOffset.z) : zoomDistance;
            SetCursorLocked(true);
        }

        void OnDisable()
        {
            SetCursorLocked(false);
        }

        void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            if (playerController != null && !playerController.IsControlEnabled)
                return;

            HandleCursorLockToggle();
            HandleZoom();

            if (cursorLocked)
            {
                Vector2 look = inputActions.Player.Look.ReadValue<Vector2>();
                float yawDelta = look.x * mouseSensitivity;
                float pitchDelta = -look.y * mouseSensitivity;

                Transform yawTarget = yawPivot != null ? yawPivot : transform;
                yawTarget.Rotate(0f, yawDelta, 0f, Space.World);

                pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
                if (pitchPivot != null)
                    pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            UpdateCameraPosition();
        }

        void HandleCursorLockToggle()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !cursorLocked)
                SetCursorLocked(true);
        }

        void HandleZoom()
        {
            if (Mouse.current == null)
                return;

            float scrollY = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) < 0.01f)
                return;

            int steps = Mathf.RoundToInt(scrollY / 120f);
            if (steps == 0)
                steps = scrollY > 0f ? 1 : -1;

            zoomDistance = Mathf.Clamp(zoomDistance - steps * zoomStep, minZoom, maxZoom);
        }

        void UpdateCameraPosition()
        {
            if (playerCamera == null || pitchPivot == null)
                return;

            Vector3 offset = cameraOffset;
            offset.z = -zoomDistance;
            Vector3 desired = pitchPivot.TransformPoint(offset);
            Vector3 pivotPos = pitchPivot.position;
            Vector3 dir = desired - pivotPos;
            float distance = dir.magnitude;

            if (distance > 0.01f && Physics.SphereCast(pivotPos, collisionRadius, dir.normalized,
                    out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                desired = pivotPos + dir.normalized * Mathf.Max(hit.distance - collisionRadius, minDistance);
            }

            playerCamera.transform.position = desired;
            playerCamera.transform.LookAt(pitchPivot.position + Vector3.up * 0.5f);
        }

        public float GetCameraYaw()
        {
            Transform yawSource = yawPivot != null ? yawPivot : transform;
            return yawSource.eulerAngles.y;
        }
    }
}
