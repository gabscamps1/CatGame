using UnityEngine;

namespace CatGame.Capabilities.Movement
{
    public class CharacterMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        private float leftBound;
        private float rightBound;

        private bool hasMovementBounds;

        public float MoveSpeed => moveSpeed;

        /// <summary>
        /// Altera a velocidade.
        /// </summary>
        public void SetMoveSpeed(float newMoveSpeed)
        {
            moveSpeed = Mathf.Max(0f, newMoveSpeed);
        }

        /// <summary>
        /// Define a área onde o personagem pode se mover.
        /// </summary>
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

        /// <summary>
        /// Move o personagem no eixo X.
        /// </summary>
        public void Move(Vector2 direction, float deltaTime)
        {
            if (!hasMovementBounds)
                return;

            float newPositionX = transform.position.x + direction.x * moveSpeed * deltaTime;
            newPositionX = Mathf.Clamp(newPositionX, leftBound, rightBound);

            transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);
        }
    }

}
