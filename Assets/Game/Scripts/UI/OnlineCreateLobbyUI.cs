using CatGame.Capabilities.UISystem;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CatGame.UI
{
    public class OnlineCreateLobbyUI : BaseUIScreen
    {
        [SerializeField] private ButtonElement buttonElement;

        protected override void OnAfterShow()
        {
            CreateLobby();
        }

        protected override void OnBeforeHide()
        {
            CloseLobby();
        }

        #region Join

        private void InputFieldElement_OnValueChanged(object sender, InputFieldElement.ValueChangedEvent e)
        {
            SetJoinButtonInteractable();
        }

        private void SetJoinButtonInteractable()
        {
            // TODO
            // buttonElement.SetInteractable();
        }

        private void ButtonElement_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {
            // TODO
        }

        #endregion


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
        }

        private void CloseLobby()
        {
            if (NetworkManager.Singleton.IsServer)
                NetworkManager.Singleton.Shutdown();
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
    }
}