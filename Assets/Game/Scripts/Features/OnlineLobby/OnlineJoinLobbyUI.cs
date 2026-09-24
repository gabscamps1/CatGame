using CatGame.Capabilities.UISystem;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CatGame.UI
{
    public class OnlineJoinLobbyUI : BaseUIScreen
    {
        [SerializeField] private InputFieldElement inputFieldElement;
        [SerializeField] private ButtonElement buttonElement;
        private UnityTransport unityTransport;

        private void Awake()
        {
            inputFieldElement.OnValueChanged += InputFieldElement_OnValueChanged;
            buttonElement.OnSubmittedEvent += ButtonElement_OnSubmittedEvent;
        }


        private void OnDestroy()
        {
            inputFieldElement.OnValueChanged -= InputFieldElement_OnValueChanged;
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
            buttonElement.SetInteractable(!string.IsNullOrEmpty(inputFieldElement.Text));
        }

        private void ButtonElement_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {
            JoinInLobby();
        }

        #endregion

        #region Client

        private void JoinInLobby()
        {
            if (unityTransport == null)
                return;

            string socket = inputFieldElement.Text;
            string[] splitSocket = socket.Split(":");

            if (splitSocket.Length != 2)
                return;

            string ip = splitSocket[0].Trim();
            string port = splitSocket[1].Trim();

            unityTransport.SetConnectionData(ip, ushort.Parse(port));
            NetworkManager.Singleton.StartClient();

            Invoke("Teste", 5f);
        }

        private void Teste()
        {
            Debug.Log(NetworkManager.Singleton.IsConnectedClient);
        }

        #endregion
  

        private void SavePreferences()
        {

        }

        private void GetPreferences()
        {

        }
    }
}