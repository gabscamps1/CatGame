using CatGame.Core.Enums;
using CatGame.Core.Events;
using System;

namespace CatGame.Core.Interfaces
{
    public interface IUINavigationService
    {
        public event EventHandler<FocusChangedEvent> OnFocusChanged;
        public void PushGroup(PlayerId player, INavigationGroup group);
        public void PopGroup(PlayerId player, INavigationGroup group);
        public void ForceRemoveGroup(PlayerId player, INavigationGroup group);
        public INavigationGroup GetActiveGroup(PlayerId player);

        public void Navigate(PlayerId player, NavigationDirection direction);
        public void Submit(PlayerId player);
        public void Cancel(PlayerId player);
         
        /// <summary> Bloqueia/libera a navegação pra esse jogador especificamente. </summary>
        public void SetNavigationLocked(PlayerId player, bool locked);
        public bool IsNavigationLocked(PlayerId player);
         
        /// <summary> Bloqueia/libera o cancelamento pra esse jogador especificamente. </summary>
        public void SetCancelLocked(PlayerId player, bool locked);
        public bool IsCancelLocked(PlayerId player);
    }
}
