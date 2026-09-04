using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // Se você usa o Text legado (UnityEngine.UI.Text), troque TextMeshProUGUI por Text

[RequireComponent(typeof(Animator))]
public class HoverButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referências")]
    [SerializeField] private Image targetImage;       // A imagem que muda de cor
    [SerializeField] private TextMeshProUGUI targetText; // O texto do botão

    [Header("Cores")]
    [SerializeField] private Color normalImageColor = Color.white;
    [SerializeField] private Color hoverImageColor = Color.yellow;
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color hoverTextColor = Color.white;

    [Header("Animação")]
    [SerializeField] private Animator animator;
    [SerializeField] private string enterTrigger = "MouseEnter";
    [SerializeField] private string exitTrigger = "MouseExit";

    // Guarda de segurança: evita que Enter/Exit disparem repetidamente
    // caso haja raycasts "piscando" entre elementos sobrepostos do botão.
    private bool isHovering = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // Garante que tudo comece no estado padrão
        SetColors(normalImageColor, normalTextColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovering)
            return; // já está em hover, ignora chamada duplicada

        isHovering = true;

        SetColors(hoverImageColor, hoverTextColor);

        if (animator != null)
            animator.SetTrigger(enterTrigger);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovering)
            return; // já está fora do hover, ignora chamada duplicada

        isHovering = false;

        SetColors(normalImageColor, normalTextColor);

        if (animator != null)
            animator.SetTrigger(exitTrigger);
    }

    private void SetColors(Color imageColor, Color textColor)
    {
        if (targetImage != null)
            targetImage.color = imageColor;

        if (targetText != null)
            targetText.color = textColor;
    }
}