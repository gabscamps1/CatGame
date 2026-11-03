using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatGame.Capabilities.UISystem.Sound
{
    /// <summary>
    /// Toca sons em um elemento da UI (botão, toggle, dropdown...) sem alterar o código dele.
    /// - Hover: quando o mouse passa por cima (sem clicar).
    /// - Clique: quando o elemento é confirmado (mouse, teclado ou controle).
    ///
    /// Coloque no MESMO GameObject do ButtonElement/ToggleElement/etc.
    /// Para o hover funcionar, o objeto precisa de um Graphic com "Raycast Target"
    /// ligado (o Image do botão já serve) e a cena precisa ter um EventSystem.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIElementSound : MonoBehaviour, IPointerEnterHandler
    {
        public enum ClickSoundType
        {
            None,
            Next,   // Avançar para a próxima tela
            Back,   // Voltar para a tela anterior
            Custom  // Usa o som do campo "Custom Click Sound"
        }

        [Header("Hover (mouse por cima)")]
        [SerializeField] private AudioData hoverSound;          // hover_click
        [Tooltip("Também tocar o hover quando o elemento for selecionado pelo teclado/controle.")]
        [SerializeField] private bool hoverAlsoOnKeyboardFocus = false;

        [Header("Clique")]
        [SerializeField] private ClickSoundType clickSoundType = ClickSoundType.Next;
        [SerializeField] private AudioData nextSound;           // Next
        [SerializeField] private AudioData backSound;           // Back
        [SerializeField] private AudioData customClickSound;

        [Header("Opções")]
        [Tooltip("Não toca nada se o elemento estiver desabilitado.")]
        [SerializeField] private bool onlyWhenInteractable = true;

        private NavigableElement element;
        private IAudioService audioService;

        private void Awake()
        {
            element = GetComponent<NavigableElement>();
        }

        private void Start()
        {
            audioService = ServiceLocator.Get<IAudioService>();
        }

        private void OnEnable()
        {
            if (element == null) element = GetComponent<NavigableElement>();
            if (element == null) return;

            element.OnSubmittedEvent += Element_OnSubmitted;
            element.OnFocusedEvent += Element_OnFocused;
        }

        private void OnDisable()
        {
            if (element == null) return;

            element.OnSubmittedEvent -= Element_OnSubmitted;
            element.OnFocusedEvent -= Element_OnFocused;
        }

        // Mouse entrou em cima do elemento.
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanPlay()) return;
            Play(hoverSound);
        }

        private void Element_OnFocused(object sender, NavigableElement.FocussedEvent e)
        {
            if (!hoverAlsoOnKeyboardFocus || !CanPlay()) return;
            Play(hoverSound);
        }

        private void Element_OnSubmitted(object sender, NavigableElement.SubmittedEvent e)
        {
            if (!CanPlay()) return;

            switch (clickSoundType)
            {
                case ClickSoundType.Next: Play(nextSound); break;
                case ClickSoundType.Back: Play(backSound); break;
                case ClickSoundType.Custom: Play(customClickSound); break;
            }
        }

        private bool CanPlay()
        {
            if (!onlyWhenInteractable || element == null) return true;
            return element.IsInteractable;
        }

        private void Play(AudioData data)
        {
            if (data == null) return;
            audioService ??= ServiceLocator.Get<IAudioService>();
            audioService?.PlaySFXClip(data);
        }
    }
}
