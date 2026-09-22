using CatGame.Capabilities.UISystem;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CatGame.UI
{
    public class OnlineLobbyUI : MonoBehaviour
    {
        [SerializeField] private InputFieldElement inputFieldElement;
        [SerializeField] private ButtonElement buttonElement;
        private UnityTransport unityTransport;

        private void Awake()
        {
            buttonElement.OnSubmittedEvent += ButtonElement_OnSubmittedEvent;
        }

        private void OnDestroy()
        {
            buttonElement.OnSubmittedEvent -= ButtonElement_OnSubmittedEvent;
        }

        private void ButtonElement_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {
            JoinInLobby();
        }

        private void Start()
        {
            if (!NetworkManager.Singleton.TryGetComponent(out unityTransport))
            {
                Core.Logger.LogError($"Não foi encontrado o {nameof(NetworkTransport)} do {nameof(NetworkManager)}");
                return;
            }

        }

        public void SetHostConnectionToEnter()
        {

            

        }

        public void SetHostPortToEnter(string port)
        {

        }

        private void CreateLobby()
        {
        }

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

            unityTransport.SetConnectionData(ip, ushort.Parse(port), "0.0.0.0");
            NetworkManager.Singleton.StartClient();

            Invoke("Teste", 5f);
        }

        private void Teste()
        {
            Debug.Log(NetworkManager.Singleton.IsConnectedClient);
        }

        private void SavePreferences()
        {

        }

        private void GetPreferences()
        {

        }
    }
}