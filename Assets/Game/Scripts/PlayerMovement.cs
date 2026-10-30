using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private float leftBound;
    private float rightBound;

    private bool hasMovementBounds;

    public void SetMovementBounds(BoxCollider2D boundary, Transform fruitThrowTransform)
    {
        if (boundary == null || fruitThrowTransform == null)
            return;

        Bounds bounds = boundary.bounds;

        float offset = transform.position.x - fruitThrowTransform.position.x;

        leftBound = bounds.min.x + offset;
        rightBound = bounds.max.x + offset;

        hasMovementBounds = true;
    }

    public void Move(Vector2 direction, float deltaTime)
    {
        if (!hasMovementBounds)
            return;

        float newPositionX = transform.position.x + direction.x * moveSpeed * deltaTime;
        newPositionX = Mathf.Clamp(newPositionX, leftBound, rightBound);

        transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);
    }
}
