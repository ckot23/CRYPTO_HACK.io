using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>Виджет, которому нужно обновляться каждый кадр.</summary>
    public interface IUiTick
    {
        void Tick();
    }

    /// <summary>Виджет, который ловит клики (своя замена EventSystem + Button).</summary>
    public interface IClickable
    {
        RectTransform Rt { get; }
        int Layer { get; }
        bool IsActive { get; }
        void OnPress();
        void OnRelease(bool inside);
        void OnHover(bool inside);
    }

    /// <summary>
    /// Фабрика элементов интерфейса — перенос scripts/ui/neon_ui.gd.
    ///
    /// Здесь же — собственный роутер кликов: игра не использует EventSystem,
    /// стандартные Button и InputField. Почему так:
    ///   * не зависит от «Active Input Handling» в настройках проекта;
    ///   * не нужны Input System UI-модули и их настройки;
    ///   * порядок кликов предсказуем — окна перекрывают друг друга по Layer.
    /// </summary>
    public static class Ui
    {
        public static readonly List<IClickable> Clickables = new List<IClickable>();
        public static readonly List<IUiTick> Tickers = new List<IUiTick>();

        public static Canvas Canvas;
        public static RectTransform Root;        // корень экрана
        public static RectTransform ModalLayer;  // модальные окна: модалка, меню, LEVEL UP
        public static RectTransform ToastRect;   // всплывающие уведомления

        public const int LayerDesktop = 0;
        public const int LayerWindowBase = 10;
        public const int LayerModal = 500;
        public const int LayerToast = 900;

        static IClickable _pressed;

        // ==================== РЕГИСТРАЦИЯ ====================
        public static void Register(IClickable c)
        {
            if (!Clickables.Contains(c)) Clickables.Add(c);
        }

        public static void Unregister(IClickable c)
        {
            Clickables.Remove(c);
            if (_pressed == c) _pressed = null;
        }

        public static void RegisterTick(IUiTick t)
        {
            if (!Tickers.Contains(t)) Tickers.Add(t);
        }

        public static void UnregisterTick(IUiTick t)
        {
            Tickers.Remove(t);
        }

        /// <summary>Разослать ввод по виджетам. Вызывается до их Tick().</summary>
        public static void UpdateInput()
        {
            IClickable top = Topmost(UiInput.MousePosition);

            for (int i = 0; i < Clickables.Count; i++)
            {
                IClickable c = Clickables[i];
                c.OnHover(c == top && c.IsActive);
            }

            if (UiInput.MouseDown)
            {
                _pressed = top;
                if (_pressed != null) _pressed.OnPress();
            }
            if (UiInput.MouseUp)
            {
                if (_pressed != null) _pressed.OnRelease(_pressed == top);
                _pressed = null;
            }
        }

        /// <summary>Верхний кликабельный элемент под курсором (по Layer, потом по порядку создания).</summary>
        public static IClickable Topmost(Vector2 pos)
        {
            int best = int.MinValue;
            for (int i = 0; i < Clickables.Count; i++)
            {
                IClickable c = Clickables[i];
                if (!c.IsActive || c.Rt == null) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(c.Rt, pos, null)) continue;
                if (c.Layer > best) best = c.Layer;
            }
            if (best == int.MinValue) return null;
            for (int i = Clickables.Count - 1; i >= 0; i--)
            {
                IClickable c = Clickables[i];
                if (!c.IsActive || c.Rt == null || c.Layer != best) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(c.Rt, pos, null)) return c;
            }
            return null;
        }

        /// <summary>Есть ли кликабельный элемент слоя >= minLayer под курсором (для перетаскивания окон).</summary>
        public static bool HasClickableAt(Vector2 pos, int minLayer)
        {
            for (int i = 0; i < Clickables.Count; i++)
            {
                IClickable c = Clickables[i];
                if (!c.IsActive || c.Rt == null) continue;
                if (c.Layer < minLayer) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(c.Rt, pos, null)) return true;
            }
            return false;
        }

        // ==================== БАЗОВЫЕ УЗЛЫ ====================
        public static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Full(RectTransform rt)
        {
            return Stretch(rt, 0f, 0f, 0f, 0f);
        }

        /// <summary>Абсолютная позиция от левого верхнего угла родителя.</summary>
        public static RectTransform TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Абсолютная позиция от правого верхнего угла родителя.</summary>
        public static RectTransform TopRight(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        // ==================== ЦВЕТНЫЕ ПРЯМОУГОЛЬНИКИ ====================
        public static Image Img(RectTransform parent, Color color, string name = "Img")
        {
            RectTransform rt = Node(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Solid;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Рамка толщиной 1 «пиксель» (спрайт-кольцо + Image.Type.Sliced).</summary>
        public static Image Border(RectTransform parent, Color color)
        {
            RectTransform rt = Node("Border", parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Border;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            Full(rt);
            return img;
        }

        public static void SetBorder(Image border, Color color)
        {
            if (border != null) border.color = color;
        }

        // ==================== ТЕКСТ ====================
        public static Text Label(RectTransform parent, string text, int size, Color color,
            TextAnchor anchor, bool bold, bool wrap)
        {
            RectTransform rt = Node("Text", parent);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = bold && Theme.MonoBold != null ? Theme.MonoBold : Theme.Mono;
            t.fontSize = size;
            t.color = color;
            t.text = text;
            t.alignment = anchor;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Однострочная метка с фиксированной высотой (для layout-групп).</summary>
        public static Text Line(RectTransform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            Text t = Label(parent, text, size, color, anchor, bold, false);
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            float h = Theme.LineHeight(size) + 2f;
            le.preferredHeight = h;
            le.minHeight = h;
            return t;
        }

        /// <summary>Метка с переносом слов; высота считается по моношрифту (см. Theme.WrappedHeight).</summary>
        public static Text Paragraph(RectTransform parent, string text, int size, Color color,
            float width, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            Text t = Label(parent, text, size, color, anchor, false, true);
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            float h = Theme.WrappedHeight(text, size, width * 0.98f) + 2f;
            le.preferredHeight = h;
            le.minHeight = h;
            le.flexibleWidth = 1f;
            return t;
        }

        public static LayoutElement Height(GameObject go, float h)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredHeight = h;
            le.minHeight = h;
            return le;
        }

        public static RectTransform Spacer(RectTransform parent, float h)
        {
            RectTransform rt = Node("Spacer", parent);
            Height(rt.gameObject, h);
            return rt;
        }

        // ==================== КОНТЕЙНЕРЫ ====================
        public static RectTransform VBox(RectTransform parent, float spacing, int padding)
        {
            return VBox(parent, spacing, padding, padding, padding, padding);
        }

        public static RectTransform VBox(RectTransform parent, float spacing,
            int left, int top, int right, int bottom)
        {
            RectTransform rt = Node("VBox", parent);
            VerticalLayoutGroup g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(left, right, top, bottom);
            g.childAlignment = TextAnchor.UpperLeft;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform HBox(RectTransform parent, float spacing, int padding)
        {
            RectTransform rt = Node("HBox", parent);
            HorizontalLayoutGroup g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childAlignment = TextAnchor.MiddleLeft;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform Grid(RectTransform parent, int columns, float spacing)
        {
            RectTransform rt = Node("Grid", parent);
            GridLayoutGroup g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(160f, 90f);
            g.spacing = new Vector2(spacing, spacing);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            g.childAlignment = TextAnchor.UpperLeft;
            return rt;
        }

        // ==================== КАРТОЧКА ====================
        /// <summary>Панель с рамкой (аналог NeonUI.card в Godot-версии).</summary>
        public static RectTransform Card(RectTransform parent, Color border, Color bg, string name = "Card")
        {
            RectTransform rt = Node(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Solid;
            img.color = bg;
            img.raycastTarget = false;
            if (border.a > 0.001f) Border(rt, border);
            return rt;
        }

        // ==================== ПРОКРУТКА ====================
        public static UiScroll Scroll(RectTransform parent, string name = "Scroll")
        {
            UiScroll s = new UiScroll();
            s.Build(parent, name, 0f, 0);
            return s;
        }

        // ==================== УТИЛИТЫ ====================
        public static void DestroyChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
            }
        }

        /// <summary>Пересчитать раскладку контейнера немедленно.</summary>
        public static void Rebuild(RectTransform rt)
        {
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
