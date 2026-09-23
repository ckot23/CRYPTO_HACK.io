using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CryptoHack
{
    public class LogLine
    {
        public readonly string Text;
        public readonly string Kind;
        public readonly int Delay;

        public LogLine(string text, string kind, int delay)
        {
            Text = text;
            Kind = kind;
            Delay = delay;
        }
    }

    public class SimResult
    {
        public bool Success;
        public List<LogLine> Logs = new List<LogLine>();
        public List<string> Missing = new List<string>();
        public int StyleScore;
    }

    public class SyntaxProblem
    {
        public int Line;
        public string Msg = "";
    }

    /// <summary>
    /// Симулятор выполнения Python-скрипта — перенос scripts/core/py_sim.gd
    /// (а тот, в свою очередь, — порт src/engine.ts из веб-версии).
    ///
    /// Нужен, когда настоящего интерпретатора нет: миссия оценивается по
    /// requiredPatterns, а в консоль выводится «хакерский» лог.
    /// </summary>
    public static class PySim
    {
        public const string KindCmd = "cmd";
        public const string KindOk = "ok";
        public const string KindInfo = "info";
        public const string KindWarn = "warn";
        public const string KindErr = "err";

        const int MaxBruteTries = 5;

        static bool Search(string pattern, string text)
        {
            try
            {
                return Regex.IsMatch(text, pattern);
            }
            catch (Exception)
            {
                Debug.LogWarning("Некорректная регулярка в данных миссии: " + pattern);
                return false;
            }
        }

        static Match Find(string pattern, string text)
        {
            try
            {
                return Regex.Match(text, pattern);
            }
            catch (Exception)
            {
                Debug.LogWarning("Некорректная регулярка в данных миссии: " + pattern);
                return Match.Empty;
            }
        }

        /// <summary>Строки без комментариев — по ним проверяются требования миссии.</summary>
        public static List<string> MeaningfulLines(string code)
        {
            List<string> result = new List<string>();
            string[] lines = code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (t.Length > 0 && !t.StartsWith("#"))
                {
                    result.Add(lines[i]);
                }
            }
            return result;
        }

        /// <summary>Каких требований миссии не хватает. Пустой список = выполнено.</summary>
        public static List<string> CheckPatterns(string code, Mission mission)
        {
            List<string> missing = new List<string>();
            string body = string.Join("\n", MeaningfulLines(code).ToArray());
            for (int idx = 0; idx < mission.RequiredPatterns.Length; idx++)
            {
                string pattern = mission.RequiredPatterns[idx];
                if (!Search(pattern, body))
                {
                    if (idx < mission.Hints.Length) missing.Add(mission.Hints[idx]);
                    else missing.Add("Требование " + (idx + 1) + " не выполнено");
                }
            }
            return missing;
        }

        /// <summary>Простая проверка синтаксиса. null, если всё чисто.</summary>
        public static SyntaxProblem CheckSyntax(string code)
        {
            string[] lines = code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                if (trimmed.Length == 0) continue;

                // for/if/while/def/else без двоеточия
                if (Search("^(for\\b|if\\b|while\\b|def\\b|else\\b)", trimmed)
                    && !trimmed.EndsWith(":") && trimmed.IndexOf('#') < 0)
                {
                    if (trimmed.IndexOf(':') < 0)
                    {
                        SyntaxProblem p = new SyntaxProblem();
                        p.Line = i + 1;
                        string head = trimmed.Length > 30 ? trimmed.Substring(0, 30) : trimmed;
                        p.Msg = "ожидалось ':' в конце строки («" + head + "...»)";
                        return p;
                    }
                }

                // незакрытые кавычки
                if (Count(line, '"') % 2 != 0 || Count(line, '\'') % 2 != 0)
                {
                    SyntaxProblem p = new SyntaxProblem();
                    p.Line = i + 1;
                    p.Msg = "незакрытая кавычка — проверь строки";
                    return p;
                }

                // несогласованные скобки
                if (Count(line, '(') != Count(line, ')'))
                {
                    SyntaxProblem p = new SyntaxProblem();
                    p.Line = i + 1;
                    p.Msg = "несогласованные скобки ( и )";
                    return p;
                }

                // после строки с «:» должен быть отступ
                if (i > 0)
                {
                    string prev = lines[i - 1].Trim();
                    if (prev.EndsWith(":") && line.Length > 0
                        && !line.StartsWith(" ") && !line.StartsWith("\t"))
                    {
                        SyntaxProblem p = new SyntaxProblem();
                        p.Line = i + 1;
                        p.Msg = "ожидался отступ после «:» (4 пробела)";
                        return p;
                    }
                }
            }
            return null;
        }

        static int Count(string s, char c)
        {
            int n = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == c) n++;
            }
            return n;
        }

        /// <summary>Оценка «стиля» кода — 1..3 звезды.</summary>
        public static int StyleScore(string code, Mission mission, List<string> missing)
        {
            int stars = 2;
            if (code.IndexOf('#') >= 0) stars = 3;
            List<string> bodyLines = MeaningfulLines(code);
            if (bodyLines.Count <= 6 && missing.Count == 0) stars = Mathf.Max(stars, 2);
            if (code.IndexOf("print", StringComparison.Ordinal) >= 0 && mission.Id > 2) stars = 3;
            return stars;
        }

        /// <summary>Главная функция: прогон кода в режиме симулятора.</summary>
        public static SimResult Simulate(string code, Mission mission, int hackSpeedLevel)
        {
            SimResult res = new SimResult();
            string body = string.Join("\n", MeaningfulLines(code).ToArray());
            List<string> missing = CheckPatterns(code, mission);

            if (body.Trim().Length == 0)
            {
                res.Logs.Add(new LogLine("! Пустой скрипт. Напиши код и попробуй снова.", KindWarn, 200));
                res.Missing.Add("Напиши код в редакторе");
                res.StyleScore = 0;
                return res;
            }

            SyntaxProblem syntax = CheckSyntax(body);
            if (syntax != null)
            {
                res.Logs.Add(new LogLine("$ python3 exploit.py --target " + mission.TargetIp, KindCmd, 250));
                res.Logs.Add(new LogLine("  File \"exploit.py\", line " + syntax.Line, KindErr, 350));
                res.Logs.Add(new LogLine("SyntaxError: " + syntax.Msg, KindErr, 300));
                res.Logs.Add(new LogLine("Подсказка: проверь двоеточия, отступы и кавычки.", KindInfo, 200));
                res.Missing.Add(syntax.Msg);
                res.StyleScore = 0;
                return res;
            }

            float speedDiv = 1f + (float)hackSpeedLevel * 0.35f;
            int baseDelay400 = D(400, speedDiv);
            int baseDelay450 = D(450, speedDiv);

            res.Logs.Add(new LogLine("$ python3 exploit.py --target " + mission.TargetIp, KindCmd, baseDelay400));
            res.Logs.Add(new LogLine("[*] Инициализация NeonHack Framework v3.7...", KindInfo, baseDelay450));

            if (Search("scan\\s*\\(", body))
            {
                res.Logs.Add(new LogLine("[SCAN] Сканирование подсети 192.168.0.0/24 ...", KindInfo, D(600, speedDiv)));
                res.Logs.Add(new LogLine("[SCAN] Найдено 4 узла. Цель: " + mission.TargetIp + " (" + mission.Os + ") OK",
                    KindOk, D(550, speedDiv)));
            }

            if (Search("print\\s*\\(", body))
            {
                if (Search("print\\s*\\(\\s*(target|ip|wallets|key)\\s*\\)", body))
                {
                    res.Logs.Add(new LogLine("[OUT] " + mission.TargetIp + " :: " + mission.TargetName,
                        KindInfo, D(350, speedDiv)));
                }
                else
                {
                    Match m = Find("print\\s*\\(\\s*([\"']?)(.*?)\\1\\s*\\)", body);
                    string value = "...";
                    if (m.Success && m.Groups.Count > 2)
                    {
                        string g = m.Groups[2].Value;
                        value = g.Length > 60 ? g.Substring(0, 60) : g;
                    }
                    res.Logs.Add(new LogLine("[OUT] " + value, KindInfo, D(350, speedDiv)));
                }
            }

            if (Search("\\bconnect\\s*\\(", body))
            {
                res.Logs.Add(new LogLine("[NET] Подключение к " + mission.TargetIp + ":22 ...", KindInfo, D(600, speedDiv)));
                res.Logs.Add(new LogLine("[NET] Туннель установлен. Обход firewall... OK", KindOk, D(550, speedDiv)));
            }

            if (Search("\\bbrute\\s*\\(", body))
            {
                int tries = 5;
                Match m = Find("range\\s*\\(\\s*(\\d+)", body);
                if (m.Success && m.Groups.Count > 1)
                {
                    int parsed;
                    if (int.TryParse(m.Groups[1].Value, out parsed)) tries = Mathf.Min(parsed, 12);
                }
                res.Logs.Add(new LogLine("[BRUTE] Перебор " + tries + " комбинаций...", KindInfo, D(500, speedDiv)));
                int attempts = Mathf.Min(tries, MaxBruteTries);
                for (int i = 0; i < attempts; i++)
                {
                    res.Logs.Add(new LogLine("  попытка " + i + " ... неверно", KindInfo, D(220, speedDiv)));
                }
                if (Search("for\\s+", body))
                {
                    res.Logs.Add(new LogLine("[BRUTE] Пароль подобран! Цикл for сработал идеально", KindOk, D(500, speedDiv)));
                }
            }

            if (Search("\\bif\\s+", body))
            {
                res.Logs.Add(new LogLine("[CHECK] Проверка условия...", KindInfo, D(400, speedDiv)));
                res.Logs.Add(new LogLine("[CHECK] Условие True -> выполняю блок if", KindOk, D(400, speedDiv)));
            }

            if (Search("else\\s*:", body))
            {
                res.Logs.Add(new LogLine("[CHECK] Ветка else готова (защита от ошибок)", KindInfo, D(250, speedDiv)));
            }

            if (Search("\\bwallets\\s*=\\s*\\[", body))
            {
                res.Logs.Add(new LogLine("[DATA] Список кошельков загружен: 3 адреса", KindInfo, D(400, speedDiv)));
                res.Logs.Add(new LogLine("  |- 0xA1 ... 0xB2 ... 0xC3", KindInfo, D(300, speedDiv)));
            }

            if (Search("\\bdrain\\s*\\(", body))
            {
                res.Logs.Add(new LogLine("[DRAIN] Извлечение средств из кошельков...", KindInfo, D(600, speedDiv)));
                res.Logs.Add(new LogLine("[DRAIN] 0xA1 drained | 0xB2 drained | 0xC3 drained", KindOk, D(600, speedDiv)));
            }

            if (Search("def\\s+\\w+", body))
            {
                string fnName = "";
                Match m = Find("def\\s+(\\w+)", body);
                if (m.Success && m.Groups.Count > 1) fnName = m.Groups[1].Value;
                res.Logs.Add(new LogLine("[FUNC] Функция " + fnName + "() скомпилирована", KindOk, D(400, speedDiv)));
            }

            if (Search("\\bbypass\\s*\\(", body))
            {
                res.Logs.Add(new LogLine("[EVADE] Антивирус обойдён, EDR ослеплён", KindOk, D(500, speedDiv)));
            }
            if (Search("\\bdecrypt\\s*\\(", body))
            {
                res.Logs.Add(new LogLine("[CRYPT] AES-256 ключи восстановлены, сид-фраза расшифрована", KindOk, D(650, speedDiv)));
            }
            if (Search("\\bextract\\s*\\(", body))
            {
                res.Logs.Add(new LogLine("[EXTRACT] Приватные ключи извлечены", KindOk, D(550, speedDiv)));
            }
            if (Search("while\\s+", body))
            {
                res.Logs.Add(new LogLine("[LOOP] while-цикл запущен, флаг mining = True", KindInfo, D(400, speedDiv)));
            }
            if (Search("install_miner\\s*\\(", body))
            {
                res.Logs.Add(new LogLine("[MINER] Загрузка xmrig-neon... компиляция...", KindInfo, D(600, speedDiv)));
                res.Logs.Add(new LogLine("[MINER] Майнер внедрён в автозагрузку, скрыт от диспетчера", KindOk, D(600, speedDiv)));
            }

            if (missing.Count > 0)
            {
                res.Logs.Add(new LogLine("[!] Эксплойт завершён с ошибками.", KindWarn, D(350, speedDiv)));
                res.Logs.Add(new LogLine("[!] Цель не взломана. Не хватает шагов: " + missing.Count, KindErr, D(300, speedDiv)));
                res.Logs.Add(new LogLine("Открой вкладку «Подсказки» — там разжёвано по шагам.", KindInfo, D(200, speedDiv)));
                res.Missing = missing;
                res.StyleScore = 1;
                return res;
            }

            res.Logs.Add(new LogLine("[ROOT] Доступ ROOT получен! Заметаю следы...", KindOk, D(550, speedDiv)));
            res.Logs.Add(new LogLine("[WALLET] Перевод " + Fmt.Crypto(mission.RewardAmount) + " "
                + mission.RewardCrypto + " на твой кошелёк...", KindOk, D(700, speedDiv)));
            res.Logs.Add(new LogLine("[OK] ВЗЛОМ ЗАВЕРШЁН. Ты — машина.", KindOk, D(400, speedDiv)));

            res.Success = true;
            res.Missing = new List<string>();
            res.StyleScore = StyleScore(code, mission, missing);
            return res;
        }

        static int D(int ms, float speedDiv)
        {
            return Mathf.Max(120, (int)Math.Round((float)ms / speedDiv));
        }
    }
}
