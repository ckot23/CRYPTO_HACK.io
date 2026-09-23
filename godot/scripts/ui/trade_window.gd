class_name TradeWindowView
extends VBoxContainer

## Окно «Биржа DARKEX» — порт TradeWindow из App.tsx.
## Живой график: SVG-полилиния из веб-версии стала Control._draw (PriceChart).

var _cards_row: HBoxContainer
var _chart: PriceChart
var _chart_title: Label
var _chart_change: Label
var _fee_label: Label
var _sell_info: Label
var _sell_result: Label
var _buy_info: Label
var _buy_result: Label
var _sell_button: Button
var _buy_button: Button
var _amount_edit: LineEdit
var _selected := "BTC"


func _ready() -> void:
	add_theme_constant_override("separation", 10)
	_build()
	Game.state_changed.connect(_refresh)
	Game.prices_changed.connect(_refresh)
	_refresh()


func _build() -> void:
	_cards_row = NeonUI.hbox(8)
	add_child(_cards_row)

	# --- график ---
	var chart_box := NeonUI.vbox(4)
	var head := NeonUI.hbox(8)
	_chart_title = NeonUI.bold_label("", 11, Neon.GREEN)
	head.add_child(_chart_title)
	head.add_child(NeonUI.stretch())
	_chart_change = NeonUI.bold_label("", 11, Neon.GREEN)
	head.add_child(_chart_change)
	chart_box.add_child(head)

	_chart = PriceChart.new()
	chart_box.add_child(_chart)

	_fee_label = NeonUI.label("", 10, Neon.TEXT_MUTED)
	chart_box.add_child(_fee_label)
	add_child(NeonUI.card_with(Neon.WHITE_10, chart_box, Color(0, 0, 0, 0.5), 10))

	# --- ввод суммы ---
	var amount_box := NeonUI.hbox(8)
	amount_box.add_child(NeonUI.label("Сумма в $:", 11, Neon.TEXT_DIM))
	_amount_edit = LineEdit.new()
	_amount_edit.text = "100"
	_amount_edit.custom_minimum_size = Vector2(120, 28)
	amount_box.add_child(_amount_edit)
	amount_box.add_child(NeonUI.stretch())
	add_child(amount_box)

	# --- продажа / покупка ---
	var columns := NeonUI.hbox(10)
	columns.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	columns.add_child(_build_sell_panel())
	columns.add_child(_build_buy_panel())
	add_child(columns)

	var tip := NeonUI.dim_label(
		"Совет трейдера: покупай на падении, продавай на росте. Стелс-модуль снижает комиссию вплоть до 0%.", 10)
	tip.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	add_child(tip)


func _build_sell_panel() -> Control:
	var box := NeonUI.vbox(4)
	box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	box.add_child(NeonUI.bold_label("ПРОДАТЬ → $", 11, Neon.GREEN))
	_sell_info = NeonUI.label("", 10, Neon.TEXT_DIM)
	box.add_child(_sell_info)
	_sell_result = NeonUI.label("", 10, Neon.TEXT_MUTED)
	box.add_child(_sell_result)

	var row := NeonUI.hbox(6)
	_sell_button = NeonUI.button("ПРОДАТЬ ЗА $", Neon.GREEN, NeonUI.KIND_SOLID, 11)
	_sell_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_sell_button.pressed.connect(func() -> void: _trade(false))
	row.add_child(_sell_button)
	var max_btn := NeonUI.button("MAX", Neon.CYAN, NeonUI.KIND_OUTLINE, 10)
	max_btn.pressed.connect(_fill_max)
	row.add_child(max_btn)
	box.add_child(row)
	return NeonUI.card_with(Neon.with_alpha(Neon.GREEN, 0.4), box,
		Neon.with_alpha(Neon.GREEN, 0.05), 10)


func _build_buy_panel() -> Control:
	var box := NeonUI.vbox(4)
	box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	box.add_child(NeonUI.bold_label("КУПИТЬ $ → монета", 11, Neon.CYAN))
	_buy_info = NeonUI.label("", 10, Neon.TEXT_DIM)
	box.add_child(_buy_info)
	_buy_result = NeonUI.label("", 10, Neon.TEXT_MUTED)
	box.add_child(_buy_result)
	_buy_button = NeonUI.button("КУПИТЬ", Neon.CYAN, NeonUI.KIND_SOLID, 11)
	_buy_button.pressed.connect(func() -> void: _trade(true))
	box.add_child(_buy_button)
	return NeonUI.card_with(Neon.with_alpha(Neon.CYAN, 0.4), box,
		Neon.with_alpha(Neon.CYAN, 0.05), 10)


func _amount() -> float:
	var text := _amount_edit.text.strip_edges().replace(",", ".")
	if not text.is_valid_float():
		return 0.0
	return maxf(0.0, float(text))


func _fill_max() -> void:
	var price := float(Game.prices[_selected])
	_amount_edit.text = str(int(floor(float(Game.crypto.get(_selected, 0.0)) * price)))


func _trade(is_buy: bool) -> void:
	var usd := _amount()
	var ok := Game.trade(_selected, usd, is_buy)
	if ok:
		var price := float(Game.prices[_selected])
		var verb := "Куплено" if is_buy else "Продано"
		var got := (usd / price) * (1.0 - Game.fee()) if is_buy else usd * (1.0 - Game.fee())
		var unit := _selected if is_buy else "$"
		var text := "%s %s %s" % [verb, Neon.fmt_crypto(got) if is_buy else Neon.fmt_dollars_full(got), unit]
		if is_buy:
			_buy_result.text = text
			_sell_result.text = ""
		else:
			_sell_result.text = text
			_buy_result.text = ""
		_refresh()
	else:
		var warn := "Недостаточно средств или монета заблокирована."
		if is_buy:
			_buy_result.text = warn
		else:
			_sell_result.text = warn


func _refresh() -> void:
	_refresh_cards()
	var price := float(Game.prices[_selected])
	var info := Game.data.crypto(_selected)
	_chart_title.text = "%s %s / USD" % [info.icon, info.name] if info != null else _selected
	var hist: Array = Game.history.get(_selected, [])
	var values: Array[float] = []
	for v in hist:
		values.append(float(v))
	if values.size() >= 2:
		var change := (values[values.size() - 1] - values[0]) / values[0] * 100.0
		_chart_change.text = "%s%.2f%%" % ["▲ " if change >= 0 else "▼ ", absf(change)]
		_chart_change.add_theme_color_override("font_color", Neon.GREEN if change >= 0 else Neon.PINK)
	else:
		_chart_change.text = ""
	_chart.set_values(values, info.color if info != null else Neon.GREEN)
	_fee_label.text = "● живой график · обновляется каждые %.0f сек · комиссия %.0f%%" % [
		Game.PRICE_TICK_SEC, Game.fee() * 100.0]

	var balance := float(Game.crypto.get(_selected, 0.0))
	_sell_info.text = "Баланс: %s %s ≈ %s" % [
		Neon.fmt_crypto(balance), _selected, Neon.fmt_dollars_full(balance * price)]
	_sell_result.text = "Получишь: %s (на руки)" % Neon.fmt_dollars_full(_amount() * (1.0 - Game.fee()))
	_buy_info.text = "Доллары: %s · 1 %s = %s" % [
		Neon.fmt_dollars_full(Game.dollars), _selected, Neon.fmt_price(price)]
	_buy_result.text = "Получишь: %s %s" % [
		Neon.fmt_crypto(_amount() / maxf(price, 0.000001) * (1.0 - Game.fee())), _selected]

	var need := _amount() / maxf(price, 0.000001)
	_sell_button.disabled = _amount() <= 0.0 or need > balance
	_buy_button.disabled = _amount() <= 0.0 or _amount() > Game.dollars


func _refresh_cards() -> void:
	NeonUI.clear(_cards_row)
	for info in Game.data.cryptos:
		var unlocked := Game.level >= info.unlock_level
		var price := float(Game.prices[info.id])
		var hist: Array = Game.history.get(info.id, [])
		var change := 0.0
		if hist.size() >= 2:
			change = (float(hist[hist.size() - 1]) - float(hist[0])) / float(hist[0]) * 100.0
		var selected := info.id == _selected

		var box := NeonUI.vbox(2)
		var id_row := NeonUI.hbox(4)
		id_row.add_child(NeonUI.bold_label(info.icon if unlocked else "○", 15,
			info.color if unlocked else Neon.TEXT_MUTED))
		id_row.add_child(NeonUI.bold_label(info.id, 12, Color.WHITE))
		box.add_child(id_row)
		if unlocked:
			box.add_child(NeonUI.label(Neon.fmt_price(price), 12, Color.WHITE))
			box.add_child(NeonUI.label("%s%.2f%%" % ["▲ " if change >= 0 else "▼ ", absf(change)],
				11, Neon.GREEN if change >= 0 else Neon.PINK))
		else:
			box.add_child(NeonUI.label("ур. %d" % info.unlock_level, 10, Neon.TEXT_MUTED))

		var card := NeonUI.card_with(
			info.color if selected else Neon.WHITE_10, box, Neon.PANEL_DARK, 8)
		card.size_flags_horizontal = Control.SIZE_EXPAND_FILL

		var btn := Button.new()
		btn.flat = true
		btn.disabled = not unlocked
		btn.pressed.connect(func() -> void:
			_selected = info.id
			_refresh())
		btn.add_theme_stylebox_override("normal", Neon.sb(Color(0, 0, 0, 0), Color(0, 0, 0, 0), 0))
		btn.add_theme_stylebox_override("hover", Neon.sb(Neon.WHITE_05, Color(0, 0, 0, 0), 0))
		btn.add_theme_stylebox_override("pressed", Neon.sb(Neon.WHITE_10, Color(0, 0, 0, 0), 0))
		card.add_child(btn)
		_cards_row.add_child(card)
