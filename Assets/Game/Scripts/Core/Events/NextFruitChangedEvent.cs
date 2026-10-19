using CatGame.Core.Interfaces;
using UnityEngine;

namespace CatGame.Core.Events
{
    public struct NextFruitChangedEvent : IGlobalEvent
    {
        public int PlayerIndex { get; set; }
        public Sprite Sprite { get; set; }
    }
}