using CatGame.Capabilities.Health;
using CatGame.Core;
using CatGame.Core.Interfaces;
using Unity.Netcode;
using UnityEngine;

public class GlassMaxHeight : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Min(1)] private float timeToLose;
    [SerializeField] private int damage = 1;

    private void OnEnable()
    {
        // Somente o server precisa conferir.
        if (!NetworkManager.Singleton.IsServer)
            enabled = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Ignora se não estiver na partida.
        if (GameManager.Instance.GetGameState() != GameManager.GameState.Playing)
            return;

        if (!collision.TryGetComponent(out FruitWorld fruitWorld))
            return;

        Debug.Log($"To na colisão matadora há {fruitWorld.TimeInCollision} segundos");

        if (fruitWorld.TimeInCollision < timeToLose)
            return;
        Debug.Log("1");
        IPlayerRegistry playerRegistry = ServiceLocator.Get<IPlayerRegistry>();

        // Pega o player dono da fruta.
        if (!playerRegistry.TryGetController(fruitWorld.PlayerOwner, out PlayerController playerController))
        {
            CatGame.Core.Logger.LogError("PlayerController não foi encontrado");
            return;
        }

        Debug.Log("2");
        if (!playerController.TryGetComponent(out IDamageable damageable))
            return;

        // Causa um de dano no jogador quando enche o pote.
        damageable.TakeDamage(damage);

        fruitWorld.ResetTimer();

        Debug.Log("Avisei que o player perdeu");
    }
}

