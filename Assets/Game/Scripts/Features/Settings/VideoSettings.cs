using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using CatGame.Core;
using CatGame.Capabilities.UISystem;

namespace CatGame.SettingsManagement
{
    public class VideoSettings : MonoBehaviour
    {
        public int ResolutionIndex => resolutionDropdown.Value;
        public int WindowModeIndex => windowModeDropdown.Value;
        public int VSyncIndex => vSyncToggle.IsOn ? 1 : 0;

        [Header("UI")]
        [SerializeField] private DropdownElement resolutionDropdown;
        [SerializeField] private DropdownElement windowModeDropdown;
        [SerializeField] private ToggleElement vSyncToggle;

        private Resolution[] resolutions;
        private Resolution lastResolution;
        private int lastSystemWidth;
        private int lastSystemHeight;

        private ISettingsService settingsService;

        private void Awake()
        {
            //settingsService = ServiceLocator.Get<ISettingsService>();

            // Resoluções.
            GetResolutions();

            windowModeDropdown.ClearOptions();
            windowModeDropdown.AddOptions("Exclusive FullScreen", "Fullscreen Window", "Window");

            // Carrega o save.
            //LoadSettings();
        }

        private void Start()
        {
            // Ao mudar os valores das configurações, a função correspondente é chamada.
            resolutionDropdown.OnValueChanged += ResolutionDropdown_OnValueChanged;
            windowModeDropdown.OnValueChanged += WindowModeDropdown_OnValueChanged;
            vSyncToggle.OnValueChanged += VSyncToggle_OnValueChanged;

            lastResolution = Screen.currentResolution;
            lastSystemWidth = Display.main.systemWidth;
            lastSystemHeight = Display.main.systemHeight;
        }       

        private void GetResolutions()
        {
            // Pega todas as resoluções suportadas.
            Resolution[] allResolutions = Screen.resolutions;

            // Salva cada resolução com a maior taxa de atualização suportada.
            Dictionary<string, Resolution> bestResolutions = new();
            foreach (var resolution in allResolutions)
            {
                string key = resolution.width + "x" + resolution.height; // Chave única para cada resolução (ex: 1920x1080).

                if (!bestResolutions.ContainsKey(key) || bestResolutions[key].refreshRateRatio.value < resolution.refreshRateRatio.value)
                {
                    bestResolutions[key] = resolution;  // Atualiza para a resolução com maior taxa de atualização.
                }
            }

            // Posiciona as resoluções da maior para a menor.
            resolutions = bestResolutions.Values.OrderBy(x => x.width).ToArray();

            // Adiciona as resoluções no dropdown.
            foreach (Resolution res in resolutions)           
                resolutionDropdown.AddOptions($"< {res.width} x {res.height} >");
            
        }

        private void ResolutionDropdown_OnValueChanged(object sender, DropdownElement.ValueChangedEvent e)
        {
            SetResolution(e.Index);
        }

        private void WindowModeDropdown_OnValueChanged(object sender, DropdownElement.ValueChangedEvent e)
        {
            SetWindowMode(e.Index);
        }

        private void VSyncToggle_OnValueChanged(object sender, ToggleElement.ValueChangedEvent e)
        {
            SetVSync(e.IsOn);
        }

        

        private (int, int) AdjustToAspect(int width, int height, int aspectIndex)
        {
            float targetRatio = 16f / 9f; // default
            switch (aspectIndex)
            {
                case 0: targetRatio = 16f / 9f; break;   // 16:9
                case 1: targetRatio = 21f / 9f; break;   // 21:9
                case 2: targetRatio = 4f / 3f; break;    // 4:3
                case 3: targetRatio = 16f / 10f; break;  // 16:10
            }

            int newWidth = width;
            int newHeight = Mathf.RoundToInt(newWidth / targetRatio);

            if (newHeight > height)
            {
                newHeight = height;
                newWidth = Mathf.RoundToInt(newHeight * targetRatio);
            }

            return (newWidth, newHeight);
        }

        private void SetResolution(int index)
        {
            //int aspectIndex = aspectRatioDropdown.value;
            int aspectIndex = 0;

            Resolution baseRes = resolutions[index];
            (int width, int height) = AdjustToAspect(baseRes.width, baseRes.height, aspectIndex);

            Screen.SetResolution(width, height, Screen.fullScreenMode, baseRes.refreshRateRatio);
        }

        private void SetAspectRatio(int index)
        {
            Resolution current = Screen.currentResolution;
            (int width, int height) = AdjustToAspect(current.width, current.height, index);

            Screen.SetResolution(width, height, Screen.fullScreenMode, current.refreshRateRatio);
        }

        private void SetWindowMode(int index)
        {
            switch (index)
            {
                case 0: Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
                case 1: Screen.fullScreenMode = FullScreenMode.Windowed; break;
                case 2: Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
            }
        }

        private void SetVSync(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }


        #region NotImplementedYet

        /*private void Update()
        {
            // Verifica se houve mudança de resolução interna da Unity (troca de monitor)
            if (HasMonitorChanged())
            {
                GetResolutions();
                MonitorChanged();
            }
        }*/

        /*private bool HasMonitorChanged()
        {
            bool resolutionChanged = lastResolution.width != Screen.currentResolution.width || lastResolution.height != Screen.currentResolution.height;

            bool systemSizeChanged = lastSystemWidth != Display.main.systemWidth || lastSystemHeight != Display.main.systemHeight;

            if (resolutionChanged || systemSizeChanged)
            {
                lastResolution = Screen.currentResolution;
                lastSystemWidth = Display.main.systemWidth;
                lastSystemHeight = Display.main.systemHeight;
                return true;
            }

            return false;
        }

        private void MonitorChanged()
        {
            Resolution current = Screen.currentResolution;

            // Verifica se resolução atual está na lista do novo monitor.
            bool exists = resolutions.Any(x => x.width == current.width && x.height == current.height);

            if (!exists)
            {
                // Seta a maior resolução suportada do monitor.
                SetResolution(0);
            }
        }*/

        private void LoadSettings()
        {
            // Pegar valores salvos ou defaults.
            SettingsData settingsData = settingsService.Load();

            // Atualiza a UI.
            resolutionDropdown.SetValue(settingsData.Resolution);
            //aspectRatioDropdown.value = settingsData.AspectRatio;
            windowModeDropdown.SetValue(settingsData.WindowMode);
            //vSyncToggle.isOn = settingsData.Vsync == 1;

            // Aplica as configs.
            SetResolution(settingsData.Resolution);
            SetAspectRatio(settingsData.AspectRatio);
            SetWindowMode(settingsData.WindowMode);
            SetVSync(settingsData.Vsync == 1);
        }

        #endregion
    }
}
