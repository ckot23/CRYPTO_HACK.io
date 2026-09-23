using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>
    /// Точки сетки на фоне — перенос scripts/ui/grid_bg.gd.
    /// В Godot это был Control._draw(); здесь — одна текстура 28x28,
    /// замощённая через RawImage.uvRect (тайлинг).
    /// </summary>
    public class GridBackground
    {
        public RectTransform Rt;
        public RawImage Image;

        const int Step = 28;

        public static GridBackground New(RectTransform parent)
        {
            GridBackground g = new GridBackground();
            g.Rt = Ui.Node("GridBg", parent);
            Ui.Full(g.Rt);

            RectTransform tex = Ui.Node("Tex", g.Rt);
            Ui.Full(tex);
            g.Image = tex.gameObject.AddComponent<RawImage>();
            g.Image.texture = MakeTexture();
            g.Image.raycastTarget = false;
            g.Image.color = new Color(1f, 1f, 1f, 0.14f);
            return g;
        }

        static Texture2D MakeTexture()
        {
            Texture2D t = new Texture2D(Step, Step, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Repeat;
            for (int y = 0; y < Step; y++)
            {
                for (int x = 0; x < Step; x++)
                {
                    bool dot = x == 0 && y == 0;
                    t.SetPixel(x, y, dot ? Theme.Green : new Color(1f, 1f, 1f, 0f));
                }
            }
            t.Apply();
            return t;
        }

        /// <summary>Пересчитывает тайлинг под текущий размер (вызывается при изменении размера окна).</summary>
        public void Refresh()
        {
            if (Rt == null || Image == null) return;
            float w = Mathf.Max(1f, Rt.rect.width);
            float h = Mathf.Max(1f, Rt.rect.height);
            Image.uvRect = new Rect(0f, 0f, w / Step, h / Step);
        }
    }

    /// <summary>
    /// «Матричный дождь» — перенос scripts/ui/matrix_bg.gd.
    /// Вместо сотен нод — пул текстовых колонок, символы меняются по таймеру.
    /// </summary>
    public class MatrixBackground : IUiTick
    {
        public RectTransform Rt;

        const int Columns = 32;
        const int RowsPerColumn = 12;
        const float StepSeconds = 0.066f;
        const int FontSize = 13;
        const string Charset = "01ABCDEF$#@%&Ξ<>+*";

        readonly List<Text> _texts = new List<Text>();
        readonly List<float> _y = new List<float>();
        readonly List<float> _speed = new List<float>();
        readonly System.Random _rng = new System.Random();
        float _acc;
        float _height;

        public static MatrixBackground New(RectTransform parent)
        {
            MatrixBackground m = new MatrixBackground();
            m.Rt = Ui.Node("MatrixBg", parent);
            Ui.Full(m.Rt);

            for (int i = 0; i < Columns; i++)
            {
                Text t = Ui.Label(m.Rt, "", FontSize, Theme.Green, TextAnchor.UpperLeft, false, false);
                RectTransform rt = t.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(14f, RowsPerColumn * Theme.LineHeight(FontSize));
                m._texts.Add(t);
                m._y.Add(-m.Rand() * 400f);
                m._speed.Add(28f + m.Rand() * 70f);
            }

            Ui.RegisterTick(m);
            m.Refresh();
            return m;
        }

        float Rand()
        {
            return (float)_rng.NextDouble();
        }

        public void Refresh()
        {
            _height = Mathf.Max(1f, Rt.rect.height);
            float width = Rt.rect.width;
            float colWidth = width / (float)Columns;
            for (int i = 0; i < _texts.Count; i++)
            {
                RectTransform rt = _texts[i].rectTransform;
                rt.anchoredPosition = new Vector2(colWidth * i, _y[i]);
            }
        }

        public void Tick()
        {
            if (Rt == null) return;
            float dt = Time.unscaledDeltaTime;

            for (int i = 0; i < _texts.Count; i++)
            {
                _y[i] -= _speed[i] * dt;
                if (_y[i] < -_height - 120f)
                {
                    _y[i] = 40f;
                    _speed[i] = 28f + Rand() * 70f;
                }
                RectTransform rt = _texts[i].rectTransform;
                Vector2 pos = rt.anchoredPosition;
                pos.y = _y[i];
                rt.anchoredPosition = pos;
            }

            _acc += dt;
            if (_acc >= StepSeconds)
            {
                _acc = 0f;
                Reroll();
            }
        }

        void Reroll()
        {
            for (int i = 0; i < _texts.Count; i++)
            {
                _texts[i].text = BuildColumn();
            }
        }

        string BuildColumn()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int r = 0; r < RowsPerColumn; r++)
            {
                char c = Charset[_rng.Next(Charset.Length)];
                float alpha = Mathf.Clamp01(1f - (float)r / (float)RowsPerColumn) * 0.55f;
                if (alpha < 0.03f)
                {
                    sb.Append("\n");
                    continue;
                }
                string hex = ColorUtility.ToHtmlStringRGBA(Theme.WithAlpha(Theme.Green, alpha));
                sb.Append("<color=#").Append(hex).Append(">").Append(c).Append("</color>");
                if (r < RowsPerColumn - 1) sb.Append("\n");
            }
            return sb.ToString();
        }
    }

    /// <summary>Сканирующая полоса (аналог CSS-анимации scan-beam из index.css).</summary>
    public class ScanBeam : IUiTick
    {
        public RectTransform Rt;
        public Image Beam;
        float _t;

        public static ScanBeam New(RectTransform parent, float height)
        {
            ScanBeam s = new ScanBeam();
            s.Rt = Ui.Node("ScanBeam", parent);
            Ui.Full(s.Rt);

            RectTransform rt = Ui.Node("Beam", s.Rt);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height);
            s.Beam = rt.gameObject.AddComponent<Image>();
            s.Beam.sprite = Theme.Solid;
            s.Beam.color = Theme.WithAlpha(Theme.Cyan, 0.05f);
            s.Beam.raycastTarget = false;
            Ui.RegisterTick(s);
            return s;
        }

        public void Tick()
        {
            if (Rt == null || Beam == null) return;
            _t += Time.unscaledDeltaTime * 0.08f;
            if (_t > 1f) _t -= 1f;
            RectTransform rt = Beam.rectTransform;
            Vector2 pos = rt.anchoredPosition;
            pos.y = -_t * (Rt.rect.height + 40f) + 20f;
            rt.anchoredPosition = pos;
        }
    }
}
