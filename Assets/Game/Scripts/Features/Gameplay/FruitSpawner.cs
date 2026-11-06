using CatGame.Core.Data;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CatGame.Core;
using CatGame.Core.Interfaces;


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
    public FruitWorld ServerSpawn(byte tier, Vector2 position, Vector2 velocity, ulong droppedByClientId, PlayerId playerOwner, uint sourceDrop)
    {
        FruitWorld fruit = Instantiate(prefab, position, Quaternion.identity);
        fruit.ServerInit(tier, droppedByClientId, playerOwner, sourceDrop);   // ANTES do Spawn
        fruit.NetworkObject.Spawn();
        fruit.ServerSetVelocity(velocity);
        return fruit;
    }
}
