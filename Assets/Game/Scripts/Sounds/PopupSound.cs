using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using UnityEngine;

namespace CatGame.Capabilities.UISystem.Sound
{
    /// <summary>
    /// Toca um som sempre que este GameObject aparece na tela (é ativado).
    /// Use para mensagens/popups: coloque no objeto da mensagem
    /// (ex.: o "serverMessageGameObject" do ServerMessageManager) ou em qualquer painel.
    /// </summary>
    public class PopupSound : MonoBehaviour
    {
        [SerializeField] private AudioData showSound;   // popup_catgame
        [SerializeField] private AudioData hideSound;   // opcional

        [Tooltip("Ignora a primeira ativação (quando a cena carrega), para não tocar no início.")]
        [SerializeField] private bool skipFirstEnable = false;

        private bool hasEnabledOnce;
        private bool started;

        private void Start()
        {
            started = true;
        }

        private void OnEnable()
        {
            bool isFirst = !hasEnabledOnce;
            hasEnabledOnce = true;
            if (isFirst && skipFirstEnable) return;

            Play(showSound);
        }

        private void OnDisable()
        {
            // Evita tocar quando a cena está sendo fechada.
            if (!started || !gameObject.scene.isLoaded) return;
            Play(hideSound);
        }

        private static void Play(AudioData data)
        {
            if (data == null) return;
            IAudioService audio = ServiceLocator.Get<IAudioService>();
            audio?.PlaySFXClip(data);
        }
    }
}
