using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    /// <summary>
    /// «ШКОЛА PYTHON» — перенос scripts/ui/learn_window.gd:
    /// список уроков слева, теория и мини-тест с автопроверкой справа.
    /// </summary>
    public class LearnWindowView : IWindowView
    {
        const float ListW = 214f;
        const float BodyH = 490f;

        readonly Game _game;
        UiWindow _win;
        RectTransform _list;
        RectTransform _rightBox;
        UiScroll _rightScroll;
        Lesson _lesson;
        readonly Dictionary<int, int> _answers = new Dictionary<int, int>();
        string _signature = "";

        public LearnWindowView(Game game)
        {
            _game = game;
        }

        public void Build(UiWindow win, RectTransform body)
        {
            _win = win;

            RectTransform left = Ui.Node("Lessons", win.BodyHolder);
            Ui.TopLeft(left, 0f, 0f, ListW, BodyH);
            Image leftBg = Ui.Img(left, Theme.PanelDeep, "Bg");
            Ui.Full(leftBg.rectTransform);

            Text head = Ui.Label(left, "ШКОЛА PYTHON", 10, Theme.TextMuted, TextAnchor.MiddleLeft, false, false);
            Ui.TopLeft(head.rectTransform, 10f, 6f, ListW - 20f, 16f);

            _list = Ui.Node("List", left);
            Ui.TopLeft(_list, 0f, 26f, ListW, BodyH - 26f);

            _rightBox = Ui.Node("Right", win.BodyHolder);
            Ui.TopLeft(_rightBox, ListW, 0f, win.Rt.sizeDelta.x - ListW, BodyH);
            _rightScroll = Ui.Scroll(_rightBox, "LessonScroll");
            Ui.Full(_rightScroll.View);

            _game.StateChanged += RefreshList;

            int firstId = _game.Data.Lessons.Count > 0 ? _game.Data.Lessons[0].Id : 1;
            Select(firstId);
        }

        // ==================== СПИСОК ====================
        void RefreshList()
        {
            string sig = _game.CompletedLessons.Count + "|" + (_lesson != null ? _lesson.Id : 0);
            if (sig == _signature) return;
            _signature = sig;

            Ui.DestroyChildren(_list);
            float y = 0f;
            foreach (Lesson l in _game.Data.Lessons)
            {
                bool done = _game.IsLessonCompleted(l.Id);
                bool selected = _lesson != null && _lesson.Id == l.Id;

                string label = (done ? "✓" : l.Id.ToString()) + "  " + l.Title + "\n" + l.Duration + " · +" + l.Xp + " XP";
                UiButton b = UiButton.New(_list, label, done ? Theme.Green : Theme.Pink, 11, UiButton.Ghost, 46f);
                Ui.TopLeft(b.Rt, 0f, y, ListW, 46f);
                b.Caption.alignment = TextAnchor.MiddleLeft;
                Ui.Stretch(b.Caption.rectTransform, 10f, 0f, 6f, 0f);
                b.LayerProvider = delegate { return Btn(); };
                if (selected)
                {
                    Image bg = b.Rt.gameObject.GetComponent<Image>();
                    if (bg != null) bg.color = Theme.WithAlpha(Theme.Pink, 0.10f);
                }
                int id = l.Id;
                b.OnClick = delegate { Select(id); };
                y += 46f;
            }
        }

        void Select(int id)
        {
            _lesson = _game.Data.GetLesson(id);
            _answers.Clear();
            _signature = "";
            RefreshList();
            Rebuild();
        }

        // ==================== ТЕОРИЯ + ТЕСТ ====================
        void Rebuild()
        {
            RectTransform content = _rightScroll.Content;
            Ui.DestroyChildren(content);
            if (_lesson == null) return;

            RectTransform box = Ui.VBox(content, 8f, 14);
            LayoutElement le = box.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            Text title = Ui.Line(box, _lesson.Title, 16, Theme.Text, TextAnchor.MiddleLeft, true);
            LayoutElement tle = title.gameObject.GetComponent<LayoutElement>();
            tle.flexibleWidth = 1f;
            Text sub = Ui.Line(box, _lesson.Subtitle, 11, Theme.Pink);
            LayoutElement sle = sub.gameObject.GetComponent<LayoutElement>();
            sle.flexibleWidth = 1f;

            for (int i = 0; i < _lesson.Content.Count; i++)
            {
                LessonBlock block = _lesson.Content[i];
                RectTransform card = Ui.Card(box, Theme.White10, new Color(0f, 0f, 0f, 0.4f), "Block");
                LayoutElement ble = card.gameObject.AddComponent<LayoutElement>();
                ble.flexibleWidth = 1f;

                float textH = Theme.WrappedHeight(block.Text, 12, 560f);
                ble.preferredHeight = textH + 46f;
                ble.minHeight = textH + 46f;

                Text heading = Ui.Label(card, block.Heading, 12, Theme.Cyan, TextAnchor.UpperLeft, true, false);
                Ui.TopLeft(heading.rectTransform, 10f, 6f, 560f, 18f);
                Text body = Ui.Label(card, block.Text, 12, Theme.TextSoft, TextAnchor.UpperLeft, false, true);
                Ui.TopLeft(body.rectTransform, 10f, 24f, 560f, textH + 4f);
            }

            BuildQuiz(box);
        }

        void BuildQuiz(RectTransform box)
        {
            int correct = CorrectCount();
            bool allAnswered = _answers.Count >= _lesson.Quiz.Count;
            bool passed = _lesson.Quiz.Count > 0 && correct == _lesson.Quiz.Count;
            bool done = _game.IsLessonCompleted(_lesson.Id);

            RectTransform card = Ui.Card(box, Theme.WithAlpha(Theme.Yellow, 0.4f), Theme.WithAlpha(Theme.Yellow, 0.04f), "Quiz");
            LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            float height = 40f;
            for (int i = 0; i < _lesson.Quiz.Count; i++)
            {
                height += 26f + Theme.WrappedHeight(_lesson.Quiz[i].Q, 12, 560f) + 34f;
            }
            height += 44f;
            le.preferredHeight = height;
            le.minHeight = height;

            Text head = Ui.Label(card, "✎ ПРОВЕРКА ЗНАНИЙ (" + correct + "/" + _lesson.Quiz.Count + ")",
                12, Theme.Yellow, TextAnchor.UpperLeft, true, false);
            Ui.TopLeft(head.rectTransform, 10f, 8f, 560f, 18f);

            float y = 32f;
            for (int qi = 0; qi < _lesson.Quiz.Count; qi++)
            {
                QuizItem q = _lesson.Quiz[qi];
                float qh = Theme.WrappedHeight(q.Q, 12, 560f);

                Text qtext = Ui.Label(card, (qi + 1) + ". " + q.Q, 12, Theme.Text, TextAnchor.UpperLeft, false, true);
                Ui.TopLeft(qtext.rectTransform, 10f, y, 560f, qh + 4f);
                y += qh + 6f;

                int picked = -1;
                _answers.TryGetValue(qi, out picked);

                float x = 10f;
                for (int oi = 0; oi < q.Options.Length; oi++)
                {
                    bool isPicked = picked == oi;
                    bool isRight = oi == q.Answer;
                    bool showRight = picked >= 0 && isRight;
                    bool showWrong = isPicked && !isRight;
                    Color color = showRight ? Theme.Green : (showWrong ? Theme.Pink : Theme.TextDim);

                    float w = Mathf.Min(180f, Theme.CharWidth(11) * q.Options[oi].Length + 22f);
                    UiButton b = UiButton.New(card, q.Options[oi], color,
                        isPicked ? UiButton.Solid : UiButton.Outline, 11, 28f);
                    Ui.TopLeft(b.Rt, x, y, w, 28f);
                    b.LayerProvider = delegate { return Btn(); };
                    b.SetDisabled(done);
                    int qIndex = qi;
                    int oIndex = oi;
                    b.OnClick = delegate
                    {
                        _answers[qIndex] = oIndex;
                        Rebuild();
                    };
                    x += w + 6f;
                }
                y += 34f;
            }

            if (done)
            {
                Text ok = Ui.Label(card, "✓ Урок пройден! +" + _lesson.Xp + " XP получено", 12, Theme.Green,
                    TextAnchor.UpperLeft, true, false);
                Ui.TopLeft(ok.rectTransform, 10f, y + 4f, 560f, 20f);
            }
            else
            {
                string label = "ОТВЕТЬ НА ВСЕ ВОПРОСЫ";
                if (allAnswered && !passed) label = "ЕСТЬ ОШИБКИ — ПОПРОБУЙ ЕЩЁ";
                else if (allAnswered && passed) label = "ЗАБРАТЬ +" + _lesson.Xp + " XP";

                UiButton claim = UiButton.New(card, label, Theme.Yellow, 12, UiButton.Solid, 32f);
                Ui.TopLeft(claim.Rt, 10f, y + 2f, 380f, 32f);
                claim.LayerProvider = delegate { return Btn(); };
                claim.SetDisabled(!(allAnswered && passed));
                int lessonId = _lesson.Id;
                claim.OnClick = delegate
                {
                    _game.CompleteLesson(lessonId);
                    _signature = "";
                    RefreshList();
                    Rebuild();
                };
            }
        }

        int CorrectCount()
        {
            int count = 0;
            for (int i = 0; i < _lesson.Quiz.Count; i++)
            {
                int picked;
                if (_answers.TryGetValue(i, out picked) && picked == _lesson.Quiz[i].Answer) count++;
            }
            return count;
        }

        /// <summary>Слой кнопок окна: выше соседних окон, но ниже следующего окна в каскаде.</summary>
        int Btn()
        {
            return (_win != null ? _win.LayerValue : Ui.LayerWindowBase) + 2;
        }

        public void Tick() { }

        public void Dispose()
        {
            _game.StateChanged -= RefreshList;
            Ui.UnregisterTick(_rightScroll);
        }
    }
}
