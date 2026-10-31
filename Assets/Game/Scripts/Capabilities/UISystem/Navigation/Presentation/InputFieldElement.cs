using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatGame.Capabilities.UISystem
{
    /// <summary>
    /// Hierarquia esperada:
    /// InputField (este script)
    ///  |_ TextArea (RectMask2D)  -> Viewport
    ///     |_ Content (RectTransform que rola no eixo X) -> scrollContent
    ///        |_ SelectionHighlight (pivot X=0, Y=0.5; anchors em um ponto)
    ///        |_ Text (TMP, sem quebra de linha, alinhado à esquerda)
    ///        |_ Caret
    /// </summary>
    public class InputFieldElement : NavigableElement
    {

        public class ValueChangedEvent : EventArgs
        {
            public readonly string Value;

            public ValueChangedEvent(string value)
            {
                Value = value;
            }
        }

        public class EndEditEvent : EventArgs
        {
            public readonly string Value;

            public EndEditEvent(string value)
            {
                Value = value;
            }
        }

        [Header("Input Settings")]
        [SerializeField] private string text = "";
        [SerializeField] private int characterLimit = 0;

        [Header("Input UI")]
        [SerializeField] private TMP_Text textComponent;
        [SerializeField] private RectTransform caret;
        [SerializeField] private GameObject selectionHighlight;

        [Header("Scroll")]
        [Tooltip("Área visível do campo (o objeto com RectMask2D).")]
        [SerializeField] private RectTransform viewport;
        [Tooltip("Objeto filho do viewport que contém Text, Caret e SelectionHighlight e é movido para rolar o texto.")]
        [SerializeField] private RectTransform scrollContent;
        [Tooltip("Folga, em pixels, entre o caret e a borda do viewport.")]
        [SerializeField] private float scrollPadding = 4f;

        [Header("Caret")]
        [SerializeField] private float caretBlinkRate = 0.5f;

        #region Public API

        public string Text => text;
        public int CharacterLimit => characterLimit;

        public int CaretPosition => caretPosition;
        public int CaretSelectPosition => caretSelectPosition;

        public bool HasSelection => caretPosition != caretSelectPosition;

        /// <summary> True se pelo menos um jogador está editando o campo.</summary>
        public bool IsEditing => editingPlayers.Count > 0;

        public bool IsEditingBy(PlayerId player) => editingPlayers.Contains(player);

        public event EventHandler<ValueChangedEvent> OnValueChanged;

        /// <summary> Disparado quando o último jogador que editava para de editar.</summary>
        public event EventHandler<EndEditEvent> OnEndEdit;

        #endregion

        #region Private State

        // Jogadores que estão com a edição ativa neste campo.
        private readonly HashSet<PlayerId> editingPlayers = new();

        private IUINavigationService navigationService;
        private NavigationMode currentNavigationMode = NavigationMode.Shared;

        private Keyboard registeredKeyboard;
        private int lastSubmitFrame = -1;

        private int caretPosition;
        private int caretSelectPosition;

        private float caretBlinkTimer;
        private bool caretVisible;

        private RectTransform selectionRect;

        // Alinhamento que foi configurado no Text pelo Inspector. O script pode trocar temporariamente
        // para Left quando o texto não cabe mais, e volta a este valor quando voltar a caber.
        private TextAlignmentOptions configuredAlignment;

        // Bits do TextAlignmentOptions do TMP. Os 8 primeiros bits são o alinhamento horizontal.
        private const int horizontalAlignmentMask = 0xFF;
        private const int leftAlignmentFlag = 0x1;
        private const int centerAlignmentFlag = 0x2;
        private const int rightAlignmentFlag = 0x4;

        private int SelectionStart => Mathf.Min(caretPosition, caretSelectPosition);
        private int SelectionEnd => Mathf.Max(caretPosition, caretSelectPosition);

        private IUINavigationService NavigationService => navigationService ??= ServiceLocator.Get<IUINavigationService>();

        #endregion

        #region Unity

        private void Awake()
        {
            if (selectionHighlight != null)
                selectionRect = selectionHighlight.GetComponent<RectTransform>();

            if (textComponent != null)
            {
                // Evita que o jogador digitar "<b>" seja interpretado como tag do TMP, o que desalinharia as posições do caret.
                textComponent.richText = false;
                configuredAlignment = textComponent.alignment;
                textComponent.overflowMode = TextOverflowModes.Overflow;
            }

            ClampPositions();
            UpdateText();
            RefreshVisuals();
        }

        private void OnEnable()
        {
            EnsureKeyboardRegistered();
            ResetCaretBlink();
        }

        private void OnDisable()
        {
            StopEditingAllPlayers();
            UnregisterKeyboard();
        }

        private void Update()
        {
            if (!IsEditing)
                return;

            EnsureKeyboardRegistered();

            if (Keyboard.current == null)
                return;

            HandleInput();
            UpdateCaretBlink();
        }

        #endregion

        #region Keyboard Registration

        // O Keyboard.current pode mudar (teclado conectado depois, reconectado etc.), então garantimos que estamos inscritos no teclado atual.
        private void EnsureKeyboardRegistered()
        {
            Keyboard currentKeyboard = Keyboard.current;

            if (currentKeyboard == registeredKeyboard)
                return;

            if (registeredKeyboard != null)
                registeredKeyboard.onTextInput -= OnTextInput;

            registeredKeyboard = currentKeyboard;

            if (registeredKeyboard != null)
                registeredKeyboard.onTextInput += OnTextInput;
        }

        private void UnregisterKeyboard()
        {
            if (registeredKeyboard != null)
                registeredKeyboard.onTextInput -= OnTextInput;

            registeredKeyboard = null;
        }

        private void OnTextInput(char character)
        {
            if (!IsEditing)
                return;

            // A tecla de confirmar pode gerar um caractere no mesmo frame em que a edição começa/termina.
            if (Time.frameCount == lastSubmitFrame)
                return;

            // Ctrl+V, Ctrl+C, Ctrl+A, Enter, Backspace, Delete, Tab e Esc chegam aqui como caracteres de controle (ex: Ctrl+V = '\u0016').
            // Eles nunca devem entrar no texto.
            if (char.IsControl(character))
                return;

            InsertText(character.ToString());
        }

        #endregion

        #region Navigation

        protected override void OnFocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            base.OnFocusedByPlayer(player, navigationMode);

            currentNavigationMode = navigationMode;

            // No modo Shared todos editam juntos: quem entra durante a edição também passa a editar (e fica travado).
            if (IsEditing && navigationMode == NavigationMode.Shared)
                StartEditing(player);
        }

        protected override void OnUnfocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            base.OnUnfocusedByPlayer(player, navigationMode);

            // Se o jogador saiu do elemento (ex.: menu fechado), a edição e o travamento dele terminam.
            StopEditing(player);
        }

        protected override void OnSubmitByPlayer(PlayerId player)
        {
            lastSubmitFrame = Time.frameCount;

            if (currentNavigationMode == NavigationMode.Shared)
            {
                // Shared: o foco é de todos, então a edição também é de todos.
                if (IsEditing)
                    StopEditingAllPlayers();
                else
                    StartEditingAllFocusingPlayers(player);

                return;
            }

            // PerPlayer: cada jogador alterna a própria edição.
            if (IsEditingBy(player))
                StopEditing(player);
            else
                StartEditing(player);
        }

        #endregion

        #region Editing State

        private void StartEditingAllFocusingPlayers(PlayerId submitter)
        {
            StartEditing(submitter);

            foreach (PlayerId focusingPlayer in FocusingPlayerList)
                StartEditing(focusingPlayer);
        }

        private void StartEditing(PlayerId player)
        {
            bool wasEditing = IsEditing;

            if (!editingPlayers.Add(player))
                return;

            NavigationService.SetNavigationLocked(player, true);

            if (!wasEditing)
                OnEditingStarted();
        }

        private void StopEditing(PlayerId player)
        {
            if (!editingPlayers.Remove(player))
                return;

            NavigationService.SetNavigationLocked(player, false);

            if (!IsEditing)
                OnEditingFinished();
        }

        private void StopEditingAllPlayers()
        {
            if (!IsEditing)
                return;

            // Copia porque StopEditing remove itens do conjunto durante o loop.
            foreach (PlayerId editingPlayer in new List<PlayerId>(editingPlayers))
                StopEditing(editingPlayer);
        }

        // Chamado quando o PRIMEIRO jogador começa a editar.
        private void OnEditingStarted()
        {
            EnsureKeyboardRegistered();

            caretPosition = text.Length;
            caretSelectPosition = caretPosition;

            ResetCaretBlink();
            RefreshVisuals();
        }

        // Chamado quando o último jogador para de editar.
        private void OnEditingFinished()
        {
            caretSelectPosition = caretPosition;

            HideCaret();
            HideSelection();
            ResetScroll();

            OnEndEdit?.Invoke(this, new EndEditEvent(text));
        }

        #endregion

        #region Text Editing

        private void InsertText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            bool hadSelection = HasSelection;

            RemoveSelection();

            value = TrimToCharacterLimit(value);

            if (value.Length == 0)
            {
                // Só apagou a seleção (limite atingido): a tela ainda precisa ser atualizada.
                if (hadSelection)
                    TextChanged();

                return;
            }

            text = text.Insert(caretPosition, value);

            caretPosition += value.Length;
            caretSelectPosition = caretPosition;

            TextChanged();
        }

        private string TrimToCharacterLimit(string value)
        {
            if (characterLimit <= 0)
                return value;

            int availableCharacters = characterLimit - text.Length;

            if (availableCharacters <= 0)
                return string.Empty;

            return value.Length > availableCharacters
                ? value.Substring(0, availableCharacters)
                : value;
        }

        private void Delete()
        {
            if (HasSelection)
            {
                RemoveSelection();
                TextChanged();
                return;
            }

            if (caretPosition >= text.Length)
                return;

            text = text.Remove(caretPosition, 1);

            TextChanged();
        }

        private void Backspace()
        {
            if (HasSelection)
            {
                RemoveSelection();
                TextChanged();
                return;
            }

            if (caretPosition <= 0)
                return;

            text = text.Remove(caretPosition - 1, 1);

            caretPosition--;
            caretSelectPosition = caretPosition;

            TextChanged();
        }

        private void RemoveSelection()
        {
            if (!HasSelection)
                return;

            int start = SelectionStart;
            int end = SelectionEnd;

            text = text.Remove(start, end - start);

            caretPosition = start;
            caretSelectPosition = start;
        }

        #endregion

        #region Caret Movement

        private void MoveCaret(int newPosition, bool extendSelection)
        {
            newPosition = Mathf.Clamp(newPosition, 0, text.Length);

            caretPosition = newPosition;

            if (!extendSelection)
                caretSelectPosition = newPosition;

            ResetCaretBlink();
            RefreshVisuals();
        }

        private void MoveLeft(bool extendSelection)
        {
            // Sem Shift e com seleção: apenas recolhe a seleção para o início dela.
            if (!extendSelection && HasSelection)
            {
                MoveCaret(SelectionStart, false);
                return;
            }

            MoveCaret(caretPosition - 1, extendSelection);
        }

        private void MoveRight(bool extendSelection)
        {
            // Sem Shift e com seleção: apenas recolhe a seleção para o fim dela.
            if (!extendSelection && HasSelection)
            {
                MoveCaret(SelectionEnd, false);
                return;
            }

            MoveCaret(caretPosition + 1, extendSelection);
        }

        private void MoveToStart(bool extendSelection)
        {
            MoveCaret(0, extendSelection);
        }

        private void MoveToEnd(bool extendSelection)
        {
            MoveCaret(text.Length, extendSelection);
        }

        private void SelectAll()
        {
            caretPosition = text.Length;
            caretSelectPosition = 0;

            ResetCaretBlink();
            RefreshVisuals();
        }

        #endregion

        #region Keyboard Input

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;

            bool shift =
                keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed;

            bool control =
                keyboard.leftCtrlKey.isPressed ||
                keyboard.rightCtrlKey.isPressed;

            bool command =
                keyboard.leftCommandKey.isPressed ||
                keyboard.rightCommandKey.isPressed;

            bool modifier = control || command;

            HandleSpecialKeys(shift, modifier);
            HandleClipboard(modifier);
        }

        private void HandleSpecialKeys(bool shift, bool modifier)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                MoveLeft(shift);
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                MoveRight(shift);
            }
            else if (keyboard.homeKey.wasPressedThisFrame)
            {
                MoveToStart(shift);
            }
            else if (keyboard.endKey.wasPressedThisFrame)
            {
                MoveToEnd(shift);
            }
            else if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                Backspace();
            }
            else if (keyboard.deleteKey.wasPressedThisFrame)
            {
                Delete();
            }
            else if (modifier && keyboard.aKey.wasPressedThisFrame)
            {
                SelectAll();
            }
        }

        #endregion

        #region Clipboard

        private void HandleClipboard(bool modifier)
        {
            if (!modifier)
                return;

            Keyboard keyboard = Keyboard.current;

            if (keyboard.cKey.wasPressedThisFrame)
            {
                CopySelection();
            }
            else if (keyboard.xKey.wasPressedThisFrame)
            {
                CutSelection();
            }
            else if (keyboard.vKey.wasPressedThisFrame)
            {
                Paste();
            }
        }

        private void CopySelection()
        {
            if (!HasSelection)
                return;

            GUIUtility.systemCopyBuffer = text.Substring(SelectionStart, SelectionEnd - SelectionStart);
        }

        private void CutSelection()
        {
            if (!HasSelection)
                return;

            CopySelection();

            RemoveSelection();
            TextChanged();
        }

        private void Paste()
        {
            string clipboardText = RemoveControlCharacters(GUIUtility.systemCopyBuffer);

            if (string.IsNullOrEmpty(clipboardText))
                return;

            InsertText(clipboardText);
        }

        private static string RemoveControlCharacters(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            StringBuilder builder = new(value.Length);

            foreach (char character in value)
            {
                if (!char.IsControl(character))
                    builder.Append(character);
            }

            return builder.ToString();
        }

        #endregion

        #region UI Refresh

        private void TextChanged()
        {
            UpdateText();
            RefreshVisuals();

            OnValueChanged?.Invoke(this, new ValueChangedEvent(text));
        }

        private void UpdateText()
        {
            if (textComponent == null)
                return;

            textComponent.text = text;
        }

        // Atualiza a malha do TMP e, em seguida, scroll, caret e seleção (nessa ordem, pois um depende do outro).
        private void RefreshVisuals()
        {
            if (textComponent == null)
                return;

            UpdateTextAlignment();
            textComponent.ForceMeshUpdate();

            if (IsEditing)
                UpdateScroll();
            else
                ResetScroll();

            UpdateCaretVisual();
            UpdateSelectionVisual();
        }

        #region Text Align

        // Se o Text foi configurado como Center ou Right, ele só mantém esse alinhamento enquanto o texto cabe.
        // Quando ultrapassa a largura, passa a Left, porque o scroll sempre parte do início do texto.
        private void UpdateTextAlignment()
        {
            int horizontalFlags = (int)configuredAlignment & horizontalAlignmentMask;

            if ((horizontalFlags & leftAlignmentFlag) != 0)
                return;

            float containerWidth = textComponent.rectTransform.rect.width;

            // Se o Text for mais largo que a área visível, vale a área visível.
            if (viewport != null)
                containerWidth = Mathf.Min(containerWidth, viewport.rect.width);

            float availableWidth =
                containerWidth -
                textComponent.margin.x -
                textComponent.margin.z;

            bool textFits = textComponent.GetPreferredValues(text).x <= availableWidth;

            TextAlignmentOptions targetAlignment = textFits
                ? configuredAlignment
                : (TextAlignmentOptions)(((int)configuredAlignment & ~horizontalAlignmentMask) | leftAlignmentFlag);

            if (textComponent.alignment != targetAlignment)
                textComponent.alignment = targetAlignment;
        }

        // Sem caracteres, o TMP não tem posição para consultar, então calculamos pela área do Text e pelo alinhamento.
        private float GetEmptyTextCaretLocalX()
        {
            Rect textRectArea = textComponent.rectTransform.rect;

            float leftEdge = textRectArea.xMin + textComponent.margin.x;
            float rightEdge = textRectArea.xMax - textComponent.margin.z;

            int horizontalFlags = (int)textComponent.alignment & horizontalAlignmentMask;

            if ((horizontalFlags & centerAlignmentFlag) != 0)
                return (leftEdge + rightEdge) * 0.5f;

            if ((horizontalFlags & rightAlignmentFlag) != 0)
                return rightEdge;

            return leftEdge;
        }

        #endregion

        #region Caret Position

        // Posição X, no espaço local do Text, onde o caret fica para o índice informado.
        // Índice N = entre o caractere N-1 e o caractere N.
        private float GetCaretTextLocalX(int index)
        {
            TMP_TextInfo textInfo = textComponent.textInfo;
            int characterCount = textInfo.characterCount;

            if (characterCount == 0)
                return GetEmptyTextCaretLocalX();

            index = Mathf.Clamp(index, 0, characterCount);

            if (index == 0)
                return textInfo.characterInfo[0].origin;

            // xAdvance = onde o próximo caractere começa. Funciona também para espaços,
            // diferente de topRight.x, que não avança em caracteres sem glifo visível.
            return textInfo.characterInfo[index - 1].xAdvance;
        }

        // Ponto do caret no mundo, centralizado verticalmente na linha de texto.
        private Vector3 GetCaretWorldPoint(int index)
        {
            RectTransform textRect = textComponent.rectTransform;

            return textRect.TransformPoint(new Vector3(
                GetCaretTextLocalX(index),
                textRect.rect.center.y,
                0f));
        }

        #endregion

        #region Scroll

        // Move o Content para o caret nunca ficar fora da área visível.
        private void UpdateScroll()
        {
            if (viewport == null || scrollContent == null)
                return;

            float visibleLeft = viewport.rect.xMin + scrollPadding;
            float visibleRight = viewport.rect.xMax - scrollPadding;

            // Todas as posições são medidas no espaço do viewport, que não se move.
            float caretX = viewport.InverseTransformPoint(GetCaretWorldPoint(caretPosition)).x;
            float textStartX = viewport.InverseTransformPoint(GetCaretWorldPoint(0)).x;
            float textEndX = viewport.InverseTransformPoint(GetCaretWorldPoint(text.Length)).x;

            // Texto cabe inteiro na área visível: não há o que rolar.
            if (textEndX - textStartX <= visibleRight - visibleLeft)
            {
                ResetScroll();
                return;
            }

            float shift = 0f;

            if (caretX > visibleRight)
                shift = visibleRight - caretX; // negativo: texto anda para a esquerda
            else if (caretX < visibleLeft)
                shift = visibleLeft - caretX; // positivo: texto anda para a direita

            // Limites: o início do texto não passa da borda esquerda e o fim não passa da borda direita,
            // senão sobraria espaço vazio. Independe do alinhamento do Text.
            shift = Mathf.Min(shift, visibleLeft - textStartX);
            shift = Mathf.Max(shift, visibleRight - textEndX);

            Vector2 contentPosition = scrollContent.anchoredPosition;
            contentPosition.x += shift;
            scrollContent.anchoredPosition = contentPosition;
        }

        private void ResetScroll()
        {
            if (scrollContent == null)
                return;

            Vector2 contentPosition = scrollContent.anchoredPosition;
            contentPosition.x = 0f;
            scrollContent.anchoredPosition = contentPosition;
        }

        #endregion

        #region Caret and selection

        private void UpdateCaretVisual()
        {
            if (caret == null)
                return;

            Vector3 localPoint = caret.parent.InverseTransformPoint(GetCaretWorldPoint(caretPosition));
            caret.localPosition = new Vector3(localPoint.x,localPoint.y, caret.localPosition.z);
        }

        private void UpdateSelectionVisual()
        {
            bool showSelection = IsEditing && HasSelection;

            if (selectionHighlight != null)
                selectionHighlight.SetActive(showSelection);

            if (!showSelection || selectionRect == null)
                return;

            RectTransform textRect = textComponent.rectTransform;
            Transform selectionParent = selectionRect.parent;

            Vector3 startLocal = selectionParent.InverseTransformPoint(GetCaretWorldPoint(SelectionStart));
            Vector3 endLocal = selectionParent.InverseTransformPoint(GetCaretWorldPoint(SelectionEnd));

            Vector3 bottomLocal = selectionParent.InverseTransformPoint(
                textRect.TransformPoint(new Vector3(0f, textRect.rect.yMin, 0f)));

            Vector3 topLocal = selectionParent.InverseTransformPoint(
                textRect.TransformPoint(new Vector3(0f, textRect.rect.yMax, 0f)));

            float width = Mathf.Abs(endLocal.x - startLocal.x);
            float height = Mathf.Abs(topLocal.y - bottomLocal.y);

            selectionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            selectionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            selectionRect.localPosition = new Vector3(startLocal.x, startLocal.y, selectionRect.localPosition.z);
        }

        #endregion

        #endregion

        #region Caret Blink

        private void ResetCaretBlink()
        {
            caretBlinkTimer = 0f;
            caretVisible = true;

            if (caret != null)
                caret.gameObject.SetActive(IsEditing);
        }

        private void UpdateCaretBlink()
        {
            if (caret == null)
                return;

            caretBlinkTimer += Time.unscaledDeltaTime;

            if (caretBlinkTimer >= caretBlinkRate)
            {
                caretBlinkTimer = 0f;
                caretVisible = !caretVisible;

                caret.gameObject.SetActive(caretVisible);
            }
        }

        private void HideCaret()
        {
            if (caret != null)
                caret.gameObject.SetActive(false);
        }

        private void HideSelection()
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(false);
        }

        #endregion

        #region Set Text

        public void SetText(string value)
        {
            ApplyExternalText(value);
            TextChanged();
        }

        public void SetTextWithoutNotify(string value)
        {
            ApplyExternalText(value);
            UpdateText();
            RefreshVisuals();
        }

        private void ApplyExternalText(string value)
        {
            value ??= string.Empty;

            if (characterLimit > 0 && value.Length > characterLimit)
                value = value.Substring(0, characterLimit);

            text = value;

            caretPosition = Mathf.Clamp(caretPosition, 0, text.Length);
            caretSelectPosition = caretPosition;
        }

        #endregion

        #region Validation

        private void ClampPositions()
        {
            caretPosition = Mathf.Clamp(caretPosition, 0, text.Length);
            caretSelectPosition = Mathf.Clamp(caretSelectPosition, 0, text.Length);
        }

        #endregion
    }
}