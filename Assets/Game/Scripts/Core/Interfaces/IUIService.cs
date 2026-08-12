using CatGame.Core.Enums;
using System;

namespace CatGame.Core.Interfaces
{
    public interface IUIService
    {
        public event Action<PanelType?> OnCurrentUIChanged;
        public void RegisterPanel(IBasePanel panel);
        public void UnregisterPanel(PanelType type);
        public void Push(PanelType type, Action onComplete = null);
        public void Pop(Action onComplete = null);
        public void PopTo(PanelType target);
        public void SwitchTo(PanelType type, Action onComplete = null);
        public void PushMany(params PanelType[] types);
        public void CloseAll();
        public void Close(PanelType type);
        public bool IsVisible(PanelType type);
        public PanelType? CurrentPanel { get; }
    }
}