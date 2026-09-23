using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>
    /// «ФАЙЛЫ // /home/ghost» — перенос scripts/ui/files_window.gd.
    /// Виртуальная файловая система собирается из состояния игры:
    /// выполненные миссии → exploit_*.py, монеты → *_wallet.dat, майнеры → *.log.
    /// </summary>
    public class FilesWindowView : IWindowView
    {
        const float TreeW = 214f;
        const float BodyH = 490f;

        class Entry
        {
            public string Folder;
            public string Name;
            public string Content;
            public Color Color;
        }

        readonly Game _game;
        UiWindow _win;
        RectTransform _tree;
        UiScroll _contentScroll;
        Text _fileTitle;
        Text _fileBody;
        Text _fileMeta;
        LayoutElement _fileLe;
        string _open = "";
        string _signature = "";

        public FilesWindowView(Game game)
        {
            _game = game;
        }

        public void Build(UiWindow win, RectTransform body)
        {
            _win = win;
            RectTransform left = Ui.Node("Tree", win.BodyHolder);
            Ui.TopLeft(left, 0f, 0f, TreeW, BodyH);
            Image bg = Ui.Img(left, Theme.PanelDeep, "Bg");
            Ui.Full(bg.rectTransform);

            Text head = Ui.Label(left, "/home/ghost", 10, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(head.rectTransform, 10f, 6f, TreeW - 20f, 16f);

            _tree = Ui.Node("TreeList", left);
            Ui.TopLeft(_tree, 0f, 26f, TreeW, BodyH - 26f);

            RectTransform right = Ui.Node("Viewer", win.BodyHolder);
            Ui.TopLeft(right, TreeW, 0f, win.Rt.sizeDelta.x - TreeW, BodyH);
            Image rbg = Ui.Img(right, new Color(0f, 0f, 0f, 0.35f), "Bg");
            Ui.Full(rbg.rectTransform);

            _fileTitle = Ui.Label(right, "выбери файл", 12, Theme.Cyan, TextAnchor.UpperLeft, true, false);
            Ui.TopLeft(_fileTitle.rectTransform, 12f, 8f, 400f, 18f);
            _fileMeta = Ui.Label(right, "", 10, Theme.TextMuted, TextAnchor.UpperLeft, false, false);
            Ui.TopLeft(_fileMeta.rectTransform, 12f, 26f, 500f, 16f);

            _contentScroll = Ui.Scroll(right, "FileScroll");
            Ui.TopLeft(_contentScroll.View, 12f, 46f, win.Rt.sizeDelta.x - TreeW - 24f, BodyH - 58f);
            Image sbg = _contentScroll.View.gameObject.GetComponent<Image>();
            if (sbg != null) sbg.color = Theme.Black;
            Ui.Border(_contentScroll.View, Theme.White10);

            _fileBody = Ui.Label(_contentScroll.Content, "", 11, Theme.Text, TextAnchor.UpperLeft, false, true);
            _fileLe = _fileBody.gameObject.AddComponent<LayoutElement>();
            _fileLe.flexibleWidth = 1f;
            _fileLe.preferredHeight = Theme.LineHeight(11) + 8f;

            _game.StateChanged += Refresh;
            Refresh();
        }

        // ==================== ФАЙЛЫ ====================
        List<Entry> BuildEntries()
        {
            List<Entry> list = new List<Entry>();

            list.Add(Make("docs", "пароли.txt", PasswordsText(), Theme.Cyan));
            list.Add(Make("docs", "шпаргалка_python.txt", CheatSheetText(), Theme.Cyan));

            foreach (Mission m in _game.Data.Missions)
            {
                if (!_game.IsMissionCompleted(m.Id)) continue;
                string code;
                if (!HackWindowView.SavedCode.TryGetValue(m.Id, out code)) code = m.Solution;
                list.Add(Make("exploits", "exploit_" + m.Id + "_" + Slug(m.Title) + ".py", code, Theme.Green));
            }

            foreach (CryptoInfo c in _game.Data.Cryptos)
            {
                float amount = _game.GetCrypto(c.Id);
                if (amount <= 0f) continue;
                string text = "# Кошелёк " + c.Name + " (" + c.Id + ")\n" +
                    "address: " + Address(c.Id) + "\n" +
                    "balance: " + Fmt.Crypto(amount) + " " + c.Id + "\n" +
                    "usd: " + Fmt.DollarsFull(amount * _game.GetPrice(c.Id)) + "\n" +
                    "note: ключ восстановления потерян при взломе. так бывает.\n";
                list.Add(Make("wallets", c.Id.ToLowerInvariant() + "_wallet.dat", text, Theme.Yellow));
            }

            foreach (Miner m in _game.Miners)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("rig: ").Append(m.PcName).Append('\n');
                sb.Append("ip: ").Append(m.Ip).Append('\n');
                sb.Append("coin: ").Append(m.Crypto).Append('\n');
                sb.Append("earned: ").Append(Fmt.Crypto(m.Earned)).Append(' ').Append(m.Crypto).Append('\n');
                sb.Append("uptime: постоянно, пока комп жертвы включён\n");
                sb.Append("log:\n");
                sb.Append("  [ok] miner installed\n");
                sb.Append("  [ok] payouts every second\n");
                sb.Append("  [ok] tracker not detected\n");
                list.Add(Make("miners", Slug(m.PcName) + ".log", sb.ToString(), Theme.Pink));
            }

            return list;
        }

        static Entry Make(string folder, string name, string content, Color color)
        {
            Entry e = new Entry();
            e.Folder = folder;
            e.Name = name;
            e.Content = content;
            e.Color = color;
            return e;
        }

        static string Slug(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            }
            return sb.ToString().Trim('_');
        }

        static string Address(string id)
        {
            System.Random rnd = new System.Random(id.GetHashCode() & 0x7fffffff);
            const string abc = "0123456789abcdef";
            StringBuilder sb = new StringBuilder("0x");
            for (int i = 0; i < 16; i++) sb.Append(abc[rnd.Next(abc.Length)]);
            return sb.ToString();
        }

        static string PasswordsText()
        {
            return "# найденные пароли (реальные люди, реальные ошибки)\n" +
                "admin@darkmail.io   : qwerty2019\n" +
                "crypto_exchange_ru  : Pa$$w0rd!\n" +
                "neon_net_router     : 12345678\n" +
                "director@corp.local : Summer2024\n" +
                "\n# вывод: длина пароля не спасает, если это дата рождения.\n";
        }

        static string CheatSheetText()
        {
            return "# Шпаргалка Python для хакера\n\n" +
                "def scan(ip):           # сканировать цель\n" +
                "def connect(ip):        # подключиться (нужен код-либ)\n" +
                "def brute(target):      # перебор пароля\n" +
                "def bypass(firewall):   # обход защиты\n" +
                "def decrypt(data):      # расшифровка\n" +
                "def extract(target):    # вытащить данные\n" +
                "def drain(target):      # вывести деньги\n" +
                "def install_miner(t):   # поставить майнер\n" +
                "def wallets():          # список кошельков\n\n" +
                "Циклы:      for i in range(3):\n" +
                "Условия:    if security < 50:\n" +
                "Печать:     print('OK')\n" +
                "Списки:     for w in wallets():\n";
        }

        // ==================== ОТРИСОВКА ====================
        void Refresh()
        {
            string sig = _game.CompletedMissions.Count + "|" + _game.Miners.Count + "|" +
                _game.TotalTrades + "|" + (int)_game.Dollars;
            foreach (CryptoInfo c in _game.Data.Cryptos) sig += "|" + _game.GetCrypto(c.Id).ToString("0.0000");
            if (sig == _signature) return;
            _signature = sig;

            List<Entry> entries = BuildEntries();
            Ui.DestroyChildren(_tree);

            float y = 0f;
            string folder = "";
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.Folder != folder)
                {
                    folder = e.Folder;
                    Text fh = Ui.Label(_tree, "▶ " + folder + "/", 11, Theme.Yellow, TextAnchor.MiddleLeft, true, false);
                    Ui.TopLeft(fh.rectTransform, 10f, y, TreeW - 20f, 20f);
                    y += 22f;
                }

                UiButton b = UiButton.New(_tree, "• " + e.Name, e.Color, 10, UiButton.Ghost, 20f);
                Ui.TopLeft(b.Rt, 0f, y, TreeW, 20f);
                b.Caption.alignment = TextAnchor.MiddleLeft;
                Ui.Stretch(b.Caption.rectTransform, 26f, 0f, 6f, 0f);
                b.LayerProvider = delegate { return Btn(); };
                Entry entry = e;
                b.OnClick = delegate { Open(entry); };
                y += 21f;
            }

            if (_open.Length > 0)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Name == _open)
                    {
                        Open(entries[i]);
                        break;
                    }
                }
            }
            else if (entries.Count > 0)
            {
                Open(entries[0]);
            }
        }

        void Open(Entry e)
        {
            _open = e.Name;
            _fileTitle.text = "▸ " + e.Folder + "/" + e.Name;
            _fileTitle.color = e.Color;
            int lines = 1;
            for (int i = 0; i < e.Content.Length; i++) if (e.Content[i] == '\n') lines++;
            _fileMeta.text = lines + " строк · " + e.Content.Length + " байт · изменён только что";
            _fileBody.text = CodeEditor.Escape(e.Content);
            _fileBody.color = Theme.Text;
            if (_fileLe != null)
            {
                float h = Theme.WrappedHeight(e.Content, 11,
                    Mathf.Max(120f, _contentScroll.View.rect.width - 20f)) + 10f;
                _fileLe.preferredHeight = h;
                _fileLe.minHeight = h;
            }
            _contentScroll.ScrollToTop();
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
            Ui.UnregisterTick(_contentScroll);
        }
    }
}
