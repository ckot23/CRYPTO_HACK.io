using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptoHack
{
    public interface IGameScreen
    {
        void Build(RectTransform parent);
        void Tick();
        void Dispose();
    }

    /// <summary>
    /// Точка входа игры — аналог autoload-ов и трёх сцен из Godot-версии
    /// (main_menu → boot → desktop) в одном запуске.
    ///
    /// Проект НЕ требует сцены с настроенными объектами: Bootstrap сам создаёт
    /// корневой GameObject до загрузки первой сцены, поэтому игра запускается
    /// даже из пустой сцены Unity (просто нажми Play).
    /// </summary>
    public class GameBoot : MonoBehaviour
    {
        public static GameBoot I;

        public Game Game;
        public UiToastLayer Toasts;
        public Canvas Canvas;
        public RectTransform ScreenRoot;
        public RectTransform ModalLayer;

        IGameScreen _screen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (I != null) return;
            GameObject go = new GameObject("CRYPTO_HACK");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameBoot>();
        }

        void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;

            Application.targetFrameRate = 60;
            if (!Application.isEditor)
            {
                Screen.SetResolution(1280, 720, false);
            }

            Theme.Init();
            UiInput.Init();
            EnsureAudioListener();
            Sfx.Init(gameObject);
            PyRunner.Init();
            BuildCanvas();

            Game = new Game();
            Game.Init();
            Game.ToastRequested += OnToast;

            // если в проекте выбрана схема ввода без нужного пакета — говорим об этом сразу
            if (!string.IsNullOrEmpty(UiInput.ErrorMessage))
            {
                Game.Notify("Ввод недоступен", UiInput.ErrorMessage, "warn");
            }

            ShowMainMenu();
        }

        /// <summary>
        /// В пустой сцене нет источника звука — добавляем свой, иначе AudioSource молчит.
        /// </summary>
        void EnsureAudioListener()
        {
#if UNITY_2022_2_OR_NEWER
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
#else
            AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>();
#endif
            if (listeners == null || listeners.Length == 0) gameObject.AddComponent<AudioListener>();
        }

        void BuildCanvas()
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            Canvas = canvasGo.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            Ui.Canvas = Canvas;

            ScreenRoot = Ui.Node("Screen", canvasGo.transform);
            Ui.Full(ScreenRoot);
            Ui.Root = ScreenRoot;

            ModalLayer = Ui.Node("ModalLayer", canvasGo.transform);
            Ui.Full(ModalLayer);
            Ui.ModalLayer = ModalLayer;

            RectTransform toastRt = Ui.Node("ToastLayer", canvasGo.transform);
            Ui.Full(toastRt);
            Ui.ToastRect = toastRt;
            Toasts = UiToastLayer.New(toastRt);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            UiInput.Update();
            Ui.UpdateInput();
            if (Game != null)
            {
                // настройка «Звук» из меню GHOST должна глушить и клики интерфейса
                Sfx.Enabled = Game.SoundOn;
                Game.Tick(dt);
            }
            Sfx.Tick(dt);

            // виджеты могут удалять себя во время Tick — идём с конца по снимку количества
            for (int i = Ui.Tickers.Count - 1; i >= 0; i--)
            {
                if (i < Ui.Tickers.Count)
                {
                    IUiTick t = Ui.Tickers[i];
                    if (t != null) t.Tick();
                }
            }

            if (_screen != null) _screen.Tick();
        }

        void OnToast(string title, string text, string kind)
        {
            if (Toasts != null) Toasts.Show(title, text, kind);
        }

        public void ShowScreen(IGameScreen screen)
        {
            if (_screen != null)
            {
                _screen.Dispose();
                Ui.DestroyChildren(ScreenRoot);
                if (ModalLayer != null) Ui.DestroyChildren(ModalLayer);
                Ui.Clickables.Clear();
                Ui.Tickers.Clear();
                // тосты живут отдельно, их тикер перерегистрируем
                if (Toasts != null) Ui.RegisterTick(Toasts);
            }
            _screen = screen;
            if (_screen != null) _screen.Build(ScreenRoot);
        }

        public void ShowMainMenu() { ShowScreen(new MainMenuScreen(this)); }
        public void ShowBoot() { ShowScreen(new BootScreen(this)); }
        public void ShowDesktop() { ShowScreen(new DesktopScreen(this)); }

        void OnApplicationQuit()
        {
            if (Game != null) Game.Save();
        }
    }
}
