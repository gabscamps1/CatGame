using UnityEngine;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using CatGame.Core;
using CatGame.Capabilities.UISystem;

namespace CatGame.Features.Settings
{
    public class GameplaySettings : MonoBehaviour
    {
        public string SelectedLanguage => GetLanguageOfValue(languageDropdown.Value);

        [Header("UI")]
        [SerializeField] private DropdownElement languageDropdown;

        private ISaveSettingsService settingsService;
        private ILocalizationService localizationService;

        private void Awake()
        {
            settingsService = ServiceLocator.Get<ISaveSettingsService>();
            localizationService = ServiceLocator.Get<ILocalizationService>();

            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(
                "< English >", 
                "< Português (BR) >", 
                "< Spanish (ES) >", 
                "< Japanese >",
                "< Korean>", 
                "< Chinese >"
   
                );

            // Carrega o save.
            LoadSettings();
        }

        private void Start()
        {
            languageDropdown.OnValueChanged += LanguageDropdown_OnValueChanged;
        }

        private void OnDestroy()
        {
            languageDropdown.OnValueChanged -= LanguageDropdown_OnValueChanged;
        }

        private void LanguageDropdown_OnValueChanged(object sender, DropdownElement.ValueChangedEvent e)
        {
            SetLanguage(e.Index);
        }

        #region Language

        private void SetLanguage(int value)
        {
            string language = GetLanguageOfValue(value);

            if (string.IsNullOrEmpty(language))
            {
                Core.Logger.LogError("Não foi possível selecionar o idioma");
                return;
            }

            Core.Logger.Log(SelectedLanguage);

            localizationService.ChangeLanguage(language);
        }

        private string GetLanguageOfValue(int value)
        {
            string language = value switch
            {
                0 => "English",
                1 => "Portuguese",
                2 => "Spanish",
                3 => "Japanese",
                4 => "Chinese",
                5 => "Korean",
                _ => "English"
            };

            return language;
        }

        private int GetValueOfLanguage(string language)
        {
            int value = language switch
            {
                "English" => 0,
                "Portuguese"  => 1,
                "Spanish"  => 2,
                "Japanese" => 3,
                "Chinese" => 4,
                "Korean" => 5,
                _ => 0,
            };    

            return value;
        }

        #endregion

        private void LoadSettings()
        {
            // Pegar valores salvos ou defaults.
            SettingsData settingsData = settingsService.Load();

            // Atualiza a UI.
            int value = GetValueOfLanguage(settingsData.Language);
            languageDropdown.SetValue(value);
        }
    }
}
