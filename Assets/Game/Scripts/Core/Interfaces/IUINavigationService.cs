using CatGame.Core.Enums;
using System;

namespace CatGame.Core.Interfaces
{
    public interface IUINavigationService
    {
        public event EventHandler OnFocusChanged;
        public void PushGroup(PlayerId player, INavigationGroup group);
        public void PopGroup(PlayerId player, INavigationGroup group);
        public void Navigate(PlayerId player, NavigationDirection direction);
        public void Submit(PlayerId player);
    }
}
