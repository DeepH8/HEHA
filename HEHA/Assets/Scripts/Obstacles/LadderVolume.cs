using HEHA.Obby.Player;
using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    [RequireComponent(typeof(Collider))]
    public class LadderVolume : MonoBehaviour
    {
        [SerializeField] Vector3 climbDirection = Vector3.up;
        [SerializeField] Vector3 ladderForward = Vector3.forward;

        void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!PlayerDeathUtility.TryGetDeathHandler(other, out _))
                return;

            LadderClimbState climb = other.GetComponent<LadderClimbState>();
            if (climb == null)
                climb = other.GetComponentInParent<LadderClimbState>();

            if (climb != null)
                climb.EnterLadder(transform.TransformDirection(climbDirection.normalized),
                    transform.TransformDirection(ladderForward.normalized));
        }

        void OnTriggerExit(Collider other)
        {
            if (!PlayerDeathUtility.TryGetDeathHandler(other, out _))
                return;

            LadderClimbState climb = other.GetComponent<LadderClimbState>();
            if (climb == null)
                climb = other.GetComponentInParent<LadderClimbState>();

            if (climb != null)
                climb.ExitLadder();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 up = transform.TransformDirection(climbDirection.normalized);
            Gizmos.DrawLine(transform.position, transform.position + up * 2f);
        }
    }
}
