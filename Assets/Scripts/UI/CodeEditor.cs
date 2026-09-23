using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>
    /// Редактор Python с подсветкой синтаксиса — замена CodeEdit из Godot.
    ///
    /// Почему свой, а не InputField/TMP_InputField: стандартные поля требуют
    /// EventSystem и модуль ввода, а игра обходится без них. Здесь всё просто:
    /// текст рисуется одним Text с rich-text-разметкой, каретка — тонкая Image,
    /// ввод берётся из UiInput.
    /// </summary>
    public class CodeEditor : IUiTick, IClickable
    {
        public const int FontSize = 13;
        public const int Pad = 8;
        const int MaxUndo = 0;

        public RectTransform Rt;
        public RectTransform Viewport;
        public Text Display;
        public Image CaretImage;

        public Action<string> OnChanged;

        public Func<int> LayerProvider;
        public int CustomLayer;

        List<string> _lines = new List<string>();
        int _line;
        int _col;
        bool _focused;
        float _scroll;
        float _blink;

        public int Layer { get { return LayerProvider != null ? LayerProvider() : CustomLayer; } }

        public bool IsActive
        {
            get { return Rt != null && Rt.gameObject.activeInHierarchy; }
        }

        public bool Focused { get { return _focused; } }

        // ==================== СОЗДАНИЕ ====================
        public static CodeEditor New(RectTransform parent, string initial, float height)
        {
            CodeEditor e = new CodeEditor();

            e.Rt = Ui.Node("CodeEditor", parent);
            Ui.Height(e.Rt.gameObject, height);

            Image bg = e.Rt.gameObject.AddComponent<Image>();
            bg.sprite = Theme.Solid;
            bg.color = Theme.Black;
            bg.raycastTarget = false;
            Ui.Border(e.Rt, Theme.White15);

            e.Viewport = Ui.Node("Viewport", e.Rt);
            Ui.Stretch(e.Viewport, Pad + 2f, Pad, Pad + 2f, Pad);
            e.Viewport.gameObject.AddComponent<RectMask2D>();

            e.Display = Ui.Label(e.Viewport, "", FontSize, Theme.Text, TextAnchor.UpperLeft, false, false);
            RectTransform dr = e.Display.rectTransform;
            dr.anchorMin = new Vector2(0f, 1f);
            dr.anchorMax = new Vector2(1f, 1f);
            dr.pivot = new Vector2(0f, 1f);
            dr.anchoredPosition = Vector2.zero;
            dr.sizeDelta = new Vector2(0f, 10f);
            e.Display.lineSpacing = 1.15f;   // см. Theme.LineHeightRatio = 1.34

            RectTransform caretRt = Ui.Node("Caret", e.Viewport);
            caretRt.anchorMin = new Vector2(0f, 1f);
            caretRt.anchorMax = new Vector2(0f, 1f);
            caretRt.pivot = new Vector2(0f, 1f);
            caretRt.sizeDelta = new Vector2(2f, Theme.LineHeight(FontSize));
            e.CaretImage = caretRt.gameObject.AddComponent<Image>();
            e.CaretImage.sprite = Theme.Solid;
            e.CaretImage.color = Theme.Green;
            e.CaretImage.raycastTarget = false;

            Ui.Register(e);
            Ui.RegisterTick(e);

            e.SetText(string.IsNullOrEmpty(initial) ? "# пиши код здесь\n" : initial, true);
            return e;
        }

        // ==================== ТЕКСТ ====================
        public string GetText()
        {
            return string.Join("\n", _lines.ToArray());
        }

        public void SetText(string text, bool caretToEnd)
        {
            _lines.Clear();
            string[] parts = (text == null ? "" : text.Replace("\r\n", "\n")).Split('\n');
            for (int i = 0; i < parts.Length; i++) _lines.Add(parts[i]);
            if (_lines.Count == 0) _lines.Add("");

            if (caretToEnd)
            {
                _line = _lines.Count - 1;
                _col = _lines[_line].Length;
            }
            else
            {
                _line = Mathf.Clamp(_line, 0, _lines.Count - 1);
                _col = Mathf.Clamp(_col, 0, _lines[_line].Length);
            }
            _scroll = 0f;
            Redraw();
            Changed();
        }

        public void Focus()
        {
            _focused = true;
            if (CaretImage != null) CaretImage.enabled = true;
        }

        public void Blur()
        {
            _focused = false;
            if (CaretImage != null) CaretImage.enabled = false;
        }

        void Changed()
        {
            if (OnChanged != null) OnChanged(GetText());
        }

        // ==================== КЛИКИ ====================
        public void OnPress()
        {
            Focus();
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Viewport, UiInput.MousePosition, null, out local))
            {
                float charW = Theme.CharWidth(FontSize);
                float lineH = Theme.LineHeight(FontSize);
                int line = Mathf.FloorToInt((-(local.y - _scroll)) / lineH);
                line = Mathf.Clamp(line, 0, _lines.Count - 1);
                int col = charW > 0f ? Mathf.RoundToInt((local.x) / charW) : 0;
                col = Mathf.Clamp(col, 0, _lines[line].Length);
                _line = line;
                _col = col;
                Redraw();
            }
        }

        public void OnRelease(bool inside) { }

        public void OnHover(bool inside) { }

        // ==================== ВВОД ====================
        public void Tick()
        {
            if (Rt == null) return;

            if (UiInput.MouseDown && !UiInput.Hover(Rt)) Blur();

            if (!_focused)
            {
                Redraw();
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _blink += dt;

            if (UiInput.KeyDown(UiKey.Backspace)) Backspace();
            if (UiInput.KeyDown(UiKey.Delete)) DeleteForward();
            if (UiInput.KeyDown(UiKey.Enter)) NewLine();
            if (UiInput.KeyDown(UiKey.Tab)) Insert("    ");
            if (UiInput.KeyDown(UiKey.Left)) MoveLeft();
            if (UiInput.KeyDown(UiKey.Right)) MoveRight();
            if (UiInput.KeyDown(UiKey.Up)) MoveUp();
            if (UiInput.KeyDown(UiKey.Down)) MoveDown();
            if (UiInput.KeyDown(UiKey.Home)) _col = 0;
            if (UiInput.KeyDown(UiKey.End)) _col = _lines[_line].Length;

            string typed = Sanitize(UiInput.TypedText);
            if (typed.Length > 0) Insert(typed);

            Redraw();
        }

        static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\b' || c == '\n' || c == '\r') continue;   // обрабатываем отдельно
                if (c < ' ') continue;
                sb.Append(c);
            }
            return sb.ToString();
        }

        void Insert(string text)
        {
            string line = _lines[_line];
            string head = line.Substring(0, _col);
            string tail = line.Substring(_col);
            _lines[_line] = head + text + tail;
            _col += text.Length;
            Changed();
        }

        void Backspace()
        {
            if (_col > 0)
            {
                string line = _lines[_line];
                _lines[_line] = line.Substring(0, _col - 1) + line.Substring(_col);
                _col--;
            }
            else if (_line > 0)
            {
                int prevLen = _lines[_line - 1].Length;
                _lines[_line - 1] = _lines[_line - 1] + _lines[_line];
                _lines.RemoveAt(_line);
                _line--;
                _col = prevLen;
            }
            Changed();
        }

        void DeleteForward()
        {
            string line = _lines[_line];
            if (_col < line.Length)
            {
                _lines[_line] = line.Substring(0, _col) + line.Substring(_col + 1);
            }
            else if (_line < _lines.Count - 1)
            {
                _lines[_line] = line + _lines[_line + 1];
                _lines.RemoveAt(_line + 1);
            }
            Changed();
        }

        void NewLine()
        {
            string line = _lines[_line];
            string head = line.Substring(0, _col);
            string tail = line.Substring(_col);
            // автоотступ: повторяем пробелы в начале строки
            StringBuilder indent = new StringBuilder();
            for (int i = 0; i < head.Length; i++)
            {
                if (head[i] == ' ' || head[i] == '\t') indent.Append(head[i]);
                else break;
            }
            if (tail.Trim().Length == 0 && _col > 0 && head.Trim().Length > 0 && head.EndsWith(":"))
            {
                indent.Append("    ");
            }

            _lines[_line] = head;
            _lines.Insert(_line + 1, indent.ToString() + tail);
            _line++;
            _col = indent.Length;
            Changed();
        }

        void MoveLeft()
        {
            if (_col > 0) _col--;
            else if (_line > 0)
            {
                _line--;
                _col = _lines[_line].Length;
            }
        }

        void MoveRight()
        {
            if (_col < _lines[_line].Length) _col++;
            else if (_line < _lines.Count - 1)
            {
                _line++;
                _col = 0;
            }
        }

        void MoveUp()
        {
            if (_line > 0)
            {
                _line--;
                _col = Mathf.Min(_col, _lines[_line].Length);
            }
        }

        void MoveDown()
        {
            if (_line < _lines.Count - 1)
            {
                _line++;
                _col = Mathf.Min(_col, _lines[_line].Length);
            }
        }

        // ==================== ОТРИСОВКА ====================
        void Redraw()
        {
            if (Display == null) return;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _lines.Count; i++)
            {
                sb.Append(Highlight(_lines[i]));
                if (i < _lines.Count - 1) sb.Append("\n");
            }
            Display.text = sb.ToString();

            float lineH = Theme.LineHeight(FontSize);
            float charW = Theme.CharWidth(FontSize);
            float contentH = (float)_lines.Count * lineH;
            float viewH = Mathf.Max(1f, Viewport.rect.height);

            float caretY = (float)_line * lineH;
            if (caretY + lineH > _scroll + viewH) _scroll = caretY + lineH - viewH;
            if (caretY < _scroll) _scroll = caretY;
            _scroll = Mathf.Clamp(_scroll, 0f, Mathf.Max(0f, contentH - viewH + Pad * 2f));

            RectTransform dr = Display.rectTransform;
            dr.anchoredPosition = new Vector2(0f, -_scroll);
            dr.sizeDelta = new Vector2(0f, contentH + 2f);

            if (CaretImage != null)
            {
                RectTransform cr = CaretImage.rectTransform;
                cr.anchoredPosition = new Vector2(charW * _col, -(_scroll + caretY));
                float pulse = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(_blink * 3.4f));
                CaretImage.color = Theme.WithAlpha(Theme.Green, pulse);
            }
        }

        // ==================== ПОДСВЕТКА ====================
        static readonly string[] Keywords =
        {
            "def", "for", "in", "if", "elif", "else", "while", "return", "import", "from", "as",
            "and", "or", "not", "break", "continue", "pass", "class", "try", "except", "with", "lambda"
        };

        static readonly string[] Builtins =
        {
            "print", "range", "len", "str", "int", "float", "bool", "list", "dict", "sum", "enumerate",
            "scan", "connect", "brute", "bypass", "decrypt", "extract", "drain", "install_miner", "wallets"
        };

        static readonly string[] Constants = { "True", "False", "None" };

        public static string Highlight(string line)
        {
            if (line == null) return "";
            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];

                if (c == '#')
                {
                    sb.Append(Colorize(Escape(line.Substring(i)), "#3a4a63"));
                    break;
                }

                if (c == '"' || c == '\'')
                {
                    int j = i + 1;
                    while (j < line.Length)
                    {
                        if (line[j] == '\\') { j += 2; continue; }
                        if (line[j] == c) { j++; break; }
                        j++;
                    }
                    if (j > line.Length) j = line.Length;
                    sb.Append(Colorize(Escape(line.Substring(i, j - i)), "#ffe600"));
                    i = j;
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int j = i;
                    while (j < line.Length && (char.IsDigit(line[j]) || line[j] == '.')) j++;
                    sb.Append(Colorize(Escape(line.Substring(i, j - i)), "#ff9f43"));
                    i = j;
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int j = i;
                    while (j < line.Length && (char.IsLetterOrDigit(line[j]) || line[j] == '_')) j++;
                    string word = line.Substring(i, j - i);
                    string color = null;
                    if (Contains(Constants, word)) color = "#00e5ff";
                    else if (Contains(Keywords, word)) color = "#ff2d78";
                    else if (Contains(Builtins, word)) color = "#00ff9d";

                    // вызов функции: имя + (
                    if (color == null && j < line.Length && line[j] == '(') color = "#00e5ff";

                    sb.Append(color == null ? Escape(word) : Colorize(Escape(word), color));
                    i = j;
                    continue;
                }

                sb.Append(Escape(c.ToString()));
                i++;
            }
            return sb.ToString();
        }

        static bool Contains(string[] arr, string word)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == word) return true;
            }
            return false;
        }

        static string Colorize(string escaped, string hex)
        {
            return "<color=" + hex + ">" + escaped + "</color>";
        }

        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }

    /// <summary>
    /// Консоль хак-терминала: строки с задержками (как в симуляторе) и автопрокруткой.
    /// </summary>
    public class UiConsole : IUiTick
    {
        public UiScroll Scroll;
        public Text Display;

        class Pending
        {
            public string Text;
            public string Kind;
            public float At;
        }

        readonly List<string> _rendered = new List<string>();
        readonly List<Pending> _queue = new List<Pending>();
        LayoutElement _displayLe;
        float _time;
        int _maxLines = 220;

        public static UiConsole New(RectTransform parent, float height)
        {
            UiConsole c = new UiConsole();
            c.Scroll = Ui.Scroll(parent, "Console");
            Ui.Height(c.Scroll.View.gameObject, height);

            Image bg = c.Scroll.View.gameObject.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0f, 0f, 0f, 0.55f);
            Ui.Border(c.Scroll.View, Theme.White10);

            c.Display = Ui.Label(c.Scroll.Content, "", 11, Theme.Text, TextAnchor.UpperLeft, false, true);
            c.Display.lineSpacing = 1.1f;
            c._displayLe = c.Display.gameObject.AddComponent<LayoutElement>();
            c._displayLe.flexibleWidth = 1f;
            c._displayLe.preferredHeight = Theme.LineHeight(11) + 4f;

            Ui.RegisterTick(c);
            return c;
        }

        public void Clear()
        {
            _rendered.Clear();
            _queue.Clear();
            _time = 0f;
            Refresh();
        }

        public void AddLine(string text, string kind)
        {
            _rendered.Add(Format(text, kind));
            Trim();
            Refresh();
        }

        /// <summary>Строка с задержкой — как в логе симулятора (delay в миллисекундах).</summary>
        public void AddDelayed(string text, string kind, int delayMs)
        {
            Pending p = new Pending();
            p.Text = text;
            p.Kind = kind;
            p.At = _time + (float)delayMs / 1000f;
            _queue.Add(p);
        }

        public void FlushAll()
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                _rendered.Add(Format(_queue[i].Text, _queue[i].Kind));
            }
            _queue.Clear();
            Trim();
            Refresh();
        }

        public void Tick()
        {
            if (_queue.Count == 0) return;
            _time += Time.unscaledDeltaTime;
            bool added = false;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                if (_time >= _queue[i].At)
                {
                    _rendered.Add(Format(_queue[i].Text, _queue[i].Kind));
                    _queue.RemoveAt(i);
                    added = true;
                }
            }
            if (added)
            {
                Trim();
                Refresh();
            }
        }

        void Trim()
        {
            while (_rendered.Count > _maxLines) _rendered.RemoveAt(0);
        }

        void Refresh()
        {
            if (Display == null) return;
            Display.text = string.Join("\n", _rendered.ToArray());
            if (_displayLe != null)
            {
                // внутри VerticalLayoutGroup высоту текста нужно задать вручную
                float h = Mathf.Max(1, _rendered.Count) * Theme.LineHeight(11) * 1.1f + 8f;
                _displayLe.preferredHeight = h;
                _displayLe.minHeight = h;
            }
            Scroll.ScrollToBottom();
        }

        static string Format(string text, string kind)
        {
            string color;
            if (kind == PySim.KindCmd) color = "#00ff9d";
            else if (kind == PySim.KindOk) color = "#00ff9d";
            else if (kind == PySim.KindInfo) color = "#8fa3bd";
            else if (kind == PySim.KindWarn) color = "#ff9f43";
            else if (kind == PySim.KindErr) color = "#ff2d78";
            else color = "#d7e3f4";
            return "<color=" + color + ">" + CodeEditor.Escape(text) + "</color>";
        }
    }

    /// <summary>
    /// Однострочное поле ввода (замена LineEdit из Godot-версии).
    /// Как и CodeEditor, обходится без InputField и EventSystem.
    /// </summary>
    public class UiInputField : IUiTick, IClickable
    {
        public RectTransform Rt;
        public Text Display;
        public Image Caret;
        public Action<string> OnChanged;
        public Action OnSubmit;
        public Func<int> LayerProvider;
        public int CustomLayer;
        public Color Accent = Theme.Cyan;
        public int MaxLength = 12;

        string _text = "";
        bool _focused;
        float _blink;

        public int Layer { get { return LayerProvider != null ? LayerProvider() : CustomLayer; } }
        public bool IsActive { get { return Rt != null && Rt.gameObject.activeInHierarchy; } }
        public bool Focused { get { return _focused; } }

        public static UiInputField New(RectTransform parent, string initial, float width, float height, Color accent)
        {
            UiInputField f = new UiInputField();
            f.Accent = accent;
            f._text = initial == null ? "" : initial;

            f.Rt = Ui.Node("InputField", parent);
            Ui.Height(f.Rt.gameObject, height);
            RectTransform rt = f.Rt;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, height);

            Image bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = Theme.Solid;
            bg.color = Theme.Black;
            bg.raycastTarget = false;
            Ui.Border(rt, Theme.White15);

            f.Display = Ui.Label(rt, f._text, 12, Theme.Text, TextAnchor.MiddleLeft, false, false);
            Ui.Stretch(f.Display.rectTransform, 8f, 0f, 26f, 0f);

            RectTransform caretRt = Ui.Node("Caret", rt);
            caretRt.anchorMin = new Vector2(0f, 0.5f);
            caretRt.anchorMax = new Vector2(0f, 0.5f);
            caretRt.pivot = new Vector2(0f, 0.5f);
            caretRt.sizeDelta = new Vector2(2f, height - 6f);
            caretRt.anchoredPosition = new Vector2(8f, 0f);
            f.Caret = caretRt.gameObject.AddComponent<Image>();
            f.Caret.sprite = Theme.Solid;
            f.Caret.color = accent;
            f.Caret.raycastTarget = false;
            f.Caret.enabled = false;

            Text suffix = Ui.Label(rt, "$", 12, Theme.TextMuted, TextAnchor.MiddleRight, false, false);
            Ui.TopRight(suffix.rectTransform, -8f, 0f, 20f, height);

            Ui.Register(f);
            Ui.RegisterTick(f);
            return f;
        }

        public string Text { get { return _text; } }

        public void SetText(string text, bool notify)
        {
            _text = text == null ? "" : text;
            if (Display != null) Display.text = _text;
            UpdateCaret();
            if (notify && OnChanged != null) OnChanged(_text);
        }

        public float Value
        {
            get
            {
                float v;
                string norm = _text.Replace(",", ".").Trim();
                return float.TryParse(norm, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out v) ? Mathf.Max(0f, v) : 0f;
            }
        }

        public void Focus()
        {
            _focused = true;
            if (Caret != null) Caret.enabled = true;
        }

        public void Blur()
        {
            _focused = false;
            if (Caret != null) Caret.enabled = false;
        }

        public void OnPress()
        {
            Focus();
        }

        public void OnRelease(bool inside) { }
        public void OnHover(bool inside) { }

        void UpdateCaret()
        {
            if (Caret == null) return;
            float x = 8f + Theme.CharWidth(12) * _text.Length;
            RectTransform rt = Caret.rectTransform;
            rt.anchoredPosition = new Vector2(x, 0f);
        }

        public void Tick()
        {
            if (Rt == null) return;

            if (UiInput.MouseDown && !UiInput.Hover(Rt)) Blur();
            if (!_focused)
            {
                if (Caret != null) Caret.enabled = false;
                return;
            }

            bool changed = false;
            if (UiInput.KeyDown(UiKey.Backspace) && _text.Length > 0)
            {
                _text = _text.Substring(0, _text.Length - 1);
                changed = true;
            }
            if (UiInput.KeyDown(UiKey.Enter) && OnSubmit != null) OnSubmit();

            string typed = UiInput.TypedText;
            for (int i = 0; i < typed.Length; i++)
            {
                char c = typed[i];
                if (c < ' ') continue;
                bool ok = char.IsDigit(c) || c == '.' || c == ',';
                if (!ok || _text.Length >= MaxLength) continue;
                _text += c;
                changed = true;
            }

            _blink += Time.unscaledDeltaTime;
            if (Caret != null)
            {
                Caret.enabled = true;
                Caret.color = Theme.WithAlpha(Accent, 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(_blink * 3.4f)));
            }

            if (changed)
            {
                if (Display != null) Display.text = _text;
                UpdateCaret();
                if (OnChanged != null) OnChanged(_text);
            }
        }
    }
}
