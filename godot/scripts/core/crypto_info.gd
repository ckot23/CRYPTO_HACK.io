class_name CryptoInfo
extends RefCounted

## Описание монеты. Перенос интерфейса CryptoInfo из src/gameData.ts

var id: String = ""
var name: String = ""
var full_name: String = ""
var icon: String = ""
var color: Color = Color.WHITE
var base_price: float = 0.0
var unlock_level: int = 1
var desc: String = ""
var volatility: float = 0.01


static func from_dict(d: Dictionary) -> CryptoInfo:
	var c := CryptoInfo.new()
	c.id = str(d.get("id", ""))
	c.name = str(d.get("name", ""))
	c.full_name = str(d.get("fullName", c.name))
	c.icon = str(d.get("icon", c.id.substr(0, 1)))
	c.color = Color(str(d.get("color", "#ffffff")))
	c.base_price = float(d.get("basePrice", 0.0))
	c.unlock_level = int(d.get("unlockLevel", 1))
	c.desc = str(d.get("desc", ""))
	c.volatility = float(d.get("volatility", 0.01))
	return c
