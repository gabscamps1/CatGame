using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CatGame.Features.OnlineLobby
{
    public class OnlineCreateLobbyPresenter : MonoBehaviour
    {
        public struct PlayerLobbyData : IEquatable<PlayerLobbyData>, INetworkSerializeByMemcpy
        {
            public ulong ClientId;
            public FixedString32Bytes Name;
            public bool IsHost;

            public PlayerLobbyData(ulong clientId, string name, bool isHost)
            {
                ClientId = clientId;
                Name = name;
                IsHost = isHost;
            }

            public bool Equals(PlayerLobbyData other) => ClientId == other.ClientId;
        }

        [SerializeField] private OnlineCreateLobbyUI onlineCreateLobbyUI;

        private NetworkList<PlayerLobbyData> playerLobbyDatas = new(); 

        private IUIService uiService;
        private CloseQuitLobbyPopUp popUp;

        private void Awake()
        {
            uiService = ServiceLocator.Get<IUIService>();
            uiService.OnCurrentUIChanged += UiService_OnCurrentUIChanged;

            onlineCreateLobbyUI.OnTriedHideMenu += OnlineCreateLobbyUI_OnTriedHideMenu;

            playerLobbyDatas.OnListChanged += PlayerLobbyDatas_OnListChanged;
        }

        private void OnDestroy()
        {
            uiService.OnCurrentUIChanged -= UiService_OnCurrentUIChanged;

            onlineCreateLobbyUI.OnTriedHideMenu -= OnlineCreateLobbyUI_OnTriedHideMenu;

            playerLobbyDatas.OnListChanged -= PlayerLobbyDatas_OnListChanged;

            if (popUp != null)
            {
                popUp.OnConfirmed -= CloseQuitLobbyPopUp_OnConfirmed;
                popUp.OnCancelled -= CloseQuitLobbyPopUp_OnCancelled;
            }
        }

        #region LobbyCreation

        public void CreateLobby()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("Server já estava criado");
                return;
            }

            string ip = GetLocalIpAddress();
            int port = 1000;

            while (!IsPortAvaliable(port))
                port++;

            if (!NetworkManager.Singleton.TryGetComponent(out UnityTransport unityTransport))
            {
                Core.Logger.LogError($"Não foi encontrado o {nameof(NetworkTransport)} do {nameof(NetworkManager)}");
                return;
            }

            unityTransport.SetConnectionData(ip, (ushort)port);

            NetworkManager.Singleton.StartHost();

            // O Host se adiciona manualmente, já que o callback de conexão ignora o pr�prio LocalClientId
            playerLobbyDatas.Add(new PlayerLobbyData(NetworkManager.Singleton.LocalClientId, "Host", true));

            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
        }


        private string GetLocalIpAddress()
        {
            string localIPAddress = "127.0.0.1";

            // Pega todos os endereços de IPs presentes no PC.
            IPAddress[] ipsAddress = Dns.GetHostAddresses(Dns.GetHostName());

            if (ipsAddress == null || ipsAddress.Length == 0)
                return localIPAddress;

            foreach (IPAddress ip in ipsAddress)
            {
                // Procura pelo IPV4 dentre esses endereços.
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIPAddress = ip.ToString();
                    break;
                }
            }

            return localIPAddress;
        }

        private bool IsPortAvaliable(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            // Lista todas as portas UDP que estão ativas no PC neste momento
            IPEndPoint[] udpListeners = ipGlobalProperties.GetActiveUdpListeners();

            // Procura se a nossa porta está nessa lista do Windows
            foreach (IPEndPoint endPoint in udpListeners)
            {
                if (endPoint.Port == port)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region CloseLobby

        private void OnlineCreateLobbyUI_OnTriedHideMenu()
        {
            // Popup já está aberta, então ignora o código.
            if (uiService.CurrentPanelType.Value == PanelType.QuitLobbyPopup)
                return;

            uiService.Push(PanelType.QuitLobbyPopup);
            IBasePanel panel = uiService.CurrentPanel;
            popUp = panel as CloseQuitLobbyPopUp;

            if (popUp == null)
            {
                Core.Logger.LogError($"Não foi encontrado o panel {nameof(CloseQuitLobbyPopUp)}");
                return;
            }

            // Quando a popup é aberta, se inscreve nos eventos da popup
            popUp.OnConfirmed += CloseQuitLobbyPopUp_OnConfirmed;
            popUp.OnCancelled += CloseQuitLobbyPopUp_OnCancelled;
        }

        private void CloseQuitLobbyPopUp_OnConfirmed()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                Core.Logger.LogWarning("Server já estava fechado");
                return;
            }

            CloseLobby();
            uiService.Pop();
        }

        private void CloseQuitLobbyPopUp_OnCancelled()
        {
            uiService.Pop();
        }

        /// <summary>
        /// Fechar/Sair do lobby
        /// </summary>
        private void CloseLobby()
        {
            NetworkManager.Singleton.Shutdown(true);
            NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;

            playerLobbyDatas.Clear();
        }

        #endregion

        private void UiService_OnCurrentUIChanged(PanelType? obj)
        {
            // Garante que quando a popup for fechada, os eventos são desinscrevidos.
            // Não faz isso diretamente no proprio método para evitar problemas caso a popup for fechada de outras maneiras.
            if (!uiService.IsVisible(PanelType.QuitLobbyPopup))
            {
                popUp.OnConfirmed -= CloseQuitLobbyPopUp_OnConfirmed;
                popUp.OnCancelled -= CloseQuitLobbyPopUp_OnCancelled;
            }
        }


        private void Singleton_OnClientConnectedCallback(ulong clientID)
        {
            if (clientID == NetworkManager.Singleton.LocalClientId)
                return; // já foi adicionado em CreateLobby

            playerLobbyDatas.Add(new PlayerLobbyData(clientID, clientID.ToString(), false));
        }

        private void Singleton_OnClientDisconnectCallback(ulong clientID)
        {
            // Volta pro menu anterior quando o próprio Owner é desconectado.
            if (clientID == NetworkManager.Singleton.LocalClientId)
            {
                onlineCreateLobbyUI.CloseLobbyUI();
                NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
                return;
            }

            for (int i = 0; i < playerLobbyDatas.Count; i++)
            {
                if (playerLobbyDatas[i].ClientId == clientID)
                {
                    playerLobbyDatas.RemoveAt(i);
                    return;
                }
            }
        }

        private void PlayerLobbyDatas_OnListChanged(NetworkListEvent<PlayerLobbyData> changeEvent)
        {
            UpdateUIWithClient();
        }

        private void UpdateUIWithClient()
        {
            for (int i = 0; i < playerLobbyDatas.Count; i++)
            {
                onlineCreateLobbyUI.UpdatePlayerText(i, playerLobbyDatas[i].Name.ToString(), playerLobbyDatas[i].IsHost);
            }
        }
    }
}