using System.IO;
using UnityEngine;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;

namespace CatGame.Services.Settings
{
    public class SaveSettingsService : ISaveSettingsService
    {
        private const string SaveData = "/configs_state.json";
        private string savePath;

        private SettingsData SettingsDataPattern => new()
        {
            // Video.
            WindowMode = 0,
            Vsync = 0,
            VFX = 1,

            // Geral.
            Language = Application.systemLanguage.ToString(),

            // Audio.
            MasterVolume = 1,
            MusicVolume = 1,
            SFXVolume = 1,
            VoiceVolume = 1,
        };

        public SaveSettingsService()
        {
            savePath = Application.persistentDataPath;
        }

        public void Save(SettingsData data)
        {
            string savePath = this.savePath;

            // Se não houver um diretório para o save, cria um.
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            string jsonConfigsData = JsonUtility.ToJson(data);
            File.WriteAllText(savePath + SaveData, jsonConfigsData);
        }

        public SettingsData Load()
        {
            string savePath = this.savePath;

            if (File.Exists(savePath + SaveData))
            {
                string configsData = File.ReadAllText(savePath + SaveData);
                SettingsData saveConfigsInfos = JsonUtility.FromJson<SettingsData>(configsData);

                return saveConfigsInfos;
            }
            else
            {
                return SettingsDataPattern;
            }
        }

        public bool HasSave()
        {
            return Directory.Exists(savePath) && File.Exists(savePath + SaveData);
        }
    }
}