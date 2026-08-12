using System;
using UnityEngine;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using Logger = CatGame.Core.Logger;

namespace CatGame.Services.GameStateManagement
{
    /// <summary>
    /// Serviço sobre o estado do jogo.
    /// </summary>
    public class GameStateService : MonoBehaviour, IGameStateService
    {
        public GameState CurrentState { get; private set; } = GameState.Boot;

        /// <summary>
        /// Disparado sempre que o estado do jogo muda. (anterior, novo)
        /// </summary>
        public event Action<GameState, GameState> OnStateChanged;

        public void SetState(GameState newState)
        {
            if (CurrentState == newState)
            {
                Logger.LogWarning($"Mesmo estado foi chamado duas vezes: {newState}");
                return;
            }

            GameState previous = CurrentState;
            CurrentState = newState;

            OnStateChanged?.Invoke(previous, newState);
        }

        public bool IsState(GameState state) => CurrentState == state;

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Debug: Exibe o estado atual do jogo")]
        public void ShowState()
        {
            Logger.Log($"[GameStateService] {CurrentState}");
        }

        [ContextMenu("Debug: Seta o estado atual do jogo para PlayingClicker")]
        public void SetPlayingClickerState()
        {
            SetState(GameState.Playing);
        }

        [ContextMenu("Debug: Seta o estado atual do jogo para Paused")]
        public void SetPausedState()
        {
            SetState(GameState.Paused);
        }
#endif

        #endregion
    }
}
