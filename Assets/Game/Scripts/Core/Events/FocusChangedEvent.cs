using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;

namespace CatGame.Core.Events
{
    public class FocusChangedEvent : EventArgs
    {
        public PlayerId PlayerId;
        public INavigableElement Previous;
        public INavigableElement Current;
    }
}