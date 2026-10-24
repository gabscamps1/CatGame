using System;
using Unity.Netcode;
using UnityEngine;

public sealed class FruitWorld : NetworkBehaviour
{
    /// <summary>
    /// Informações do merge entre as frutas.
    /// </summary>
    public class ServerMergedArgs : EventArgs
    {
        public ulong PlayerId { get; private set; }
        public byte FruitTier { get; private set; }
        public Vector2 MergePosition { get; private set; }

        public ServerMergedArgs(ulong playerId, byte fruitTier, Vector2 mergePosition)
        {
            PlayerId = playerId;
            FruitTier = fruitTier;
            MergePosition = mergePosition;
        }
    }

    public const ulong NoPlayer = ulong.MaxValue;

    [SerializeField] private FruitCatalog catalog;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D fruitCollider;
    [SerializeField] private float maxAirTime = 5f;   // Se nunca colidir, devolve a fruta à mão do jogador.

    // Escritas só pelo server, ANTES do Spawn: chegam junto com a mensagem de spawn.
    private readonly NetworkVariable<byte> tier = new();
    private readonly NetworkVariable<ulong> droppedByClientId = new(NoPlayer);
    private readonly NetworkVariable<uint> sourceDrop = new();

    private bool hasLanded; // Colidiu com um objeto.
    private bool isMerging; // Evita que ocorra um merge com 3 ou mais frutas.
    private float spawnTime;

    public static event EventHandler<ServerMergedArgs> OnFruitMergedServer;    

    public void ServerInit(byte newTier, ulong droppedBy, uint drop)
    {
        tier.Value = newTier;
        droppedByClientId.Value = droppedBy;
        sourceDrop.Value = drop;
    }

    public void ServerSetVelocity(Vector2 velocity)
    {
        if (IsServer) body.linearVelocity = velocity;
    }

    public override void OnNetworkSpawn()
    {
        FruitCatalog.TierData data = catalog.Get(tier.Value);
        spriteRenderer.sprite = data.sprite;
        transform.localScale = Vector3.one * data.diameter;
        
        if (IsServer) 
            body.mass = data.mass;

        fruitCollider.enabled = IsServer; // Colisão só é ativada no server
        spawnTime = Time.time;

        // Avisa ao player dono dessa fruta, que esta fruta já existe aqui.
        if (droppedByClientId.Value != NoPlayer)
        {
            PlayerFruitHand hand = FindHand();
            if (hand != null) hand.NotifyWorldFruitSpawned(sourceDrop.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Se for destruida, garante que o jogador seja avisado.
        if (IsServer) NotifyLanded(); 
    }

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned || hasLanded || droppedByClientId.Value == NoPlayer) return;
        if (Time.time - spawnTime > maxAirTime) NotifyLanded();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer || !IsSpawned || isMerging) return;

        // O player ganha a próxima fruta (antes de qualquer merge)
        NotifyLanded();   

        FruitWorld other = collision.collider.GetComponentInParent<FruitWorld>();
        if (other == null || other == this) return;
        if (other.isMerging || !other.IsSpawned) return;
        if (other.tier.Value != tier.Value) return;
        if (tier.Value >= catalog.MaxTier) return;
        if (NetworkObjectId > other.NetworkObjectId) return; // Só o de menor id faz o merge.

        FruitMerge(other);
    }

    private void FruitMerge(FruitWorld other)
    {
        isMerging = true;
        other.isMerging = true;

        Vector2 point = (body.position + other.body.position) * 0.5f;
        Vector2 velocity = (body.linearVelocity + other.body.linearVelocity) * 0.5f;
        byte newTier = (byte)(tier.Value + 1);
        ulong credited = droppedByClientId.Value != NoPlayer ? droppedByClientId.Value : other.droppedByClientId.Value;

        other.NetworkObject.Despawn();
        NetworkObject.Despawn();

        FruitSpawner.Instance.ServerSpawn(newTier, point, velocity, NoPlayer, 0);
        OnFruitMergedServer?.Invoke(this, new ServerMergedArgs(credited, newTier, point));
    }

    /// <summary>
    /// Avisa ao player para se Reabastecer com a próxima fruta.
    /// </summary>
    private void NotifyLanded()
    {
        if (hasLanded || droppedByClientId.Value == NoPlayer) return;
        hasLanded = true;

        PlayerFruitHand hand = FindHand();
        if (hand != null) hand.ServerRefill(sourceDrop.Value);
    }

    private PlayerFruitHand FindHand()
    {
        if (NetworkManager == null || NetworkManager.SpawnManager == null) return null;

        NetworkObject playerObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(droppedByClientId.Value);
        return playerObject != null ? playerObject.GetComponent<PlayerFruitHand>() : null;
    }
}
