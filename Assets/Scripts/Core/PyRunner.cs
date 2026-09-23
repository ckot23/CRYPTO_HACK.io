using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CryptoHack
{
    public class PyRunResult
    {
        public bool Ok;
        public List<string> Lines = new List<string>();
        public int ExitCode;
        public bool TimedOut;
        public string Error = "";
    }

    /// <summary>
    /// Запуск настоящего Python — перенос scripts/core/py_runner.gd.
    ///
    /// В веб-версии код студента вообще не выполнялся: engine.ts подделывал
    /// вывод. Здесь запускается реальный интерпретатор, если он установлен:
    ///   1) код пишется в &lt;persistentDataPath&gt;/py_run/exploit.py
    ///   2) рядом кладётся обёртка с заглушками scan/connect/brute/...
    ///   3) процесс запускается и ждёт завершения в фоновом потоке (главный
    ///      поток не морозится — Unity не любит блокировки)
    ///   4) вывод читается построчно
    /// Если Python нет или что-то пошло не так — игра молча возвращается к
    /// симулятору PySim.
    /// </summary>
    public static class PyRunner
    {
        public const string RunnerFile = "neon_runner.py";
        public const string ScriptFile = "exploit.py";
        public const string ExitMark = "@@EXIT:";
        public const int DefaultTimeoutMs = 8000;

        public static bool Available { get; private set; }
        public static bool Running { get; private set; }
        public static PyRunResult Result { get; private set; }

        static string _cmd = "";
        static string _preArgs = "";
        static string _version = "";
        static bool _checked;
        static Thread _thread;

        public static string WorkDir
        {
            get { return Path.Combine(Application.persistentDataPath, "py_run"); }
        }

        /// <summary>Python-обёртка: заглушки хакерских функций + запуск кода студента.</summary>
        public const string RunnerSrc = @"# NeonHack runner — генерируется игрой CRYPTO_HACK (автоматически)
import sys, traceback

try:
    sys.stdout.reconfigure(encoding=""utf-8"")
except Exception:
    pass

TARGET_IP = ""@@TARGET_IP@@""
TARGET_NAME = ""@@TARGET_NAME@@"" 
WALLETS = [""0xA1"", ""0xB2"", ""0xC3""]
KEY = ""neon-77""
MINING = True
MINER_INSTALLED = False


def _out(text):
    print(text, flush=True)


def scan():
    _out(""[SCAN] Сканирование подсети 192.168.0.0/24 ..."")
    _out(""[SCAN] Найдено 4 узла. Цель: %s (%s)"" % (TARGET_IP, TARGET_NAME))
    return TARGET_IP


def connect(ip=""127.0.0.1""):
    _out(""[NET] Подключение к %s:22 ..."" % ip)
    _out(""[NET] Туннель установлен. Обход firewall... OK"")
    return True


def brute(pin=None):
    _out(""  попытка %s ... неверно"" % (pin,))
    if pin == 3:
        _out(""[BRUTE] Пароль подобран: 3"")
    return pin == 3


def bypass():
    _out(""[EVADE] Антивирус обойдён, EDR ослеплён"")
    return True


def decrypt():
    _out(""[CRYPT] AES-256 ключи восстановлены, сид-фраза расшифрована"")
    return KEY


def extract():
    _out(""[EXTRACT] Приватные ключи извлечены"")
    return True


def drain(wallet=None):
    _out(""[DRAIN] %s drained"" % (wallet,))
    return True


def install_miner():
    global MINER_INSTALLED
    MINER_INSTALLED = True
    _out(""[MINER] Майнер внедрён в автозагрузку, скрыт от диспетчера"")
    return True


def wallets():
    return list(WALLETS)


def main(path):
    try:
        src = open(path, ""r"", encoding=""utf-8"").read()
    except OSError as e:
        _out(""Ошибка чтения скрипта: %s"" % e)
        return 2

    env = dict(globals())
    try:
        code = compile(src, ""exploit.py"", ""exec"")
    except SyntaxError as e:
        _out('  File ""exploit.py"", line %s' % (e.lineno or 0))
        _out(""SyntaxError: %s"" % (e.msg,))
        _out(""Подсказка: проверь двоеточия, отступы и кавычки."")
        return 1

    try:
        exec(code, env)
    except Exception as exc:
        tb = traceback.extract_tb(sys.exc_info()[2])
        frame = tb[-1] if tb else None
        if frame is not None and frame.filename == ""exploit.py"":
            _out('  File ""exploit.py"", line %d, in %s' % (frame.lineno, frame.name))
            _out(""%s: %s"" % (type(exc).__name__, exc))
        else:
            traceback.print_exc()
        _out(""Скрипт завершился с ошибкой — цель не взломана."")
        return 1

    _out(""[OK] Скрипт выполнен без ошибок."")
    return 0


if __name__ == ""__main__"":
    rc = main(sys.argv[1])
    print(""@@EXIT:%d"" % rc, flush=True)
    sys.exit(rc)
";

        /// <summary>Поиск интерпретатора (один раз при старте, быстро).</summary>
        public static void Init()
        {
            if (_checked) return;
            _checked = true;

            List<string[]> candidates = new List<string[]>();
            if (Application.platform == RuntimePlatform.WindowsPlayer
                || Application.platform == RuntimePlatform.WindowsEditor)
            {
                candidates.Add(new string[] { "py", "-3" });
                candidates.Add(new string[] { "python", "" });
                candidates.Add(new string[] { "python3", "" });
            }
            else
            {
                candidates.Add(new string[] { "python3", "" });
                candidates.Add(new string[] { "python", "" });
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                string cmd = candidates[i][0];
                string pre = candidates[i][1];
                string probe = "-X utf8 -c \"print('PYOK')\"";
                if (!string.IsNullOrEmpty(pre)) probe = pre + " " + probe;

                string outText = TryRun(cmd, probe, 3000, out _);
                if (outText != null && outText.IndexOf("PYOK", StringComparison.Ordinal) >= 0)
                {
                    _cmd = cmd;
                    _preArgs = pre;
                    Available = true;
                    _version = QueryVersion(cmd, pre);
                    Debug.Log("PyRunner: найден " + Describe());
                    return;
                }
            }

            Available = false;
            Debug.Log("PyRunner: Python не найден — работает симулятор терминала");
        }

        static string QueryVersion(string cmd, string pre)
        {
            string args = "-X utf8 -c \"import sys;print(sys.version.split()[0])\"";
            if (!string.IsNullOrEmpty(pre)) args = pre + " " + args;
            string outText = TryRun(cmd, args, 3000, out _);
            if (outText == null) return "";
            return outText.Trim();
        }

        public static string Describe()
        {
            if (!Available) return "не найден";
            string label = _cmd;
            if (!string.IsNullOrEmpty(_preArgs)) label += " " + _preArgs;
            if (!string.IsNullOrEmpty(_version)) label += ", Python " + _version;
            return label;
        }

        /// <summary>Запуск кода в фоне. Результат читается через Running/Result.</summary>
        public static void Begin(string code, Mission mission, int timeoutMs)
        {
            if (!Available || Running) return;
            Running = true;
            Result = null;

            string codeCopy = code;
            Mission missionCopy = mission;
            int timeout = timeoutMs <= 0 ? DefaultTimeoutMs : timeoutMs;

            _thread = new Thread(delegate ()
            {
                PyRunResult res;
                try
                {
                    res = RunProcess(codeCopy, missionCopy, timeout);
                }
                catch (Exception e)
                {
                    res = new PyRunResult();
                    res.Error = e.GetType().Name + ": " + e.Message;
                }
                Result = res;
                Running = false;
            });
            _thread.IsBackground = true;
            _thread.Start();
        }

        static PyRunResult RunProcess(string code, Mission mission, int timeoutMs)
        {
            PyRunResult res = new PyRunResult();

            Directory.CreateDirectory(WorkDir);
            string scriptPath = Path.Combine(WorkDir, ScriptFile);
            string runnerPath = Path.Combine(WorkDir, RunnerFile);

            string runner = RunnerSrc
                .Replace("@@TARGET_IP@@", mission == null ? "127.0.0.1" : mission.TargetIp)
                .Replace("@@TARGET_NAME@@", mission == null ? "target" : mission.TargetName);

            File.WriteAllText(runnerPath, runner, new UTF8Encoding(false));
            File.WriteAllText(scriptPath, code, new UTF8Encoding(false));

            string args = "-X utf8 \"" + runnerPath + "\" \"" + scriptPath + "\"";
            if (!string.IsNullOrEmpty(_preArgs)) args = _preArgs + " " + args;

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = _cmd;
            psi.Arguments = args;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.WorkingDirectory = WorkDir;

            Process proc = new Process();
            proc.StartInfo = psi;
            proc.Start();

            if (!proc.WaitForExit(timeoutMs))
            {
                try { proc.Kill(); } catch (Exception) { }
                res.TimedOut = true;
            }

            string stdout = "";
            string stderr = "";
            try { stdout = proc.StandardOutput.ReadToEnd(); } catch (Exception) { }
            try { stderr = proc.StandardError.ReadToEnd(); } catch (Exception) { }
            try { res.ExitCode = proc.ExitCode; } catch (Exception) { res.ExitCode = -1; }
            try { proc.Close(); } catch (Exception) { }

            res.Lines = SplitLines(stdout);
            if (res.Lines.Count == 0 && !string.IsNullOrEmpty(stderr))
            {
                res.Lines = SplitLines(stderr);
            }

            // последняя строка вида "@@EXIT:n" — это код возврата, в консоль её не показываем
            int exitFromMark = int.MinValue;
            for (int i = res.Lines.Count - 1; i >= 0; i--)
            {
                if (res.Lines[i].StartsWith(ExitMark, StringComparison.Ordinal))
                {
                    int parsed;
                    if (int.TryParse(res.Lines[i].Substring(ExitMark.Length).Trim(), out parsed))
                    {
                        exitFromMark = parsed;
                    }
                    res.Lines.RemoveAt(i);
                }
            }
            if (exitFromMark != int.MinValue) res.ExitCode = exitFromMark;

            if (res.TimedOut)
            {
                res.Lines.Add("[!] Процесс убит по таймауту (" + (timeoutMs / 1000) + " с). Похоже на бесконечный цикл.");
                res.Error = "timeout";
            }
            res.Ok = !res.TimedOut && res.ExitCode == 0;
            return res;
        }

        static List<string> SplitLines(string text)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            string[] parts = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < parts.Length; i++)
            {
                string t = parts[i];
                if (t.Length > 0) lines.Add(t);
            }
            return lines;
        }

        static string TryRun(string cmd, string args, int timeoutMs, out int exitCode)
        {
            exitCode = -1;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = cmd;
                psi.Arguments = args;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                Process proc = new Process();
                proc.StartInfo = psi;
                proc.Start();
                if (!proc.WaitForExit(timeoutMs))
                {
                    try { proc.Kill(); } catch (Exception) { }
                    return null;
                }
                string stdout = "";
                try { stdout = proc.StandardOutput.ReadToEnd(); } catch (Exception) { }
                try { proc.StandardError.ReadToEnd(); } catch (Exception) { }
                exitCode = proc.ExitCode;
                try { proc.Close(); } catch (Exception) { }
                return stdout;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
