using CatGame.Core;
using CatGame.Core.Interfaces;
using TMPro;
using UnityEngine;

namespace CatGame.Features.Localization
{
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] TextMeshProUGUI text;

        private ILocalizationService localizationService;

        private void Awake()
        {
            localizationService = ServiceLocator.Get<ILocalizationService>();

            if (text != null)
                localizationService.OnLocalizationChanged += LocalizationManager_OnLocalizationChanged;
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            localizationService.OnLocalizationChanged += LocalizationManager_OnLocalizationChanged;
        }
#endif
        private void OnDestroy()
        {
            localizationService.OnLocalizationChanged -= LocalizationManager_OnLocalizationChanged;
        }

        private void Start()
        {
            if (text != null)
                LocalizationManager_OnLocalizationChanged();
        }

        private void LocalizationManager_OnLocalizationChanged()
        {
            text.text = localizationService.GetTextByID(id);
        }
    }
}