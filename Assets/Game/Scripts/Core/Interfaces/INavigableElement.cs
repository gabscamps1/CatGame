using CatGame.Core.Enums;

namespace CatGame.Core.Interfaces
{
    public interface INavigableElement
    {
        public bool IsInteractable { get; }
        public void OnFocused(PlayerId player);
        public void OnUnfocused(PlayerId player);
        public void OnSubmit(PlayerId player);
        public INavigableElement GetNeightbor(NavigationDirection direction);
    }
}
