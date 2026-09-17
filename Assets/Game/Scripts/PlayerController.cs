using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private BoxCollider2D boundaries;
    [SerializeField] private Transform fruitThrowTransform;
    [SerializeField] private ThrowFruitController throwFruitController;

    private Bounds bounds;

    private float leftBound;
    private float rightBound;

    private float startingLeftBound;
    private float startingRightBound;

    private float offset;

    private IInputService inputService;
    private IPlayerInputController playerInputController;
    private PlayerId playerId;

    private void Awake()
    {
        bounds = boundaries.bounds;

        offset = transform.position.x - fruitThrowTransform.position.x;

        leftBound = bounds.min.x + offset;
        rightBound = bounds.max.x + offset;

        startingLeftBound = leftBound;
        startingRightBound = rightBound;
    }

    private void Start()
    {
        inputService = ServiceLocator.Get<IInputService>();
        playerInputController = inputService.GetInputFromPlayer(playerId);

        playerInputController.OnThrow += PlayerInputController_OnThrow;
    }

    private void OnDisable()
    {
        playerInputController.OnThrow -= PlayerInputController_OnThrow;
    }

    private void PlayerInputController_OnThrow()
    {
        throwFruitController.ThrowFruit();
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
        leftBound = startingLeftBound;
        rightBound = startingRightBound;

        leftBound += throwFruitController.Bounds.extents.x + extraWidth;
        rightBound -= throwFruitController.Bounds.extents.x + extraWidth;
    }

}
