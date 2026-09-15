using UnityEngine;
using UnityEngine.InputSystem;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using CatGame.Core;

namespace CatGame.SettingsManagement
{
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] private VideoSettings videoSettings;
        [SerializeField] private GraphicsSettings graphicsSettings;
        [SerializeField] private GameplaySettings gameplaySettings;
        [SerializeField] private AudioSettings audioSettings;
        [SerializeField] private InputActionAsset actions;
        private string inputs;

        private bool canMakeSave;
        private ISettingsService settingsService;

        private void Awake()
        {
            //settingsService = ServiceLocator.Get<ISettingsService>();
        }

        private void Start()
        {
            /*string inputsData = settingsService.Load().Inputs;
            if (!string.IsNullOrEmpty(inputsData))
                actions.LoadBindingOverridesFromJson(inputsData);*/
        }

        private void OnDisable()
        {
            if (canMakeSave == false)
            {
                canMakeSave = true;
                return;
            }

            //inputs = actions.SaveBindingOverridesAsJson();
            SettingsData settingsData = new()
            {
                Resolution = videoSettings.ResolutionIndex,
                //AspectRatio = videoSettings.AspectRationIndex,
                //WindowMode = videoSettings.WindowModeIndex,
                //Vsync = videoSettings.VSyncIndex,

                //Texture = graphicsSettings.TextureQualityIndex,
                //Shadown = graphicsSettings.ShadowQualityIndex,
                //Model = graphicsSettings.ModelQualityIndex,
                //MotionBlur = graphicsSettings.MotionBlurIndex,
                //Preset = graphicsSettings.PresetIndex,

                //Language = gameplaySettings.SelectedLanguage,

                //MasterVolume = audioSettings.MasterVolumeValue,
                //MusicVolume = audioSettings.MusicVolumeValue,
                //SFXVolume = audioSettings.SFXVolumeValue,

                //Inputs = inputs,
            };

            // Faz o save das configurações ao desativar o menu de opções.
            //settingsService.Save(settingsData);
        }
    }
}
