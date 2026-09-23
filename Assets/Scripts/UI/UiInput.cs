using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace CryptoHack
{
    public enum UiKey
    {
        Backspace,
        Delete,
        Enter,
        Tab,
        Escape,
        Left,
        Right,
        Up,
        Down,
        Home,
        End,
        Space,
        Digit1,
        Digit2,
        Digit3,
        Digit4,
        Digit5,
        Digit6,
        Digit7,
        Digit8,
        Digit9
    }

    /// <summary>
    /// Ввод с мыши и клавиатуры, работающий и со старым Input Manager, и с новым
    /// Input System (в Unity 6 новые проекты по умолчанию используют его).
    ///
    /// Своя абстракция нужна ещё и потому, что игра не использует EventSystem и
    /// стандартные Button/InputField: весь интерфейс — самодельные виджеты
    /// (см. Ui.cs и Widgets.cs). Это убирает зависимость от Input System UI-
    /// модулей и связанных с ними настроек проекта.
    /// </summary>
    public static class UiInput
    {
        public static Vector2 MousePosition;
        public static bool MouseDown;      // нажатие в этом кадре
        public static bool MouseUp;        // отпускание в этом кадре
        public static bool MouseHeld;      // кнопка зажата
        public static float Scroll;        // колесо (+вверх) в этом кадре
        public static string TypedText = "";

        /// <summary>Непустая строка — ввод недоступен, текст подсказывает, что включить.</summary>
        public static string ErrorMessage = "";

        static readonly List<char> _typedBuffer = new List<char>();
        static bool _init;
        static bool _inputBroken;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        static bool _textHooked;

        static void OnTextInput(char c)
        {
            if (c >= ' ') _typedBuffer.Add(c);
        }

        /// <summary>
        /// Подписываемся на текст лениво: в Unity 6 устройства ввода создаются
        /// не мгновенно, и на старте Keyboard.current может быть ещё null.
        /// </summary>
        static void HookTextInput()
        {
            if (_textHooked) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            kb.onTextInput += OnTextInput;
            _textHooked = true;
        }
#endif

        public static void Init()
        {
            if (_init) return;
            _init = true;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            HookTextInput();
#endif
            Probe();
        }

        /// <summary>
        /// Разовая проверка ввода при старте. Если в Player Settings выбран
        /// «Input System Package (New)», а пакет не установлен (или наоборот),
        /// обращение к UnityEngine.Input бросает исключение — ловим это сразу
        /// и показываем понятную подсказку вместо спама ошибок каждый кадр.
        /// </summary>
        static void Probe()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Mouse.current == null && Keyboard.current == null)
            {
                ErrorMessage = "Input System не видит мышь и клавиатуру — проверь, что пакет Input System установлен.";
                Debug.LogWarning("CRYPTO_HACK: " + ErrorMessage);
            }
#else
            try
            {
                // само обращение к Input и есть проверка
                _ = Input.mousePosition;
                _ = Input.GetMouseButton(0);
            }
            catch (System.Exception e)
            {
                _inputBroken = true;
                ErrorMessage = "Ввод отключён: Edit → Project Settings → Player → Other Settings → Active Input Handling "
                    + "поставь «Input Manager (Old)» или «Both», иначе клики не работают.";
                Debug.LogError("CRYPTO_HACK: " + ErrorMessage + " (" + e.GetType().Name + ")");
            }
#endif
        }

        /// <summary>Вызывается один раз в начале кадра (из GameBoot).</summary>
        public static void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            HookTextInput();
            MousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            MouseDown = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            MouseUp = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            MouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
            Scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
            if (_typedBuffer.Count > 0)
            {
                TypedText = new string(_typedBuffer.ToArray());
                _typedBuffer.Clear();
            }
            else
            {
                TypedText = "";
            }
#else
            if (_inputBroken)
            {
                MouseDown = false;
                MouseUp = false;
                MouseHeld = false;
                Scroll = 0f;
                TypedText = "";
                return;
            }
            MousePosition = Input.mousePosition;
            MouseDown = Input.GetMouseButtonDown(0);
            MouseUp = Input.GetMouseButtonUp(0);
            MouseHeld = Input.GetMouseButton(0);
            Scroll = Input.mouseScrollDelta.y;
            TypedText = Input.inputString;
#endif
        }

        public static bool Ctrl
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
#else
                if (_inputBroken) return false;
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                    || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
#endif
            }
        }

        public static bool KeyDown(UiKey key)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Keyboard.current == null) return false;
            switch (key)
            {
                case UiKey.Backspace: return Keyboard.current[Key.Backspace].wasPressedThisFrame;
                case UiKey.Delete: return Keyboard.current[Key.Delete].wasPressedThisFrame;
                case UiKey.Enter: return Keyboard.current[Key.Enter].wasPressedThisFrame
                        || Keyboard.current[Key.NumpadEnter].wasPressedThisFrame;
                case UiKey.Tab: return Keyboard.current[Key.Tab].wasPressedThisFrame;
                case UiKey.Escape: return Keyboard.current[Key.Escape].wasPressedThisFrame;
                case UiKey.Left: return Keyboard.current[Key.LeftArrow].wasPressedThisFrame;
                case UiKey.Right: return Keyboard.current[Key.RightArrow].wasPressedThisFrame;
                case UiKey.Up: return Keyboard.current[Key.UpArrow].wasPressedThisFrame;
                case UiKey.Down: return Keyboard.current[Key.DownArrow].wasPressedThisFrame;
                case UiKey.Home: return Keyboard.current[Key.Home].wasPressedThisFrame;
                case UiKey.End: return Keyboard.current[Key.End].wasPressedThisFrame;
                case UiKey.Space: return Keyboard.current[Key.Space].wasPressedThisFrame;
                case UiKey.Digit1: return Keyboard.current[Key.Digit1].wasPressedThisFrame;
                case UiKey.Digit2: return Keyboard.current[Key.Digit2].wasPressedThisFrame;
                case UiKey.Digit3: return Keyboard.current[Key.Digit3].wasPressedThisFrame;
                case UiKey.Digit4: return Keyboard.current[Key.Digit4].wasPressedThisFrame;
                case UiKey.Digit5: return Keyboard.current[Key.Digit5].wasPressedThisFrame;
                case UiKey.Digit6: return Keyboard.current[Key.Digit6].wasPressedThisFrame;
                case UiKey.Digit7: return Keyboard.current[Key.Digit7].wasPressedThisFrame;
                case UiKey.Digit8: return Keyboard.current[Key.Digit8].wasPressedThisFrame;
                case UiKey.Digit9: return Keyboard.current[Key.Digit9].wasPressedThisFrame;
            }
            return false;
#else
            if (_inputBroken) return false;
            switch (key)
            {
                case UiKey.Backspace: return Input.GetKeyDown(KeyCode.Backspace);
                case UiKey.Delete: return Input.GetKeyDown(KeyCode.Delete);
                case UiKey.Enter: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
                case UiKey.Tab: return Input.GetKeyDown(KeyCode.Tab);
                case UiKey.Escape: return Input.GetKeyDown(KeyCode.Escape);
                case UiKey.Left: return Input.GetKeyDown(KeyCode.LeftArrow);
                case UiKey.Right: return Input.GetKeyDown(KeyCode.RightArrow);
                case UiKey.Up: return Input.GetKeyDown(KeyCode.UpArrow);
                case UiKey.Down: return Input.GetKeyDown(KeyCode.DownArrow);
                case UiKey.Home: return Input.GetKeyDown(KeyCode.Home);
                case UiKey.End: return Input.GetKeyDown(KeyCode.End);
                case UiKey.Space: return Input.GetKeyDown(KeyCode.Space);
                case UiKey.Digit1: return Input.GetKeyDown(KeyCode.Alpha1);
                case UiKey.Digit2: return Input.GetKeyDown(KeyCode.Alpha2);
                case UiKey.Digit3: return Input.GetKeyDown(KeyCode.Alpha3);
                case UiKey.Digit4: return Input.GetKeyDown(KeyCode.Alpha4);
                case UiKey.Digit5: return Input.GetKeyDown(KeyCode.Alpha5);
                case UiKey.Digit6: return Input.GetKeyDown(KeyCode.Alpha6);
                case UiKey.Digit7: return Input.GetKeyDown(KeyCode.Alpha7);
                case UiKey.Digit8: return Input.GetKeyDown(KeyCode.Alpha8);
                case UiKey.Digit9: return Input.GetKeyDown(KeyCode.Alpha9);
            }
            return false;
#endif
        }

        /// <summary>Пробел — используется для «пропустить» на экране загрузки.</summary>
        public static bool SpaceDown { get { return KeyDown(UiKey.Space); } }

        /// <summary>Нажата любая клавиша (кроме модификаторов) — для «пропустить».</summary>
        public static bool AnyKeyDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
#else
                return !_inputBroken && Input.anyKeyDown;
#endif
            }
        }

        /// <summary>Цифра 1..9 в верхнем ряду (0 — не нажата), для горячих клавиш окон.</summary>
        public static int DigitDown
        {
            get
            {
                for (int i = 0; i < 9; i++)
                {
                    if (KeyDown((UiKey)((int)UiKey.Digit1 + i))) return i + 1;
                }
                return 0;
            }
        }

        /// <summary>Курсор находится внутри прямоугольника (экранные координаты).</summary>
        public static bool Hover(RectTransform rect)
        {
            if (rect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, MousePosition, null);
        }
    }
}
