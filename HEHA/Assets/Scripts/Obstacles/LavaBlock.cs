using HEHA.Obby.Core;
using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    [RequireComponent(typeof(Collider))]
    public class LavaBlock : MonoBehaviour
    {
        void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
            gameObject.tag = "Lava";
        }

        void OnTriggerEnter(Collider other) => PlayerDeathUtility.Kill(other, DeathCause.Lava);

        void OnCollisionEnter(Collision collision) =>
            PlayerDeathUtility.Kill(collision.collider, DeathCause.Lava);
    }
}
