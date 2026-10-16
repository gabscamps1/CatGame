using TMPro;
using UnityEngine;

namespace CatGame.Features.Localization
{
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] TextMeshProUGUI text;

        private void Awake()
        {
            if (text != null)
                LocalizationManager.Instance.OnLocalizationChanged += LocalizationManager_OnLocalizationChanged;
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            LocalizationManager.Instance.OnLocalizationChanged += LocalizationManager_OnLocalizationChanged;
        }
#endif
        private void OnDestroy()
        {
            LocalizationManager.Instance.OnLocalizationChanged -= LocalizationManager_OnLocalizationChanged;
        }

        private void Start()
        {
            if (text != null)
                LocalizationManager_OnLocalizationChanged();
        }

        private void LocalizationManager_OnLocalizationChanged()
        {
            text.text = LocalizationManager.Instance.GetTextByID(id);
        }
    }
}