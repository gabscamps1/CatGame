using System.Collections.Generic;
using CatGame.Core.Interfaces;
using CatGame.Core.Enums;
using System;
using Logger = CatGame.Core.Logger;

namespace CatGame.Services.UISystem
{
    public class UINavigationService : IUINavigationService
    {
        public class FocusChangedEvent : EventArgs
        {
            public PlayerId PlayerId;
            public INavigableElement Previous;
            public INavigableElement Current;
        }

        public event EventHandler OnFocusChanged;

        private readonly Dictionary<PlayerId, Stack<INavigationGroup>> groupsByPlayer = new();

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

        public void Navigate(PlayerId player, NavigationDirection direction)
        {
            if (!groupsByPlayer.TryGetValue(player, out Stack<INavigationGroup> groups) || groups.Count == 0)
            {
                Logger.Log("Teste");
                return;
            }

            INavigationGroup currentGroup = groups.Peek();
            INavigableElement previousElement = currentGroup.GetCurrentElement(player);

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

            Logger.Log($"[UINavigationService] Elemento {current} foi selecionado pelo {playerId}");
        }      
    }
}
