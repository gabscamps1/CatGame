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
    }
}
