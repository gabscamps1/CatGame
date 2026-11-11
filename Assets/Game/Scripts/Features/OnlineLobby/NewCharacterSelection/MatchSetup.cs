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
        }

        public void Capture(IEnumerable<LobbySlot> slots)
        {
            if (!IsServer)
              return; 

            Players.Clear();
            foreach (var slot in slots)
                Players.Add(slot);
        }

        public PlayerId? FindPlayerId(ulong clientId)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                LobbySlot slot = Players[i];

                if (slot.OwnerId != clientId)
                    continue;

                // Encontrou o PlayerId referente ao client.
                return new PlayerId(i);
            }

            return null;
        }
    }
}
