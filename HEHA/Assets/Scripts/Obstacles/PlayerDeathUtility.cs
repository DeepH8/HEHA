using HEHA.Obby.Core;
using HEHA.Obby.Player;
using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    public static class PlayerDeathUtility
    {
        public static bool TryGetDeathHandler(Collider other, out PlayerDeathHandler handler)
        {
            handler = null;
            if (other == null || !other.CompareTag("Player"))
                return false;

            handler = other.GetComponent<PlayerDeathHandler>();
            if (handler == null)
                handler = other.GetComponentInParent<PlayerDeathHandler>();

            return handler != null;
        }

        public static void Kill(Collider other, DeathCause cause)
        {
            if (TryGetDeathHandler(other, out PlayerDeathHandler handler))
                handler.Die(cause);
        }
    }
}
