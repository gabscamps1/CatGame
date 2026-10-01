using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CatGame.Features.OnlineLobby
{
    public class OnlineCreateLobbyPresenter : MonoBehaviour
    {
        [SerializeField] private OnlineCreateLobbyUI OnlineCreateLobbyUI;
        private IPlayerInputController playerInputController;

        private IUINavigationService uINavigationService;
        private IUIService uiService;
        private CloseQuitLobbyPopUp popUp;

        private void Awake()
        {
            uINavigationService = ServiceLocator.Get<IUINavigationService>();
            uiService = ServiceLocator.Get<IUIService>();
            uiService.OnCurrentUIChanged += UiService_OnCurrentUIChanged;

            // Somente permite a navegação do player 1 no menu.
            PlayerId playerOne = new PlayerId(0);
            playerInputController = ServiceLocator.Get<IInputService>().GetInputFromPlayer(playerOne);

            OnlineCreateLobbyUI.OnEnabledMenu += OnlineCreateLobbyUI_OnEnabledMenu;
            OnlineCreateLobbyUI.OnDisabledMenu += OnlineCreateLobbyUI_OnDisabledMenu;
            OnlineCreateLobbyUI.OnTriedHideMenu += OnlineCreateLobbyUI_OnTriedHideMenu;
        }

        

        private void Start()
        {
            
        }

        private void UiService_OnCurrentUIChanged(PanelType? obj)
        {
            // Garante que quando a popup for fechada, os eventos são desinscrevidos.
            if (!uiService.IsVisible(PanelType.QuitLobbyPopup))
            {
                popUp.OnConfirmed -= CloseQuitLobbyPopUp_OnConfirmed;
                popUp.OnCancelled -= CloseQuitLobbyPopUp_OnCancelled;
            }
        }

        private void OnDestroy()
        {
            OnlineCreateLobbyUI.OnEnabledMenu -= OnlineCreateLobbyUI_OnEnabledMenu;
            OnlineCreateLobbyUI.OnDisabledMenu -= OnlineCreateLobbyUI_OnDisabledMenu;
            OnlineCreateLobbyUI.OnTriedHideMenu -= OnlineCreateLobbyUI_OnTriedHideMenu;

            uiService.OnCurrentUIChanged -= UiService_OnCurrentUIChanged;

            if (popUp != null)
            {
                popUp.OnConfirmed -= CloseQuitLobbyPopUp_OnConfirmed;
                popUp.OnCancelled -= CloseQuitLobbyPopUp_OnCancelled;
            }       
        }

        private void OnlineCreateLobbyUI_OnEnabledMenu()
        {
            CreateLobby();
            //playerInputController.OnCancelled += PlayerInputController_OnCancelled;
        }

        private void OnlineCreateLobbyUI_OnDisabledMenu()
        {
            CloseLobby();
            //playerInputController.OnCancelled -= PlayerInputController_OnCancelled;
        }

        private void OnlineCreateLobbyUI_OnTriedHideMenu()
        {
            // Popup já está aberta, então ignora o código.
            if (uiService.CurrentPanelType.Value == PanelType.QuitLobbyPopup)
                return;

            uiService.Push(PanelType.QuitLobbyPopup);
            IBasePanel panel = uiService.CurrentPanel;
            popUp = panel as CloseQuitLobbyPopUp;

            //uINavigationService.PushGroup();

            if (popUp == null)
            {
                Core.Logger.LogError($"Não foi encontrado o panel {nameof(CloseQuitLobbyPopUp)}");
                return;
            }

            // Quando a popup é aberta, se inscreve nos eventos da popup
            popUp.OnConfirmed += CloseQuitLobbyPopUp_OnConfirmed;
            popUp.OnCancelled += CloseQuitLobbyPopUp_OnCancelled;
        }

        private void PlayerInputController_OnCancelled()
        {
            // Popup já está aberta, então ignora o código.
            if (uiService.CurrentPanelType.Value == PanelType.QuitLobbyPopup)
                return;

            uiService.Push(PanelType.QuitLobbyPopup);
            IBasePanel panel = uiService.CurrentPanel;
            popUp = panel as CloseQuitLobbyPopUp;

            //uINavigationService.PushGroup();

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
        }

        private void CloseQuitLobbyPopUp_OnCancelled()
        {
            uiService.Pop();
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