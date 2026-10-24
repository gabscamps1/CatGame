using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using CatGame.Features.CharacterSelection;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform fruitThrowTransform;
    [SerializeField] private ThrowFruitController throwFruitController;
    [SerializeField] private PlayerFruitHand playerFruitHand;
    private BoxCollider2D boundary;

    private Bounds bounds;

    private float leftBound;
    private float rightBound;

    private float startingLeftBound;
    private float startingRightBound;

    private float offset;

    private IInputService inputService;
    private IPlayerInputController playerInputController;

    [SerializeField] private int playerIndex;
    private PlayerId playerId;

    private void Awake()
    {
        playerFruitHand = GetComponent<PlayerFruitHand>();
    }

    private void Start()
    {
        inputService = ServiceLocator.Get<IInputService>();
        playerInputController = inputService.GetInputFromPlayer(playerId);

        playerInputController.OnThrow += PlayerInputController_OnThrow;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;
        
        for (int i = 0; i < MatchSetup.Players.Count; i++)
        {
            if (MatchSetup.Players[i].OwnerId != OwnerClientId)
                continue;

            int playerIndex = i;

            if (GameManager.Instance.Boundaries.Length == 0 || playerIndex >= GameManager.Instance.Boundaries.Length)
                return;

            SetAreaToMoveRpc((byte)playerIndex);
        }
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
        Vector2 move = playerInputController.Move.ReadValue<Vector2>();

        float newPositionX = transform.position.x + (move.x * moveSpeed * Time.deltaTime);
        newPositionX = Mathf.Clamp(newPositionX, leftBound, rightBound);

        transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);
    }

    /// <summary>
    /// Altera o quanto o jogador pode se locomover para os lados.
    /// </summary>
    public void ChangeBoundary(float extraWidth)
    {
        return;
        leftBound = startingLeftBound;
        rightBound = startingRightBound;

        leftBound += throwFruitController.Bounds.extents.x + extraWidth;
        rightBound -= throwFruitController.Bounds.extents.x + extraWidth;
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
}
