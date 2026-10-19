using CatGame.Core.Data;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FruitSelector : MonoBehaviour
{
    public static FruitSelector Instance;

    [Serializable]
    public class FruitGroups
    {
        public GameObject PhysicalFruit => fruits;
        public GameObject NoPhysicsFruit => noPhysicsFruits;
        public Sprite FruitSprite => fruitSprites;

        [SerializeField] private Sprite fruitSprites;
        [SerializeField] private GameObject fruits;
        [SerializeField] private GameObject noPhysicsFruits;
    }

    private readonly Dictionary<PlayerId, FruitGroups> nextClientFruitGroup = new();

    [SerializeField] private FruitGroups[] fruitGroups;
    
    private int highestStartingFruitIndex = 3;

    public FruitGroups NextFruit { get; private set; }

    
    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            CatGame.Core.Logger.LogWarning("[FruitSelector] Objeto duplicado.");
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        PickNextFruit();
    }

    public GameObject PickRandomFruitForThrow()
    {
        int randomIndex = UnityEngine.Random.Range(0, highestStartingFruitIndex + 1);

        if (randomIndex < fruitGroups.Length)
        {
            GameObject randomFruit = fruitGroups[randomIndex].NoPhysicsFruit;
            return randomFruit;
        }

        return null;
    }

    public void PickNextFruit()
    {
        int randomIndex = UnityEngine.Random.Range(0, highestStartingFruitIndex + 1);

        if (randomIndex < fruitGroups.Length)
        {
            NextFruit = fruitGroups[randomIndex];
        }
    }
    
    

    public GameObject GetPhysicalFruit(int index)
    {
        return fruitGroups[index].PhysicalFruit;
    }

    public GameObject GetNoPhysicalFruit(int index)
    {
        return fruitGroups[index].NoPhysicsFruit;
    }

    public Sprite GetFruitSprite(int index)
    {
        return fruitGroups[index].FruitSprite;
    }

    public bool IsLastTypeOfFruit(int index)
    {
        return index == fruitGroups.Length - 1;
    }


    // ONLINE

    /// <summary>
    /// Seleciona a próxima fruta do jogador.
    /// </summary>
    /// <param name="playerId"></param>
    public void PickPlayerNextFruit(PlayerId playerId)
    {
        int randomIndex = UnityEngine.Random.Range(0, highestStartingFruitIndex + 1);

        if (randomIndex >= fruitGroups.Length)
            return;

        // Seleciona a próxima fruta pro client.
        if (nextClientFruitGroup.ContainsKey(playerId))
            nextClientFruitGroup[playerId] = fruitGroups[randomIndex];
        else
            nextClientFruitGroup.Add(playerId, fruitGroups[randomIndex]);
    }

    public FruitGroups GetPlayerNextFruit(PlayerId playerId)
    {
        return nextClientFruitGroup.TryGetValue(playerId, out FruitGroups fruitGroup) ? fruitGroup : null;
    }
}
