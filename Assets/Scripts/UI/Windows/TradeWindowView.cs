using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>«БИРЖА DARKEX» — перенос scripts/ui/trade_window.gd (живой график + сделки).</summary>
    public class TradeWindowView : IWindowView
    {
        readonly Game _game;
        UiWindow _win;

        RectTransform _cardsRow;
        UiChart _chart;
        Text _chartTitle;
        Text _chartChange;
        Text _feeLabel;
        Text _sellInfo;
        Text _sellResult;
        Text _buyInfo;
        Text _buyResult;
        UiButton _sellButton;
        UiButton _buyButton;
        UiInputField _amount;
        string _selected = "BTC";
        string _signature = "";

        public TradeWindowView(Game game)
        {
            _game = game;
        }

        public void Build(UiWindow win, RectTransform body)
        {
            _win = win;
            RectTransform box = Ui.VBox(body, 8f, 12);
            LayoutElement le = box.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            // ---- карточки монет ----
            _cardsRow = Ui.HBox(box, 8f, 0);
            LayoutElement cre = _cardsRow.gameObject.AddComponent<LayoutElement>();
            cre.flexibleWidth = 1f;
            cre.preferredHeight = 74f;
            cre.minHeight = 74f;

            // ---- график ----
            RectTransform chartBox = Ui.Card(box, Theme.White10, new Color(0f, 0f, 0f, 0.5f), "ChartBox");
            LayoutElement chle = chartBox.gameObject.AddComponent<LayoutElement>();
            chle.flexibleWidth = 1f;
            chle.preferredHeight = 168f;
            chle.minHeight = 168f;

            RectTransform head = Ui.Node("Head", chartBox);
            Ui.TopLeft(head, 10f, 4f, 600f, 18f);
            _chartTitle = Ui.Label(head, "", 11, Theme.Green, TextAnchor.MiddleLeft, true, false);
            Ui.TopLeft(_chartTitle.rectTransform, 0f, 0f, 420f, 18f);
            _chartChange = Ui.Label(head, "", 11, Theme.Green, TextAnchor.MiddleRight, true, false);
            Ui.TopRight(_chartChange.rectTransform, -10f, 0f, 160f, 18f);

            _chart = UiChart.New(chartBox, 112f, 600);
            Ui.TopLeft(_chart.Rt, 10f, 24f, 600f, 112f);

            _feeLabel = Ui.Label(chartBox, "", 10, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(_feeLabel.rectTransform, 10f, 142f, 600f, 18f);

            // ---- ввод суммы ----
            RectTransform amountRow = Ui.HBox(box, 8f, 0);
            LayoutElement ale = amountRow.gameObject.AddComponent<LayoutElement>();
            ale.flexibleWidth = 1f;
            ale.preferredHeight = 30f;
            Ui.Line(amountRow, "Сумма в $:", 11, Theme.TextDim);

            _amount = UiInputField.New(amountRow, "100", 120f, 28f, Theme.Cyan);
            _amount.LayerProvider = delegate { return Btn(); };
            _amount.OnChanged = delegate { RefreshInfo(); };

            Text hint = Ui.Line(amountRow, "Enter — MAX · кнопки ниже выполняют сделку", 10, Theme.TextMuted);

            // ---- панели продажи и покупки ----
            RectTransform columns = Ui.HBox(box, 10f, 0);
            LayoutElement cole = columns.gameObject.AddComponent<LayoutElement>();
            cole.flexibleWidth = 1f;
            cole.preferredHeight = 118f;
            cole.minHeight = 118f;

            RectTransform sell = Ui.Card(columns, Theme.WithAlpha(Theme.Green, 0.4f), Theme.WithAlpha(Theme.Green, 0.05f), "Sell");
            LayoutElement sle = sell.gameObject.AddComponent<LayoutElement>();
            sle.flexibleWidth = 1f;
            sle.preferredHeight = 118f;
            RectTransform sellBox = Ui.VBox(sell, 4f, 10);
            Ui.Full(sellBox);
            Ui.Line(sellBox, "ПРОДАТЬ → $", 11, Theme.Green, TextAnchor.MiddleLeft, true);
            _sellInfo = Ui.Line(sellBox, "", 10, Theme.TextDim);
            _sellResult = Ui.Line(sellBox, "", 10, Theme.TextMuted);
            RectTransform sellRow = Ui.HBox(sellBox, 6f, 0);
            LayoutElement srle = sellRow.gameObject.AddComponent<LayoutElement>();
            srle.flexibleWidth = 1f;
            _sellButton = UiButton.New(sellRow, "ПРОДАТЬ ЗА $", Theme.Green, 11, UiButton.Solid, 30f);
            LayoutElement sb = _sellButton.Rt.gameObject.GetComponent<LayoutElement>();
            sb.flexibleWidth = 1f;
            _sellButton.LayerProvider = delegate { return Btn(); };
            _sellButton.OnClick = delegate { DoTrade(false); };
            UiButton maxBtn = UiButton.New(sellRow, "MAX", Theme.Cyan, 10, UiButton.Outline, 30f);
            Ui.Height(maxBtn.Rt.gameObject, 30f);
            maxBtn.Rt.sizeDelta = new Vector2(60f, 30f);
            maxBtn.LayerProvider = delegate { return Btn(); };
            maxBtn.OnClick = FillMax;

            RectTransform buy = Ui.Card(columns, Theme.WithAlpha(Theme.Cyan, 0.4f), Theme.WithAlpha(Theme.Cyan, 0.05f), "Buy");
            LayoutElement ble = buy.gameObject.AddComponent<LayoutElement>();
            ble.flexibleWidth = 1f;
            ble.preferredHeight = 118f;
            RectTransform buyBox = Ui.VBox(buy, 4f, 10);
            Ui.Full(buyBox);
            Ui.Line(buyBox, "КУПИТЬ $ → монета", 11, Theme.Cyan, TextAnchor.MiddleLeft, true);
            _buyInfo = Ui.Line(buyBox, "", 10, Theme.TextDim);
            _buyResult = Ui.Line(buyBox, "", 10, Theme.TextMuted);
            _buyButton = UiButton.New(buyBox, "КУПИТЬ", Theme.Cyan, 11, UiButton.Solid, 30f);
            LayoutElement bbl = _buyButton.Rt.gameObject.GetComponent<LayoutElement>();
            bbl.flexibleWidth = 1f;
            _buyButton.LayerProvider = delegate { return Btn(); };
            _buyButton.OnClick = delegate { DoTrade(true); };

            Text tip = Ui.Paragraph(box,
                "Совет трейдера: покупай на падении, продавай на росте. Стелс-модуль снижает комиссию вплоть до 0%.",
                10, Theme.TextMuted, 560f);

            _game.StateChanged += Refresh;
            _game.PricesChanged += Refresh;
            Refresh();
        }

        // ==================== СДЕЛКИ ====================
        void FillMax()
        {
            float price = _game.GetPrice(_selected);
            float amount = Mathf.Floor(_game.GetCrypto(_selected) * price);
            _amount.SetText(((long)amount).ToString(), true);
            RefreshInfo();
        }

        void DoTrade(bool isBuy)
        {
            float usd = _amount.Value;
            bool ok = _game.Trade(_selected, usd, isBuy);
            if (ok)
            {
                float price = Mathf.Max(0.000001f, _game.GetPrice(_selected));
                string verb = isBuy ? "Куплено" : "Продано";
                string text = isBuy
                    ? verb + " " + Fmt.Crypto(usd / price * (1f - _game.Fee())) + " " + _selected
                    : verb + " " + Fmt.DollarsFull(usd * (1f - _game.Fee())) + " $";
                if (isBuy)
                {
                    _buyResult.text = text;
                    _sellResult.text = "";
                }
                else
                {
                    _sellResult.text = text;
                    _buyResult.text = "";
                }
                Sfx.Trade();
                _signature = "";
                Refresh();
            }
            else
            {
                string warn = "Недостаточно средств или монета заблокирована.";
                if (isBuy) _buyResult.text = warn;
                else _sellResult.text = warn;
            }
        }

        // ==================== ОБНОВЛЕНИЕ ====================
        void Refresh()
        {
            string sig = _selected + "|" + (int)_game.Dollars + "|" + _game.Level;
            foreach (CryptoInfo c in _game.Data.Cryptos) sig += "|" + (int)_game.GetPrice(c.Id);
            if (sig == _signature) return;
            _signature = sig;

            RefreshCards();

            CryptoInfo info = _game.Data.GetCrypto(_selected);
            float price = _game.GetPrice(_selected);
            _chartTitle.text = info != null ? info.Icon + " " + info.Name + " / USD" : _selected;

            List<float> hist;
            if (!_game.History.TryGetValue(_selected, out hist)) hist = new List<float>();
            if (hist.Count >= 2)
            {
                float change = (hist[hist.Count - 1] - hist[0]) / hist[0] * 100f;
                _chartChange.text = (change >= 0 ? "▲ " : "▼ ") + Mathf.Abs(change).ToString("0.00") + "%";
                _chartChange.color = change >= 0 ? Theme.Green : Theme.Pink;
            }
            else
            {
                _chartChange.text = "";
            }
            _chart.SetValues(hist, info != null ? info.Color : Theme.Green);

            _feeLabel.text = "● живой график · обновляется каждые " + Game.PriceTickSec.ToString("0") +
                " сек · комиссия " + (_game.Fee() * 100f).ToString("0") + "%";

            RefreshInfo();
        }

        void RefreshInfo()
        {
            if (_amount == null) return;
            float price = Mathf.Max(0.000001f, _game.GetPrice(_selected));
            float balance = _game.GetCrypto(_selected);
            float usd = _amount.Value;

            if (_sellInfo != null)
            {
                _sellInfo.text = "Баланс: " + Fmt.Crypto(balance) + " " + _selected + " ≈ " +
                    Fmt.DollarsFull(balance * price);
            }
            if (_sellResult != null && _sellResult.text.Length == 0)
            {
                _sellResult.text = "Получишь: " + Fmt.DollarsFull(usd * (1f - _game.Fee()));
            }
            if (_buyInfo != null)
            {
                _buyInfo.text = "Доллары: " + Fmt.DollarsFull((long)_game.Dollars) + " · 1 " +
                    _selected + " = " + Fmt.Price(price);
            }
            if (_buyResult != null && _buyResult.text.Length == 0)
            {
                _buyResult.text = "Получишь: " + Fmt.Crypto(usd / price * (1f - _game.Fee())) + " " + _selected;
            }

            if (_sellButton != null) _sellButton.SetDisabled(usd <= 0f || usd / price > balance);
            if (_buyButton != null) _buyButton.SetDisabled(usd <= 0f || usd > _game.Dollars);
        }

        void RefreshCards()
        {
            Ui.DestroyChildren(_cardsRow);

            foreach (CryptoInfo info in _game.Data.Cryptos)
            {
                bool unlocked = _game.CryptoUnlocked(info.Id);
                bool selected = info.Id == _selected;
                float price = _game.GetPrice(info.Id);

                List<float> hist;
                if (!_game.History.TryGetValue(info.Id, out hist)) hist = new List<float>();
                float change = hist.Count >= 2 ? (hist[hist.Count - 1] - hist[0]) / hist[0] * 100f : 0f;

                RectTransform card = Ui.Card(_cardsRow, selected ? info.Color : Theme.White10, Theme.PanelDark, "Coin");
                LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.preferredHeight = 74f;
                le.minHeight = 74f;

                Text icon = Ui.Label(card, unlocked ? info.Icon : "○", 15, unlocked ? info.Color : Theme.TextMuted,
                    TextAnchor.UpperLeft, true, false);
                icon.font = Theme.IconFontFor(icon.text);
                Ui.TopLeft(icon.rectTransform, 8f, 6f, 22f, 20f);
                Text id = Ui.Label(card, info.Id, 12, Theme.Text, TextAnchor.UpperLeft, true, false);
                Ui.TopLeft(id.rectTransform, 30f, 6f, 80f, 18f);

                if (unlocked)
                {
                    Text p = Ui.Label(card, Fmt.Price(price), 12, Theme.Text, TextAnchor.UpperLeft, false, false);
                    Ui.TopLeft(p.rectTransform, 8f, 26f, 110f, 18f);
                    Text ch = Ui.Label(card, (change >= 0 ? "▲ " : "▼ ") + Mathf.Abs(change).ToString("0.00") + "%",
                        11, change >= 0 ? Theme.Green : Theme.Pink, TextAnchor.UpperLeft, false, false);
                    Ui.TopLeft(ch.rectTransform, 8f, 44f, 110f, 18f);
                }
                else
                {
                    Text lvl = Ui.Label(card, "ур. " + info.UnlockLevel, 10, Theme.TextMuted, TextAnchor.UpperLeft, false, false);
                    Ui.TopLeft(lvl.rectTransform, 8f, 40f, 110f, 18f);
                }

                string cid = info.Id;
                UiButton pick = UiButton.New(card, "", Color.white, 10, UiButton.Ghost, 60f);
                Ui.Full(pick.Rt);
                pick.LayerProvider = delegate { return Btn(); };
                pick.SetDisabled(!unlocked);
                pick.OnClick = delegate
                {
                    _selected = cid;
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
            _game.PricesChanged -= Refresh;
            Ui.UnregisterTick(_amount);
        }
    }
}
