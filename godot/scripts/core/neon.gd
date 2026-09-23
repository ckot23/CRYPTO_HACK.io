class_name Neon
extends RefCounted

## Палитра, шрифты, стили и форматирование чисел.
##
## Прямой перенос визуального языка веб-версии (src/index.css + Tailwind-классы)
## в один статический класс. Все UI-скрипты берут цвета/шрифты только отсюда —
## так проще менять вид игры в одном месте.

# ============ ЦВЕТА (index.css -> @theme) ============
static var BG := Color("#04070f")          # --color-bg-deep
static var PANEL := Color("#0a111f")       # --color-panel
static var PANEL_DARK := Color("#070d1a")  # фон окон
static var PANEL_DEEP := Color("#050b16")  # тёмные списки
static var PANEL_HEAD := Color("#0b1322")  # заголовок окна
static var PANEL_BAR := Color("#0a1120")   # топбар/статусбар

static var GREEN := Color("#00ff9d")       # --color-neon
static var GREEN_HOVER := Color("#5cffb8")
static var CYAN := Color("#00e5ff")        # --color-cyber
static var CYAN_HOVER := Color("#7df3ff")
static var PINK := Color("#ff2d78")        # --color-pink-neon
static var YELLOW := Color("#ffe600")      # --color-amber-neon
static var YELLOW_HOVER := Color("#fff36b")
static var ORANGE := Color("#ff9f43")

static var TEXT := Color("#d7e3f4")
static var TEXT_SOFT := Color("#b9c7dd")
static var TEXT_DIM := Color("#8fa3bd")
static var TEXT_MUTED := Color("#5b6b85")
static var TEXT_FAINT := Color("#3a4a63")
static var BLACK := Color("#000000")

static var WHITE_05 := Color(1, 1, 1, 0.05)
static var WHITE_10 := Color(1, 1, 1, 0.10)
static var WHITE_15 := Color(1, 1, 1, 0.15)
static var WHITE_20 := Color(1, 1, 1, 0.20)

# ============ ШРИФТЫ ============
# Шрифты нарезаны на сабсеты (latin / cyrillic / latin-ext / greek), поэтому
# основной файл подключается с цепочкой fallback-шрифтов. Подробности — docs/PORTING_GUIDE.md

const FONTS_DIR := "res://assets/fonts/"

static var _theme: Theme


static func _load_font(path: String) -> Font:
	if not ResourceLoader.exists(path):
		push_warning("Шрифт не найден: " + path)
		return null
	return load(path) as Font


static func _chain(paths: Array) -> Font:
	# Первый шрифт — основной, остальные — fallback по отсутствующим глифам.
	var base: Font = null
	var fallbacks: Array[Font] = []
	for p in paths:
		var f := _load_font(p)
		if f == null:
			continue
		if base == null:
			base = f
		else:
			fallbacks.append(f)
	if base == null:
		return ThemeDB.fallback_font
	if not fallbacks.is_empty():
		base.fallbacks = fallbacks
	return base


## Основной моношрифт (код, терминал, весь интерфейс).
static func font_main() -> Font:
	return _chain([
		FONTS_DIR + "JetBrainsMono-Regular.ttf",
		FONTS_DIR + "JetBrainsMono-LatinExt.ttf",   # ₿ (bitcoin), ₽
		FONTS_DIR + "JetBrainsMono-Greek.ttf",      # Ξ (ethereum)
		FONTS_DIR + "JetBrainsMono-Cyrillic.ttf",   # русский текст
		FONTS_DIR + "DejaVuSansMono.ttf",           # ɱ, ◎, ★, ▶, ✓, рамки ─│└
	])


## Жирный вариант основного шрифта.
static func font_bold() -> Font:
	return _chain([
		FONTS_DIR + "JetBrainsMono-Bold.ttf",
		FONTS_DIR + "JetBrainsMono-BoldLatinExt.ttf",
		FONTS_DIR + "JetBrainsMono-BoldGreek.ttf",
		FONTS_DIR + "JetBrainsMono-BoldCyrillic.ttf",
		FONTS_DIR + "DejaVuSansMono-Bold.ttf",
	])


## Заголовочный шрифт (логотип, заголовки модалок).
static func font_display() -> Font:
	return _chain([
		FONTS_DIR + "Unbounded-Bold.ttf",
		FONTS_DIR + "Unbounded-BoldCyrillic.ttf",
	])


## Самый тяжёлый вариант заголовочного шрифта (надпись CRYPTO_HACK).
static func font_display_black() -> Font:
	return _chain([
		FONTS_DIR + "Unbounded-Black.ttf",
		FONTS_DIR + "Unbounded-BlackCyrillic.ttf",
	])


# ============ ФОРМАТИРОВАНИЕ ЧИСЕЛ (перенос helpers из App.tsx) ============
static func _group_digits(int_str: String) -> String:
	# 1234567 -> "1 234 567" (аналог toLocaleString('ru-RU'))
	var negative := int_str.begins_with("-")
	var digits := int_str.substr(1) if negative else int_str
	var out := ""
	var count := 0
	for i in range(digits.length() - 1, -1, -1):
		out = digits[i] + out
		count += 1
		if count % 3 == 0 and i > 0:
			out = " " + out
	return ("-" + out) if negative else out


static func fmt_dollars(n: float) -> String:
	if n >= 10000.0:
		return "$%.1fK" % (n / 1000.0)
	if n < 100.0:
		return "$%.2f" % n
	return "$%.0f" % n


static func fmt_dollars_full(n: float) -> String:
	var s := "%.2f" % absf(n)
	var parts := s.split(".")
	return ("-$" if n < 0.0 else "$") + _group_digits(parts[0]) + "," + parts[1]


static func fmt_crypto(n: float) -> String:
	if is_zero_approx(n):
		return "0"
	if absf(n) < 0.000001:
		return "%.2e" % n
	if absf(n) < 0.01:
		return "%.6f" % n
	if absf(n) < 1.0:
		return "%.4f" % n
	return "%.3f" % n


static func fmt_price(n: float) -> String:
	return "$" + _group_digits("%.0f" % n)


static func fmt_int(n: float) -> String:
	return _group_digits("%.0f" % n)


# ============ СТИЛИ (StyleBoxFlat вместо CSS box-shadow) ============
## Плоский прямоугольник с рамкой и «неоновым» свечением через shadow.
static func sb(bg: Color, border: Color = Color(0, 0, 0, 0), border_width: int = 1, glow: float = 0.0) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = bg
	s.border_color = border
	s.set_border_width_all(border_width)
	s.set_content_margin_all(8)
	if glow > 0.0:
		s.shadow_color = Color(border.r, border.g, border.b, 0.35 * glow)
		s.shadow_size = int(10.0 * glow)
		s.shadow_offset = Vector2.ZERO
	return s


static func sb_borderless(bg: Color, margin: int = 0) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = bg
	s.set_border_width_all(0)
	s.set_content_margin_all(margin)
	return s


static func sb_empty() -> StyleBoxEmpty:
	return StyleBoxEmpty.new()


# ============ ТЕМА ============
## Общая тема: подключается к корневому Control каждой сцены.
## Аналог глобальных стилей Tailwind в веб-версии.
static func theme() -> Theme:
	if _theme != null:
		return _theme

	var t := Theme.new()
	var main := font_main()
	var bold := font_bold()
	t.default_font = main
	t.default_font_size = 14

	# --- Label ---
	t.set_color("font_color", "Label", TEXT)

	# --- Label / Button / LineEdit ---
	var panel_style := sb(PANEL_DARK, Color(GREEN.r, GREEN.g, GREEN.b, 0.18), 1)
	t.set_stylebox("panel", "PanelContainer", panel_style)
	t.set_stylebox("panel", "Panel", panel_style)
	t.set_stylebox("panel", "ScrollContainer", sb(PANEL_DEEP, Color(0, 0, 0, 0), 0))
	t.set_stylebox("panel", "PopupPanel", sb(PANEL, GREEN, 1))

	# --- Button ---
	var btn_normal := sb(Color(GREEN.r, GREEN.g, GREEN.b, 0.15), Color(GREEN.r, GREEN.g, GREEN.b, 0.45), 1)
	var btn_hover := sb(Color(GREEN.r, GREEN.g, GREEN.b, 0.28), GREEN, 1)
	var btn_pressed := sb(Color(GREEN.r, GREEN.g, GREEN.b, 0.42), GREEN, 1)
	var btn_disabled := sb(Color(1, 1, 1, 0.06), WHITE_10, 1)
	t.set_stylebox("normal", "Button", btn_normal)
	t.set_stylebox("hover", "Button", btn_hover)
	t.set_stylebox("pressed", "Button", btn_pressed)
	t.set_stylebox("disabled", "Button", btn_disabled)
	t.set_stylebox("focus", "Button", sb_empty())
	t.set_color("font_color", "Button", GREEN)
	t.set_color("font_hover_color", "Button", Color.WHITE)
	t.set_color("font_pressed_color", "Button", Color.WHITE)
	t.set_color("font_disabled_color", "Button", TEXT_MUTED)
	t.set_font("font", "Button", bold)

	# --- LineEdit ---
	var le_normal := sb(BLACK, WHITE_15, 1)
	var le_focus := sb(BLACK, GREEN, 1)
	t.set_stylebox("normal", "LineEdit", le_normal)
	t.set_stylebox("focus", "LineEdit", le_focus)
	t.set_stylebox("read_only", "LineEdit", le_normal)
	t.set_color("font_color", "LineEdit", Color.WHITE)
	t.set_color("font_placeholder_color", "LineEdit", TEXT_MUTED)
	t.set_color("caret_color", "LineEdit", GREEN)
	t.set_color("selection_color", "LineEdit", Color(GREEN.r, GREEN.g, GREEN.b, 0.25))

	# --- TextEdit / CodeEdit (редактор кода) ---
	t.set_stylebox("normal", "CodeEdit", sb(Color("#000000"), WHITE_10, 1))
	t.set_stylebox("focus", "CodeEdit", sb(Color("#000000"), Color(GREEN.r, GREEN.g, GREEN.b, 0.5), 1))
	t.set_stylebox("read_only", "CodeEdit", sb(PANEL_DEEP, WHITE_10, 1))
	t.set_color("font_color", "CodeEdit", TEXT)
	t.set_color("caret_color", "CodeEdit", GREEN)
	t.set_color("line_number_color", "CodeEdit", TEXT_FAINT)
	t.set_color("current_line_color", "CodeEdit", WHITE_05)
	t.set_color("selection_color", "CodeEdit", Color(CYAN.r, CYAN.g, CYAN.b, 0.25))
	t.set_font_size("font_size", "CodeEdit", 14)

	# --- RichTextLabel (консоль, тексты миссий) ---
	t.set_color("default_color", "RichTextLabel", TEXT)
	t.set_font("normal_font", "RichTextLabel", main)
	t.set_font("bold_font", "RichTextLabel", bold)
	t.set_font("mono_font", "RichTextLabel", main)
	t.set_constant("line_separation", "RichTextLabel", 3)

	# --- Прогресс-бар (опыт) ---
	t.set_stylebox("background", "ProgressBar", sb(Color("#0f1a2e"), WHITE_10, 1))
	t.set_stylebox("fill", "ProgressBar", sb(GREEN, Color(0, 0, 0, 0), 0))
	t.set_color("font_color", "ProgressBar", Color.WHITE)

	# --- Скроллбары ---
	var track := sb(PANEL_DARK, Color(0, 0, 0, 0), 0)
	var grabber := sb(Color("#1b2942"), Color(0, 0, 0, 0), 0)
	var grabber_hi := sb(GREEN, Color(0, 0, 0, 0), 0)
	for type_name in ["VScrollBar", "HScrollBar"]:
		t.set_stylebox("scroll", type_name, track)
		t.set_stylebox("grabber", type_name, grabber)
		t.set_stylebox("grabber_highlight", type_name, grabber_hi)
		t.set_stylebox("grabber_pressed", type_name, grabber_hi)

	_theme = t
	return t


## Подключить тему и шрифт к конкретному узлу-корню.
static func apply(root: Control) -> void:
	root.theme = theme()
	# Запасной шрифт движка — чтобы кириллица/символы рисовались даже в
	# служебных элементах (тултипы, меню редактора).
	ThemeDB.fallback_font = font_main()
	ThemeDB.fallback_font_size = 14


# ============ ПРОЧИЕ ПОМОЩНИКИ ============
## Цвет свечения текста: имитируем CSS text-shadow через Label-«ореол» в UI-хелперах.
static func glow(color: Color, strength: float = 0.8) -> Color:
	return Color(color.r, color.g, color.b, strength)


static func with_alpha(c: Color, a: float) -> Color:
	return Color(c.r, c.g, c.b, a)


static func mix(a: Color, b: Color, t: float) -> Color:
	return a.lerp(b, t)


static func brightness(c: Color, mul: float) -> Color:
	return Color(minf(c.r * mul, 1.0), minf(c.g * mul, 1.0), minf(c.b * mul, 1.0), c.a)
