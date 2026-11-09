using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using UnityEngine;
using CatGame.Core.Data;
using CatGame.Services.AudioManagement;


namespace CatGame.UI
{
    public class SceneLoadingUI : BasePanel
    {
        [Header("Settings")]
        [SerializeField] private Canvas sceneLoadingCanvas;

        private IUIService uiService;
        private ISceneLoadingService sceneLoadingService;

        [Header("Sounds")]
        [SerializeField] private AudioData LOADING_SOUND;

        private IAudioService audioService;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        protected override void Start()
        {
            audioService = ServiceLocator.Get<IAudioService>();
            uiService = ServiceLocator.Get<IUIService>();
            sceneLoadingService = ServiceLocator.Get<ISceneLoadingService>();

            sceneLoadingService.OnLoadingChanged += SceneLoadingService_OnLoadingChanged;

            // Garante o estado certo ao iniciar
            SceneLoadingService_OnLoadingChanged(this, new LoadingChangedEvent(
                sceneLoadingService.IsLoading, 
                sceneLoadingService.CurrentSceneGroup));

            base.Start();

            
        }      

        protected override void OnDestroy()
        {
            
            sceneLoadingService.OnLoadingChanged -= SceneLoadingService_OnLoadingChanged;
            base.OnDestroy();

            
        }

        private void SceneLoadingService_OnLoadingChanged(object sender, LoadingChangedEvent e)
        {
            switch (e.SceneGroup)
            {
                // Não mostra a tela de loading para essas cenas.
                case Scenes.Bootstrap:
                case Scenes.Splashs:
                case Scenes.Cutscene:
                    sceneLoadingCanvas.gameObject.SetActive(false);

                    break;

                case Scenes.MainMenu:
                case Scenes.Playing:

                     if (!audioService.IsPlayingThisMusic(LOADING_SOUND))
                    {
                        //audioService.PlayMusic(LOADING_SOUND, true);
                    }

                    sceneLoadingCanvas.gameObject.SetActive(e.IsLoading);
                    break;
            }
        }

        protected override void GameStateService_OnStateChanged(GameState previousState, GameState currentState)
        {
            // Garante que na primeira vez será 
            switch (currentState)
            {
                case GameState.Loading:
                    if (!uiService.IsVisible(PanelType))
                        uiService.Push(PanelType);
                    break;
            }
        }     
    }

}
