using HEHA.Obby.Core;
using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    [RequireComponent(typeof(Collider))]
    public class FinishGoal : MonoBehaviour
    {
        void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!PlayerDeathUtility.TryGetDeathHandler(other, out _))
                return;

            if (GameOutcomeManager.Instance == null)
            {
                Debug.LogWarning("FinishGoal: GameOutcomeManager not found in scene.");
                return;
            }

            GameOutcomeManager.Instance.OnFinishReached();
        }
    }
}
