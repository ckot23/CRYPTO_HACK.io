using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>
    /// Палитра, шрифты и мелкие утилиты — перенос scripts/core/neon.gd.
    ///
    /// Отличие от Godot: там шрифты собирались в цепочку fallbacks (latin →
    /// cyrillic → DejaVu). В Unity у legacy Text цепочки нет, поэтому взят
    /// один шрифт с полным покрытием — DejaVu Sans Mono (кириллица, греческий,
    /// стрелки, рамки, ★ ● ▲). Единственный символ, которого в нём нет, — ₿:
    /// для него отдельный шрифт JetBrainsMono-LatinExt (см. IconFontFor).
    /// </summary>
    public static class Theme
    {
        // ---------------- Палитра (из src/index.css @theme) ----------------
        public static readonly Color Bg = Hex("#04070f");
        public static readonly Color Panel = Hex("#0a111f");
        public static readonly Color PanelDark = Hex("#070d1a");
        public static readonly Color PanelDeep = Hex("#050b16");
        public static readonly Color PanelHead = Hex("#0b1322");
        public static readonly Color PanelBar = Hex("#0a1120");

        public static readonly Color Green = Hex("#00ff9d");
        public static readonly Color GreenHover = Hex("#5cffb8");
        public static readonly Color Cyan = Hex("#00e5ff");
        public static readonly Color CyanHover = Hex("#7df3ff");
        public static readonly Color Pink = Hex("#ff2d78");
        public static readonly Color Yellow = Hex("#ffe600");
        public static readonly Color YellowHover = Hex("#fff36b");
        public static readonly Color Orange = Hex("#ff9f43");

        public static readonly Color Text = Hex("#d7e3f4");
        public static readonly Color TextSoft = Hex("#b9c7dd");
        public static readonly Color TextDim = Hex("#8fa3bd");
        public static readonly Color TextMuted = Hex("#5b6b85");
        public static readonly Color TextFaint = Hex("#3a4a63");
        public static readonly Color Black = Hex("#000000");

        public static readonly Color White05 = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color White10 = new Color(1f, 1f, 1f, 0.10f);
        public static readonly Color White15 = new Color(1f, 1f, 1f, 0.15f);
        public static readonly Color White20 = new Color(1f, 1f, 1f, 0.20f);

        public static Font Mono;
        public static Font MonoBold;
        public static Font IconFont;      // для ₿
        public static Sprite Solid;
        public static Sprite Border;      // рамка 1px: спрайт-кольцо для Image.Type.Sliced

        /// <summary>Ширина символа моношрифта в долях от кегля (DejaVu Sans Mono: 1233/2048).</summary>
        public const float CharWidthRatio = 0.60205f;
        public const float LineHeightRatio = 1.34f;

        static bool _ready;

        public static void Init()
        {
            if (_ready) return;
            _ready = true;

            Mono = LoadFont("Fonts/DejaVuSansMono");
            MonoBold = LoadFont("Fonts/DejaVuSansMono-Bold");
            IconFont = LoadFont("Fonts/JetBrainsMono-LatinExt");

            if (Mono == null) Mono = BuiltinFont();
            if (MonoBold == null) MonoBold = Mono;
            if (IconFont == null) IconFont = Mono;

            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            Solid = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            Border = MakeBorderSprite();
        }

        /// <summary>Спрайт-рамка 3x3: белое кольцо, прозрачная середина (тип Sliced, border = 1px).</summary>
        static Sprite MakeBorderSprite()
        {
            Texture2D tex = new Texture2D(3, 3, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    bool edge = x == 0 || y == 0 || x == 2 || y == 2;
                    tex.SetPixel(x, y, edge ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, 3f, 3f), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(1f, 1f, 1f, 1f));
        }

        static Font LoadFont(string path)
        {
            Font f = Resources.Load<Font>(path);
            if (f == null) Debug.LogWarning("Не найден шрифт Assets/Resources/" + path + ".ttf");
            return f;
        }

        static Font BuiltinFont()
        {
            // запасной вариант, если шрифты из Resources почему-то не импортировались
            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.Exception) { }
            try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch (System.Exception) { }
            return null;
        }

        /// <summary>
        /// Шрифт для текста с иконкой: если основного моношрифта не хватает
        /// (например нет глифа ₿), берём JetBrainsMono-LatinExt.
        /// Важно: тексты на кириллице так подменять нельзя — в сабсете JetBrains
        /// кириллицы нет (см. INSTALL.md в корне репозитория).
        /// </summary>
        public static Font IconFontFor(string text)
        {
            if (string.IsNullOrEmpty(text) || IconFont == null || Mono == null) return Mono;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 128) continue;
                if (!Mono.HasCharacter(c) && IconFont.HasCharacter(c)) return IconFont;
            }
            return Mono;
        }

        public static Color Hex(string hex)
        {
            Color c;
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out c)) return c;
            return Color.white;
        }

        public static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        public static Color Brightness(Color c, float mul)
        {
            return new Color(Mathf.Clamp01(c.r * mul), Mathf.Clamp01(c.g * mul), Mathf.Clamp01(c.b * mul), c.a);
        }

        public static Color Mix(Color a, Color b, float t)
        {
            return new Color(Mathf.Lerp(a.r, b.r, t), Mathf.Lerp(a.g, b.g, t),
                Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.a, b.a, t));
        }

        /// <summary>Цвет по «ключу» из данных (green/cyan/pink/yellow/orange).</summary>
        public static Color ByKey(string key)
        {
            if (key == "cyan") return Cyan;
            if (key == "pink") return Pink;
            if (key == "yellow") return Yellow;
            if (key == "orange") return Orange;
            if (key == "red") return Pink;
            return Green;
        }

        public static Color KindColor(string kind)
        {
            if (kind == "ok") return Green;
            if (kind == "gold") return Yellow;
            if (kind == "err") return Pink;
            if (kind == "warn") return Orange;
            return Cyan;
        }

        // ---------------- Измерение текста (моношрифт!) ----------------
        public static float CharWidth(int fontSize)
        {
            return (float)fontSize * CharWidthRatio;
        }

        public static float LineHeight(int fontSize)
        {
            return (float)fontSize * LineHeightRatio;
        }

        /// <summary>Сколько строк займёт текст при заданной ширине (жадная переноска слов).</summary>
        public static int WrappedLineCount(string text, int fontSize, float width)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            float charW = CharWidth(fontSize);
            if (charW <= 0f) return 1;
            int perLine = Mathf.Max(1, (int)(width / charW));
            int lines = 0;
            string[] paragraphs = text.Split('\n');
            for (int p = 0; p < paragraphs.Length; p++)
            {
                string para = paragraphs[p];
                if (para.Length == 0)
                {
                    lines++;
                    continue;
                }
                int cur = 0;
                string[] words = para.Split(' ');
                for (int w = 0; w < words.Length; w++)
                {
                    int len = words[w].Length;
                    if (cur == 0)
                    {
                        cur = len;
                        if (len > perLine)   // очень длинное слово рвём по символам
                        {
                            lines += (len - 1) / perLine;
                            cur = len % perLine;
                        }
                    }
                    else if (cur + 1 + len <= perLine)
                    {
                        cur += 1 + len;
                    }
                    else
                    {
                        lines++;
                        cur = len;
                    }
                }
                lines++;
            }
            return Mathf.Max(1, lines);
        }

        public static float WrappedHeight(string text, int fontSize, float width)
        {
            return (float)WrappedLineCount(text, fontSize, width) * LineHeight(fontSize);
        }
    }
}
