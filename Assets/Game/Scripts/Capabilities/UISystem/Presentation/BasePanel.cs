using CatGame.Core.Interfaces;
using CatGame.Core.Enums;
using CatGame.Core;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    /// <summary>
    /// Classe base para todos os painéis.
    /// </summary>
    public abstract class BasePanel : BaseUIScreen, IBasePanel
    {
        [Header("Painel")]
        [SerializeField] private PanelType panelType;

        public PanelType PanelType => panelType;

        private IUIService uiService;
        private IGameStateService gameStateService;

        protected virtual void Start()
        {
            uiService = ServiceLocator.Get<IUIService>();
            gameStateService = ServiceLocator.Get<IGameStateService>();

            uiService.RegisterPanel(this);
            uiService.OnCurrentUIChanged += UIManager_OnCurrentUIChanged;
            
            gameStateService.OnStateChanged += GameStateService_OnStateChanged;

            // Garante que o código reconheça o evento atual.
            GameStateService_OnStateChanged(gameStateService.CurrentState, gameStateService.CurrentState);

            gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            uiService.UnregisterPanel(panelType);
            uiService.OnCurrentUIChanged -= UIManager_OnCurrentUIChanged;

            gameStateService.OnStateChanged -= GameStateService_OnStateChanged;
        }

        #region Subclass Override

        /// <summary> Chamado ao mudar o estado do jogo. </summary>
        protected virtual void GameStateService_OnStateChanged(GameState previousState, GameState currentState) { }

        /// <summary> Chamado ao mudar a UI de foco. </summary>
        protected virtual void UIManager_OnCurrentUIChanged(PanelType? type) { }

        #endregion
    }
}
