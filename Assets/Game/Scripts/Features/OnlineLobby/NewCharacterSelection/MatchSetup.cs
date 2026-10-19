using CatGame.Core.Data;
using System.Collections.Generic;

namespace CatGame.Features.CharacterSelection
{
    public static class MatchSetup
    {
        public static readonly List<LobbySlot> Players = new();

        public static void Capture(IEnumerable<LobbySlot> slots)
        {
            Players.Clear();
            Players.AddRange(slots);
        }

        public static PlayerId? FindPlayerId(ulong clientId)
        {
            for (int i = 0; i < MatchSetup.Players.Count; i++)
            {
                LobbySlot slot = MatchSetup.Players[i];

                if (slot.OwnerId != clientId)
                    continue;

                // Encontrou o playerId referente ao client.
                return new PlayerId(i);
            }

            return null;
        }
    }
}
