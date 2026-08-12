using System;
using System.Collections.Generic;
using UnityEngine;
using CatGame.Core.Interfaces;
using CatGame.Core.Enums;
using CatGame.Core;
using Logger = CatGame.Core.Logger;

namespace CatGame.Services.UISystem
{
    /// <summary>
    /// Gerencia todos as UIs registradas do jogo.
    /// </summary>
    public class UIManager : MonoBehaviour, IUIService
    {
        public static UIManager Instance { get; private set; }

        public event Action<PanelType?> OnCurrentUIChanged;

        // Todos os painéis registrados.
        private readonly Dictionary<PanelType, IBasePanel> panels = new();

        // Grupo de painéis visíveis.
        private readonly Stack<PanelType> panelStack = new();

        private IGameStateService gameStateService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            gameStateService = ServiceLocator.Get<IGameStateService>();
            gameStateService.OnStateChanged += GameStateService_OnStateChanged;
        }

        private void OnDestroy()
        {
            gameStateService.OnStateChanged -= GameStateService_OnStateChanged;
        }

        #region BasePanel Register

        /// <summary>
        /// Registra um painel. 
        /// Substitui um painel atual que estiver registrado pelo novo.
        /// </summary>
        public void RegisterPanel(IBasePanel panel)
        {
            if (panels.ContainsKey(panel.PanelType))
            {
                Logger.LogWarning($"[UIManager] Painel {panel.PanelType} já registrado. Substituindo pela novo painél '{panel.gameObject.name}'.");
            }

            panels[panel.PanelType] = panel;
        }

        /// <summary>
        /// Remove um painél registrado.
        /// </summary>
        public void UnregisterPanel(PanelType type)
        {
            if (panels.ContainsKey(type))
                panels.Remove(type);
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Abre um painel.
        /// </summary>
        public void Push(PanelType type, Action onComplete = null)
        {
            if (!TryGetPanel(type, out var panel)) return;
            if (panelStack.Contains(type))
            {
                Logger.LogWarning($"[UIManager] {type} já está na pilha.");
                return;
            }

            panelStack.Push(type);
            panel.Show(onComplete);

            OnCurrentUIChanged?.Invoke(panelStack.Peek());
        }

        /// <summary>
        /// Fecha o painel do topo do grupo.
        /// </summary>
        public void Pop(Action onComplete = null)
        {
            if (panelStack.Count == 0) return;

            var type = panelStack.Pop();
            if (TryGetPanel(type, out var panel))
                panel.Hide(onComplete);

            OnCurrentUIChanged?.Invoke(panelStack.Count > 0 ? panelStack.Peek() : null);
        }

        /// <summary>
        /// Fecha todos os painéis do grupo até chegar no alvo. 
        /// Se o painel indicado não estiver na pilha, todos os painéis serão fechados.
        /// </summary>
        public void PopTo(PanelType target)
        {
            while (panelStack.Count > 0 && panelStack.Peek() != target)
                Pop();
        }

        /// <summary>
        /// Fecha tudo submitEvent abre apenas este painel.
        /// </summary>
        public void SwitchTo(PanelType type, Action onComplete = null)
        {
            CloseAll();
            Push(type, onComplete);
        }

        /// <summary>
        /// Abre um grupo de painéis.
        /// </summary>
        public void PushMany(params PanelType[] types)
        {
            foreach (var type in types)
                Push(type);
        }

        /// <summary>
        /// Fecha todos os painéis abertos.
        /// </summary>
        public void CloseAll()
        {
            while (panelStack.Count > 0)
                Pop();
        }

        /// <summary>
        /// Fecha um painel específico que esteja no grupo.
        /// </summary>
        public void Close(PanelType type)
        {
            if (!panelStack.Contains(type)) 
                return;

            if (!IsVisible(type))
                return;

            // Reconstrói o grupo sem o painel alvo.
            var temp = new Stack<PanelType>();
            while (panelStack.Count > 0)
            {
                var top = panelStack.Pop();
                if (top == type)
                {
                    if (TryGetPanel(type, out var panel))
                        panel.Hide();
                    break;
                }
                temp.Push(top);
            }

            // Recoloca o que estava acima.
            while (temp.Count > 0)
                panelStack.Push(temp.Pop());
        }

        /// <summary> Retorna se um painel específico está visível. </summary>
        public bool IsVisible(PanelType type) => panelStack.Contains(type);

        /// <summary> Retorna o painel do topo (foco atual) ou null se vazio. </summary>
        public PanelType? CurrentPanel => panelStack.Count > 0 ? panelStack.Peek() : null;

        #endregion

        #region Events

        /// <summary>
        /// Altera os painéis de acordo com o estado do jogo.
        /// </summary>
        private void GameStateService_OnStateChanged(GameState previousState, GameState nextState)
        {
            switch (nextState)
            {
                case GameState.MainMenu:
                    SwitchTo(PanelType.MainMenu);
                    break;

                case GameState.Loading:
                    SwitchTo(PanelType.Loading);
                    break;

                case GameState.Playing:
                    CloseAll();
                    Push(PanelType.HUD);
                    break;

                case GameState.Paused:
                    // Mantém o HUD submitEvent coloca o pause por cima.
                    if (!IsVisible(PanelType.HUD))
                        Push(PanelType.HUD);

                    Push(PanelType.Pause);
                    break;

                default:
                    Logger.LogWarning($"[UIManager] Estado '{nextState}' não foi adicionado");
                    break;
            }
        }

        #endregion

        #region Check

        private bool TryGetPanel(PanelType type, out IBasePanel panel)
        {
            if (panels.TryGetValue(type, out panel)) return true;

            Logger.LogWarning($"[UIManager] Painel '{type}' não está registrado. Verifique se o gameObject que contém esse painel está ativado.");
            return false;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Debug: Listar painéis registrados")]
        private void DebugListPanels()
        {
            Logger.Log($"[UIManager] {panels.Count} painel(is) registrado(s):");
            foreach (var kvp in panels)
                Logger.Log($"{kvp.Key} - {kvp.Value.gameObject.name}");
        }

        [ContextMenu("Debug: Listar painéis na pilha")]
        private void DebugListStack()
        {
            Logger.Log($"[UIManager] {panelStack.Count} item(ns) na pilha:");
            foreach (var type in panelStack)
                Logger.Log($"{type}");
        }
#endif

        #endregion
    }
}
