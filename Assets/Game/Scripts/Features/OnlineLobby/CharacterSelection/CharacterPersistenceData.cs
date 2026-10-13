using CatGame.Core.Data;
using System.Collections.Generic;
using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    public static class CharacterPersistenceData
    {
        private static readonly Dictionary<PlayerId, CharacterData> charactersSelected = new();

        public static void AssignCharacterToPlayer(PlayerId playerId, CharacterData character)
        {
            charactersSelected[playerId] = character;
            Debug.Log($"Foi selecionado {character.CharacterName}");
        }

        public static void UnassignCharacterToPlayer(PlayerId playerId)
        {
            charactersSelected.Remove(playerId);
        }

        public static CharacterData GetCharacterData(PlayerId playerId)
        {
            return charactersSelected.GetValueOrDefault(playerId);
        }
    }
}