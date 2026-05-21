using HEHA.Obby.Obstacles;
using UnityEngine;

namespace HEHA.Obby.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerObstacleContact : MonoBehaviour
    {
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider == null)
                return;

            DisappearingFloor floor = hit.collider.GetComponent<DisappearingFloor>();
            if (floor == null)
                floor = hit.collider.GetComponentInParent<DisappearingFloor>();

            if (floor != null)
                floor.ActivateFromPlayer();
        }
    }
}
