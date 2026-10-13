namespace CatGame.Core.Interfaces
{
    public interface ISceneLoadingService
    {
        public void LoadAsyncScene(Enums.Scenes scene);
        public void LoadOnlineAsyncScene(Enums.Scenes scene);
    }
}

