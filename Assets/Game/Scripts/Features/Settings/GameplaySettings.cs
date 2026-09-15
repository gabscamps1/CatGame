using System.Collections.Generic;
using TMPro;
using UnityEngine;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using CatGame.Core;
using CatGame.Core.Events;

namespace CatGame.SettingsManagement
{
    public class GameplaySettings : MonoBehaviour
    {
        public string SelectedLanguage { get; private set; }

        [Header("UI")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        private ISettingsService settingsService;

        private void Awake()
        {
            settingsService = ServiceLocator.Get<ISettingsService>();

            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string> { "English", "Português (BR)" });

            // Carrega o save.
            LoadSettings();
        }

        private void Start()
        {
            // Ao mudar os valores das configurações, a função correspondente é chamada.
            languageDropdown.onValueChanged.AddListener(SetLanguage);
        }

        #region Language
        private void SetLanguage(int value)
        {
            string language = GetLanguageOfValue(languageDropdown.value);

            // Dispara o evento de troca de idioma.
            //EventBus.Publish(new LanguageChangedEvent()
            //{
            //    Language = language,
            //});

            SelectedLanguage = language;

            //Core.Logger.Log(SelectedLanguage);

            /*if (LocalizationManager.Instance.AllLanguages.Length > value)
            {
                LocalizationManager.Instance.ChangeLanguage(value);
                
            }*/
        }

        private string GetLanguageOfValue(int value)
        {
            string language = value switch
            {
                0 => "English",
                1 => "Portuguese",
                _ => "English",
            };

            return language;
        }

        private int GetValueOfLanguage(string language)
        {
            int value = language switch
            {
                "English" => 0,
                "Portuguese"  => 1,
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
            languageDropdown.value = GetValueOfLanguage(settingsData.Language);
        }
    }
}
