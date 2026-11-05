using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace CatGame.Features.Cutscene
{
    public class CutsceneSkip : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;

        [Header("Skip Settings")]
        [SerializeField] private GameObject skipTextGameObject;
        [SerializeField, Min(1)] private float timeToCanTrySkip;
        [SerializeField] private float timeHoldedToSkip;

        [Header("Sounds")]
        [SerializeField] private AudioData skipSound;

        private bool canTrySkip;

        private bool isLoading;

        private float countTimeHoldToSkip;

        private IPlayerInputController playerInput;

        private IGameStateService gameStateService;
        private IAudioService audioService;

        private void Awake()
        {
            skipTextGameObject.SetActive(false);
        }

        private void Start()
        {
            playerInput = ServiceLocator.Get<IInputService>().GetInputFromPlayer(new PlayerId(0));
            playerInput.SkipCutscene.performed += SkipCutscene_performed;

            gameStateService = ServiceLocator.Get<IGameStateService>();
            gameStateService.OnStateChanged += GameStateService_OnStateChanged;

            audioService = ServiceLocator.Get<IAudioService>();

            GameStateService_OnStateChanged(gameStateService.CurrentState, gameStateService.CurrentState);
        }

        private void OnDestroy()
        {
            gameStateService.OnStateChanged -= GameStateService_OnStateChanged;
            playerInput.SkipCutscene.performed -= SkipCutscene_performed;
        }

        private void GameStateService_OnStateChanged(GameState previous, GameState current)
        {
            if (videoPlayer.isPlaying)
                return;

            switch (current)
            {
                case GameState.Cutscene:
                    WaitToVideoPrepared();
                    break;
                default:
                    break;
            }
        }   

        private void WaitToVideoPrepared()
        {
            videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
            videoPlayer.Prepare();
        }

        private void VideoPlayer_prepareCompleted(VideoPlayer source)
        {
            videoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;

            if (source.isPlaying)
                return;

            videoPlayer.Play();
            StartCoroutine(WaitToCanSkip());
        }

        private IEnumerator WaitToCanSkip()
        {
            yield return new WaitForSeconds(timeToCanTrySkip);
            skipTextGameObject.SetActive(true);
            canTrySkip = true;
        }

        private void SkipCutscene_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (isLoading) return;
            SkipCutscene();
        }

        private void Update()
        {
            bool canSkip =
                videoPlayer.time > 1.0 // Se o v�deo ja reproduziu ao menos 1 segundo.
                && !videoPlayer.isPlaying // Se o v�deo n�o estiver tocando.
                && !isLoading; // J� iniciou o loading da pr�xima cena.

            if (canSkip)
            
                
                SkipCutscene();

            // Implementa��o futura
            /*if (playerInput.SkipCutscene.IsPressed())
            {
                countTimeHoldToSkip += Time.deltaTime;

                if (countTimeHoldToSkip > timeHoldedToSkip)
                    SkipCutscene();
            }
            else if (countTimeHoldToSkip > 0)
            {
                countTimeHoldToSkip -= Time.deltaTime;
                countTimeHoldToSkip = Mathf.Max(0, countTimeHoldToSkip);
            }*/
        }

        private void SkipCutscene()
        {
            if (!canTrySkip)
                return;

            isLoading = true;
            audioService.PlaySFXClip(skipSound, false);
            videoPlayer.time = videoPlayer.length;
            skipTextGameObject.SetActive(false);

            // Carrega a cena do main menu.
            ServiceLocator.Get<ISceneLoadingService>().LoadAsyncScene(Scenes.MainMenu);
        }
    }
}