using System;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>
    /// Перетаскиваемое окно с шапкой и прокруткой содержимого —
    /// перенос scripts/ui/neon_window.gd (там это был PanelContainer + ScrollContainer).
    /// </summary>
    public class UiWindow : IUiTick
    {
        public const float HeaderHeight = 30f;
        public const float DragBlockY = 44f;      // не заезжать под верхнюю панель
        public const float WideWidth = 880f;
        public const float NormalWidth = 660f;

        public string Id = "";
        public RectTransform Rt;
        public RectTransform Header;
        public RectTransform BodyHolder;
        public UiScroll Scroll;
        public Text TitleText;
        public UiButton CloseButton;
        public Image Frame;
        public Image HeaderBg;

        public Color Accent = Theme.Green;
        public bool DragEnabled = true;
        public int LayerValue = Ui.LayerWindowBase;
        public Action<UiWindow> OnCloseRequested;
        public Action<UiWindow> OnFocusRequested;

        bool _dragging;
        Vector2 _dragOffset;
        bool _closing;

        public int Layer { get { return LayerValue; } }

        public static UiWindow New(RectTransform parent, string id, string title, Color accent,
            bool wide, int seedIndex, float height)
        {
            UiWindow w = new UiWindow();
            w.Id = id;
            w.Accent = accent;

            float width = wide ? WideWidth : NormalWidth;
            float s = (float)(seedIndex % 7) * 36f;

            w.Rt = Ui.Node("Window_" + id, parent);
            Ui.TopLeft(w.Rt, 48f + s, 72f + s, width, height);

            Image bg = w.Rt.gameObject.AddComponent<Image>();
            bg.sprite = Theme.Solid;
            bg.color = Theme.PanelDark;
            bg.raycastTarget = false;
            w.Frame = Ui.Border(w.Rt, Theme.WithAlpha(accent, 0.5f));

            // ---- шапка ----
            w.Header = Ui.Node("Header", w.Rt);
            w.Header.anchorMin = new Vector2(0f, 1f);
            w.Header.anchorMax = new Vector2(1f, 1f);
            w.Header.pivot = new Vector2(0.5f, 1f);
            w.Header.offsetMin = new Vector2(0f, -HeaderHeight);
            w.Header.offsetMax = Vector2.zero;

            w.HeaderBg = w.Header.gameObject.AddComponent<Image>();
            w.HeaderBg.sprite = Theme.Solid;
            w.HeaderBg.color = Theme.PanelHead;
            w.HeaderBg.raycastTarget = false;

            RectTransform underline = Ui.Node("Underline", w.Header);
            Ui.TopLeft(underline, 0f, HeaderHeight - 1f, width, 1f);
            Image ul = underline.gameObject.AddComponent<Image>();
            ul.sprite = Theme.Solid;
            ul.color = Theme.WithAlpha(accent, 0.55f);
            ul.raycastTarget = false;

            w.TitleText = Ui.Label(w.Header, title, 12, accent, TextAnchor.MiddleLeft, true, false);
            Ui.TopLeft(w.TitleText.rectTransform, 10f, 0f, width - 60f, HeaderHeight);

            w.CloseButton = UiButton.New(w.Header, "✗", Theme.Pink, 12, UiButton.Ghost, 24f);
            Ui.TopRight(w.CloseButton.Rt, -6f, 3f, 26f, 24f);
            w.CloseButton.LayerProvider = delegate { return w.LayerValue + 1; };
            w.CloseButton.OnClick = delegate { w.RequestClose(); };

            // ---- тело с прокруткой ----
            w.BodyHolder = Ui.Node("Body", w.Rt);
            Ui.Stretch(w.BodyHolder, 0f, HeaderHeight, 0f, 0f);

            w.Scroll = Ui.Scroll(w.BodyHolder, "ScrollView");
            Ui.Full(w.Scroll.View);

            Ui.RegisterTick(w);
            return w;
        }

        public void SetLayer(int layer)
        {
            LayerValue = layer;
        }

        public void SetTitle(string title)
        {
            if (TitleText != null) TitleText.text = title;
        }

        public void SetAccent(Color accent)
        {
            Accent = accent;
            if (TitleText != null) TitleText.color = accent;
            if (Frame != null) Frame.color = Theme.WithAlpha(accent, 0.5f);
            if (CloseButton != null) CloseButton.SetAccent(Theme.Pink, UiButton.Ghost);
        }

        void RequestClose()
        {
            if (_closing) return;
            _closing = true;
            if (OnCloseRequested != null) OnCloseRequested(this);
        }

        public void Tick()
        {
            if (Rt == null) return;

            if (UiInput.MouseDown && UiInput.Hover(Rt) && OnFocusRequested != null)
            {
                OnFocusRequested(this);
            }

            if (DragEnabled && UiInput.MouseDown && UiInput.Hover(Header)
                && !Ui.HasClickableAt(UiInput.MousePosition, LayerValue + 1))
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        (RectTransform)Rt.parent, UiInput.MousePosition, null, out local))
                {
                    _dragging = true;
                    _dragOffset = local - Rt.anchoredPosition;
                }
            }

            if (_dragging && UiInput.MouseHeld)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        (RectTransform)Rt.parent, UiInput.MousePosition, null, out local))
                {
                    Vector2 target = local - _dragOffset;
                    RectTransform parent = (RectTransform)Rt.parent;
                    float maxX = parent.rect.width - 120f;
                    float maxY = parent.rect.height - 60f;
                    target.x = Mathf.Clamp(target.x, -(Rt.rect.width - 220f), Mathf.Max(maxX, 220f));
                    target.y = Mathf.Clamp(target.y, -maxY, -DragBlockY);
                    Rt.anchoredPosition = target;
                }
            }

            if (UiInput.MouseUp) _dragging = false;
        }

        public void Destroy()
        {
            Ui.UnregisterTick(this);
            if (Scroll != null) Ui.UnregisterTick(Scroll);
            if (Rt != null) UnityEngine.Object.Destroy(Rt.gameObject);
            Rt = null;
        }
    }
}
