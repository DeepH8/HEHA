using HEHA.Obby.Player;
using UnityEngine;

namespace HEHA.Obby.Core
{
    public class ObbySessionManager : MonoBehaviour
    {
        public static ObbySessionManager Instance { get; private set; }

        [SerializeField] GameObject playerPrefab;
        [SerializeField] string playerPrefabResourcePath = "Assets/Prefabs/Player/PlayerR6.prefab";
        [SerializeField] Transform initialSpawn;
        [SerializeField] float respawnDelay = 1.75f;

        GameObject activePlayer;

        public GameObject ActivePlayer => activePlayer;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsurePlayerPrefab();
        }

        void EnsurePlayerPrefab()
        {
            if (playerPrefab != null)
                return;

#if UNITY_EDITOR
            playerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabResourcePath);
#endif
        }

        void Start()
        {
            SpawnPlayer(GetSpawnTransform());
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        Transform GetSpawnTransform()
        {
            if (CheckpointManager.Instance != null && CheckpointManager.Instance.CurrentSpawn != null)
                return CheckpointManager.Instance.CurrentSpawn;

            return initialSpawn;
        }

        public void RequestRespawn(float delay = -1f)
        {
            if (GameOutcomeManager.Instance != null && GameOutcomeManager.Instance.IsGameEnded)
                return;

            if (delay < 0f)
                delay = respawnDelay;

            CancelInvoke(nameof(RespawnPlayer));
            Invoke(nameof(RespawnPlayer), delay);
        }

        public void CancelPendingRespawn() => CancelInvoke(nameof(RespawnPlayer));

        public void RespawnPlayer()
        {
            if (activePlayer != null)
                Destroy(activePlayer);

            RagdollDisassembler.CleanupOrphanedParts();
            SpawnPlayer(GetSpawnTransform());
        }

        void SpawnPlayer(Transform spawn)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("ObbySessionManager: Player prefab is not assigned.");
                return;
            }

            Vector3 position = spawn != null ? spawn.position : Vector3.zero;
            Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;

            activePlayer = Instantiate(playerPrefab, position, rotation);
            activePlayer.tag = "Player";

            if (GameOutcomeManager.Instance != null)
            {
                PlayerDeathHandler deathHandler = activePlayer.GetComponent<PlayerDeathHandler>();
                if (deathHandler != null)
                    GameOutcomeManager.Instance.RegisterPlayer(deathHandler);
            }
        }
    }
}
