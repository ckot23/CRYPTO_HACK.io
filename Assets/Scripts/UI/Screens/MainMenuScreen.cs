using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>Стартовый экран — перенос scripts/ui/main_menu.gd.</summary>
    public class MainMenuScreen : IGameScreen
    {
        readonly GameBoot _boot;
        RectTransform _root;
        GridBackground _grid;
        MatrixBackground _matrix;
        ScanBeam _beam;
        Text _titleMain;
        Text _titleCyan;
        Text _titlePink;
        Text _quote;
        float _quoteTimer;
        int _quoteIndex;
        float _glitchTimer;
        List<string> _quotes = new List<string>();

        public MainMenuScreen(GameBoot boot)
        {
            _boot = boot;
        }

        public void Build(RectTransform parent)
        {
            _root = parent;
            Game game = _boot.Game;

            Image bg = Ui.Img(parent, Theme.Bg, "Bg");
            Ui.Full(bg.rectTransform);

            _grid = GridBackground.New(parent);
            _matrix = MatrixBackground.New(parent);
            _beam = ScanBeam.New(parent, 120f);

            _quotes = game.Data.Quotes;
            _quoteIndex = 0;

            // ---------------- шапка ----------------
            RectTransform top = Ui.Node("Top", parent);
            Ui.TopLeft(top, 0f, 0f, 1280f, 60f);
            Image topBg = Ui.Img(top, Theme.WithAlpha(Theme.PanelBar, 0.5f), "TopBg");
            Ui.Full(topBg.rectTransform);

            Text status = Ui.Label(top, "● NEON NET ONLINE", 11, Theme.Green, TextAnchor.MiddleLeft, true, false);
            Ui.TopLeft(status.rectTransform, 18f, 0f, 300f, 60f);

            Text hint = Ui.Label(top, "v1.0.4 · UNITY 6.6 · UGUI C#", 11, Theme.TextMuted, TextAnchor.MiddleRight, false, false);
            Ui.TopRight(hint.rectTransform, -18f, 0f, 320f, 60f);

            // ---------------- центр ----------------
            RectTransform center = Ui.Node("Center", parent);
            Ui.TopLeft(center, 0f, 96f, 1280f, 420f);

            // глитч-заголовок: две цветные подложки + основной текст
            _titleCyan = Ui.Label(center, "CRYPTO_HACK", 58, Theme.Cyan, TextAnchor.MiddleCenter, true, false);
            Ui.TopLeft(_titleCyan.rectTransform, -3f, 0f, 1280f, 80f);

            _titlePink = Ui.Label(center, "CRYPTO_HACK", 58, Theme.Pink, TextAnchor.MiddleCenter, true, false);
            Ui.TopLeft(_titlePink.rectTransform, 3f, 0f, 1280f, 80f);

            _titleMain = Ui.Label(center, "CRYPTO_HACK", 58, Theme.Text, TextAnchor.MiddleCenter, true, false);
            Ui.TopLeft(_titleMain.rectTransform, 0f, 0f, 1280f, 80f);
            _titleMain.color = new Color(Theme.Text.r, Theme.Text.g, Theme.Text.b, 0.92f);

            Text subtitle = Ui.Label(center,
                "симулятор хакера · учи Python · взламывай цели · майни крипту",
                14, Theme.Cyan, TextAnchor.MiddleCenter, false, false);
            Ui.TopLeft(subtitle.rectTransform, 0f, 76f, 1280f, 26f);

            Text sub2 = Ui.Label(center, "8 миссий · 5 уроков · 4 апгрейда · одна легенда",
                12, Theme.TextMuted, TextAnchor.MiddleCenter, false, false);
            Ui.TopLeft(sub2.rectTransform, 0f, 102f, 1280f, 22f);

            // ---------------- кнопки ----------------
            RectTransform buttons = Ui.Node("Buttons", center);
            Ui.TopLeft(buttons, 490f, 160f, 300f, 60f);

            bool hasSave = game.HasSave;
            UiButton start = UiButton.New(buttons, hasSave ? "ПРОДОЛЖИТЬ ВЗЛОМ" : "НАЧАТЬ ИГРУ",
                Theme.Green, 16, UiButton.Solid, 54f);
            Ui.TopLeft(start.Rt, 0f, 0f, 300f, 54f);
            start.CustomLayer = Ui.LayerDesktop;
            start.OnClick = delegate
            {
                if (!game.HasSave) game.Save();
                _boot.ShowBoot();
            };

            UiButton help = UiButton.New(buttons, "КАК ИГРАТЬ", Theme.Cyan, 13, UiButton.Outline, 40f);
            Ui.TopLeft(help.Rt, 40f, 66f, 220f, 40f);
            help.OnClick = ShowHelp;

            if (hasSave)
            {
                UiButton reset = UiButton.New(buttons, "СБРОСИТЬ ПРОГРЕСС", Theme.Pink, 11, UiButton.Ghost, 28f);
                Ui.TopLeft(reset.Rt, 40f, 114f, 220f, 28f);
                reset.OnClick = delegate
                {
                    game.ResetProgress();
                    _boot.ShowMainMenu();
                    game.Notify("Прогресс сброшен", "Новая жизнь начинается с $150", "warn");
                };
            }

            // ---------------- фичи ----------------
            string[] features =
            {
                "⌁  ВЗЛОМ — 8 миссий на Python",
                "⚙  МАЙНИНГ — пассивный доход с чужих ПК",
                "▲  БИРЖА — живые курсы и комиссия",
                "✎  ШКОЛА — 5 уроков с мини-тестами",
            };
            for (int i = 0; i < features.Length; i++)
            {
                float y = 320f + (i / 2) * 34f;
                float x = 300f + (i % 2) * 360f;
                RectTransform card = Ui.Card(parent, Theme.WithAlpha(Theme.Green, 0.25f),
                    Theme.WithAlpha(Theme.PanelDark, 0.75f), "Feature");
                Ui.TopLeft(card, x, y, 340f, 28f);
                Text t = Ui.Label(card, features[i], 12, Theme.TextSoft, TextAnchor.MiddleLeft, false, false);
                Ui.Stretch(t.rectTransform, 12f, 0f, 8f, 0f);
            }

            // ---------------- низ ----------------
            _quote = Ui.Label(parent, "", 13, Theme.Cyan, TextAnchor.MiddleCenter, false, true);
            Ui.TopLeft(_quote.rectTransform, 240f, 560f, 800f, 24f);
            RefreshQuote();

            Text footer = Ui.Label(parent, "Сделано на Unity · весь код на C# · F1 — подсказка по управлению",
                10, Theme.TextFaint, TextAnchor.MiddleCenter, false, false);
            Ui.TopLeft(footer.rectTransform, 0f, 660f, 1280f, 20f);
        }

        void RefreshQuote()
        {
            if (_quote == null) return;
            if (_quotes == null || _quotes.Count == 0)
            {
                _quote.text = "\"Тишина — лучший шифр\"";
                return;
            }
            _quoteIndex = _quoteIndex % _quotes.Count;
            _quote.text = "«" + _quotes[_quoteIndex] + "»";
        }

        void ShowHelp()
        {
            if (Ui.ModalLayer == null) return;
            UiModal m = UiModal.New(Ui.ModalLayer, 640f, 400f, Theme.Cyan, true);
            m.CustomLayer = Ui.LayerModal;

            RectTransform box = Ui.VBox(m.Card, 8f, 18);
            Ui.Full(box);

            Text title = Ui.Line(box, "КАК ИГРАТЬ", 20, Theme.Cyan, TextAnchor.MiddleLeft, true);
            Text hint = Ui.Line(box, "5 шагов до первого взлома", 11, Theme.TextMuted);

            string[] steps =
            {
                "1. Иконка ⌁ ХАК‐ТЕРМИНАЛ — выбери миссию слева.",
                "2. Пиши код на Python в редакторе и жми ▶ ЗАПУСТИТЬ.",
                "3. Соблюдай требования миссии — они во вкладке ПОДСКАЗКИ.",
                "4. Награда: крипта, доллары, XP. XP повышает уровень.",
                "5. Ставь майнеры (⚙) и торгуй на бирже (▲), чтобы расти.",
            };
            for (int i = 0; i < steps.Length; i++)
            {
                Ui.Paragraph(box, steps[i], 13, Theme.TextSoft, 590f);
            }

            UiButton close = UiButton.New(box, "ПОНЯТНО", Theme.Green, 13, UiButton.Solid, 38f);
            close.OnClick = delegate { m.Close(); };
            close.LayerProvider = delegate { return Ui.LayerModal; };
        }

        public void Tick()
        {
            float dt = Time.unscaledDeltaTime;

            _glitchTimer -= dt;
            if (_glitchTimer <= 0f)
            {
                _glitchTimer = 0.08f;
                float dx = Random.Range(-3f, 3f);
                float dy = Random.Range(-1.5f, 1.5f);
                if (_titleCyan != null) _titleCyan.rectTransform.anchoredPosition = new Vector2(dx - 1f, dy);
                if (_titlePink != null) _titlePink.rectTransform.anchoredPosition = new Vector2(-dx + 1f, -dy);
            }

            _quoteTimer += dt;
            if (_quoteTimer >= 8f)
            {
                _quoteTimer = 0f;
                _quoteIndex++;
                RefreshQuote();
            }

            if (_grid != null) _grid.Refresh();
            if (_matrix != null) _matrix.Refresh();

            if (UiInput.KeyDown(UiKey.Escape)) ShowHelp();
        }

        public void Dispose() { }
    }
}
