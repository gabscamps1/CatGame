using CatGame.Core.Data;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace CatGame.Features.CharacterSelection
{
    [DisallowMultipleComponent]
    public class MatchSetup : NetworkBehaviour
    {
        public static MatchSetup Instance { get; private set; } 

        public NetworkList<LobbySlot> Players { get; private set; } = new();

        /// <summary>
        /// Só no server. O host pediu a volta ao lobby e o próximo lobby deve ser montado a partir desta partida.
        /// </summary>
        public bool IsReturningToLobby { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        public void Capture(IEnumerable<LobbySlot> slots)
        {
            if (!IsServer)
              return; 

            Players.Clear();
            foreach (var slot in slots)
                Players.Add(slot);
        }

        public void BeginLobbyReturn()
        {
            if (IsServer)
                IsReturningToLobby = true;
        }

        public void CompleteLobbyReturn()
        {
            IsReturningToLobby = false;
        }

        public override void OnNetworkDespawn()
        {
            // Uma sessão nova nunca restaura o lobby de uma sessão antiga.
            IsReturningToLobby = false;
        }
    }
}
