using CatGame.Core.Data;
using System.Collections.Generic;
using UnityEngine;
using CatGame.Core;
using CatGame.Core.Interfaces;


public class FruitSpawner : MonoBehaviour
{
    public static FruitSpawner Instance { get; private set; }

    [SerializeField] private FruitWorld prefab;

    [Header("Sounds")]
    [SerializeField] private AudioData throwSound;

    private IAudioService audioService;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }

    private void Start(){
	    audioService = ServiceLocator.Get<IAudioService>();
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
        Debug.Log("SPAWN FRUTA");
        audioService.PlaySFXClip(throwSound);
        fruit.ServerInit(tier, droppedByClientId, sourceDrop);   // ANTES do Spawn
        fruit.NetworkObject.Spawn();
        fruit.ServerSetVelocity(velocity);
        return fruit;
    }
}
