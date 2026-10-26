using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace CatGame.Features.Settings
{
    public class VideoSettings : MonoBehaviour
    {
        public int ResolutionIndex => selectedResolutionIndex;
        public int WindowModeIndex => windowModeDropdown.Value;
        public int VSyncIndex => vSyncToggle.IsOn ? 1 : 0;
        public int VFXIndex => vfxToggle.IsOn ? 1 : 0;

        [Header("UI")]
        [SerializeField] private DropdownElement resolutionDropdown;
        [SerializeField] private DropdownElement windowModeDropdown;
        [SerializeField] private ToggleElement vSyncToggle;
        [SerializeField] private ToggleElement vfxToggle;

        // Mesma ordem das opções do dropdown de modo de janela.
        private static readonly FullScreenMode[] windowModes =
        {
            FullScreenMode.ExclusiveFullScreen, // 0 - Exclusive FullScreen
            FullScreenMode.FullScreenWindow,    // 1 - Fullscreen Window
            FullScreenMode.Windowed             // 2 - Window
        };

        private Resolution[] resolutions;
        private FullScreenMode currentWindowMode;
        private int selectedResolutionIndex;

        private Resolution lastResolution;
        private int lastSystemWidth;
        private int lastSystemHeight;

        private ISaveSettingsService settingsService;

        private void Awake()
        {
            settingsService = ServiceLocator.Get<ISaveSettingsService>();

            currentWindowMode = Screen.fullScreenMode;

            // Resoluções.
            GetResolutions();

            // Índice da resolução escolhida pelo jogador.
            selectedResolutionIndex = Mathf.Max(0, Array.FindIndex(resolutions, r => r.width == Screen.width && r.height == Screen.height));

            windowModeDropdown.ClearOptions();
            windowModeDropdown.AddOptions("< Exclusive FullScreen >", "< Fullscreen Window >", "< Window >");

            LoadSettings();
        }

        private void Start()
        {
            // Ao mudar os valores das configurações, a função correspondente é chamada.
            resolutionDropdown.OnValueChanged += ResolutionDropdown_OnValueChanged;
            windowModeDropdown.OnValueChanged += WindowModeDropdown_OnValueChanged;
            vSyncToggle.OnValueChanged += VSyncToggle_OnValueChanged;

            lastResolution = Screen.currentResolution;
            lastSystemWidth = Screen.mainWindowDisplayInfo.width;
            lastSystemHeight = Screen.mainWindowDisplayInfo.height;
        }

        private void OnDestroy()
        {
            resolutionDropdown.OnValueChanged -= ResolutionDropdown_OnValueChanged;
            windowModeDropdown.OnValueChanged -= WindowModeDropdown_OnValueChanged;
            vSyncToggle.OnValueChanged -= VSyncToggle_OnValueChanged;
        }

        private void GetResolutions()
        {
            // Pega todas as resoluções suportadas.
            Resolution[] allResolutions = Screen.resolutions;

            // Salva cada resolução com a maior taxa de atualização suportada.
            Dictionary<string, Resolution> bestResolutions = new();
            foreach (var resolution in allResolutions)
            {
                string key = resolution.width + "x" + resolution.height; // Chave única (ex: 1920x1080).

                if (!bestResolutions.ContainsKey(key) || bestResolutions[key].refreshRateRatio.value < resolution.refreshRateRatio.value)
                {
                    bestResolutions[key] = resolution; // Atualiza para a resolução com maior taxa de atualização.
                }
            }

            // Posiciona as resoluções da maior para a menor.
            resolutions = bestResolutions.Values
                .OrderBy(x => x.width)
                .ThenBy(x => x.height)
                .ToArray();

            // Adiciona as resoluções no dropdown.
            resolutionDropdown.ClearOptions();
            foreach (Resolution res in resolutions)
                resolutionDropdown.AddOptions($"< {res.width} x {res.height} >");
        }

        private void ResolutionDropdown_OnValueChanged(object sender, DropdownElement.ValueChangedEvent e)
        {
            // Em Fullscreen Window a resolução é fixa.
            if (currentWindowMode == FullScreenMode.FullScreenWindow)
                return;

            selectedResolutionIndex = e.Index;
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

        private void SetResolution(int index)
        {
            // Em Fullscreen Window a resolução é sempre a nativa do monitor.
            if (currentWindowMode == FullScreenMode.FullScreenWindow)
                return;

            if (index < 0 || index >= resolutions.Length)
                return;

            Resolution resolution = resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, currentWindowMode, resolution.refreshRateRatio);
        }

        private void SetWindowMode(int index)
        {
            if (index < 0 || index >= windowModes.Length)
                return;

            currentWindowMode = windowModes[index];

            if (currentWindowMode == FullScreenMode.FullScreenWindow)
            {
                // Usa a resolução nativa do monitor.
                Screen.SetResolution(Screen.mainWindowDisplayInfo.width, Screen.mainWindowDisplayInfo.height, currentWindowMode, Screen.currentResolution.refreshRateRatio);

                int nativeIndex = Array.FindIndex(resolutions, r => r.width == Screen.mainWindowDisplayInfo.width && r.height == Screen.mainWindowDisplayInfo.height);
                
                if (nativeIndex >= 0)
                    resolutionDropdown.SetValue(nativeIndex);
            }
            else
            {
                // Restaura a resolução escolhida pelo jogador.
                resolutionDropdown.SetValue(selectedResolutionIndex);
                resolutionDropdown.SetInteractable(true);
                SetResolution(selectedResolutionIndex);
            }

            resolutionDropdown.SetOptionInteractable(currentWindowMode != FullScreenMode.FullScreenWindow);
        }

        private void SetVSync(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }

        private void SetVFX(bool enabled)
        {
            // Todo - Aplicar.
        }

        private void LoadSettings()
        {
            // Pegar valores salvos ou defaults.
            SettingsData settingsData = settingsService.Load();

            if (settingsService.HasSave())
            {
                // Guarda a resolução escolhida.
                selectedResolutionIndex = Mathf.Clamp(settingsData.Resolution, 0, resolutions.Length - 1);  
            }

            // Atualiza a UI.
            resolutionDropdown.SetValue(selectedResolutionIndex);
            windowModeDropdown.SetValue(settingsData.WindowMode);
            vSyncToggle.SetActivate(settingsData.Vsync == 1);
            vfxToggle.SetActivate(settingsData.VFX == 1);

            // Aplica as configs. SetWindowMode já aplica a resolução quando o modo permite.
            SetWindowMode(settingsData.WindowMode);
            SetVSync(settingsData.Vsync == 1);
            SetVFX(settingsData.Vsync == 1);
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



        #endregion
    }
}
