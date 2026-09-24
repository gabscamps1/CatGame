using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CatGame.Features.OnlineLobby
{
    public class OnlineCreateLobbyPresenter : NetworkBehaviour
    {
        [SerializeField] private OnlineCreateLobbyUI OnlineCreateLobbyUI;

        private void Awake()
        {
            OnlineCreateLobbyUI.OnEnabledMenu += OnlineCreateLobbyUI_OnEnabledMenu;
            OnlineCreateLobbyUI.OnDisabledMenu += OnlineCreateLobbyUI_OnDisabledMenu;
        }

        public override void OnDestroy()
        {
            OnlineCreateLobbyUI.OnEnabledMenu -= OnlineCreateLobbyUI_OnEnabledMenu;
            OnlineCreateLobbyUI.OnDisabledMenu -= OnlineCreateLobbyUI_OnDisabledMenu;
        }

        private void OnlineCreateLobbyUI_OnEnabledMenu()
        {
            CreateLobby();

        }


        private void OnlineCreateLobbyUI_OnDisabledMenu()
        {
            CloseLobby();
        }

        #region Host

        private void CreateLobby()
        {
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
        }

        private void CloseLobby()
        {
            if (NetworkManager.Singleton.IsServer)
                NetworkManager.Singleton.Shutdown();

            NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
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

        private void Singleton_OnClientConnectedCallback(ulong clientID)
        {
            if (clientID == NetworkManager.Singleton.LocalClientId)
                return;


        }

        private void UpdateUIWithClient()
        {

        }
    }
}