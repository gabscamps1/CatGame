using CatGame.Core;
using CatGame.Core.Interfaces;
using Unity.Netcode;
using UnityEngine;

public class GlassMaxHeight : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Min(1)] private float timeToLose;
    [SerializeField] private int damage = 1;

    private Collider2D zoneCollider;

    public float TimeToLose => timeToLose;

    /// <summary>
    /// Base da zona de derrota. Uma fruta cuja borda de cima passa daqui começa a contar tempo.
    /// </summary>
    public float DangerLineY => ZoneCollider.bounds.min.y;

    private Collider2D ZoneCollider
    {
        get
        {
            // Checagem explícita: o ??= ignora a checagem de objeto destruído da Unity.
            if (zoneCollider == null)
                zoneCollider = GetComponent<Collider2D>();

            return zoneCollider;
        }
    }

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

        // Pega o dono da fruta.
        if (!playerRegistry.TryGetActor(fruitWorld.PlayerOwner, out IPlayerActor actor))
        {
            CatGame.Core.Logger.LogError("IPlayerActor não foi encontrado");
            return;
        }

        Debug.Log("2");
        if (actor.Damageable == null)
            return;

        // Causa um de dano no jogador quando enche o pote.
        actor.Damageable.TakeDamage(damage);

        fruitWorld.ResetTimer();

        Debug.Log("Avisei que o player perdeu");
    }
}

