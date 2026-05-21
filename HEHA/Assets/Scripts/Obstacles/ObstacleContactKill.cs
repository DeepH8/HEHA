using HEHA.Obby.Core;
using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    public class ObstacleContactKill : MonoBehaviour
    {
        [SerializeField] DeathCause cause = DeathCause.Obstacle;
        [SerializeField] bool useTrigger = true;

        Collider col;

        void Awake()
        {
            col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = useTrigger;
        }

        void OnTriggerEnter(Collider other)
        {
            if (useTrigger)
                PlayerDeathUtility.Kill(other, cause);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!useTrigger)
                PlayerDeathUtility.Kill(collision.collider, cause);
        }
    }
}
