using System;
using CatGame.Core.Enums;

namespace CatGame.Core.Interfaces
{
    public interface IGameStateService
    {
        public event Action<GameState, GameState> OnStateChanged;
        public GameState CurrentState { get; }
        public void SetState(GameState gameState);
        public bool IsState(GameState gameState);
    }
}
