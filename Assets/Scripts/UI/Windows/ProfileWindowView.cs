using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>
    /// «ПРОФИЛЬ ХАКЕРА» — перенос scripts/ui/profile_window.gd:
    /// уровень, капитал, статистика и достижения.
    /// </summary>
    public class ProfileWindowView : IWindowView
    {
        struct Achievement
        {
            public string Name;
            public string Desc;
            public Achievement(string n, string d) { Name = n; Desc = d; }
        }

        static readonly Achievement[] Achievements =
        {
            new Achievement("Первый взлом", "Взломай первую цель"),
            new Achievement("Серийный хакер", "Взломай 4 цели"),
            new Achievement("Легенда даркнета", "Взломай все 8 целей"),
            new Achievement("Фермер", "Установи первый майнер"),
            new Achievement("Магнат", "Держи 4+ майнера"),
            new Achievement("Трейдер", "Соверши 5 сделок"),
            new Achievement("Студент", "Пройди 3 урока Python"),
            new Achievement("Кит", "Капитал $10,000+"),
        };

        readonly Game _game;
        UiWindow _win;
        RectTransform _body;
        UiScroll _scroll;
        string _signature = "";

        public ProfileWindowView(Game game)
        {
            _game = game;
        }

        public void Build(UiWindow win, RectTransform body)
        {
            _win = win;
            _scroll = Ui.Scroll(win.BodyHolder, "ProfileScroll");
            Ui.Full(_scroll.View);
            _body = Ui.VBox(_scroll.Content, 10f, 14);
            LayoutElement le = _body.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            _game.StateChanged += Refresh;
            _game.PricesChanged += Refresh;
            Refresh();
        }

        string StatusText()
        {
            if (_game.Level >= 5) return "ЛЕГЕНДА ДАРКНЕТА";
            if (_game.Level >= 3) return "ОПЫТНЫЙ ХАКЕР";
            return "СКРИПТ-КИДДИ";
        }

        void Refresh()
        {
            string sig = _game.Level + "|" + _game.Xp + "|" + (int)_game.Dollars + "|" + _game.TotalHacked +
                "|" + _game.Miners.Count + "|" + _game.TotalTrades + "|" + _game.CompletedLessons.Count +
                "|" + (int)_game.PortfolioValue();
            if (sig == _signature) return;
            _signature = sig;

            Ui.DestroyChildren(_body);
            ProfileCard();
            StatsRow();
            AchievementsCard();
        }

        void ProfileCard()
        {
            RectTransform card = Ui.Card(_body, Theme.WithAlpha(Theme.Green, 0.4f), Theme.PanelBar, "ProfileCard");
            LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 84f;
            le.minHeight = 84f;

            Text avatar = Ui.Label(card, "⌁_", 26, Theme.Green, TextAnchor.MiddleCenter, false, false);
            Ui.TopLeft(avatar.rectTransform, 14f, 10f, 60f, 60f);

            Text name = Ui.Label(card, "ghost", 14, Theme.Text, TextAnchor.UpperLeft, true, false);
            Ui.TopLeft(name.rectTransform, 84f, 12f, 240f, 20f);
            Text status = Ui.Label(card, "статус: " + StatusText(), 11, Theme.TextDim, TextAnchor.UpperLeft, false, false);
            Ui.TopLeft(status.rectTransform, 84f, 32f, 280f, 18f);

            float p = Mathf.Clamp01(_game.XpProgress());
            RectTransform bar = Ui.Node("XpBar", card);
            Ui.TopLeft(bar, 84f, 54f, 240f, 8f);
            Ui.Img(bar, Theme.White10, "BarBg");
            RectTransform fill = Ui.Node("Fill", bar);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = new Vector2(240f * p, 0f);
            fill.anchoredPosition = Vector2.zero;
            Ui.Img(fill, Theme.Green, "FillImg");

            Text xp = Ui.Label(card, _game.Xp + " / " + _game.XpForLevel(_game.Level) + " XP", 11, Theme.TextMuted,
                TextAnchor.UpperLeft, false, false);
            Ui.TopLeft(xp.rectTransform, 84f, 64f, 240f, 16f);

            Text money = Ui.Label(card, "КАПИТАЛ", 10, Theme.TextMuted, TextAnchor.UpperRight, false, false);
            Ui.TopRight(money.rectTransform, -14f, 12f, 220f, 16f);
            Text total = Ui.Label(card,
                Fmt.DollarsFull((long)(_game.Dollars + _game.PortfolioValue())), 20, Theme.Yellow,
                TextAnchor.UpperRight, true, false);
            Ui.TopRight(total.rectTransform, -14f, 28f, 260f, 26f);
            Text income = Ui.Label(card, Fmt.Dollars((long)_game.MinerIncomePerMin()) + "/мин майнинг", 10,
                Theme.Green, TextAnchor.UpperRight, false, false);
            Ui.TopRight(income.rectTransform, -14f, 58f, 260f, 16f);
        }

        void StatsRow()
        {
            RectTransform row = Ui.HBox(_body, 6f, 0);
            LayoutElement rle = row.gameObject.AddComponent<LayoutElement>();
            rle.flexibleWidth = 1f;
            rle.preferredHeight = 62f;
            rle.minHeight = 62f;

            string[] values = { _game.TotalHacked.ToString(), _game.Miners.Count.ToString(), _game.TotalTrades.ToString(), _game.CompletedLessons.Count.ToString() };
            string[] captions = { "ВЗЛОМОВ", "МАЙНЕРОВ", "СДЕЛОК", "УРОКОВ" };
            Color[] colors = { Theme.Green, Theme.Cyan, Theme.Yellow, Theme.Pink };

            for (int i = 0; i < 4; i++)
            {
                RectTransform cell = Ui.Card(row, Theme.White10, Theme.PanelBar, "Stat");
                LayoutElement le = cell.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.preferredHeight = 62f;

                Text v = Ui.Label(cell, values[i], 18, colors[i], TextAnchor.MiddleCenter, true, false);
                Ui.TopLeft(v.rectTransform, 0f, 8f, 130f, 26f);
                Text c = Ui.Label(cell, captions[i], 10, Theme.TextMuted, TextAnchor.MiddleCenter, false, false);
                Ui.TopLeft(c.rectTransform, 0f, 36f, 130f, 18f);
            }
        }

        void AchievementsCard()
        {
            bool[] state = AchievementState();
            int done = 0;
            for (int i = 0; i < state.Length; i++) if (state[i]) done++;

            RectTransform card = Ui.Card(_body, Theme.White10, Theme.PanelDeep, "Achievements");
            LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            float h = 46f + 4f * 50f;
            le.preferredHeight = h;
            le.minHeight = h;

            Text head = Ui.Label(card, "ДОСТИЖЕНИЯ · " + done + "/" + Achievements.Length, 12, Theme.Cyan,
                TextAnchor.UpperLeft, true, false);
            Ui.TopLeft(head.rectTransform, 12f, 10f, 400f, 20f);

            float y = 34f;
            for (int i = 0; i < Achievements.Length; i++)
            {
                bool ok = state[i];
                Text star = Ui.Label(card, ok ? "★" : "☆", 15, ok ? Theme.Yellow : Theme.TextFaint,
                    TextAnchor.MiddleCenter, false, false);
                Ui.TopLeft(star.rectTransform, 12f, y, 22f, 44f);

                Text name = Ui.Label(card, Achievements[i].Name, 11, ok ? Theme.Text : Theme.TextMuted,
                    TextAnchor.UpperLeft, true, false);
                Ui.TopLeft(name.rectTransform, 40f, y + 4f, 300f, 18f);
                Text desc = Ui.Label(card, Achievements[i].Desc, 10, Theme.TextMuted, TextAnchor.UpperLeft, false, false);
                Ui.TopLeft(desc.rectTransform, 40f, y + 22f, 400f, 16f);
                y += 50f;
            }
        }

        bool[] AchievementState()
        {
            float total = _game.PortfolioValue() + _game.Dollars;
            return new bool[]
            {
                _game.TotalHacked >= 1,
                _game.TotalHacked >= 4,
                _game.TotalHacked >= 8,
                _game.Miners.Count >= 1,
                _game.Miners.Count >= 4,
                _game.TotalTrades >= 5,
                _game.CompletedLessons.Count >= 3,
                total >= 10000f,
            };
        }

        /// <summary>Слой кнопок окна: выше соседних окон, но ниже следующего окна в каскаде.</summary>
        int Btn()
        {
            return (_win != null ? _win.LayerValue : Ui.LayerWindowBase) + 2;
        }

        public void Tick() { }

        public void Dispose()
        {
            _game.StateChanged -= Refresh;
            _game.PricesChanged -= Refresh;
            Ui.UnregisterTick(_scroll);
        }
    }
}
