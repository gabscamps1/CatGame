using CatGame.Capabilities.Movement;
using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controller do jogador: lê o input e controla o movimento e o drop.
/// </summary>
[RequireComponent(typeof(PlayerActor))]
public class PlayerController : NetworkBehaviour
{
    private IPlayerActor actor;
    private PlayerInputHandler inputHandler;
    private CharacterMovement characterMovement;
    private PlayerFruitHand playerFruitHand;

    private IInputService inputService;
    private IPlayerRegistry playerRegistry;

    private void Awake()
    {
        actor = GetComponent<IPlayerActor>();
        characterMovement = GetComponent<CharacterMovement>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerFruitHand = GetComponent<PlayerFruitHand>();

        inputService = ServiceLocator.Get<IInputService>();
        playerRegistry = ServiceLocator.Get<IPlayerRegistry>();
    }

    #region Unity Network

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        actor.PlayerIdChanged += OnPlayerIdChanged;
    }

    public override void OnNetworkDespawn()
    {
        actor.PlayerIdChanged -= OnPlayerIdChanged;

        inputHandler.OnThrow -= HandleThrow;
        inputHandler.Dispose();

        base.OnNetworkDespawn();
    }

    #endregion

    private void OnPlayerIdChanged(PlayerId current)
    {
        // O PlayerId mudou: o input precisa ser religado ao jogador local certo.
        inputHandler.OnThrow -= HandleThrow;
        inputHandler.Dispose();

        InitializeInput(current);
    }

    private void InitializeInput(PlayerId current)
    {
        // Configura o input somente no Owner.
        if (!IsOwner)
            return;

        bool initialized = inputHandler.Initialize(current, inputService, playerRegistry);

        if (initialized)
        {
            inputHandler.OnThrow -= HandleThrow;
            inputHandler.OnThrow += HandleThrow;
        }
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || !actor.IsReady)
            return;

        // O PlayerActor pode ter aplicado o PlayerId antes deste componente nascer na rede.
        if (!inputHandler.IsInitialized)
            InitializeInput(actor.PlayerId);

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
}
