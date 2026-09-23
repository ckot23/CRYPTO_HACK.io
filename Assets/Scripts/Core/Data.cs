using System;
using UnityEngine;

namespace CryptoHack
{
    // ---------------------------------------------------------------------
    //  Структуры для JsonUtility.
    //
    //  В Godot-версии данные читались через JSON.parse_string() в словари.
    //  Unity умеет разбирать JSON только в классы через JsonUtility, поэтому
    //  здесь описаны DTO: имена полей ОБЯЗАНЫ совпадать с ключами в
    //  Assets/Resources/gamedata.json (регистр важен!). Отсюда camelCase.
    // ---------------------------------------------------------------------

    [Serializable]
    public class CryptoDto
    {
        public string id;
        public string name;
        public string fullName;
        public string icon;
        public string color;
        public int basePrice;
        public int unlockLevel;
        public string desc;
        public float volatility;
    }

    [Serializable]
    public class MissionDto
    {
        public int id;
        public string title;
        public string targetName;
        public string targetIp;
        public string os;
        public int security;
        public string difficulty;
        public string concept;
        public string conceptDesc;
        public string briefing;
        public string task;
        public string starterCode;
        public string solution;
        public string[] hints;
        public string[] requiredPatterns;
        public string rewardCrypto;
        public float rewardAmount;
        public int rewardDollars;
        public int rewardXp;
        public int requiredCodeLib;
        public int requiredLevel;
        public string[] theory;
    }

    [Serializable]
    public class UpgradeDto
    {
        public string id;
        public string name;
        public string desc;
        public string icon;
        public int maxLevel;
        public int[] costs;
        public string[] effects;
    }

    [Serializable]
    public class LessonBlockDto
    {
        public string heading;
        public string text;
    }

    [Serializable]
    public class QuizItemDto
    {
        public string q;
        public string[] options;
        public int answer;
    }

    [Serializable]
    public class LessonDto
    {
        public int id;
        public string title;
        public string subtitle;
        public string duration;
        public int xp;
        public LessonBlockDto[] content;
        public QuizItemDto[] quiz;
    }

    [Serializable]
    public class GameDataDto
    {
        public int format_version;
        public string source;
        public CryptoDto[] cryptos;
        public MissionDto[] missions;
        public UpgradeDto[] upgrades;
        public LessonDto[] lessons;
        public string[] quotes;
    }
}
