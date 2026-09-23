using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>«ЧЁРНЫЙ РЫНОК» (апгрейды) — перенос scripts/ui/upgrade_window.gd.</summary>
    public class UpgradeWindowView : IWindowView
    {
        readonly Game _game;
        UiWindow _win;
        RectTransform _balance;
        RectTransform _cards;
        string _signature = "";

        static string Icon(string id)
        {
            if (id == "hackSpeed") return "⚡";
            if (id == "minerEff") return "⚙";
            if (id == "codeLib") return "✎";
            if (id == "stealth") return "○";
            return "◆";
        }

        public UpgradeWindowView(Game game)
        {
            _game = game;
        }

        public void Build(UiWindow win, RectTransform body)
        {
            _win = win;
            RectTransform box = Ui.VBox(body, 10f, 12);
            LayoutElement le = box.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            _balance = Ui.Card(box, Theme.WithAlpha(Theme.Yellow, 0.45f), Theme.WithAlpha(Theme.Yellow, 0.05f), "Balance");
            LayoutElement ble = _balance.gameObject.AddComponent<LayoutElement>();
            ble.flexibleWidth = 1f;
            ble.preferredHeight = 34f;

            _cards = Ui.VBox(box, 10f, 0);
            LayoutElement cle = _cards.gameObject.AddComponent<LayoutElement>();
            cle.flexibleWidth = 1f;

            _game.StateChanged += Refresh;
            Refresh();
        }

        void Refresh()
        {
            string sig = (int)_game.Dollars + "|" + _game.Level;
            for (int i = 0; i < _game.Data.Upgrades.Count; i++)
            {
                sig += "|" + _game.UpgradeLevel(_game.Data.Upgrades[i].Id);
            }
            if (sig == _signature) return;
            _signature = sig;

            Ui.DestroyChildren(_balance);
            RectTransform row = Ui.HBox(_balance, 8f, 10);
            Ui.Full(row);
            Text label = Ui.Line(row, "Баланс:", 12, Theme.TextDim);
            Text amount = Ui.Line(row, Fmt.DollarsFull((long)_game.Dollars), 15, Theme.Yellow, TextAnchor.MiddleLeft, true);
            amount.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;
            Ui.Line(row, "продавай крипту на бирже → качайся", 10, Theme.TextMuted);

            Ui.DestroyChildren(_cards);
            for (int i = 0; i < _game.Data.Upgrades.Count; i++) BuildCard(_game.Data.Upgrades[i]);
        }

        void BuildCard(UpgradeInfo up)
        {
            int lvl = _game.UpgradeLevel(up.Id);
            bool maxed = lvl >= up.MaxLevel;
            int cost = _game.UpgradeCost(up.Id);
            bool afford = cost >= 0 && _game.Dollars >= cost;

            RectTransform card = Ui.Card(_cards, Theme.White10, Theme.PanelBar, "Upgrade_" + up.Id);
            LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 84f;
            le.minHeight = 84f;

            Text icon = Ui.Label(card, Icon(up.Id), 20, Theme.Cyan, TextAnchor.MiddleCenter, false, false);
            Ui.TopLeft(icon.rectTransform, 6f, 6f, 40f, 70f);

            Text name = Ui.Label(card, up.Name, 13, Theme.Text, TextAnchor.UpperLeft, true, false);
            Ui.TopLeft(name.rectTransform, 52f, 6f, 300f, 18f);

            Text lvlLabel = Ui.Label(card, "ур. " + lvl + "/" + up.MaxLevel, 10, Theme.TextMuted, TextAnchor.UpperLeft, false, false);
            Ui.TopLeft(lvlLabel.rectTransform, 250f, 8f, 100f, 16f);

            Text desc = Ui.Label(card, up.Desc, 11, Theme.TextDim, TextAnchor.UpperLeft, false, true);
            Ui.TopLeft(desc.rectTransform, 52f, 26f, 420f, 30f);

            // полоска уровней
            float pipW = 380f / Mathf.Max(1, up.MaxLevel);
            for (int i = 0; i < up.MaxLevel; i++)
            {
                RectTransform pip = Ui.Node("Pip", card);
                Ui.TopLeft(pip, 52f + i * pipW, 58f, pipW - 2f, 5f);
                Ui.Img(pip, i < lvl ? Theme.Green : Theme.White10, "PipImg");
            }

            string effect = "Сейчас: " + up.EffectText(lvl);
            if (!maxed) effect += "  →  " + up.EffectText(lvl + 1);
            Text effectLabel = Ui.Label(card, effect, 10, Theme.Cyan, TextAnchor.UpperLeft, false, false);
            Ui.TopLeft(effectLabel.rectTransform, 52f, 66f, 420f, 16f);

            if (maxed)
            {
                Text max = Ui.Label(card, "✓ MAX", 12, Theme.Green, TextAnchor.MiddleRight, true, false);
                Ui.TopRight(max.rectTransform, -12f, 0f, 90f, 84f);
            }
            else
            {
                UiButton buy = UiButton.New(card, Fmt.Dollars(cost), Theme.Yellow, 11, UiButton.Solid, 30f);
                Ui.TopRight(buy.Rt, -12f, 27f, 100f, 30f);
                buy.LayerProvider = delegate { return Btn(); };
                buy.SetDisabled(!afford);
                string id = up.Id;
                buy.OnClick = delegate
                {
                    _game.BuyUpgrade(id);
                    _signature = "";
                    Refresh();
                };
            }
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
        }
    }
}
