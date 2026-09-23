class_name Mission
extends RefCounted

## Миссия-взлом. Перенос интерфейса Mission из src/gameData.ts.
## required_patterns — те же регулярки, что и в веб-версии (движок RegEx = PCRE2,
## JS-совместимые шаблоны из gameData.ts работают без изменений).

var id: int = 0
var title: String = ""
var target_name: String = ""
var target_ip: String = ""
var os_name: String = ""
var security: int = 0
var difficulty: String = ""
var concept: String = ""
var concept_desc: String = ""
var briefing: String = ""
var task: String = ""
var starter_code: String = ""
var solution: String = ""
var hints: Array[String] = []
var required_patterns: Array[String] = []
var reward_crypto: String = "BTC"
var reward_amount: float = 0.0
var reward_dollars: float = 0.0
var reward_xp: int = 0
var required_code_lib: int = 0
var required_level: int = 1
var theory: Array[String] = []


static func from_dict(d: Dictionary) -> Mission:
	var m := Mission.new()
	m.id = int(d.get("id", 0))
	m.title = str(d.get("title", ""))
	m.target_name = str(d.get("targetName", ""))
	m.target_ip = str(d.get("targetIp", ""))
	m.os_name = str(d.get("os", ""))
	m.security = int(d.get("security", 0))
	m.difficulty = str(d.get("difficulty", ""))
	m.concept = str(d.get("concept", ""))
	m.concept_desc = str(d.get("conceptDesc", ""))
	m.briefing = str(d.get("briefing", ""))
	m.task = str(d.get("task", ""))
	m.starter_code = str(d.get("starterCode", ""))
	m.solution = str(d.get("solution", ""))
	for h in d.get("hints", []):
		m.hints.append(str(h))
	for p in d.get("requiredPatterns", []):
		m.required_patterns.append(str(p))
	m.reward_crypto = str(d.get("rewardCrypto", "BTC"))
	m.reward_amount = float(d.get("rewardAmount", 0.0))
	m.reward_dollars = float(d.get("rewardDollars", 0.0))
	m.reward_xp = int(d.get("rewardXp", 0))
	m.required_code_lib = int(d.get("requiredCodeLib", 0))
	m.required_level = int(d.get("requiredLevel", 1))
	for line in d.get("theory", []):
		m.theory.append(str(line))
	return m


func reward_title() -> String:
	return "+%s %s" % [Neon.fmt_crypto(reward_amount), reward_crypto]
