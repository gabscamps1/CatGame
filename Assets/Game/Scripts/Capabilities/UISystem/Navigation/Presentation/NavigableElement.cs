using CatGame.Core.Enums;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CatGame.Core;

namespace CatGame.Capabilities.UISystem
{
    public abstract class NavigableElement : MonoBehaviour, INavigableElement
    {
        public class FocussedEvent : EventArgs
        {
            public readonly PlayerId PlayerId;

            public FocussedEvent(PlayerId playerId)
            {
                PlayerId = playerId;
            }
        }

        public class SubmittedEvent : EventArgs
        {
            public readonly PlayerId PlayerId;

            public SubmittedEvent(PlayerId playerId)
            {
                PlayerId = playerId;
            }
        }

        public event EventHandler<SubmittedEvent> OnSubmittedEvent;
        public event EventHandler<FocussedEvent> OnFocusedEvent;

        [Header("Settings")]
        [SerializeField] private bool ownInteractable = true;

        [Header("Directions")]
        [SerializeField] private NavigableElement up;
        [SerializeField] private NavigableElement down;
        [SerializeField] private NavigableElement left;
        [SerializeField] private NavigableElement right;

        [Header("Visual")]
        [SerializeField] private HighlightVisual highlightVisual;

        [Header("Sounds")]
        [SerializeField] private AudioData onFocusSound;

        public IReadOnlyList<PlayerId> FocusingPlayerList => FocusingPlayer.ToList();
        public bool IsFocused => FocusingPlayer.Count > 0;
        public bool IsInteractable => ownInteractable && heirInteractable && isActiveAndEnabled;


        protected bool heirInteractable = true;

        protected readonly HashSet<PlayerId> FocusingPlayer = new();

        private readonly Dictionary<PlayerId, IHighlightVisual> attachedHighlights = new();

        private IAudioService audioService;

        protected virtual void Awake()
        {
            audioService = ServiceLocator.Get<IAudioService>();
        }

        public void SetInteractable(bool value) => ownInteractable = value;

        public INavigableElement GetNeightbor(NavigationDirection direction)
        {
            return direction switch
            {
                NavigationDirection.Up => up,
                NavigationDirection.Down => down,
                NavigationDirection.Left => left,
                NavigationDirection.Right => right,
                _ => null
            };
        }

        public void OnFocused(PlayerId player, NavigationMode navigationMode = NavigationMode.Shared)
        {
            if (!FocusingPlayer.Add(player)) return;
            OnFocusedByPlayer(player, navigationMode);
            OnFocusedEvent?.Invoke(this, new FocussedEvent(player));
            audioService?.PlaySFXClip(onFocusSound);
        }

        public void OnUnfocused(PlayerId player, NavigationMode navigationMode = NavigationMode.Shared)
        {
            if (!FocusingPlayer.Remove(player)) return;
            OnUnfocusedByPlayer(player, navigationMode);
        }

        public void OnSubmit(PlayerId player)
        {
            OnSubmitByPlayer(player);
            OnSubmittedEvent?.Invoke(this, new SubmittedEvent(player));
        }

        protected virtual void OnFocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            if (highlightVisual == null) 
                return;

            switch (navigationMode)
            {  
                case NavigationMode.Shared:

                    bool isPlayerEnterBefore = FocusingPlayer.Count > 1;

                    if (!isPlayerEnterBefore)
                    {
                        IHighlightVisual instance = HighlightManager.Instance.AssignHighlightToTarget(highlightVisual, this, player);
                        attachedHighlights[player] = instance;
                    }

                    break;

                case NavigationMode.PerPlayer:

                    IHighlightVisual instancePerPlayer = HighlightManager.Instance.AssignHighlightToTarget(highlightVisual, this, player);
                    attachedHighlights[player] = instancePerPlayer;
                    break;
            }

            RefreshAllHighlights();
        }

        protected virtual void OnUnfocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            if (highlightVisual == null) 
                return;

            if (attachedHighlights.ContainsKey(player))
            {
                HighlightManager.Instance.UnassignHightlightOfTarget(highlightVisual, attachedHighlights[player], player);
                attachedHighlights.Remove(player);
            }       

            RefreshAllHighlights();
        }

        protected virtual void OnSubmitByPlayer(PlayerId player) { }

        private void RefreshAllHighlights()
        {
            foreach (IHighlightVisual instance in attachedHighlights.Values)
                instance.Refresh();
        }
    }
}
