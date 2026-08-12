using System;
using CatGame.Core.Enums;

namespace CatGame.Capabilities.UISystem
{
    public class ButtonElement : NavigableElement
    {
        public event EventHandler<PlayerId> OnSubmitted;

        protected override void OnSubmitByPlayer(PlayerId player)
        {
            OnSubmitted?.Invoke(this, player);
        }
    }
}