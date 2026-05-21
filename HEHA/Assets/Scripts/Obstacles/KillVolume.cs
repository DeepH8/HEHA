using HEHA.Obby.Core;
using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    [RequireComponent(typeof(Collider))]
    public class KillVolume : MonoBehaviour
    {
        [SerializeField] DeathCause cause = DeathCause.Fall;

        void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other) => PlayerDeathUtility.Kill(other, cause);
    }
}
