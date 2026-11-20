using CatGame.Capabilities.UISystem;
using CatGame.Core.Enums;
using CatGame.Core.Events;
using CatGame.Features.CharacterSelection;
using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame.UI
{
    public class OnlineJoinLobbyUI : BaseUIScreen
    {
        [SerializeField] private InputFieldElement inputFieldElementIp;
        [SerializeField] private InputFieldElement inputFieldElementPort;
        [SerializeField] private ButtonElement buttonElement;
        [SerializeField] private MenuController menuController;
        [SerializeField] private int menuIndexWhenConnectLobby;
        [SerializeField] private LobbyRoot lobbyRoot;

        [Header("Visual Components")]
        [SerializeField] private Image buttonImage;
        [SerializeField] private Color disableButtonImageColor;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Color disableButtonTextColor;
        private Color initButtonImageColor;
        private Color initButtonTextColor;


        private UnityTransport unityTransport;

        private void Awake()
        {
            initButtonImageColor = buttonImage.color;
            initButtonTextColor = buttonText.color;

            inputFieldElementIp.OnValueChanged += InputFieldElement_OnValueChanged;
            inputFieldElementPort.OnValueChanged += InputFieldElement_OnValueChanged;
            buttonElement.OnSubmittedEvent += ButtonElement_OnSubmittedEvent;
        }

        private void OnDestroy()
        {
            inputFieldElementIp.OnValueChanged -= InputFieldElement_OnValueChanged;
            inputFieldElementPort.OnValueChanged -= InputFieldElement_OnValueChanged;
            buttonElement.OnSubmittedEvent -= ButtonElement_OnSubmittedEvent;
        }

        private void Start()
        {
            if (!NetworkManager.Singleton.TryGetComponent(out unityTransport))
            {
                Core.Logger.LogError($"Não foi encontrado o {nameof(NetworkTransport)} do {nameof(NetworkManager)}");
                return;
            }
        }

        protected override void OnAfterShow()
        {
            SetJoinButtonInteractable();
        }

        #region Join

        private void InputFieldElement_OnValueChanged(object sender, InputFieldElement.ValueChangedEvent e)
        {
            SetJoinButtonInteractable();
        }

        private void SetJoinButtonInteractable()
        {
            bool isInteractable = !string.IsNullOrEmpty(inputFieldElementIp.Text) && !string.IsNullOrEmpty(inputFieldElementPort.Text);

            buttonElement.SetOptionInteractable(isInteractable);
            buttonImage.color = isInteractable ? initButtonImageColor : disableButtonImageColor;
            buttonText.color = isInteractable ? initButtonTextColor : disableButtonTextColor;
        }

        private void ButtonElement_OnSubmittedEvent(object sender, ButtonElement.SubmittedEvent e)
        {
            JoinInLobby();
        }

        #endregion

        #region Client

        private void JoinInLobby()
        {
            if (unityTransport == null)
                return;

            string ip = inputFieldElementIp.Text.Trim();
            string port = inputFieldElementPort.Text.Trim();
            
            if (!IsValidIp(ip))
            {
                EventBus.Publish(new SessionEndedEvent(false, false, "", SessionCallbackReason.IpError));
                return;
            }

            if (!IsValidPort(port))
            {
                EventBus.Publish(new SessionEndedEvent(false, false, "", SessionCallbackReason.PortError));
                return;
            }

            unityTransport.SetConnectionData(ip, ushort.Parse(port));

            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
            NetworkManager.Singleton.StartClient();
        }

        private bool IsValidIp(string ip)
        {
            return !string.IsNullOrEmpty(ip) 
                && ip.Split(".").Length == 4
                && IPAddress.TryParse(ip, out IPAddress ipAddress) 
                && ipAddress.AddressFamily == AddressFamily.InterNetwork;   
        }

        private bool IsValidPort(string port)
        {
            return ushort.TryParse(port, out ushort otherPort) && otherPort != 0;
        }

        private void Singleton_OnClientConnectedCallback(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                menuController.ChangeMenu(menuIndexWhenConnectLobby);
                lobbyRoot.OpenOnlineClient();

                NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
            }
        }

        private void Singleton_OnClientDisconnectCallback(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
            }           
        }

        #endregion        
    }
}