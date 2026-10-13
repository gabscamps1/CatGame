using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    public class CharacterSelectionRoot : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CharacterData[] characterDatas;
        [SerializeField] private CharacterView[] characterViews;
        [SerializeField] private CharacterSelectionView characterSelectionView;

        [Header("RawImages")]
        [SerializeField] private Transform firstPlayerSpawnTransform;
        [SerializeField] private Transform secondPlayerSpawnTransform;

        private CharacterSelectionPresenter characterSelectionPresenter;

        private void Awake()
        {
            CharacterSelectionModel model = new CharacterSelectionModel(characterDatas,
                firstPlayerSpawnTransform,
                secondPlayerSpawnTransform);

            CharacterSelectionPresenter presenter = new CharacterSelectionPresenter(model, 
                characterSelectionView, 
                characterDatas, 
                characterViews);
        }
    }
}