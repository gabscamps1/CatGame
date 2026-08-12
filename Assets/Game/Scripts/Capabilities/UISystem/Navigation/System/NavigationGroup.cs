using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System.Collections.Generic;
using Logger = CatGame.Core.Logger;

namespace CatGame.Capabilities.UISystem
{
    public class NavigationGroup : INavigationGroup
    {
        private readonly NavigationMode ownershipMode;

        private readonly HashSet<INavigableElement> elements = new();

        // Per Player.
        private readonly Dictionary<PlayerId, INavigableElement> currentByPlayer = new();
        private readonly Dictionary<PlayerId, INavigableElement> defaultByPlayer = new();
        private readonly HashSet<PlayerId> activePlayers = new();

        // Shared.
        private INavigableElement defaultElement;
        private INavigableElement sharedCurrent;

        public NavigationGroup(NavigationMode ownershipMode = NavigationMode.Shared)
        {
            this.ownershipMode = ownershipMode;
        }

        public void Register(INavigableElement element, bool isDefault = false)
        {
            if (element == null || elements.Contains(element)) 
                return;

            elements.Add(element);

            if (isDefault || defaultElement == null)
                defaultElement = element;
        }

        public void SetDefaultForPlayer(PlayerId player, INavigableElement element)
        {
            defaultByPlayer[player] = element;
        }

        public INavigableElement GetCurrentElement(PlayerId player)
        {
            return ownershipMode == NavigationMode.Shared
                ? sharedCurrent // Shared
                : currentByPlayer.GetValueOrDefault(player); // PerPlayer
        }

        public void Enter(PlayerId player)
        {
            bool wasInactive = activePlayers.Count == 0;
            activePlayers.Add(player);

            switch (ownershipMode)
            {
                case NavigationMode.Shared:

                    INavigableElement startSharedElement = sharedCurrent ?? defaultElement;

                    if (wasInactive)
                    {
                        sharedCurrent = startSharedElement;
                        startSharedElement?.OnFocused(player);
                    }
                    else
                    {
                        if (sharedCurrent == null)
                            SetElementFocus(player, startSharedElement);
                    }

                    break;

                case NavigationMode.PerPlayer:

                    INavigableElement startPerPlayerElement = currentByPlayer.GetValueOrDefault(player)
                    ?? defaultByPlayer.GetValueOrDefault(player)
                    ?? defaultElement;

                    SetElementFocus(player, startPerPlayerElement);
                    break;

                default:
                    Logger.LogWarning("[NavigationGroup] Enum não foi adicionado");
                    break;
            }
        }

        public void Exit(PlayerId player)
        {
            activePlayers.Remove(player);

            switch (ownershipMode)
            {
                case NavigationMode.Shared:

                    // Só desfoca quando o último jogador sai do grupo compartilhado.
                    if (activePlayers.Count == 0)
                        sharedCurrent?.OnUnfocused(player);

                    break;
                case NavigationMode.PerPlayer:

                    if (currentByPlayer.TryGetValue(player, out INavigableElement element))
                        element?.OnUnfocused(player);

                    break;

                default:
                    Logger.LogWarning("[NavigationGroup] Enum não foi adicionado");
                    break;
            }

            // TODO: Opção de perder ou não a seleção atual quando o menu for fechado. Isso tanto para shared e perPlayer.
        }

        public void SetElementFocus(PlayerId player, INavigableElement element)
        {
            if (element == null) return;

            switch (ownershipMode)
            {
                case NavigationMode.Shared:
                    if (element == sharedCurrent)
                        return;

                    sharedCurrent?.OnUnfocused(player);
                    sharedCurrent = element;
                    sharedCurrent.OnFocused(player);
                    break;

                case NavigationMode.PerPlayer:

                    if (currentByPlayer.TryGetValue(player, out INavigableElement current) && current == element)
                        return;

                    current?.OnUnfocused(player);
                    currentByPlayer[player] = element;
                    element.OnFocused(player);

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

            INavigableElement candidate = currentPlayerElement;
            var visited = new HashSet<INavigableElement>();

            while (true)
            {
                candidate = candidate.GetNeightbor(direction);

                if (candidate == null || !visited.Add(candidate))
                    return;

                if (candidate.IsInteractable)
                {
                    SetElementFocus(player, candidate);
                    return;
                }
            }
        }

        public void Submit(PlayerId player)
        {
            GetCurrentElement(player)?.OnSubmit(player);
        }
    }
}
