using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CryptoHack
{
    public class Miner
    {
        public string Id = "";
        public int MissionId;
        public string PcName = "";
        public string Ip = "";
        public string Crypto = "BTC";
        public float Earned;
    }

    /// <summary>
    /// Всё состояние игры и экономика — перенос scripts/autoload/game.gd.
    ///
    /// В Godot это был autoload-синглтон с сигналами. Здесь обычный C#-класс
    /// с событиями (Action): UI подписывается на StateChanged/PricesChanged и
    /// перерисовывает себя. Тикает не сам, а из GameBoot.Update() — так порядок
    /// обновления предсказуем.
    /// </summary>
    public class Game
    {
        // ---- Экономика (перенос констант из App.tsx / game.gd) ----
        public static readonly Dictionary<string, float> MinerBaseRate = new Dictionary<string, float>
        {
            { "BTC", 0.0000009f },
            { "ETH", 0.000018f },
            { "XMR", 0.00042f },
            { "SOL", 0.00031f },
        };

        public static readonly float[] MinerEffMult = { 1f, 1.5f, 2.2f, 3.2f, 4.5f, 6f };
        public static readonly float[] HackRewardBonus = { 0f, 0.05f, 0.1f, 0.2f, 0.35f, 0.5f };
        public static readonly float[] StealthFee = { 0.05f, 0.04f, 0.03f, 0.02f, 0f };
        public static readonly float[] StealthXp = { 0f, 0.1f, 0.2f, 0.35f, 0.5f };

        public const float PriceTickSec = 2.5f;
        public const int HistoryLen = 40;
        public const float MinerInstallBase = 120f;
        public const float MinerInstallStep = 80f;
        public const float LessonStipend = 40f;
        public const int SaveVersion = 1;
        public const string SaveFileName = "cryptohack_save.json";

        // ---- Данные контента ----
        public GameData Data = new GameData();

        // ---- Состояние игрока ----
        public float Dollars = 150f;
        public Dictionary<string, float> Crypto = new Dictionary<string, float>();
        public int Xp;
        public int Level = 1;
        public List<int> CompletedMissions = new List<int>();
        public List<Miner> Miners = new List<Miner>();
        public Dictionary<string, int> Upgrades = new Dictionary<string, int>();
        public List<int> CompletedLessons = new List<int>();
        public int TotalHacked;
        public float TotalEarnedDollars;
        public int TotalTrades;

        // ---- Настройки ----
        public bool SoundOn = true;
        public bool RealPython = true;
        public int SelectedMission = 1;

        // ---- Рынок ----
        public Dictionary<string, float> Prices = new Dictionary<string, float>();
        public Dictionary<string, List<float>> History = new Dictionary<string, List<float>>();

        public bool HasSave;

        // ---- События (замена сигналов Godot) ----
        public event Action StateChanged;
        public event Action PricesChanged;
        public event Action<string, string, string> ToastRequested;
        public event Action<int> LevelUp;
        public event Action<Mission, int> MissionCompleted;
        public event Action MinersChanged;

        public string SfxReport = "";

        float _priceTimer;
        float _mineTimer;
        readonly System.Random _rng = new System.Random();

        public string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
        }

        // ==================================================================
        //  ИНИЦИАЛИЗАЦИЯ
        // ==================================================================
        public void Init()
        {
            if (!Data.Load())
            {
                Debug.LogError("Не удалось загрузить данные игры: " + Data.LoadError);
            }
            InitState();
            InitMarket();
            LoadSave();
            if (RealPython && !PyRunner.Available)
            {
                RealPython = false;
            }
        }

        void InitState()
        {
            Crypto = new Dictionary<string, float>();
            Upgrades = new Dictionary<string, int>();
            for (int i = 0; i < Data.Cryptos.Count; i++)
            {
                Crypto[Data.Cryptos[i].Id] = 0f;
            }
            string[] upgradeIds = { "hackSpeed", "minerEff", "codeLib", "stealth" };
            for (int i = 0; i < upgradeIds.Length; i++)
            {
                Upgrades[upgradeIds[i]] = 0;
            }
        }

        public void InitMarket()
        {
            Prices = new Dictionary<string, float>();
            History = new Dictionary<string, List<float>>();
            for (int i = 0; i < Data.Cryptos.Count; i++)
            {
                CryptoInfo c = Data.Cryptos[i];
                Prices[c.Id] = c.BasePrice;
                List<float> hist = new List<float>();
                for (int k = 0; k < 30; k++)
                {
                    hist.Add(c.BasePrice * (1f + (Rand() - 0.5f) * 0.02f));
                }
                History[c.Id] = hist;
            }
        }

        float Rand()
        {
            return (float)_rng.NextDouble();
        }

        // ==================================================================
        //  ТАКТЫ (в Godot это были два Timer-а)
        // ==================================================================
        public void Tick(float dt)
        {
            _priceTimer += dt;
            if (_priceTimer >= PriceTickSec)
            {
                _priceTimer -= PriceTickSec;
                TickPrices();
            }
            _mineTimer += dt;
            if (_mineTimer >= 1f)
            {
                _mineTimer -= 1f;
                TickMiners();
            }
        }

        public void TickPrices()
        {
            float now = Time.realtimeSinceStartup;
            for (int i = 0; i < Data.Cryptos.Count; i++)
            {
                CryptoInfo c = Data.Cryptos[i];
                float drift = (Rand() - 0.5f) * 2f * c.Volatility
                    + Mathf.Sin(now / 60f + c.BasePrice) * 0.002f;
                float price = Mathf.Clamp(Prices[c.Id] * (1f + drift), c.BasePrice * 0.5f, c.BasePrice * 2f);
                Prices[c.Id] = price;
                List<float> hist = History[c.Id];
                hist.Add(price);
                while (hist.Count > HistoryLen) hist.RemoveAt(0);
            }
            if (PricesChanged != null) PricesChanged();
        }

        void TickMiners()
        {
            if (Miners.Count == 0) return;
            float eff = MinerEffMultNow();
            for (int i = 0; i < Miners.Count; i++)
            {
                Miner m = Miners[i];
                float rate;
                if (!MinerBaseRate.TryGetValue(m.Crypto, out rate)) rate = 0f;
                float gain = rate * eff;
                Crypto[m.Crypto] = GetCrypto(m.Crypto) + gain;
                m.Earned += gain;
            }
            if (StateChanged != null) StateChanged();
        }

        // ==================================================================
        //  ОПЫТ И УРОВНИ
        // ==================================================================
        public int XpForLevel(int lvl)
        {
            return lvl * 300;
        }

        public float XpProgress()
        {
            return Mathf.Clamp((float)Xp / (float)XpForLevel(Level), 0f, 1f);
        }

        public void AddXp(int amount)
        {
            float bonus = 1f + StealthXp[Mathf.Clamp(UpgradeLevel("stealth"), 0, StealthXp.Length - 1)];
            Xp += (int)Math.Round((float)amount * bonus);

            bool leveled = false;
            while (Xp >= XpForLevel(Level))
            {
                Xp -= XpForLevel(Level);
                Level++;
                leveled = true;
            }

            if (leveled)
            {
                CryptoInfo unlocked = CryptoForLevel(Level);
                string text = "Открыты новые возможности.";
                if (unlocked != null)
                {
                    text = "Разблокирована монета " + unlocked.Name + " (" + unlocked.Id + ")!";
                }
                Notify("Уровень " + Level + "!", text, "gold");
                if (LevelUp != null) LevelUp(Level);
                if (SoundOn) Sfx.LevelUp();
            }
            if (StateChanged != null) StateChanged();
            Save();
        }

        CryptoInfo CryptoForLevel(int lvl)
        {
            for (int i = 0; i < Data.Cryptos.Count; i++)
            {
                if (Data.Cryptos[i].UnlockLevel == lvl) return Data.Cryptos[i];
            }
            return null;
        }

        // ==================================================================
        //  МИССИИ
        // ==================================================================
        public bool MissionUnlocked(Mission m)
        {
            return Level >= m.RequiredLevel && UpgradeLevel("codeLib") >= m.RequiredCodeLib;
        }

        public bool IsMissionCompleted(int id)
        {
            return CompletedMissions.Contains(id);
        }

        public void HackSuccess(Mission m, int stars)
        {
            float bonus = 1f + HackRewardBonus[Mathf.Clamp(UpgradeLevel("hackSpeed"), 0, HackRewardBonus.Length - 1)];
            Crypto[m.RewardCrypto] = GetCrypto(m.RewardCrypto) + m.RewardAmount * bonus;
            Dollars += m.RewardDollars;
            TotalEarnedDollars += m.RewardDollars;
            TotalHacked++;
            if (!CompletedMissions.Contains(m.Id)) CompletedMissions.Add(m.Id);

            Notify("Взлом успешен!",
                "+" + Fmt.Crypto(m.RewardAmount * bonus) + " " + m.RewardCrypto
                + " · +" + Fmt.Dollars(m.RewardDollars) + " · +" + m.RewardXp + " XP", "ok");

            if (MissionCompleted != null) MissionCompleted(m, stars);
            AddXp(m.RewardXp);

            if (m.Id == Data.Missions.Count && Data.Missions.Count > 0)
            {
                Notify("ТЫ — ЛЕГЕНДА!", "Все цели взломаны. Дата-центр твой. Майни и богатей!", "gold");
            }
            Save();
        }

        // ==================================================================
        //  МАЙНЕРЫ
        // ==================================================================
        public float MinerInstallCost()
        {
            return MinerInstallBase + (float)Miners.Count * MinerInstallStep;
        }

        public float MinerEffMultNow()
        {
            return MinerEffMult[Mathf.Clamp(UpgradeLevel("minerEff"), 0, MinerEffMult.Length - 1)];
        }

        public bool MinersOn(int missionId)
        {
            for (int i = 0; i < Miners.Count; i++)
            {
                if (Miners[i].MissionId == missionId) return true;
            }
            return false;
        }

        public bool InstallMiner(int missionId, string cryptoId)
        {
            float cost = MinerInstallCost();
            if (Dollars < cost)
            {
                Notify("Нет денег", "Продай крипту на бирже.", "err");
                return false;
            }
            Mission m = Data.GetMission(missionId);
            if (m == null) return false;

            Dollars -= cost;
            Miner miner = new Miner();
            miner.Id = DateTime.UtcNow.Ticks.ToString();
            miner.MissionId = missionId;
            miner.PcName = m.TargetName;
            miner.Ip = m.TargetIp;
            miner.Crypto = cryptoId;
            miner.Earned = 0f;
            Miners.Add(miner);

            Notify("Майнер установлен", m.TargetName + " теперь майнит " + cryptoId, "ok");
            if (SoundOn) Sfx.BeepSquare(500f, 0.08f, 0.3f);
            if (MinersChanged != null) MinersChanged();
            if (StateChanged != null) StateChanged();
            AddXp(30);
            Save();
            return true;
        }

        public void RemoveMiner(string minerId)
        {
            List<Miner> kept = new List<Miner>();
            for (int i = 0; i < Miners.Count; i++)
            {
                if (Miners[i].Id != minerId) kept.Add(Miners[i]);
            }
            Miners = kept;
            if (MinersChanged != null) MinersChanged();
            if (StateChanged != null) StateChanged();
            Save();
        }

        public float MinerIncomePerMin()
        {
            float eff = MinerEffMultNow();
            float total = 0f;
            for (int i = 0; i < Miners.Count; i++)
            {
                string cid = Miners[i].Crypto;
                float rate;
                if (!MinerBaseRate.TryGetValue(cid, out rate)) rate = 0f;
                total += rate * eff * 60f * GetPrice(cid);
            }
            return total;
        }

        // ==================================================================
        //  БИРЖА
        // ==================================================================
        public float Fee()
        {
            return StealthFee[Mathf.Clamp(UpgradeLevel("stealth"), 0, StealthFee.Length - 1)];
        }

        public float CryptoValue(string cid)
        {
            return GetCrypto(cid) * GetPrice(cid);
        }

        public float PortfolioValue()
        {
            float total = 0f;
            for (int i = 0; i < Data.Cryptos.Count; i++)
            {
                total += CryptoValue(Data.Cryptos[i].Id);
            }
            return total;
        }

        public bool CryptoUnlocked(string cid)
        {
            CryptoInfo c = Data.GetCrypto(cid);
            return c != null && Level >= c.UnlockLevel;
        }

        public bool Trade(string cid, float usd, bool isBuy)
        {
            if (usd <= 0f || !CryptoUnlocked(cid)) return false;
            float price = GetPrice(cid);
            if (price <= 0f) return false;

            if (isBuy)
            {
                if (usd > Dollars) return false;
                float got = (usd / price) * (1f - Fee());
                Dollars -= usd;
                Crypto[cid] = GetCrypto(cid) + got;
                Notify("Покупка", "Куплено " + Fmt.Crypto(got) + " " + cid + " за " + Fmt.DollarsFull(usd), "info");
            }
            else
            {
                float need = usd / price;
                if (need > GetCrypto(cid)) return false;
                float gotUsd = usd * (1f - Fee());
                Dollars += gotUsd;
                Crypto[cid] = GetCrypto(cid) - need;
                TotalEarnedDollars += gotUsd;
                Notify("Продажа", "Продано " + Fmt.Crypto(need) + " " + cid + " → " + Fmt.DollarsFull(gotUsd), "ok");
            }

            TotalTrades++;
            if (SoundOn) Sfx.Trade();
            if (StateChanged != null) StateChanged();
            AddXp(10);
            Save();
            return true;
        }

        // ==================================================================
        //  АПГРЕЙДЫ
        // ==================================================================
        public int UpgradeLevel(string id)
        {
            int lvl;
            if (Upgrades.TryGetValue(id, out lvl)) return lvl;
            return 0;
        }

        public int UpgradeCost(string id)
        {
            UpgradeInfo u = Data.GetUpgrade(id);
            if (u == null) return -1;
            return u.CostForLevel(UpgradeLevel(id));
        }

        public bool BuyUpgrade(string id)
        {
            UpgradeInfo u = Data.GetUpgrade(id);
            if (u == null) return false;
            int lvl = UpgradeLevel(id);
            int cost = u.CostForLevel(lvl);
            if (cost < 0) return false;
            if (Dollars < (float)cost)
            {
                Notify("Не хватает долларов", "Нужно " + Fmt.DollarsFull((float)cost), "err");
                return false;
            }

            Dollars -= (float)cost;
            Upgrades[id] = lvl + 1;
            Notify("Апгрейд куплен", u.Name + " → уровень " + (lvl + 1), "gold");
            if (SoundOn) Sfx.Buy();
            if (id == "codeLib")
            {
                Notify("Новые функции!", "Открыты: " + u.EffectText(lvl + 1), "info");
            }
            if (StateChanged != null) StateChanged();
            Save();
            return true;
        }

        // ==================================================================
        //  УРОКИ
        // ==================================================================
        public bool IsLessonCompleted(int id)
        {
            return CompletedLessons.Contains(id);
        }

        public void CompleteLesson(int id)
        {
            if (CompletedLessons.Contains(id)) return;
            Lesson l = Data.GetLesson(id);
            if (l == null) return;

            CompletedLessons.Add(id);
            Dollars += LessonStipend;
            Notify("Урок пройден!", "+" + l.Xp + " XP · +$" + (int)LessonStipend + " стипендия", "ok");
            if (SoundOn) Sfx.UiOk();
            AddXp(l.Xp);
            if (StateChanged != null) StateChanged();
            Save();
        }

        // ==================================================================
        //  ВСПОМОГАТЕЛЬНОЕ
        // ==================================================================
        public void Notify(string title, string text, string kind)
        {
            if (ToastRequested != null) ToastRequested(title, text, kind);
        }

        public void Notify(string title, string text)
        {
            Notify(title, text, "info");
        }

        public float GetCrypto(string id)
        {
            float v;
            if (Crypto.TryGetValue(id, out v)) return v;
            return 0f;
        }

        public float GetPrice(string id)
        {
            float v;
            if (Prices.TryGetValue(id, out v)) return v;
            return 0f;
        }

        public string PythonModeText()
        {
            if (RealPython && PyRunner.Available)
            {
                return "РЕАЛЬНЫЙ PYTHON (" + PyRunner.Describe() + ")";
            }
            return "СИМУЛЯТОР ТЕРМИНАЛА";
        }

        // ==================================================================
        //  СОХРАНЕНИЕ (в Godot был user://cryptohack_save_v1.json)
        // ==================================================================
        public void Save()
        {
            try
            {
                SaveDto dto = new SaveDto();
                dto.version = SaveVersion;
                dto.dollars = Dollars;
                dto.xp = Xp;
                dto.level = Level;
                dto.totalHacked = TotalHacked;
                dto.totalEarnedDollars = TotalEarnedDollars;
                dto.totalTrades = TotalTrades;
                dto.soundOn = SoundOn;
                dto.realPython = RealPython;
                dto.selectedMission = SelectedMission;

                dto.cryptoIds = new string[Crypto.Count];
                dto.cryptoAmounts = new float[Crypto.Count];
                int ci = 0;
                foreach (KeyValuePair<string, float> kv in Crypto)
                {
                    dto.cryptoIds[ci] = kv.Key;
                    dto.cryptoAmounts[ci] = kv.Value;
                    ci++;
                }

                dto.upgradeIds = new string[Upgrades.Count];
                dto.upgradeLevels = new int[Upgrades.Count];
                int ui = 0;
                foreach (KeyValuePair<string, int> kv in Upgrades)
                {
                    dto.upgradeIds[ui] = kv.Key;
                    dto.upgradeLevels[ui] = kv.Value;
                    ui++;
                }

                dto.completedMissions = CompletedMissions.ToArray();
                dto.completedLessons = CompletedLessons.ToArray();

                dto.minerIds = new string[Miners.Count];
                dto.minerMissionIds = new int[Miners.Count];
                dto.minerPcNames = new string[Miners.Count];
                dto.minerIps = new string[Miners.Count];
                dto.minerCryptos = new string[Miners.Count];
                dto.minerEarned = new float[Miners.Count];
                for (int i = 0; i < Miners.Count; i++)
                {
                    dto.minerIds[i] = Miners[i].Id;
                    dto.minerMissionIds[i] = Miners[i].MissionId;
                    dto.minerPcNames[i] = Miners[i].PcName;
                    dto.minerIps[i] = Miners[i].Ip;
                    dto.minerCryptos[i] = Miners[i].Crypto;
                    dto.minerEarned[i] = Miners[i].Earned;
                }

                File.WriteAllText(SavePath, JsonUtility.ToJson(dto, true));
                HasSave = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Не удалось сохранить игру: " + e.Message);
            }
        }

        public bool LoadSave()
        {
            try
            {
                if (!File.Exists(SavePath)) return false;
                string text = File.ReadAllText(SavePath);
                SaveDto dto = JsonUtility.FromJson<SaveDto>(text);
                if (dto == null) return false;

                Dollars = dto.dollars;
                Xp = dto.xp;
                Level = Mathf.Max(1, dto.level);
                SoundOn = dto.soundOn;
                RealPython = dto.realPython;
                SelectedMission = dto.selectedMission;
                TotalHacked = dto.totalHacked;
                TotalEarnedDollars = dto.totalEarnedDollars;
                TotalTrades = dto.totalTrades;

                if (dto.cryptoIds != null && dto.cryptoAmounts != null)
                {
                    for (int i = 0; i < dto.cryptoIds.Length && i < dto.cryptoAmounts.Length; i++)
                    {
                        if (Crypto.ContainsKey(dto.cryptoIds[i])) Crypto[dto.cryptoIds[i]] = dto.cryptoAmounts[i];
                    }
                }
                if (dto.upgradeIds != null && dto.upgradeLevels != null)
                {
                    for (int i = 0; i < dto.upgradeIds.Length && i < dto.upgradeLevels.Length; i++)
                    {
                        if (Upgrades.ContainsKey(dto.upgradeIds[i])) Upgrades[dto.upgradeIds[i]] = dto.upgradeLevels[i];
                    }
                }

                CompletedMissions.Clear();
                if (dto.completedMissions != null)
                {
                    for (int i = 0; i < dto.completedMissions.Length; i++) CompletedMissions.Add(dto.completedMissions[i]);
                }
                CompletedLessons.Clear();
                if (dto.completedLessons != null)
                {
                    for (int i = 0; i < dto.completedLessons.Length; i++) CompletedLessons.Add(dto.completedLessons[i]);
                }

                Miners.Clear();
                if (dto.minerIds != null && dto.minerMissionIds != null)
                {
                    for (int i = 0; i < dto.minerIds.Length; i++)
                    {
                        Miner m = new Miner();
                        m.Id = dto.minerIds[i];
                        m.MissionId = dto.minerMissionIds[i];
                        if (dto.minerPcNames != null && i < dto.minerPcNames.Length) m.PcName = dto.minerPcNames[i];
                        if (dto.minerIps != null && i < dto.minerIps.Length) m.Ip = dto.minerIps[i];
                        if (dto.minerCryptos != null && i < dto.minerCryptos.Length) m.Crypto = dto.minerCryptos[i];
                        if (dto.minerEarned != null && i < dto.minerEarned.Length) m.Earned = dto.minerEarned[i];
                        Miners.Add(m);
                    }
                }

                HasSave = true;
                if (StateChanged != null) StateChanged();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Не удалось загрузить сохранение: " + e.Message);
                return false;
            }
        }

        public void ResetProgress()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Не удалось удалить сохранение: " + e.Message);
            }

            Dollars = 150f;
            InitState();
            Xp = 0;
            Level = 1;
            CompletedMissions.Clear();
            CompletedLessons.Clear();
            Miners.Clear();
            TotalHacked = 0;
            TotalEarnedDollars = 0f;
            TotalTrades = 0;
            SelectedMission = 1;
            HasSave = false;
            InitMarket();
            if (StateChanged != null) StateChanged();
            if (PricesChanged != null) PricesChanged();
        }

        [Serializable]
        public class SaveDto
        {
            public int version;
            public float dollars;
            public string[] cryptoIds;
            public float[] cryptoAmounts;
            public int xp;
            public int level;
            public int[] completedMissions;
            public string[] minerIds;
            public int[] minerMissionIds;
            public string[] minerPcNames;
            public string[] minerIps;
            public string[] minerCryptos;
            public float[] minerEarned;
            public string[] upgradeIds;
            public int[] upgradeLevels;
            public int[] completedLessons;
            public int totalHacked;
            public float totalEarnedDollars;
            public int totalTrades;
            public bool soundOn;
            public bool realPython;
            public int selectedMission;
        }
    }
}
