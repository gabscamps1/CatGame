using UnityEngine;

public class FruitInfo : MonoBehaviour
{
    public int FruitIndex = 0;
    public int PointsWhenAnnihilated = 1;
    public float FruitMass = 1f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.mass = FruitMass;
    }
}