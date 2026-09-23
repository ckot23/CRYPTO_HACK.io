class_name UpgradeInfo
extends RefCounted

## Апгрейд «чёрного рынка». Перенос интерфейса Upgrade из src/gameData.ts.

var id: String = ""
var name: String = ""
var desc: String = ""
var icon: String = ""
var max_level: int = 0
var costs: Array[int] = []
var effects: Array[String] = []


static func from_dict(d: Dictionary) -> UpgradeInfo:
	var u := UpgradeInfo.new()
	u.id = str(d.get("id", ""))
	u.name = str(d.get("name", ""))
	u.desc = str(d.get("desc", ""))
	u.icon = str(d.get("icon", ""))
	u.max_level = int(d.get("maxLevel", 0))
	for c in d.get("costs", []):
		u.costs.append(int(c))
	for e in d.get("effects", []):
		u.effects.append(str(e))
	return u


## Стоимость следующего уровня (-1, если уже максимум).
func cost_for_level(level: int) -> int:
	if level >= max_level or level < 0 or level >= costs.size():
		return -1
	return costs[level]


func effect_text(level: int) -> String:
	if level < 0 or level >= effects.size():
		return "—"
	return effects[level]
