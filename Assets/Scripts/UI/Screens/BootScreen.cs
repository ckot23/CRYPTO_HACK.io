using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>Экран «загрузки BIOS» — перенос scripts/ui/boot_screen.gd.</summary>
    public class BootScreen : IGameScreen
    {
        readonly GameBoot _boot;
        RectTransform _root;
        Text _log;
        Text _percent;
        Text _status;
        RectTransform _barFill;
        float _barWidth = 520f;

        List<string> _lines = new List<string>();
        readonly List<string> _queue = new List<string>();
        float _lineTimer;
        int _totalLines;
        float _elapsed;
        bool _done;
        float _charTimer;
        string _typing = "";
        int _charIndex;
        float _holdTimer;

        MatrixBackground _matrix;

        const float LineDelay = 0.16f;
        const float CharDelay = 0.006f;

        public BootScreen(GameBoot boot)
        {
            _boot = boot;
        }

        public void Build(RectTransform parent)
        {
            _root = parent;
            Game game = _boot.Game;

            Image bg = Ui.Img(parent, Theme.Black, "Bg");
            Ui.Full(bg.rectTransform);

            _matrix = MatrixBackground.New(parent);
            _matrix.Refresh();

            RectTransform logBox = Ui.Node("LogBox", parent);
            Ui.TopLeft(logBox, 48f, 48f, 1180f, 560f);

            _log = Ui.Label(logBox, "", 12, Theme.Green, TextAnchor.UpperLeft, false, true);
            Ui.Full(_log.rectTransform);
            _log.lineSpacing = 1.2f;

            RectTransform bar = Ui.Node("Bar", parent);
            Ui.TopLeft(bar, 330f, 640f, _barWidth, 8f);
            Image barBg = Ui.Img(bar, Theme.White15, "BarBg");
            Ui.Full(barBg.rectTransform);

            RectTransform fill = Ui.Node("Fill", bar);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = new Vector2(0f, 0f);
            fill.anchoredPosition = Vector2.zero;
            Image fillImg = Ui.Img(fill, Theme.Green, "FillImg");
            Ui.Full(fillImg.rectTransform);
            _barFill = fill;

            _percent = Ui.Label(parent, "0%", 12, Theme.Green, TextAnchor.MiddleCenter, false, false);
            Ui.TopLeft(_percent.rectTransform, 330f, 612f, _barWidth, 20f);

            _status = Ui.Label(parent, "NEON OS v4.2 · инициализация...", 11, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(_status.rectTransform, 48f, 656f, 900f, 20f);

            Text skip = Ui.Label(parent, "[ нажми ЛЮБУЮ КЛАВИШУ или ПРОБЕЛ — пропустить ]",
                10, Theme.TextFaint, TextAnchor.MiddleRight, false, false);
            Ui.TopLeft(skip.rectTransform, 700f, 656f, 528f, 20f);

            BuildQueue(game);
        }

        void BuildQueue(Game game)
        {
            int missions = game.Data.Missions.Count;
            int lessons = game.Data.Lessons.Count;
            int upgrades = game.Data.Upgrades.Count;
            int cryptos = game.Data.Cryptos.Count;

            _queue.Clear();
            _queue.Add("NEON BIOS v4.2.1 — POST OK");
            _queue.Add("CPU: quantum-core 4x5.2GHz · RAM: 64GB · GPU: n/a");
            _queue.Add("mount /dev/neo0 → /neon .............. OK");
            _queue.Add("crypto modules: " + cryptos + " · missions: " + missions + " · upgrades: " + upgrades);
            _queue.Add("loading school_db (" + lessons + " lessons) ..... OK");
            _queue.Add("starting net daemon ............... OK");
            _queue.Add("checking blacklist ............... CLEAN");
            _queue.Add("checking whitehat tracker ........ CLEAN");
            _queue.Add("profile: level " + game.Level + " · xp " + game.Xp + " · $" + Fmt.Dollars((long)game.Dollars));
            _queue.Add("» всё готово. Добро пожаловать в NEON NET, хакер.");
            _totalLines = _queue.Count;
        }

        void Push(string line)
        {
            _lines.Add(line);
            if (_log != null) _log.text = string.Join("\n", _lines.ToArray());
            Sfx.BootLine(_lines.Count);
        }

        int ConsumedLines { get { return _lines.Count + (_typing.Length > 0 ? 1 : 0); } }

        void RefreshBar()
        {
            float p = _totalLines > 0 ? Mathf.Clamp01((float)ConsumedLines / (float)_totalLines) : 1f;
            if (_percent != null) _percent.text = Mathf.RoundToInt(p * 100f) + "%" + (p >= 1f ? " · ГОТОВО" : "") + "  [ ПРОБЕЛ — ПРОПУСТИТЬ ]";
            if (_barFill != null) _barFill.sizeDelta = new Vector2(_barWidth * p, 0f);
            if (_status != null && _lines.Count > 0) _status.text = _lines[_lines.Count - 1];
        }

        public void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            if (_matrix != null) _matrix.Refresh();

            bool skip = UiInput.SpaceDown || UiInput.AnyKeyDown;

            if (skip)
            {
                while (_queue.Count > 0)
                {
                    Push(_queue[0]);
                    _queue.RemoveAt(0);
                }
                Finish();
                return;
            }

            if (_done) return;

            if (_charIndex < _typing.Length)
            {
                _charTimer += dt;
                while (_charIndex < _typing.Length && _charTimer >= CharDelay)
                {
                    _charTimer -= CharDelay;
                    _charIndex++;
                }
                _log.text = string.Join("\n", _lines.ToArray()) +
                    (_lines.Count > 0 ? "\n" : "") + _typing.Substring(0, _charIndex) + "█";
                RefreshBar();
                return;
            }

            if (_typing.Length > 0)
            {
                _queue.Insert(0, _typing);
                _typing = "";
                _charIndex = 0;
            }

            if (_queue.Count == 0)
            {
                _holdTimer += dt;
                if (_holdTimer >= 1.2f) Finish();
                return;
            }

            _lineTimer += dt;
            if (_lineTimer >= LineDelay)
            {
                _lineTimer = 0f;
                _typing = _queue[0];
                _queue.RemoveAt(0);
                _charIndex = 0;
                _charTimer = 0f;
            }
        }

        void Finish()
        {
            if (_done) return;
            _done = true;

            // сохраняем время последнего запуска, как в Godot-версии
            PlayerPrefs.SetString("ch_last_boot", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            PlayerPrefs.Save();

            if (_percent != null) _percent.text = "100% · ГОТОВО";
            if (_status != null) _status.text = "запуск рабочего стола...";
            if (_barFill != null) _barFill.sizeDelta = new Vector2(_barWidth, 0f);

            _boot.ShowDesktop();
        }

        public void Dispose() { }
    }
}
