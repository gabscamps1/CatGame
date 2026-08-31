using System;
using UnityEngine;

public class ThrowFruitController : MonoBehaviour
{
    private const float EXTRA_WIDTH = 0.02f;

    public static ThrowFruitController Instance { get; private set; }

    public Bounds Bounds { get; private set; }

    [SerializeField] private Transform _fruitTransform;
    [SerializeField] private Transform _parentAfterThrow;

    private PlayerController _playerController;
    private CircleCollider2D _circleCollider;

    private GameObject currentFruit;
    private bool canThrow = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            CatGame.Core.Logger.LogWarning("[ThrowFruitController] Objeto duplicado.");
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
         _playerController = GetComponent<PlayerController>();

         PickFruit(FruitSelector.Instance.PickRandomFruitForThrow());
    }

    private void Update()
    {
        if (UserInput.IsThrowPressed && canThrow)
        {
            ThrowFruit();
        }
    }

    public void PickFruit(GameObject fruit)
    {
        GameObject fruitInstance = SpawnFruit(fruit, _fruitTransform.position, _fruitTransform.rotation, _fruitTransform);
        currentFruit = fruitInstance;

        _circleCollider = currentFruit.GetComponent<CircleCollider2D>();
        Bounds = _circleCollider.bounds;

        _playerController.ChangeBoundary(EXTRA_WIDTH);
    }

    public void ThrowFruit()
    {
        SpriteIndex index = currentFruit.GetComponent<SpriteIndex>();
        Quaternion rot = currentFruit.transform.rotation;

        SpawnFruit(FruitSelector.Instance.Fruits[index.Index], currentFruit.transform.position, rot);

        Destroy(currentFruit);

        canThrow = false;
    }

    private GameObject SpawnFruit(GameObject fruit, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return Instantiate(fruit, position, rotation, parent);
    }

    public void AllowThrowFruit()
    {
        canThrow = true;
    }
}