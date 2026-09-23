using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>«МАЙНИНГ-ФЕРМА» — перенос scripts/ui/miner_window.gd.</summary>
    public class MinerWindowView : IWindowView
    {
        readonly Game _game;
        UiWindow _win;
        RectTransform _head;
        RectTransform _list;
        RectTransform _cryptoRow;
        RectTransform _installBox;
        Text _costLabel;
        string _selectedCrypto = "BTC";
        string _signature = "";

        public MinerWindowView(Game game)
        {
            _game = game;
        }

        public void Build(UiWindow win, RectTransform body)
        {
            _win = win;
            RectTransform box = Ui.VBox(body, 10f, 12);
            LayoutElement le = box.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            _head = Ui.Card(box, Theme.WithAlpha(Theme.Cyan, 0.45f), Theme.WithAlpha(Theme.Cyan, 0.05f), "Head");
            LayoutElement hle = _head.gameObject.GetComponent<LayoutElement>();
            if (hle == null) hle = _head.gameObject.AddComponent<LayoutElement>();
            hle.flexibleWidth = 1f;

            _list = Ui.VBox(box, 6f, 0);
            LayoutElement lle = _list.gameObject.GetComponent<LayoutElement>();
            if (lle == null) lle = _list.gameObject.AddComponent<LayoutElement>();
            lle.flexibleWidth = 1f;

            RectTransform pick = Ui.VBox(box, 6f, 0);
            LayoutElement ple = pick.gameObject.GetComponent<LayoutElement>();
            if (ple == null) ple = pick.gameObject.AddComponent<LayoutElement>();
            ple.flexibleWidth = 1f;

            Ui.Line(pick, "УСТАНОВИТЬ МАЙНЕР", 11, Theme.TextDim, TextAnchor.MiddleLeft, true);
            _cryptoRow = Ui.HBox(pick, 6f, 0);
            LayoutElement cle = _cryptoRow.gameObject.GetComponent<LayoutElement>();
            if (cle == null) cle = _cryptoRow.gameObject.AddComponent<LayoutElement>();
            cle.flexibleWidth = 1f;
            _costLabel = Ui.Line(pick, "", 11, Theme.TextMuted);

            _installBox = Ui.VBox(box, 6f, 0);
            LayoutElement ile = _installBox.gameObject.GetComponent<LayoutElement>();
            if (ile == null) ile = _installBox.gameObject.AddComponent<LayoutElement>();
            ile.flexibleWidth = 1f;

            Text hint = Ui.Paragraph(box,
                "Майнеры капают крипту каждую секунду, даже когда окно закрыто.",
                10, Theme.TextMuted, 560f);

            _game.StateChanged += Refresh;
            _game.MinersChanged += Refresh;
            Refresh();
        }

        // ==================== ОБНОВЛЕНИЕ ====================
        void Refresh()
        {
            string sig = _game.Miners.Count + "|" + _game.Level + "|" + _selectedCrypto + "|" +
                (int)_game.Dollars + "|" + (int)_game.MinerInstallCost() + "|" + _game.CompletedMissions.Count;
            if (sig == _signature) return;
            _signature = sig;

            RefreshHead();
            RefreshMiners();
            RefreshPicker();
            RefreshInstallList();
        }

        void RefreshHead()
        {
            Ui.DestroyChildren(_head);
            RectTransform row = Ui.HBox(_head, 6f, 12);
            Ui.Full(row);

            Text title = Ui.Line(row, "МАЙНИНГ-ФЕРМА · " + _game.Miners.Count + " РИГ(ОВ)", 13, Theme.Cyan,
                TextAnchor.MiddleLeft, true);
            title.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

            Ui.Line(row, "мощность x" + _game.MinerEffMultNow().ToString("0.0"), 11, Theme.TextMuted);

            Text line = Ui.Paragraph(_head, "Доход: ~" + Fmt.DollarsFull((long)_game.MinerIncomePerMin()) +
                "/мин пассивно", 11, Theme.Green, 540f);
            Ui.TopLeft(line.rectTransform, 12f, 34f, 560f, 18f);

            LayoutElement le = _head.gameObject.GetComponent<LayoutElement>();
            le.preferredHeight = 58f;
            le.minHeight = 58f;
        }

        void RefreshMiners()
        {
            Ui.DestroyChildren(_list);
            if (_game.Miners.Count == 0) return;

            for (int i = 0; i < _game.Miners.Count; i++)
            {
                Miner m = _game.Miners[i];
                CryptoInfo info = _game.Data.GetCrypto(m.Crypto);
                Color c = info != null ? info.Color : Theme.Cyan;

                RectTransform card = Ui.Card(_list, Theme.White10, Theme.PanelBar, "Miner");
                LayoutElement cle = card.gameObject.GetComponent<LayoutElement>();
                cle.flexibleWidth = 1f;
                cle.preferredHeight = 62f;
                cle.minHeight = 62f;

                Text icon = Ui.Label(card, info != null ? info.Icon : "?", 18, c, TextAnchor.MiddleCenter, true, false);
                icon.font = Theme.IconFontFor(icon.text);
                Ui.TopLeft(icon.rectTransform, 8f, 6f, 30f, 50f);

                Text name = Ui.Label(card, m.PcName, 12, Theme.Text, TextAnchor.MiddleLeft, true, false);
                Ui.TopLeft(name.rectTransform, 44f, 6f, 300f, 18f);

                Text meta = Ui.Label(card, m.Ip + " · майнит " + m.Crypto, 10, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
                Ui.TopLeft(meta.rectTransform, 44f, 24f, 300f, 15f);

                Text earned = Ui.Label(card, "добыто: " + Fmt.Crypto(m.Earned) + " " + m.Crypto, 11, Theme.Green, TextAnchor.MiddleLeft, false, false);
                Ui.TopLeft(earned.rectTransform, 44f, 39f, 300f, 16f);

                Text live = Ui.Label(card, "● LIVE", 10, Theme.Green, TextAnchor.MiddleRight, false, false);
                Ui.TopRight(live.rectTransform, -110f, 0f, 90f, 62f);

                string minerId = m.Id;
                UiButton stop = UiButton.New(card, "снять", Theme.Pink, 10, UiButton.Outline, 24f);
                Ui.TopRight(stop.Rt, -10f, 19f, 88f, 24f);
                stop.LayerProvider = delegate { return Btn(); };
                stop.OnClick = delegate { _game.RemoveMiner(minerId); };
            }
        }

        void RefreshPicker()
        {
            Ui.DestroyChildren(_cryptoRow);
            foreach (CryptoInfo info in _game.Data.Cryptos)
            {
                bool unlocked = _game.CryptoUnlocked(info.Id);
                bool selected = info.Id == _selectedCrypto;

                UiButton b = UiButton.New(_cryptoRow, info.Icon + " " + info.Id, info.Color,
                    selected ? UiButton.Solid : UiButton.Outline, 11, 30f);
                // тут текст только латиница, поэтому иконочный шрифт подходит целиком
                b.Caption.font = Theme.IconFontFor(b.Caption.text);
                Ui.Height(b.Rt.gameObject, 30f);
                b.Rt.anchorMin = new Vector2(0f, 0f);
                b.Rt.anchorMax = new Vector2(0f, 0f);
                b.Rt.pivot = new Vector2(0f, 0f);
                b.Rt.sizeDelta = new Vector2(82f, 30f);
                b.LayerProvider = delegate { return Btn(); };
                b.SetDisabled(!unlocked);
                string cid = info.Id;
                b.OnClick = delegate { _selectedCrypto = cid; _signature = ""; Refresh(); };
            }

            _costLabel.text = "Стоимость установки: " + Fmt.DollarsFull((long)_game.MinerInstallCost()) +
                " · " + _game.PythonModeText();
        }

        void RefreshInstallList()
        {
            Ui.DestroyChildren(_installBox);

            List<Mission> free = new List<Mission>();
            foreach (Mission m in _game.Data.Missions)
            {
                if (_game.IsMissionCompleted(m.Id) && !_game.MinersOn(m.Id)) free.Add(m);
            }

            if (free.Count == 0)
            {
                RectTransform card = Ui.Card(_installBox, Theme.White10, Theme.PanelDark, "Empty");
                LayoutElement le = card.gameObject.GetComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.preferredHeight = 34f;
                Text t = Ui.Label(card, "Нет свободных взломанных компов. Взломай новую цель в Хак-терминале.",
                    11, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
                Ui.Stretch(t.rectTransform, 10f, 0f, 10f, 0f);
                return;
            }

            float cost = _game.MinerInstallCost();
            for (int i = 0; i < free.Count; i++)
            {
                Mission m = free[i];
                RectTransform card = Ui.Card(_installBox, Theme.White10, new Color(0f, 0f, 0f, 0.4f), "Target");
                LayoutElement le = card.gameObject.GetComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.preferredHeight = 46f;
                le.minHeight = 46f;

                Text name = Ui.Label(card, m.TargetName, 12, Theme.Text, TextAnchor.MiddleLeft, false, false);
                Ui.TopLeft(name.rectTransform, 10f, 4f, 340f, 18f);
                Text meta = Ui.Label(card, m.TargetIp + " · " + m.Os, 10, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
                Ui.TopLeft(meta.rectTransform, 10f, 24f, 340f, 16f);

                int missionId = m.Id;
                UiButton install = UiButton.New(card, "+ " + _selectedCrypto, Theme.Cyan, 11, UiButton.Solid, 26f);
                Ui.TopRight(install.Rt, -10f, 10f, 130f, 26f);
                install.LayerProvider = delegate { return Btn(); };
                install.SetDisabled(_game.Dollars < cost);
                install.OnClick = delegate
                {
                    _game.InstallMiner(missionId, _selectedCrypto);
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
            _game.MinersChanged -= Refresh;
        }
    }
}
