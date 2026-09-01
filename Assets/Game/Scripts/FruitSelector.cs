using System;
using UnityEngine;
using UnityEngine.UI;

public class FruitSelector : MonoBehaviour
{
    [Serializable]
    public class FruitGroups
    {
        public GameObject Fruit => fruits;
        public GameObject NoPhysicsFruit => noPhysicsFruits;
        public Sprite FruitSprite => fruitSprites;

        [SerializeField] private Sprite fruitSprites;
        [SerializeField] private GameObject fruits;
        [SerializeField] private GameObject noPhysicsFruits;
    }

    public static FruitSelector Instance;

    [SerializeField] private FruitGroups[] fruitGroups;
    [SerializeField] private Image nextFruitImage;
    
    private int highestStartingFruitIndex = 3;

    public GameObject NextFruit { get; private set; }

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
            GameObject nextFruit = GetNoPhysicalFruit(randomIndex);
            NextFruit = nextFruit;

            nextFruitImage.sprite = GetFruitSprite(randomIndex);
        }
    }

    public GameObject GetPhysicalFruit(int index)
    {
        return fruitGroups[index].Fruit;
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
}
