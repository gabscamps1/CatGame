using CatGame.Core.Data;
using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    public static FruitSpawner Instance { get; private set; }

    [SerializeField] private FruitWorld prefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) 
            Instance = null;
    }

    /// <summary>
    /// Instancia as frutas em todos os clients
    /// </summary>
    public FruitWorld ServerSpawn(byte tier, in Vector2 position, in Vector2 velocity, in PlayerId playerOwner, uint sourceDrop)
    {
        FruitWorld fruit = Instantiate(prefab, position, Quaternion.identity);
        fruit.ServerInit(tier, playerOwner, sourceDrop);   // ANTES do Spawn
        fruit.NetworkObject.Spawn();
        fruit.ServerSetVelocity(velocity);
        return fruit;
    }

    /// <summary>
    /// Raio em mundo de uma fruta de um tier.
    /// </summary>
    public float GetWorldRadius(byte tier)
    {
        return prefab.RadiusForTier(tier);
    }
}
