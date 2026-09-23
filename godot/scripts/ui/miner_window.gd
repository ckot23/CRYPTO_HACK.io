class_name MinerWindowView
extends VBoxContainer

## Окно «Майнинг-ферма» — порт MinerWindow из App.tsx.

var _list_box: VBoxContainer
var _install_box: VBoxContainer
var _head_box: VBoxContainer
var _crypto_row: HBoxContainer
var _cost_label: Label
var _selected_crypto := "BTC"


func _ready() -> void:
	add_theme_constant_override("separation", 10)
	_build()
	Game.state_changed.connect(_refresh)
	Game.miners_changed.connect(_refresh)
	_refresh()


func _build() -> void:
	_head_box = NeonUI.vbox(2)
	add_child(NeonUI.card_with(Neon.with_alpha(Neon.CYAN, 0.45), _head_box,
		Neon.with_alpha(Neon.CYAN, 0.05), 12))

	_list_box = NeonUI.vbox(6)
	add_child(_list_box)

	# --- выбор монеты для нового майнера ---
	var pick_box := NeonUI.vbox(6)
	pick_box.add_child(NeonUI.bold_label("УСТАНОВИТЬ МАЙНЕР", 11, Neon.TEXT_DIM))
	_crypto_row = NeonUI.hbox(6)
	pick_box.add_child(_crypto_row)
	_cost_label = NeonUI.label("", 11, Neon.TEXT_MUTED)
	pick_box.add_child(_cost_label)
	add_child(pick_box)

	_install_box = NeonUI.vbox(6)
	add_child(_install_box)

	var hint := NeonUI.dim_label("Майнеры капают крипту каждую секунду, даже когда окно закрыто в трей.", 10)
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	add_child(hint)


func _refresh() -> void:
	_refresh_head()
	_refresh_miners()
	_refresh_picker()
	_refresh_install_list()


func _refresh_head() -> void:
	NeonUI.clear(_head_box)
	var title := NeonUI.hbox(6)
	title.add_child(NeonUI.bold_label("МАЙНИНГ-ФЕРМА · %d РИГ(ОВ)" % Game.miners.size(), 13, Neon.CYAN))
	title.add_child(NeonUI.stretch())
	title.add_child(NeonUI.label("мощность x%.1f" % Game.miner_eff_mult(), 11, Neon.TEXT_MUTED))
	_head_box.add_child(title)
	_head_box.add_child(NeonUI.label("Доход: ~%s/мин пассивно" % Neon.fmt_dollars_full(Game.miner_income_per_min()),
		11, Neon.GREEN))


func _refresh_miners() -> void:
	NeonUI.clear(_list_box)
	if Game.miners.is_empty():
		return
	for m in Game.miners:
		var cid: String = m["crypto"]
		var info := Game.data.crypto(cid)
		var row := NeonUI.hbox(10)

		var icon := NeonUI.bold_label(info.icon if info != null else "?", 18,
			info.color if info != null else Neon.CYAN)
		icon.custom_minimum_size = Vector2(28, 0)
		row.add_child(icon)

		var texts := NeonUI.vbox(0)
		texts.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		texts.add_child(NeonUI.bold_label(str(m["pc_name"]), 12, Color.WHITE))
		texts.add_child(NeonUI.label("%s · майнит %s" % [m["ip"], cid], 10, Neon.TEXT_MUTED))
		texts.add_child(NeonUI.label("добыто: %s %s" % [Neon.fmt_crypto(float(m["earned"])), cid],
			11, Neon.GREEN))
		row.add_child(texts)

		row.add_child(NeonUI.label("● LIVE", 10, Neon.GREEN))
		var stop := NeonUI.button("снять", Neon.PINK, NeonUI.KIND_OUTLINE, 10)
		stop.pressed.connect(func() -> void: Game.remove_miner(str(m["id"])))
		row.add_child(stop)

		_list_box.add_child(NeonUI.card_with(Neon.WHITE_10, row, Neon.PANEL_BAR, 10))


func _refresh_picker() -> void:
	NeonUI.clear(_crypto_row)
	for info in Game.data.cryptos:
		var unlocked := Game.level >= info.unlock_level
		var selected := info.id == _selected_crypto
		var b := NeonUI.button("%s %s" % [info.icon, info.id], info.color,
			NeonUI.KIND_SOLID if selected else NeonUI.KIND_OUTLINE, 11)
		b.disabled = not unlocked
		b.custom_minimum_size = Vector2(78, 30)
		b.pressed.connect(func() -> void:
			_selected_crypto = info.id
			_refresh())
		_crypto_row.add_child(b)
	_cost_label.text = "Стоимость установки: %s · %s" % [
		Neon.fmt_dollars_full(Game.miner_install_cost()), Game.python_mode_text()]


func _refresh_install_list() -> void:
	NeonUI.clear(_install_box)
	var free_targets: Array[Mission] = []
	for m in Game.data.missions:
		if Game.mission_completed(m.id) and not Game.miners_on(m.id):
			free_targets.append(m)

	if free_targets.is_empty():
		var empty := NeonUI.label(
			"Нет свободных взломанных компов. Взломай новую цель в Хак-терминале.", 11, Neon.TEXT_MUTED)
		empty.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_install_box.add_child(NeonUI.card_with(Neon.WHITE_10, empty, Neon.PANEL_DARK, 10))
		return

	var cost := Game.miner_install_cost()
	for m in free_targets:
		var row := NeonUI.hbox(10)
		var texts := NeonUI.vbox(0)
		texts.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		texts.add_child(NeonUI.label(m.target_name, 12, Color.WHITE))
		texts.add_child(NeonUI.label("%s · %s" % [m.target_ip, m.os_name], 10, Neon.TEXT_MUTED))
		row.add_child(texts)
		var install := NeonUI.button("+ %s" % _selected_crypto, Neon.CYAN, NeonUI.KIND_SOLID, 11)
		install.disabled = Game.dollars < cost
		install.pressed.connect(func() -> void: Game.install_miner(m.id, _selected_crypto))
		row.add_child(install)
		_install_box.add_child(NeonUI.card_with(Neon.WHITE_10, row, Color(0, 0, 0, 0.4), 8))
