using CatGame.Core.Enums;

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