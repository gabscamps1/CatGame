using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using CatGame.Core.Events;
using CatGame.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatGame.Services.SceneManagement
{
    public class SceneLoadingService : MonoBehaviour, ISceneLoadingService
    {
        [Serializable]
        public class SceneSettings
        {
            public string scene;
            public bool isToReloadScene;
        }

        [Serializable]
        public class ScenesGroup
        {
            public Scenes sceneID;
            public SceneSettings[] sceneSettings;
            public GameState gameState;
        }

        [Header("Loading Map")]
        [SerializeField] private ScenesGroup[] scenesGroup;

        public void LoadAsyncScene(Scenes sceneID)
        {
            for (int i = 0; i < scenesGroup.Length; i++)
            {
                ScenesGroup currentSceneGroup = scenesGroup[i];

                if (currentSceneGroup.sceneSettings == null || currentSceneGroup.sceneSettings.Length <= 0)
                    return;

                if (currentSceneGroup.sceneID == sceneID)
                {
                    // Chama coroutine de carregar cena.
                    StartCoroutine(LoadSceneGroup(currentSceneGroup));
                }
            }   
        }

        private IEnumerator LoadSceneGroup(ScenesGroup sceneGroup)
        {
            ServiceLocator.Get<IGameStateService>().SetState(GameState.Loading);
            
            List<string> scenesToKeepActivated = new();
            List<string> scenesToUnload = new();
            List<string> scenesToLoadAfter = new();

            // Confere quais cenas devem ser mantidas, carregadas, descarregades ou recarregadas.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                bool isSceneUsed = false;

                for (int j = 0; j < sceneGroup.sceneSettings.Length; j++)
                {
                    SceneSettings currentSceneSettings = sceneGroup.sceneSettings[j];
                    string currentSceneName = sceneGroup.sceneSettings[j].scene;

                    // Se a cena nas configurações está cena precisar ser recarregada, já vai verificar a próxima.
                    if (currentSceneSettings.isToReloadScene)
                    {
                        if (!scenesToLoadAfter.Contains(currentSceneName))
                            scenesToLoadAfter.Add(currentSceneName);

                        continue;
                    }

                    if (SceneManager.GetSceneAt(i).name == currentSceneName)
                    {
                        // Deve manter essa cena ativada por estar no group.
                        scenesToKeepActivated.Add(currentSceneName);
                        isSceneUsed = true;

                        break;
                    }               
                }

                if (!isSceneUsed)                
                    scenesToUnload.Add(SceneManager.GetSceneAt(i).name);     
            }

            int totalLoadOperations = sceneGroup.sceneSettings.Length + scenesToUnload.Count - scenesToKeepActivated.Count;
            int completedLoadOperations = 0;
            float progressPerOperation = 1f / totalLoadOperations;
            float completedOperationsProgress = progressPerOperation * completedLoadOperations;

            // Carrega somente as cenas necessárias.
            foreach (var sceneSettings in sceneGroup.sceneSettings)
            {
                if (scenesToKeepActivated.Contains(sceneSettings.scene) || scenesToLoadAfter.Contains(sceneSettings.scene))
                    continue;

                yield return StartCoroutine(LoadScene(sceneSettings.scene, progressPerOperation, completedOperationsProgress));

                completedLoadOperations++;
            }

            yield return new WaitForSeconds(1);

            // Descarrega as cenas que não são necessárias.
            foreach (var sceneToUnload in scenesToUnload)
            {
                // Descarrega a cena.
                yield return UnloadScene(sceneToUnload, progressPerOperation, completedOperationsProgress);

                completedLoadOperations++;
            }

            yield return new WaitForSeconds(1);

            // Recarrega as cenas somente depois de descarregar as cenas desnecessárias.
            foreach (var sceneToLoadAfter in scenesToLoadAfter)
            {
                yield return StartCoroutine(LoadScene(sceneToLoadAfter, progressPerOperation, completedOperationsProgress));

                completedLoadOperations++;
            }

            ServiceLocator.Get<IGameStateService>().SetState(sceneGroup.gameState);
        }

        private IEnumerator LoadScene(string scene, float progressPerOperation, float previousOperationsProgress)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            operation.allowSceneActivation = false;

            // Espera até a cena carregar 90%
            while (operation.progress < 0.9f)
            {           
                EventBus.Publish(new LoadingBarChangedEvent()
                {
                    LoadingBarValue = previousOperationsProgress + progressPerOperation * operation.progress,
                    LoadingBarMaxValue = 1,
                    IsLoading = true
                });

                yield return null;
            }

            // Ativa a cena
            operation.allowSceneActivation = true;

            // Espera a cena ativar.
            while (!operation.isDone)
                yield return null;
        }

        private IEnumerator UnloadScene(string scene, float progressPerOperation, float previousOperationsProgress)
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);

            while (unloadOperation.progress < 0.9f)
            {
                EventBus.Publish(new LoadingBarChangedEvent()
                {
                    LoadingBarValue = previousOperationsProgress + progressPerOperation * unloadOperation.progress,
                    LoadingBarMaxValue = 1,
                    IsLoading = true
                });

                yield return null;
            }

            while (!unloadOperation.isDone)
                yield return null;
        }
    }
}

