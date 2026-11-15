using CatGame.Capabilities.Health;
using CatGame.Capabilities.Movement;
using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Identidade de um participante da partida (jogador ou IA)
/// </summary>
[DisallowMultipleComponent]
public class PlayerActor : NetworkBehaviour, IPlayerActor
{
    [Header("References")]
    [SerializeField] private Transform fruitThrowTransform;

    private CharacterMovement characterMovement;
    private HealthController healthController;
    private PlayerFruitHand playerFruitHand;
    private IPlayerRegistry playerRegistry;

    private readonly NetworkVariable<PlayerId> playerId = new();
    private PlayerId previewPlayerId = PlayerId.None;
    private bool isReady;

    public event Action<PlayerId> PlayerIdChanged;

    public bool IsReady => isReady;
    public PlayerId PlayerId => isReady ? playerId.Value : previewPlayerId;
    public IDamageable Damageable => healthController;
    public PlayerFruitHand Hand => playerFruitHand;

    private void Awake()
    {
        characterMovement = GetComponent<CharacterMovement>();
        healthController = GetComponent<HealthController>();
        playerFruitHand = GetComponent<PlayerFruitHand>();

        playerRegistry = ServiceLocator.Get<IPlayerRegistry>();

        healthController.OnDead += HealthController_OnDead;
    }

    public override void OnDestroy()
    {
        if (healthController != null)
            healthController.OnDead -= HealthController_OnDead;

        base.OnDestroy();
    }

    private void HealthController_OnDead()
    {
        // Anuncia que o jogador morreu.
        if (IsSpawned && IsServer)
            GameManager.Instance.PlayerGameOver(playerId.Value);

        Debug.Log("Player perdeu");
    }

    #region Unity Network

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playerId.OnValueChanged += OnPlayerIdValueChanged;

        if (IsServer)
        {
            playerId.Value = previewPlayerId;
        }

        isReady = true;
        ApplyPlayerId(playerId.Value);
    }

    public override void OnNetworkPreDespawn()
    {
        playerRegistry.UnregisterActor(playerId.Value, this);

        base.OnNetworkPreDespawn();
    }

    public override void OnNetworkDespawn()
    {
        playerId.OnValueChanged -= OnPlayerIdValueChanged;
        isReady = false;

        base.OnNetworkDespawn();
    }

    #endregion

    private void OnPlayerIdValueChanged(PlayerId previous, PlayerId current)
    {
        if (previous == current)
            return;

        playerRegistry.UnregisterActor(previous, this);

        ApplyPlayerId(current);
    }

    /// <summary>
    /// Aplica o ID em todos os clients.
    /// </summary>
    private void ApplyPlayerId(PlayerId current)
    {
        // Atualiza a identificação da mão de frutas.
        playerFruitHand.SetPlayerId(current);

        if (current.IsValid)
        {
            playerRegistry.RegisterActor(current, this);

            ConfigureMovementArea(current);
        }

        PlayerIdChanged?.Invoke(current);
    }

    /// <summary>
    /// Define o ID desse participant no server.
    /// </summary>
    public void SetPlayerId(PlayerId newPlayerId)
    {
        previewPlayerId = newPlayerId;

        if (IsServer && IsSpawned)
        {
            playerId.Value = newPlayerId;
        }
    }

    /// <summary>
    /// Define a área de movimento do jogador pelo pote do seu PlayerId.
    /// </summary>
    private void ConfigureMovementArea(PlayerId current)
    {
        if (!GlassArea.TryGet(current.Id, out GlassArea glassArea) || glassArea.MovementBounds == null)
        {
            CatGame.Core.Logger.LogError($"Pote (GlassArea) não encontrado para o jogador {current.Id}");
            return;
        }

        characterMovement.SetMovementBounds(glassArea.MovementBounds, fruitThrowTransform);
    }
}
