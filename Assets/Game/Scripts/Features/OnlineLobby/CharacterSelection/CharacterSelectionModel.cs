using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using CatGame.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    public class CharacterSelectionModel
    {
        private readonly CharacterData[] characterDatas;
        private readonly Dictionary<PlayerId, CharacterData> charactersSelected = new();
        private readonly Transform firstCharacterSpawnPosition;
        private readonly Transform secondCharacterSpawnPosition;

        private Dictionary<PlayerId, (IPool<CharacterSelectionBody> , CharacterSelectionBody)> activeCharacters = new();

        public CharacterSelectionModel(CharacterData[] characterDatas, Transform firstCharacterSpawnPosition, Transform secondCharacterSpawnPosition)
        {
            this.characterDatas = characterDatas;
            this.firstCharacterSpawnPosition = firstCharacterSpawnPosition;
            this.secondCharacterSpawnPosition = secondCharacterSpawnPosition;
        }

        public void ShowCharacter(PlayerId player, CharacterModel character)
        {
            HideCharacter(player);

            if (!TryGetDataByID(character.CharacterID, out CharacterData data))
                return;

            CharacterSelectionBody characterTransformPrefab = data.CharacterSelectionBody; 
            IPool<CharacterSelectionBody> pool = ServiceLocator.Get<IObjectPoolingService>().GetOrCreatePool(characterTransformPrefab, 0);

            Transform characterSpawnPosition = player == new PlayerId(0)
                ? firstCharacterSpawnPosition 
                : secondCharacterSpawnPosition;

            CharacterSelectionBody characterBody = pool.Dequeue(characterSpawnPosition);
            characterBody.transform.localPosition = Vector3.zero;

            characterBody.OnCharacterSelect();

            (IPool<CharacterSelectionBody>, CharacterSelectionBody) pair = (pool, characterBody);
            activeCharacters[player] = pair;
        }

        private void HideCharacter(PlayerId player)
        {
            if (!activeCharacters.TryGetValue(player, out (IPool<CharacterSelectionBody>, CharacterSelectionBody) pair))
                return;

            pair.Item1.Enqueue(pair.Item2);   
            
            activeCharacters.Remove(player);
        }

        public void AssignCharacterToPlayer(PlayerId playerId, CharacterModel character)
        {
            if (!TryGetDataByID(character.CharacterID, out CharacterData data))
                return;

            CharacterPersistenceData.AssignCharacterToPlayer(playerId, data);
            charactersSelected[playerId] = data;
        }

        public void UnassignCharacterToPlayer(PlayerId playerId)
        {
            CharacterPersistenceData.UnassignCharacterToPlayer(playerId);
            charactersSelected.Remove(playerId);
        }

        public bool IsAssignCharacterToPlayer(PlayerId playerId)
        {
            return charactersSelected.ContainsKey(playerId);
        }

        public bool IsAllCharactersSelected()
        {
            return charactersSelected.ContainsKey(new PlayerId(0)) 
                && charactersSelected.ContainsKey(new PlayerId(1));
        }

        private bool TryGetDataByID(string characterId, out CharacterData data)
        {
            data = characterDatas.ToList().Find(x => x.CharacterID == characterId);

            if (data == null)
                return false;

            return true;
        }
    }
}