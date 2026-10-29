using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using Unity.Netcode;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IPlayerIdConfig
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform fruitThrowTransform;
    [SerializeField] private PlayerFruitHand playerFruitHand;
    private BoxCollider2D boundary;

    private Bounds bounds;

    private float leftBound;
    private float rightBound;

    private float startingLeftBound;
    private float startingRightBound;

    private float offset;

    private IInputService inputService;
    private IPlayerRegistry playerRegistry;
    private IPlayerInputController playerInputController;

    private NetworkVariable<PlayerId> playerId = new();
    private PlayerId previewPlayerId = PlayerId.None;

    private void Awake()
    {
        playerFruitHand = GetComponent<PlayerFruitHand>();
        
        inputService = ServiceLocator.Get<IInputService>();
        playerRegistry = ServiceLocator.Get<IPlayerRegistry>();
    }

    private void Start()
    {
        if (playerRegistry.TryGetPlayerInfo(playerId.Value, out PlayerInfo playerInfo))
        {
            PlayerId playerInputId = new PlayerId(playerInfo.localIndex);
            playerInputController = inputService.GetInputFromPlayer(playerInputId);
            playerInputController.OnThrow += PlayerInputController_OnThrow;
        }
        else
        {
            CatGame.Core.Logger.LogError($"Não foi encontrado o player. PlayerId: {playerId.Value}");
        }
    }

    public override void OnNetworkPreDespawn()
    {
        playerRegistry.UnregisterController(playerId.Value, this);

        base.OnNetworkPreDespawn();
    }

    public override void OnNetworkSpawn()
    {
        playerId.OnValueChanged += Init;

        if (IsServer)
        {
            playerId.Value = previewPlayerId;
            

            if (GameManager.Instance.Boundaries.Length == 0 || playerId.Value.Id >= GameManager.Instance.Boundaries.Length)
                return;

            SetAreaToMoveRpc((byte)playerId.Value.Id);
        }

        if (IsClient)
        {
            SetPlayerId(playerId.Value);
        }

        playerRegistry.RegisterController(playerId.Value, this);
    }

    public override void OnNetworkDespawn()
    {
        playerId.OnValueChanged -= Init;
    }

    private void Init(PlayerId previous, PlayerId current)
    {
        if (previous == current) return;
        playerFruitHand.SetPlayerId(current);
    }

    private void OnDisable()
    {
        playerInputController.OnThrow -= PlayerInputController_OnThrow;
    }

    private void PlayerInputController_OnThrow()
    {
        playerFruitHand.RequestDrop();
    }

    private void Update()
    {
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        if (GameManager.Instance.GetGameState() != GameManager.GameState.Playing)
            return;

        Vector2 move = playerInputController.Move.ReadValue<Vector2>();

        float newPositionX = transform.position.x + (move.x * moveSpeed * Time.deltaTime);
        newPositionX = Mathf.Clamp(newPositionX, leftBound, rightBound);

        transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void SetAreaToMoveRpc(byte playerId)
    {
        BoxCollider2D newBoundary = GameManager.Instance.Boundaries[playerId];
        
        if (newBoundary == null)
            return;

        boundary = newBoundary;

        bounds = boundary.bounds;

        offset = transform.position.x - fruitThrowTransform.position.x;

        leftBound = bounds.min.x + offset;
        rightBound = bounds.max.x + offset;

        startingLeftBound = leftBound;
        startingRightBound = rightBound;
    }

    public void SetPlayerId(PlayerId playerId)
    {
        previewPlayerId = playerId;
        playerFruitHand.SetPlayerId(playerId);
    }
}
