using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>Содержимое окна: строится один раз, дальше обновляется по событиям Game.</summary>
    public interface IWindowView
    {
        void Build(UiWindow win, RectTransform body);
        void Tick();
        void Dispose();
    }

    /// <summary>
    /// Рабочий стол NeonOS — главный игровой экран, перенос scripts/ui/desktop.gd:
    /// топбар с курсами, колонка иконок, менеджер окон, панель задач, стартовое меню,
    /// обучение и модалка повышения уровня.
    /// </summary>
    public class DesktopScreen : IGameScreen
    {
        const float TopbarH = 34f;
        const float TaskbarH = 40f;
        const float IconColumnW = 96f;

        class AppDef
        {
            public string Id;
            public string Title;
            public string Label;
            public string Icon;
            public string ColorKey;
            public bool Wide;
            public Func<IWindowView> Make;
        }

        readonly GameBoot _boot;
        readonly Game _game;
        readonly List<AppDef> _apps = new List<AppDef>();
        readonly Dictionary<string, UiWindow> _windows = new Dictionary<string, UiWindow>();
        readonly Dictionary<string, IWindowView> _views = new Dictionary<string, IWindowView>();
        readonly Dictionary<string, Text> _badges = new Dictionary<string, Text>();
        readonly Dictionary<string, Text> _tickers = new Dictionary<string, Text>();
        readonly Dictionary<string, float> _prevPrices = new Dictionary<string, float>();
        readonly Dictionary<string, UiButton> _taskButtons = new Dictionary<string, UiButton>();

        RectTransform _root;
        RectTransform _windowLayer;
        RectTransform _taskbarButtons;
        GridBackground _grid;
        MatrixBackground _matrix;

        Text _money;
        Text _crypto;
        Text _xp;
        Text _level;
        Text _clock;
        Text _income;
        Text _taskBtc;
        Text _pyMode;
        RectTransform _xpFill;
        float _xpBarW = 90f;
        float _clockTimer;
        int _cascade;

        UiButton _startButton;
        UiWindow _startMenu;
        UiModal _modal;

        public DesktopScreen(GameBoot boot)
        {
            _boot = boot;
            _game = boot.Game;
            BuildAppList();
        }

        void BuildAppList()
        {
            _apps.Add(App("hack", "NEON_HACK // ТЕРМИНАЛ ВЗЛОМА", "Хак-терминал", "⌁", "green", true,
                delegate { return new HackWindowView(_game); }));
            _apps.Add(App("miner", "МАЙНИНГ-ФЕРМА", "Майнеры", "⚙", "cyan", false,
                delegate { return new MinerWindowView(_game); }));
            _apps.Add(App("trade", "БИРЖА DARKEX", "Биржа", "▲", "yellow", false,
                delegate { return new TradeWindowView(_game); }));
            _apps.Add(App("upgrade", "ЧЁРНЫЙ РЫНОК // АПГРЕЙДЫ", "Апгрейды", "✚", "orange", false,
                delegate { return new UpgradeWindowView(_game); }));
            _apps.Add(App("learn", "ШКОЛА PYTHON", "Школа Python", "✎", "pink", true,
                delegate { return new LearnWindowView(_game); }));
            _apps.Add(App("files", "ФАЙЛЫ // /home/ghost", "Файлы", "▶", "cyan", false,
                delegate { return new FilesWindowView(_game); }));
            _apps.Add(App("profile", "ПРОФИЛЬ ХАКЕРА", "Профиль", "★", "yellow", false,
                delegate { return new ProfileWindowView(_game); }));
        }

        static AppDef App(string id, string title, string label, string icon, string colorKey, bool wide,
            Func<IWindowView> make)
        {
            AppDef a = new AppDef();
            a.Id = id;
            a.Title = title;
            a.Label = label;
            a.Icon = icon;
            a.ColorKey = colorKey;
            a.Wide = wide;
            a.Make = make;
            return a;
        }

        Color ColorFor(string key)
        {
            if (key == "green") return Theme.Green;
            if (key == "cyan") return Theme.Cyan;
            if (key == "yellow") return Theme.Yellow;
            if (key == "orange") return Theme.Orange;
            if (key == "pink") return Theme.Pink;
            return Theme.Text;
        }

        AppDef AppById(string id)
        {
            for (int i = 0; i < _apps.Count; i++)
            {
                if (_apps[i].Id == id) return _apps[i];
            }
            return null;
        }

        // ==================== ПОСТРОЕНИЕ ====================
        public void Build(RectTransform parent)
        {
            _root = parent;

            Image bg = Ui.Img(parent, Theme.Bg, "Bg");
            Ui.Full(bg.rectTransform);
            _grid = GridBackground.New(parent);
            _matrix = MatrixBackground.New(parent);
            ScanBeam.New(parent, 140f);

            _windowLayer = Ui.Node("Windows", parent);
            Ui.Full(_windowLayer);

            BuildTopbar(parent);
            BuildTaskbar(parent);
            BuildIconColumn(parent);

            _game.StateChanged += RefreshHud;
            _game.PricesChanged += RefreshHud;
            _game.LevelUp += OnLevelUp;
            _game.MinersChanged += RefreshHud;

            RefreshHud();
            OpenWindow("hack");
            if (!_game.HasSave) ShowOnboarding();
        }

        void BuildTopbar(RectTransform parent)
        {
            RectTransform bar = Ui.Node("Topbar", parent);
            Ui.TopLeft(bar, 0f, 0f, 1280f, TopbarH);
            Image bg = Ui.Img(bar, Theme.WithAlpha(Theme.PanelBar, 0.95f), "Bg");
            Ui.Full(bg.rectTransform);
            Ui.Border(bar, Theme.White10);

            float x = 12f;
            foreach (CryptoInfo c in _game.Data.Cryptos)
            {
                // ₿ нет в основном моношрифте — рисуем иконку отдельной меткой
                Text icon = Ui.Label(bar, c.Icon, 13, c.Color, TextAnchor.MiddleLeft, false, false);
                icon.font = Theme.IconFontFor(c.Icon);
                Ui.TopLeft(icon.rectTransform, x, 0f, 18f, TopbarH);

                Text t = Ui.Label(bar, c.Id + " —", 11, Theme.Text, TextAnchor.MiddleLeft, false, false);
                Ui.TopLeft(t.rectTransform, x + 16f, 0f, 112f, TopbarH);
                _tickers[c.Id] = t;
                x += 132f;
            }

            _pyMode = Ui.Label(bar, _game.PythonModeText(), 10, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(_pyMode.rectTransform, x + 4f, 0f, 210f, TopbarH);

            RectTransform right = Ui.Node("Right", bar);
            Ui.TopRight(right, 12f, 0f, 560f, TopbarH);

            _level = Ui.Label(right, "LV 1", 11, Theme.Yellow, TextAnchor.MiddleLeft, true, false);
            Ui.TopLeft(_level.rectTransform, 0f, 0f, 48f, TopbarH);

            RectTransform xpBar = Ui.Node("XpBar", right);
            Ui.TopLeft(xpBar, 50f, 13f, _xpBarW, 8f);
            Image xpBg = Ui.Img(xpBar, Theme.White10, "Bg");
            Ui.Full(xpBg.rectTransform);
            RectTransform xpFill = Ui.Node("Fill", xpBar);
            xpFill.anchorMin = new Vector2(0f, 0f);
            xpFill.anchorMax = new Vector2(0f, 1f);
            xpFill.pivot = new Vector2(0f, 0.5f);
            xpFill.sizeDelta = new Vector2(0f, 0f);
            xpFill.anchoredPosition = Vector2.zero;
            Ui.Img(xpFill, Theme.Yellow, "FillImg");
            _xpFill = xpFill;

            _xp = Ui.Label(right, "", 10, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(_xp.rectTransform, 148f, 0f, 74f, TopbarH);

            _money = Ui.Label(right, "", 12, Theme.Green, TextAnchor.MiddleLeft, true, false);
            Ui.TopLeft(_money.rectTransform, 224f, 0f, 130f, TopbarH);

            _crypto = Ui.Label(right, "", 10, Theme.Cyan, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(_crypto.rectTransform, 356f, 0f, 120f, TopbarH);

            _clock = Ui.Label(right, "", 11, Theme.TextMuted, TextAnchor.MiddleRight, false, false);
            Ui.TopLeft(_clock.rectTransform, 470f, 0f, 90f, TopbarH);
            UpdateClock();
        }

        void BuildIconColumn(RectTransform parent)
        {
            RectTransform col = Ui.Node("Icons", parent);
            Ui.TopLeft(col, 8f, TopbarH + 12f, IconColumnW, 7f * 62f + 10f);

            for (int i = 0; i < _apps.Count; i++)
            {
                AppDef a = _apps[i];
                Color c = ColorFor(a.ColorKey);
                string id = a.Id;

                UiButton b = UiButton.New(col, a.Icon + "\n" + a.Label, c, 11, UiButton.Ghost, 58f);
                Ui.TopLeft(b.Rt, 0f, i * 62f, IconColumnW, 58f);
                b.Rt.anchorMin = new Vector2(0f, 1f);
                b.Rt.anchorMax = new Vector2(0f, 1f);
                b.Rt.pivot = new Vector2(0f, 1f);
                b.Rt.anchoredPosition = new Vector2(0f, -i * 62f);
                // иконки всегда доступны: слой выше любого окна
                b.CustomLayer = Ui.LayerWindowBase + 290;
                b.OnClick = delegate { OpenWindow(id); };

                Text badge = Ui.Label(b.Rt, "", 9, Theme.Yellow, TextAnchor.UpperRight, true, false);
                Ui.TopRight(badge.rectTransform, -5f, 3f, 60f, 14f);
                _badges[id] = badge;
            }
        }

        void BuildTaskbar(RectTransform parent)
        {
            RectTransform bar = Ui.Node("Taskbar", parent);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = new Vector2(0f, 0f);
            bar.offsetMax = new Vector2(0f, TaskbarH);
            Image bg = Ui.Img(bar, Theme.WithAlpha(Theme.PanelBar, 0.95f), "Bg");
            Ui.Full(bg.rectTransform);
            Ui.Border(bar, Theme.WithAlpha(Theme.Green, 0.25f));

            _startButton = UiButton.New(bar, "⌁ GHOST", Theme.Green, 11, UiButton.Outline, 32f);
            Ui.TopLeft(_startButton.Rt, 10f, 4f, 96f, 32f);
            _startButton.CustomLayer = Ui.LayerWindowBase + 290;
            _startButton.OnClick = ToggleStartMenu;

            _taskbarButtons = Ui.Node("TaskButtons", bar);
            Ui.TopLeft(_taskbarButtons, 114f, 6f, 700f, 28f);

            RectTransform right = Ui.Node("Right", bar);
            Ui.TopRight(right, 12f, 0f, 320f, TaskbarH);
            _income = Ui.Label(right, "", 10, Theme.Green, TextAnchor.MiddleRight, false, false);
            Ui.TopLeft(_income.rectTransform, 0f, 0f, 190f, TaskbarH);
            _taskBtc = Ui.Label(right, "", 10, Theme.TextMuted, TextAnchor.MiddleRight, false, false);
            Ui.TopLeft(_taskBtc.rectTransform, 190f, 0f, 130f, TaskbarH);
        }

        // ==================== ОКНА ====================
        void OpenWindow(string id)
        {
            AppDef a = AppById(id);
            if (a == null) return;

            UiWindow existing;
            if (_windows.TryGetValue(id, out existing) && existing != null)
            {
                FocusWindow(existing);
                return;
            }

            Color accent = ColorFor(a.ColorKey);
            float height = Mathf.Clamp(720f - TopbarH - TaskbarH - 60f, 320f, 520f);
            UiWindow win = UiWindow.New(_windowLayer, id, a.Title, accent, a.Wide, _cascade, height);
            _cascade = (_cascade + 1) % 7;
            _windows[id] = win;
            win.LayerValue = Ui.LayerWindowBase + _windows.Count * 3;
            win.OnCloseRequested = delegate(UiWindow w) { CloseWindow(id); };
            win.OnFocusRequested = FocusWindow;

            if (id != "hack")
            {
                // оставляем терминал на экране сбоку, остальные — каскадом
                float shift = (_windows.Count % 3) * 30f;
                win.Rt.anchoredPosition = new Vector2(win.Rt.anchoredPosition.x + shift, win.Rt.anchoredPosition.y - shift);
            }

            IWindowView view = a.Make();
            _views[id] = view;
            view.Build(win, win.Scroll.Content);
            Sfx.UiOpen();
            RefreshTaskButtons();
        }

        void FocusWindow(UiWindow win)
        {
            if (win == null) return;
            int top = win.LayerValue;
            foreach (KeyValuePair<string, UiWindow> kv in _windows)
            {
                if (kv.Value != null && kv.Value.LayerValue > top) top = kv.Value.LayerValue;
            }
            if (top == win.LayerValue) return;
            win.LayerValue = top + 3;
        }

        public void CloseWindow(string id)
        {
            UiWindow win;
            if (_windows.TryGetValue(id, out win) && win != null)
            {
                Sfx.UiClick();
                IWindowView view;
                if (_views.TryGetValue(id, out view) && view != null) view.Dispose();
                _views.Remove(id);
                _windows.Remove(id);
                win.Destroy();
            }
            RefreshTaskButtons();
        }

        void CloseTopWindow()
        {
            UiWindow top = null;
            foreach (KeyValuePair<string, UiWindow> kv in _windows)
            {
                if (kv.Value == null) continue;
                if (top == null || kv.Value.LayerValue > top.LayerValue) top = kv.Value;
            }
            if (top != null) CloseWindow(top.Id);
        }

        void RefreshTaskButtons()
        {
            Ui.DestroyChildren(_taskbarButtons);
            _taskButtons.Clear();
            int i = 0;
            foreach (KeyValuePair<string, UiWindow> kv in _windows)
            {
                if (kv.Value == null) continue;
                AppDef a = AppById(kv.Key);
                if (a == null) continue;
                string id = kv.Key;
                UiButton b = UiButton.New(_taskbarButtons, a.Title.Split(new string[] { "//" }, StringSplitOptions.None)[0].Trim(),
                    ColorFor(a.ColorKey), 10, UiButton.Outline, 26f);
                b.Rt.anchorMin = new Vector2(0f, 0f);
                b.Rt.anchorMax = new Vector2(0f, 1f);
                b.Rt.pivot = new Vector2(0f, 0.5f);
                b.Rt.sizeDelta = new Vector2(112f, 0f);
                b.Rt.anchoredPosition = new Vector2(i * 118f, 0f);
                b.CustomLayer = Ui.LayerWindowBase + 290;
                b.OnClick = delegate { CloseWindow(id); };
                _taskButtons[id] = b;
                i++;
            }
        }

        // ==================== HUD ====================
        void RefreshHud()
        {
            RefreshTickers();

            if (_money != null) _money.text = Fmt.Dollars((long)_game.Dollars);
            if (_crypto != null) _crypto.text = Fmt.DollarsFull((long)_game.PortfolioValue()) + " в крипте";
            if (_level != null) _level.text = "LV " + _game.Level;
            if (_xp != null) _xp.text = _game.Xp + "/" + _game.XpForLevel(_game.Level) + " xp";
            if (_xpFill != null) _xpFill.sizeDelta = new Vector2(_xpBarW * Mathf.Clamp01(_game.XpProgress()), 0f);
            if (_income != null)
            {
                _income.text = _game.Miners.Count == 0
                    ? ""
                    : "⚙ +" + Fmt.Dollars((long)_game.MinerIncomePerMin()) + "/мин";
            }
            if (_taskBtc != null) _taskBtc.text = Fmt.Crypto(_game.GetCrypto("BTC")) + " BTC";
            if (_pyMode != null) _pyMode.text = _game.PythonModeText();

            for (int i = 0; i < _apps.Count; i++)
            {
                AppDef a = _apps[i];
                Text badge;
                if (!_badges.TryGetValue(a.Id, out badge)) continue;
                if (a.Id == "hack") badge.text = _game.CompletedMissions.Count + "/" + _game.Data.Missions.Count;
                else if (a.Id == "learn") badge.text = _game.CompletedLessons.Count + "/" + _game.Data.Lessons.Count;
                else if (a.Id == "miner") badge.text = _game.Miners.Count == 0 ? "" : _game.Miners.Count.ToString();
                else badge.text = "";
            }
        }

        void RefreshTickers()
        {
            foreach (CryptoInfo c in _game.Data.Cryptos)
            {
                Text t;
                if (!_tickers.TryGetValue(c.Id, out t) || t == null) continue;

                float price = _game.GetPrice(c.Id);
                float prev;
                string arrow = "";
                if (_prevPrices.TryGetValue(c.Id, out prev) && prev > 0f)
                {
                    float delta = (price - prev) / prev;
                    if (Mathf.Abs(delta) > 0.0005f)
                    {
                        arrow = delta > 0f ? " ▲" : " ▼";
                        t.color = delta > 0f ? Theme.Green : Theme.Pink;
                    }
                }
                _prevPrices[c.Id] = price;

                t.text = c.Icon + " " + c.Id + " " + Fmt.Price(price) + arrow;
            }
        }

        void UpdateClock()
        {
            if (_clock != null) _clock.text = System.DateTime.Now.ToString("HH:mm");
        }

        // ==================== МЕНЮ И МОДАЛКИ ====================
        void ToggleStartMenu()
        {
            if (_startMenu != null)
            {
                _startMenu.Destroy();
                _startMenu = null;
                return;
            }
            Sfx.UiClick();

            UiWindow menu = UiWindow.New(_windowLayer, "startmenu", "NEON_OS // МЕНЮ", Theme.Green, false, 3, 168f);
            menu.Rt.anchorMin = new Vector2(0f, 0f);
            menu.Rt.anchorMax = new Vector2(0f, 0f);
            menu.Rt.pivot = new Vector2(0f, 0f);
            menu.Rt.anchoredPosition = new Vector2(10f, TaskbarH + 8f);
            menu.LayerValue = Ui.LayerWindowBase + 60;
            menu.CloseButton.Rt.gameObject.SetActive(false);
            menu.DragEnabled = false;
            menu.OnCloseRequested = delegate(UiWindow w)
            {
                w.Destroy();
                _startMenu = null;
            };
            _startMenu = menu;

            RectTransform box = Ui.VBox(menu.Scroll.Content, 4f, 8);
            Ui.Height(box.gameObject, 150f);

            MenuItem(box, "Звук: " + (_game.SoundOn ? "ВКЛ" : "ВЫКЛ"), delegate
            {
                _game.SoundOn = !_game.SoundOn;
                _game.Save();
                CloseStartMenuThen(null);
                ToggleStartMenu();
            });
            MenuItem(box, "Показать обучение", delegate
            {
                CloseStartMenuThen(null);
                ShowOnboarding();
            });
            MenuItem(box, "Сбросить прогресс", delegate
            {
                CloseStartMenuThen(null);
                _game.ResetProgress();
                RefreshHud();
                _game.Notify("Прогресс сброшен", "Начинаем с $150 и чистой репутации", "warn");
            });
            MenuItem(box, "В главное меню", delegate
            {
                _game.Save();
                CloseStartMenuThen(null);
                _boot.ShowMainMenu();
            });
        }

        void CloseStartMenuThen(Action after)
        {
            if (_startMenu != null)
            {
                _startMenu.Destroy();
                _startMenu = null;
            }
            if (after != null) after();
        }

        void MenuItem(RectTransform box, string text, Action action)
        {
            UiButton b = UiButton.New(box, text, Theme.Text, 11, UiButton.Ghost, 30f);
            LayoutElement le = b.Rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = b.Rt.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            b.LayerProvider = delegate { return Ui.LayerWindowBase + 61; };
            b.OnClick = action;
        }

        void OnLevelUp(int level)
        {
            string info = "";
            foreach (CryptoInfo c in _game.Data.Cryptos)
            {
                if (c.UnlockLevel == level)
                {
                    info = "Разблокирована " + c.Name + "! Новые миссии и майнеры ждут.";
                    break;
                }
            }
            if (info.Length == 0) info = "Ты становишься сильнее. Продолжай взламывать!";

            UiModal m = OpenModal("LEVEL " + level, info, "ЗАБРАТЬ НАГРАДУ", Theme.Yellow);
            Sfx.LevelUp();
        }

        UiModal OpenModal(string title, string text, string ok, Color accent)
        {
            UiModal m = UiModal.New(Ui.ModalLayer, 520f, 240f, accent, false);
            m.CustomLayer = Ui.LayerModal;

            RectTransform box = Ui.VBox(m.Card, 8f, 16);
            Ui.Full(box);

            Text t = Ui.Line(box, title, 24, accent, TextAnchor.MiddleCenter, true);
            Ui.Paragraph(box, text, 12, Theme.TextSoft, 460f);

            UiButton b = UiButton.New(box, ok, accent, 12, UiButton.Solid, 36f);
            LayoutElement le = b.Rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = b.Rt.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            b.LayerProvider = delegate { return Ui.LayerModal; };
            b.OnClick = delegate { m.Close(); };
            _modal = m;
            return m;
        }

        void ShowOnboarding()
        {
            UiModal m = UiModal.New(Ui.ModalLayer, 560f, 300f, Theme.Cyan, false);
            m.CustomLayer = Ui.LayerModal;

            RectTransform box = Ui.VBox(m.Card, 8f, 16);
            Ui.Full(box);

            Text title = Ui.Line(box, "Добро пожаловать, ghost!", 20, Theme.Cyan, TextAnchor.MiddleLeft, true);
            Ui.Paragraph(box, "Это NeonOS — твоя хакерская ОС. Слева — программы, сверху — деньги и курсы, снизу — панель задач.\n" +
                "1. Открой «Хак-терминал» и взломай первую цель.\n" +
                "2. Поставь майнер на взломанный комп.\n" +
                "3. Продавай крипту на бирже и покупай апгрейды.", 12, Theme.TextSoft, 500f);

            UiButton b = UiButton.New(box, "ПОГНАЛИ", Theme.Cyan, 13, UiButton.Solid, 36f);
            LayoutElement le = b.Rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = b.Rt.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            b.LayerProvider = delegate { return Ui.LayerModal; };
            b.OnClick = delegate
            {
                m.Close();
                _game.Save();
            };
        }

        // ==================== ЦИКЛ ====================
        public void Tick()
        {
            if (_grid != null) _grid.Refresh();
            if (_matrix != null) _matrix.Refresh();

            _clockTimer += Time.unscaledDeltaTime;
            if (_clockTimer >= 10f)
            {
                _clockTimer = 0f;
                UpdateClock();
            }

            foreach (KeyValuePair<string, IWindowView> kv in _views)
            {
                if (kv.Value != null) kv.Value.Tick();
            }

            // горячие клавиши 1..7 — окна, ESC — закрыть верхнее
            int digit = UiInput.DigitDown;
            if (digit > 0 && digit <= _apps.Count) OpenWindow(_apps[digit - 1].Id);

            if (UiInput.KeyDown(UiKey.Escape))
            {
                if (_startMenu != null) CloseStartMenuThen(null);
                else if (ModalOpen()) _modal.Close();
                else CloseTopWindow();
            }
        }

        bool ModalOpen()
        {
            return _modal != null && _modal.Rt != null && _modal.Rt.gameObject.activeInHierarchy;
        }

        public void Dispose()
        {
            _game.StateChanged -= RefreshHud;
            _game.PricesChanged -= RefreshHud;
            _game.LevelUp -= OnLevelUp;
            _game.MinersChanged -= RefreshHud;

            foreach (KeyValuePair<string, IWindowView> kv in _views)
            {
                if (kv.Value != null) kv.Value.Dispose();
            }
            _views.Clear();
            _windows.Clear();
            _taskButtons.Clear();
        }
    }
}
