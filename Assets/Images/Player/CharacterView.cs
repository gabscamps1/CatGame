using UnityEngine;

public class CharacterView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private static readonly int HitHash =
        Animator.StringToHash("Hit");

    public void SetFacingDirection(FacingDirection direction)
    {
        if (direction == FacingDirection.Left)
        {
            SetFlip(true);
        }
        else if (direction == FacingDirection.Right)
        {
            SetFlip(false);
        }
    }

    public void SetMoveSpeed(float speed)
    {
        //animator.SetFloat(SpeedHash, speed);
    }

    public void PlayAttack()
    {
        //animator.SetTrigger(AttackHash);
    }

    public void PlayHit()
    {
        //animator.SetTrigger(HitHash);
    }

    private void SetFlip(bool flip)
    {
        Vector3 scale = transform.localScale;

        scale.x = flip ? -1f : 1f;

        transform.localScale = scale;
    }
}