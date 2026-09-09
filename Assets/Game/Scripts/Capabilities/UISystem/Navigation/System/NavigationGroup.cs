using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Logger = CatGame.Core.Logger;

namespace CatGame.Capabilities.UISystem
{
    public class NavigationGroup : INavigationGroup
    {
        private readonly NavigationMode navigationMode;

        private readonly HashSet<INavigableElement> elements = new();

        // Per Player.
        private readonly Dictionary<PlayerId, INavigableElement> currentByPlayer = new();
        private readonly Dictionary<PlayerId, INavigableElement> defaultByPlayer = new();
        private readonly HashSet<PlayerId> activePlayers = new();

        // Shared.
        private INavigableElement defaultElement;
        private INavigableElement sharedCurrent;

        public event EventHandler<PlayerId> OnCancelRequested;

        public NavigationGroup(NavigationMode navigationMode = NavigationMode.Shared)
        {
            this.navigationMode = navigationMode;
        }

        public void Register(INavigableElement element, bool isDefault = false)
        {
            if (element == null || elements.Contains(element)) 
                return;

            elements.Add(element);

            if (isDefault || defaultElement == null)
                defaultElement = element;
        }

        /// <summary>
        /// Somente funciona no NavigationMode=PerPlayer.
        /// </summary>
        public void SetDefaultForPlayer(PlayerId player, INavigableElement element)
        {
            defaultByPlayer[player] = element;
        }

        public INavigableElement GetCurrentElement(PlayerId player)
        {
            return navigationMode == NavigationMode.Shared
                ? sharedCurrent // Shared
                : currentByPlayer.GetValueOrDefault(player); // PerPlayer
        }

        /// <summary>
        /// Reseta o elemento selecionado para o default.
        /// </summary>
        public void ResetFocusToDefault(PlayerId player)
        {
            INavigableElement target = navigationMode == NavigationMode.Shared
                ? defaultElement
                : defaultByPlayer.GetValueOrDefault(player) ?? defaultElement;

            if (target == null)
            {
                Logger.LogWarning($"[NavigationGroup] Não foi possível resetar a seleção para default");
                return;
            }

            SetElementFocus(player, target);
        }

        public void Enter(PlayerId player)
        {
            if (!activePlayers.Add(player))
                return;

            switch (navigationMode)
            {
                case NavigationMode.Shared:

                    sharedCurrent ??= defaultElement;
                    sharedCurrent?.OnFocused(player, navigationMode); // Não foca através do método SetElementFocus() porque ao abrir o menu é necessário focar no elemento atual. O método impede isso.             
                    break;

                case NavigationMode.PerPlayer:

                    INavigableElement startPerPlayerElement = currentByPlayer.GetValueOrDefault(player)
                    ?? defaultByPlayer.GetValueOrDefault(player)
                    ?? defaultElement;

                    currentByPlayer[player] = startPerPlayerElement;
                    startPerPlayerElement?.OnFocused(player, navigationMode);  // Não foca através do método SetElementFocus() porque ao abrir o menu é necessário focar no elemento atual. O método impede isso.
                    break;

                default:
                    Logger.LogWarning("[NavigationGroup] Enum não foi adicionado");
                    break;
            }
        }

        public void Exit(PlayerId player)
        {
            activePlayers.Remove(player);

            switch (navigationMode)
            {
                case NavigationMode.Shared:

                    // Só desfoca quando o último jogador sai do grupo compartilhado.
                    sharedCurrent?.OnUnfocused(player, navigationMode);

                    break;

                case NavigationMode.PerPlayer:

                    if (currentByPlayer.TryGetValue(player, out INavigableElement element))
                        element?.OnUnfocused(player, navigationMode);

                    break;

                default:
                    Logger.LogWarning("[NavigationGroup] Enum não foi adicionado");
                    break;
            }
        }

        public void SetElementFocus(PlayerId player, INavigableElement element)
        {
            if (element == null) return;

            switch (navigationMode)
            {
                case NavigationMode.Shared:

                    if (element == sharedCurrent) return;

                    INavigableElement previousSharedCurrent = sharedCurrent;
                    sharedCurrent = element;

                    foreach (PlayerId activePlayer in activePlayers)
                    {
                        previousSharedCurrent?.OnUnfocused(activePlayer, navigationMode);
                        sharedCurrent.OnFocused(activePlayer, navigationMode);
                    }
                    Core.Logger.Log(sharedCurrent.ToString());
                    break;

                case NavigationMode.PerPlayer:

                    if (currentByPlayer.TryGetValue(player, out INavigableElement current) && current == element)
                        return;

                    current?.OnUnfocused(player, navigationMode);
                    currentByPlayer[player] = element;
                    element.OnFocused(player, navigationMode);
                    break;

                default:
                    Logger.LogWarning("[NavigationGroup] Enum não foi adicionado");
                    break;
            }
        }
       
        public void Navigate(PlayerId player, NavigationDirection direction)
        {
            INavigableElement currentPlayerElement = GetCurrentElement(player);

            if (currentPlayerElement == null)
            {
                Enter(player);
                Logger.LogWarning("[NavigationGroup] Nenhum elemento estava em foco ao tentar navegar");
                return;
            }

            INavigableElement candidateElement = currentPlayerElement;
            var visited = new HashSet<INavigableElement>();
            Core.Logger.Log("TryNavigate3");

            while (true)
            {
                candidateElement = candidateElement.GetNeightbor(direction);

                if (candidateElement == null || !visited.Add(candidateElement))
                    return;

                Core.Logger.Log("TryNavigate4");

                if (candidateElement.IsInteractable)
                {
                    Core.Logger.Log("TryNavigate5");

                    SetElementFocus(player, candidateElement);
                    return;
                }
            }
        }

        public void Submit(PlayerId player)
        {
            GetCurrentElement(player)?.OnSubmit(player);
        }

        public void Cancel(PlayerId player)
        {
            OnCancelRequested?.Invoke(this, player);
        }
    }
}
