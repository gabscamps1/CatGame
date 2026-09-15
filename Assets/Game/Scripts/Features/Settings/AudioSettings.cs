using UnityEngine;
using UnityEngine.UI;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using CatGame.Core;

namespace CatGame.SettingsManagement
{
    public class AudioSettings : MonoBehaviour
    {
        public float MasterVolumeValue => masterVolumeSlider.value;
        public float MusicVolumeValue => musicVolumeSlider.value;
        public float SFXVolumeValue => sfxVolumeSlider.value;

        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        private IAudioService audioService;
        private ISettingsService settingsService;

        private void Awake()
        {
            audioService = ServiceLocator.Get<IAudioService>();
            settingsService = ServiceLocator.Get<ISettingsService>();
        }

        private void Start()
        {
            // Carrega o save.
            LoadSettings();

            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        private void SetMasterVolume(float value)
        {
            audioService.SetMasterVolume(value);
        }
        private void SetMusicVolume(float value)
        {
            audioService.SetMusicVolume(value);
        }

        private void SetSFXVolume(float value)
        {
            audioService.SetSFXVolume(value);
        }

        private void LoadSettings()
        {
            // Pegar valores salvos ou defaults.
            SettingsData settingsData = settingsService.Load();

            // Atualiza a UI.
            masterVolumeSlider.value = settingsData.MasterVolume;
            musicVolumeSlider.value = settingsData.MusicVolume;
            sfxVolumeSlider.value = settingsData.SFXVolume;

            // Aplica as configs.
            SetMasterVolume(settingsData.MasterVolume);
            SetMusicVolume(settingsData.MusicVolume);
            SetSFXVolume(settingsData.SFXVolume);
        }
    }
}
