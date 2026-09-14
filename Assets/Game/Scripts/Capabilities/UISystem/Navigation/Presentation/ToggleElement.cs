using CatGame.Core.Enums;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public class ToggleElement : NavigableElement
    {
        [SerializeField] private bool isActive;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            IsActive = isActive;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }

        public void ChangeState()
        {
            IsActive = !IsActive;
        }

        protected override void OnSubmitByPlayer(PlayerId player)
        {
            ChangeState();
        }
    }
}