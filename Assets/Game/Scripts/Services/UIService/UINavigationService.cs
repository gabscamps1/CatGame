using CatGame.Core.Enums;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Logger = CatGame.Core.Logger;

namespace CatGame.Services.UISystem
{
    public class UINavigationService : IUINavigationService
    {
        public event EventHandler<FocusChangedEvent> OnFocusChanged;

        private readonly Dictionary<PlayerId, Stack<INavigationGroup>> groupsByPlayer = new();
        private readonly HashSet<PlayerId> navigationLockedPlayers = new();
        private readonly HashSet<PlayerId> cancelLockedPlayers = new();

        public void PushGroup(PlayerId player, INavigationGroup group)
        {
            if (group == null) 
                return;

            Stack<INavigationGroup> groups = GetOrCreateStack(player);

            if (groups.Count > 0)
                groups.Peek().Exit(player);

            groups.Push(group);
            group.Enter(player);

            FocusChanged(player, null, group.GetCurrentElement(player));
        }

        public void PopGroup(PlayerId player, INavigationGroup group)
        {
            if (!groupsByPlayer.TryGetValue(player, out Stack<INavigationGroup> groups) || groups.Count == 0)
                return;

            if (groups.Peek() != group)
            {
                Logger.LogWarning("[UINavigationService] Tentativa de PopGroup de um grupo que não está no topo da pilha.");
                return;
            }

            INavigationGroup closingGroup = groups.Pop();
            INavigableElement previousElement = closingGroup.GetCurrentElement(player);
            closingGroup.Exit(player);

            if (groups.Count > 0)
            {
                INavigationGroup resumedGroup = groups.Peek();
                resumedGroup.Enter(player);
                FocusChanged(player, previousElement, resumedGroup.GetCurrentElement(player));
            }
            else
            {
                FocusChanged(player, previousElement, null);
            }
        }

        public void ForceRemoveGroup(PlayerId player, INavigationGroup group)
        {
            if (!groupsByPlayer.TryGetValue(player, out Stack<INavigationGroup> groups) || groups.Count == 0)
                return;

            if (!groups.Contains(group))
                return;

            // Stack não tem "remover do meio" nativo — reconstrói preservando a ordem, só sem
            // esse grupo. ToArray() de um Stack vem do topo pra base, por isso o Reverse().
            INavigationGroup[] remaining = groups.ToArray().Where(g => g != group).Reverse().ToArray();

            groups.Clear();
            foreach (INavigationGroup g in remaining)
                groups.Push(g);

            if (groups.Count == 0)
                groupsByPlayer.Remove(player);
        }

        public INavigationGroup GetActiveGroup(PlayerId player)
        {
            return groupsByPlayer.TryGetValue(player, out Stack<INavigationGroup> groups) && groups.Count > 0
               ? groups.Peek()
               : null;
        }

        public void Navigate(PlayerId player, NavigationDirection direction)
        {
            if (navigationLockedPlayers.Contains(player))
                return;

            if (!groupsByPlayer.TryGetValue(player, out Stack<INavigationGroup> groups) || groups.Count == 0)
                return;            

            INavigationGroup currentGroup = groups.Peek();
            INavigableElement previousElement = currentGroup.GetCurrentElement(player);

            Logger.Log("TryNavigate2");
            currentGroup.Navigate(player, direction);

            INavigableElement currentElement = currentGroup.GetCurrentElement(player);

            if (currentElement != previousElement)
                FocusChanged(player, previousElement, currentElement);
        }

        public void Submit(PlayerId player)
        {
            if (!groupsByPlayer.TryGetValue(player, out Stack<INavigationGroup> groups) || groups.Count == 0)
                return;

            groups.Peek().Submit(player);
        }

        public void Cancel(PlayerId player)
        {
            if (cancelLockedPlayers.Contains(player))
                return;

            if (!groupsByPlayer.TryGetValue(player, out Stack<INavigationGroup> group) || group.Count == 0)
                return;

            group.Peek().Cancel(player);
        }

        private Stack<INavigationGroup> GetOrCreateStack(PlayerId player)
        {
            if (!groupsByPlayer.TryGetValue(player, out Stack<INavigationGroup> groups))
            {
                groups = new Stack<INavigationGroup>();
                groupsByPlayer[player] = groups;
            }

            return groups;
        }

        private void FocusChanged(PlayerId playerId, INavigableElement previous, INavigableElement current)
        {
            OnFocusChanged?.Invoke(this, new FocusChangedEvent()
            {
                PlayerId = playerId,
                Previous = previous,
                Current = current
            });
        }
      
        public void SetNavigationLocked(PlayerId player, bool locked)
        {
            if (locked) navigationLockedPlayers.Add(player);
            else navigationLockedPlayers.Remove(player);
        }

        public bool IsNavigationLocked(PlayerId player)
        {
            return navigationLockedPlayers.Contains(player);
        }

        public void SetCancelLocked(PlayerId player, bool locked)
        {
            if (locked) cancelLockedPlayers.Add(player);
            else cancelLockedPlayers.Remove(player);
        }

        public bool IsCancelLocked(PlayerId player)
        {
            return cancelLockedPlayers.Contains(player);
        }

        
    }
}
