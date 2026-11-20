using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;

namespace CatGame.Features.InputManagement
{
    [DefaultExecutionOrder(-50)]
    public class InputIconsManager : MonoBehaviour
    {
        [Serializable]
        public class GamepadIcons
        {
            // Default.
            public Sprite defaultButton;

            // Face buttons.
            public Sprite buttonSouth; // A / Cross
            public Sprite buttonEast;  // B / Circle
            public Sprite buttonWest;  // X / Square
            public Sprite buttonNorth; // Y / Triangle

            // Bumpers and triggers.
            public Sprite leftBumper;  // LB / L1
            public Sprite rightBumper; // RB / R1
            public Sprite leftTrigger; // LT / L2
            public Sprite rightTrigger;// RT / R2

            // Stick buttons.
            public Sprite leftStick;   // L3
            public Sprite rightStick;  // R3

            // D-pad.
            public Sprite dpadUp;
            public Sprite dpadDown;
            public Sprite dpadLeft;
            public Sprite dpadRight;

            // D-pad agrupado (usado pelo InputIconVariant).
            public Sprite dpad;
            public Sprite dpadVertical;
            public Sprite dpadHorizontal;

            // Start / Back.
            public Sprite start;   // Start / Options
            public Sprite select;  // Back / Share

            // Dicionário dos icones.
            private Dictionary<string, Sprite> gamepadIcons;

            // Inicializa o dicionario.
            public void Init()
            {
                gamepadIcons = new()
            {
                // Face buttons
                { "buttonSouth", buttonSouth },
                { "buttonEast", buttonEast },
                { "buttonWest", buttonWest },
                { "buttonNorth", buttonNorth },

                // Shoulder / Triggers
                { "leftShoulder", leftBumper },
                { "rightShoulder", rightBumper },
                { "leftTrigger", leftTrigger },
                { "rightTrigger", rightTrigger },

                // Stick buttons
                { "leftStick", leftStick },
                { "rightStick", rightStick },

                // D-pad
                { "dpadUp", dpadUp },
                { "dpadDown", dpadDown },
                { "dpadLeft", dpadLeft },
                { "dpadRight", dpadRight },

                // D-pad (partes de composite: <Gamepad>/dpad/up).
                { "dpad/up", dpadUp },
                { "dpad/down", dpadDown },
                { "dpad/left", dpadLeft },
                { "dpad/right", dpadRight },

                // Sticks (partes de composite: <Gamepad>/leftStick/up).
                { "leftStick/up", leftStick },
                { "leftStick/down", leftStick },
                { "leftStick/left", leftStick },
                { "leftStick/right", leftStick },
                { "rightStick/up", rightStick },
                { "rightStick/down", rightStick },
                { "rightStick/left", rightStick },
                { "rightStick/right", rightStick },

                // Start / Back / System
                { "start", start },
                { "select", select },
            };
            }

            // Pega o sprite correspondente a bind.
            public Sprite GetIcon(string controlPath, InputIconVariant variant = InputIconVariant.Single)
            {
                if (variant != InputIconVariant.Single && controlPath.StartsWith("dpad"))
                {
                    Sprite group = GetGroup(variant, dpad, dpadVertical, dpadHorizontal);
                    if (group != null) return group;
                }

                return gamepadIcons.TryGetValue(controlPath, out var sprite) && sprite != null ? sprite : defaultButton;
            }
        }

        [Serializable]
        public class KeyboardIcons
        {
            // Default.
            public Sprite defaultKey;

            // Letras.
            public Sprite a, b, c, d, e, f, g, h, i, j;
            public Sprite k, l, m, n, o, p, q, r, s, t;
            public Sprite u, v, w, x, y, z;

            // Números.
            /*public Sprite zero, one, two, three, four;
            public Sprite five, six, seven, eight, nine;*/

            // Especiais.
            public Sprite space;
            public Sprite enter;
            public Sprite tab;
            public Sprite escape;
            public Sprite backspace;
            public Sprite delete;

            // Setas.
            public Sprite upArrow;
            public Sprite downArrow;
            public Sprite leftArrow;
            public Sprite rightArrow;

            // Setas agrupadas (usado pelo InputIconVariant).
            public Sprite arrows;
            public Sprite arrowsVertical;
            public Sprite arrowsHorizontal;

            // Modificadores.
            /*public Sprite shift;
            public Sprite ctrl;
            public Sprite alt;*/

            // Funções.
            /*public Sprite f1, f2, f3, f4, f5, f6;
            public Sprite f7, f8, f9, f10, f11, f12;*/

            // Numpad.
            /*public Sprite numpad0, numpad1, numpad2, numpad3, numpad4;
            public Sprite numpad5, numpad6, numpad7, numpad8, numpad9;
            public Sprite numpadEnter;
            public Sprite numpadPlus;
            public Sprite numpadMinus;
            public Sprite numpadMultiply;
            public Sprite numpadDivide;
            public Sprite numpadPeriod;*/

            // Outros.
            /*public Sprite capsLock;
            public Sprite insert;
            public Sprite home;
            public Sprite end;
            public Sprite pageUp;
            public Sprite pageDown;*/

            // Mouse.
            /*public Sprite leftButton;
            public Sprite rightButton;
            public Sprite middleButton;  // Scroll click
            public Sprite forwardButton; // Botão lateral 1
            public Sprite backButton;    // Botão lateral 2
            public Sprite scrollUp;
            public Sprite scrollDown;*/


            // Dicionário dos icones.
            private Dictionary<string, Sprite> keyboardIcons;

            // Inicializa o dicionario.
            public void Init()
            {
                keyboardIcons = new()
            {
                // Letras.
                { "a", a }, { "b", b }, { "c", c }, { "d", d }, { "e", e },
                { "f", f }, { "g", g }, { "h", h }, { "i", i }, { "j", j },
                { "k", k }, { "l", l }, { "m", m }, { "n", n }, { "o", o },
                { "p", p }, { "q", q }, { "r", r }, { "s", s }, { "t", t },
                { "u", u }, { "v", v }, { "w", w }, { "x", x }, { "y", y },
                { "z", z },

                // Números.
                /*{ "0", zero }, { "1", one }, { "2", two }, { "3", three },
                { "4", four }, { "5", five }, { "6", six }, { "7", seven },
                { "8", eight }, { "9", nine },*/

                // Especiais.
                { "space", space },
                { "enter", enter },
                { "tab", tab },
                { "escape", escape },
                { "backspace", backspace },
                { "delete", delete },

                // Setas.
                { "upArrow", upArrow },
                { "downArrow", downArrow },
                { "leftArrow", leftArrow },
                { "rightArrow", rightArrow },

                // Modificadores.
                /*{ "leftShift", shift },
                { "rightShift", shift },
                { "leftCtrl", ctrl },
                { "rightCtrl", ctrl },
                { "leftAlt", alt },
                { "rightAlt", alt },*/

                // Funções.
                /*{ "f1", f1 }, { "f2", f2 }, { "f3", f3 }, { "f4", f4 },
                { "f5", f5 }, { "f6", f6 }, { "f7", f7 }, { "f8", f8 },
                { "f9", f9 }, { "f10", f10 }, { "f11", f11 }, { "f12", f12 },*/

                // Numpad.
                /*{ "numpad0", numpad0 },
                { "numpad1", numpad1 },
                { "numpad2", numpad2 },
                { "numpad3", numpad3 },
                { "numpad4", numpad4 },
                { "numpad5", numpad5 },
                { "numpad6", numpad6 },
                { "numpad7", numpad7 },
                { "numpad8", numpad8 },
                { "numpad9", numpad9 },
                { "numpadEnter", numpadEnter },
                { "numpadPlus", numpadPlus },
                { "numpadMinus", numpadMinus },
                { "numpadMultiply", numpadMultiply },
                { "numpadDivide", numpadDivide },
                { "numpadPeriod", numpadPeriod },*/

                // Outros.
                /*{ "capsLock", capsLock },
                { "insert", insert },
                { "home", home },
                { "end", end },
                { "pageUp", pageUp },
                { "pageDown", pageDown },*/

                // Mouse.
                /*{ "leftButton", leftButton },
                { "rightButton", rightButton },
                { "middleButton", middleButton },
                { "forwardButton", forwardButton },
                { "backButton", backButton },
                { "scrollUp", scrollUp },
                { "scrollDown", scrollDown }*/
            };
            }

            // Pega o sprite correspondente a bind.
            public Sprite GetIcon(string controlPath, InputIconVariant variant = InputIconVariant.Single)
            {
                if (variant != InputIconVariant.Single && controlPath.EndsWith("Arrow"))
                {
                    Sprite group = GetGroup(variant, arrows, arrowsVertical, arrowsHorizontal);
                    if (group != null) return group;
                }

                return keyboardIcons.TryGetValue(controlPath, out var sprite) && sprite != null ? sprite : defaultKey;
            }
        }

        // Escolhe o sprite agrupado conforme a variante.
        private static Sprite GetGroup(InputIconVariant variant, Sprite all, Sprite vertical, Sprite horizontal)
        {
            return variant switch
            {
                InputIconVariant.All => all,
                InputIconVariant.Vertical => vertical,
                InputIconVariant.Horizontal => horizontal,
                _ => null
            };
        }

        public static InputIconsManager Instance => inputIconsManager;
        private static InputIconsManager inputIconsManager;

        [SerializeField, Tooltip("Icones do Teclado")]
        private KeyboardIcons keyboard;

        [SerializeField, Tooltip("Icones do Xbox")]
        private GamepadIcons xbox;

        [SerializeField, Tooltip("Icones do PS")]
        private GamepadIcons pS;

        [SerializeField, Tooltip("Icones do Switch")]
        private GamepadIcons switchGp;

        // Events.
        public event Action<InputDevice> OnIconsChanged;
        public InputDevice CurrentDevice { get; private set; }

        private IPlayerInputController playerInputController;

        private void Awake()
        {
            if (inputIconsManager == null)
            {
                inputIconsManager = this;
            }
            else if (inputIconsManager != this)
            {
                Debug.Log($"Manager duplicado: Principal = {inputIconsManager.name} | Duplicado = {gameObject.name}");
                Destroy(gameObject);
            }

            // Inicializa os dicionários.
            keyboard.Init();
            xbox.Init();
            pS.Init();
            switchGp.Init();
        }

        private void Start()
        {
            // Se inscreve no evento de troca de dispositivo.
            PlayerId playerId = new PlayerId(0);
            playerInputController = ServiceLocator.Get<IInputService>().GetInputFromPlayer(playerId);
            playerInputController.OnCurrentDeviceChanged += PlayerInputController_OnCurrentDeviceChanged;

            PlayerInputController_OnCurrentDeviceChanged(playerInputController.CurrentDevice);
        }

        private void OnDestroy()
        {
            playerInputController.OnCurrentDeviceChanged -= PlayerInputController_OnCurrentDeviceChanged;
        }

        private void PlayerInputController_OnCurrentDeviceChanged(InputDevice currentDevice)
        {
            CurrentDevice = currentDevice;
            OnIconsChanged?.Invoke(currentDevice);
        }

        /// <summary>
        /// Retorna o ícone correspondente a bind.
        /// </summary>
        public Sprite GetIcon(string binding, InputIconVariant variant = InputIconVariant.Single)
        {
            string gamepadBindName = binding.Replace("<Gamepad>/", "");

            switch (playerInputController.CurrentDevice)
            {
                case Mouse:
                case Keyboard:
                    // Seta o ícone do teclado correspondente a tecla.
                    string bindingName = binding.Replace("<Keyboard>/", "").Replace("<Mouse>/", "");
                    return keyboard.GetIcon(bindingName, variant);

                case XInputController:
                    return xbox.GetIcon(gamepadBindName, variant);

                case DualSenseGamepadHID:
                    return pS.GetIcon(gamepadBindName, variant);

                case DualShockGamepad:
                    return pS.GetIcon(gamepadBindName, variant);

                case SwitchProControllerHID: 
                    return switchGp.GetIcon(gamepadBindName, variant);

                default:
                    Core.Logger.LogError($"[{nameof(InputIconsManager)}] Device não foi reconhecido? {playerInputController.CurrentDevice}");
                    return null;
            }
        }
    }
}

