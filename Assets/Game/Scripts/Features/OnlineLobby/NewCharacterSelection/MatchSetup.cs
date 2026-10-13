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
    }
}
