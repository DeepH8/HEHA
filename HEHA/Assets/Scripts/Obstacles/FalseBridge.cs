using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    public class FalseBridge : MonoBehaviour
    {
        [SerializeField] float fallDelay = 0.35f;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Collider solidCollider;
        [SerializeField] Collider triggerZone;

        bool triggered;

        void Awake()
        {
            if (solidCollider == null)
                solidCollider = GetComponent<Collider>();

            if (solidCollider != null)
                solidCollider.isTrigger = false;

            if (triggerZone != null)
                triggerZone.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (triggered)
                return;

            if (!other.CompareTag("Player"))
                return;

            triggered = true;
            Invoke(nameof(DropBridge), fallDelay);
        }

        void DropBridge()
        {
            if (solidCollider != null)
                solidCollider.enabled = false;

            if (triggerZone != null)
                triggerZone.enabled = false;

            if (targetRenderer != null)
            {
                Color c = targetRenderer.material.color;
                c.a = 0.25f;
                targetRenderer.material.color = c;
            }
        }
    }
}
