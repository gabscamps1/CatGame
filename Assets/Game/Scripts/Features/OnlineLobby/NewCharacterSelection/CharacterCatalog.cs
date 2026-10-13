using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    [CreateAssetMenu(menuName = "Game/Character Data", fileName = "New Character")]
    public class CharacterCatalog : ScriptableObject
    {       
        [Serializable]
        public struct Entry
        {
            public ushort Id => id;
            [SerializeField] private ushort id;

            public string DisplayName => displayName;
            [SerializeField] private string displayName;

            public Sprite Portrait => portrait;
            [SerializeField] private Sprite portrait;

            public NetworkObject Prefab => prefab;
            [SerializeField] private NetworkObject prefab;
        }

        public IReadOnlyList<Entry> Entries => entries;
        [SerializeField] private Entry[] entries;

        public bool TryGet(ushort id, out Entry entry)
        {
            if (id != 0)
            {
                foreach (var currentEntry in entries)
                {
                    if (currentEntry.Id == id)
                    {
                        entry = currentEntry;
                        return true;
                    }
                }                
            }

            entry = default;
            return false;
        }

        public bool Exists(ushort id) => TryGet(id, out _);
    }
}