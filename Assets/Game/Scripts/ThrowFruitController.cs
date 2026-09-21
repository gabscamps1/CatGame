using System;
using UnityEngine;

public class ThrowFruitController : MonoBehaviour
{
    private const float EXTRA_WIDTH = 0.02f;

    public Bounds Bounds { get; private set; }

    [SerializeField] private Transform fruitTransform;

    private PlayerController playerController;
    private CircleCollider2D circleCollider;

    private GameObject currentFruit;
    private bool canThrow = true;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        PickFruit(FruitSelector.Instance.PickRandomFruitForThrow());
    }

    private void PickFruit(GameObject fruit)
    {
        GameObject fruitInstance = SpawnFruit(fruit, fruitTransform.position, fruitTransform.rotation, fruitTransform);

        currentFruit = fruitInstance;

        if (!currentFruit.TryGetComponent(out CircleCollider2D circleCollider2D))
        {
            CatGame.Core.Logger.LogError($"{currentFruit} não possui {nameof(CircleCollider2D)}");
            return;
        }

        circleCollider = circleCollider2D;
        Bounds = circleCollider.bounds;

        playerController.ChangeBoundary(EXTRA_WIDTH);
    }

    private void ColliderInformer_OnCollidedWithBase(object sender, EventArgs e)
    {
        AllowThrowFruit();
        PickFruit(FruitSelector.Instance.NextFruit);

        ColliderInformer informer = (ColliderInformer)sender;
        informer.OnCollidedWithBase -= ColliderInformer_OnCollidedWithBase;
    }


    private GameObject SpawnFruit(GameObject fruit, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return Instantiate(fruit, position, rotation, parent);
    }

    private void AllowThrowFruit()
    {
        canThrow = true;
    }

    public void ThrowFruit()
    {
        if (!canThrow)
            return;

        SpriteIndex index = currentFruit.GetComponent<SpriteIndex>();
        Quaternion rot = currentFruit.transform.rotation;

        GameObject fruitInstance = SpawnFruit(FruitSelector.Instance.GetPhysicalFruit(index.Index), currentFruit.transform.position, rot);

        if (!fruitInstance.TryGetComponent(out ColliderInformer informer))
        {
            CatGame.Core.Logger.LogError($"[{nameof(ThrowFruitController)}] Não foi encontrado {nameof(ColliderInformer)} da fruta {fruitInstance}");
        }

        informer.OnCollidedWithBase += ColliderInformer_OnCollidedWithBase;

        Destroy(currentFruit);

        canThrow = false;
    }
}