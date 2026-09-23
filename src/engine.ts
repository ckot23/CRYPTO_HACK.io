import type { Mission } from './gameData';

export interface ExecLine {
  text: string;
  kind: 'cmd' | 'ok' | 'info' | 'warn' | 'err' | 'code';
  delay: number;
}

export interface ExecResult {
  success: boolean;
  logs: ExecLine[];
  missing: string[];
  styleScore: number; // 1-3 stars
}

// Симулятор выполнения Python-кода для миссии
export function simulateExecution(code: string, mission: Mission, hackSpeedLevel: number): ExecResult {
  const logs: ExecLine[] = [];
  const lines = code.split('\n').map(l => l.trimEnd());
  const codeNoComments = lines.filter(l => l.trim() !== '' && !l.trim().startsWith('#')).join('\n');

  // Проверка паттернов
  const missing: string[] = [];
  mission.requiredPatterns.forEach((pat, idx) => {
    const re = new RegExp(pat, 'm');
    if (!re.test(codeNoComments)) missing.push(mission.hints[idx] || `Требование ${idx + 1} не выполнено`);
  });

  // Пустой код
  if (codeNoComments.trim() === '') {
    return {
      success: false,
      logs: [{ text: '⚠ Пустой скрипт. Напиши код и попробуй снова.', kind: 'warn', delay: 200 }],
      missing: ['Напиши код в редакторе'],
      styleScore: 0,
    };
  }

  // Проверка синтаксических ошибок-ловушек
  const syntaxErr = checkSyntax(codeNoComments);
  if (syntaxErr) {
    return {
      success: false,
      logs: [
        { text: `$ python3 exploit.py --target ${mission.targetIp}`, kind: 'cmd', delay: 250 },
        { text: `  File "exploit.py", line ${syntaxErr.line}`, kind: 'err', delay: 350 },
        { text: `SyntaxError: ${syntaxErr.msg}`, kind: 'err', delay: 300 },
        { text: '💡 Подсказка: проверь двоеточия, отступы и кавычки.', kind: 'info', delay: 200 },
      ],
      missing: [syntaxErr.msg],
      styleScore: 0,
    };
  }

  const speedDiv = 1 + hackSpeedLevel * 0.35;
  const d = (ms: number) => Math.max(120, Math.round(ms / speedDiv));

  logs.push({ text: `$ python3 exploit.py --target ${mission.targetIp}`, kind: 'cmd', delay: d(400) });
  logs.push({ text: `[*] Инициализация NeonHack Framework v3.7...`, kind: 'info', delay: d(450) });

  // Генерация логов на основе того, что написано в коде
  const has = (re: string) => new RegExp(re, 'm').test(codeNoComments);

  if (has('scan\\s*\\(')) {
    logs.push({ text: '[SCAN] Сканирование подсети 192.168.0.0/24 ...', kind: 'info', delay: d(600) });
    logs.push({ text: `[SCAN] Найдено 4 узла. Цель: ${mission.targetIp} (${mission.os}) ✓`, kind: 'ok', delay: d(550) });
  }
  if (has('print\\s*\\(')) {
    const m = codeNoComments.match(/print\s*\(\s*(["']?)(.*?)\1\s*\)/);
    const val = m ? m[2].slice(0, 60) : '...';
    // если печатают переменную target/ip
    if (/print\s*\(\s*(target|ip|wallets|key)\s*\)/.test(codeNoComments)) {
      logs.push({ text: `[OUT] ${mission.targetIp} :: ${mission.targetName}`, kind: 'info', delay: d(350) });
    } else {
      logs.push({ text: `[OUT] ${val}`, kind: 'info', delay: d(350) });
    }
  }
  if (has('\\bconnect\\s*\\(')) {
    logs.push({ text: `[NET] Подключение к ${mission.targetIp}:22 ...`, kind: 'info', delay: d(600) });
    logs.push({ text: '[NET] Туннель установлен. Обход firewall... OK', kind: 'ok', delay: d(550) });
  }
  if (has('\\bbrute\\s*\\(')) {
    const rangeM = codeNoComments.match(/range\s*\(\s*(\d+)/);
    const n = rangeM ? Math.min(parseInt(rangeM[1]), 12) : 5;
    logs.push({ text: `[BRUTE] Перебор ${n} комбинаций...`, kind: 'info', delay: d(500) });
    for (let i = 0; i < Math.min(n, 5); i++) {
      logs.push({ text: `  попытка ${i} ... неверно`, kind: 'info', delay: d(220) });
    }
    if (has('for\\s+')) {
      logs.push({ text: `[BRUTE] Пароль подобран! Цикл for сработал идеально ✓`, kind: 'ok', delay: d(500) });
    }
  }
  if (has('\\bif\\s+')) {
    logs.push({ text: '[CHECK] Проверка условия...', kind: 'info', delay: d(400) });
    logs.push({ text: '[CHECK] Условие True → выполняю блок if ✓', kind: 'ok', delay: d(400) });
  }
  if (has('else\\s*:')) {
    logs.push({ text: '[CHECK] Ветка else готова (защита от ошибок) ✓', kind: 'info', delay: d(250) });
  }
  if (has('\\bwallets\\s*=\\s*\\[')) {
    logs.push({ text: '[DATA] Список кошельков загружен: 3 адреса', kind: 'info', delay: d(400) });
    logs.push({ text: '  ├ 0xA1 ... 0xB2 ... 0xC3', kind: 'info', delay: d(300) });
  }
  if (has('\\bdrain\\s*\\(')) {
    logs.push({ text: '[DRAIN] Извлечение средств из кошельков...', kind: 'info', delay: d(600) });
    logs.push({ text: '[DRAIN] 0xA1 drained ✓ | 0xB2 drained ✓ | 0xC3 drained ✓', kind: 'ok', delay: d(600) });
  }
  if (has('def\\s+\\w+')) {
    const fnM = codeNoComments.match(/def\s+(\w+)/);
    logs.push({ text: `[FUNC] Функция ${fnM ? fnM[1] : ''}() скомпилирована ✓`, kind: 'ok', delay: d(400) });
  }
  if (has('\\bbypass\\s*\\(')) logs.push({ text: '[EVADE] Антивирус обойдён, EDR ослеплён ✓', kind: 'ok', delay: d(500) });
  if (has('\\bdecrypt\\s*\\(')) logs.push({ text: '[CRYPT] AES-256 ключи восстановлены, сид-фраза расшифрована ✓', kind: 'ok', delay: d(650) });
  if (has('\\bextract\\s*\\(')) logs.push({ text: '[EXTRACT] Приватные ключи извлечены ✓', kind: 'ok', delay: d(550) });
  if (has('while\\s+')) {
    logs.push({ text: '[LOOP] while-цикл запущен, флаг mining = True', kind: 'info', delay: d(400) });
  }
  if (has('install_miner\\s*\\(')) {
    logs.push({ text: '[MINER] Загрузка xmrig-neon... компиляция...', kind: 'info', delay: d(600) });
    logs.push({ text: '[MINER] Майнер внедрён в автозагрузку, скрыт от диспетчера ✓', kind: 'ok', delay: d(600) });
  }

  if (missing.length > 0) {
    logs.push({ text: '[!] Эксплойт завершён с ошибками.', kind: 'warn', delay: d(350) });
    logs.push({ text: `[!] Цель не взломана. Не хватает шагов: ${missing.length}`, kind: 'err', delay: d(300) });
    logs.push({ text: '💡 Открой вкладку «Подсказки» — там разжёвано по шагам.', kind: 'info', delay: d(200) });
    return { success: false, logs, missing, styleScore: 1 };
  }

  // Успех
  logs.push({ text: '[ROOT] Доступ ROOT получен! Заметаю следы...', kind: 'ok', delay: d(550) });
  logs.push({ text: `[WALLET] Перевод ${mission.rewardAmount} ${mission.rewardCrypto} на твой кошелёк...`, kind: 'ok', delay: d(700) });
  logs.push({ text: '[✓] ВЗЛОМ ЗАВЕРШЁН. Ты — машина.', kind: 'ok', delay: d(400) });

  // Звёзды стиля: комментарии + чистота + использование переменных
  let stars = 2;
  if (code.includes('#')) stars = 3;
  if (lines.filter(l => l.trim() !== '').length <= 6 && missing.length === 0) stars = Math.max(stars, 2);
  if (/print/.test(codeNoComments) && mission.id > 2) stars = 3;

  return { success: true, logs, missing: [], styleScore: stars };
}

function checkSyntax(code: string): { line: number; msg: string } | null {
  const lines = code.split('\n');
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const trimmed = line.trim();
    if (trimmed === '') continue;
    // for/if/while/def/else без двоеточия
    if (/^(for\b|if\b|while\b|def\b|else\b)/.test(trimmed) && !trimmed.endsWith(':') && !trimmed.includes('#')) {
      // разрешаем однострочный if с : внутри
      if (!trimmed.includes(':')) return { line: i + 1, msg: `ожидалось ':' в конце строки («${trimmed.slice(0, 30)}...»)` };
    }
    // нечётные кавычки
    const sq = (line.match(/"/g) || []).length;
    const sq2 = (line.match(/'/g) || []).length;
    if (sq % 2 !== 0 || sq2 % 2 !== 0) return { line: i + 1, msg: 'незакрытая кавычка — проверь строки' };
    // скобки
    const open = (line.match(/\(/g) || []).length;
    const close = (line.match(/\)/g) || []).length;
    if (open !== close) return { line: i + 1, msg: 'несогласованные скобки ( и )' };
    // отступ: строка после : должна быть с отступом
    if (i > 0) {
      const prev = lines[i - 1].trim();
      if (prev.endsWith(':') && line.length > 0 && !line.startsWith(' ') && !line.startsWith('\t') && trimmed !== '') {
        return { line: i + 1, msg: 'ожидался отступ после «:» (4 пробела)' };
      }
    }
  }
  return null;
}

// Простая подсветка Python для превью
export function highlightPython(code: string): { text: string; cls: string }[][] {
  const keywords = new Set(['for', 'in', 'if', 'else', 'while', 'def', 'True', 'False', 'and', 'or', 'not', 'range', 'return']);
  return code.split('\n').map(line => {
    const tokens: { text: string; cls: string }[] = [];
    // комментарий
    const commentIdx = line.indexOf('#');
    let codePart = line;
    let commentPart = '';
    if (commentIdx >= 0) {
      codePart = line.slice(0, commentIdx);
      commentPart = line.slice(commentIdx);
    }
    // токенизация по словам, строкам, числам
    const re = /("[^"]*"|'[^']*'|\b\d+\.?\d*\b|\b[A-Za-z_]\w*\b|[^\s\w])/g;
    let last = 0;
    let m: RegExpExecArray | null;
    while ((m = re.exec(codePart)) !== null) {
      if (m.index > last) tokens.push({ text: codePart.slice(last, m.index), cls: '' });
      const t = m[0];
      let cls = '';
      if (/^["']/.test(t)) cls = 'token-string';
      else if (/^\d/.test(t)) cls = 'token-number';
      else if (keywords.has(t)) cls = 'token-keyword';
      else if (/^[A-Za-z_]\w*$/.test(t)) {
        // если дальше скобка — функция
        const rest = codePart.slice(m.index + t.length).trimStart();
        cls = rest.startsWith('(') ? 'token-func' : 'token-var';
      }
      tokens.push({ text: t, cls });
      last = m.index + t.length;
    }
    if (last < codePart.length) tokens.push({ text: codePart.slice(last), cls: '' });
    if (commentPart) tokens.push({ text: commentPart, cls: 'token-comment' });
    if (tokens.length === 0) tokens.push({ text: '', cls: '' });
    return tokens;
  });
}
