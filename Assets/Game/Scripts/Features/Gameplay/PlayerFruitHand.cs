using CatGame.Core.Data;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using CatGame.Features.CharacterSelection;
using System;
using Unity.Netcode;
using UnityEngine;

public struct HandState : INetworkSerializable, IEquatable<HandState>
{
    public byte tier; // Tier da fruta atual que está na mão do jogador. 0 = mão vazia
    public byte nextFruitTier; // Tier da próxima fruta que vai para a mão do jogador. 0 = mão vazia
    public uint dropCount; // Quantas frutas já saíram desta mão.

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref tier);
        serializer.SerializeValue(ref nextFruitTier);
        serializer.SerializeValue(ref dropCount);
    }

    public bool Equals(HandState other) => tier == other.tier && nextFruitTier == other.nextFruitTier &&  dropCount == other.dropCount;
}

public class PlayerFruitHand : NetworkBehaviour, IPlayerIdConfig
{
    [SerializeField] private FruitCatalog catalog;
    [SerializeField] private SpriteRenderer heldRenderer; // Visual da fruta. 
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float arenaMinX = -4f;
    [SerializeField] private float arenaMaxX = 4f;
    
    [SerializeField] private float maxDriftX = 1.5f; // Tolerância entre a posição X do dono e o do server.
    [SerializeField] private float dropCooldown = 0.15f; // Cooldown minimo para o drop.

    private readonly NetworkVariable<HandState> state = new();

    private uint spawnedDropCount;
    private byte lastShownTier;
    private float lastDropTime;

    private PlayerId playerId = PlayerId.None;
    public override void OnNetworkSpawn()
    {
        state.OnValueChanged += OnStateChanged;

        if (IsServer)
        {
            state.Value = new HandState
            {
                tier = catalog.RandomStartTier(),
                nextFruitTier = catalog.RandomStartTier(),
                dropCount = 0
            };            
        }          

        Refresh();
    }

    public override void OnNetworkDespawn()
    {
        state.OnValueChanged -= OnStateChanged;
    }

    private void OnStateChanged(HandState previous, HandState current)
    {
        Refresh();
    }

    /// <summary>
    /// Avisa para o player quando uma fruta é criada localmente.
    /// </summary>
    public void NotifyWorldFruitSpawned(uint sourceDrop)
    {
        if (sourceDrop > spawnedDropCount) spawnedDropCount = sourceDrop;
        Refresh();
    }

    private void Refresh()
    {
        HandState currentHandState = state.Value;
        bool show;
        byte tierToShow = 0;

        if (playerId.IsValid)
        {
            EventBus.Publish(new NextFruitChangedEvent()
            {
                PlayerIndex = playerId.Id,
                Sprite = catalog.Get(currentHandState.nextFruitTier).sprite,
            });
        }     

        if (spawnedDropCount > currentHandState.dropCount)
        {
            show = false; // O spawn chegou antes da variável
        }
        else if (currentHandState.tier != 0)
        {
            show = true; // Estado normal: fruta na mão
            tierToShow = currentHandState.tier;
        }
        else if (spawnedDropCount < currentHandState.dropCount)
        {
            show = lastShownTier != 0; // Variável chegou antes do spawn: segura
            tierToShow = lastShownTier;
        }
        else // Quando a mão está vazia e a fruta do mundo já foi dropada
        {
            show = false; 
        }

        heldRenderer.enabled = show;
        if (!show) return;

        lastShownTier = tierToShow;
        heldRenderer.sprite = catalog.Get(tierToShow).sprite;
    }

    /// <summary>
    /// Requisição para o client dropar a fruta.
    /// </summary>
    public void RequestDrop()
    {
        if (!IsOwner || !IsSpawned) return;
        DropRpc(dropPoint.position.x);
    }

    /// <summary>
    /// Valida e decide se vai dropar a fruta.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void DropRpc(float requestedX)
    {
        HandState handState = state.Value;

        if (handState.tier == 0) return; // Mão vazia
        if (Time.time - lastDropTime < dropCooldown) return;
        if (!float.IsFinite(requestedX)) return;

        lastDropTime = Time.time;

        // A posição X que o dono do player está é considerada para o drop, somado à tolerância em torno da posição X que esse player está no server.
        // Quando o lag está alto, o erro máximo da posição de spawn da fruta é o maxDriftX.
        float serverX = dropPoint.position.x;
        float x = Mathf.Clamp(requestedX, serverX - maxDriftX, serverX + maxDriftX);
        x = Mathf.Clamp(x, arenaMinX, arenaMaxX);

        uint drop = handState.dropCount + 1;
        FruitSpawner.Instance.ServerSpawn(handState.tier, new Vector2(x, dropPoint.position.y), Vector2.zero, OwnerClientId, drop);

        state.Value = new HandState 
        { 
            tier = 0, 
            nextFruitTier = state.Value.nextFruitTier, 
            dropCount = drop 
        };
    }

    /// <summary>
    /// Reabastece o jogador com a próxima fruta.
    /// </summary>
    public void ServerRefill(uint sourceDrop)
    {
        if (!IsServer) return;

        HandState current = state.Value;
        if (current.tier != 0 || current.dropCount != sourceDrop) return;   // refil duplicado ou atrasado

        state.Value = new HandState 
        { 
            tier = state.Value.nextFruitTier,
            nextFruitTier = catalog.RandomStartTier(),
            dropCount = sourceDrop 
        };
    }

    public void SetPlayerId(PlayerId playerId)
    {
        this.playerId = playerId;
    }
}
