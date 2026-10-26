using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using UnityEngine;
using UnityEngine.Video;

namespace CatGame.Features.Cutscene
{
    public class CutsceneSkip : MonoBehaviour
    {
        [SerializeField] private float timeHoldedToSkip;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private GameObject skipTextGameObject;

        private bool isLoading;

        private float countTimeHoldToSkip;

        private IPlayerInputController playerInput;

        private void Start()
        {         
            playerInput = ServiceLocator.Get<IInputService>().GetInputFromPlayer(new PlayerId(0));
            playerInput.SkipCutscene.performed += SkipCutscene_performed;
        }

        private void OnDestroy()
        {
            playerInput.SkipCutscene.performed -= SkipCutscene_performed;
        }

        private void SkipCutscene_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (isLoading) return;
            SkipCutscene();
        }

        private void Update()
        {
            bool canSkip =
                videoPlayer.time > 1.0 // Se o vídeo ja reproduziu ao menos 1 segundo.
                && !videoPlayer.isPlaying // Se o vídeo não estiver tocando.
                && !isLoading; // Já iniciou o loading da próxima cena.

            if (canSkip)
                SkipCutscene();

            return;

            if (playerInput.SkipCutscene.IsPressed())
            {
                countTimeHoldToSkip += Time.deltaTime;

                if (countTimeHoldToSkip > timeHoldedToSkip)
                    SkipCutscene();
            }
            else if (countTimeHoldToSkip > 0)
            {
                countTimeHoldToSkip -= Time.deltaTime;
                countTimeHoldToSkip = Mathf.Max(0, countTimeHoldToSkip);
            }
        }

        private void SkipCutscene()
        {
            isLoading = true;
            videoPlayer.time = videoPlayer.length;
            skipTextGameObject.SetActive(false);

            // Carrega a cena do main menu.
            ServiceLocator.Get<ISceneLoadingService>().LoadAsyncScene(Scenes.MainMenu);
        }
    }
}