using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using CatGame.Features.CharacterSelection;
using CatGame.Features.Gameplay;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.UI
{
    /// <summary>
    /// Tela que aparece depois da vitória. Somente o jogador local 0 de cada máquina responde.
    /// Host leva todos de volta ao lobby. Client abre a popup para sair do grupo.
    /// </summary>
    public class MatchEndUI : BasePanel
    {
        private const int MAIN_LOCAL_ID = 0;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private string hostTextID = "MATCH_END_HOST";
        [SerializeField] private string clientTextID = "MATCH_END_CLIENT";

        [Header("Match")]
        [SerializeField] private MatchDespawner matchDespawner;

        private IUIService uiService;
        private IInputService inputService;
        private ILocalizationService localizationService;
        private IPlayerInputController mainInput;

        private int lastUIChangedFrame = -1;
        private bool isReturningToLobby;

        #region Unity

        protected override void Start()
        {
            uiService = ServiceLocator.Get<IUIService>();
            inputService = ServiceLocator.Get<IInputService>();
            localizationService = ServiceLocator.Get<ILocalizationService>();
            localizationService.OnLocalizationChanged += RefreshText;

            base.Start();
        }

        protected override void OnDestroy()
        {
            UnbindInput();

            if (localizationService != null)
                localizationService.OnLocalizationChanged -= RefreshText;

            base.OnDestroy();
        }

        #endregion

        #region Show And Hide

        protected override void OnBeforeShow()
        {
            // Durante a partida o input está no mapa de gameplay e o Cancel fica no mapa de UI.
            inputService.SwitchToUI();

            RefreshText();
            BindInput();
        }

        protected override void OnBeforeHide()
        {
            UnbindInput();
        }

        protected override void UIManager_OnCurrentUIChanged(PanelType? type)
        {
            // O toque que fechou a popup não pode abrir ela de novo.
            lastUIChangedFrame = Time.frameCount;
        }

        #endregion

        #region Input

        private void BindInput()
        {
            UnbindInput();

            mainInput = inputService.GetInputFromLocalPlayer(MAIN_LOCAL_ID);

            if (mainInput != null)
                mainInput.OnCancelled += MainInput_OnCancelled;
        }

        private void UnbindInput()
        {
            if (mainInput != null)
                mainInput.OnCancelled -= MainInput_OnCancelled;

            mainInput = null;
        }

        private void MainInput_OnCancelled()
        {
            if (Time.frameCount == lastUIChangedFrame)
                return;

            // Outra tela está na frente, como a popup do client.
            if (!uiService.CurrentPanelType.HasValue || uiService.CurrentPanelType.Value != PanelType.MatchEnd)
                return;

            if (IsServer())
                ReturnToLobby();
            else
                uiService.Push(PanelType.LeaveMatchPopup);
        }

        #endregion

        #region Actions

        private void ReturnToLobby()
        {
            if (isReturningToLobby)
                return;

            isReturningToLobby = true;

            if (matchDespawner != null)
                matchDespawner.DespawnMatchObjects();
            else
                Core.Logger.LogWarning($"[{nameof(MatchEndUI)}] {nameof(MatchDespawner)} não foi configurado, os objetos da partida podem ir para o lobby.");

            if (MatchSetup.Instance != null)
                MatchSetup.Instance.BeginLobbyReturn();

            ServiceLocator.Get<ISceneLoadingService>().LoadOnlineAsyncScene(Scenes.MainMenu);
        }

        private void RefreshText()
        {
            if (actionText != null)
                actionText.text = localizationService.GetTextByID(IsServer() ? hostTextID : clientTextID);
        }

        private static bool IsServer()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        }

        #endregion
    }
}
