using UnityEngine;
using CatGame.Core.Interfaces;
using CatGame.Core.Enums;

namespace CatGame.Capabilities.UISystem
{
    public abstract class NavigableElement : MonoBehaviour, INavigableElement
    {
        [Header("Settings")]
        [SerializeField] private bool interactable = true;

        [Header("Directions")]
        [SerializeField] private NavigableElement up;
        [SerializeField] private NavigableElement down;
        [SerializeField] private NavigableElement left;
        [SerializeField] private NavigableElement right;

        public bool IsInteractable => interactable && isActiveAndEnabled;

        public void SetInteractable(bool value) => interactable = value;

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

        public void OnFocused(PlayerId player) 
        {
            OnFocusedByPlayer(player);
        }

        public void OnUnfocused(PlayerId player) 
        {
            OnUnfocusedByPlayer(player);
        }

        public void OnSubmit(PlayerId player) 
        {
            OnSubmitByPlayer(player);
        }

        protected virtual void OnFocusedByPlayer(PlayerId player) { }

        protected virtual void OnUnfocusedByPlayer(PlayerId player) { }

        protected virtual void OnSubmitByPlayer(PlayerId player) { }      
    }
}
