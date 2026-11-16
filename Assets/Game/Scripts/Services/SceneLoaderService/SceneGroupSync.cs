using CatGame.Core.Enums;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.Services.SceneManagement
{
    /// <summary>
    /// Ponte de rede do carregamento de cenas: o servidor publica o estado do grupo e quais cenas o NetworkManager carregou, e os clientes recebem.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneGroupSync : NetworkBehaviour
    {
        private readonly NetworkVariable<SceneGroupState> state = new(SceneGroupState.None);

        // Cenas que o servidor carregou pelo NetworkManager nesta sessão. As outras cenas abertas são locais (cada máquina é responsável pela sua).
        private readonly NetworkList<FixedString64Bytes> networkedScenes = new();

        // Só no servidor: clientes que já terminaram a parte local da fase Loading do grupo atual.
        private readonly HashSet<ulong> readyClients = new();

        /// <summary>
        /// Disparado somente nos clientes.
        /// </summary>
        public event Action<SceneGroupState> OnStateReceived;

        public SceneGroupState State => state.Value;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        #region Unity Network

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Sessão nova não herda o estado da sessão anterior.
                state.Value = SceneGroupState.None;
                networkedScenes.Clear();
                readyClients.Clear();
                return;
            }

            state.OnValueChanged += State_OnValueChanged;

            // Cliente que conectou no meio de um carregamento (ou depois dele) recebe o estado atual.
            if (state.Value.Phase != SceneGroupPhase.None)
                OnStateReceived?.Invoke(state.Value);
        }

        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= State_OnValueChanged;
        }

        #endregion

        public void Publish(SceneGroupState newState)
        {
            if (!IsServer)
            {
                Core.Logger.LogWarning($"[{nameof(SceneGroupSync)}] Somente o servidor pode publicar o estado do carregamento");
                return;
            }

            if (newState.Phase == SceneGroupPhase.Loading)
                readyClients.Clear();

            state.Value = newState;
        }

        #region Ready Handshake

        /// <summary>
        /// Cliente avisa que terminou a parte local da fase Loading (ex: cena de transição criada, cenas locais descarregadas).
        /// Só depois disso o servidor mexe nas cenas pelo NGO.
        /// </summary>
        public void ReportLoadingReady(Scenes group)
        {
            if (IsServer || !IsSpawned)
                return;

            ReportLoadingReadyRpc(group);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReportLoadingReadyRpc(Scenes group, RpcParams rpcParams = default)
        {
            // Não confia no payload: confere se é o grupo que está carregando agora.
            if (state.Value.Phase != SceneGroupPhase.Loading || state.Value.Group != group)
                return;

            readyClients.Add(rpcParams.Receive.SenderClientId);
        }

        public bool AreAllClientsReady()
        {
            return GetClientsNotReady().Count == 0;
        }

        public List<ulong> GetClientsNotReady()
        {
            List<ulong> clientsNotReady = new();

            if (!IsServer)
                return clientsNotReady;

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId != NetworkManager.ServerClientId && !readyClients.Contains(clientId))
                    clientsNotReady.Add(clientId);
            }

            return clientsNotReady;
        }

        #endregion

        public bool IsNetworkedScene(string scene)
        {
            if (!IsSpawned)
                return false;

            FixedString64Bytes sceneName = new(scene);

            foreach (FixedString64Bytes networkedScene in networkedScenes)
            {
                if (networkedScene == sceneName)
                    return true;
            }

            return false;
        }

        public void ServerAddNetworkedScene(string scene)
        {
            if (!IsServer)
            {
                CatGame.Core.Logger.LogWarning("Somente o servidor pode registrar cenas de rede");
                return;
            }

            if (!IsNetworkedScene(scene))
                networkedScenes.Add(new FixedString64Bytes(scene));
        }

        public void ServerRemoveNetworkedScene(string scene)
        {
            if (!IsServer)
            {
                CatGame.Core.Logger.LogWarning("Somente o servidor pode remover cenas de rede");
                return;
            }

            FixedString64Bytes sceneName = new(scene);

            for (int i = networkedScenes.Count - 1; i >= 0; i--)
            {
                if (networkedScenes[i] == sceneName)
                    networkedScenes.RemoveAt(i);
            }
        }

        private void State_OnValueChanged(SceneGroupState previous, SceneGroupState current)
        {
            OnStateReceived?.Invoke(current);
        }
    }
}
