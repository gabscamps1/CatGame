using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using UnityEngine;

namespace CatGame.Capabilities.UISystem.Sound
{
    /// <summary>
    /// Toca sons quando o estado do jogo muda:
    /// - Pause: ao entrar em GameState.Paused.
    /// - Início da partida: ao entrar em GameState.Playing vindo de outro estado
    ///   (por padrão, NÃO toca ao voltar do pause).
    ///
    /// Coloque UM deste em um objeto que exista durante a partida (ex.: na MainScene).
    /// </summary>
    public class GameStateSound : MonoBehaviour
    {
        [Header("Pause")]
        [SerializeField] private AudioData pauseSound;          // pause
        [Tooltip("Toca o mesmo som ao despausar.")]
        [SerializeField] private bool playPauseSoundOnResume = false;

        [Header("Início da partida")]
        [SerializeField] private AudioData startMatchSound;     // Start_game_1

        private IGameStateService gameStateService;
        private IAudioService audioService;

        private void Start()
        {
            gameStateService = ServiceLocator.Get<IGameStateService>();
            audioService = ServiceLocator.Get<IAudioService>();
            gameStateService.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (gameStateService != null)
                gameStateService.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState previous, GameState current)
        {
            if (previous == current) return;

            if (current == GameState.Paused)
            {
                Play(pauseSound);
                return;
            }

            if (current == GameState.Playing)
            {
                if (previous == GameState.Paused)
                {
                    if (playPauseSoundOnResume) Play(pauseSound);
                    return;
                }

                Play(startMatchSound);
            }
        }

        private void Play(AudioData data)
        {
            if (data != null) audioService?.PlaySFXClip(data);
        }
    }
}
