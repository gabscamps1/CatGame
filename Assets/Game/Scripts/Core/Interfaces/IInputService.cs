using CatGame.Core.Data;

namespace CatGame.Core.Interfaces
{
    public interface IInputService
    {
        public IPlayerInputController GetInputFromPlayer(PlayerId playerId);
        public void SwitchToGame();
        public void SwitchToUI();
        public void DisableAllActions();
    }
}