using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptoHack
{
    public enum SfxWave
    {
        Square,
        Sine,
        Saw
    }

    /// <summary>
    /// Синтезатор звуков — перенос scripts/autoload/sfx.gd.
    ///
    /// В веб-версии звук генерировался через WebAudio (playBeep в App.tsx).
    /// Здесь то же самое делается «руками»: сэмплы считаются в float[] и
    /// загружаются в AudioClip через SetData — никаких внешних аудиофайлов.
    /// </summary>
    public static class Sfx
    {
        public const int MixRate = 22050;
        const int PoolSize = 8;

        public static bool Enabled = true;

        static AudioSource[] _players;
        static int _next;
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        static readonly List<Note> Scheduled = new List<Note>();
        static float _time;

        class Note
        {
            public float At;
            public float Freq;
            public float Dur;
            public float Vol;
            public SfxWave Wave;
        }

        /// <summary>Создаёт пул плееров на указанном объекте (вызывается один раз из GameBoot).</summary>
        public static void Init(GameObject host)
        {
            _players = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                AudioSource src = host.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.volume = 0.5f;          // в Godot было volume_db = -6
                src.spatialBlend = 0f;      // 2D-звук
                _players[i] = src;
            }
        }

        /// <summary>Проигрывает отложенные ноты (используется для «мелодий»).</summary>
        public static void Tick(float dt)
        {
            if (Scheduled.Count == 0) return;
            _time += dt;
            for (int i = Scheduled.Count - 1; i >= 0; i--)
            {
                if (_time >= Scheduled[i].At)
                {
                    Note n = Scheduled[i];
                    Beep(n.Freq, n.Dur, n.Wave, n.Vol);
                    Scheduled.RemoveAt(i);
                }
            }
        }

        public static void Beep(float freq, float dur, SfxWave wave, float vol)
        {
            if (!Enabled || _players == null) return;
            AudioClip clip = GetClip(freq, dur, wave, vol);
            if (clip == null) return;
            AudioSource src = _players[_next];
            _next = (_next + 1) % _players.Length;
            src.PlayOneShot(clip, 1f);
        }

        static AudioClip GetClip(float freq, float dur, SfxWave wave, float vol)
        {
            string key = Mathf.RoundToInt(freq) + "|" + Mathf.RoundToInt(dur * 1000f) + "|"
                + (int)wave + "|" + Mathf.RoundToInt(vol * 1000f);
            AudioClip clip;
            if (Cache.TryGetValue(key, out clip) && clip != null) return clip;
            clip = MakeClip(freq, dur, wave, vol);
            if (Cache.Count > 64) Cache.Clear();
            Cache[key] = clip;
            return clip;
        }

        static AudioClip MakeClip(float freq, float dur, SfxWave wave, float vol)
        {
            int frames = Mathf.Max(1, (int)(MixRate * dur));
            float[] data = new float[frames];
            float safeFreq = Mathf.Max(freq, 1f);
            float period = 1f / safeFreq;

            for (int i = 0; i < frames; i++)
            {
                float t = (float)i / (float)MixRate;
                float phase = (t % period) / period;
                float sample;
                if (wave == SfxWave.Square) sample = phase < 0.5f ? 1f : -1f;
                else if (wave == SfxWave.Saw) sample = phase * 2f - 1f;
                else sample = Mathf.Sin(2f * Mathf.PI * safeFreq * t);

                float fade = 1f - (float)i / (float)frames;   // мягкое затухание, чтобы не было щелчков
                data[i] = Mathf.Clamp(sample * vol * fade, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("neon_beep", frames, 1, MixRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Отложенная нота — для «мелодий» (в Godot это был create_timer).</summary>
        static void After(float delay, float freq, float dur, SfxWave wave, float vol)
        {
            if (!Enabled) return;
            Note n = new Note();
            n.At = _time + delay;
            n.Freq = freq;
            n.Dur = dur;
            n.Wave = wave;
            n.Vol = vol;
            Scheduled.Add(n);
        }

        // ---------------- Готовые «фирменные» звуки интерфейса ----------------
        public static void UiClick()
        {
            Beep(550f, 0.05f, SfxWave.Sine, 0.25f);
        }

        /// <summary>Окно открылось — короткий «выезд» вверх.</summary>
        public static void UiOpen()
        {
            if (!Enabled) return;
            Beep(520f, 0.05f, SfxWave.Square, 0.05f);
            After(0.05f, 760f, 0.06f, SfxWave.Square, 0.04f);
        }

        public static void UiOk()
        {
            Beep(700f, 0.03f, SfxWave.Sine, 0.18f);
        }

        public static void HackSuccess()
        {
            Beep(660f, 0.10f, SfxWave.Square, 0.35f);
            After(0.12f, 880f, 0.12f, SfxWave.Square, 0.35f);
            After(0.24f, 1320f, 0.18f, SfxWave.Square, 0.35f);
        }

        public static void HackFail()
        {
            Beep(180f, 0.25f, SfxWave.Saw, 0.35f);
        }

        public static void LevelUp()
        {
            Beep(523f, 0.12f, SfxWave.Square, 0.35f);
            After(0.13f, 659f, 0.12f, SfxWave.Square, 0.35f);
            After(0.26f, 784f, 0.20f, SfxWave.Square, 0.35f);
        }

        public static void BootLine(int index)
        {
            Beep(300f + (float)index * 90f, 0.05f, SfxWave.Square, 0.18f);
        }

        public static void Buy()
        {
            Beep(600f, 0.08f, SfxWave.Square, 0.35f);
            After(0.10f, 900f, 0.10f, SfxWave.Square, 0.35f);
        }

        public static void Trade()
        {
            Beep(760f, 0.07f, SfxWave.Sine, 0.3f);
        }

        // Обёртки, чтобы не обращаться к перечислению снаружи.
        public static void BeepSquare(float freq, float dur, float vol)
        {
            Beep(freq, dur, SfxWave.Square, vol);
        }

        public static void BeepSine(float freq, float dur, float vol)
        {
            Beep(freq, dur, SfxWave.Sine, vol);
        }

        public static void BeepSaw(float freq, float dur, float vol)
        {
            Beep(freq, dur, SfxWave.Saw, vol);
        }
    }
}
