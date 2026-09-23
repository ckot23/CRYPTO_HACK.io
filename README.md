# CRYPTO_HACK

Симулятор хакера с обучением Python: взламывай цели, пиши код в терминале, ставь майнеры
на чужие компы, торгуй криптой на бирже и прокачивайся от «скрипт-кидди» до легенды даркнета.

В репозитории **две версии одной игры**:

| Версия | Где лежит | Технологии | Как запустить |
| --- | --- | --- | --- |
| 🌐 Веб | `src/`, `index.html` | React 19 + TypeScript + Vite + Tailwind | `npm install`, `npm run dev` |
| 🎮 Godot (десктоп) | `godot/` | Godot 4 + GDScript | открыть `godot/project.godot` в Godot 4.3+ и нажать F5 |

Обе версии используют один и тот же контент (8 миссий, 5 уроков, 4 монеты, 4 апгрейда,
биржевая экономика) и одинаковые цифры баланса.

## Веб-версия

```bash
npm install
npm run dev      # http://localhost:5173
npm run build    # сборка в dist/ (один index.html файл)
```

## Godot-версия

Кратко: скачай Godot 4.3+ (godotengine.org/download) → **Import** → выбери
`godot/project.godot` → **Import & Edit** → **F5**.

Что нового по сравнению с веб-версией: код в миссиях может выполняться **настоящим
Python** (если он установлен в системе), есть сохранения в файл, экспорт в один `.exe`
и нормальная работа офлайн.

* `godot/README.md` — как запустить, структура проекта, управление, FAQ.
* `godot/docs/PORTING_GUIDE.md` — **подробный гайд по переносу веб-игры на Godot**:
  карта соответствий файлов, пошаговый перенос (данные → JSON, CSS → Theme,
  useState → autoload + сигналы, WebAudio → AudioStreamWAV, framer-motion → Tween),
  подводные камни GDScript и инструкция «как добавить миссию».

## Структура репозитория

```
├── src/                     веб-версия: App.tsx (UI), engine.ts (симулятор Python), gameData.ts (контент)
├── godot/                   Godot-версия игры
│   ├── scenes/              3 сцены: main_menu → boot → desktop
│   ├── scripts/autoload/    Game (состояние/экономика), Sfx (звуки)
│   ├── scripts/core/        палитра/тема, данные, симулятор и раннер Python, подсветка
│   ├── scripts/ui/          экраны и окна (хак-терминал, биржа, майнеры, школа, файлы, профиль)
│   ├── assets/data/         gamedata.json — весь контент игры
│   ├── assets/fonts/        JetBrains Mono, Unbounded, DejaVu Sans Mono (+ лицензии)
│   └── docs/PORTING_GUIDE.md  гайд по переносу
└── package.json             зависимости веб-версии
```

## Контент игры

Все миссии, уроки, монеты и апгрейды описаны в `godot/assets/data/gamedata.json`
(для Godot) и в `src/gameData.ts` (для веб-версии). Требования к коду миссии проверяются
регулярными выражениями из поля `requiredPatterns`.
