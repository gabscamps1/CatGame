using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CatGame.Core.Events;

namespace CatGame.Features.Localization
{
    [DefaultExecutionOrder(-50)]
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance => languageManager;
        private static LocalizationManager languageManager;

        [Serializable]
        private class LanguageEntry
        {
            public string key;
            public string value;
        }

        [Serializable]
        private class LanguageFile
        {
            public List<LanguageEntry> text;
        }

        // Events.
        public event Action OnLocalizationChanged;

        private string currentLanguage;
        private Dictionary<string, string> languageData = new();

        //private ISettingsService settingsService;

        private void Awake()
        {
            if (languageManager == null)
            {
                languageManager = this;
            }
            else if (languageManager != this)
            {
                Destroy(gameObject);
                return;
            }

            //settingsService = Services.Get<ISettingsService>();

            //currentLanguage = settingsService.Load().Language;
            LoadLanguage();

            // Se inscreve no evento da troca de idioma.
            //EventBus.Subscribe<LanguageChangedEvent>(ChangeLanguage);
        }

        private void OnDisable()
        {
            //EventBus.Unsubscribe<LanguageChangedEvent>(ChangeLanguage);
        }

        private string GetLanguagePath()
        {
            string langCode = currentLanguage switch
            {
                "Portuguese" => "pt.json",
                _ => "en.json"
            };

            string path = Path.Combine(Application.streamingAssetsPath, "Languages/" + langCode);

            return path;
        }

        private void LoadLanguage()
        {
            string path = GetLanguagePath();
            Core.Logger.Log(path);

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                LanguageFile file = JsonUtility.FromJson<LanguageFile>(json);

                languageData.Clear();

                foreach (var entry in file.text)
                {
                    languageData[entry.key] = entry.value;
                }

                OnLocalizationChanged?.Invoke();
            }
        }

        /*private void ChangeLanguage(LanguageChangedEvent langEvent)
        {
            currentLanguage = langEvent.Language;
            LoadLanguage();
        }*/

        public string GetTextByID(string key)
        {
            if (languageData.TryGetValue(key, out string value))
            {
                return value;
            }

            return "Missing[" + key + "]";
        }
    }
}