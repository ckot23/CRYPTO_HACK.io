class_name PyRunner
extends RefCounted

## Запуск настоящего Python-интерпретатора.
##
## В веб-версии код студента никогда не выполнялся: src/engine.ts просто
## подделывал вывод терминала. В Godot-порте можно запускать реальный Python:
##   1) код пишется в файл в user://py_run/exploit.py
##   2) рядом кладётся runner с заглушками хакерских функций (scan, connect, ...)
##   3) процесс запускается через sh/cmd с перенаправлением вывода в файл
##   4) вывод построчно читается и показывается в консоли игры
##
## Если интерпретатор не найден или запуск не удался — игра автоматически
## возвращается к симулятору PySim.

const WORK_DIR := "user://py_run"
const RUNNER_FILE := "neon_runner.py"
const SCRIPT_FILE := "exploit.py"
const OUT_FILE := "output.txt"
const EXIT_MARK := "@@EXIT:"
const DEFAULT_TIMEOUT_MS := 5000

static var _cached_cmd: PackedStringArray = PackedStringArray()
static var _detected: bool = false

## Python-обёртка: определяет заглушки и выполняет код студента так, чтобы
## номера строк в ошибках соответствовали файлу exploit.py.
const RUNNER_SRC := """# NeonHack runner — генерируется игрой CRYPTO_HACK (автоматически)
import sys, traceback

try:
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

TARGET_IP = "@@TARGET_IP@@"
TARGET_NAME = "@@TARGET_NAME@@"
WALLETS = ["0xA1", "0xB2", "0xC3"]
KEY = "neon-77"
MINING = True
MINER_INSTALLED = False


def _out(text):
    print(text, flush=True)


def scan():
    _out("[SCAN] Сканирование подсети 192.168.0.0/24 ...")
    _out("[SCAN] Найдено 4 узла. Цель: %s (%s)" % (TARGET_IP, TARGET_NAME))
    return TARGET_IP


def connect(ip="127.0.0.1"):
    _out("[NET] Подключение к %s:22 ..." % ip)
    _out("[NET] Туннель установлен. Обход firewall... OK")
    return True


def brute(pin=None):
    _out("  попытка %s ... неверно" % (pin,))
    if pin == 3:
        _out("[BRUTE] Пароль подобран: 3")
    return pin == 3


def bypass():
    _out("[EVADE] Антивирус обойдён, EDR ослеплён")
    return True


def decrypt():
    _out("[CRYPT] AES-256 ключи восстановлены, сид-фраза расшифрована")
    return KEY


def extract():
    _out("[EXTRACT] Приватные ключи извлечены")
    return True


def drain(wallet=None):
    _out("[DRAIN] %s drained" % (wallet,))
    return True


def install_miner():
    global MINER_INSTALLED
    MINER_INSTALLED = True
    _out("[MINER] Майнер внедрён в автозагрузку, скрыт от диспетчера")
    return True


def wallets():
    return list(WALLETS)


def main(path):
    try:
        src = open(path, "r", encoding="utf-8").read()
    except OSError as e:
        _out("Ошибка чтения скрипта: %s" % e)
        return 2

    env = dict(globals())
    try:
        code = compile(src, "exploit.py", "exec")
    except SyntaxError as e:
        _out('  File "exploit.py", line %s' % (e.lineno or 0))
        _out("SyntaxError: %s" % (e.msg,))
        _out("Подсказка: проверь двоеточия, отступы и кавычки.")
        return 1

    try:
        exec(code, env)
    except Exception as exc:
        tb = traceback.extract_tb(sys.exc_info()[2])
        frame = tb[-1] if tb else None
        if frame is not None and frame.filename == "exploit.py":
            _out('  File "exploit.py", line %d, in %s' % (frame.lineno, frame.name))
            _out("%s: %s" % (type(exc).__name__, exc))
        else:
            traceback.print_exc()
        _out("Скрипт завершился с ошибкой — цель не взломана.")
        return 1

    _out("[OK] Скрипт выполнен без ошибок.")
    return 0


if __name__ == "__main__":
    rc = main(sys.argv[1])
    print("@@EXIT:%d" % rc, flush=True)
    sys.exit(rc)
"""


## Поиск интерпретатора Python. Результат кэшируется.
static func detect() -> PackedStringArray:
	if _detected:
		return _cached_cmd
	_detected = true
	var candidates: Array = [["python3"], ["python"]]
	if OS.get_name() == "Windows":
		candidates = [["python"], ["py", "-3"], ["python3"]]
	for c in candidates:
		var probe: PackedStringArray = PackedStringArray(c)
		var probe_args := probe.duplicate()
		probe_args.append("--version")
		var out: Array = []
		var code := OS.execute(probe[0], probe_args, out, true)
		if code == 0:
			_cached_cmd = probe
			print("[PyRunner] Найден интерпретатор: ", " ".join(Array(_cached_cmd)))
			return _cached_cmd
	_cached_cmd = PackedStringArray()
	print("[PyRunner] Python не найден — используется встроенный симулятор.")
	return _cached_cmd


static func available() -> bool:
	return detect().size() > 0


static func describe() -> String:
	var cmd := detect()
	if cmd.is_empty():
		return "Python не найден"
	return " ".join(Array(cmd))


static func _shell_args(command: String) -> Array:
	if OS.get_name() == "Windows":
		return ["cmd", ["/c", command]]
	return ["sh", ["-c", command]]


static func _quoted(path: String) -> String:
	if OS.get_name() == "Windows":
		return '"' + path + '"'
	return "'" + path + "'"


## Запустить код. Возвращает:
## { ok: bool, lines: Array[String], exit_code: int, timed_out: bool, error: String }
## Метод асинхронный: вызывать как `var res = await PyRunner.run_async(get_tree(), code, mission)`.
static func run_async(tree: SceneTree, code: String, mission: Mission,
		timeout_ms: int = DEFAULT_TIMEOUT_MS) -> Dictionary:
	var cmd_prefix := detect()
	if cmd_prefix.is_empty():
		return { "ok": false, "lines": [] as Array[String], "exit_code": -1,
			"timed_out": false, "error": "Python не найден" }

	var work := _ensure_work_dir()
	if work == "":
		return { "ok": false, "lines": [] as Array[String], "exit_code": -1,
			"timed_out": false, "error": "Не удалось создать рабочую папку" }

	# runner + скрипт студента
	var runner_src := RUNNER_SRC.replace("@@TARGET_IP@@", mission.target_ip) \
		.replace("@@TARGET_NAME@@", mission.target_name)
	var f := FileAccess.open(work + "/" + RUNNER_FILE, FileAccess.WRITE)
	if f == null:
		return { "ok": false, "lines": [] as Array[String], "exit_code": -1,
			"timed_out": false, "error": "Не удалось записать runner" }
	f.store_string(runner_src)
	f.close()

	var s := FileAccess.open(work + "/" + SCRIPT_FILE, FileAccess.WRITE)
	if s == null:
		return { "ok": false, "lines": [] as Array[String], "exit_code": -1,
			"timed_out": false, "error": "Не удалось записать скрипт" }
	s.store_string(code)
	s.close()

	if FileAccess.file_exists(work + "/" + OUT_FILE):
		DirAccess.remove_absolute(work + "/" + OUT_FILE)

	# команда: <python> -X utf8 neon_runner.py exploit.py > output.txt 2>&1
	var parts: Array[String] = []
	for p in cmd_prefix:
		parts.append(p)
	parts.append("-X")
	parts.append("utf8")
	parts.append(_quoted(work + "/" + RUNNER_FILE))
	parts.append(_quoted(work + "/" + SCRIPT_FILE))
	var command := " ".join(parts) + " > " + _quoted(work + "/" + OUT_FILE) + " 2>&1"

	var shell := _shell_args(command)
	var pid := OS.create_process(shell[0], shell[1])
	if pid <= 0:
		return { "ok": false, "lines": [] as Array[String], "exit_code": -1,
			"timed_out": false, "error": "Не удалось запустить процесс" }

	# ждём завершения, не блокируя интерфейс
	var started := Time.get_ticks_msec()
	var timed_out := false
	while OS.is_process_running(pid):
		if Time.get_ticks_msec() - started > timeout_ms:
			timed_out = true
			OS.kill(pid)
			break
		await tree.process_frame
	# даём файловой системе дописать буфер
	await tree.process_frame
	await tree.process_frame

	var lines: Array[String] = []
	var exit_code := -1
	if FileAccess.file_exists(work + "/" + OUT_FILE):
		var text := FileAccess.get_file_as_string(work + "/" + OUT_FILE)
		for raw in text.split("\n"):
			var line := String(raw).strip_edges(false, true)
			if line.begins_with(EXIT_MARK):
				exit_code = int(line.substr(EXIT_MARK.length()))
				continue
			if line.strip_edges() == "":
				continue
			lines.append(line)

	var ok := (not timed_out) and exit_code == 0
	var error := ""
	if timed_out:
		error = "Превышено время выполнения — похоже на бесконечный цикл"
	elif exit_code < 0:
		error = "Не удалось выполнить скрипт"

	return { "ok": ok, "lines": lines, "exit_code": exit_code,
		"timed_out": timed_out, "error": error }


static func _ensure_work_dir() -> String:
	var abs_dir := ProjectSettings.globalize_path(WORK_DIR)
	if not DirAccess.dir_exists_absolute(abs_dir):
		if DirAccess.make_dir_recursive_absolute(abs_dir) != OK:
			return ""
	return abs_dir
