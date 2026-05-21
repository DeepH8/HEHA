using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    public class DisappearingFloorStepDetector : MonoBehaviour
    {
        [SerializeField] DisappearingFloor floor;

        public void Bind(DisappearingFloor target)
        {
            if (target != null)
                floor = target;
        }

        void OnTriggerEnter(Collider other) => floor?.NotifyPlayerStepped(other);

        void OnTriggerStay(Collider other) => floor?.NotifyPlayerStepped(other);
    }
}
