using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>
    /// «NEON_HACK // ТЕРМИНАЛ ВЗЛОМА» — перенос scripts/ui/hack_window.gd.
    /// Слева список целей, справа вкладки (задание/теория/подсказки), редактор Python
    /// и консоль с выводом. Запуск идёт либо реальным python3 (PyRunner), либо
    /// встроенным симулятором (PySim) — как в веб-версии.
    /// </summary>
    public class HackWindowView : IWindowView
    {
        const float ListW = 218f;
        const float BodyH = 490f;
        const float TargetBarH = 36f;
        const float TabsH = 26f;
        const float TabBoxH = 148f;
        const float EditorH = 132f;
        const float ResultBarH = 26f;

        const string TabBrief = "brief";
        const string TabTheory = "theory";
        const string TabHints = "hints";

        /// <summary>Код, написанный игроком по миссиям — живёт до перезапуска игры.</summary>
        public static readonly Dictionary<int, string> SavedCode = new Dictionary<int, string>();

        readonly Game _game;
        UiWindow _win;
        RectTransform _list;
        RectTransform _tabRow;
        RectTransform _tabBox;
        Text _targetLabel;
        Text _targetMeta;
        Text _targetSec;
        CodeEditor _editor;
        UiConsole _console;
        UiButton _play;
        UiButton _modeButton;
        UiButton _resetButton;
        Text _syntaxLabel;
        RectTransform _resultBar;
        Text _resultLabel;
        Text _starsLabel;

        Mission _mission;
        string _tab = TabBrief;
        bool _showSolution;
        bool _running;
        int _stars;
        float _simRemaining;
        bool _simResultOk;
        bool _modeReal;
        List<string> _simMissing;
        int _simStyle;
        string _lastListSignature = "";

        public HackWindowView(Game game)
        {
            _game = game;
        }

        // ==================== ПОСТРОЕНИЕ ====================
        public void Build(UiWindow win, RectTransform body)
        {
            _win = win;
            UiWindow w = win;

            // ---- колонка целей ----
            RectTransform left = Ui.Node("Missions", w.BodyHolder);
            Ui.TopLeft(left, 0f, 0f, ListW, BodyH);
            Image leftBg = Ui.Img(left, Theme.PanelDeep, "Bg");
            Ui.Full(leftBg.rectTransform);

            Text head = Ui.Label(left, "ЦЕЛИ", 10, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(head.rectTransform, 10f, 6f, ListW - 20f, 16f);

            _list = Ui.Node("List", left);
            Ui.TopLeft(_list, 0f, 26f, ListW, BodyH - 26f);

            // ---- правая часть ----
            float rightW = win.Rt.sizeDelta.x - ListW;
            float x = ListW;

            RectTransform targetBar = Ui.Card(w.BodyHolder, Theme.White10, Theme.PanelDark, "TargetBar");
            Ui.TopLeft(targetBar, x, 0f, rightW, TargetBarH);
            _targetLabel = Ui.Label(targetBar, "—", 12, Theme.Text, TextAnchor.MiddleLeft, true, false);
            Ui.TopLeft(_targetLabel.rectTransform, 12f, 0f, 150f, 16f);
            _targetMeta = Ui.Label(targetBar, "", 11, Theme.Cyan, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(_targetMeta.rectTransform, 12f, 17f, 300f, 15f);
            _targetSec = Ui.Label(targetBar, "", 11, Theme.Yellow, TextAnchor.MiddleRight, false, false);
            Ui.TopRight(_targetSec.rectTransform, -12f, 0f, 200f, TargetBarH);

            float y = TargetBarH + 4f;

            // ---- вкладки ----
            RectTransform tabs = Ui.Card(w.BodyHolder, Theme.White10, Theme.PanelDark, "Tabs");
            Ui.TopLeft(tabs, x, y, rightW, TabsH);
            _tabRow = Ui.Node("TabRow", tabs);
            Ui.Stretch(_tabRow, 6f, 1f, 6f, 1f);
            Text fw = Ui.Label(tabs, "PYTHON 3.12 · NeonHack FW", 10, Theme.TextMuted, TextAnchor.MiddleRight, false, false);
            Ui.TopRight(fw.rectTransform, -10f, 0f, 220f, TabsH);
            y += TabsH + 4f;

            // ---- содержимое вкладки ----
            RectTransform tabPanel = Ui.Card(w.BodyHolder, Theme.White10, Theme.PanelDeep, "TabPanel");
            Ui.TopLeft(tabPanel, x, y, rightW, TabBoxH);
            UiScroll tabScroll = Ui.Scroll(tabPanel, "TabScroll");
            Ui.Full(tabScroll.View);
            _tabBox = tabScroll.Content;
            y += TabBoxH + 6f;

            // ---- редактор ----
            BuildEditor(w.BodyHolder, x, y, rightW);
            y += EditorH + 6f;

            // ---- консоль ----
            float consoleH = Mathf.Max(60f, BodyH - y - ResultBarH - 4f);
            _console = UiConsole.New(w.BodyHolder, consoleH);
            Ui.TopLeft(_console.Scroll.View, x, y, rightW, consoleH);
            y += consoleH + 4f;

            _resultBar = Ui.Card(w.BodyHolder, Theme.WithAlpha(Theme.Green, 0.35f), Theme.PanelBar, "ResultBar");
            Ui.TopLeft(_resultBar, x, y, rightW, ResultBarH);
            _resultLabel = Ui.Label(_resultBar, "", 11, Theme.Green, TextAnchor.MiddleLeft, true, false);
            Ui.TopLeft(_resultLabel.rectTransform, 10f, 0f, rightW - 120f, ResultBarH);
            _starsLabel = Ui.Label(_resultBar, "", 12, Theme.Yellow, TextAnchor.MiddleRight, false, false);
            Ui.TopRight(_starsLabel.rectTransform, -10f, 0f, 110f, ResultBarH);
            _resultBar.gameObject.SetActive(false);

            _game.StateChanged += OnStateChanged;
            _game.PricesChanged += OnStateChanged;
            SelectMission(_game.SelectedMission);
        }

        void BuildEditor(RectTransform parent, float x, float y, float width)
        {
            RectTransform head = Ui.Card(parent, Theme.White10, Theme.PanelDark, "EditorHead");
            Ui.TopLeft(head, x, y, width, 22f);

            Text title = Ui.Label(head, "⌁ exploit.py", 10, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(title.rectTransform, 10f, 0f, 160f, 22f);

            _modeButton = UiButton.New(head, "РЕЖИМ", Theme.Cyan, 10, UiButton.Ghost, 18f);
            Ui.TopRight(_modeButton.Rt, -104f, 2f, 118f, 18f);
            _modeButton.LayerProvider = delegate { return Btn(); };
            _modeButton.OnClick = ToggleMode;

            _resetButton = UiButton.New(head, "сбросить", Theme.TextMuted, 10, UiButton.Ghost, 18f);
            Ui.TopRight(_resetButton.Rt, -8f, 2f, 88f, 18f);
            _resetButton.LayerProvider = delegate { return Btn(); };
            _resetButton.OnClick = delegate
            {
                _editor.SetText(_mission.StarterCode, true);
                _console.Clear();
                _console.AddLine("Редактор сброшен к заготовке миссии.", PySim.KindInfo);
                SetResult("", 0, true);
            };

            float editorY = y + 22f;
            _editor = CodeEditor.New(parent, "", EditorH - 22f - 30f);
            Ui.TopLeft(_editor.Rt, x, editorY, width, EditorH - 22f - 30f);
            _editor.LayerProvider = delegate { return Btn(); };
            _editor.OnChanged = OnCodeChanged;

            RectTransform foot = Ui.Card(parent, Theme.White10, Theme.PanelDark, "EditorFoot");
            Ui.TopLeft(foot, x, editorY + (EditorH - 52f), width, 30f);

            _play = UiButton.New(foot, "▶ ЗАПУСТИТЬ", Theme.Green, 12, UiButton.Solid, 24f);
            Ui.TopLeft(_play.Rt, 8f, 3f, 150f, 24f);
            _play.LayerProvider = delegate { return Btn(); };
            _play.OnClick = Run;

            Text fileHint = Ui.Label(foot, "Ctrl+Enter — запуск · Tab — отступ", 10, Theme.TextFaint, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(fileHint.rectTransform, 168f, 0f, 260f, 30f);

            _syntaxLabel = Ui.Label(foot, "", 10, Theme.TextMuted, TextAnchor.MiddleRight, false, false);
            Ui.TopRight(_syntaxLabel.rectTransform, -10f, 0f, 300f, 30f);
        }

        // ==================== СПИСОК ЦЕЛЕЙ ====================
        string ListSignature()
        {
            return _game.CompletedMissions.Count + "|" + _game.Level + "|" + _game.Data.Missions.Count + "|" + _game.SelectedMission;
        }

        void RefreshList()
        {
            string sig = ListSignature();
            if (sig == _lastListSignature) return;
            _lastListSignature = sig;

            Ui.DestroyChildren(_list);

            float rowH = 52f;
            float y = 0f;
            foreach (Mission m in _game.Data.Missions)
            {
                bool done = _game.IsMissionCompleted(m.Id);
                bool unlocked = _game.MissionUnlocked(m);
                Color accent = done ? Theme.Green : (unlocked ? Theme.Text : Theme.TextFaint);

                string badge = done ? "✓" : (unlocked ? m.Id.ToString() : "✗");
                string line2 = m.RewardCrypto + " +" + Fmt.Crypto(m.RewardAmount) + " · " + Fmt.Dollars((long)m.RewardDollars) + " · " + m.RewardXp + " XP";
                string label = badge + "  " + m.Title + "\n" + line2;

                int id = m.Id;
                UiButton b = UiButton.New(_list, label, accent, 10, UiButton.Ghost, rowH - 2f);
                Ui.TopLeft(b.Rt, 0f, y, ListW, rowH - 2f);
                b.Caption.alignment = TextAnchor.MiddleLeft;
                Ui.Stretch(b.Caption.rectTransform, 10f, 0f, 6f, 0f);
                b.LayerProvider = delegate { return Btn(); };
                b.OnClick = delegate { SelectMission(id); };

                if (_mission != null && _mission.Id == m.Id)
                {
                    b.Kind = UiButton.Ghost;
                    b.Refresh();
                }
                y += rowH;
            }
        }

        void SelectMission(int id)
        {
            Mission found = _game.Data.GetMission(id);
            if (found == null) found = _game.Data.Missions.Count > 0 ? _game.Data.Missions[0] : null;
            if (found == null) return;
            if (_mission != null && _mission.Id == found.Id && _editor != null) return;

            _mission = found;
            _game.SelectedMission = _mission.Id;
            _tab = TabBrief;
            _showSolution = false;

            _targetLabel.text = _mission.TargetName;
            _targetMeta.text = _mission.TargetIp + " · " + _mission.Os + " · " + _mission.Difficulty;
            _targetSec.text = "Защита " + _mission.Security + "%";

            if (_editor != null)
            {
                string code;
                if (!SavedCode.TryGetValue(_mission.Id, out code)) code = _mission.StarterCode;
                _editor.SetText(code, true);
            }
            if (_console != null)
            {
                _console.Clear();
                _console.AddLine("$ цель загружена: " + _mission.TargetIp + " (" + _mission.Os + ")", PySim.KindCmd);
            }

            RefreshTab();
            _lastListSignature = "";
            RefreshList();
            RefreshModeButton();
            OnCodeChanged(_editor != null ? _editor.GetText() : "");
            SetResult("", 0, true);
        }

        // ==================== ВКЛАДКИ ====================
        void RefreshTab()
        {
            if (_mission == null || _tabRow == null) return;

            Ui.DestroyChildren(_tabRow);
            KeyValuePair<string, string>[] tabs =
            {
                new KeyValuePair<string, string>(TabBrief, "ЗАДАНИЕ"),
                new KeyValuePair<string, string>(TabTheory, "ТЕОРИЯ"),
                new KeyValuePair<string, string>(TabHints, "ПОДСКАЗКИ"),
            };

            float x = 0f;
            for (int i = 0; i < tabs.Length; i++)
            {
                bool active = _tab == tabs[i].Key;
                UiButton b = UiButton.New(_tabRow, tabs[i].Value,
                    active ? Theme.Green : Theme.TextMuted, 10, UiButton.Ghost, TabsH - 4f);
                Ui.TopLeft(b.Rt, x, 2f, 104f, TabsH - 4f);
                b.LayerProvider = delegate { return Btn(); };
                string key = tabs[i].Key;
                b.OnClick = delegate
                {
                    _tab = key;
                    _showSolution = false;
                    RefreshTab();
                };
                if (active)
                {
                    Image bg = b.Rt.gameObject.GetComponent<Image>();
                    if (bg != null) bg.color = Theme.WithAlpha(Theme.Green, 0.10f);
                }
                x += 106f;
            }

            Ui.DestroyChildren(_tabBox);
            bool locked = !_game.MissionUnlocked(_mission);
            if (locked) BuildLocked();
            else if (_tab == TabTheory) BuildTheory();
            else if (_tab == TabHints) BuildHints();
            else BuildBrief();
        }

        Text AddParagraph(RectTransform box, string text, int size, Color color, float width)
        {
            Text t = Ui.Paragraph(box, text, size, color, width);
            AddRowFix(t.rectTransform);
            return t;
        }

        /// <summary>Внутри VBox ширина тянется, но нужно явно разрешить flexible-ширину.</summary>
        void AddRowFix(RectTransform rt)
        {
            LayoutElement le = rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
        }

        void BuildLocked()
        {
            RectTransform box = Ui.VBox(_tabBox, 6f, 10);
            AddRowFix(box);
            Text t = Ui.Line(box, "ДОСТУП ЗАБЛОКИРОВАН", 18, Theme.Pink, TextAnchor.MiddleLeft, true);
            AddParagraph(box, "Цель требует уровень " + _mission.RequiredLevel +
                " (сейчас " + _game.Level + "). Взламывай другие миссии, учись в школе Python и качайся.",
                12, Theme.TextSoft, 540f);
            Ui.Line(box, "Концепт миссии: " + _mission.Concept, 12, Theme.Yellow);
        }

        void BuildBrief()
        {
            RectTransform box = Ui.VBox(_tabBox, 8f, 10);
            AddRowFix(box);
            AddParagraph(box, _mission.Briefing, 12, Theme.TextSoft, 560f);

            RectTransform taskBox = Ui.Card(box, Theme.WithAlpha(Theme.Green, 0.4f), Theme.WithAlpha(Theme.Green, 0.05f), "Task");
            Ui.Height(taskBox.gameObject, Theme.WrappedHeight(_mission.Task, 12, 540f) + 44f);
            LayoutElement tle = taskBox.gameObject.GetComponent<LayoutElement>();
            tle.flexibleWidth = 1f;
            Text th = Ui.Label(taskBox, "ЗАДАЧА:", 11, Theme.Green, TextAnchor.UpperLeft, true, false);
            Ui.TopLeft(th.rectTransform, 10f, 8f, 200f, 16f);
            Text tt = Ui.Label(taskBox, _mission.Task, 12, Theme.Text, TextAnchor.UpperLeft, false, true);
            Ui.TopLeft(tt.rectTransform, 10f, 26f, 520f, Theme.WrappedHeight(_mission.Task, 12, 520f) + 6f);

            string rewards = "  +" + Fmt.Crypto(_mission.RewardAmount) + " " + _mission.RewardCrypto +
                "   +" + Fmt.Dollars((long)_mission.RewardDollars) +
                "   +" + _mission.RewardXp + " XP" +
                (_game.IsMissionCompleted(_mission.Id) ? "   ✓ пройдено — повтор без награды" : "");
            Text rl = Ui.Line(box, rewards, 12, Theme.Yellow);
            AddRowFix(rl.rectTransform);
        }

        void BuildTheory()
        {
            RectTransform box = Ui.VBox(_tabBox, 6f, 10);
            AddRowFix(box);
            Text head = Ui.Line(box, "Тема: " + _mission.Concept, 12, Theme.Cyan, TextAnchor.MiddleLeft, true);
            AddRowFix(head.rectTransform);
            AddParagraph(box, _mission.ConceptDesc, 12, Theme.TextDim, 560f);

            for (int i = 0; i < _mission.Theory.Length; i++)
            {
                Text t = Ui.Paragraph(box, (i + 1) + ". " + _mission.Theory[i], 12, Theme.TextSoft, 540f);
                AddRowFix(t.rectTransform);
            }
        }

        void BuildHints()
        {
            RectTransform box = Ui.VBox(_tabBox, 6f, 10);
            AddRowFix(box);

            for (int i = 0; i < _mission.Hints.Length; i++)
            {
                Text t = Ui.Paragraph(box, "● " + _mission.Hints[i], 12, Theme.TextSoft, 540f);
                AddRowFix(t.rectTransform);
            }

            if (!_showSolution)
            {
                UiButton show = UiButton.New(box, "показать готовое решение (без штрафа, ты же учишься)",
                    Theme.TextMuted, 11, UiButton.Ghost, 26f);
                show.LayerProvider = delegate { return Btn(); };
                show.OnClick = delegate
                {
                    _showSolution = true;
                    RefreshTab();
                };
            }
            else
            {
                RectTransform sol = Ui.Card(box, Theme.WithAlpha(Theme.Pink, 0.45f), Theme.Black, "Solution");
                float h = Theme.WrappedHeight(_mission.Solution, 12, 520f) + 52f;
                Ui.Height(sol.gameObject, h);
                LayoutElement le = sol.gameObject.GetComponent<LayoutElement>();
                le.flexibleWidth = 1f;

                Text head = Ui.Label(sol, "РЕШЕНИЕ:", 10, Theme.Pink, TextAnchor.MiddleLeft, true, false);
                Ui.TopLeft(head.rectTransform, 10f, 6f, 200f, 16f);

                UiButton insert = UiButton.New(sol, "вставить в редактор", Theme.Green, 10, UiButton.Ghost, 20f);
                Ui.TopRight(insert.Rt, -8f, 6f, 140f, 20f);
                insert.LayerProvider = delegate { return Btn(); };
                insert.OnClick = delegate
                {
                    _editor.SetText(_mission.Solution, true);
                    _console.Clear();
                    _console.AddLine("Решение вставлено в редактор — разберись, как оно работает.", PySim.KindInfo);
                };

                Text body = Ui.Label(sol, _mission.Solution, 12, Theme.Text, TextAnchor.UpperLeft, false, true);
                Ui.TopLeft(body.rectTransform, 10f, 28f, 520f, h - 34f);
            }
        }

        // ==================== РЕЖИМ И СИНТАКСИС ====================
        void RefreshModeButton()
        {
            if (_modeButton == null) return;
            _modeButton.SetText("РЕЖИМ: " + (_game.RealPython ? "PYTHON" : "СИМУЛЯТОР"));
            _modeButton.SetDisabled(!PyRunner.Available);
        }

        void ToggleMode()
        {
            if (!PyRunner.Available) return;
            _game.RealPython = !_game.RealPython;
            _game.Save();
            RefreshModeButton();
            Sfx.UiClick();
            _console.Clear();
            _console.AddLine("Режим выполнения: " + _game.PythonModeText(), PySim.KindInfo);
        }

        void OnCodeChanged(string code)
        {
            if (_mission == null) return;
            SavedCode[_mission.Id] = code;

            if (_syntaxLabel == null) return;
            SyntaxProblem p = PySim.CheckSyntax(code);
            if (p != null && p.Line > 0)
            {
                _syntaxLabel.text = "⚠ строка " + p.Line + ": " + p.Msg;
                _syntaxLabel.color = Theme.Orange;
            }
            else if (p != null)
            {
                _syntaxLabel.text = "⚠ " + p.Msg;
                _syntaxLabel.color = Theme.Orange;
            }
            else
            {
                _syntaxLabel.text = "синтаксис: ок";
                _syntaxLabel.color = Theme.TextFaint;
            }
        }

        void OnStateChanged()
        {
            RefreshList();
        }

        // ==================== ЗАПУСК ====================
        void SetResult(string text, int stars, bool ok)
        {
            if (_resultBar == null) return;
            if (string.IsNullOrEmpty(text))
            {
                _resultBar.gameObject.SetActive(false);
                _stars = 0;
                return;
            }
            _resultBar.gameObject.SetActive(true);
            _resultLabel.text = text;
            _resultLabel.color = ok ? Theme.Green : Theme.Pink;
            SetBorderColor(_resultBar, Theme.WithAlpha(ok ? Theme.Green : Theme.Pink, 0.35f));
            _stars = stars;
            _starsLabel.text = stars <= 0 ? "" : new string('★', stars) + new string('☆', Mathf.Max(0, 3 - stars));
        }

        void SetBorderColor(RectTransform card, Color color)
        {
            Image[] images = card.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject.name == "Border") images[i].color = color;
            }
        }

        void Run()
        {
            if (_running || _mission == null || !_game.MissionUnlocked(_mission)) return;
            _running = true;
            _simMissing = null;
            _play.SetDisabled(true);
            _play.SetText("ВЫПОЛНЯЕТСЯ...");
            _console.Clear();
            SetResult("", 0, true);
            Sfx.BeepSquare(440f, 0.07f, 0.3f);

            string code = _editor != null ? _editor.GetText() : "";
            _modeReal = _game.RealPython && PyRunner.Available;
            if (_modeReal)
            {
                _console.AddLine("$ " + PyRunner.Describe() + " exploit.py --target " + _mission.TargetIp, PySim.KindCmd);
                PyRunner.Begin(code, _mission, PyRunner.DefaultTimeoutMs);
            }
            else
            {
                SimResult r = PySim.Simulate(code, _mission, _game.UpgradeLevel("hackSpeed"));
                int total = 0;
                for (int i = 0; i < r.Logs.Count; i++)
                {
                    total = Mathf.Max(total, r.Logs[i].Delay);
                    _console.AddDelayed(r.Logs[i].Text, r.Logs[i].Kind, r.Logs[i].Delay);
                }
                _simRemaining = total / 1000f + 0.12f;
                _simResultOk = r.Success;
                _simMissing = r.Missing;
                _simStyle = r.StyleScore;
            }
        }

        void FinishRun(bool success, List<string> missing, int stars)
        {
            _running = false;
            _play.SetDisabled(false);
            _play.SetText("▶ ЗАПУСТИТЬ");

            if (success)
            {
                if (!_game.IsMissionCompleted(_mission.Id))
                {
                    _game.HackSuccess(_mission, stars);
                }
                else
                {
                    _console.AddLine("Цель уже взломана — награда не начисляется.", PySim.KindInfo);
                }
                SetResult("ВЗЛОМ УСПЕШЕН", stars, true);
                Sfx.HackSuccess();
            }
            else
            {
                if (missing != null && missing.Count > 0)
                {
                    _console.AddLine("Не хватает шагов: " + missing.Count + ". Открой вкладку «Подсказки».", PySim.KindWarn);
                }
                SetResult("ВЗЛОМ ПРОВАЛЕН — смотри подсказки и пробуй ещё", 0, false);
                Sfx.HackFail();
            }
        }

        static string GuessKind(string line)
        {
            if (line.StartsWith("$")) return PySim.KindCmd;
            if (line.Contains("Error") || line.Contains("Ошибка") || line.StartsWith("  File")) return PySim.KindErr;
            if (line.StartsWith("[") && (line.Contains("OK") || line.Contains("drained")
                || line.Contains("внедрён") || line.Contains("извлечены") || line.Contains("подобран")
                || line.Contains("ослеплён") || line.Contains("расшифрована")))
            {
                return PySim.KindOk;
            }
            return PySim.KindInfo;
        }

        // ==================== ЦИКЛ ====================
        /// <summary>Слой кнопок окна: выше соседних окон, но ниже следующего окна в каскаде.</summary>
        int Btn()
        {
            return (_win != null ? _win.LayerValue : Ui.LayerWindowBase) + 2;
        }

        public void Tick()
        {
            if (_mission == null) return;

            // Ctrl+Enter — быстрый запуск
            if (UiInput.Ctrl && UiInput.KeyDown(UiKey.Enter)) Run();

            if (!_running) return;

            if (!_modeReal)
            {
                _simRemaining -= Time.unscaledDeltaTime;
                if (_simRemaining <= 0f)
                {
                    List<string> missing = _simMissing;
                    _simMissing = null;
                    FinishRun(_simResultOk, missing, _simStyle);
                }
                return;
            }

            if (!PyRunner.Running)
            {
                PyRunResult res = PyRunner.Result;
                if (res == null) return;

                List<string> missing = PySim.CheckPatterns(_editor.GetText(), _mission);
                if (res.TimedOut)
                {
                    _console.AddLine("Превышено время выполнения — вероятно, бесконечный цикл.", PySim.KindWarn);
                }
                string all = string.Join("\n", res.Lines.ToArray());
                bool syntaxFailed = all.Contains("SyntaxError");
                if (res.Lines.Count == 0 && !string.IsNullOrEmpty(res.Error))
                {
                    _console.AddLine(res.Error, PySim.KindErr);
                }
                bool ok = missing.Count == 0 && !syntaxFailed && !res.TimedOut;
                FinishRun(ok, missing, PySim.StyleScore(_editor.GetText(), _mission, missing));
            }
        }

        public void Dispose()
        {
            _game.StateChanged -= OnStateChanged;
            _game.PricesChanged -= OnStateChanged;
            if (_console != null) Ui.UnregisterTick(_console);
        }
    }
}
