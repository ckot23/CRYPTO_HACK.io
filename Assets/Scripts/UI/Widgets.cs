using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>Кнопка — свой виджет вместо EventSystem + Button.</summary>
    public class UiButton : IClickable
    {
        public const int Solid = 0;
        public const int Outline = 1;
        public const int Ghost = 2;

        public RectTransform Rt;
        public Image Bg;
        public Image Frame;
        public Text Caption;
        public Action OnClick;
        public Color Accent = Color.white;
        public int Kind = Solid;
        public int CustomLayer;
        public Func<int> LayerProvider;
        public float FontSize = 12f;
        public bool Disabled;

        bool _hover;
        bool _press;

        public int Layer { get { return LayerProvider != null ? LayerProvider() : CustomLayer; } }

        public bool IsActive
        {
            get { return !Disabled && Rt != null && Rt.gameObject.activeInHierarchy; }
        }

        public static UiButton New(RectTransform parent, string text, Color accent, int size, int kind, float height)
        {
            UiButton b = new UiButton();
            b.Accent = accent;
            b.Kind = kind;

            b.Rt = Ui.Node("Button", parent);
            Ui.Height(b.Rt.gameObject, height);

            b.Bg = b.Rt.gameObject.AddComponent<Image>();
            b.Bg.sprite = Theme.Solid;
            b.Bg.raycastTarget = false;

            if (kind != Ghost) b.Frame = Ui.Border(b.Rt, accent);

            b.Caption = Ui.Label(b.Rt, text, size, accent, TextAnchor.MiddleCenter, true, false);
            Ui.Full(b.Caption.rectTransform);

            Ui.Register(b);
            b.Refresh();

            // курсор-рука, если система это поддерживает
            return b;
        }

        public void SetText(string text)
        {
            if (Caption != null) Caption.text = text;
        }

        public void SetAccent(Color accent, int kind)
        {
            Accent = accent;
            Kind = kind;
            Refresh();
        }

        public void SetDisabled(bool disabled)
        {
            if (Disabled == disabled) return;
            Disabled = disabled;
            Refresh();
        }

        public void OnPress()
        {
            if (!IsActive) return;
            _press = true;
            Refresh();
        }

        public void OnRelease(bool inside)
        {
            bool wasPress = _press;
            _press = false;
            Refresh();
            if (wasPress && inside && IsActive && OnClick != null) OnClick();
        }

        public void OnHover(bool inside)
        {
            if (_hover == inside) return;
            _hover = inside;
            Refresh();
        }

        void Refresh()
        {
            if (Bg == null || Caption == null) return;

            if (Disabled)
            {
                Bg.color = Theme.White05;
                if (Frame != null) Frame.color = Theme.White10;
                Caption.color = Theme.TextFaint;
                return;
            }

            if (Kind == Ghost)
            {
                Bg.color = _press ? Theme.White15 : (_hover ? Theme.White10 : Theme.White05);
                Caption.color = _hover || _press ? Theme.Brightness(Accent, 1.2f) : Theme.TextDim;
                return;
            }

            if (Kind == Outline)
            {
                Bg.color = _press ? Theme.WithAlpha(Accent, 0.22f)
                    : (_hover ? Theme.WithAlpha(Accent, 0.12f) : new Color(0f, 0f, 0f, 0.35f));
                if (Frame != null) Frame.color = _hover || _press ? Accent : Theme.WithAlpha(Accent, 0.55f);
                Caption.color = _hover || _press ? Theme.Brightness(Accent, 1.25f) : Accent;
                return;
            }

            // Solid
            Bg.color = _press ? Theme.WithAlpha(Accent, 0.42f)
                : (_hover ? Theme.WithAlpha(Accent, 0.28f) : Theme.WithAlpha(Accent, 0.16f));
            if (Frame != null) Frame.color = _hover || _press ? Accent : Theme.WithAlpha(Accent, 0.75f);
            Caption.color = _hover || _press ? Theme.Brightness(Accent, 1.3f) : Accent;
        }
    }

    /// <summary>Прокручиваемый список — свой виджет (в Godot был ScrollContainer).</summary>
    public class UiScroll : IUiTick
    {
        public RectTransform View;
        public RectTransform Content;
        public float ScrollSpeed = 48f;

        float _scroll;

        public void Build(RectTransform parent, string name, float spacing, int padding)
        {
            View = Ui.Node(name, parent);

            Image bg = View.gameObject.AddComponent<Image>();
            bg.sprite = Theme.Solid;
            bg.color = new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = false;
            View.gameObject.AddComponent<RectMask2D>();

            Content = Ui.Node("Content", View);
            Content.anchorMin = new Vector2(0f, 1f);
            Content.anchorMax = new Vector2(1f, 1f);
            Content.pivot = new Vector2(0.5f, 1f);
            Content.anchoredPosition = Vector2.zero;
            Content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup g = Content.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childAlignment = TextAnchor.UpperLeft;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;

            ContentSizeFitter fitter = Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Ui.RegisterTick(this);
        }

        public void ScrollToTop()
        {
            _scroll = 0f;
            Apply();
        }

        public void ScrollToBottom()
        {
            _scroll = Mathf.Max(0f, Content.rect.height - View.rect.height);
            Apply();
        }

        public void Tick()
        {
            if (View == null || Content == null) return;

            if (Mathf.Abs(UiInput.Scroll) > 0.001f && UiInput.Hover(View))
            {
                _scroll -= UiInput.Scroll * ScrollSpeed;
            }

            float max = Mathf.Max(0f, Content.rect.height - View.rect.height);
            _scroll = Mathf.Clamp(_scroll, 0f, max);
            Apply();
        }

        void Apply()
        {
            Vector2 pos = Content.anchoredPosition;
            pos.y = -_scroll;
            Content.anchoredPosition = pos;
        }
    }

    /// <summary>Полоса прогресса (XP, износ, загрузка).</summary>
    public class UiProgress
    {
        public RectTransform Rt;
        public Image Track;
        public Image Fill;
        public Image Frame;

        public static UiProgress New(RectTransform parent, Color color, float height)
        {
            UiProgress p = new UiProgress();
            p.Rt = Ui.Node("Progress", parent);
            Ui.Height(p.Rt.gameObject, height);

            p.Track = p.Rt.gameObject.AddComponent<Image>();
            p.Track.sprite = Theme.Solid;
            p.Track.color = Theme.WithAlpha(color, 0.14f);
            p.Track.raycastTarget = false;

            RectTransform fillRt = Ui.Node("Fill", p.Rt);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            p.Fill = fillRt.gameObject.AddComponent<Image>();
            p.Fill.sprite = Theme.Solid;
            p.Fill.color = color;
            p.Fill.raycastTarget = false;

            p.Frame = Ui.Border(p.Rt, Theme.WithAlpha(color, 0.35f));
            p.SetValue(0f);
            return p;
        }

        public void SetValue(float value)
        {
            float v = Mathf.Clamp01(value);
            if (Fill == null) return;
            Fill.rectTransform.anchorMax = new Vector2(v, 1f);
            Fill.rectTransform.offsetMin = Vector2.zero;
            Fill.rectTransform.offsetMax = Vector2.zero;
        }

        public void SetColor(Color color)
        {
            if (Fill != null) Fill.color = color;
            if (Track != null) Track.color = Theme.WithAlpha(color, 0.14f);
            if (Frame != null) Frame.color = Theme.WithAlpha(color, 0.35f);
        }
    }

    /// <summary>График курса: текстура рисуется вручную (в Godot был Control._draw).</summary>
    public class UiChart
    {
        public RectTransform Rt;
        public RawImage Image;
        public Color LineColor = Theme.Green;
        List<float> _values = new List<float>();
        int _texW = 320;
        int _texH = 96;
        Texture2D _tex;

        public static UiChart New(RectTransform parent, float height, int width)
        {
            UiChart c = new UiChart();
            c._texW = Mathf.Max(64, width);
            c._texH = Mathf.Max(32, (int)height);

            RectTransform rt = Ui.Node("Chart", parent);
            c.Rt = rt;
            Ui.Height(rt.gameObject, height);

            Image bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = Theme.Solid;
            bg.color = new Color(0f, 0f, 0f, 0.35f);
            bg.raycastTarget = false;
            Ui.Border(rt, Theme.White10);

            RectTransform imgRt = Ui.Node("Tex", rt);
            Ui.Stretch(imgRt, 1f, 1f, 1f, 1f);
            c.Image = imgRt.gameObject.AddComponent<RawImage>();
            c.Image.raycastTarget = false;

            c._tex = new Texture2D(c._texW, c._texH, TextureFormat.RGBA32, false);
            c._tex.filterMode = FilterMode.Point;
            c._tex.wrapMode = TextureWrapMode.Clamp;
            c.Image.texture = c._tex;
            c.Clear();
            return c;
        }

        public void SetValues(List<float> values, Color color)
        {
            LineColor = color;
            _values = values != null ? new List<float>(values) : new List<float>();
            Redraw();
        }

        void Clear()
        {
            Color[] px = new Color[_texW * _texH];
            Color empty = new Color(0f, 0f, 0f, 0f);
            for (int i = 0; i < px.Length; i++) px[i] = empty;
            _tex.SetPixels(px);
            _tex.Apply();
        }

        void Redraw()
        {
            Clear();
            if (_values.Count < 2) return;

            float min = _values[0];
            float max = _values[0];
            for (int i = 0; i < _values.Count; i++)
            {
                if (_values[i] < min) min = _values[i];
                if (_values[i] > max) max = _values[i];
            }
            float span = Mathf.Max(max - min, 1e-6f);

            // сетка
            Color grid = new Color(1f, 1f, 1f, 0.06f);
            for (int g = 1; g < 4; g++)
            {
                int y = _texH * g / 4;
                for (int x = 0; x < _texW; x++) _tex.SetPixel(x, y, grid);
            }

            // линия
            int prevX = -1;
            int prevY = -1;
            for (int i = 0; i < _values.Count; i++)
            {
                int x = (int)((float)i / (float)(_values.Count - 1) * (_texW - 1));
                float norm = (_values[i] - min) / span;
                int y = Mathf.Clamp((int)(norm * (_texH - 6)) + 3, 0, _texH - 1);

                if (prevX >= 0)
                {
                    int steps = Mathf.Max(Mathf.Abs(x - prevX), Mathf.Abs(y - prevY));
                    for (int s = 0; s <= steps; s++)
                    {
                        float t = steps == 0 ? 0f : (float)s / (float)steps;
                        int px = Mathf.RoundToInt(Mathf.Lerp(prevX, x, t));
                        int py = Mathf.RoundToInt(Mathf.Lerp(prevY, y, t));
                        DrawDot(px, py, LineColor, 1f);
                        DrawDot(px, py - 1, LineColor, 0.45f);
                    }
                }
                else
                {
                    DrawDot(x, y, LineColor, 1f);
                }
                prevX = x;
                prevY = y;
            }

            _tex.Apply();
        }

        void DrawDot(int x, int y, Color color, float alpha)
        {
            if (x < 0 || y < 0 || x >= _texW || y >= _texH) return;
            Color c = _tex.GetPixel(x, y);
            Color blended = Theme.Mix(c, new Color(color.r, color.g, color.b, alpha), alpha);
            _tex.SetPixel(x, y, blended);
        }
    }

    /// <summary>Модальное окно по центру экрана (меню «Как играть», LEVEL UP и т. п.).</summary>
    public class UiModal : IUiTick
    {
        public RectTransform Rt;
        public RectTransform Card;
        public int CustomLayer;
        public UiButton CloseBtn;
        public Action OnClose;
        public bool AutoClose;
        public float Life = 0f;

        public static UiModal New(RectTransform parent, float width, float height, Color accent, bool dim)
        {
            UiModal m = new UiModal();
            m.Rt = Ui.Node("Modal", parent);
            Ui.Full(m.Rt);

            if (dim)
            {
                Image shade = m.Rt.gameObject.AddComponent<Image>();
                shade.sprite = Theme.Solid;
                shade.color = new Color(0f, 0f, 0f, 0.62f);
                shade.raycastTarget = false;
            }

            m.Card = Ui.Node("Card", m.Rt);
            m.Card.anchorMin = new Vector2(0.5f, 0.5f);
            m.Card.anchorMax = new Vector2(0.5f, 0.5f);
            m.Card.pivot = new Vector2(0.5f, 0.5f);
            m.Card.anchoredPosition = Vector2.zero;
            m.Card.sizeDelta = new Vector2(width, height);

            Image bg = m.Card.gameObject.AddComponent<Image>();
            bg.sprite = Theme.Solid;
            bg.color = Theme.PanelDark;
            bg.raycastTarget = false;
            Ui.Border(m.Card, Theme.WithAlpha(accent, 0.6f));

            // перехватываем клики «мимо» модалки, чтобы они не улетали в окна под ней
            UiBlocker.New(m.Rt, Ui.LayerModal - 1);

            Ui.RegisterTick(m);
            return m;
        }

        public void Close()
        {
            if (OnClose != null) OnClose();
            Destroy();
        }

        public void Destroy()
        {
            Ui.UnregisterTick(this);
            if (Rt != null) UnityEngine.Object.Destroy(Rt.gameObject);
            Rt = null;
        }

        public void Tick()
        {
            if (AutoClose)
            {
                Life -= Time.unscaledDeltaTime;
                if (Life <= 0f || UiInput.KeyDown(UiKey.Escape)) Close();
            }
        }
    }

    /// <summary>
    /// Перехватчик кликов: занимает весь экран и «съедает» нажатия на своём слое
    /// (нужен под модалками и меню, чтобы клик не проваливался в окна ниже).
    /// </summary>
    public class UiBlocker : IClickable
    {
        public RectTransform Rt;
        public int CustomLayer;
        public int Layer { get { return CustomLayer; } }
        public bool IsActive { get { return Rt != null && Rt.gameObject.activeInHierarchy; } }

        public static UiBlocker New(RectTransform parent, int layer)
        {
            UiBlocker b = new UiBlocker();
            b.Rt = Ui.Node("Blocker", parent);
            Ui.Full(b.Rt);
            b.CustomLayer = layer;
            Ui.Register(b);
            return b;
        }

        public void OnPress() { }
        public void OnRelease(bool inside) { }
        public void OnHover(bool inside) { }
    }

    /// <summary>Всплывающие уведомления в правом нижнем углу (перенос toast_layer.gd).</summary>
    public class UiToastLayer : IUiTick
    {
        const float CardWidth = 320f;
        const float Gap = 8f;
        const float BottomMargin = 14f;
        const float RightMargin = 14f;
        const float Lifetime = 4.2f;
        const int MaxToasts = 4;

        public RectTransform Rt;

        class Toast
        {
            public RectTransform Rt;
            public Text TitleText;
            public Text BodyText;
            public Image Frame;
            public float Life;
            public float Height;
        }

        readonly List<Toast> _toasts = new List<Toast>();

        public static UiToastLayer New(RectTransform parent)
        {
            UiToastLayer layer = new UiToastLayer();
            layer.Rt = Ui.Node("Toasts", parent);
            Ui.Full(layer.Rt);
            Ui.RegisterTick(layer);
            return layer;
        }

        public void Show(string title, string text, string kind)
        {
            Color accent = Theme.KindColor(kind);
            if (_toasts.Count >= MaxToasts)
            {
                Toast oldest = _toasts[0];
                _toasts.RemoveAt(0);
                if (oldest.Rt != null) UnityEngine.Object.Destroy(oldest.Rt.gameObject);
            }

            Toast t = new Toast();
            t.Rt = Ui.Node("Toast", Rt);
            t.Rt.anchorMin = new Vector2(1f, 0f);
            t.Rt.anchorMax = new Vector2(1f, 0f);
            t.Rt.pivot = new Vector2(1f, 0f);
            t.Rt.sizeDelta = new Vector2(CardWidth, 10f);
            t.Life = Lifetime;

            Image bg = t.Rt.gameObject.AddComponent<Image>();
            bg.sprite = Theme.Solid;
            bg.color = Theme.WithAlpha(Theme.PanelDark, 0.97f);
            bg.raycastTarget = false;
            t.Frame = Ui.Border(t.Rt, Theme.WithAlpha(accent, 0.75f));

            float wrapWidth = CardWidth - 24f;
            RectTransform box = Ui.VBox(t.Rt, 2f, 10, 8, 10, 8);
            Ui.Full(box);

            t.TitleText = Ui.Paragraph(box, title, 12, accent, wrapWidth);
            t.TitleText.font = Theme.MonoBold != null ? Theme.MonoBold : Theme.Mono;
            t.BodyText = Ui.Paragraph(box, text, 11, Theme.TextSoft, wrapWidth);

            float h = 8f + 8f + Theme.WrappedHeight(title, 12, wrapWidth) + 2f
                + Theme.WrappedHeight(text, 11, wrapWidth) + 4f;
            t.Height = h;
            t.Rt.sizeDelta = new Vector2(CardWidth, h);

            _toasts.Add(t);
            Relayout();
        }

        public void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            bool changed = false;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                Toast t = _toasts[i];
                t.Life -= dt;
                if (t.Life <= 0f)
                {
                    if (t.Rt != null) UnityEngine.Object.Destroy(t.Rt.gameObject);
                    _toasts.RemoveAt(i);
                    changed = true;
                }
                else if (t.Rt != null && t.Life < 0.6f)
                {
                    CanvasGroup cg = t.Rt.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = Mathf.Clamp01(t.Life / 0.6f);
                }
            }
            if (changed) Relayout();
        }

        void Relayout()
        {
            float y = BottomMargin;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                Toast t = _toasts[i];
                if (t.Rt == null) continue;
                t.Rt.anchoredPosition = new Vector2(-RightMargin, y);
                if (t.Rt.GetComponent<CanvasGroup>() == null) t.Rt.gameObject.AddComponent<CanvasGroup>();
                y += t.Height + Gap;
            }
        }
    }
}
