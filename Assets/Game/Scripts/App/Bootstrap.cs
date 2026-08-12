using CatGame.Services.GameStateManagement;
using CatGame.Services.SceneManagement;
using CatGame.Services.AudioManagement;
using CatGame.Core;
using CatGame.Core.Enums; // Usado fora do UnityEditor.
using CatGame.Services.UISystem;
using CatGame.Core.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace CatGame.App
{
    [DefaultExecutionOrder(10)]
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private GameStateService gameStateService;
        [SerializeField] private AudioService audioService;
        [SerializeField] private SceneLoadingService sceneLoadingService;
        [SerializeField] private UIManager uiManager;

        private const string BOOTSTRAP_SCENE = "Bootstrap";

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == BOOTSTRAP_SCENE)
                    return;
            }

            if (!IsSceneLoaded(BOOTSTRAP_SCENE))
                SceneManager.LoadScene(BOOTSTRAP_SCENE, LoadSceneMode.Additive);
        }
#endif

        private void Awake()
        {
            // Inicializa os serviços.
            ServiceLocator.Register<IGameStateService>(gameStateService);
            ServiceLocator.Register<IAudioService>(audioService);
            ServiceLocator.Register<ISceneLoadingService>(sceneLoadingService);
            ServiceLocator.Register<IUIService>(uiManager);
            ServiceLocator.Register<IUINavigationService>(new UINavigationService());

            // Garante que todos os scripts de inicialização percorrerão o jogo todo.
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
#if UNITY_EDITOR
            // Recarrega as cenas depois de carregar o Bootstrap para evitar que faltem referências necessárias.
            if (!IsActiveScene(BOOTSTRAP_SCENE))
                ReloadScenes();
#else
            sceneLoadingService.LoadAsyncScene(Scenes.MainMenu);
#endif
        }

        /// <summary>
        /// Recarrega as cenas depois da inicialização do Bootstrap. 
        /// </summary>
        private async static void ReloadScenes()
        {
            List<string> loadedScenesNames = new();

            // Descarrega as cenas atuais.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                // Se for o Bootstrap ignora porque ele deve continuar carregado.
                if (SceneManager.GetSceneAt(i).name == BOOTSTRAP_SCENE)
                    continue;

                loadedScenesNames.Add(SceneManager.GetSceneAt(i).name);
            }

            foreach (var scene in loadedScenesNames)
            {
                await SceneManager.UnloadSceneAsync(scene);
            }

            foreach (var sceneName in loadedScenesNames)
            {
                if (string.IsNullOrEmpty(sceneName))
                    continue;
                
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }

            Core.Logger.Log("Terminou de recarregar as cenas");
        }

        /// <summary>
        /// Confere se a cena já está carregada.
        /// </summary>
        private static bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                    return true;
            }

            return false;
        }

        private static bool IsActiveScene(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {          
                if (SceneManager.GetActiveScene().name == sceneName)
                    return true;
            }

            return false;
        }
    }
}

