using CatGame.Core.Enums;
using System;
using System.Threading.Tasks;

namespace CatGame.Core.Interfaces
{
    public interface IBasePanel
    {
        public Task Show(Action onComplete = null);
        public Task Hide(Action onComplete = null);
        public PanelType PanelType { get; }
        public bool IsVisible { get; }
        public UnityEngine.GameObject gameObject { get; }
    }
}