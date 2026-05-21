using UnityEngine;

namespace HEHA.Obby.Core
{
    public class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        [SerializeField] Transform defaultSpawn;

        Transform currentSpawn;
        int lastCheckpointIndex = -1;

        public Transform CurrentSpawn => currentSpawn != null ? currentSpawn : defaultSpawn;
        public int LastCheckpointIndex => lastCheckpointIndex;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            currentSpawn = defaultSpawn;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetCheckpoint(int index, Transform spawn)
        {
            if (spawn == null)
                return;

            lastCheckpointIndex = index;
            currentSpawn = spawn;
        }

        public void ResetProgress()
        {
            lastCheckpointIndex = -1;
            currentSpawn = defaultSpawn;
        }
    }
}
