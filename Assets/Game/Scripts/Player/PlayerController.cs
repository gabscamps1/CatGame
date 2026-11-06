using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using Unity.Netcode;
using UnityEngine;
using CatGame.Capabilities.Movement;
using CatGame.Capabilities.Health;

public class PlayerController : NetworkBehaviour, IPlayerIdConfig
{
    [Header("References")]
    [SerializeField] private Transform fruitThrowTransform;

    private PlayerInputHandler inputHandler;
    private CharacterMovement characterMovement;
    private HealthController healthController;
    private PlayerFruitHand playerFruitHand;

    private IInputService inputService;
    private IPlayerRegistry playerRegistry;

    private readonly NetworkVariable<PlayerId> playerId = new();
    private PlayerId previewPlayerId = PlayerId.None;

    private void Awake()
    {
        characterMovement = GetComponent<CharacterMovement>();
        healthController = GetComponent<HealthController>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerFruitHand = GetComponent<PlayerFruitHand>();

        inputService = ServiceLocator.Get<IInputService>();
        playerRegistry = ServiceLocator.Get<IPlayerRegistry>();

        healthController.OnDead += HealthController_OnDead;
    }
  
    public override void OnDestroy()
    {
        healthController.OnDead -= HealthController_OnDead;
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

        playerId.OnValueChanged += OnPlayerIdChanged;

        if (IsServer)
        {
            playerId.Value = previewPlayerId;

            ConfigureMovementAreaRpc((byte)playerId.Value.Id);
        }

        InitializePlayer(playerId.Value);

        playerRegistry.RegisterController(playerId.Value, this);
    }

    public override void OnNetworkPreDespawn()
    {
        playerRegistry.UnregisterController(playerId.Value, this);

        base.OnNetworkPreDespawn();
    }

    public override void OnNetworkDespawn()
    {
        playerId.OnValueChanged -= OnPlayerIdChanged;

        inputHandler.OnThrow -= HandleThrow;
        inputHandler.Dispose();

        base.OnNetworkDespawn();
    }

    #endregion



    private void OnPlayerIdChanged(PlayerId previous, PlayerId current)
    {
        if (previous == current)
            return;

        playerRegistry.UnregisterController(previous, this);

        InitializePlayer(current);

        playerRegistry.RegisterController(current, this);

        if (IsServer)
            ConfigureMovementAreaRpc((byte)current.Id);
    }

    private void InitializePlayer(PlayerId current)
    {
        // Atualiza a identificação da mão de frutas.
        playerFruitHand.SetPlayerId(current);

        // Configura o input somente no Owner.
        if (!IsOwner)
            return;

        inputHandler.OnThrow -= HandleThrow;
        inputHandler.Dispose();

        bool initialized = inputHandler.Initialize(current, inputService, playerRegistry);

        if (initialized)
            inputHandler.OnThrow += HandleThrow;
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (!inputHandler.IsInitialized)
            InitializePlayer(playerId.Value);

        if (GameManager.Instance.GetGameState() != GameManager.GameState.Playing)     
            return;
        
        Vector2 move = inputHandler.ReadMove();
        characterMovement.Move(move, Time.deltaTime);
    }

    private void HandleThrow()
    {
        if (!IsOwner || !IsSpawned)
            return;

        playerFruitHand.RequestDrop();
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void ConfigureMovementAreaRpc(byte playerIndex)
    {
        BoxCollider2D[] boundaries = GameManager.Instance.Boundaries;

        int index = playerIndex;

        if (boundaries == null || index < 0 || index >= boundaries.Length)
        {
            CatGame.Core.Logger.LogError($"Limite não encontrado para o jogador {index}");
            return;
        }

        BoxCollider2D boundary = boundaries[index];

        if (boundary == null)
            return;

        characterMovement.SetMovementBounds(boundary, fruitThrowTransform);
    }

    public void SetPlayerId(PlayerId newPlayerId)
    {
        previewPlayerId = newPlayerId;

        if (IsServer && IsSpawned)
        {
            playerId.Value = newPlayerId;
        }
    }


    
}
