using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    public class CharacterModel
    {
        public readonly string CharacterID;
        public readonly string CharacterName;
        public readonly Sprite CharacterSprite;
        
        public CharacterModel(string characterID, string characterName, Sprite characterSprite)
        {
            CharacterID = characterID;
            CharacterName = characterName;
            CharacterSprite = characterSprite;
        }
    }
}