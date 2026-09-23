using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptoHack
{
    /// <summary>Числовое форматирование — перенос Neon.fmt_* из Godot-версии.</summary>
    public static class Fmt
    {
        /// <summary>1234567 -> "1 234 567" (как toLocaleString('ru-RU')).</summary>
        public static string GroupDigits(string intStr)
        {
            bool negative = intStr.StartsWith("-");
            string digits = negative ? intStr.Substring(1) : intStr;
            string result = "";
            int count = 0;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                result = digits[i] + result;
                count++;
                if (count % 3 == 0 && i > 0)
                {
                    result = " " + result;
                }
            }
            return negative ? "-" + result : result;
        }

        public static string Dollars(float n)
        {
            if (n >= 10000f) return "$" + (n / 1000f).ToString("0.0", Num) + "K";
            if (n < 100f) return "$" + n.ToString("0.00", Num);
            return "$" + n.ToString("0", Num);
        }

        public static string DollarsFull(float n)
        {
            string s = Mathf.Abs(n).ToString("0.00", Num);
            int dot = s.IndexOf('.');
            string whole = dot >= 0 ? s.Substring(0, dot) : s;
            string frac = dot >= 0 ? s.Substring(dot + 1) : "00";
            return (n < 0f ? "-$" : "$") + GroupDigits(whole) + "," + frac;
        }

        public static string Crypto(float n)
        {
            if (Mathf.Abs(n) < 1e-9f) return "0";
            float a = Mathf.Abs(n);
            if (a < 0.000001f) return n.ToString("0.00e+00", Num);
            if (a < 0.01f) return n.ToString("0.000000", Num);
            if (a < 1f) return n.ToString("0.0000", Num);
            return n.ToString("0.000", Num);
        }

        public static string Price(float n)
        {
            return "$" + GroupDigits(n.ToString("0", Num));
        }

        public static string Int(float n)
        {
            return GroupDigits(n.ToString("0", Num));
        }

        static readonly System.Globalization.NumberFormatInfo Num =
            new System.Globalization.NumberFormatInfo
            {
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = "\u00a0",
                NumberGroupSizes = new int[0]
            };
    }

    /// <summary>Монета. Перенос CryptoInfo из Godot-версии / gameData.ts.</summary>
    public class CryptoInfo
    {
        public string Id = "";
        public string Name = "";
        public string FullName = "";
        public string Icon = "";
        public Color Color = Color.white;
        public float BasePrice;
        public int UnlockLevel = 1;
        public string Desc = "";
        public float Volatility = 0.01f;

        public static CryptoInfo From(CryptoDto d)
        {
            CryptoInfo c = new CryptoInfo();
            c.Id = d.id == null ? "" : d.id;
            c.Name = d.name == null ? "" : d.name;
            c.FullName = string.IsNullOrEmpty(d.fullName) ? c.Name : d.fullName;
            c.Icon = string.IsNullOrEmpty(d.icon) ? (c.Id.Length > 0 ? c.Id.Substring(0, 1) : "?") : d.icon;
            c.Color = Theme.Hex(string.IsNullOrEmpty(d.color) ? "#ffffff" : d.color);
            c.BasePrice = d.basePrice;
            c.UnlockLevel = d.unlockLevel <= 0 ? 1 : d.unlockLevel;
            c.Desc = d.desc == null ? "" : d.desc;
            c.Volatility = d.volatility <= 0f ? 0.01f : d.volatility;
            return c;
        }
    }

    /// <summary>Миссия-взлом. Перенос Mission из Godot-версии.</summary>
    public class Mission
    {
        public int Id;
        public string Title = "";
        public string TargetName = "";
        public string TargetIp = "";
        public string Os = "";
        public int Security;
        public string Difficulty = "";
        public string Concept = "";
        public string ConceptDesc = "";
        public string Briefing = "";
        public string Task = "";
        public string StarterCode = "";
        public string Solution = "";
        public string[] Hints = new string[0];
        public string[] RequiredPatterns = new string[0];
        public string RewardCrypto = "BTC";
        public float RewardAmount;
        public float RewardDollars;
        public int RewardXp;
        public int RequiredCodeLib;
        public int RequiredLevel = 1;
        public string[] Theory = new string[0];

        public static Mission From(MissionDto d)
        {
            Mission m = new Mission();
            m.Id = d.id;
            m.Title = Str(d.title);
            m.TargetName = Str(d.targetName);
            m.TargetIp = Str(d.targetIp);
            m.Os = Str(d.os);
            m.Security = d.security;
            m.Difficulty = Str(d.difficulty);
            m.Concept = Str(d.concept);
            m.ConceptDesc = Str(d.conceptDesc);
            m.Briefing = Str(d.briefing);
            m.Task = Str(d.task);
            m.StarterCode = Str(d.starterCode);
            m.Solution = Str(d.solution);
            m.Hints = d.hints == null ? new string[0] : d.hints;
            m.RequiredPatterns = d.requiredPatterns == null ? new string[0] : d.requiredPatterns;
            m.RewardCrypto = string.IsNullOrEmpty(d.rewardCrypto) ? "BTC" : d.rewardCrypto;
            m.RewardAmount = d.rewardAmount;
            m.RewardDollars = d.rewardDollars;
            m.RewardXp = d.rewardXp;
            m.RequiredCodeLib = d.requiredCodeLib;
            m.RequiredLevel = d.requiredLevel <= 0 ? 1 : d.requiredLevel;
            m.Theory = d.theory == null ? new string[0] : d.theory;
            return m;
        }

        public string RewardTitle()
        {
            return "+" + Fmt.Crypto(RewardAmount) + " " + RewardCrypto;
        }

        public static string Str(string s)
        {
            return s == null ? "" : s;
        }
    }

    /// <summary>Апгрейд «чёрного рынка».</summary>
    public class UpgradeInfo
    {
        public string Id = "";
        public string Name = "";
        public string Desc = "";
        public string Icon = "";
        public int MaxLevel;
        public int[] Costs = new int[0];
        public string[] Effects = new string[0];

        public static UpgradeInfo From(UpgradeDto d)
        {
            UpgradeInfo u = new UpgradeInfo();
            u.Id = Mission.Str(d.id);
            u.Name = Mission.Str(d.name);
            u.Desc = Mission.Str(d.desc);
            u.Icon = Mission.Str(d.icon);
            u.MaxLevel = d.maxLevel;
            u.Costs = d.costs == null ? new int[0] : d.costs;
            u.Effects = d.effects == null ? new string[0] : d.effects;
            return u;
        }

        /// <summary>Стоимость следующего уровня (-1, если уже максимум).</summary>
        public int CostForLevel(int level)
        {
            if (level >= MaxLevel || level < 0 || level >= Costs.Length) return -1;
            return Costs[level];
        }

        public string EffectText(int level)
        {
            if (level < 0 || level >= Effects.Length) return "—";
            return Effects[level];
        }
    }

    public class LessonBlock
    {
        public string Heading = "";
        public string Text = "";
    }

    public class QuizItem
    {
        public string Q = "";
        public string[] Options = new string[0];
        public int Answer;
    }

    /// <summary>Урок «Школы Python» + мини-тест.</summary>
    public class Lesson
    {
        public int Id;
        public string Title = "";
        public string Subtitle = "";
        public string Duration = "";
        public int Xp;
        public List<LessonBlock> Content = new List<LessonBlock>();
        public List<QuizItem> Quiz = new List<QuizItem>();

        public static Lesson From(LessonDto d)
        {
            Lesson l = new Lesson();
            l.Id = d.id;
            l.Title = Mission.Str(d.title);
            l.Subtitle = Mission.Str(d.subtitle);
            l.Duration = Mission.Str(d.duration);
            l.Xp = d.xp;
            if (d.content != null)
            {
                for (int i = 0; i < d.content.Length; i++)
                {
                    LessonBlock b = new LessonBlock();
                    b.Heading = Mission.Str(d.content[i].heading);
                    b.Text = Mission.Str(d.content[i].text);
                    l.Content.Add(b);
                }
            }
            if (d.quiz != null)
            {
                for (int i = 0; i < d.quiz.Length; i++)
                {
                    QuizItem q = new QuizItem();
                    q.Q = Mission.Str(d.quiz[i].q);
                    q.Options = d.quiz[i].options == null ? new string[0] : d.quiz[i].options;
                    q.Answer = d.quiz[i].answer;
                    l.Quiz.Add(q);
                }
            }
            return l;
        }
    }

    /// <summary>Загрузчик контента игры из Assets/Resources/gamedata.json.</summary>
    public class GameData
    {
        public const string ResourcePath = "gamedata";

        public List<CryptoInfo> Cryptos = new List<CryptoInfo>();
        public List<Mission> Missions = new List<Mission>();
        public List<UpgradeInfo> Upgrades = new List<UpgradeInfo>();
        public List<Lesson> Lessons = new List<Lesson>();
        public List<string> Quotes = new List<string>();
        public string LoadError = "";

        public bool Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                LoadError = "Не найден Assets/Resources/" + ResourcePath + ".json";
                Debug.LogError(LoadError);
                return false;
            }

            GameDataDto dto = null;
            try
            {
                dto = JsonUtility.FromJson<GameDataDto>(asset.text);
            }
            catch (Exception e)
            {
                LoadError = "Некорректный JSON в " + ResourcePath + ".json: " + e.Message;
                Debug.LogError(LoadError);
                return false;
            }

            if (dto == null)
            {
                LoadError = "Пустой JSON в " + ResourcePath + ".json";
                Debug.LogError(LoadError);
                return false;
            }

            if (dto.cryptos != null)
            {
                for (int i = 0; i < dto.cryptos.Length; i++) Cryptos.Add(CryptoInfo.From(dto.cryptos[i]));
            }
            if (dto.missions != null)
            {
                for (int i = 0; i < dto.missions.Length; i++) Missions.Add(Mission.From(dto.missions[i]));
            }
            if (dto.upgrades != null)
            {
                for (int i = 0; i < dto.upgrades.Length; i++) Upgrades.Add(UpgradeInfo.From(dto.upgrades[i]));
            }
            if (dto.lessons != null)
            {
                for (int i = 0; i < dto.lessons.Length; i++) Lessons.Add(Lesson.From(dto.lessons[i]));
            }
            if (dto.quotes != null)
            {
                for (int i = 0; i < dto.quotes.Length; i++)
                {
                    if (!string.IsNullOrEmpty(dto.quotes[i])) Quotes.Add(dto.quotes[i]);
                }
            }
            return true;
        }

        public CryptoInfo GetCrypto(string id)
        {
            for (int i = 0; i < Cryptos.Count; i++)
            {
                if (Cryptos[i].Id == id) return Cryptos[i];
            }
            return null;
        }

        public Mission GetMission(int id)
        {
            for (int i = 0; i < Missions.Count; i++)
            {
                if (Missions[i].Id == id) return Missions[i];
            }
            return null;
        }

        public UpgradeInfo GetUpgrade(string id)
        {
            for (int i = 0; i < Upgrades.Count; i++)
            {
                if (Upgrades[i].Id == id) return Upgrades[i];
            }
            return null;
        }

        public Lesson GetLesson(int id)
        {
            for (int i = 0; i < Lessons.Count; i++)
            {
                if (Lessons[i].Id == id) return Lessons[i];
            }
            return null;
        }
    }
}
