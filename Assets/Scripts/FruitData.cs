using UnityEngine;

/// <summary>
/// ScriptableObject that defines one "tier" of fruit (e.g. Cherry, Strawberry, Watermelon...).
/// Create one asset per tier via: Assets > Create > Suika > Fruit Data
/// </summary>
[CreateAssetMenu(fileName = "FruitData", menuName = "Suika/Fruit Data")]
public class FruitData : ScriptableObject
{
    [Tooltip("0 = smallest fruit, increases toward the final fruit (e.g. watermelon)")]
    public int tier;

    public string fruitName;

    [Tooltip("Prefab with SpriteRenderer, Rigidbody2D, CircleCollider2D and the Fruit script")]
    public GameObject prefab;

    [Tooltip("Radius of the fruit's CircleCollider2D, used for spawn offset checks")]
    public float radius = 0.5f;

    [Tooltip("Score awarded when two of this tier merge")]
    public int scoreValue = 10;

    public Sprite icon;
}
