using HEHA.Obby.Core;
using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] int checkpointIndex;
        [SerializeField] Transform spawnPoint;
        [SerializeField] Renderer padRenderer;
        [SerializeField] Color inactiveColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] Color activeColor = new Color(0.2f, 1f, 0.4f);

        void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
            gameObject.tag = "Checkpoint";
        }

        void Awake()
        {
            if (spawnPoint == null)
                spawnPoint = transform;

            ApplyColor(inactiveColor);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (CheckpointManager.Instance == null)
                return;

            CheckpointManager.Instance.SetCheckpoint(checkpointIndex, spawnPoint);
            ApplyColor(activeColor);
        }

        void ApplyColor(Color color)
        {
            if (padRenderer != null && padRenderer.material != null)
                padRenderer.material.color = color;
        }
    }
}
