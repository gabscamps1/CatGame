using UnityEngine;
using UnityEngine.UI;

public class FruitSelector : MonoBehaviour
{
    public static FruitSelector Instance;

    public GameObject[] Fruits => fruits;
    public GameObject[] NoPhysicsFruits => noPhysicsFruits;

    [SerializeField] private GameObject[] fruits;
    [SerializeField] private GameObject[] noPhysicsFruits;
    private int highestStartingFruitIndex = 3;

    [SerializeField] private Image _nextFruitImage;
    [SerializeField] private Sprite[] _fruitSprites;

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
        int randomIndex = Random.Range(0, highestStartingFruitIndex + 1);

        if (randomIndex < NoPhysicsFruits.Length)
        {
            GameObject randomFruit = NoPhysicsFruits[randomIndex];
            return randomFruit;
        }

        return null;
    }

    public void PickNextFruit()
    {
        int randomIndex = Random.Range(0, highestStartingFruitIndex + 1);

        if (randomIndex < Fruits.Length)
        {
            GameObject nextFruit = NoPhysicsFruits[randomIndex];
            NextFruit = nextFruit;

            _nextFruitImage.sprite = _fruitSprites[randomIndex];
        }
    }
}
