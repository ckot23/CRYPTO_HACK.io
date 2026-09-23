# Гайд: перенос CRYPTO_HACK из веб-версии (React) в Godot 4

Этот документ объясняет, **как именно** игра из корня репозитория (`src/App.tsx`,
`src/engine.ts`, `src/gameData.ts`, `src/index.css`) была перенесена на Godot 4 —
и как повторить (или продолжить) этот перенос самому.

Готовый порт уже лежит в папке `godot/`, его можно просто запустить (см. «Вариант А»).
Раздел «Вариант Б» — пошаговый перенос своими руками для тех, кто хочет понять механику
или перенести похожий проект.

---

## 0. Что было и что стало

| | Веб-версия | Godot-версия |
| --- | --- | --- |
| Язык | TypeScript / React 19 | GDScript 4 |
| Разметка | JSX + Tailwind CSS | Control-ноды (Container/Button/Label) |
| Стили | CSS-классы, box-shadow, @keyframes | `Theme`, `StyleBoxFlat`, `Tween` |
| Состояние | `useState` + `useEffect` | autoload `Game` + сигналы |
| Сохранение | `localStorage` | `user://cryptohack_save_v1.json` |
| Данные | `src/gameData.ts` | `assets/data/gamedata.json` |
| Редактор кода | `<textarea>` + самописная подсветка | `CodeEdit` + `CodeHighlighter` |
| Запуск Python | симулятор вывода (`engine.ts`) | симулятор **+ реальный Python** |
| Звук | WebAudio осциллятор | `AudioStreamWAV`, сгенерированный кодом |
| Анимации | framer-motion | `Tween` (`create_tween()`) |
| Сборка | Vite (`npm run build`) | Export в `.exe` / `.x86_64` |

Структура геймплея перенесена 1:1: 8 миссий, 4 монеты, 4 апгрейда, 5 уроков,
8 достижений, те же цифры экономики (старт $150, `XP = level × 300`,
комиссия биржи 5 %→0 %, стоимость майнера `120 + 80×N` и т. д.).

### Карта файлов

| Веб-файл | Что в нём было | Куда переехало |
| --- | --- | --- |
| `src/gameData.ts` | миссии, уроки, монеты, апгрейды, цитаты | `assets/data/gamedata.json` + модели `scripts/core/*.gd` |
| `src/engine.ts` | `simulateExecution`, `checkSyntax`, `highlightPython` | `scripts/core/py_sim.gd`, `scripts/core/py_highlighter.gd` |
| `src/App.tsx` → типы/хелперы | `GameState`, `fmtDollars`, `playBeep` | `scripts/autoload/game.gd`, `scripts/autoload/sfx.gd`, `scripts/core/neon.gd` |
| `src/App.tsx` → `StartScreen` | стартовый экран | `scripts/ui/main_menu.gd` |
| `src/App.tsx` → `BootScreen` | BIOS-строки | `scripts/ui/boot_screen.gd` |
| `src/App.tsx` → `App` | рабочий стол, топбар, панель задач, окна | `scripts/ui/desktop.gd` |
| `src/App.tsx` → `WindowFrame` | перетаскиваемое окно | `scripts/ui/neon_window.gd` |
| `src/App.tsx` → `HackWindow` | хак-терминал | `scripts/ui/hack_window.gd` |
| `src/App.tsx` → `MinerWindow`, `TradeWindow`, `UpgradeWindow`, `LearnWindow`, `FilesWindow`, `ProfileWindow` | остальные «программы» | одноимённые файлы в `scripts/ui/` |
| `src/App.tsx` → `MatrixBg` | canvas-дождь символов | `scripts/ui/matrix_bg.gd` |
| `src/App.tsx` → тосты | всплывашки | `scripts/ui/toast_layer.gd` |
| `src/index.css` | палитра, glows, scan-beam, токены подсветки | `scripts/core/neon.gd`, `scripts/ui/grid_bg.gd`, `scripts/ui/neon_ui.gd` |

---

## Вариант А. Просто запустить готовый порт

1. Установи **Godot 4.3+** (godotengine.org/download). Установка не требуется — это один файл.
2. В менеджере проектов: **Import** → выбери `godot/project.godot` → **Import & Edit**.
3. Дождись импорта (шрифты + JSON, 5–15 секунд), затем **F5**.
4. Игра откроет главное меню: «НАЧАТЬ ИГРУ» → загрузка BIOS → рабочий стол.

Если после первого запуска Godot предложит обновить проект под свою версию — соглашайся,
набор `config/features` в `project.godot` обновится автоматически.

---

## Вариант Б. Перенос своими руками (по шагам)

### Шаг 1. Создать каркас проекта

`Project → New Project`: рендерер **Compatibility** (игра — чистый 2D-интерфейс, он быстрее
запускается и работает на слабых GPU), размер окна 1280×720.

Папки:

```
scenes/  scripts/autoload/  scripts/core/  scripts/ui/  assets/data/  assets/fonts/
```

### Шаг 2. Перенести данные контента (TS → JSON)

В веб-версии все 8 миссий, 5 уроков и апгрейды лежали прямо в TypeScript. Тип данных
намного удобнее держать в JSON: правки контента не требуют лезть в код.

Способ, которым это сделано здесь — «выполнить TS в Node и сохранить JSON»:

```bash
mkdir -p /tmp/portgen && cd /tmp/portgen && npm init -y
npm i esbuild
cp /path/to/src/gameData.ts .
./node_modules/.bin/esbuild gameData.ts --format=esm --outfile=gameData.mjs
node -e "import('./gameData.mjs').then(g => require('fs').writeFileSync('gamedata.json',
  JSON.stringify({cryptos:g.CRYPTOS, missions:g.MISSIONS, upgrades:g.UPGRADES,
  lessons:g.LESSONS, quotes:g.HACKER_QUOTES}, null, 2)))"
```

Дальше в Godot: `FileAccess.get_file_as_string("res://assets/data/gamedata.json")` +
`JSON.parse_string()`. Загрузкой занимается `scripts/core/game_data.gd`, а каждая миссия
превращается в объект класса `Mission` (`scripts/core/mission.gd`):

```gdscript
var parsed = JSON.parse_string(FileAccess.get_file_as_string(path))
for m in parsed["missions"]:
    missions.append(Mission.from_dict(m))
```

> **Зачем отдельные классы, а не словари?** В GDScript автодополнение и проверки типов
> работают только с полями класса. `mission.reward_dollars` поймает опечатку,
> `m["rewardDollars"]` — молча вернёт `null`.

### Шаг 3. Перенести палитру и шрифты (CSS → Theme)

Цвета из `index.css` (`@theme`) стали константами в `scripts/core/neon.gd`:

```gdscript
static var GREEN := Color("#00ff9d")   # --color-neon
static var BG := Color("#04070f")      # --color-bg-deep
```

Дальше вместо Tailwind-классов работают три инструмента Godot:

| CSS | Godot |
| --- | --- |
| `border border-[#00ff9d]/40 bg-[#00ff9d]/5 p-3` | `StyleBoxFlat` (bg_color, border_*, content_margin_*) |
| `box-shadow: 0 0 24px rgba(...)` | `StyleBoxFlat.shadow_size` + `shadow_color` |
| `text-shadow: 0 0 8px ...` | тема `Label`: `font_outline_color` + `outline_size` |
| `@keyframes` / framer-motion | `Tween` (`create_tween()`) |
| `font-family: "JetBrains Mono"` | `Theme.default_font` + цепочка `Font.fallbacks` |

Готовые фабрики виджетов — в `scripts/ui/neon_ui.gd`:

```gdscript
var card := NeonUI.card_with(Neon.with_alpha(Neon.GREEN, 0.4), inner_box,
    Neon.with_alpha(Neon.GREEN, 0.05), 10)   # рамка + фон + отступы
var btn := NeonUI.button("▶ ЗАПУСТИТЬ", Neon.GREEN, NeonUI.KIND_SOLID, 12)
```

Тема собирается один раз в `Neon.theme()` и вешается на корневой Control каждой сцены:

```gdscript
func _ready() -> void:
    Neon.apply(self)   # theme + ThemeDB.fallback_font (кириллица в служебных элементах)
```

### Шаг 4. Перенести состояние (useState → autoload + сигналы)

React перерисовывает компоненты сам. В Godot такого нет — вместо этого:

1. `scripts/autoload/game.gd` регистрируется как autoload (Project Settings → Globals →
   Autoload, имя `Game`) и хранит всё состояние: доллары, крипту, XP, уровень, миссии,
   майнеров, апгрейды, курсы монет и историю цен.
2. Любое изменение заканчивается `state_changed.emit()` (в вебе аналог — новый объект
   состояния, на который реагируют `useEffect`).
3. Каждый экран в `_ready()` подписывается:

```gdscript
func _ready() -> void:
    Game.state_changed.connect(_refresh)
    Game.prices_changed.connect(_refresh)
```

Таймеры из веб-версии (`setInterval(..., 2500)` для цен и `1000` для майнинга) стали
двумя нодами `Timer` внутри `Game`:

```gdscript
_price_timer.timeout.connect(_tick_prices)   # каждые 2.5 сек
_mine_timer.timeout.connect(_tick_miners)    # каждую 1 сек
```

### Шаг 5. Перенести сохранения (localStorage → user://)

```gdscript
const SAVE_PATH := "user://cryptohack_save_v1.json"

func save() -> void:
    var f := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
    f.store_string(JSON.stringify(payload, "  "))
    f.close()
```

Важно: `res://` доступен только на чтение в экспортированной игре, писать можно лишь в
`user://`. Поэтому и файл сохранения, и временные файлы для Python живут в `user://`.

### Шаг 6. Перенести окна (React-компонент → Control)

`WindowFrame` из `App.tsx` стал `NeonWindow` (`scripts/ui/neon_window.gd`):
`PanelContainer` → заголовок + тело, перетаскивание за заголовок, клик поднимает окно наверх.

Ключевой приём — «менеджер окон» в `desktop.gd`:

```gdscript
func _open_window(id: String) -> void:
    var meta: Dictionary = WINDOWS[id]
    var win := NeonWindow.new()
    win.setup(str(meta["title"]), _color_for(str(meta["color_key"])), bool(meta["wide"]))
    var view: Control = (load(str(meta["script"])) as GDScript).new()
    win.set_body(view)
    _window_layer.add_child(win)
```

Каждое «приложение» — отдельный скрипт, который сам строит своё содержимое в `_ready()`
(и подписывается на сигналы `Game`). Это заменяет `{id === 'hack' && <HackWindow .../>}`.

### Шаг 7. Перенести симулятор Python

`src/engine.ts` перенесён в `scripts/core/py_sim.gd` почти дословно:

```typescript
// было (TS)
mission.requiredPatterns.forEach((pat, idx) => {
  const re = new RegExp(pat, 'm');
  if (!re.test(codeNoComments)) missing.push(mission.hints[idx]);
});
```

```gdscript
# стало (GDScript)
for idx in range(mission.required_patterns.size()):
    if not _search(mission.required_patterns[idx], body):
        missing.append(mission.hints[idx] if idx < mission.hints.size() else "…")
```

Регулярки JS-совместимы: Godot использует PCRE2, поэтому шаблоны из `gameData.ts`
(`scan\s*\(`, `wallets\s*=\s*\[`, `\bif\s+`) работают без изменений. Флаг `m` не нужен:
`RegEx.search()` ищет по всей строке, как `RegExp.test()`.

Функция `highlightPython()` из веб-версии **не переносилась вообще** — в Godot подсветку
даёт встроенный `CodeHighlighter` (см. `scripts/core/py_highlighter.gd`), и это в разы
короче: около 40 строк вместо ручного токенизатора.

### Шаг 8. (Бонус) Запускать настоящий Python

В веб-версии код студента никогда не выполнялся — выводился «поддельный» терминал.
В Godot можно запустить реальный интерпретатор (`scripts/core/py_runner.gd`):

1. код пишется в `user://py_run/exploit.py`;
2. рядом кладётся `neon_runner.py` — обёртка, которая определяет заглушки `scan()`,
   `connect()`, `brute()`, `drain()`… и выполняет код через `exec(compile(src, "exploit.py", "exec"))`,
   чтобы номера строк в ошибках совпадали с редактором;
3. процесс запускается через `OS.create_process("sh"/"cmd", [...])` с перенаправлением вывода
   в файл, а игра ждёт его завершения через `await tree.process_frame`, чтобы не морозить интерфейс;
4. через 5 секунд процесс убивается (`OS.kill`) — защита от `while True`;
5. вывод читается построчно, кодировка принудительно UTF-8 (`python -X utf8`), иначе на
   Windows русские буквы станут кракозябрами.

Если Python не найден — игра молча переключается на симулятор, поэтому ничего не ломается.

### Шаг 9. Перенести звук

`playBeep()` из `App.tsx` создавал `OscillatorNode`. В Godot осциллятора нет, но есть
`AudioStreamWAV`, который можно собрать кодом:

```gdscript
var data := PackedByteArray()
data.resize(frames * 2)
data.encode_s16(i * 2, int(clampf(sample * vol * fade, -1.0, 1.0) * 32767.0))

var stream := AudioStreamWAV.new()
stream.format = AudioStreamWAV.FORMAT_16_BITS
stream.mix_rate = 22050
stream.data = data
```

Дальше `Sfx.ui_click()`, `Sfx.hack_success()`, `Sfx.level_up()` — аналоги веб-звуков.
Пул из 8 `AudioStreamPlayer` нужен потому, что «мелодии» из нескольких нот накладываются.

### Шаг 10. Перенести анимации

| framer-motion | Godot |
| --- | --- |
| `initial={{opacity:0,y:20}} animate={{opacity:1,y:0}}` | `NeonUI.fade_in(node)` |
| `exit={{opacity:0}}` + удаление | `NeonUI.fade_out_and_free(node)` |
| `animate-pulse` | `NeonUI.pulse(node)` или `Tween.set_loops()` |
| `scan-line` CSS | `Tween` по `offset_top`/`offset_bottom` у `ColorRect` (см. `desktop.gd`) |

### Шаг 11. Сцены и поток экранов

Сцены минимальные — корневой `Control` + скрипт (весь UI строится кодом):

`scenes/main_menu.tscn` → `scenes/boot.tscn` → `scenes/desktop.tscn`.

Переходы — `get_tree().change_scene_to_file("res://scenes/desktop.tscn")`.

Такие же «пустые» сцены достаточно, чтобы потом переехать на редактирование в редакторе:
вынести построение в `.tscn` можно постепенно, узел за узлом.

### Шаг 12. Экспорт

`Project → Export…` → `Add…` → Windows Desktop (или Linux) → `Manage Export Templates → Download`
→ `Export Project`. Внутрь `.pck` попадут и JSON, и шрифты — рядом ничего не нужно.

---

## Подводные камни (найдены при этом переносе)

1. **В GDScript нет list comprehensions.** `[str(x) for x in list]` — синтаксическая ошибка.
   Пиши цикл:
   ```gdscript
   var out: Array[String] = []
   for x in list:
       out.append(str(x))
   ```
2. **`const` требует constant expression.** `Color8(1,2,3)` в `const` работает, а вот
   `Color.hex(...)` / `Color.from_rgba8(...)` — нет (это вызовы статических методов).
   Палитру проще объявить через `static var`, тем более что доступ синтаксически тот же:
   `Neon.GREEN`.
3. **`const`-словарь не может ссылаться на `static var`.** Конструкция
   `const COLORS := {"ok": Neon.GREEN}` не соберётся — тоже делай `static var`.
4. **`x as int` не работает** для встроенных типов: используй `int(x)`, `float(x)`, `str(x)`.
5. **Контейнеры перезаписывают позиции детей.** Если положить карточку в `VBoxContainer`,
   анимация `position` не сработает — контейнер пересчитает раскладку. Либо анимируй
   `modulate`/`scale`, либо раскладывай вручную (так сделано в `toast_layer.gd`).
6. **Эмодзи в шрифтах нет.** Ни JetBrains Mono, ни Unbounded не содержат эмодзи, а движок
   не тащит с собой эмодзи-шрифт. Все ⛏ 📚 👻 🏆 💡 из веб-версии заменены на безопасные
   символы (`⚙ ✎ ○ ★ ●`), которые есть в fallback-шрифте DejaVu Sans Mono.
7. **Сабсеты шрифтов.** Google раздаёт шрифты по сабсетам: `latin` не содержит кириллицу,
   `latin-ext` содержит `₿`, `greek` — `Ξ`. Один файл → «квадратики». Поэтому в `neon.gd`
   собирается цепочка `fallbacks`: latin → latin-ext → greek → cyrillic → DejaVu.
8. **`.woff2` ≠ `.ttf`.** Из веб-наборов шрифты пришлось конвертировать (`fontTools`), потому
   что Godot импортирует `.ttf`/`.otf` надёжнее всего (и шрифты нужны офлайн, без CDN).
9. **`OS.execute()` блокирует поток** и не даёт убить зацикленный скрипт студента.
   Поэтому запуск Python сделан через `OS.create_process` + опрос `OS.is_process_running`
   + `OS.kill` по таймауту, а ожидание — через `await get_tree().process_frame`.
10. **Кодировка вывода Python.** На Windows `stdout` в cp1251 → запускаем с `-X utf8`
    и читаем файл как UTF-8.
11. **Свойства CodeEdit зависят от версии** (`gutters_zero_padding`, `indent_automatic` и др.).
    Безопасный приём — выставлять только существующие:
    ```gdscript
    static func _set_if(node: Object, prop: String, value) -> void:
        if prop in node:
            node.set(prop, value)
    ```
12. **HUD нельзя пересобирать каждый тик.** Майнеры начисляют крипту раз в секунду, каждый
    такой тик шлёт `state_changed`. Если в обработчике создавать/удалять узлы, интерфейс
    начнёт мигать. Правильно: один раз создать виджеты, дальше менять `.text`
    (см. `_refresh_tickers()` в `desktop.gd`).
13. **`OS.get_name()` вместо `navigator.platform`.** Различия Windows/Linux нужны только для
    выбора `cmd`/`sh` и кандидатов интерпретатора (`python` / `py -3` / `python3`).
14. **`queue_free()` не мгновенный** — узел живёт до конца кадра. Если держишь ссылку на
    закрытое окно/меню, обнуляй её сразу, иначе получишь «Invalid access to property».
15. **Очистка контейнера с `queue_free()` даёт мигание.** Если делать
    `for c in box.get_children(): c.queue_free()` и сразу добавлять новые узлы, в текущем
    кадре старые и новые дети существуют одновременно (удаление отложено до конца кадра) —
    видно «двойные» строки. Правильно так:
    ```gdscript
    static func clear(container: Node) -> void:
        for child in container.get_children():
            container.remove_child(child)   # сразу выкинуть из дерева
            child.queue_free()              # удалить в конце кадра
    ```
    В проекте это `NeonUI.clear()` — используется везде, где список перерисовывается
    (окно майнеров обновляется каждый игровой тик).

---

## Как добавить контент

### Новая миссия

Дописываешь объект в `assets/data/gamedata.json` → массив `missions`:

```json
{
  "id": 9,
  "title": "Новая цель",
  "targetName": "Сервер банка",
  "targetIp": "10.0.0.9",
  "os": "Ubuntu 24.04",
  "security": 95,
  "difficulty": "ЭКСПЕРТ",
  "concept": "Комбинация: список + цикл + функция",
  "conceptDesc": "…",
  "briefing": "…",
  "task": "…",
  "starterCode": "# …\n",
  "solution": "for w in wallets:\n    drain(w)\n",
  "hints": ["…", "…", "…"],
  "requiredPatterns": ["drain\\s*\\(", "for\\s+"],
  "rewardCrypto": "SOL",
  "rewardAmount": 5.0,
  "rewardDollars": 2000,
  "rewardXp": 1200,
  "requiredCodeLib": 3,
  "requiredLevel": 4,
  "theory": ["…", "…", "…"]
}
```

`requiredPatterns` — обычные регулярки (PCRE2). Код перезапускать не нужно, миссия появится
в списке сама.

### Новый урок / монета / апгрейд

То же самое — массивы `lessons`, `cryptos`, `upgrades` в `gamedata.json`.
Для монеты достаточно `id`, `icon`, `color`, `basePrice`, `unlockLevel`, `volatility` —
котировки, разблокировка и майнинг подхватятся автоматически.

### Хочешь заменить шрифт

Положи `.ttf` в `assets/fonts/` и добавь его в цепочку в `scripts/core/neon.gd`.
Проверить, что нужные символы есть в шрифте, можно так:

```bash
pip install fonttools
python -c "from fontTools.ttLib import TTFont; cmap=TTFont('assets/fonts/MyFont.ttf').getBestCmap();
print('★' in [chr(c) for c in cmap])"
```

---

## Проверка кода без запуска Godot

Синтаксис GDScript можно проверить из консоли (пригодится, если правишь много файлов сразу):

```bash
pip install gdtoolkit
gdparse scripts/ui/hack_window.gd      # только парсер: молчит = ошибок нет
gdlint scripts/                        # стиль: длина строк, имена переменных
```

`gdparse` ловит именно синтаксические ошибки — например, тот самый list comprehension,
который невозможно было увидеть в диффе. Он **не** проверяет существование методов и
ресурсов, поэтому «а есть ли такой метод у Neon?» лучше сверять глазами по файлу класса.

---

## Отличия Godot-версии от веб-версии

1. **Реальный Python** (кнопка переключения режима в хак-терминале) — то, чего в браузере
   не было: студент видит настоящий `stdout` и настоящие `SyntaxError`.
2. **Эмодзи заменены** на монохромные символы (см. пункт 6 в подводных камнях).
3. **Сохранения лежат в файле**, а не в localStorage — прогресс не теряется при чистке
   браузера, зато у веб- и десктоп-версий свои независимые сохранения.
4. **Окна не «резиновые»**: контент, который не помещается, прокручивается внутри окна
   (`ScrollContainer` в `NeonWindow`), а не растягивает его.
5. **Экспорт**: одна папка → один `.exe` (или `.x86_64` на Linux) без сервера и Node.js.

---

## Чек-лист первого запуска

- [ ] Godot 4.3+ скачан, проект `godot/project.godot` импортирован.
- [ ] В логе нет красных ошибок вида `Parse Error` (значит, версия движка совпала).
- [ ] Главное меню показывает заголовок CRYPTO_HACK и цитату внизу.
- [ ] «НАЧАТЬ ИГРУ» → BIOS-строки → рабочий стол с открытым хак-терминалом.
- [ ] В миссии 1 вводишь `scan()` → «ЗАПУСТИТЬ» → в консоли `[SCAN] …` и «ВЗЛОМ УСПЕШЕН».
- [ ] Топбар показывает `$150` и `ур.1 · 0/300 XP` (значит, `Game` загрузился).
- [ ] Меню «GHOST» (слева снизу) открывается, звук переключается.
- [ ] После перезапуска игры прогресс сохранился (миссия 1 осталась пройденной).

Если что-то не так — смотри раздел «Подводные камни» и блок «Частые вопросы»
в `godot/README.md`.
