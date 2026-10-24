using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    public static FruitSpawner Instance { get; private set; }

    [SerializeField] private FruitWorld prefab;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }

    private void OnDestroy()
    {
        if (Instance == this) 
            Instance = null;
    }

    /// <summary>
    /// Instancia as frutas em todos os clients
    /// </summary>
    public FruitWorld ServerSpawn(byte tier, Vector2 position, Vector2 velocity, ulong droppedByClientId, uint sourceDrop)
    {
        FruitWorld fruit = Instantiate(prefab, position, Quaternion.identity);
        fruit.ServerInit(tier, droppedByClientId, sourceDrop);   // ANTES do Spawn
        fruit.NetworkObject.Spawn();
        fruit.ServerSetVelocity(velocity);
        return fruit;
    }
}
