using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private CharacterView characterView;

    private Rigidbody2D rb;

    private Vector2 moveInput;

    public Vector2 MoveInput => moveInput;

    public FacingDirection CurrentDirection { get; private set; }
        = FacingDirection.Right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (characterView == null)
        {
            characterView = GetComponent<CharacterView>();
        }
    }

    private void Start()
    {
        characterView.SetFacingDirection(CurrentDirection);
    }

    private void Update()
    {
        UpdateDirection();

        characterView.SetMoveSpeed(moveInput.magnitude);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    private void UpdateDirection()
    {
        if (moveInput.x > 0.01f)
        {
            if (CurrentDirection != FacingDirection.Right)
            {
                CurrentDirection = FacingDirection.Right;
                characterView.SetFacingDirection(CurrentDirection);
            }
        }
        else if (moveInput.x < -0.01f)
        {
            if (CurrentDirection != FacingDirection.Left)
            {
                CurrentDirection = FacingDirection.Left;
                characterView.SetFacingDirection(CurrentDirection);
            }
        }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void StopMovement()
    {
        moveInput = Vector2.zero;

        rb.linearVelocity = Vector2.zero;

        characterView.SetMoveSpeed(0f);
    }
}