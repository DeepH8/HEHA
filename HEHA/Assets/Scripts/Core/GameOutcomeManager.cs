using System;
using HEHA.Obby.Player;
using HEHA.Obby.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HEHA.Obby.Core
{
    public class GameOutcomeManager : MonoBehaviour
    {
        public static GameOutcomeManager Instance { get; private set; }

        [SerializeField] int maxDeaths = 10;
        [SerializeField] WinLoseScreenUI resultScreen;

        int deathCount;
        bool gameEnded;
        PlayerDeathHandler activePlayer;

        public int DeathCount => deathCount;
        public int MaxDeaths => maxDeaths;
        public bool IsGameEnded => gameEnded;

        public event Action<int, int> DeathCountChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResetSession();
        }

        void OnDestroy()
        {
            UnregisterPlayer();
            if (Instance == this)
                Instance = null;
        }

        public void ResetSession()
        {
            deathCount = 0;
            gameEnded = false;
            resultScreen?.Hide();
            NotifyDeathCountChanged();
        }

        public void RegisterPlayer(PlayerDeathHandler handler)
        {
            UnregisterPlayer();
            activePlayer = handler;
            if (activePlayer != null)
                activePlayer.OnDied += HandlePlayerDied;
        }

        void UnregisterPlayer()
        {
            if (activePlayer != null)
                activePlayer.OnDied -= HandlePlayerDied;
            activePlayer = null;
        }

        void HandlePlayerDied(DeathCause cause)
        {
            if (gameEnded)
                return;

            deathCount++;
            NotifyDeathCountChanged();
            if (deathCount >= maxDeaths)
                EndGame(false);
        }

        void NotifyDeathCountChanged() => DeathCountChanged?.Invoke(deathCount, maxDeaths);

        public void OnFinishReached()
        {
            if (gameEnded)
                return;

            if (deathCount < maxDeaths)
                EndGame(true);
        }

        void EndGame(bool won)
        {
            gameEnded = true;
            ObbySessionManager.Instance?.CancelPendingRespawn();
            DisableActivePlayer();
            resultScreen?.Show(won, deathCount, maxDeaths);
        }

        static void DisableActivePlayer()
        {
            GameObject player = ObbySessionManager.Instance != null
                ? ObbySessionManager.Instance.ActivePlayer
                : null;
            if (player == null)
                return;

            if (player.TryGetComponent<RobloxPlayerController>(out RobloxPlayerController movement))
                movement.SetControlEnabled(false);

            if (player.TryGetComponent<PlayerInput>(out PlayerInput playerInput))
                playerInput.enabled = false;

            if (player.TryGetComponent<CharacterController>(out CharacterController controller))
                controller.enabled = false;
        }
    }
}
