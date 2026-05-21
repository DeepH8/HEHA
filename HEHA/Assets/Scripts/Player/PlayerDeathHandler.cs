using System;
using HEHA.Obby.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HEHA.Obby.Player
{
    public class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] float respawnDelay = 1.75f;

        RobloxPlayerController movement;
        PlayerInput playerInput;
        RagdollDisassembler ragdoll;
        PlayerAnimationController animationController;
        CharacterController characterController;

        public bool IsDead { get; private set; }
        public event Action<DeathCause> OnDied;

        void Awake()
        {
            movement = GetComponent<RobloxPlayerController>();
            playerInput = GetComponent<PlayerInput>();
            ragdoll = GetComponent<RagdollDisassembler>();
            animationController = GetComponent<PlayerAnimationController>();
            characterController = GetComponent<CharacterController>();
        }

        public void Die(DeathCause cause)
        {
            if (IsDead)
                return;

            IsDead = true;
            GameAudioController.Instance?.PlayDeath();
            OnDied?.Invoke(cause);

            if (movement != null)
                movement.SetControlEnabled(false);

            if (playerInput != null)
                playerInput.enabled = false;

            if (characterController != null)
                characterController.enabled = false;

            animationController?.SetAnimatorEnabled(false);

            if (ragdoll != null)
                ragdoll.Explode();

            if (ObbySessionManager.Instance != null)
                ObbySessionManager.Instance.RequestRespawn(respawnDelay);
        }
    }
}
