using CatGame.Core.Interfaces;

namespace CatGame.Core.Events
{
    public class LoadingBarChangedEvent : IGlobalEvent
    {
        public float LoadingBarMaxValue { get; set; }
        public float LoadingBarValue { get; set; }
        public bool IsLoading { get; set; }
    }
}