using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CatGame.Capabilities.UISystem
{
    public class InputFieldElement : NavigableElement, IUpdateSelectedHandler
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

        [Header("Caret")]
        [SerializeField] private float caretBlinkRate = 0.5f;

        public string Text => text;
        public int CharacterLimit => characterLimit;

        public int CaretPosition => caretPosition;
        public int CaretSelectPosition => caretSelectPosition;

        public bool HasSelection => caretPosition != caretSelectPosition;

        public event EventHandler<ValueChangedEvent> OnValueChanged;
        public event EventHandler<EndEditEvent> OnEndEdit;

        private int caretPosition;
        private int caretSelectPosition;

        private bool isEditing;

        private float caretBlinkTimer;
        private bool caretVisible;

        private void Awake()
        {
            ClampPositions();
            UpdateText();
            UpdateCaret();
            UpdateSelection();
        }

        private void OnEnable()
        {
            RegisterKeyboard();
            ResetCaretBlink();
        }

        private void OnDisable()
        {
            UnregisterKeyboard();
        }

        private void RegisterKeyboard()
        {
            if (Keyboard.current == null)
                return;

            Keyboard.current.onTextInput += OnTextInput;
        }

        private void UnregisterKeyboard()
        {
            if (Keyboard.current == null)
                return;

            Keyboard.current.onTextInput -= OnTextInput;
        }

        private void Update()
        {
            if (!isEditing)
                return;

            HandleInput();
            UpdateCaretBlink();
        }

        

        private void OnTextInput(char character)
        {
            if (!IsFocused || !isEditing)
                return;

            if (!CanText())
                return;

            InsertText(character.ToString());
        }

        private bool CanText()
        {
            return !(Keyboard.current.backspaceKey.isPressed || Keyboard.current.deleteKey.isPressed);
        }


        // ============================================================
        // NAVIGATION
        // ============================================================

        protected override void OnFocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            base.OnFocusedByPlayer(player, navigationMode);
            Debug.Log("Estou sendo focadooo");
            ConfigNavigation(); // Garante que se um novo player foi adicionado e estiver focando no element, ele terá sua navegação bloqueada/liberada.
        }

        protected override void OnUnfocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            base.OnUnfocusedByPlayer(player, navigationMode);
            Debug.Log("AAAAAAA");
            ConfigNavigation();
            ServiceLocator.Get<IUINavigationService>().SetNavigationLocked(player, false);
        }

        protected override void OnSubmitByPlayer(PlayerId player)
        {
            if (isEditing)
            {
                EndEditing();
            }
            else
            {
                BeginEditing();
            }

            ConfigNavigation();
        }

        private void ConfigNavigation()
        {
            if (isEditing)
            {
                foreach (var playerId in FocusingPlayerList)
                    ServiceLocator.Get<IUINavigationService>().SetNavigationLocked(playerId, true);
            }
            else
            {
                foreach (var playerId in FocusingPlayerList)
                    ServiceLocator.Get<IUINavigationService>().SetNavigationLocked(playerId, false);
            }
        }

        // ============================================================
        // EDITING
        // ============================================================

        private void BeginEditing()
        {
            Debug.Log("BEGIN EDITING");

            if (isEditing)
                return;

            isEditing = true;

            caretPosition = Mathf.Clamp(
                caretPosition,
                0,
                text.Length);

            caretSelectPosition = caretPosition;

            ResetCaretBlink();
            UpdateCaret();
            UpdateSelection();
        }

        private void EndEditing()
        {
            if (!isEditing)
                return;

            isEditing = false;

            caretSelectPosition = caretPosition;

            HideCaret();
            HideSelection();

            OnEndEdit?.Invoke(
                this,
                new EndEditEvent(text));
        }

        private void InsertText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            RemoveSelection();

            if (characterLimit > 0)
            {
                int availableCharacters = characterLimit - text.Length;

                if (availableCharacters <= 0)
                    return;

                if (value.Length > availableCharacters)
                    value = value.Substring(0, availableCharacters);
            }

            text = text.Insert(caretPosition, value);

            caretPosition += value.Length;
            caretSelectPosition = caretPosition;

            TextChanged();
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
                Debug.Log("Tava com seleção");
                RemoveSelection();
                TextChanged();
                return;
            }

            if (caretPosition <= 0)
            {
                Debug.Log("Menor que 0");
                return;
            }
            Debug.Log("Apagou");

            text = text.Remove(caretPosition - 1, 1);

            caretPosition--;
            caretSelectPosition = caretPosition;

            TextChanged();
        }

        private void RemoveSelection()
        {
            if (!HasSelection)
                return;

            int start = Mathf.Min(
                caretPosition,
                caretSelectPosition);

            int end = Mathf.Max(
                caretPosition,
                caretSelectPosition);

            text = text.Remove(
                start,
                end - start);

            caretPosition = start;
            caretSelectPosition = start;
        }

        // ============================================================
        // CURSOR
        // ============================================================

        private void MoveCaret(int position, bool select)
        {
            position = Mathf.Clamp(
                position,
                0,
                text.Length);

            if (select)
            {
                caretPosition = position;
            }
            else
            {
                caretPosition = position;
                caretSelectPosition = position;
            }

            ResetCaretBlink();

            UpdateCaret();
            UpdateSelection();
        }

        private void MoveLeft(bool select)
        {
            MoveCaret(
                Mathf.Max(0, caretPosition - 1),
                select);
        }

        private void MoveRight(bool select)
        {
            MoveCaret(
                Mathf.Min(text.Length, caretPosition + 1),
                select);
        }

        private void MoveStart(bool select)
        {
            MoveCaret(0, select);
        }

        private void MoveEnd(bool select)
        {
            MoveCaret(text.Length, select);
        }

        private void SelectAll()
        {
            caretPosition = text.Length;
            caretSelectPosition = 0;

            ResetCaretBlink();

            UpdateCaret();
            UpdateSelection();
        }

        // ============================================================
        // KEYBOARD INPUT
        // ============================================================

        private void HandleInput()
        {
            bool shift =
                Keyboard.current.leftShiftKey.isPressed ||
                Keyboard.current.rightShiftKey.isPressed;

            bool control =
                Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed;

            bool command =
                Keyboard.current.leftCommandKey.isPressed ||
                Keyboard.current.rightCommandKey.isPressed;

            bool modifier = control || command;

            HandleSpecialKeys(shift, modifier);
            HandleClipboard(modifier);
            /*HandleTextInput();*/
        }

        public void OnUpdateSelected(BaseEventData eventData)
        {
            Debug.Log("UPDATE SELECTED");

            if (!isEditing)
                return;

            bool shift = false /*Input.GetKey(KeyCode.LeftShift) ||
                         Input.GetKey(KeyCode.RightShift)*/;

            bool control = false /*Input.GetKey(KeyCode.LeftControl) ||
                           Input.GetKey(KeyCode.RightControl)*/;

            bool command = false /*Input.GetKey(KeyCode.LeftCommand) ||
                          Input.GetKey(KeyCode.RightCommand)*/;

            bool modifier = control || command;

            HandleSpecialKeys(
                shift,
                modifier);

            /*HandleTextInput();*/

            HandleClipboard(modifier);

            ResetCaretBlink();
        }

        private void HandleSpecialKeys(bool shift, bool modifier)
        {
            Debug.Log("Conrerindo");
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                MoveLeft(shift);
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                MoveRight(shift);
            }
            else if (Keyboard.current.homeKey.wasPressedThisFrame)
            {
                MoveStart(shift);
            }
            else if (Keyboard.current.endKey.wasPressedThisFrame)
            {
                MoveEnd(shift);
            }
            else if (Keyboard.current.backspaceKey.wasPressedThisFrame)
            {
                Debug.Log("Backspace");
                Backspace();
            }
            else if (Keyboard.current.deleteKey.wasPressedThisFrame)
            {
                Delete();
            }
            else if (modifier && Keyboard.current.aKey.wasPressedThisFrame)
            {
                SelectAll();
            }
        }

        private void HandleTextInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return;

            foreach (char character in input)
            {
                if (character == '\b')
                    continue;

                if (character == '\n' ||
                    character == '\r')
                    continue;

                InsertText(character.ToString());
            }
        }

        // ============================================================
        // CLIPBOARD
        // ============================================================

        private void HandleClipboard(bool modifier)
        {
            if (!modifier)
                return;

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                CopySelection();
            }
            else if (Keyboard.current.xKey.wasPressedThisFrame)
            {
                CutSelection();
            }
            else if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                Paste();
            }
        }

        private void CopySelection()
        {
            if (!HasSelection)
                return;

            int start = Mathf.Min(
                caretPosition,
                caretSelectPosition);

            int end = Mathf.Max(
                caretPosition,
                caretSelectPosition);

            GUIUtility.systemCopyBuffer =
                text.Substring(start, end - start);
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
            string clipboard = GUIUtility.systemCopyBuffer;

            if (string.IsNullOrEmpty(clipboard))
                return;

            InsertText(clipboard);
        }

        // ============================================================
        // UI
        // ============================================================

        private void TextChanged()
        {
            UpdateText();
            UpdateCaret();
            UpdateSelection();

            OnValueChanged?.Invoke(
                this,
                new ValueChangedEvent(text));
        }

        private void UpdateText()
        {
            if (textComponent == null)
                return;

            textComponent.text = text;
        }

        private void UpdateCaret()
        {
            if (caret == null || textComponent == null)
                return;

            TMP_TextInfo textInfo = textComponent.textInfo;

            textComponent.ForceMeshUpdate();

            textInfo = textComponent.textInfo;

            if (textInfo.characterCount == 0)
            {
                caret.anchoredPosition = new Vector2(0, caret.anchoredPosition.y);
                caret.sizeDelta = new Vector2(
                    caret.sizeDelta.x,
                    textComponent.fontSize);

                return;
            }

            int position =
                Mathf.Clamp(
                    caretPosition,
                    0,
                    textInfo.characterCount);

            Vector3 localPosition;

            if (position == 0)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[0];

                localPosition = new Vector3(
                    character.bottomLeft.x,
                    caret.localPosition.y/*character.bottomLeft.y*/,
                    0);
            }
            else if (position >= textInfo.characterCount)
            {
                TMP_CharacterInfo character =
                    textInfo.characterInfo[
                        textInfo.characterCount - 1];

                localPosition = new Vector3(
                    character.topRight.x,
                    caret.localPosition.y/*character.bottomLeft.y*/,
                    0);
            }
            else
            {
                TMP_CharacterInfo character =
                    textInfo.characterInfo[position];

                localPosition = new Vector3(
                    character.bottomLeft.x,
                    caret.localPosition.y/*character.bottomLeft.y*/,
                    0);
            }

            caret.localPosition = localPosition;
        }

        private void UpdateSelection()
        {
            if (selectionHighlight == null)
                return;

            selectionHighlight.SetActive(
                isEditing && HasSelection);
        }

        // ============================================================
        // CARET
        // ============================================================

        private void ResetCaretBlink()
        {
            caretBlinkTimer = 0f;
            caretVisible = true;

            if (caret != null)
                caret.gameObject.SetActive(
                    isEditing);
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

                caret.gameObject.SetActive(
                    caretVisible);
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

        // ============================================================
        // SET TEXT
        // ============================================================

        public void SetText(string value)
        {
            value ??= string.Empty;

            if (characterLimit > 0 &&
                value.Length > characterLimit)
            {
                value = value.Substring(
                    0,
                    characterLimit);
            }

            text = value;

            caretPosition = Mathf.Clamp(
                caretPosition,
                0,
                text.Length);

            caretSelectPosition =
                caretPosition;

            TextChanged();
        }

        public void SetTextWithoutNotify(string value)
        {
            value ??= string.Empty;

            if (characterLimit > 0 &&
                value.Length > characterLimit)
            {
                value = value.Substring(
                    0,
                    characterLimit);
            }

            text = value;

            caretPosition = Mathf.Clamp(
                caretPosition,
                0,
                text.Length);

            caretSelectPosition =
                caretPosition;

            UpdateText();
            UpdateCaret();
            UpdateSelection();
        }

        // ============================================================
        // VALIDATION
        // ============================================================

        private void ClampPositions()
        {
            caretPosition =
                Mathf.Clamp(
                    caretPosition,
                    0,
                    text.Length);

            caretSelectPosition =
                Mathf.Clamp(
                    caretSelectPosition,
                    0,
                    text.Length);
        }
    }
}