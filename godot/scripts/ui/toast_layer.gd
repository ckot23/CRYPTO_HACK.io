class_name ToastLayer
extends Control

## Всплывающие уведомления справа снизу (порт блока toasts из App.tsx).
##
## Важный нюанс Godot: контейнеры (VBoxContainer и т.п.) сами расставляют детей,
## поэтому анимация position у них не работает. Здесь раскладка делается вручную,
## чтобы можно было свободно анимировать появление карточек — см. docs/PORTING_GUIDE.md.

const CARD_WIDTH := 300.0
const GAP := 8.0
const BOTTOM_MARGIN := 14.0
const RIGHT_MARGIN := 14.0
const MAX_TOASTS := 4
const LIFETIME := 4.2


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE


static func color_for_kind(kind: String) -> Color:
	match kind:
		"ok": return Neon.GREEN
		"err": return Neon.PINK
		"gold": return Neon.YELLOW
		_: return Neon.CYAN


func push_toast(title: String, text: String, kind: String = "info") -> void:
	var color := color_for_kind(kind)

	var inner := NeonUI.vbox(2)
	inner.add_child(NeonUI.bold_label(title, 12, color))
	var body := NeonUI.label(text, 12, Neon.TEXT_SOFT)
	body.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	body.custom_minimum_size = Vector2(CARD_WIDTH - 24.0, 0)
	inner.add_child(body)

	var card := NeonUI.card_with(color, inner, Neon.PANEL_DARK, 10, 0.6)
	card.mouse_filter = Control.MOUSE_FILTER_IGNORE
	card.custom_minimum_size = Vector2(CARD_WIDTH, 0)
	add_child(card)

	while get_child_count() > MAX_TOASTS:
		var oldest := get_child(0)
		remove_child(oldest)
		oldest.queue_free()
		await get_tree().process_frame

	_relayout()

	# появление: выезд справа + проявление
	var target_x := card.position.x
	card.modulate.a = 0.0
	card.position.x = target_x + 42.0
	var tween := card.create_tween()
	tween.set_parallel(true)
	tween.tween_property(card, "modulate:a", 1.0, 0.18)
	tween.tween_property(card, "position:x", target_x, 0.22) \
		.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)

	await get_tree().create_timer(LIFETIME).timeout
	if is_instance_valid(card):
		var out := card.create_tween()
		out.tween_property(card, "modulate:a", 0.0, 0.22)
		out.tween_callback(card.queue_free)
		out.tween_callback(_relayout)


func _relayout() -> void:
	var y := size.y - BOTTOM_MARGIN
	for i in range(get_child_count() - 1, -1, -1):
		var card := get_child(i) as Control
		if card == null or not is_instance_valid(card):
			continue
		var h := card.get_combined_minimum_size().y
		card.size = Vector2(CARD_WIDTH, h)
		card.position = Vector2(size.x - CARD_WIDTH - RIGHT_MARGIN, y - h)
		y -= h + GAP


func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		_relayout()
