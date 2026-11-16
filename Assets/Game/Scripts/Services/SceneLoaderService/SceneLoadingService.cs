using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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

        // Cena vazia criada em runtime para nunca ficar sem cena aberta enquanto as antigas são descarregadas.
        private const string TRANSITION_SCENE = "SceneLoadingTransition";
        private const float DESPAWN_WAIT_TIMEOUT = 5f;
        private const float NETWORK_OPERATIONS_WAIT_TIMEOUT = 10f;
        private const float CLIENTS_READY_TIMEOUT = 15f;
        private const string LOAD_TIMEOUT_REASON = "Não terminou o carregamento a tempo.";

        [Header("Loading Map")]
        [SerializeField] private ScenesGroup[] scenesGroup;

        [Header("Online")]
        [SerializeField] private SceneGroupSync sceneGroupSync;

        public bool IsLoading { get; private set; }
        public Scenes CurrentSceneGroup { get; private set; }

        public event EventHandler<LoadingChangedEvent> OnLoadingChanged;

        private ScenesGroup loadingGroup;
        private Coroutine loadingRoutine;
        private Coroutine remoteBeginRoutine;
        private Scene transitionScene;
        private AsyncOperation pendingLoadOperation;
        private bool lastNetworkOperationSucceeded;
        private NetworkSceneManager observedNetworkSceneManager;
        private readonly List<AsyncOperation> pendingNetworkOperations = new();

        #region Unity

        private void Awake()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            if (sceneGroupSync != null)
                sceneGroupSync.OnStateReceived += SceneGroupSync_OnStateReceived;
        }

        private void Start()
        {
            if (NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.OnServerStarted += NetworkManager_OnSessionStarted;
            NetworkManager.Singleton.OnClientStarted += NetworkManager_OnSessionStarted;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

            if (sceneGroupSync != null)
                sceneGroupSync.OnStateReceived -= SceneGroupSync_OnStateReceived;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= NetworkManager_OnSessionStarted;
                NetworkManager.Singleton.OnClientStarted -= NetworkManager_OnSessionStarted;
            }

            StopObservingNetworkSceneManager();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Carrega um grupo localmente.
        /// </summary>
        public void LoadAsyncScene(Scenes sceneID)
        {
            LoadAsyncScene(sceneID, false);
        }

        /// <summary>
        /// Carrega um grupo localmente. Com forceReload, todas as cenas do grupo são recarregadas do zero, mesmo as que já estão abertas.
        /// </summary>
        public void LoadAsyncScene(Scenes sceneID, bool forceReload)
        {
            if (!TryGetGroup(sceneID, out ScenesGroup sceneGroup))
                return;

            // Um carregamento novo substitui o atual. A tela de loading continua aberta.
            StopActiveLoading();
            loadingRoutine = StartCoroutine(LoadSceneGroup(sceneGroup, forceReload));
        }

        /// <summary>
        /// Carrega um grupo para todas as máquinas da sessão. Somente o servidor pode chamar.
        /// </summary>
        public void LoadOnlineAsyncScene(Scenes sceneID)
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null || !networkManager.IsServer)
            {
                Core.Logger.LogWarning("Somente o servidor pode carregar um grupo de cenas online");
                return;
            }

            if (sceneGroupSync == null || !sceneGroupSync.IsSpawned)
            {
                Core.Logger.LogError($"{nameof(SceneGroupSync)} não está spawnado, os clientes não seriam avisados do carregamento");
                return;
            }

            if (IsLoading)
            {
                Core.Logger.LogWarning($"Já existe um grupo sendo carregado ({CurrentSceneGroup})");
                return;
            }

            if (!TryGetGroup(sceneID, out ScenesGroup sceneGroup))
                return;

            loadingRoutine = StartCoroutine(LoadOnlineSceneGroup(sceneGroup));
        }

        #endregion

        #region Controller

        private IEnumerator LoadSceneGroup(ScenesGroup sceneGroup, bool forceReload)
        {
            BeginLoading(sceneGroup);

            // Se a sessão caiu com o NGO no meio de uma cena, espera ela terminar para entrar no plano e ser descarregada.
            yield return WaitForNetworkOperations();

            List<string> scenesToUnload = new();
            List<string> scenesToLoad = new();

            BuildLoadPlan(sceneGroup, forceReload, scenesToUnload, scenesToLoad);

            // Descarrega primeiro e libera a memória antes de carregar,
            // para as cenas antigas e as novas nunca estarem na memória ao mesmo tempo.
            if (WouldUnloadAllScenes(scenesToUnload))
                CreateTransitionScene();

            foreach (string sceneToUnload in scenesToUnload)
                yield return UnloadScene(sceneToUnload);

            yield return Resources.UnloadUnusedAssets();
            yield return new WaitForSeconds(1);

            // Carrega somente as cenas necessárias.
            foreach (string sceneToLoad in scenesToLoad)
                yield return LoadScene(sceneToLoad);

            yield return new WaitForSeconds(1);

            yield return FinishLoading(sceneGroup, false);
        }

        private void BeginLoading(ScenesGroup sceneGroup)
        {
            IsLoading = true;
            loadingGroup = sceneGroup;
            CurrentSceneGroup = sceneGroup.sceneID;

            OnLoadingChanged?.Invoke(this, new LoadingChangedEvent(
                true,
                sceneGroup.sceneID));

            ServiceLocator.Get<IGameStateService>().SetState(GameState.Loading);
        }

        /// <summary>
        /// Etapa final comum a todos os caminhos: deixa uma cena do grupo ativa, remove a cena de transição e cenas que sobraram.
        /// </summary>
        private IEnumerator FinishLoading(ScenesGroup sceneGroup, bool onlyLocalScenes)
        {
            EnsureGroupSceneIsActive(sceneGroup);

            yield return UnloadTransitionScene();

            // Remove cenas que terminaram de carregar depois do plano (ex: um carregamento interrompido no meio).
            List<string> leftoverScenes = GetScenesOutsideGroup(sceneGroup, onlyLocalScenes);

            foreach (string leftoverScene in leftoverScenes)
                yield return UnloadScene(leftoverScene);

            CompleteLoading(sceneGroup);
        }

        private void CompleteLoading(ScenesGroup sceneGroup)
        {
            ServiceLocator.Get<IGameStateService>().SetState(sceneGroup.gameState);

            IsLoading = false;
            loadingGroup = null;
            loadingRoutine = null;
            CurrentSceneGroup = sceneGroup.sceneID;

            OnLoadingChanged?.Invoke(this, new LoadingChangedEvent(
                false,
                sceneGroup.sceneID));
        }

        private void StopActiveLoading()
        {
            if (loadingRoutine != null)
                StopCoroutine(loadingRoutine);

            if (remoteBeginRoutine != null)
                StopCoroutine(remoteBeginRoutine);

            loadingRoutine = null;
            remoteBeginRoutine = null;

            // Uma cena segurada em 90% (allowSceneActivation = false) trava todas as próximas operações de cena da Unity.
            // Deixa ela terminar: a limpeza do próximo carregamento remove o que sobrar.
            if (pendingLoadOperation != null && !pendingLoadOperation.isDone)
                pendingLoadOperation.allowSceneActivation = true;

            pendingLoadOperation = null;
        }

        /// <summary>
        /// Confere quais cenas devem ser descarregadas e quais devem ser carregadas.
        /// Cenas recarregadas entram nas duas listas: saem primeiro e voltam depois.
        /// </summary>
        private void BuildLoadPlan(ScenesGroup sceneGroup, bool forceReload, List<string> scenesToUnload, List<string> scenesToLoad)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string sceneName = SceneManager.GetSceneAt(i).name;

                if (sceneName == TRANSITION_SCENE)
                    continue;

                SceneSettings sceneSettings = FindSettings(sceneGroup, sceneName);

                // Cena fora do grupo, ou do grupo mas que precisa ser recarregada.
                if (sceneSettings == null || sceneSettings.isToReloadScene || forceReload)
                    scenesToUnload.Add(sceneName);
            }

            foreach (SceneSettings sceneSettings in sceneGroup.sceneSettings)
            {
                if (scenesToLoad.Contains(sceneSettings.scene))
                    continue;

                if (!IsSceneLoaded(sceneSettings.scene) || scenesToUnload.Contains(sceneSettings.scene))
                    scenesToLoad.Add(sceneSettings.scene);
            }
        }

        private List<string> GetScenesOutsideGroup(ScenesGroup sceneGroup, bool onlyLocalScenes)
        {
            List<string> scenes = new();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string sceneName = SceneManager.GetSceneAt(i).name;

                if (sceneName == TRANSITION_SCENE || IsInGroup(sceneGroup, sceneName))
                    continue;

                // No online, cenas de rede são responsabilidade do NGO.
                if (onlyLocalScenes && IsNetworkedScene(sceneName))
                    continue;

                scenes.Add(sceneName);
            }

            return scenes;
        }

        private bool TryGetGroup(Scenes sceneID, out ScenesGroup sceneGroup)
        {
            for (int i = 0; i < scenesGroup.Length; i++)
            {
                ScenesGroup currentSceneGroup = scenesGroup[i];

                if (currentSceneGroup.sceneSettings == null || currentSceneGroup.sceneSettings.Length <= 0)
                    continue;

                if (currentSceneGroup.sceneID == sceneID)
                {
                    sceneGroup = currentSceneGroup;
                    return true;
                }
            }

            Core.Logger.LogError($"Grupo de cenas {sceneID} não encontrado ou sem cenas");
            sceneGroup = null;
            return false;
        }

        private SceneSettings FindSettings(ScenesGroup sceneGroup, string scene)
        {
            foreach (SceneSettings sceneSettings in sceneGroup.sceneSettings)
            {
                if (sceneSettings.scene == scene)
                    return sceneSettings;
            }

            return null;
        }

        private bool IsInGroup(ScenesGroup sceneGroup, string scene)
        {
            return FindSettings(sceneGroup, scene) != null;
        }

        /// <summary>
        /// Uma cena é de rede se o servidor a carregou pelo NGO nesta sessão (lista publicada no SceneGroupSync).
        /// A mesma cena pode ser local numa vez (lobby antes de conectar) e de rede em outra (volta ao lobby pela partida).
        /// Sem sessão, toda cena é local.
        /// </summary>
        private bool IsNetworkedScene(string scene)
        {
            return sceneGroupSync != null && sceneGroupSync.IsSpawned && sceneGroupSync.IsNetworkedScene(scene);
        }

        private bool IsSceneLoaded(string scene)
        {
            Scene loadedScene = SceneManager.GetSceneByName(scene);
            return loadedScene.IsValid() && loadedScene.isLoaded;
        }

        #endregion

        #region Active Scene

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            // Assim que a primeira cena do grupo abre, ela vira a ativa. Objetos instanciados (ex: jogadores do MatchSpawner)
            // vão para ela, e não para a cena de transição ou para uma cena que ainda vai ser descarregada.
            if (!IsLoading || loadingGroup == null || !IsInGroup(loadingGroup, scene.name))
                return;

            if (IsInGroup(loadingGroup, SceneManager.GetActiveScene().name))
                return;

            SceneManager.SetActiveScene(scene);
        }

        private void EnsureGroupSceneIsActive(ScenesGroup sceneGroup)
        {
            if (IsInGroup(sceneGroup, SceneManager.GetActiveScene().name))
                return;

            foreach (SceneSettings sceneSettings in sceneGroup.sceneSettings)
            {
                Scene scene = SceneManager.GetSceneByName(sceneSettings.scene);

                if (scene.IsValid() && scene.isLoaded)
                {
                    SceneManager.SetActiveScene(scene);
                    return;
                }
            }
        }

        #endregion

        #region Transition Scene

        /// <summary>
        /// A Unity não deixa descarregar a última cena aberta (o DontDestroyOnLoad não conta).
        /// </summary>
        private bool WouldUnloadAllScenes(List<string> scenesToUnload)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (!scenesToUnload.Contains(SceneManager.GetSceneAt(i).name))
                    return false;
            }

            return true;
        }

        private void CreateTransitionScene()
        {
            if (transitionScene.IsValid() && transitionScene.isLoaded)
                return;

            transitionScene = SceneManager.CreateScene(TRANSITION_SCENE);
            SceneManager.SetActiveScene(transitionScene);
        }

        private IEnumerator UnloadTransitionScene()
        {
            if (!transitionScene.IsValid() || !transitionScene.isLoaded)
                yield break;

            // Se nenhuma cena do grupo abriu, mantém a transição para não ficar sem cena.
            if (SceneManager.sceneCount <= 1)
                yield break;

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(transitionScene);
            transitionScene = default;

            if (unloadOperation == null)
                yield break;

            while (!unloadOperation.isDone)
                yield return null;
        }

        #endregion

        #region Local

        private IEnumerator LoadScene(string scene)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);

            if (operation == null)
            {
                Core.Logger.LogError($"Não foi possível carregar a cena {scene}");
                yield break;
            }

            operation.allowSceneActivation = false;
            pendingLoadOperation = operation;

            // Espera até a cena carregar 90%
            while (operation.progress < 0.9f)
                yield return null;

            // Ativa a cena
            operation.allowSceneActivation = true;

            // Espera a cena ativar.
            while (!operation.isDone)
                yield return null;

            pendingLoadOperation = null;
        }

        private IEnumerator UnloadScene(string scene)
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);

            // Nulo quando a cena já não está aberta ou já está sendo descarregada.
            if (unloadOperation == null)
                yield break;

            while (!unloadOperation.isDone)
                yield return null;
        }

        #endregion

        #region Online Server

        /// <summary>
        /// Roda só no servidor. Os clientes acompanham pelo SceneGroupSync.
        /// Se a sessão cair no meio, o carregamento para e o SessionEndHandler leva todos de volta ao menu.
        /// </summary>
        private IEnumerator LoadOnlineSceneGroup(ScenesGroup sceneGroup)
        {
            BeginLoading(sceneGroup);

            List<string> scenesToUnload = new();
            List<string> scenesToLoad = new();

            BuildLoadPlan(sceneGroup, false, scenesToUnload, scenesToLoad);

            // Separa as cenas locais (cada máquina carregou sozinha, ex: lobby) das cenas carregadas pelo NGO.
            List<string> localScenesToUnload = new();
            List<string> networkedScenesToUnload = new();

            foreach (string sceneToUnload in scenesToUnload)
            {
                if (IsNetworkedScene(sceneToUnload))
                    networkedScenesToUnload.Add(sceneToUnload);
                else
                    localScenesToUnload.Add(sceneToUnload);
            }

            // Sai da pilha de quem pediu o carregamento (ex: RPC do lobby) antes de despawnar objetos.
            yield return null;

            // O despawn vai antes do Loading: quando os clientes descarregarem o lobby, os objetos já saíram da rede.
            DespawnNetworkObjectsIn(localScenesToUnload);
            sceneGroupSync.Publish(new SceneGroupState(sceneGroup.sceneID, SceneGroupPhase.Loading));

            if (WouldUnloadAllScenes(scenesToUnload))
                CreateTransitionScene();

            // Descarrega primeiro e libera a memória antes de carregar. As locais do servidor saem enquanto os clientes fazem o mesmo.
            foreach (string sceneToUnload in localScenesToUnload)
                yield return UnloadScene(sceneToUnload);

            // Só mexe nas cenas pelo NGO depois que todos os clientes terminaram a parte local
            // (ex: criar a transição antes do NGO descarregar a última cena aberta deles).
            float readyTimeLimit = Time.unscaledTime + CLIENTS_READY_TIMEOUT;
            yield return new WaitUntil(() => sceneGroupSync.AreAllClientsReady() || !IsSessionActive() || Time.unscaledTime >= readyTimeLimit);

            if (!IsSessionActive())
                yield break;

            DisconnectClients(sceneGroupSync.GetClientsNotReady(), "a fase de loading");

            foreach (string sceneToUnload in networkedScenesToUnload)
            {
                yield return UnloadNetworkedScene(sceneToUnload);

                if (!lastNetworkOperationSucceeded)
                {
                    FailOnlineLoading($"Falha ao descarregar a cena {sceneToUnload} pela rede");
                    yield break;
                }
            }

            yield return Resources.UnloadUnusedAssets();

            // Num grupo online todas as cenas entram pelo NGO, uma por vez (o NGO só aceita um evento de cena por vez).
            foreach (string sceneToLoad in scenesToLoad)
            {
                yield return LoadNetworkedScene(sceneToLoad);

                if (!lastNetworkOperationSucceeded)
                {
                    FailOnlineLoading($"Falha ao carregar a cena {sceneToLoad} pela rede");
                    yield break;
                }
            }

            sceneGroupSync.Publish(new SceneGroupState(sceneGroup.sceneID, SceneGroupPhase.Finished));
            yield return FinishLoading(sceneGroup, true);
        }

        private IEnumerator LoadNetworkedScene(string scene)
        {
            lastNetworkOperationSucceeded = false;

            if (!IsSessionActive())
                yield break;

            NetworkSceneManager networkSceneManager = NetworkManager.Singleton.SceneManager;
            bool isCompleted = false;
            List<ulong> timedOutClients = null;

            void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
            {
                if (sceneName != scene)
                    return;

                timedOutClients = new List<ulong>(clientsTimedOut);
                isCompleted = true;
            }

            networkSceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            SceneEventProgressStatus status = networkSceneManager.LoadScene(scene, LoadSceneMode.Additive);

            if (status == SceneEventProgressStatus.Started)
                yield return new WaitUntil(() => isCompleted || !IsSessionActive());
            else
                Core.Logger.LogError($"Não foi possível carregar a cena {scene} pela rede: {status}");

            networkSceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            lastNetworkOperationSucceeded = isCompleted;

            if (!isCompleted)
                yield break;

            sceneGroupSync.ServerAddNetworkedScene(scene);
            DisconnectClients(timedOutClients, $"o carregamento de {scene}");
        }

        private IEnumerator UnloadNetworkedScene(string scene)
        {
            lastNetworkOperationSucceeded = false;

            if (!IsSessionActive())
                yield break;

            Scene loadedScene = SceneManager.GetSceneByName(scene);

            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                lastNetworkOperationSucceeded = true;
                yield break;
            }

            NetworkSceneManager networkSceneManager = NetworkManager.Singleton.SceneManager;
            bool isCompleted = false;
            List<ulong> timedOutClients = null;

            void OnUnloadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
            {
                if (sceneName != scene)
                    return;

                timedOutClients = new List<ulong>(clientsTimedOut);
                isCompleted = true;
            }

            networkSceneManager.OnUnloadEventCompleted += OnUnloadEventCompleted;
            SceneEventProgressStatus status = networkSceneManager.UnloadScene(loadedScene);

            if (status == SceneEventProgressStatus.Started)
                yield return new WaitUntil(() => isCompleted || !IsSessionActive());
            else
                Core.Logger.LogError($"Não foi possível descarregar a cena {scene} pela rede: {status}");

            networkSceneManager.OnUnloadEventCompleted -= OnUnloadEventCompleted;
            lastNetworkOperationSucceeded = isCompleted;

            if (!isCompleted)
                yield break;

            sceneGroupSync.ServerRemoveNetworkedScene(scene);
            DisconnectClients(timedOutClients, $"o descarregamento de {scene}");
        }

        /// <summary>
        /// Quem estourou o tempo fica fora de sincronia com os outros: a sessão segue sem ele.
        /// </summary>
        private void DisconnectClients(List<ulong> timedOutClients, string step)
        {
            if (timedOutClients == null || !IsSessionActive())
                return;

            foreach (ulong clientId in timedOutClients)
            {
                if (clientId == NetworkManager.ServerClientId || !NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                    continue;

                Core.Logger.LogWarning($"Cliente {clientId} não terminou {step} a tempo e foi desconectado");
                NetworkManager.Singleton.DisconnectClient(clientId, LOAD_TIMEOUT_REASON);
            }
        }

        /// <summary>
        /// Encerra a sessão quando o carregamento online não tem como continuar.
        /// O fim da sessão é tratado num lugar só (SessionEndHandler), que leva todos de volta ao menu.
        /// </summary>
        private void FailOnlineLoading(string reason)
        {
            loadingRoutine = null;

            // Sessão já caiu: o SessionEndHandler já assumiu.
            if (!IsSessionActive())
                return;

            Core.Logger.LogError($"{reason}. Encerrando a sessão.");
            NetworkManager.Singleton.Shutdown();
        }

        private void DespawnNetworkObjectsIn(List<string> scenes)
        {
            if (scenes.Count == 0)
                return;

            // Copia antes, porque o Despawn altera a lista de objetos spawnados.
            List<NetworkObject> objectsToDespawn = new();

            foreach (NetworkObject networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (scenes.Contains(networkObject.gameObject.scene.name))
                    objectsToDespawn.Add(networkObject);
            }

            // Objetos colocados na cena não são destruídos aqui: o descarregamento local da cena cuida disso.
            foreach (NetworkObject networkObject in objectsToDespawn)
                networkObject.Despawn(networkObject.InScenePlaced != true);
        }

        private bool HasSpawnedObjectsIn(List<string> scenes)
        {
            if (scenes.Count == 0 || !IsSessionActive())
                return false;

            foreach (NetworkObject networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (scenes.Contains(networkObject.gameObject.scene.name))
                    return true;
            }

            return false;
        }

        private bool IsSessionActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }

        #endregion

        #region Network Scene Operations

        private void NetworkManager_OnSessionStarted()
        {
            // No host os dois eventos disparam com o mesmo NetworkSceneManager.
            NetworkSceneManager networkSceneManager = NetworkManager.Singleton.SceneManager;

            if (networkSceneManager == null || networkSceneManager == observedNetworkSceneManager)
                return;

            StopObservingNetworkSceneManager();
            observedNetworkSceneManager = networkSceneManager;
            observedNetworkSceneManager.OnSceneEvent += NetworkSceneManager_OnSceneEvent;
        }

        private void StopObservingNetworkSceneManager()
        {
            if (observedNetworkSceneManager != null)
                observedNetworkSceneManager.OnSceneEvent -= NetworkSceneManager_OnSceneEvent;

            observedNetworkSceneManager = null;
        }

        /// <summary>
        /// Guarda as operações de cena que o NGO abriu nesta máquina. Se a sessão cair no meio de uma,
        /// a cena termina de carregar sozinha depois, e a volta ao menu precisa esperar por ela.
        /// </summary>
        private void NetworkSceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.AsyncOperation == null || sceneEvent.ClientId != NetworkManager.Singleton.LocalClientId)
                return;

            if (sceneEvent.SceneEventType != SceneEventType.Load && sceneEvent.SceneEventType != SceneEventType.Unload)
                return;

            pendingNetworkOperations.RemoveAll(operation => operation.isDone);
            pendingNetworkOperations.Add(sceneEvent.AsyncOperation);
        }

        private IEnumerator WaitForNetworkOperations()
        {
            float timeLimit = Time.unscaledTime + NETWORK_OPERATIONS_WAIT_TIMEOUT;

            while (pendingNetworkOperations.Exists(operation => !operation.isDone) && Time.unscaledTime < timeLimit)
                yield return null;

            pendingNetworkOperations.Clear();
        }

        #endregion

        #region Online Client

        private void SceneGroupSync_OnStateReceived(SceneGroupState state)
        {
            if (!TryGetGroup(state.Group, out ScenesGroup sceneGroup))
                return;

            switch (state.Phase)
            {
                case SceneGroupPhase.Loading:
                    StopActiveLoading();
                    remoteBeginRoutine = StartCoroutine(BeginRemoteSceneGroup(sceneGroup));
                    break;

                case SceneGroupPhase.Finished:
                    if (loadingRoutine != null)
                        StopCoroutine(loadingRoutine);

                    loadingRoutine = StartCoroutine(FinishRemoteSceneGroup(sceneGroup));
                    break;
            }
        }

        /// <summary>
        /// Cliente: mostra o loading e descarrega as cenas locais (ex: lobby) enquanto o NGO carrega as cenas de rede.
        /// </summary>
        private IEnumerator BeginRemoteSceneGroup(ScenesGroup sceneGroup)
        {
            BeginLoading(sceneGroup);

            List<string> localScenesToUnload = GetScenesOutsideGroup(sceneGroup, true);

            // O despawn e o estado do carregamento podem chegar em qualquer ordem.
            // Espera os objetos de rede dessas cenas saírem para não destruir um objeto ainda spawnado.
            float despawnTimeLimit = Time.unscaledTime + DESPAWN_WAIT_TIMEOUT;
            yield return new WaitUntil(() => !HasSpawnedObjectsIn(localScenesToUnload) || !IsSessionActive() || Time.unscaledTime >= despawnTimeLimit);

            if (HasSpawnedObjectsIn(localScenesToUnload))
                Core.Logger.LogWarning("Objetos de rede ainda spawnados nas cenas locais que serão descarregadas");

            // Conta também as cenas de rede que o NGO vai descarregar: se nenhuma cena sobrar, a Unity recusa o descarregamento.
            if (WouldUnloadAllScenes(GetScenesOutsideGroup(sceneGroup, false)))
                CreateTransitionScene();

            foreach (string sceneToUnload in localScenesToUnload)
                yield return UnloadScene(sceneToUnload);

            yield return Resources.UnloadUnusedAssets();

            // Libera o servidor para mexer nas cenas pelo NGO.
            sceneGroupSync.ReportLoadingReady(sceneGroup.sceneID);

            remoteBeginRoutine = null;
        }

        /// <summary>
        /// Cliente: o NGO já carregou as cenas de rede. Falta finalizar o que é local.
        /// </summary>
        private IEnumerator FinishRemoteSceneGroup(ScenesGroup sceneGroup)
        {
            if (remoteBeginRoutine != null)
            {
                yield return new WaitWhile(() => remoteBeginRoutine != null);
            }
            else if (!IsLoading || loadingGroup != sceneGroup)
            {
                // Cliente que conectou depois do Loading não passou pelo início.
                yield return BeginRemoteSceneGroup(sceneGroup);
            }

            yield return FinishLoading(sceneGroup, true);
        }

        #endregion
    }
}
