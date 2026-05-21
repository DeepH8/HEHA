using UnityEngine;

namespace HEHA.Obby.Player
{
    public class LadderClimbState : MonoBehaviour
    {
        public bool IsClimbing { get; private set; }
        public Vector3 ClimbDirection { get; private set; } = Vector3.up;
        public Vector3 LadderRight { get; private set; } = Vector3.right;

        [SerializeField] float climbSpeed = 8f;
        [SerializeField] float strafeSpeed = 4f;

        public float ClimbSpeed => climbSpeed;
        public float StrafeSpeed => strafeSpeed;

        public void EnterLadder(Vector3 up, Vector3 forward)
        {
            IsClimbing = true;
            ClimbDirection = up.normalized;
            LadderRight = Vector3.Cross(ClimbDirection, forward).normalized;
            if (LadderRight.sqrMagnitude < 0.01f)
                LadderRight = Vector3.right;
        }

        public void ExitLadder()
        {
            IsClimbing = false;
        }
    }
}
