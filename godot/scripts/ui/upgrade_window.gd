class_name UpgradeWindowView
extends VBoxContainer

## Окно «Чёрный рынок» (апгрейды) — порт UpgradeWindow из App.tsx.
##
## Иконки апгрейдов в веб-версии были эмодзи (⚡⛏📚👻). В Godot эмодзи-шрифта нет,
## поэтому подобраны символы, которые точно есть в подключённых шрифтах —
## см. docs/PORTING_GUIDE.md, раздел про шрифты.

const ICONS := {
	"hackSpeed": "⚡",
	"minerEff": "⚙",
	"codeLib": "✎",
	"stealth": "○",
}

var _balance_box: VBoxContainer
var _cards_box: VBoxContainer


func _ready() -> void:
	add_theme_constant_override("separation", 10)
	_build()
	Game.state_changed.connect(_refresh)
	_refresh()


func _build() -> void:
	_balance_box = NeonUI.vbox(0)
	add_child(NeonUI.card_with(Neon.with_alpha(Neon.YELLOW, 0.45), _balance_box,
		Neon.with_alpha(Neon.YELLOW, 0.05), 12))
	_cards_box = NeonUI.vbox(10)
	add_child(_cards_box)


func _refresh() -> void:
	NeonUI.clear(_balance_box)
	var row := NeonUI.hbox(8)
	row.add_child(NeonUI.label("Баланс:", 12, Neon.TEXT_DIM))
	row.add_child(NeonUI.bold_label(Neon.fmt_dollars_full(Game.dollars), 15, Neon.YELLOW))
	row.add_child(NeonUI.stretch())
	row.add_child(NeonUI.label("продавай крипту на бирже → качайся", 10, Neon.TEXT_MUTED))
	_balance_box.add_child(row)

	NeonUI.clear(_cards_box)
	for up in Game.data.upgrades:
		_cards_box.add_child(_upgrade_card(up))


func _upgrade_card(up: UpgradeInfo) -> Control:
	var lvl := Game.upgrade_level(up.id)
	var maxed := lvl >= up.max_level
	var cost := up.cost_for_level(lvl)
	var afford := cost >= 0 and Game.dollars >= float(cost)

	var row := NeonUI.hbox(12)

	var icon := NeonUI.display_label(str(ICONS.get(up.id, "◆")), 20, Neon.CYAN)
	icon.custom_minimum_size = Vector2(40, 0)
	icon.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	row.add_child(icon)

	var info := NeonUI.vbox(2)
	info.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	var name_row := NeonUI.hbox(8)
	name_row.add_child(NeonUI.bold_label(up.name, 13, Color.WHITE))
	name_row.add_child(NeonUI.label("ур. %d/%d" % [lvl, up.max_level], 10, Neon.TEXT_MUTED))
	info.add_child(name_row)

	var desc := NeonUI.label(up.desc, 11, Neon.TEXT_DIM)
	desc.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	desc.custom_minimum_size = Vector2(380, 0)
	info.add_child(desc)

	# полоска уровней
	var pips := NeonUI.hbox(2)
	for i in range(up.max_level):
		var pip := ColorRect.new()
		pip.color = Neon.GREEN if i < lvl else Neon.WHITE_10
		pip.custom_minimum_size = Vector2(0, 5)
		pip.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		pips.add_child(pip)
	info.add_child(pips)

	var effect := "Сейчас: " + up.effect_text(lvl)
	if not maxed:
		effect += "  →  " + up.effect_text(lvl + 1)
	info.add_child(NeonUI.label(effect, 10, Neon.CYAN))
	row.add_child(info)

	if maxed:
		row.add_child(NeonUI.chip("✓ MAX", Neon.GREEN))
	else:
		var buy := NeonUI.button("$%s" % Neon.fmt_int(float(cost)), Neon.YELLOW, NeonUI.KIND_SOLID, 11)
		buy.disabled = not afford
		buy.custom_minimum_size = Vector2(96, 30)
		buy.pressed.connect(func() -> void: Game.buy_upgrade(up.id))
		row.add_child(buy)

	return NeonUI.card_with(Neon.WHITE_10, row, Neon.PANEL_BAR, 12)
