using CatGame.Core.Enums;
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace CatGame.Features.CharacterSelection
{
    public static class LobbyRules
    {
        #region Slots

        /// <summary>
        /// Primeira vaga livre, do menor índice para o maior. -1 se o lobby estiver cheio.
        /// </summary>
        public static int FirstFreeSlot(IReadOnlyList<LobbySlot> slots)
        {
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].Occupied) return i;

            return -1;
        }

        public static bool IsFull(IReadOnlyList<LobbySlot> slots)
        {
            return FirstFreeSlot(slots) < 0;
        }

        public static int FindSlot(IReadOnlyList<LobbySlot> slots, ulong clientId, byte localId)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                LobbySlot slot = slots[i];
                if (slot.Occupied && !slot.IsAI && slot.OwnerId == clientId && slot.LocalId == localId)
                    return i;
            }

            return -1;
        }

        #endregion

        #region Characters

        public static bool CanSelect(IReadOnlyList<LobbySlot> slots, int slot, ushort characterId, CharacterCatalog characterCatalog, bool allowDuplicates)
        {
            if (slot < 0 || slot >= slots.Count) return false;
            if (!slots[slot].Occupied) return false;
            if (slots[slot].Ready) return false; // Já está selecionado.
            if (!characterCatalog.Exists(characterId)) return false;

            if (!allowDuplicates && IsCharacterTaken(slots, characterId, slot))
                return false;

            return true;
        }

        /// <summary>
        /// Personagem inicial de quem entra: o primeiro do catálogo que nenhuma vaga ocupada está usando.
        /// Se todos estiverem em uso, devolve o primeiro do catálogo (personagem repetido é permitido).
        /// </summary>
        public static ushort FirstFreeCharacter(IReadOnlyList<LobbySlot> slots, CharacterCatalog catalog)
        {
            var entries = catalog.Entries;
            ushort fallback = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                ushort id = entries[i].Id;
                if (id == 0) continue;
                if (fallback == 0) fallback = id;
                if (!IsCharacterTaken(slots, id, -1)) return id;
            }

            return fallback;
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

        /// <summary>
        /// Algum outro slot ocupado (diferente de ignoredSlot) já usa este personagem?
        /// </summary>
        private static bool IsCharacterTaken(IReadOnlyList<LobbySlot> slots, ushort characterId, int ignoredSlot)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (i == ignoredSlot) continue;
                if (slots[i].Occupied && slots[i].CharacterId == characterId) return true;
            }

            return false;
        }

        private static int IndexOf(IReadOnlyList<CharacterCatalog.Entry> entries, ushort id)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Id == id) return i;
            return -1;
        }

        #endregion

        #region Ready / Start

        public static bool CanSetReady(IReadOnlyList<LobbySlot> slots, int slot, bool ready)
        {
            if (slot < 0 || slot >= slots.Count) return false;
            if (!slots[slot].Occupied) return false;
            if (!ready) return true;

            return !slots[slot].WaitingDevice && slots[slot].CharacterId != 0;
        }

        public static bool CanStart(IReadOnlyList<LobbySlot> slots)
        {
            int humans = 0;

            foreach (var slot in slots)
            {
                if (!slot.Occupied) continue;

                humans++;
                if (!slot.Ready || slot.WaitingDevice) return false;
            }

            return humans > 0;
        }

        #endregion

        #region AI

        /// <summary>
        /// Coloca uma IA em cada vaga livre, mantendo os índices das vagas.
        /// Usa primeiro os personagens que ninguém escolheu (na ordem do catálogo). Só entram personagens com AIPrefab.
        /// Retorna quantas IAs foram adicionadas.
        /// </summary>
        public static int FillWithAI(List<LobbySlot> slots, CharacterCatalog catalog, ulong serverClientId)
        {
            int added = 0;
            var entries = catalog.Entries;

            for (int slot = 0; slot < slots.Count; slot++)
            {
                if (slots[slot].Occupied) continue;

                ushort characterId = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    CharacterCatalog.Entry entry = entries[i];
                    if (entry.Id == 0 || entry.AIPrefab == null) continue;
                    if (IsCharacterTaken(slots, entry.Id, -1)) continue;

                    characterId = entry.Id;
                    break;
                }

                if (characterId == 0) continue; // Faltam personagens livres com AIPrefab.

                added++;
                slots[slot] = new LobbySlot
                {
                    Occupied = true,
                    OwnerId = serverClientId,
                    LocalId = 0,
                    CharacterId = characterId,
                    Ready = true,
                    PlayerName = new FixedString32Bytes($"IA {added}"),
                    ControlType = PlayerControlType.AI,
                };
            }

            return added;
        }

        public static int CountFreeSlots(IReadOnlyList<LobbySlot> slots)
        {
            int free = 0;
            foreach (var slot in slots)
                if (!slot.Occupied) free++;
            return free;
        }

        #endregion
    }
}
