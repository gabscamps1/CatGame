using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using UnityEngine;

namespace CatGame.Features.Settings
{
    public class AudioSettings : MonoBehaviour
    {
        public float MasterVolumePercentage => masterVolumeSlider.Value / masterVolumeSlider.MaxValue;
        public float MusicVolumePercentage => musicVolumeSlider.Value / musicVolumeSlider.MaxValue;
        public float SFXVolumePercentage => sfxVolumeSlider.Value / sfxVolumeSlider.MaxValue;
        public float VoiceVolumePercentage => voiceVolumeSlider.Value / voiceVolumeSlider.MaxValue;

        [SerializeField] private SliderElement masterVolumeSlider;
        [SerializeField] private SliderElement musicVolumeSlider;
        [SerializeField] private SliderElement sfxVolumeSlider;
        [SerializeField] private SliderElement voiceVolumeSlider;

        private IAudioService audioService;
        private ISaveSettingsService settingsService;

        private void Awake()
        {
            audioService = ServiceLocator.Get<IAudioService>();
            settingsService = ServiceLocator.Get<ISaveSettingsService>();
        }

        private void Start()
        {
            // Carrega o save.
            LoadSettings();

            masterVolumeSlider.OnValueChanged += MasterVolumeSlider_OnValueChanged;
            musicVolumeSlider.OnValueChanged += MusicVolumeSlider_OnValueChanged;
            sfxVolumeSlider.OnValueChanged += SfxVolumeSlider_OnValueChanged;
            voiceVolumeSlider.OnValueChanged += VoiceVolumeSlider_OnValueChanged;
        }

        private void OnDestroy()
        {
            masterVolumeSlider.OnValueChanged -= MasterVolumeSlider_OnValueChanged;
            musicVolumeSlider.OnValueChanged -= MusicVolumeSlider_OnValueChanged;
            sfxVolumeSlider.OnValueChanged -= SfxVolumeSlider_OnValueChanged;
            voiceVolumeSlider.OnValueChanged -= VoiceVolumeSlider_OnValueChanged;
        }

        private void MasterVolumeSlider_OnValueChanged(object sender, SliderElement.ValueChangedEvent e)
        {
            SetMasterVolume(e.Value);
        }
        private void MusicVolumeSlider_OnValueChanged(object sender, SliderElement.ValueChangedEvent e)
        {
            SetMusicVolume(e.Value);
        }

        private void SfxVolumeSlider_OnValueChanged(object sender, SliderElement.ValueChangedEvent e)
        {
            SetSFXVolume(e.Value);
        }

        private void VoiceVolumeSlider_OnValueChanged(object sender, SliderElement.ValueChangedEvent e)
        {
            SetVoiceVolume(e.Value);
        }

        private void SetMasterVolume(float value)
        {
            float porcentage = value / masterVolumeSlider.MaxValue;
            audioService.SetMasterVolume(porcentage);
        }
        private void SetMusicVolume(float value)
        {
            float porcentage = value / musicVolumeSlider.MaxValue;
            audioService.SetMusicVolume(porcentage);
        }

        private void SetSFXVolume(float value)
        {
            float porcentage = value / sfxVolumeSlider.MaxValue;
            audioService.SetSFXVolume(porcentage);
        }

        private void SetVoiceVolume(float value)
        {
            float porcentage = value / voiceVolumeSlider.MaxValue;
            audioService.SetVoiceVolume(porcentage);
        }

        private void LoadSettings()
        {
            // Pegar valores salvos ou defaults.
            SettingsData settingsData = settingsService.Load();

            float masterValue = settingsData.MasterVolume * masterVolumeSlider.MaxValue;
            float musicValue = settingsData.MusicVolume * musicVolumeSlider.MaxValue;
            float sfxValue = settingsData.SFXVolume * sfxVolumeSlider.MaxValue;
            float voiceValue = settingsData.VoiceVolume * voiceVolumeSlider.MaxValue;

            // Atualiza a UI.
            masterVolumeSlider.SetValue(masterValue);
            musicVolumeSlider.SetValue(musicValue);
            sfxVolumeSlider.SetValue(sfxValue);
            voiceVolumeSlider.SetValue(voiceValue);

            // Aplica as configs.
            SetMasterVolume(masterValue);
            SetMusicVolume(musicValue);
            SetSFXVolume(sfxValue);
            SetVoiceVolume(voiceValue);
        }
    }
}
