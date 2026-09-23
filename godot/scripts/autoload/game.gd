extends Node

## Состояние игры и вся экономика (autoload `Game`).
##
## Аналог React-состояния из App.tsx (useState + useEffect). Разница в том, что
## вместо перерисовки компонентов мы рассылаем сигналы: любой экран подписывается
## на state_changed / prices_changed и обновляет свои подписи.
##
## Сохранение: было localStorage, стало user://cryptohack_save_v1.json
## (Windows: %APPDATA%\Godot\app_userdata\CRYPTO_HACK\)

signal state_changed
signal prices_changed
signal toast_requested(title: String, text: String, kind: String)
signal level_up(level: int)
signal mission_completed(mission: Mission, stars: int)
signal miners_changed

const SAVE_PATH := "user://cryptohack_save_v1.json"
const SAVE_VERSION := 1

# ---- Экономика (перенос констант из App.tsx) ----
const MINER_BASE_RATE := {
	"BTC": 0.0000009,
	"ETH": 0.000018,
	"XMR": 0.00042,
	"SOL": 0.00031,
}
const MINER_EFF_MULT := [1.0, 1.5, 2.2, 3.2, 4.5, 6.0]
const HACK_REWARD_BONUS := [0.0, 0.05, 0.1, 0.2, 0.35, 0.5]
const STEALTH_FEE := [0.05, 0.04, 0.03, 0.02, 0.0]
const STEALTH_XP := [0.0, 0.1, 0.2, 0.35, 0.5]

const PRICE_TICK_SEC := 2.5
const HISTORY_LEN := 40
const MINER_INSTALL_BASE := 120.0
const MINER_INSTALL_STEP := 80.0
const LESSON_STIPEND := 40.0

# ---- Данные контента ----
var data := GameData.new()

# ---- Состояние игрока ----
var dollars := 150.0
var crypto := {"BTC": 0.0, "ETH": 0.0, "XMR": 0.0, "SOL": 0.0}
var xp := 0
var level := 1
var completed_missions: Array[int] = []
var miners: Array[Dictionary] = []
var upgrades := {"hackSpeed": 0, "minerEff": 0, "codeLib": 0, "stealth": 0}
var completed_lessons: Array[int] = []
var total_hacked := 0
var total_earned_dollars := 0.0
var total_trades := 0

# ---- Настройки ----
var sound_on := true
var real_python := true          ## выполнять код настоящим Python (если он найден)
var selected_mission := 1

# ---- Рынок ----
var prices := {}
var history := {}

var has_save := false

var _price_timer: Timer
var _mine_timer: Timer


func _ready() -> void:
	if not data.load_all():
		push_error("Не удалось загрузить данные игры: " + data.load_error)
	_init_market()
	load_save()
	if real_python and not PyRunner.available():
		real_python = false
	_price_timer = Timer.new()
	_price_timer.wait_time = PRICE_TICK_SEC
	_price_timer.autostart = true
	_price_timer.timeout.connect(_tick_prices)
	add_child(_price_timer)

	_mine_timer = Timer.new()
	_mine_timer.wait_time = 1.0
	_mine_timer.autostart = true
	_mine_timer.timeout.connect(_tick_miners)
	add_child(_mine_timer)


func _init_market() -> void:
	for c in data.cryptos:
		prices[c.id] = c.base_price
		var hist: Array[float] = []
		for i in range(30):
			hist.append(c.base_price * (1.0 + (randf() - 0.5) * 0.02))
		history[c.id] = hist


# ==================== ОПЫТ И УРОВНИ ====================
func xp_for_level(lvl: int) -> int:
	return lvl * 300


func xp_progress() -> float:
	return clampf(float(xp) / float(xp_for_level(level)), 0.0, 1.0)


func add_xp(amount: int) -> void:
	var bonus := 1.0 + STEALTH_XP[clampi(upgrades["stealth"], 0, STEALTH_XP.size() - 1)]
	xp += int(round(float(amount) * bonus))
	var leveled := false
	while xp >= xp_for_level(level):
		xp -= xp_for_level(level)
		level += 1
		leveled = true
	if leveled:
		var unlocked := _crypto_for_level(level)
		var text := "Открыты новые возможности."
		if unlocked != null:
			text = "Разблокирована монета %s (%s)!" % [unlocked.name, unlocked.id]
		notify("Уровень %d!" % level, text, "gold")
		level_up.emit(level)
		if sound_on:
			Sfx.level_up()
	state_changed.emit()
	save()


func _crypto_for_level(lvl: int) -> CryptoInfo:
	for c in data.cryptos:
		if c.unlock_level == lvl:
			return c
	return null


# ==================== МИССИИ ====================
func mission_unlocked(m: Mission) -> bool:
	return level >= m.required_level and upgrade_level("codeLib") >= m.required_code_lib


func mission_completed(id: int) -> bool:
	return completed_missions.has(id)


func hack_success(m: Mission, stars: int) -> void:
	var bonus := 1.0 + HACK_REWARD_BONUS[clampi(upgrades["hackSpeed"], 0, HACK_REWARD_BONUS.size() - 1)]
	crypto[m.reward_crypto] = float(crypto.get(m.reward_crypto, 0.0)) + m.reward_amount * bonus
	dollars += m.reward_dollars
	total_earned_dollars += m.reward_dollars
	total_hacked += 1
	if not completed_missions.has(m.id):
		completed_missions.append(m.id)
	notify("Взлом успешен!",
		"+%s %s · +%s · +%d XP" % [
			Neon.fmt_crypto(m.reward_amount * bonus), m.reward_crypto,
			Neon.fmt_dollars(m.reward_dollars), m.reward_xp], "ok")
	mission_completed.emit(m, stars)
	add_xp(m.reward_xp)
	if m.id == 8:
		notify("ТЫ — ЛЕГЕНДА!", "Все цели взломаны. Дата-центр твой. Майни и богатей!", "gold")
	save()


# ==================== МАЙНЕРЫ ====================
func miner_install_cost() -> float:
	return MINER_INSTALL_BASE + float(miners.size()) * MINER_INSTALL_STEP


func miner_eff_mult() -> float:
	return MINER_EFF_MULT[clampi(upgrades["minerEff"], 0, MINER_EFF_MULT.size() - 1)]


func miners_on(mission_id: int) -> bool:
	for m in miners:
		if m["mission_id"] == mission_id:
			return true
	return false


func install_miner(mission_id: int, crypto_id: String) -> bool:
	var cost := miner_install_cost()
	if dollars < cost:
		notify("Нет денег", "Продай крипту на бирже.", "err")
		return false
	var m := data.mission(mission_id)
	if m == null:
		return false
	dollars -= cost
	miners.append({
		"id": str(Time.get_ticks_usec()),
		"mission_id": mission_id,
		"pc_name": m.target_name,
		"ip": m.target_ip,
		"crypto": crypto_id,
		"earned": 0.0,
	})
	notify("Майнер установлен", "%s теперь майнит %s" % [m.target_name, crypto_id], "ok")
	if sound_on:
		Sfx.beep_square(500.0, 0.08, 0.3)
	miners_changed.emit()
	state_changed.emit()
	add_xp(30)
	save()
	return true


func remove_miner(miner_id: String) -> void:
	var kept: Array[Dictionary] = []
	for m in miners:
		if str(m["id"]) != miner_id:
			kept.append(m)
	miners = kept
	miners_changed.emit()
	state_changed.emit()
	save()


func _tick_miners() -> void:
	if miners.is_empty():
		return
	var eff := miner_eff_mult()
	for m in miners:
		var cid: String = m["crypto"]
		var gain: float = float(MINER_BASE_RATE[cid]) * eff
		crypto[cid] = float(crypto.get(cid, 0.0)) + gain
		m["earned"] = float(m["earned"]) + gain
	state_changed.emit()


func miner_income_per_min() -> float:
	var eff := miner_eff_mult()
	var total := 0.0
	for m in miners:
		var cid: String = m["crypto"]
		total += float(MINER_BASE_RATE[cid]) * eff * 60.0 * float(prices.get(cid, 0.0))
	return total


# ==================== БИРЖА ====================
func fee() -> float:
	return STEALTH_FEE[clampi(upgrades["stealth"], 0, STEALTH_FEE.size() - 1)]


func crypto_value(cid: String) -> float:
	return float(crypto.get(cid, 0.0)) * float(prices.get(cid, 0.0))


func portfolio_value() -> float:
	var total := 0.0
	for c in data.cryptos:
		total += crypto_value(c.id)
	return total


func crypto_unlocked(cid: String) -> bool:
	var c := data.crypto(cid)
	return c != null and level >= c.unlock_level


func trade(cid: String, usd: float, is_buy: bool) -> bool:
	if usd <= 0.0 or not crypto_unlocked(cid):
		return false
	var price := float(prices[cid])
	if is_buy:
		if usd > dollars:
			return false
		var got := (usd / price) * (1.0 - fee())
		dollars -= usd
		crypto[cid] = float(crypto.get(cid, 0.0)) + got
		notify("Покупка", "Куплено %s %s за %s" % [Neon.fmt_crypto(got), cid, Neon.fmt_dollars_full(usd)], "info")
	else:
		var need := usd / price
		if need > float(crypto.get(cid, 0.0)):
			return false
		var got_usd := usd * (1.0 - fee())
		dollars += got_usd
		crypto[cid] = float(crypto.get(cid, 0.0)) - need
		total_earned_dollars += got_usd
		notify("Продажа", "Продано %s %s → %s" % [Neon.fmt_crypto(need), cid, Neon.fmt_dollars_full(got_usd)], "ok")
	total_trades += 1
	if sound_on:
		Sfx.trade()
	state_changed.emit()
	add_xp(10)
	save()
	return true


func _tick_prices() -> void:
	var now := Time.get_ticks_msec() / 1000.0
	for c in data.cryptos:
		var drift := (randf() - 0.5) * 2.0 * c.volatility + sin(now / 60.0 + c.base_price) * 0.002
		prices[c.id] = clampf(float(prices[c.id]) * (1.0 + drift),
			c.base_price * 0.5, c.base_price * 2.0)
		var hist: Array = history[c.id]
		hist.append(prices[c.id])
		while hist.size() > HISTORY_LEN:
			hist.pop_front()
	prices_changed.emit()


# ==================== АПГРЕЙДЫ ====================
func upgrade_level(id: String) -> int:
	return int(upgrades.get(id, 0))


func upgrade_cost(id: String) -> int:
	var u := data.upgrade(id)
	if u == null:
		return -1
	return u.cost_for_level(upgrade_level(id))


func buy_upgrade(id: String) -> bool:
	var u := data.upgrade(id)
	if u == null:
		return false
	var lvl := upgrade_level(id)
	var cost := u.cost_for_level(lvl)
	if cost < 0:
		return false
	if dollars < float(cost):
		notify("Не хватает долларов", "Нужно %s" % Neon.fmt_dollars_full(float(cost)), "err")
		return false
	dollars -= float(cost)
	upgrades[id] = lvl + 1
	notify("Апгрейд куплен", "%s → уровень %d" % [u.name, lvl + 1], "gold")
	if sound_on:
		Sfx.buy()
	if id == "codeLib":
		notify("Новые функции!", "Открыты: %s" % u.effect_text(lvl + 1), "info")
	state_changed.emit()
	save()
	return true


# ==================== УРОКИ ====================
func lesson_completed(id: int) -> bool:
	return completed_lessons.has(id)


func complete_lesson(id: int) -> void:
	if completed_lessons.has(id):
		return
	var l := data.lesson(id)
	if l == null:
		return
	completed_lessons.append(id)
	dollars += LESSON_STIPEND
	notify("Урок пройден!", "+%d XP · +$%d стипендия" % [l.xp, int(LESSON_STIPEND)], "ok")
	if sound_on:
		Sfx.ui_ok()
	add_xp(l.xp)
	state_changed.emit()
	save()


# ==================== УВЕДОМЛЕНИЯ ====================
func notify(title: String, text: String, kind: String = "info") -> void:
	toast_requested.emit(title, text, kind)


# ==================== СОХРАНЕНИЕ ====================
func save() -> void:
	var payload := {
		"version": SAVE_VERSION,
		"dollars": dollars,
		"crypto": crypto,
		"xp": xp,
		"level": level,
		"completed_missions": completed_missions,
		"miners": miners,
		"upgrades": upgrades,
		"completed_lessons": completed_lessons,
		"total_hacked": total_hacked,
		"total_earned_dollars": total_earned_dollars,
		"total_trades": total_trades,
		"sound_on": sound_on,
		"real_python": real_python,
		"selected_mission": selected_mission,
	}
	var f := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if f == null:
		push_warning("Не удалось сохранить игру")
		return
	f.store_string(JSON.stringify(payload, "  "))
	f.close()
	has_save = true


func load_save() -> bool:
	if not FileAccess.file_exists(SAVE_PATH):
		return false
	var text := FileAccess.get_file_as_string(SAVE_PATH)
	var parsed = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		return false
	var d: Dictionary = parsed

	dollars = float(d.get("dollars", dollars))
	xp = int(d.get("xp", xp))
	level = maxi(1, int(d.get("level", level)))
	sound_on = bool(d.get("sound_on", sound_on))
	real_python = bool(d.get("real_python", real_python))
	selected_mission = int(d.get("selected_mission", selected_mission))
	total_hacked = int(d.get("total_hacked", 0))
	total_earned_dollars = float(d.get("total_earned_dollars", 0.0))
	total_trades = int(d.get("total_trades", 0))

	for cid in d.get("crypto", {}):
		if crypto.has(cid):
			crypto[cid] = float(d["crypto"][cid])

	completed_missions.clear()
	for id in d.get("completed_missions", []):
		completed_missions.append(int(id))

	completed_lessons.clear()
	for id in d.get("completed_lessons", []):
		completed_lessons.append(int(id))

	miners.clear()
	for m in d.get("miners", []):
		miners.append({
			"id": str(m.get("id", "")),
			"mission_id": int(m.get("mission_id", 0)),
			"pc_name": str(m.get("pc_name", "")),
			"ip": str(m.get("ip", "")),
			"crypto": str(m.get("crypto", "BTC")),
			"earned": float(m.get("earned", 0.0)),
		})

	for uid in d.get("upgrades", {}):
		if upgrades.has(uid):
			upgrades[uid] = int(d["upgrades"][uid])

	has_save = true
	state_changed.emit()
	return true


func reset_progress() -> void:
	if FileAccess.file_exists(SAVE_PATH):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(SAVE_PATH))
	dollars = 150.0
	crypto = {"BTC": 0.0, "ETH": 0.0, "XMR": 0.0, "SOL": 0.0}
	xp = 0
	level = 1
	completed_missions.clear()
	completed_lessons.clear()
	miners.clear()
	upgrades = {"hackSpeed": 0, "minerEff": 0, "codeLib": 0, "stealth": 0}
	total_hacked = 0
	total_earned_dollars = 0.0
	total_trades = 0
	selected_mission = 1
	has_save = false
	_init_market()
	state_changed.emit()
	prices_changed.emit()


# ==================== ОБРАЗ КОДА ====================
## Режим выполнения: настоящий Python или встроенный симулятор.
func python_mode_text() -> String:
	if real_python and PyRunner.available():
		return "РЕАЛЬНЫЙ PYTHON (%s)" % PyRunner.describe()
	return "СИМУЛЯТОР ТЕРМИНАЛА"
