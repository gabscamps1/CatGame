using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private List<Button> buttons;
    private int currentIndex = 0;

    private float navCooldown = 0.2f;
    private float navTimer = 0f;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        buttons = new List<Button>
        {
            root.Q<Button>("Option1"),
            root.Q<Button>("Option2"),
            root.Q<Button>("Option3")
        };

        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            buttons[i].focusable = true;

            buttons[i].RegisterCallback<PointerEnterEvent>(evt => Select(index));
            buttons[i].RegisterCallback<FocusEvent>(evt => Select(index));
            buttons[i].RegisterCallback<ClickEvent>(evt => Confirm(index));
        }

        Select(0);
        buttons[0].Focus();
    }

    void Update()
    {
        HandleKeyboard();
        HandleGamepad();
    }

    void HandleKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            MoveSelection(1);
        else if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
            MoveSelection(-1);
        else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            Confirm(currentIndex);
    }

    void HandleGamepad()
    {
        var gp = Gamepad.current;
        if (gp == null) return;

        if (gp.buttonSouth.wasPressedThisFrame) // A / Cross
            Confirm(currentIndex);

        float vertical = gp.leftStick.ReadValue().y;
        if (gp.dpad.down.wasPressedThisFrame) vertical = -1f;
        if (gp.dpad.up.wasPressedThisFrame) vertical = 1f;

        // Cooldown pra evitar navegar várias vezes com analógico segurado
        navTimer -= Time.deltaTime;
        if (navTimer <= 0f)
        {
            if (vertical < -0.5f) { MoveSelection(1); navTimer = navCooldown; }
            else if (vertical > 0.5f) { MoveSelection(-1); navTimer = navCooldown; }
        }
    }

    void MoveSelection(int direction)
    {
        int newIndex = (currentIndex + direction + buttons.Count) % buttons.Count;
        Select(newIndex);
        buttons[newIndex].Focus();
    }

    void Select(int index)
    {
        currentIndex = index;
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].EnableInClassList("selected", i == index);
    }

    void Confirm(int index)
    {
        switch (index)
        {
            case 0: Debug.Log("MULTIJOGADOR"); break;
            case 1: Debug.Log("OPÇÕES"); break;
            case 2: Debug.Log("SAIR"); Application.Quit(); break;
        }
    }
}