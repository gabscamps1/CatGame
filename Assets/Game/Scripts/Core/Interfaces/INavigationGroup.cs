using CatGame.Core.Enums;

namespace CatGame.Core.Interfaces
{
    public interface INavigationGroup
    {
        public void Register(INavigableElement element, bool isDefault = false);
        public void SetDefaultForPlayer(PlayerId player, INavigableElement element);
        public INavigableElement GetCurrentElement(PlayerId player);
        public void Enter(PlayerId player);
        public void Exit(PlayerId player);
        public void SetElementFocus(PlayerId player, INavigableElement element);
        public void Navigate(PlayerId player, NavigationDirection direction);
        public void Submit(PlayerId player);
    }
}