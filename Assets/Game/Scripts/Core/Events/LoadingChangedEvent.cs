using CatGame.Core.Enums;
using System;

namespace CatGame.Core.Events
{
    public class LoadingChangedEvent : EventArgs
    {
        public bool IsLoading { get; private set; }
        public Scenes SceneGroup { get; private set; }

        public LoadingChangedEvent(bool isLoading, Scenes sceneGroup)
        {
            IsLoading = isLoading;
            SceneGroup = sceneGroup;
        }
    }
}
