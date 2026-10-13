using CatGame.UI;
using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    [CreateAssetMenu(menuName = "Character/Character Data", fileName = "New Character")]
    public class CharacterData : ScriptableObject
    {
        public string CharacterID => characterID;
        [SerializeField] private string characterID;

        public string CharacterName => characterName;
        [SerializeField] private string characterName;

        public Sprite CharacterSprite => characterSprite;
        [SerializeField] private Sprite characterSprite;

        public GameObject CharacterObject => characterGameObject;
        [SerializeField] private GameObject characterGameObject;

        public GameObject CharacterModel => characterModel;
        [SerializeField] private GameObject characterModel;

        public CharacterSelectionBody CharacterSelectionBody => characterSelectionBody;
        [SerializeField] private CharacterSelectionBody characterSelectionBody;
    }
}