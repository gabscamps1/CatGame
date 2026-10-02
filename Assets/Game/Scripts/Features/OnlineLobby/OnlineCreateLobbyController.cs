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
        public struct PlayerEntered : INetworkSerializeByMemcpy, IEquatable<PlayerEntered>
        {
            public PlayerId PlayerId;
            public FixedString32Bytes PlayerName;

            public bool Equals(PlayerEntered other)
            {
                return this.PlayerId == other.PlayerId;
            }

            public override bool Equals(object obj)
            {
                return obj is PlayerEntered other && Equals(other);
            }

            public override int GetHashCode()
            {
                return PlayerId.Id;
            }
        }

        [SerializeField] private OnlineCreateLobbyUI onlineCreateLobbyUI;


        private NetworkList<PlayerEntered> playerIds = new(); 

        private IUIService uiService;
        private CloseQuitLobbyPopUp popUp;

        private void Awake()
        {
            uiService = ServiceLocator.Get<IUIService>();
            uiService.OnCurrentUIChanged += UiService_OnCurrentUIChanged;

            onlineCreateLobbyUI.OnTriedHideMenu += OnlineCreateLobbyUI_OnTriedHideMenu;

            playerIds.OnListChanged += PlayerIds_OnListChanged;
        }

        private void PlayerIds_OnListChanged(NetworkListEvent<PlayerEntered> changeEvent)
        {
            onlineCreateLobbyUI.UpdateUI();
        }

        private void OnDestroy()
        {
            onlineCreateLobbyUI.OnTriedHideMenu -= OnlineCreateLobbyUI_OnTriedHideMenu;

            uiService.OnCurrentUIChanged -= UiService_OnCurrentUIChanged;

            if (popUp != null)
            {
                popUp.OnConfirmed -= CloseQuitLobbyPopUp_OnConfirmed;
                popUp.OnCancelled -= CloseQuitLobbyPopUp_OnCancelled;
            }
        }

        #region LobbyCreation

        public void CreateLobby()
        {
            Debug.Log("Server criado");

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
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;

            PlayerEntered playerEntered = new PlayerEntered();
            playerEntered.PlayerId = new PlayerId(0);
            playerEntered.PlayerName = "Casa";
            playerIds.Add(playerEntered);

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
            CloseLobby();
            uiService.Pop();
        }

        private void CloseQuitLobbyPopUp_OnCancelled()
        {
            uiService.Pop();
        }

        private void CloseLobby()
        {
            playerIds.Clear();
            NetworkManager.Singleton.Shutdown(true);
            NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
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
                return;

            PlayerEntered playerEntered = new PlayerEntered();
            playerEntered.PlayerId = new PlayerId(1);
            playerEntered.PlayerName = "Casa";

            playerIds.Add(playerEntered);

            // TODO - atualiar os jogadores quando alguem entrar.
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

            // TODO - atualiar os jogadores quando alguem sair.
        }

        private void UpdateUIWithClient()
        {

        }

    }
}