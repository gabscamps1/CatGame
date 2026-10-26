using UnityEngine;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using CatGame.Core;
using CatGame.Capabilities.UISystem;

namespace CatGame.Features.Settings
{
    public class SettingsController : BaseUIScreen
    {
        [Header("References")]
        [SerializeField] private VideoSettings videoSettings;
        [SerializeField] private GameplaySettings gameplaySettings;
        [SerializeField] private AudioSettings audioSettings;

        private ISaveSettingsService settingsService;

        private void Awake()
        {
            settingsService = ServiceLocator.Get<ISaveSettingsService>();
        }

        protected override void OnBeforeHide()
        {
            SettingsData settingsData = new()
            {
                WindowMode = videoSettings.WindowModeIndex,
                Resolution = videoSettings.ResolutionIndex,
                Vsync = videoSettings.VSyncIndex,
                VFX = videoSettings.VFXIndex,

                Language = gameplaySettings.SelectedLanguage,

                MasterVolume = audioSettings.MasterVolumePercentage,
                MusicVolume = audioSettings.MusicVolumePercentage,
                SFXVolume = audioSettings.SFXVolumePercentage,
                VoiceVolume = audioSettings.VoiceVolumePercentage,
            };

            // Faz o save das configurações ao desativar o menu de opções.
            settingsService.Save(settingsData);
        }
    }
}
