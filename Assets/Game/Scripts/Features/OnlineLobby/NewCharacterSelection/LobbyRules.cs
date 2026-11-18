using CatGame.Core.Enums;
using System;
using System.Collections.Generic;
using Unity.Collections;

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

        /// <summary>
        /// Completa os participantes com IAs até o limite da sessão, usando os personagens que ninguém escolheu (na ordem do catálogo).
        /// Conta participantes, não clients: um client com mais de um jogador ocupa mais de uma vaga.
        /// Retorna quantas IAs foram adicionadas. Temporário até a IA ser escolhida manualmente no lobby.
        /// </summary>
        public static int FillWithAI(List<LobbySlot> participants, int maxParticipants, CharacterCatalog catalog, ulong serverClientId)
        {
            int freeSlots = maxParticipants - participants.Count;
            int added = 0;

            var entries = catalog.Entries;
            for (int i = 0; i < entries.Count && added < freeSlots; i++)
            {
                CharacterCatalog.Entry entry = entries[i];
                if (entry.Id == 0 || entry.AIPrefab == null) continue;
                if (IsCharacterTaken(participants, entry.Id)) continue;

                added++;
                participants.Add(new LobbySlot
                {
                    OwnerId = serverClientId,
                    LocalId = 0,
                    CharacterId = entry.Id,
                    Ready = true,
                    PlayerName = new FixedString32Bytes($"IA {added}"),
                    ControlType = PlayerControlType.AI,
                });
            }

            return added;
        }

        private static bool IsCharacterTaken(IReadOnlyList<LobbySlot> participants, ushort characterId)
        {
            for (int i = 0; i < participants.Count; i++)
                if (participants[i].CharacterId == characterId) return true;
            return false;
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