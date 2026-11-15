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

    [Header("Settings")]
    [SerializeField] private bool isAI;

    private CharacterMovement characterMovement;
    private HealthController healthController;
    private PlayerFruitHand playerFruitHand;
    private IPlayerRegistry playerRegistry;

    private readonly NetworkVariable<PlayerId> playerId = new();
    private PlayerId previewPlayerId = PlayerId.None;
    private bool isReady;

    public event Action<PlayerId> PlayerIdChanged;

    public bool IsAI => isAI;
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

            ConfigureMovementAreaRpc((byte)playerId.Value.Id);
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

        if (IsServer)
            ConfigureMovementAreaRpc((byte)current.Id);
    }

    private void ApplyPlayerId(PlayerId current)
    {
        // Atualiza a identificação da mão de frutas.
        playerFruitHand.SetPlayerId(current);

        if (current.IsValid)
            playerRegistry.RegisterActor(current, this);

        PlayerIdChanged?.Invoke(current);
    }

    public void SetPlayerId(PlayerId newPlayerId)
    {
        previewPlayerId = newPlayerId;

        if (IsServer && IsSpawned)
        {
            playerId.Value = newPlayerId;
        }
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void ConfigureMovementAreaRpc(byte playerIndex)
    {
        if (!GlassArea.TryGet(playerIndex, out GlassArea glassArea) || glassArea.MovementBounds == null)
        {
            CatGame.Core.Logger.LogError($"Pote (GlassArea) não encontrado para o jogador {playerIndex}");
            return;
        }

        characterMovement.SetMovementBounds(glassArea.MovementBounds, fruitThrowTransform);
    }
}
