using CatGame.Core.Enums;
using CatGame.Core.Events;
using System;

namespace CatGame.Core.Interfaces
{
    public interface ISceneLoadingService
    {
        public void LoadAsyncScene(Scenes scene);
        public void LoadOnlineAsyncScene(Scenes scene);

        public event EventHandler<LoadingChangedEvent> OnLoadingChanged;
        public bool IsLoading { get; }
        public Scenes CurrentSceneGroup { get; }
    }
}

