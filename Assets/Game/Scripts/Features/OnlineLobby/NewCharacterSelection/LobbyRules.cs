using System;
using System.Collections.Generic;

namespace CatGame.Features.CharacterSelection
{
    public static class LobbyRules
    {
        public static bool CanSelect(IReadOnlyList<LobbySlot> slots, int slot, ushort characterId, CharacterCatalog characterCatalog, bool allowDuplicates)
        {
            if (slot < 0 || slot >= slots.Count) return false;
            if (slots[slot].Ready) return false; // Já está selecionado.
            if (!characterCatalog.Exists(characterId)) return false;

            if (!allowDuplicates)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    // Se alguem já estiver selecionado, não permite selecionar.
                    if (i != slot && slots[i].CharacterId == characterId /*&& slots[i].Ready*/)
                        return false;
                }              
            }

            return true;
        }

        public static bool CanSetReady(IReadOnlyList<LobbySlot> slots, int slot, bool ready)
        {
            if (slot < 0 || slot >= slots.Count) return false;
            return !ready || slots[slot].CharacterId != 0;
        }

        public static bool CanStart(IReadOnlyList<LobbySlot> slots)
        {
            if (slots.Count == 0 /*|| slots.Count == 1*/) 
                return false;

            foreach (var slot in slots)
                if (!slot.Ready) return false;

            return true;
        }


        public static ushort NextAvailable(IReadOnlyList<LobbySlot> slots, int slot, int direction,
                                       CharacterCatalog catalog, bool allowDuplicates,
                                       Func<ushort, bool> isUnlocked = null)
        {
            var entries = catalog.Entries;
            int n = entries.Count;
            if (n == 0) return 0;

            int start = IndexOf(entries, slots[slot].CharacterId);
            if (start < 0) start = direction > 0 ? -1 : n;

            for (int step = 1; step <= n; step++)
            {
                int i = ((start + direction * step) % n + n) % n;
                ushort id = entries[i].Id;

                if (!CanSelect(slots, slot, id, catalog, allowDuplicates)) continue;
                if (isUnlocked != null && !isUnlocked(id)) continue;
                return id;
            }
            return 0;
        }

        static int IndexOf(IReadOnlyList<CharacterCatalog.Entry> entries, ushort id)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Id == id) return i;
            return -1;
        }
    }
}