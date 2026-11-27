using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.Features.Gameplay
{
    /// <summary>
    /// Tira da rede os objetos criados durante a partida (jogadores, IAs e frutas).
    /// Sem isso, ao descarregar a cena o NGO move esses objetos para o DontDestroyOnLoad e eles aparecem no lobby.
    /// </summary>
    public class MatchDespawner : MonoBehaviour
    {
        /// <summary>
        /// Só no server. Objetos colocados na cena ficam, porque o descarregamento da cena cuida deles.
        /// </summary>
        public void DespawnMatchObjects()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null || !networkManager.IsServer)
                return;

            // Copia antes, porque o Despawn altera a lista de objetos spawnados.
            List<NetworkObject> objectsToDespawn = new();

            foreach (NetworkObject networkObject in networkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject.InScenePlaced == true)
                    continue;

                if (networkObject.gameObject.scene != gameObject.scene)
                    continue;

                objectsToDespawn.Add(networkObject);
            }

            foreach (NetworkObject networkObject in objectsToDespawn)
            {
                if (networkObject != null && networkObject.IsSpawned)
                    networkObject.Despawn(true);
            }
        }
    }
}
