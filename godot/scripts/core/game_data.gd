class_name GameData
extends RefCounted

## Загрузчик игровых данных из res://assets/data/gamedata.json.
##
## В веб-версии данные лежали прямо в TypeScript (src/gameData.ts).
## Здесь они вынесены в JSON: правки контента (миссии, уроки, цены) не требуют
## трогать код — см. docs/PORTING_GUIDE.md, раздел «Как добавить миссию».

const DATA_PATH := "res://assets/data/gamedata.json"

var cryptos: Array[CryptoInfo] = []
var missions: Array[Mission] = []
var upgrades: Array[UpgradeInfo] = []
var lessons: Array[Lesson] = []
var quotes: Array[String] = []
var load_error: String = ""


func load_all(path: String = DATA_PATH) -> bool:
	if not FileAccess.file_exists(path):
		load_error = "Не найден файл данных: " + path
		push_error(load_error)
		return false

	var text := FileAccess.get_file_as_string(path)
	var parsed = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		load_error = "Некорректный JSON в " + path
		push_error(load_error)
		return false

	var data: Dictionary = parsed
	for c in data.get("cryptos", []):
		cryptos.append(CryptoInfo.from_dict(c))
	for m in data.get("missions", []):
		missions.append(Mission.from_dict(m))
	for u in data.get("upgrades", []):
		upgrades.append(UpgradeInfo.from_dict(u))
	for l in data.get("lessons", []):
		lessons.append(Lesson.from_dict(l))
	for q in data.get("quotes", []):
		quotes.append(str(q))
	return true


func crypto(id: String) -> CryptoInfo:
	for c in cryptos:
		if c.id == id:
			return c
	return null


func mission(id: int) -> Mission:
	for m in missions:
		if m.id == id:
			return m
	return null


func upgrade(id: String) -> UpgradeInfo:
	for u in upgrades:
		if u.id == id:
			return u
	return null


func lesson(id: int) -> Lesson:
	for l in lessons:
		if l.id == id:
			return l
	return null


## Иконка монеты. Если в шрифте нет символа (₿ Ξ ɱ ◎) — используем буквенный код,
## чтобы в интерфейсе не было «квадратиков». См. docs/PORTING_GUIDE.md.
static func crypto_icon(c: CryptoInfo) -> String:
	return c.icon
