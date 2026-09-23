class_name Lesson
extends RefCounted

## Урок «Школы Python» + мини-тест. Перенос интерфейса Lesson из src/gameData.ts.

var id: int = 0
var title: String = ""
var subtitle: String = ""
var duration: String = ""
var xp: int = 0
## Блоки теории: [{ heading: String, text: String, code: String }]
var content: Array[Dictionary] = []
## Вопросы: [{ q: String, options: Array, answer: int }]
var quiz: Array[Dictionary] = []


static func from_dict(d: Dictionary) -> Lesson:
	var l := Lesson.new()
	l.id = int(d.get("id", 0))
	l.title = str(d.get("title", ""))
	l.subtitle = str(d.get("subtitle", ""))
	l.duration = str(d.get("duration", ""))
	l.xp = int(d.get("xp", 0))
	for block in d.get("content", []):
		var b: Dictionary = block
		l.content.append({
			"heading": str(b.get("heading", "")),
			"text": str(b.get("text", "")),
			"code": str(b.get("code", "")),
		})
	for q in d.get("quiz", []):
		var qd: Dictionary = q
		var opts: Array[String] = []
		for o in qd.get("options", []):
			opts.append(str(o))
		l.quiz.append({
			"q": str(qd.get("q", "")),
			"options": opts,
			"answer": int(qd.get("answer", 0)),
		})
	return l
