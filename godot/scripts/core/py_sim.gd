class_name PySim
extends RefCounted

## Симулятор выполнения Python-скрипта.
## Порт 1:1 из src/engine.ts (simulateExecution + checkSyntax).
##
## Зачем он нужен, если есть настоящий Python: игра оценивает миссию по
## паттернам (required_patterns) и умеет показывать «хакерский» вывод терминала
## даже без установленного интерпретатора. Реальный Python подключается
## отдельно — см. scripts/core/py_runner.gd.

# Виды строк лога — используются для раскраски консоли.
const KIND_CMD := "cmd"
const KIND_OK := "ok"
const KIND_INFO := "info"
const KIND_WARN := "warn"
const KIND_ERR := "err"

const MAX_BRUTE_TRIES := 5


static func _re(pattern: String) -> RegEx:
	var re := RegEx.new()
	if re.compile(pattern) != OK:
		push_warning("Некорректная регулярка в данных миссии: " + pattern)
		return null
	return re


static func _search(pattern: String, text: String) -> bool:
	var re := _re(pattern)
	return re != null and re.search(text) != null


## Строки без комментариев — по ним проверяются требования миссии.
static func meaningful_lines(code: String) -> Array[String]:
	var out: Array[String] = []
	for line in code.split("\n"):
		var t := line.strip_edges()
		if t != "" and not t.begins_with("#"):
			out.append(String(line))
	return out


## Какой части требований не хватает. Пустой массив = миссия выполнена.
static func check_patterns(code: String, mission: Mission) -> Array[String]:
	var missing: Array[String] = []
	var body := "\n".join(meaningful_lines(code))
	for idx in range(mission.required_patterns.size()):
		var pattern: String = mission.required_patterns[idx]
		if not _search(pattern, body):
			if idx < mission.hints.size():
				missing.append(mission.hints[idx])
			else:
				missing.append("Требование %d не выполнено" % (idx + 1))
	return missing


## Простая проверка синтаксиса: двоеточия, кавычки, скобки, отступы.
## Возвращает {} если всё чисто, иначе { "line": int, "msg": String }.
static func check_syntax(code: String) -> Dictionary:
	var lines := code.split("\n")
	for i in range(lines.size()):
		var line: String = lines[i]
		var trimmed := line.strip_edges()
		if trimmed == "":
			continue

		# for/if/while/def/else без двоеточия
		if _search("^(for\\b|if\\b|while\\b|def\\b|else\\b)", trimmed) \
				and not trimmed.ends_with(":") and not trimmed.contains("#"):
			if not trimmed.contains(":"):
				return {
					"line": i + 1,
					"msg": "ожидалось ':' в конце строки («%s...»)" % trimmed.substr(0, 30),
				}

		# незакрытые кавычки
		var dq := line.count("\"")
		var sq := line.count("'")
		if dq % 2 != 0 or sq % 2 != 0:
			return { "line": i + 1, "msg": "незакрытая кавычка — проверь строки" }

		# несогласованные скобки
		if line.count("(") != line.count(")"):
			return { "line": i + 1, "msg": "несогласованные скобки ( и )" }

		# после строки с «:» должен быть отступ
		if i > 0:
			var prev := String(lines[i - 1]).strip_edges()
			if prev.ends_with(":") and line.length() > 0 \
					and not line.begins_with(" ") and not line.begins_with("\t"):
				return { "line": i + 1, "msg": "ожидался отступ после «:» (4 пробела)" }
	return {}


## Оценка «стиля» кода — 1..3 звезды (перенос из engine.ts).
static func style_score(code: String, mission: Mission, missing: Array[String]) -> int:
	var stars := 2
	if code.contains("#"):
		stars = 3
	var body_lines := meaningful_lines(code)
	if body_lines.size() <= 6 and missing.is_empty():
		stars = maxi(stars, 2)
	if code.contains("print") and mission.id > 2:
		stars = 3
	return stars


static func _line(text: String, kind: String, delay: int) -> Dictionary:
	return { "text": text, "kind": kind, "delay": delay }


## Главная функция: прогон кода в режиме симулятора.
## Возвращает { success: bool, logs: Array, missing: Array, style_score: int }.
static func simulate(code: String, mission: Mission, hack_speed_level: int) -> Dictionary:
	var logs: Array[Dictionary] = []
	var body := "\n".join(meaningful_lines(code))
	var missing := check_patterns(code, mission)

	if body.strip_edges() == "":
		logs.append(_line("! Пустой скрипт. Напиши код и попробуй снова.", KIND_WARN, 200))
		var miss_empty: Array[String] = ["Напиши код в редакторе"]
		return { "success": false, "logs": logs, "missing": miss_empty, "style_score": 0 }

	var syntax_err := check_syntax(body)
	if not syntax_err.is_empty():
		logs.append(_line("$ python3 exploit.py --target " + mission.target_ip, KIND_CMD, 250))
		logs.append(_line('  File "exploit.py", line %d' % int(syntax_err["line"]), KIND_ERR, 350))
		logs.append(_line("SyntaxError: " + str(syntax_err["msg"]), KIND_ERR, 300))
		logs.append(_line("Подсказка: проверь двоеточия, отступы и кавычки.", KIND_INFO, 200))
		var miss_syntax: Array[String] = [str(syntax_err["msg"])]
		return { "success": false, "logs": logs, "missing": miss_syntax, "style_score": 0 }

	var speed_div := 1.0 + float(hack_speed_level) * 0.35
	var base_delay := func(ms: int) -> int: return maxi(120, int(round(float(ms) / speed_div)))

	logs.append(_line("$ python3 exploit.py --target " + mission.target_ip, KIND_CMD, base_delay.call(400)))
	logs.append(_line("[*] Инициализация NeonHack Framework v3.7...", KIND_INFO, base_delay.call(450)))

	if _search("scan\\s*\\(", body):
		logs.append(_line("[SCAN] Сканирование подсети 192.168.0.0/24 ...", KIND_INFO, base_delay.call(600)))
		logs.append(_line("[SCAN] Найдено 4 узла. Цель: %s (%s) OK" % [mission.target_ip, mission.os_name], KIND_OK, base_delay.call(550)))

	if _search("print\\s*\\(", body):
		if _search("print\\s*\\(\\s*(target|ip|wallets|key)\\s*\\)", body):
			logs.append(_line("[OUT] %s :: %s" % [mission.target_ip, mission.target_name], KIND_INFO, base_delay.call(350)))
		else:
			var re := _re("print\\s*\\(\\s*([\"']?)(.*?)\\1\\s*\\)")
			var value := "..."
			if re != null:
				var m := re.search(body)
				if m != null:
					value = m.get_string(2).substr(0, 60)
			logs.append(_line("[OUT] " + value, KIND_INFO, base_delay.call(350)))

	if _search("\\bconnect\\s*\\(", body):
		logs.append(_line("[NET] Подключение к %s:22 ..." % mission.target_ip, KIND_INFO, base_delay.call(600)))
		logs.append(_line("[NET] Туннель установлен. Обход firewall... OK", KIND_OK, base_delay.call(550)))

	if _search("\\bbrute\\s*\\(", body):
		var tries := 5
		var re_range := _re("range\\s*\\(\\s*(\\d+)")
		if re_range != null:
			var m := re_range.search(body)
			if m != null:
				tries = mini(int(m.get_string(1)), 12)
		logs.append(_line("[BRUTE] Перебор %d комбинаций..." % tries, KIND_INFO, base_delay.call(500)))
		for i in range(mini(tries, MAX_BRUTE_TRIES)):
			logs.append(_line("  попытка %d ... неверно" % i, KIND_INFO, base_delay.call(220)))
		if _search("for\\s+", body):
			logs.append(_line("[BRUTE] Пароль подобран! Цикл for сработал идеально", KIND_OK, base_delay.call(500)))

	if _search("\\bif\\s+", body):
		logs.append(_line("[CHECK] Проверка условия...", KIND_INFO, base_delay.call(400)))
		logs.append(_line("[CHECK] Условие True -> выполняю блок if", KIND_OK, base_delay.call(400)))

	if _search("else\\s*:", body):
		logs.append(_line("[CHECK] Ветка else готова (защита от ошибок)", KIND_INFO, base_delay.call(250)))

	if _search("\\bwallets\\s*=\\s*\\[", body):
		logs.append(_line("[DATA] Список кошельков загружен: 3 адреса", KIND_INFO, base_delay.call(400)))
		logs.append(_line("  |- 0xA1 ... 0xB2 ... 0xC3", KIND_INFO, base_delay.call(300)))

	if _search("\\bdrain\\s*\\(", body):
		logs.append(_line("[DRAIN] Извлечение средств из кошельков...", KIND_INFO, base_delay.call(600)))
		logs.append(_line("[DRAIN] 0xA1 drained | 0xB2 drained | 0xC3 drained", KIND_OK, base_delay.call(600)))

	if _search("def\\s+\\w+", body):
		var fn_name := ""
		var re_fn := _re("def\\s+(\\w+)")
		if re_fn != null:
			var m := re_fn.search(body)
			if m != null:
				fn_name = m.get_string(1)
		logs.append(_line("[FUNC] Функция %s() скомпилирована" % fn_name, KIND_OK, base_delay.call(400)))

	if _search("\\bbypass\\s*\\(", body):
		logs.append(_line("[EVADE] Антивирус обойдён, EDR ослеплён", KIND_OK, base_delay.call(500)))
	if _search("\\bdecrypt\\s*\\(", body):
		logs.append(_line("[CRYPT] AES-256 ключи восстановлены, сид-фраза расшифрована", KIND_OK, base_delay.call(650)))
	if _search("\\bextract\\s*\\(", body):
		logs.append(_line("[EXTRACT] Приватные ключи извлечены", KIND_OK, base_delay.call(550)))
	if _search("while\\s+", body):
		logs.append(_line("[LOOP] while-цикл запущен, флаг mining = True", KIND_INFO, base_delay.call(400)))
	if _search("install_miner\\s*\\(", body):
		logs.append(_line("[MINER] Загрузка xmrig-neon... компиляция...", KIND_INFO, base_delay.call(600)))
		logs.append(_line("[MINER] Майнер внедрён в автозагрузку, скрыт от диспетчера", KIND_OK, base_delay.call(600)))

	if not missing.is_empty():
		logs.append(_line("[!] Эксплойт завершён с ошибками.", KIND_WARN, base_delay.call(350)))
		logs.append(_line("[!] Цель не взломана. Не хватает шагов: %d" % missing.size(), KIND_ERR, base_delay.call(300)))
		logs.append(_line("Открой вкладку «Подсказки» — там разжёвано по шагам.", KIND_INFO, base_delay.call(200)))
		return { "success": false, "logs": logs, "missing": missing, "style_score": 1 }

	logs.append(_line("[ROOT] Доступ ROOT получен! Заметаю следы...", KIND_OK, base_delay.call(550)))
	logs.append(_line("[WALLET] Перевод %s %s на твой кошелёк..." % [
		Neon.fmt_crypto(mission.reward_amount), mission.reward_crypto], KIND_OK, base_delay.call(700)))
	logs.append(_line("[OK] ВЗЛОМ ЗАВЕРШЁН. Ты — машина.", KIND_OK, base_delay.call(400)))

	var no_missing: Array[String] = []
	return {
		"success": true, "logs": logs, "missing": no_missing,
		"style_score": style_score(code, mission, missing),
	}
