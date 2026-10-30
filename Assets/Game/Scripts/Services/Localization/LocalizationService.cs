using CatGame.Core;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CatGame.Services.Localization
{
    [DefaultExecutionOrder(-50)]
    public class LocalizationService : ILocalizationService
    {
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
        public string CurrentLanguage { get; private set; }

        private readonly Dictionary<string, string> languageData = new();
        private readonly ISaveSettingsService settingsService;

        public LocalizationService()
        {
            settingsService = ServiceLocator.Get<ISaveSettingsService>();

            string language = settingsService.Load().Language;
            LoadLanguage(language);
        }

        private string GetLanguagePath(string language)
        {
            string langCode = language switch
            {
                "English" => "en.json",
                "Portuguese" => "pt.json",
                "Spanish" => "es.json",
                "Japanese" => "jp.json",
                _ => "en.json"
            };

            string path = Path.Combine(Application.streamingAssetsPath, "Languages/" + langCode);

            return path;
        }

        private void LoadLanguage(string language)
        {
            string path = GetLanguagePath(language);

            if (!File.Exists(path))
            {
                Core.Logger.LogWarning($"Path [{path}] não existe.");
                return;
            }

            string json = File.ReadAllText(path);
            LanguageFile file = JsonUtility.FromJson<LanguageFile>(json);

            languageData.Clear();

            foreach (var entry in file.text)
            {
                languageData[entry.key] = entry.value;
            }

            OnLocalizationChanged?.Invoke();
        }

        public string GetTextByID(string key)
        {
            if (languageData.TryGetValue(key, out string value))
            {
                return value;
            }

            return "Missing[" + key + "]";
        }

        public void ChangeLanguage(string language)
        {
            CurrentLanguage = language;
            LoadLanguage(language);
        }
    }
}