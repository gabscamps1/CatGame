using CatGame.Core.Data;
using CatGame.Features.CharacterSelection;
using CatGame.Features.Gameplay;
using System;
using Unity.Netcode;
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
        SelectFruit(FruitSelector.Instance.PickRandomFruitForThrow());
    }

    private void ColliderInformer_OnCollidedWithBase(object sender, EventArgs e)
    {
        AllowThrowFruit();

        SelectFruit(FruitSelector.Instance.NextFruit.NoPhysicsFruit);

        ColliderInformer informer = (ColliderInformer)sender;
        informer.OnCollidedWithBase -= ColliderInformer_OnCollidedWithBase;
    }

    private GameObject SpawnFruit(GameObject fruit, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject instance = Instantiate(fruit, position, rotation, parent);

        /*if (!instance.TryGetComponent(out NetworkObject netObj))
            return instance;

        netObj.SpawnWithOwnership()*/

        return instance;
    }

    private void AllowThrowFruit()
    {
        canThrow = true;
    }

    private void SelectFruit(GameObject fruit)
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

    [Rpc(SendTo.Server)]
    public void RequestHoldFruitRpc(RpcParams rpc = default)
    {
        ulong clientId = rpc.Receive.SenderClientId;

        PlayerId playerId = new PlayerId(-1);

        for (int i = 0; i < MatchSetup.Players.Count; i++)
        {
            LobbySlot slot = MatchSetup.Players[i];

            if (slot.OwnerId != clientId)
                continue;

            // Encontrou o PlayerId referente ao client.
            playerId = new PlayerId(i);
        }

        // Não encontrou o player.
        if (playerId.Id == -1)
            return;

        if (!ThrowFruitManager.CanPlayerHoldFruit(playerId))
            return;

        GameObject fruitPrefab = FruitSelector.Instance.GetPlayerNextFruit(playerId).NoPhysicsFruit ?? FruitSelector.Instance.PickRandomFruitForThrow();
        GameObject fruitInstance = SpawnFruit(fruitPrefab, fruitTransform.position, fruitTransform.rotation, fruitTransform);

        ThrowFruitManager.SetPlayerNoPhysicalFruit(playerId, fruitInstance);

        if (!fruitInstance.TryGetComponent(out CircleCollider2D circleCollider2D))
        {
            CatGame.Core.Logger.LogError($"{fruitInstance} não possui {nameof(CircleCollider2D)}");
            return;
        }

        //FAZER DEPOIS
        circleCollider = circleCollider2D;
        Bounds = circleCollider.bounds;

        playerController.ChangeBoundary(EXTRA_WIDTH);

        ThrowFruitManager.PreventPlayerHoldFruit(playerId);
    }

    [Rpc(SendTo.Server)]
    public void RequestThrowFruitRpc(RpcParams rpc = default)
    {
        ulong clientId = rpc.Receive.SenderClientId;

        PlayerId playerId = new PlayerId(-1);

        for (int i = 0; i < MatchSetup.Players.Count; i++)
        {
            LobbySlot slot = MatchSetup.Players[i];

            if (slot.OwnerId != clientId)
                continue;

            // Encontrou o PlayerId referente ao client.
            playerId = new PlayerId(i);
        }

        // Não encontrou o player.
        if (playerId.Id == -1)
            return;

        // Validação - Já existe alguma fruta spawnada?
        if (!ThrowFruitManager.CanPlayerSpawnedFruit(playerId))
            return;


        GameObject fruitPrefab = FruitSelector.Instance.GetPlayerNextFruit(playerId).PhysicalFruit;
        GameObject fruitInstance = SpawnFruit(fruitPrefab, currentFruit.transform.position, currentFruit.transform.rotation);

        if (!fruitInstance.TryGetComponent(out ColliderInformer informer))
        {
            CatGame.Core.Logger.LogError($"[{nameof(ThrowFruitController)}] Não foi encontrado {nameof(ColliderInformer)} da fruta {fruitInstance}");
        }

        informer.OnCollidedWithBase += ColliderInformer_OnCollidedWithBase;

        // Remove a fruta da mão do personagem.
        GameObject previewFruit = ThrowFruitManager.GetPlayerNoPhysicalFruit(playerId);
        ThrowFruitManager.RemovePlayerNoPhysicalFruit(playerId);
        Destroy(previewFruit);

        if (!fruitInstance.TryGetComponent(out NetworkObject fruitNetworkObject))
        {
            CatGame.Core.Logger.LogError($"[{nameof(ThrowFruitController)}] Não foi encontrado {nameof(NetworkObject)} da fruta {fruitInstance}");
            return;
        }

        // O servidor define o dono e faz o spawn pro online
        fruitNetworkObject.SpawnWithOwnership(clientId);

        // Proibe esse jogador de criar outra fruta.
        ThrowFruitManager.PreventPlayerSpawnFruit(playerId);
    }


}