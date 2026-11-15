using CatGame.Core.Data;
using CatGame.Core.Events;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CatGame.Core;
using CatGame.Core.Interfaces;

public class FruitWorld : NetworkBehaviour
{
    /// <summary>
    /// Informações do merge entre as frutas.
    /// </summary>
    /*public class ServerMergedArgs : EventArgs
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
    }*/

    public PlayerId PlayerOwner => IsSpawned 
        ? playerOwner.Value 
        : PlayerId.None;
   
    // Frutas existentes neste peer. A IA (que roda só no server) filtra por PlayerOwner.
    private static readonly List<FruitWorld> active = new();
    public static IReadOnlyList<FruitWorld> Active => active;

    // Informações sobre a fruta (valores de física só fazem sentido no server).
    public byte Tier => tier.Value;
    public bool HasLanded => hasLanded; // Já encostou no chão ou em outra fruta. Antes disso, ainda está caindo.
    public bool IsMerging => isMerging;
    public Vector2 Position => body.position;
    public Vector2 Velocity => body.linearVelocity;
    public float WorldRadius => fruitCollider.radius * Mathf.Abs(transform.lossyScale.x);

    public float TimeInCollision { get; private set; }
    private bool stayInCollision = false;

    [SerializeField] private FruitCatalog catalog;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private CircleCollider2D fruitCollider;
    [SerializeField] private float maxAirTime = 5f; // Se nunca colidir, devolve a fruta à mão do jogador.

    // Escritas só pelo server, ANTES do Spawn: chegam junto com a mensagem de spawn.
    private readonly NetworkVariable<byte> tier = new();
    private readonly NetworkVariable<PlayerId> playerOwner = new();
    private readonly NetworkVariable<uint> sourceDrop = new(); // 0 = fruta de merge. 1 ou mais = número do drop da mão.

    private bool hasLanded; // Colidiu com um objeto.
    private bool isMerging; // Evita que ocorra um merge com 3 ou mais frutas.
    private float spawnTime;

    [Header("Sounds")]
    [SerializeField] private AudioData MATCH_THROW;


    private IAudioService audioService;
    private IAudioService AudioService => audioService ??= ServiceLocator.Get<IAudioService>();

    private IPlayerRegistry playerRegistry;
    private IPlayerRegistry PlayerRegistry => playerRegistry ??= ServiceLocator.Get<IPlayerRegistry>();

    // Veio da mão de um jogador (e não de um merge).
    private bool IsDroppedByHand => sourceDrop.Value != 0;


    //public static event EventHandler<ServerMergedArgs> OnFruitMergedServer;    

    private void Start()
    {
        audioService = ServiceLocator.Get<IAudioService>();
    }

    public void ServerInit(byte newTier, PlayerId playerOwner, uint drop)
    {
        tier.Value = newTier;
        this.playerOwner.Value = playerOwner;
        sourceDrop.Value = drop;
    }

    public void ServerSetVelocity(Vector2 velocity)
    {
        if (IsServer) body.linearVelocity = velocity;
    }

    /// <summary>
    /// Raio em mundo que uma fruta teria em um tier.
    /// </summary>
    public float RadiusForTier(byte tierToCheck)
    {
        float baseRadius = fruitCollider.radius;
        return baseRadius * catalog.Get(tierToCheck).diameter;
    }

    public override void OnNetworkSpawn()
    {
        if (!active.Contains(this)) active.Add(this);

        FruitCatalog.TierData data = catalog.Get(tier.Value);
        spriteRenderer.sprite = data.sprite;
        transform.localScale = Vector3.one * data.diameter;
        
        if (IsServer)
        {
            body.mass = data.mass;
            GameManager.Instance.OnPlayerLost += GameManager_OnPlayerLost;
        }

        fruitCollider.enabled = IsServer; // Colisão só é ativada no server
        spawnTime = Time.time;

        // Avisa ao player dono dessa fruta, que esta fruta já existe aqui.
        if (IsDroppedByHand)
        {
            PlayerFruitHand hand = FindHand();
            if (hand != null) hand.NotifyWorldFruitSpawned(sourceDrop.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Se for destruida, garante que o jogador seja avisado.
        if (IsServer) NotifyLanded();
        active.Remove(this);
        GameManager.Instance.OnPlayerLost -= GameManager_OnPlayerLost;
    }

    public override void OnDestroy()
    {
        active.Remove(this);
        base.OnDestroy();
    }

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned || hasLanded || !IsDroppedByHand) return;
        if (Time.time - spawnTime > maxAirTime) NotifyLanded();
    }

    private void Update()
    {
        IncressTimer();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer || !IsSpawned || isMerging) return;

        if (!collision.collider.TryGetComponent(out FruitCollider fruitCollider))
            return;

        // O player ganha a próxima fruta (antes de qualquer merge)
        NotifyLanded();   

        if (!collision.collider.TryGetComponent(out FruitWorld other)) return;
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
        
        // Cada pote pertence a um jogador: os pontos vão para o dono do pote.
        PlayerId scorer = playerOwner.Value;

        other.NetworkObject.Despawn();
        NetworkObject.Despawn();

        FruitSpawner.Instance.ServerSpawn(newTier, point, velocity, scorer, 0);
        //OnFruitMergedServer?.Invoke(this, new ServerMergedArgs(scorer, newTier, point));

        // Score ao dar merge.
        int score = catalog.Get(newTier).score;

        if (scorer.IsValid)
            ScoreManager.Instance.IncreaseScore(scorer, score);
    }

    /// <summary>
    /// Avisa ao player para se Reabastecer com a próxima fruta.
    /// </summary>
    private void NotifyLanded()
    {
        if (hasLanded || !IsDroppedByHand) return;
        hasLanded = true;

        PlayerFruitHand hand = FindHand();
        if (hand != null) hand.ServerRefill(sourceDrop.Value);
    }

    /// <summary>
    /// Acha a mão do dono desta fruta pelo PlayerId (humano ou IA).
    /// </summary>
    private PlayerFruitHand FindHand()
    {
        return PlayerRegistry.TryGetActor(playerOwner.Value, out IPlayerActor actor) ? actor.Hand : null;
    }

    #region Trigger

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.TryGetComponent(out GlassMaxHeight glass))
            return;

        stayInCollision = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.TryGetComponent(out GlassMaxHeight glass))
            return;

        stayInCollision = false;
        ResetTimer();
    }

    private void IncressTimer()
    {
        if (!stayInCollision)
            return;

        TimeInCollision += Time.deltaTime;
    }

    public void ResetTimer()
    {
        TimeInCollision = 0;
    }

    #endregion


    private void GameManager_OnPlayerLost(PlayerId playerId)
    {
        if (playerId != playerOwner.Value)
            return;

        NetworkObject.Despawn();
    }

}
