using UnityEngine;

/// <summary>
/// Attach to every fruit prefab. Detects collisions with same-tier fruit and
/// hands the merge off to GameManager. Requires Rigidbody2D + CircleCollider2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Fruit : MonoBehaviour
{
    public int tier;

    [HideInInspector] public bool hasMerged = false;   // prevents double-merging in one frame
    [HideInInspector] public bool isDropped = false;    // false while still "aiming" at the top

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDropped || hasMerged) return;

        Fruit other = collision.gameObject.GetComponent<Fruit>();
        if (other == null || other.tier != tier || other.hasMerged || !other.isDropped) return;

        // Only the "lower" instance ID triggers the merge so it isn't processed twice.
        if (GetInstanceID() < other.GetInstanceID())
        {
            GameManager.Instance.MergeFruits(this, other, collision.GetContact(0).point);
        }
    }

    public void MarkDropped()
    {
        isDropped = true;
    }
}
