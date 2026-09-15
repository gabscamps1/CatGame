using TMPro;
using UnityEngine;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using CatGame.Core;

namespace CatGame.SettingsManagement
{
    public class GraphicsSettings : MonoBehaviour
    {
        public int TextureQualityIndex => textureQualityDropdown.value;
        public int ShadowQualityIndex => shadowQualityDropdown.value;
        public int ModelQualityIndex => modelQualityDropdown.value;
        public int MotionBlurIndex => motionBlurDropdown.value;
        public int PresetIndex => presetDropdown.value;

        [Header("UI")]
        [SerializeField] private TMP_Dropdown presetDropdown;
        [SerializeField] private TMP_Dropdown textureQualityDropdown;
        [SerializeField] private TMP_Dropdown shadowQualityDropdown;
        [SerializeField] private TMP_Dropdown modelQualityDropdown;
        [SerializeField] private TMP_Dropdown motionBlurDropdown;

        private ISettingsService settingsService;

        private void Awake()
        {
            settingsService = ServiceLocator.Get<ISettingsService>();

            textureQualityDropdown.ClearOptions();
            textureQualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "Baixa", "Média", "Alta", "Ultra" });

            shadowQualityDropdown.ClearOptions();
            shadowQualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "Desativado", "Baixa", "Média", "Alta" });

            modelQualityDropdown.ClearOptions();
            modelQualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "Baixo", "Médio", "Alto" });

            motionBlurDropdown.ClearOptions();
            motionBlurDropdown.AddOptions(new System.Collections.Generic.List<string> { "Desligado", "Baixo", "Médio", "Alto" });

            presetDropdown.ClearOptions();
            presetDropdown.AddOptions(new System.Collections.Generic.List<string> { "Baixo", "Médio", "Alto", "Ultra" });

            // Carrega o save.
            LoadSettings();
        }

        private void Start()
        {
            // Ao mudar os valores das configurações, a função correspondente é chamada.
            textureQualityDropdown.onValueChanged.AddListener(SetTextureQuality);
            shadowQualityDropdown.onValueChanged.AddListener(SetShadowQuality);
            modelQualityDropdown.onValueChanged.AddListener(SetModelQuality);
            motionBlurDropdown.onValueChanged.AddListener(SetMotionBlur);
            presetDropdown.onValueChanged.AddListener(SetPreset);
        }

        public void SetTextureQuality(int index)
        {
            index = index == 0 ? 3 : index == 3 ? 0 : index;
            index = index == 1 ? 2 : index == 2 ? 1 : index;

            QualitySettings.globalTextureMipmapLimit = index;
        }

        private void SetShadowQuality(int index)
        {
            switch (index)
            {
                case 0: QualitySettings.shadows = ShadowQuality.Disable; break;
                case 1: QualitySettings.shadows = ShadowQuality.HardOnly; break;
                case 2: QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.Medium; break;
                case 3: QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.VeryHigh; break;
            }
        }

        private void SetModelQuality(int index)
        {
            QualitySettings.lodBias = 0.5f + index * 0.5f;
        }

        private void SetMotionBlur(int index)
        {
            // Precisa de p�s-processamento (exemplo apenas)
            //Debug.Log("Motion Blur n�vel " + index);
        }

        private void SetPreset(int index)
        {
            switch (index)
            {
                case 0: // Baixo.
                    SetTextureQuality(0);
                    SetShadowQuality(0);
                    SetModelQuality(0);
                    SetMotionBlur(0);

                    textureQualityDropdown.value = 0;
                    shadowQualityDropdown.value = 0;
                    modelQualityDropdown.value = 0;
                    motionBlurDropdown.value = 0;
                    break;

                case 1: // Médio.
                    SetTextureQuality(1);
                    SetShadowQuality(1);
                    SetModelQuality(1);
                    SetMotionBlur(1);

                    textureQualityDropdown.value = 1;
                    shadowQualityDropdown.value = 1;
                    modelQualityDropdown.value = 1;
                    motionBlurDropdown.value = 1;
                    break;

                case 2: // Alto.
                    SetTextureQuality(2);
                    SetShadowQuality(2);
                    SetModelQuality(2);
                    SetMotionBlur(2);

                    textureQualityDropdown.value = 2;
                    shadowQualityDropdown.value = 2;
                    modelQualityDropdown.value = 2;
                    motionBlurDropdown.value = 2;
                    break;

                case 3: // Ultra.
                    SetTextureQuality(3);
                    SetShadowQuality(3);
                    SetModelQuality(2);
                    SetMotionBlur(3);

                    textureQualityDropdown.value = 3;
                    shadowQualityDropdown.value = 3;
                    modelQualityDropdown.value = 2;
                    motionBlurDropdown.value = 3;
                    break;
            }
        }

        private void LoadSettings()
        {
            SettingsData settingsData = settingsService.Load();

            // Atualiza a UI.
            textureQualityDropdown.value = settingsData.Texture;
            shadowQualityDropdown.value = settingsData.Shadown;
            modelQualityDropdown.value = settingsData.Model;
            motionBlurDropdown.value = settingsData.MotionBlur;
            presetDropdown.value = settingsData.Preset;

            // Aplica as configs.
            SetShadowQuality(settingsData.Shadown);
            SetModelQuality(settingsData.Model);
            SetTextureQuality(settingsData.Texture);
            SetMotionBlur(settingsData.MotionBlur);
        }
    }
}