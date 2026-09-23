import { useState, useEffect, useRef, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Terminal, Cpu, TrendingUp, ArrowUpCircle, GraduationCap, FolderOpen, User,
  X, Play, Lightbulb, BookOpen, ChevronRight, Coins, DollarSign, Zap,
  Pickaxe, Ghost, Lock, CheckCircle2, Star, Wallet, Plus, Power, Volume2,
  VolumeX, RotateCcw, Shield, Wifi, CircleDollarSign, FileText, Folder,
  ChevronLeft, Sparkles, Trophy, Target, Keyboard, AlertTriangle, Trash2, Flame
} from 'lucide-react';
import { CRYPTOS, MISSIONS, UPGRADES, LESSONS, HACKER_QUOTES, xpForLevel, type CryptoId, type Mission } from './gameData';
import { simulateExecution, highlightPython, type ExecLine } from './engine';
import { cn } from './utils/cn';

// ============ TYPES ============
interface Miner {
  id: string;
  pcName: string;
  ip: string;
  crypto: CryptoId;
  earned: number;
  startedAt: number;
}

interface GameState {
  dollars: number;
  crypto: Record<CryptoId, number>;
  xp: number;
  level: number;
  completedMissions: number[];
  miners: Miner[];
  upgrades: Record<string, number>;
  completedLessons: number[];
  totalHacked: number;
  totalEarnedDollars: number;
  totalTrades: number;
}

interface Toast {
  id: number;
  title: string;
  desc: string;
  kind: 'ok' | 'err' | 'info' | 'gold';
}

const SAVE_KEY = 'cryptohack_save_v1';

const DEFAULT_STATE: GameState = {
  dollars: 150,
  crypto: { BTC: 0, ETH: 0, XMR: 0, SOL: 0 },
  xp: 0,
  level: 1,
  completedMissions: [],
  miners: [],
  upgrades: { hackSpeed: 0, minerEff: 0, codeLib: 0, stealth: 0 },
  completedLessons: [],
  totalHacked: 0,
  totalEarnedDollars: 0,
  totalTrades: 0,
};

const MINER_BASE_RATE: Record<CryptoId, number> = {
  BTC: 0.0000009,
  ETH: 0.000018,
  XMR: 0.00042,
  SOL: 0.00031,
};

const MINER_EFF_MULT = [1, 1.5, 2.2, 3.2, 4.5, 6];
const HACK_REWARD_BONUS = [0, 0.05, 0.1, 0.2, 0.35, 0.5];
const STEALTH_FEE = [0.05, 0.04, 0.03, 0.02, 0];
const STEALTH_XP = [0, 0.1, 0.2, 0.35, 0.5];

// ============ HELPERS ============
const fmtDollars = (n: number) =>
  n >= 10000 ? `$${(n / 1000).toFixed(1)}K` : `$${n.toFixed(n < 100 ? 2 : 0)}`;
const fmtDollarsFull = (n: number) => `$${n.toLocaleString('ru-RU', { maximumFractionDigits: 2, minimumFractionDigits: 2 })}`;
const fmtCrypto = (n: number) => {
  if (n === 0) return '0';
  if (n < 0.000001) return n.toExponential(2);
  if (n < 0.01) return n.toFixed(6);
  if (n < 1) return n.toFixed(4);
  return n.toFixed(3);
};
const fmtPrice = (n: number) => `$${n.toLocaleString('ru-RU', { maximumFractionDigits: 0 })}`;

function playBeep(freq = 660, dur = 0.08, type: OscillatorType = 'square', vol = 0.04) {
  try {
    const Ctx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    const ctx = new Ctx();
    const o = ctx.createOscillator();
    const g = ctx.createGain();
    o.type = type;
    o.frequency.value = freq;
    g.gain.value = vol;
    o.connect(g);
    g.connect(ctx.destination);
    o.start();
    o.stop(ctx.currentTime + dur);
    setTimeout(() => ctx.close(), dur * 1000 + 100);
  } catch { /* ignore */ }
}

// ============ MATRIX BG ============
function MatrixBg({ opacity = 0.5 }: { opacity?: number }) {
  const ref = useRef<HTMLCanvasElement>(null);
  useEffect(() => {
    const canvas = ref.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    let w = (canvas.width = canvas.offsetWidth);
    let h = (canvas.height = canvas.offsetHeight);
    const chars = '01ABCDEF$#@%&Ξ₿<>+*'.split('');
    const fontSize = 13;
    let cols = Math.floor(w / fontSize);
    let drops: number[] = Array(cols).fill(0).map(() => Math.random() * h / fontSize);
    const onResize = () => {
      w = canvas.width = canvas.offsetWidth;
      h = canvas.height = canvas.offsetHeight;
      cols = Math.floor(w / fontSize);
      drops = Array(cols).fill(0).map(() => Math.random() * h / fontSize);
    };
    window.addEventListener('resize', onResize);
    let raf = 0;
    const draw = () => {
      ctx.fillStyle = 'rgba(4,7,15,0.12)';
      ctx.fillRect(0, 0, w, h);
      ctx.font = `${fontSize}px monospace`;
      for (let i = 0; i < cols; i++) {
        const ch = chars[Math.floor(Math.random() * chars.length)];
        const green = Math.random() > 0.12;
        ctx.fillStyle = green ? 'rgba(0,255,157,0.55)' : 'rgba(255,45,120,0.6)';
        ctx.fillText(ch, i * fontSize, drops[i] * fontSize);
        if (drops[i] * fontSize > h && Math.random() > 0.975) drops[i] = 0;
        drops[i]++;
      }
      raf = requestAnimationFrame(() => setTimeout(draw, 66));
    };
    draw();
    return () => { cancelAnimationFrame(raf); window.removeEventListener('resize', onResize); };
  }, []);
  return <canvas ref={ref} className="absolute inset-0 h-full w-full" style={{ opacity }} />;
}

// ============ START SCREEN ============
function StartScreen({ onStart, hasSave, onReset }: { onStart: () => void; hasSave: boolean; onReset: () => void }) {
  const [showHelp, setShowHelp] = useState(false);
  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center overflow-hidden bg-[#04070f] px-4">
      <MatrixBg opacity={0.4} />
      <div className="scan-beam" />
      <div className="pointer-events-none absolute left-0 right-0 top-0 flex items-center justify-between border-b border-[#00ff9d]/20 bg-[#04070f]/80 px-4 py-2 font-mono text-[11px] text-[#00ff9d]/70 backdrop-blur">
        <span className="flex items-center gap-2"><span className="h-2 w-2 animate-pulse rounded-full bg-[#00ff9d]" /> NEON_NET :: ЗАЩИЩЁННОЕ СОЕДИНЕНИЕ</span>
        <span className="hidden sm:block">TOR-узел: 185.220.70.4 :: ping 12ms</span>
      </div>

      <motion.div initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.7 }} className="relative z-10 mt-10 w-full max-w-3xl text-center">
        <div className="mb-4 inline-flex items-center gap-2 border border-[#ff2d78]/50 bg-[#ff2d78]/10 px-3 py-1 font-mono text-[11px] tracking-[0.25em] text-[#ff2d78]">
          <Flame size={13} /> СИМУЛЯТОР ХАКЕРА // УЧИ PYTHON ВЗЛАМЫВАЯ
        </div>
        <h1 className="glitch font-display font-black leading-none text-[#eafff4]" data-text="CRYPTO_HACK" style={{ fontFamily: 'Unbounded, sans-serif', fontSize: 'clamp(2.4rem, 9vw, 5.5rem)' }}>
          CRYPTO<span className="text-[#00ff9d] glow-green">_</span>HACK
        </h1>
        <p className="mx-auto mt-4 max-w-xl text-sm leading-relaxed text-[#8fa3bd] sm:text-base">
          Пиши настоящий <span className="font-bold text-[#ffe600]">Python-код</span>, взламывай криптокошельки,
          ставь <span className="font-bold text-[#00e5ff]">майнеры</span> на чужие компы,
          торгуй на <span className="font-bold text-[#00ff9d]">бирже</span> и стань легендой даркнета.
        </p>

        <div className="mx-auto mt-6 grid max-w-2xl grid-cols-2 gap-2 text-left sm:grid-cols-4">
          {[
            { icon: Terminal, t: '8 миссий-взломов', c: '#00ff9d' },
            { icon: Pickaxe, t: 'Пассивный майнинг', c: '#00e5ff' },
            { icon: TrendingUp, t: 'Живая биржа', c: '#ffe600' },
            { icon: GraduationCap, t: 'Школа Python', c: '#ff2d78' },
          ].map((f, i) => (
            <motion.div key={i} initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.3 + i * 0.1 }} className="flex items-center gap-2 border border-white/10 bg-[#0a111f]/90 px-3 py-2.5">
              <f.icon size={18} style={{ color: f.c }} />
              <span className="font-mono text-[11px] text-[#d7e3f4]">{f.t}</span>
            </motion.div>
          ))}
        </div>

        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <button onClick={onStart} className="btn-pulse group flex items-center gap-2 bg-[#00ff9d] px-8 py-3.5 font-mono text-sm font-bold tracking-widest text-black transition hover:bg-[#5cffb8]">
            <Power size={16} /> {hasSave ? 'ПРОДОЛЖИТЬ ВЗЛОМ' : 'НАЧАТЬ ИГРУ'}
          </button>
          <button onClick={() => setShowHelp(true)} className="flex items-center gap-2 border border-[#00e5ff]/50 bg-[#00e5ff]/5 px-6 py-3.5 font-mono text-sm tracking-widest text-[#00e5ff] transition hover:bg-[#00e5ff]/15">
            <BookOpen size={16} /> КАК ИГРАТЬ
          </button>
        </div>
        {hasSave && (
          <button onClick={onReset} className="mx-auto mt-4 flex items-center gap-1.5 font-mono text-[11px] text-[#5b6b85] hover:text-[#ff2d78]">
            <Trash2 size={12} /> стереть сохранение и начать заново
          </button>
        )}
        <p className="mt-6 font-mono text-[11px] text-[#5b6b85]">💡 {HACKER_QUOTES[Math.floor(Date.now() / 8000) % HACKER_QUOTES.length]}</p>
      </motion.div>

      <AnimatePresence>
        {showHelp && (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-4 backdrop-blur-sm" onClick={() => setShowHelp(false)}>
            <motion.div initial={{ scale: 0.92, y: 20 }} animate={{ scale: 1, y: 0 }} exit={{ scale: 0.92 }} onClick={e => e.stopPropagation()} className="box-glow-green w-full max-w-lg border border-[#00ff9d]/40 bg-[#070d1a] p-6">
              <div className="mb-4 flex items-center justify-between">
                <h3 className="font-mono text-lg font-bold text-[#00ff9d] glow-green">КАК ИГРАТЬ</h3>
                <button onClick={() => setShowHelp(false)} className="text-[#5b6b85] hover:text-white"><X size={20} /></button>
              </div>
              <ol className="space-y-3 text-sm leading-relaxed text-[#b9c7dd]">
                <li><b className="text-[#00ff9d]">1. Взламывай.</b> Открой «Хак-терминал», выбери миссию и пиши Python-код по обучению. Каждая миссия учит новой конструкции языка.</li>
                <li><b className="text-[#00e5ff]">2. Ставь майнеры.</b> Взломанные компы — твои. Установи на них майнер, и крипта будет капать даже пока ты читаешь обучение.</li>
                <li><b className="text-[#ffe600]">3. Торгуй.</b> Продавай крипту за доллары на бирже, когда цена высокая. Следи за графиком!</li>
                <li><b className="text-[#ff2d78]">4. Прокачивайся.</b> За доллары покупай ускорение взлома, мощность майнинга и новые функции кода.</li>
                <li><b className="text-white">5. Расти.</b> Опыт поднимает уровень и открывает новые монеты: BTC → ETH → XMR → SOL.</li>
              </ol>
              <button onClick={() => { setShowHelp(false); onStart(); }} className="mt-5 w-full bg-[#00ff9d] py-3 font-mono text-sm font-bold text-black hover:bg-[#5cffb8]">ПОНЯЛ, ПОГНАЛИ!</button>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      <div className="pointer-events-none absolute bottom-3 left-0 right-0 text-center font-mono text-[10px] text-[#3a4a63]">NEON_OS v3.7 :: сделано для будущих python-хакеров :: 18+ только в душе</div>
    </div>
  );
}

// ============ BOOT SCREEN ============
function BootScreen({ onDone }: { onDone: () => void }) {
  const [lines, setLines] = useState<string[]>([]);
  const bootLines = [
    'NEON BIOS v3.7 — проверка памяти ... OK',
    'Загрузка ядра NeonOS ... OK',
    'Подключение к TOR-сети ... OK (3 ретранслятора)',
    'Монтирование /dev/blackvault ... OK',
    'Запуск ghost-драйверов ... OK',
    'Обход телеметрии ... OK',
    'Добро пожаловать, ghost. Доступ ROOT подтверждён.',
  ];
  useEffect(() => {
    let i = 0;
    const t = setInterval(() => {
      if (i < bootLines.length) {
        setLines(prev => [...prev, bootLines[i]]);
        playBeep(300 + i * 90, 0.05, 'square', 0.02);
        i++;
      } else {
        clearInterval(t);
        setTimeout(onDone, 700);
      }
    }, 320);
    return () => clearInterval(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  return (
    <div className="flex min-h-screen items-center justify-center bg-black p-6">
      <div className="w-full max-w-xl font-mono text-sm">
        {lines.map((l, i) => (
          <div key={i} className={cn('py-0.5', i === lines.length - 1 ? 'text-[#00ff9d] glow-green' : 'text-[#5b7a5e]')}>{'> '}{l}</div>
        ))}
        <span className="cursor-blink text-[#00ff9d]">▊</span>
      </div>
    </div>
  );
}

// ============ WINDOW FRAME ============
function WindowFrame({ id, title, icon, color, onClose, onFocus, z, children, wide }: {
  id: string; title: string; icon: React.ReactNode; color: string;
  onClose: () => void; onFocus: () => void; z: number; children: React.ReactNode; wide?: boolean;
}) {
  const [pos, setPos] = useState(() => {
    const seeds: Record<string, number> = { hack: 0, miner: 1, trade: 2, upgrade: 3, learn: 4, files: 5, profile: 6 };
    const s = (seeds[id] ?? 0) * 36;
    return { x: 40 + s, y: 70 + s };
  });
  const dragRef = useRef<{ dx: number; dy: number; on: boolean }>({ dx: 0, dy: 0, on: false });

  const onMouseDown = (e: React.MouseEvent) => {
    onFocus();
    if (window.innerWidth < 768) return;
    dragRef.current = { dx: e.clientX - pos.x, dy: e.clientY - pos.y, on: true };
    const move = (ev: MouseEvent) => {
      if (!dragRef.current.on) return;
      setPos({ x: Math.max(0, Math.min(window.innerWidth - 300, ev.clientX - dragRef.current.dx)), y: Math.max(48, Math.min(window.innerHeight - 120, ev.clientY - dragRef.current.dy)) });
    };
    const up = () => { dragRef.current.on = false; window.removeEventListener('mousemove', move); window.removeEventListener('mouseup', up); };
    window.addEventListener('mousemove', move);
    window.addEventListener('mouseup', up);
  };

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.94, y: 16 }} animate={{ opacity: 1, scale: 1, y: 0 }} exit={{ opacity: 0, scale: 0.94, y: 10 }}
      transition={{ duration: 0.22 }}
      onMouseDown={onFocus}
      style={{ zIndex: z, left: window.innerWidth < 768 ? 8 : pos.x, top: window.innerWidth < 768 ? 56 : pos.y }}
      className={cn('window-shadow fixed border bg-[#070d1a]', wide ? 'w-[calc(100vw-16px)] md:w-[880px]' : 'w-[calc(100vw-16px)] md:w-[640px]')}
    >
      <div onMouseDown={onMouseDown} className="flex cursor-move select-none items-center justify-between border-b border-white/10 bg-[#0b1322] px-3 py-2">
        <div className="flex items-center gap-2 font-mono text-xs font-bold tracking-wider" style={{ color }}>
          {icon} {title}
        </div>
        <div className="flex items-center gap-1.5">
          <span className="hidden font-mono text-[10px] text-[#3a4a63] sm:block">● ● ○</span>
          <button onClick={onClose} className="flex h-6 w-6 items-center justify-center bg-[#ff2d78]/15 text-[#ff2d78] hover:bg-[#ff2d78] hover:text-black"><X size={14} /></button>
        </div>
      </div>
      <div className="max-h-[calc(100vh-190px)] overflow-y-auto">{children}</div>
    </motion.div>
  );
}

// ============ HACK TERMINAL ============
function HackWindow({ mission, setMissionId, game, onHackSuccess, soundOn }: {
  mission: Mission;
  setMissionId: (id: number) => void;
  game: GameState;
  onHackSuccess: (m: Mission, stars: number) => void;
  soundOn: boolean;
}) {
  const [code, setCode] = useState(mission.starterCode);
  const [tab, setTab] = useState<'brief' | 'theory' | 'hints'>('brief');
  const [logs, setLogs] = useState<ExecLine[]>([]);
  const [running, setRunning] = useState(false);
  const [lastResult, setLastResult] = useState<'ok' | 'fail' | null>(null);
  const [stars, setStars] = useState(0);
  const [showSolution, setShowSolution] = useState(false);
  const logRef = useRef<HTMLDivElement>(null);

  useEffect(() => { setCode(mission.starterCode); setLogs([]); setLastResult(null); setShowSolution(false); setTab('brief'); setStars(0); }, [mission.id]);

  useEffect(() => { logRef.current?.scrollTo({ top: 99999 }); }, [logs]);

  const lockedByLib = game.upgrades.codeLib < mission.requiredCodeLib;
  const lockedByLevel = game.level < mission.requiredLevel;
  const completed = game.completedMissions.includes(mission.id);
  const locked = lockedByLib || lockedByLevel;

  const runCode = async () => {
    if (running || locked) return;
    setRunning(true);
    setLogs([]);
    setLastResult(null);
    if (soundOn) playBeep(440, 0.07);
    const res = simulateExecution(code, mission, game.upgrades.hackSpeed);
    for (const line of res.logs) {
      await new Promise(r => setTimeout(r, line.delay));
      setLogs(prev => [...prev, line]);
      if (soundOn && line.kind === 'ok') playBeep(700 + Math.random() * 300, 0.03, 'sine', 0.02);
    }
    setRunning(false);
    if (res.success) {
      setLastResult('ok');
      setStars(res.styleScore);
      if (soundOn) { playBeep(660, 0.1); setTimeout(() => playBeep(880, 0.12), 120); setTimeout(() => playBeep(1320, 0.18), 240); }
      if (!completed) onHackSuccess(mission, res.styleScore);
    } else {
      setLastResult('fail');
      if (soundOn) playBeep(180, 0.25, 'sawtooth', 0.05);
    }
  };

  const highlighted = highlightPython(code);

  return (
    <div className="flex flex-col lg:flex-row">
      {/* Mission list */}
      <div className="w-full shrink-0 border-b border-white/10 bg-[#050b16] lg:w-56 lg:border-b-0 lg:border-r">
        <div className="border-b border-white/10 px-3 py-2 font-mono text-[10px] tracking-widest text-[#5b6b85]">ЦЕЛИ // {game.completedMissions.length}/{MISSIONS.length}</div>
        <div className="max-h-40 overflow-y-auto lg:max-h-[520px]">
          {MISSIONS.map(m => {
            const done = game.completedMissions.includes(m.id);
            const libLock = game.upgrades.codeLib < m.requiredCodeLib;
            const lvlLock = game.level < m.requiredLevel;
            const isLock = libLock || lvlLock;
            return (
              <button key={m.id} onClick={() => setMissionId(m.id)} className={cn('flex w-full items-center gap-2 border-l-2 px-3 py-2.5 text-left transition', mission.id === m.id ? 'border-[#00ff9d] bg-[#00ff9d]/10' : 'border-transparent hover:bg-white/5')}>
                <span className={cn('flex h-6 w-6 shrink-0 items-center justify-center font-mono text-[11px] font-bold', done ? 'bg-[#00ff9d] text-black' : isLock ? 'bg-white/10 text-[#5b6b85]' : 'bg-[#00e5ff]/20 text-[#00e5ff]')}>
                  {done ? <CheckCircle2 size={14} /> : isLock ? <Lock size={13} /> : m.id}
                </span>
                <span className="min-w-0">
                  <span className={cn('block truncate font-mono text-[11px] font-bold', done ? 'text-[#00ff9d]' : 'text-[#d7e3f4]')}>{m.title}</span>
                  <span className="block font-mono text-[10px] text-[#5b6b85]">{m.rewardAmount} {m.rewardCrypto} · {m.difficulty}</span>
                </span>
              </button>
            );
          })}
        </div>
      </div>

      {/* Main */}
      <div className="min-w-0 flex-1">
        {/* target header */}
        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 border-b border-white/10 bg-[#0a1120] px-4 py-2.5">
          <div className="flex items-center gap-2">
            <Target size={14} className="text-[#ff2d78]" />
            <span className="font-mono text-xs font-bold text-white">{mission.targetName}</span>
          </div>
          <span className="font-mono text-[11px] text-[#00e5ff]">{mission.targetIp}</span>
          <span className="font-mono text-[11px] text-[#5b6b85]">{mission.os}</span>
          <span className="ml-auto flex items-center gap-1 font-mono text-[11px] text-[#ffe600]"><Shield size={12} /> {mission.security}%</span>
        </div>

        {locked ? (
          <div className="flex flex-col items-center px-6 py-10 text-center">
            <Lock size={40} className="mb-3 text-[#ff2d78]" />
            <h4 className="font-mono text-sm font-bold text-[#ff2d78]">ДОСТУП ЗАБЛОКИРОВАН</h4>
            <p className="mt-2 max-w-sm text-sm text-[#8fa3bd]">
              {lockedByLevel && <>Требуется <b className="text-white">уровень {mission.requiredLevel}</b> (у тебя {game.level}). Качай опыт на взломах и обучении.<br /></>}
              {lockedByLib && <>Требуется <b className="text-[#00e5ff]">«Библиотека кода» ур. {mission.requiredCodeLib}</b>. Купи апгрейд за доллары во вкладке «Апгрейды».</>}
            </p>
            <div className="mt-3 border border-[#ffe600]/40 bg-[#ffe600]/5 px-3 py-2 font-mono text-[11px] text-[#ffe600]">Концепт миссии: {mission.concept}</div>
          </div>
        ) : (
          <>
            {/* tabs */}
            <div className="flex border-b border-white/10">
              {([['brief', '📋 Брифинг'], ['theory', '🎓 Теория'], ['hints', '💡 Подсказки']] as const).map(([k, label]) => (
                <button key={k} onClick={() => setTab(k)} className={cn('flex-1 px-2 py-2 font-mono text-[11px] font-bold tracking-wide transition sm:flex-none sm:px-4', tab === k ? 'bg-[#00ff9d]/10 text-[#00ff9d]' : 'text-[#5b6b85] hover:text-white')}>{label}</button>
              ))}
              <div className="ml-auto hidden items-center px-3 font-mono text-[10px] text-[#5b6b85] sm:flex">PYTHON 3.12 ● NeonHack FW</div>
            </div>

            <div className="border-b border-white/10 bg-[#050b16] px-4 py-3 text-[13px] leading-relaxed">
              {tab === 'brief' && (
                <>
                  <p className="text-[#b9c7dd]">{mission.briefing}</p>
                  <div className="mt-2 border-l-2 border-[#00ff9d] bg-[#00ff9d]/5 px-3 py-2">
                    <span className="font-mono text-[11px] font-bold text-[#00ff9d]">ЗАДАЧА:</span>
                    <pre className="whitespace-pre-wrap font-mono text-[12px] text-white">{mission.task}</pre>
                  </div>
                  <div className="mt-2 flex flex-wrap gap-2 font-mono text-[11px]">
                    <span className="border border-[#ffe600]/40 bg-[#ffe600]/10 px-2 py-0.5 text-[#ffe600]">+{mission.rewardAmount} {mission.rewardCrypto}</span>
                    <span className="border border-[#00ff9d]/40 bg-[#00ff9d]/10 px-2 py-0.5 text-[#00ff9d]">+${mission.rewardDollars}</span>
                    <span className="border border-[#00e5ff]/40 bg-[#00e5ff]/10 px-2 py-0.5 text-[#00e5ff]">+{mission.rewardXp} XP</span>
                    {completed && <span className="border border-white/20 px-2 py-0.5 text-[#8fa3bd]">✓ пройдено — повтор без награды</span>}
                  </div>
                </>
              )}
              {tab === 'theory' && (
                <div className="space-y-2">
                  <div className="font-mono text-[12px] font-bold text-[#00e5ff]">📖 {mission.concept}</div>
                  <p className="text-[#8fa3bd]">{mission.conceptDesc}</p>
                  {mission.theory.map((t, i) => (
                    <div key={i} className="flex gap-2 text-[#b9c7dd]"><span className="font-mono text-[#00ff9d]">{i + 1}.</span><span>{t}</span></div>
                  ))}
                </div>
              )}
              {tab === 'hints' && (
                <div className="space-y-2">
                  {mission.hints.map((h, i) => (
                    <div key={i} className="flex gap-2 text-[#b9c7dd]"><Lightbulb size={14} className="mt-0.5 shrink-0 text-[#ffe600]" /><span>{h}</span></div>
                  ))}
                  <div className="pt-1">
                    {!showSolution ? (
                      <button onClick={() => setShowSolution(true)} className="font-mono text-[11px] text-[#5b6b85] underline hover:text-[#ff2d78]">показать готовое решение (без штрафа, ты же учишься)</button>
                    ) : (
                      <div className="border border-[#ff2d78]/40 bg-black/60 p-2">
                        <div className="mb-1 flex items-center justify-between">
                          <span className="font-mono text-[10px] text-[#ff2d78]">РЕШЕНИЕ:</span>
                          <button onClick={() => setCode(mission.solution)} className="font-mono text-[10px] text-[#00ff9d] underline">вставить в редактор</button>
                        </div>
                        <pre className="font-mono text-[12px] text-[#d7e3f4]">{mission.solution}</pre>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* editor */}
            <div className="grid lg:grid-cols-2">
              <div className="border-b border-white/10 lg:border-b-0 lg:border-r">
                <div className="flex items-center justify-between bg-black/40 px-3 py-1.5">
                  <span className="flex items-center gap-1.5 font-mono text-[10px] text-[#5b6b85]"><Keyboard size={12} /> exploit.py</span>
                  <button onClick={() => setCode(mission.starterCode)} className="font-mono text-[10px] text-[#5b6b85] hover:text-white">сбросить</button>
                </div>
                <div className="relative h-52 bg-black/70">
                  <pre aria-hidden className="code-editor pointer-events-none absolute inset-0 overflow-hidden whitespace-pre-wrap break-all p-3 text-[12.5px] leading-5">
                    {highlighted.map((line, i) => (
                      <div key={i} className="flex">
                        <span className="w-7 shrink-0 select-none text-right text-[#2a3a52]">{i + 1} </span>
                        <code>{line.map((t, j) => <span key={j} className={t.cls}>{t.text || ' '}</span>)}<span> </span></code>
                      </div>
                    ))}
                  </pre>
                  <textarea
                    value={code}
                    onChange={e => setCode(e.target.value)}
                    onKeyDown={e => {
                      if (e.key === 'Tab') {
                        e.preventDefault();
                        const el = e.currentTarget;
                        const s = el.selectionStart;
                        setCode(code.slice(0, s) + '    ' + code.slice(el.selectionEnd));
                        requestAnimationFrame(() => { el.selectionStart = el.selectionEnd = s + 4; });
                      }
                    }}
                    spellCheck={false}
                    className="code-editor absolute inset-0 h-full w-full resize-none bg-transparent p-3 pl-[40px] text-[12.5px] leading-5 text-transparent"
                    style={{ caretColor: '#00ff9d' }}
                  />
                </div>
                <div className="flex gap-2 border-t border-white/10 bg-[#0a1120] p-2.5">
                  <button onClick={runCode} disabled={running} className={cn('flex flex-1 items-center justify-center gap-2 py-2.5 font-mono text-xs font-bold tracking-widest transition', running ? 'bg-[#3a4a63] text-[#0a111f]' : 'bg-[#00ff9d] text-black hover:bg-[#5cffb8]')}>
                    <Play size={14} /> {running ? 'ВЫПОЛНЯЕТСЯ...' : '▶ ЗАПУСТИТЬ'}
                  </button>
                </div>
              </div>
              {/* output */}
              <div className="flex min-h-[220px] flex-col bg-black">
                <div className="bg-black/60 px-3 py-1.5 font-mono text-[10px] text-[#5b6b85]">⌁ вывод терминала</div>
                <div ref={logRef} className="h-52 flex-1 overflow-y-auto p-3 font-mono text-[11.5px] leading-5">
                  {logs.length === 0 && !running && (
                    <div className="text-[#2f3f58]">Напиши код слева и нажми «Запустить».<br />Здесь появится вывод твоего скрипта...</div>
                  )}
                  {logs.map((l, i) => (
                    <div key={i} className={cn(
                      l.kind === 'cmd' && 'text-white',
                      l.kind === 'ok' && 'text-[#00ff9d]',
                      l.kind === 'info' && 'text-[#7d93b0]',
                      l.kind === 'warn' && 'text-[#ffe600]',
                      l.kind === 'err' && 'text-[#ff2d78]',
                    )}>{l.text}</div>
                  ))}
                  {running && <span className="cursor-blink text-[#00ff9d]">▊</span>}
                </div>
                {lastResult && (
                  <div className={cn('flex items-center gap-2 border-t px-3 py-2 font-mono text-[11px] font-bold', lastResult === 'ok' ? 'border-[#00ff9d]/30 bg-[#00ff9d]/10 text-[#00ff9d]' : 'border-[#ff2d78]/30 bg-[#ff2d78]/10 text-[#ff2d78]')}>
                    {lastResult === 'ok' ? (
                      <><CheckCircle2 size={15} /> ВЗЛОМ УСПЕШЕН
                        <span className="ml-auto flex text-[#ffe600]">{[1, 2, 3].map(s => <Star key={s} size={13} fill={s <= stars ? '#ffe600' : 'transparent'} />)}</span>
                      </>
                    ) : (
                      <><AlertTriangle size={15} /> ВЗЛОМ ПРОВАЛЕН — смотри подсказки и пробуй ещё</>
                    )}
                  </div>
                )}
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

// ============ MINER WINDOW ============
function MinerWindow({ game, onInstall, onRemove, minerIncome }: {
  game: GameState; onInstall: (missionId: number, crypto: CryptoId) => void; onRemove: (id: string) => void; minerIncome: number;
}) {
  const [selCrypto, setSelCrypto] = useState<CryptoId>('BTC');
  const hackedNotMined = MISSIONS.filter(m => game.completedMissions.includes(m.id) && !game.miners.some(min => min.ip === m.targetIp));
  const installCost = 120 + game.miners.length * 80;

  return (
    <div className="p-4">
      <div className="mb-3 flex flex-wrap items-center gap-2 border border-[#00e5ff]/30 bg-[#00e5ff]/5 p-3">
        <Pickaxe className="text-[#00e5ff]" size={20} />
        <div>
          <div className="font-mono text-xs font-bold text-[#00e5ff]">МАЙНИНГ-ФЕРМА · {game.miners.length} РИГ(ОВ)</div>
          <div className="font-mono text-[11px] text-[#8fa3bd]">Доход: <b className="text-[#00ff9d]">~${minerIncome.toFixed(2)}/мин</b> пассивно · мощность x{MINER_EFF_MULT[game.upgrades.minerEff]}</div>
        </div>
        <div className="ml-auto font-mono text-[10px] text-[#5b6b85]">работает даже со свёрнутым окном</div>
      </div>

      {game.miners.length > 0 && (
        <div className="mb-4 space-y-2">
          {game.miners.map(min => {
            const c = CRYPTOS.find(x => x.id === min.crypto)!;
            return (
              <div key={min.id} className="flex items-center gap-3 border border-white/10 bg-[#0a1120] p-2.5">
                <span className="flex h-9 w-9 items-center justify-center border font-mono text-lg font-bold" style={{ color: c.color, borderColor: c.color + '55', background: c.color + '12' }}>{c.icon}</span>
                <div className="min-w-0 flex-1">
                  <div className="truncate font-mono text-[12px] font-bold text-white">{min.pcName}</div>
                  <div className="font-mono text-[10px] text-[#5b6b85]">{min.ip} · майнит {c.name}</div>
                  <div className="font-mono text-[11px] text-[#00ff9d]">добыто: {fmtCrypto(min.earned)} {min.crypto}</div>
                </div>
                <div className="flex items-center gap-1 font-mono text-[10px] text-[#00ff9d]"><span className="h-1.5 w-1.5 animate-pulse rounded-full bg-[#00ff9d]" /> LIVE</div>
                <button onClick={() => onRemove(min.id)} className="border border-[#ff2d78]/40 px-2 py-1 font-mono text-[10px] text-[#ff2d78] hover:bg-[#ff2d78]/15">снять</button>
              </div>
            );
          })}
        </div>
      )}

      <div className="font-mono text-[11px] font-bold tracking-widest text-[#8fa3bd]">УСТАНОВИТЬ МАЙНЕР · ${installCost}</div>
      {hackedNotMined.length === 0 ? (
        <div className="mt-2 border border-dashed border-white/15 p-4 text-center text-sm text-[#5b6b85]">
          Нет свободных взломанных компов.<br />Взломай новую цель в <b className="text-[#00ff9d]">Хак-терминале</b>, чтобы ставить майнеры.
        </div>
      ) : (
        <>
          <div className="mt-2 flex gap-1.5">
            {CRYPTOS.filter(c => game.level >= c.unlockLevel).map(c => (
              <button key={c.id} onClick={() => setSelCrypto(c.id)} className={cn('flex-1 border px-2 py-1.5 font-mono text-[11px] font-bold transition', selCrypto === c.id ? 'text-black' : 'text-[#8fa3bd]')} style={selCrypto === c.id ? { background: c.color, borderColor: c.color } : { borderColor: '#ffffff22' }}>
                {c.icon} {c.id}
              </button>
            ))}
          </div>
          <div className="mt-2 space-y-2">
            {hackedNotMined.map(m => (
              <div key={m.id} className="flex items-center gap-3 border border-white/10 bg-black/40 p-2.5">
                <Cpu size={18} className="shrink-0 text-[#00e5ff]" />
                <div className="min-w-0 flex-1">
                  <div className="truncate font-mono text-[12px] text-white">{m.targetName}</div>
                  <div className="font-mono text-[10px] text-[#5b6b85]">{m.targetIp} · {m.os}</div>
                </div>
                <button onClick={() => onInstall(m.id, selCrypto)} disabled={game.dollars < installCost} className={cn('flex items-center gap-1 px-3 py-1.5 font-mono text-[11px] font-bold', game.dollars < installCost ? 'bg-white/10 text-[#5b6b85]' : 'bg-[#00e5ff] text-black hover:bg-[#7df3ff]')}>
                  <Plus size={13} /> {selCrypto}
                </button>
              </div>
            ))}
          </div>
          {game.dollars < installCost && <div className="mt-2 font-mono text-[11px] text-[#ff2d78]">Не хватает ${installCost - game.dollars} — продай крипту на бирже!</div>}
        </>
      )}
    </div>
  );
}

// ============ TRADE WINDOW ============
function TradeWindow({ game, prices, history, onTrade, fee }: {
  game: GameState; prices: Record<CryptoId, number>; history: Record<CryptoId, number[]>; onTrade: (c: CryptoId, usd: number, isBuy: boolean) => void; fee: number;
}) {
  const [sel, setSel] = useState<CryptoId>('BTC');
  const [amount, setAmount] = useState('100');
  const c = CRYPTOS.find(x => x.id === sel)!;
  const hist = history[sel];
  const usd = parseFloat(amount) || 0;
  const price = prices[sel];
  const change = hist.length > 1 ? ((hist[hist.length - 1] - hist[0]) / hist[0]) * 100 : 0;
  const cryptoOut = usd / price * (1 - fee);

  const min = Math.min(...hist), max = Math.max(...hist);
  const pts = hist.map((p, i) => `${(i / (hist.length - 1)) * 100},${28 - ((p - min) / (max - min || 1)) * 24}`).join(' ');

  return (
    <div className="p-4">
      <div className="mb-3 grid grid-cols-2 gap-2 sm:grid-cols-4">
        {CRYPTOS.map(x => {
          const locked = game.level < x.unlockLevel;
          const h = history[x.id];
          const ch = h.length > 1 ? ((h[h.length - 1] - h[0]) / h[0]) * 100 : 0;
          return (
            <button key={x.id} disabled={locked} onClick={() => setSel(x.id)} className={cn('border p-2.5 text-left transition', sel === x.id ? 'bg-white/5' : 'opacity-80 hover:opacity-100', locked && 'opacity-40')} style={{ borderColor: sel === x.id ? x.color : '#ffffff1a' }}>
              <div className="flex items-center gap-1.5">
                <span className="font-mono text-lg font-bold" style={{ color: x.color }}>{locked ? <Lock size={15} className="text-[#5b6b85]" /> : x.icon}</span>
                <span className="font-mono text-xs font-bold text-white">{x.id}</span>
              </div>
              {locked ? <div className="mt-1 font-mono text-[10px] text-[#5b6b85]">ур. {x.unlockLevel}</div> : (
                <>
                  <div className="mt-1 font-mono text-[13px] text-white">{fmtPrice(prices[x.id])}</div>
                  <div className={cn('font-mono text-[11px] font-bold', ch >= 0 ? 'text-[#00ff9d]' : 'text-[#ff2d78]')}>{ch >= 0 ? '▲' : '▼'} {Math.abs(ch).toFixed(2)}%</div>
                </>
              )}
            </button>
          );
        })}
      </div>

      <div className="border border-white/10 bg-black/50 p-3">
        <div className="mb-1 flex items-center justify-between font-mono text-[11px]">
          <span className="font-bold" style={{ color: c.color }}>{c.icon} {c.name} / USD</span>
          <span className={cn('font-bold', change >= 0 ? 'text-[#00ff9d]' : 'text-[#ff2d78]')}>{change >= 0 ? '+' : ''}{change.toFixed(2)}%</span>
        </div>
        <svg viewBox="0 0 100 30" className="h-28 w-full" preserveAspectRatio="none">
          <polyline points={pts} fill="none" stroke={c.color} strokeWidth="1.2" vectorEffect="non-scaling-stroke" />
          {hist.map((p, i) => i % 8 === 0 && <circle key={i} cx={(i / (hist.length - 1)) * 100} cy={28 - ((p - min) / (max - min || 1)) * 24} r="1.4" fill={c.color} />)}
        </svg>
        <div className="mt-1 font-mono text-[10px] text-[#5b6b85]">● живой график · обновляется каждые 2 сек · комиссия {(fee * 100).toFixed(0)}%</div>
      </div>

      <div className="mt-3 grid gap-3 sm:grid-cols-2">
        <div className="border border-[#00ff9d]/30 bg-[#00ff9d]/5 p-3">
          <div className="font-mono text-[11px] font-bold text-[#00ff9d]">ПРОДАТЬ {sel} → $</div>
          <div className="mt-1 font-mono text-[10px] text-[#8fa3bd]">Баланс: {fmtCrypto(game.crypto[sel])} {sel} ≈ {fmtDollarsFull(game.crypto[sel] * price)}</div>
          <div className="mt-2 flex gap-1.5">
            <div className="relative flex-1">
              <span className="absolute left-2 top-1/2 -translate-y-1/2 font-mono text-xs text-[#5b6b85]">$</span>
              <input value={amount} onChange={e => setAmount(e.target.value)} inputMode="decimal" className="w-full border border-white/15 bg-black py-2 pl-6 pr-2 font-mono text-sm text-white outline-none focus:border-[#00ff9d]" />
            </div>
            <button onClick={() => setAmount(String(Math.floor(game.crypto[sel] * price)))} className="border border-white/15 px-2 font-mono text-[10px] text-[#8fa3bd] hover:text-white">MAX</button>
          </div>
          <div className="mt-1 font-mono text-[10px] text-[#5b6b85]">Получишь: {fmtDollarsFull(usd * (1 - fee))} (на руки)</div>
          <button onClick={() => onTrade(sel, usd, false)} disabled={usd <= 0 || usd / price > game.crypto[sel]} className="mt-2 w-full bg-[#00ff9d] py-2 font-mono text-xs font-bold text-black transition hover:bg-[#5cffb8] disabled:bg-white/10 disabled:text-[#5b6b85]">ПРОДАТЬ ЗА $</button>
        </div>
        <div className="border border-[#00e5ff]/30 bg-[#00e5ff]/5 p-3">
          <div className="font-mono text-[11px] font-bold text-[#00e5ff]">КУПИТЬ $ → {sel}</div>
          <div className="mt-1 font-mono text-[10px] text-[#8fa3bd]">Доллары: {fmtDollarsFull(game.dollars)} · 1 {sel} = {fmtPrice(price)}</div>
          <div className="mt-2 flex gap-1.5">
            <div className="relative flex-1">
              <span className="absolute left-2 top-1/2 -translate-y-1/2 font-mono text-xs text-[#5b6b85]">$</span>
              <input value={amount} onChange={e => setAmount(e.target.value)} inputMode="decimal" className="w-full border border-white/15 bg-black py-2 pl-6 pr-2 font-mono text-sm text-white outline-none focus:border-[#00e5ff]" />
            </div>
          </div>
          <div className="mt-1 font-mono text-[10px] text-[#5b6b85]">Получишь: {fmtCrypto(cryptoOut)} {sel}</div>
          <button onClick={() => onTrade(sel, usd, true)} disabled={usd <= 0 || usd > game.dollars} className="mt-2 w-full bg-[#00e5ff] py-2 font-mono text-xs font-bold text-black transition hover:bg-[#7df3ff] disabled:bg-white/10 disabled:text-[#5b6b85]">КУПИТЬ {sel}</button>
        </div>
      </div>
      <p className="mt-2 font-mono text-[10px] leading-relaxed text-[#5b6b85]">💡 Совет трейдера: покупай на падении, продавай на росте. Стелс-модуль снижает комиссию до 0%.</p>
    </div>
  );
}

// ============ UPGRADE WINDOW ============
function UpgradeWindow({ game, onBuy }: { game: GameState; onBuy: (id: string) => void }) {
  const icons: Record<string, React.ReactNode> = {
    hackSpeed: <Zap size={22} className="text-[#ffe600]" />,
    minerEff: <Pickaxe size={22} className="text-[#00e5ff]" />,
    codeLib: <BookOpen size={22} className="text-[#00ff9d]" />,
    stealth: <Ghost size={22} className="text-[#ff2d78]" />,
  };
  return (
    <div className="space-y-3 p-4">
      <div className="flex items-center gap-2 border border-[#ffe600]/30 bg-[#ffe600]/5 p-3">
        <CircleDollarSign className="text-[#ffe600]" size={20} />
        <div className="font-mono text-xs text-[#8fa3bd]">Баланс: <b className="text-base text-[#ffe600]">{fmtDollarsFull(game.dollars)}</b></div>
        <div className="ml-auto font-mono text-[10px] text-[#5b6b85]">продавай крипту на бирже → качайся</div>
      </div>
      {UPGRADES.map(u => {
        const lvl = game.upgrades[u.id];
        const maxed = lvl >= u.maxLevel;
        const cost = maxed ? 0 : u.costs[lvl];
        const afford = game.dollars >= cost;
        return (
          <div key={u.id} className="border border-white/10 bg-[#0a1120] p-3">
            <div className="flex items-start gap-3">
              <div className="flex h-11 w-11 shrink-0 items-center justify-center border border-white/10 bg-black/50">{icons[u.id]}</div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="font-mono text-[13px] font-bold text-white">{u.name}</span>
                  <span className="font-mono text-[10px] text-[#5b6b85]">ур. {lvl}/{u.maxLevel}</span>
                </div>
                <p className="mt-0.5 text-[12px] leading-snug text-[#8fa3bd]">{u.desc}</p>
                <div className="mt-1.5 flex gap-1">
                  {Array.from({ length: u.maxLevel }).map((_, i) => (
                    <div key={i} className={cn('h-1.5 flex-1', i < lvl ? 'bg-[#00ff9d]' : 'bg-white/10')} />
                  ))}
                </div>
                <div className="mt-1 font-mono text-[10px] text-[#00e5ff]">Сейчас: {u.effects[lvl]}{!maxed && <span className="text-[#5b6b85]"> → {u.effects[lvl + 1]}</span>}</div>
              </div>
              <div className="shrink-0">
                {maxed ? (
                  <span className="flex items-center gap-1 border border-[#00ff9d]/40 bg-[#00ff9d]/10 px-3 py-2 font-mono text-[11px] font-bold text-[#00ff9d]"><CheckCircle2 size={13} /> MAX</span>
                ) : (
                  <button onClick={() => onBuy(u.id)} disabled={!afford} className={cn('px-3 py-2 font-mono text-[11px] font-bold transition', afford ? 'bg-[#ffe600] text-black hover:bg-[#fff36b]' : 'bg-white/10 text-[#5b6b85]')}>
                    ${cost.toLocaleString('ru-RU')}
                  </button>
                )}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

// ============ LEARN WINDOW ============
function LearnWindow({ game, onCompleteLesson }: { game: GameState; onCompleteLesson: (id: number, xp: number) => void }) {
  const [sel, setSel] = useState(1);
  const [answers, setAnswers] = useState<Record<number, number>>({});
  const lesson = LESSONS.find(l => l.id === sel)!;
  const done = game.completedLessons.includes(sel);
  const correct = lesson.quiz.filter((q, i) => answers[i] === q.answer).length;
  const allAnswered = lesson.quiz.every((_, i) => answers[i] !== undefined);
  const passed = correct === lesson.quiz.length;

  useEffect(() => { setAnswers({}); }, [sel]);

  return (
    <div className="flex flex-col sm:flex-row">
      <div className="w-full shrink-0 border-b border-white/10 bg-[#050b16] sm:w-52 sm:border-b-0 sm:border-r">
        <div className="border-b border-white/10 px-3 py-2 font-mono text-[10px] tracking-widest text-[#5b6b85]">ШКОЛА PYTHON · {game.completedLessons.length}/{LESSONS.length}</div>
        {LESSONS.map(l => {
          const d = game.completedLessons.includes(l.id);
          return (
            <button key={l.id} onClick={() => setSel(l.id)} className={cn('flex w-full items-center gap-2 border-l-2 px-3 py-2.5 text-left', sel === l.id ? 'border-[#ff2d78] bg-[#ff2d78]/10' : 'border-transparent hover:bg-white/5')}>
              <span className={cn('flex h-6 w-6 shrink-0 items-center justify-center font-mono text-[11px] font-bold', d ? 'bg-[#00ff9d] text-black' : 'bg-[#ff2d78]/20 text-[#ff2d78]')}>{d ? <CheckCircle2 size={13} /> : l.id}</span>
              <span>
                <span className="block font-mono text-[11px] font-bold text-white">{l.title}</span>
                <span className="block font-mono text-[10px] text-[#5b6b85]">{l.duration} · +{l.xp} XP</span>
              </span>
            </button>
          );
        })}
      </div>
      <div className="min-w-0 flex-1 p-4">
        <h4 className="font-mono text-base font-bold text-white">{lesson.title}</h4>
        <p className="font-mono text-[11px] text-[#ff2d78]">{lesson.subtitle}</p>
        <div className="mt-3 space-y-3">
          {lesson.content.map((b, i) => (
            <div key={i} className="border border-white/10 bg-black/40 p-3">
              <div className="font-mono text-[12px] font-bold text-[#00e5ff]">{b.heading}</div>
              <p className="mt-1 text-[13px] leading-relaxed text-[#b9c7dd]">{b.text}</p>
              {b.code && <pre className="mt-2 overflow-x-auto border border-white/10 bg-black p-2 font-mono text-[12px] leading-5 text-[#00ff9d]">{b.code}</pre>}
            </div>
          ))}
        </div>
        <div className="mt-3 border border-[#ffe600]/30 bg-[#ffe600]/5 p-3">
          <div className="mb-2 font-mono text-[12px] font-bold text-[#ffe600]">✎ ПРОВЕРКА ЗНАНИЙ ({correct}/{lesson.quiz.length})</div>
          {lesson.quiz.map((q, qi) => (
            <div key={qi} className="mb-2.5">
              <div className="text-[13px] font-medium text-white">{qi + 1}. {q.q}</div>
              <div className="mt-1 flex flex-wrap gap-1.5">
                {q.options.map((o, oi) => {
                  const picked = answers[qi] === oi;
                  const showRight = answers[qi] !== undefined && oi === q.answer;
                  const showWrong = picked && oi !== q.answer;
                  return (
                    <button key={oi} disabled={done} onClick={() => setAnswers({ ...answers, [qi]: oi })} className={cn('border px-2.5 py-1 font-mono text-[11px] transition', showRight ? 'border-[#00ff9d] bg-[#00ff9d]/20 text-[#00ff9d]' : showWrong ? 'border-[#ff2d78] bg-[#ff2d78]/20 text-[#ff2d78]' : picked ? 'border-white/40 text-white' : 'border-white/15 text-[#8fa3bd] hover:border-white/40 hover:text-white')}>
                      {o}
                    </button>
                  );
                })}
              </div>
            </div>
          ))}
          {done ? (
            <div className="flex items-center gap-2 font-mono text-[12px] font-bold text-[#00ff9d]"><CheckCircle2 size={15} /> Урок пройден! +{lesson.xp} XP получено</div>
          ) : (
            <button disabled={!allAnswered || !passed} onClick={() => onCompleteLesson(lesson.id, lesson.xp)} className={cn('mt-1 w-full py-2 font-mono text-xs font-bold', allAnswered && passed ? 'bg-[#ffe600] text-black hover:bg-[#fff36b]' : 'bg-white/10 text-[#5b6b85]')}>
              {!allAnswered ? 'ОТВЕТЬ НА ВСЕ ВОПРОСЫ' : !passed ? 'ЕСТЬ ОШИБКИ — ПОПРОБУЙ ЕЩЁ' : `ЗАБРАТЬ +${lesson.xp} XP`}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

// ============ FILES WINDOW ============
function FilesWindow({ game }: { game: GameState }) {
  const [path, setPath] = useState('home');
  const [openFile, setOpenFile] = useState<string | null>(null);
  const files: Record<string, { name: string; type: 'folder' | 'txt' | 'key' | 'exe'; content?: string }[]> = {
    home: [
      { name: 'Документы', type: 'folder' },
      { name: 'Эксплойты', type: 'folder' },
      { name: 'Кошельки', type: 'folder' },
      { name: 'README.txt', type: 'txt', content: 'NEON_OS v3.7 — система анонимного хакера.\n\nПравила выживания:\n1. Никогда не взламывай без VPN (у нас он встроен).\n2. Майнеры — твой пассивный доход. Ставь на каждый взломанный комп.\n3. Продавай крипту на пике, покупай на дне.\n4. Учи Python. Код — единственное оружие, которое нельзя отобрать.\n\n— ghost' },
      { name: 'план.txt', type: 'txt', content: 'ПЛАН СТАНОВЛЕНИЯ ЛЕГЕНДОЙ:\n\n[ ] Взломать 8 целей\n[ ] Открыть все 4 монеты (BTC, ETH, XMR, SOL)\n[ ] Поставить 5+ майнеров\n[ ] Прокачать всё до MAX\n[ ] Заработать $50,000\n\nУдачи, будущий кит.' },
    ],
    'Документы': [
      { name: 'пароли.txt', type: 'txt', content: 'Слитые пароли (не использовать для зла... ладно, использовать):\n\nadmin:123456\nroot:qwerty\ntrader:neon-77  <-- тот самый ключ из миссии 5!\nminer:ferma2024' },
      { name: 'шпаргалка_python.txt', type: 'txt', content: 'ШПАРГАЛКА PYTHON:\n\nprint("текст")     — вывод\nx = 5             — переменная\nif x == 5:        — условие (два равно!)\nfor i in range(5): — цикл 5 раз\nwhile x > 0:      — цикл пока верно\ndef f():          — своя функция\nlist = [1,2,3]    — список\n\n# — комментарий\n: — двоеточие перед блоком\n4 пробела — отступ блока' },
    ],
    'Эксплойты': MISSIONS.filter(m => game.completedMissions.includes(m.id)).map(m => ({
      name: `exploit_${m.id}_${m.title.replace(/\s/g, '_')}.py`, type: 'exe' as const, content: `# ${m.title} — успешно применён!\n# Цель: ${m.targetName} (${m.targetIp})\n\n${m.solution}\n\n# Награда: ${m.rewardAmount} ${m.rewardCrypto} + $${m.rewardDollars}`
    })),
    'Кошельки': CRYPTOS.map(c => ({
      name: `${c.id}_wallet.dat`, type: 'key' as const,
      content: `${c.fullName} WALLET\nАдрес: ${c.id.toLowerCase()}1qxy...${Math.floor(Math.random() * 9000 + 1000)}\nБаланс: ${fmtCrypto(game.crypto[c.id])} ${c.id}\nСтатус: ${game.level >= c.unlockLevel ? 'АКТИВЕН' : 'ЗАБЛОКИРОВАН (нужен уровень ' + c.unlockLevel + ')'}`,
    })),
  };
  const list = files[path] || [];

  return (
    <div className="flex min-h-[320px] flex-col sm:flex-row">
      <div className="w-full shrink-0 border-b border-white/10 bg-[#050b16] p-2 sm:w-44 sm:border-b-0 sm:border-r">
        {['home', 'Документы', 'Эксплойты', 'Кошельки'].map(p => (
          <button key={p} onClick={() => { setPath(p); setOpenFile(null); }} className={cn('flex w-full items-center gap-2 px-2.5 py-2 font-mono text-[12px]', path === p ? 'bg-[#00e5ff]/15 text-[#00e5ff]' : 'text-[#8fa3bd] hover:bg-white/5')}>
            <Folder size={14} /> {p === 'home' ? '/home/ghost' : p}
          </button>
        ))}
      </div>
      <div className="min-w-0 flex-1 p-3">
        <div className="mb-2 font-mono text-[11px] text-[#5b6b85]">📁 /home/ghost/{path === 'home' ? '' : path + '/'} · {list.length} объектов</div>
        {!openFile ? (
          <div className="grid grid-cols-1 gap-1.5 sm:grid-cols-2">
            {list.length === 0 && <div className="col-span-2 border border-dashed border-white/15 p-4 text-center font-mono text-[11px] text-[#5b6b85]">пусто... пока что</div>}
            {list.map(f => (
              <button key={f.name} onClick={() => { if (f.type === 'folder') { setPath(f.name); } else setOpenFile(f.name); }} className="flex items-center gap-2 border border-white/10 bg-black/40 px-2.5 py-2 text-left font-mono text-[11px] text-[#d7e3f4] hover:border-[#00e5ff]/50">
                {f.type === 'folder' ? <Folder size={15} className="shrink-0 text-[#ffe600]" /> : f.type === 'key' ? <Wallet size={15} className="shrink-0 text-[#00ff9d]" /> : f.type === 'exe' ? <Terminal size={15} className="shrink-0 text-[#ff2d78]" /> : <FileText size={15} className="shrink-0 text-[#00e5ff]" />}
                <span className="truncate">{f.name}</span>
              </button>
            ))}
          </div>
        ) : (
          <div className="border border-white/15 bg-black/60">
            <div className="flex items-center justify-between border-b border-white/10 px-3 py-1.5">
              <span className="font-mono text-[11px] text-[#00e5ff]">{openFile}</span>
              <button onClick={() => setOpenFile(null)} className="flex items-center gap-1 font-mono text-[10px] text-[#5b6b85] hover:text-white"><ChevronLeft size={12} /> назад</button>
            </div>
            <pre className="max-h-64 overflow-y-auto whitespace-pre-wrap p-3 font-mono text-[12px] leading-5 text-[#b9c7dd]">{list.find(f => f.name === openFile)?.content}</pre>
          </div>
        )}
      </div>
    </div>
  );
}

// ============ PROFILE ============
function ProfileWindow({ game, prices, minerIncome }: { game: GameState; prices: Record<CryptoId, number>; minerIncome: number }) {
  const portfolio = CRYPTOS.reduce((s, c) => s + game.crypto[c.id] * prices[c.id], 0);
  const total = portfolio + game.dollars;
  const ach = [
    { n: 'Первый взлом', d: 'Взломай первую цель', ok: game.totalHacked >= 1, icon: Terminal },
    { n: 'Серийный хакер', d: 'Взломай 4 цели', ok: game.totalHacked >= 4, icon: Zap },
    { n: 'Легенда даркнета', d: 'Взломай все 8 целей', ok: game.totalHacked >= 8, icon: Trophy },
    { n: 'Фермер', d: 'Установи первый майнер', ok: game.miners.length >= 1, icon: Pickaxe },
    { n: 'Магнат', d: 'Держи 4+ майнера', ok: game.miners.length >= 4, icon: Cpu },
    { n: 'Трейдер', d: 'Соверши 5 сделок', ok: game.totalTrades >= 5, icon: TrendingUp },
    { n: 'Студент', d: 'Пройди 3 урока Python', ok: game.completedLessons.length >= 3, icon: GraduationCap },
    { n: 'Кит', d: 'Капитал $10,000+', ok: total >= 10000, icon: Sparkles },
  ];
  return (
    <div className="p-4">
      <div className="flex items-center gap-3 border border-[#00ff9d]/30 bg-[#00ff9d]/5 p-3">
        <div className="flex h-14 w-14 items-center justify-center border border-[#00ff9d]/50 bg-black font-mono text-2xl">👨‍💻</div>
        <div>
          <div className="font-mono text-sm font-bold text-white">ghost <span className="text-[#00ff9d] glow-green">[ур. {game.level}]</span></div>
          <div className="font-mono text-[11px] text-[#8fa3bd]">статус: {game.level >= 5 ? 'ЛЕГЕНДА ДАРКНЕТА' : game.level >= 3 ? 'ОПЫТНЫЙ ХАКЕР' : 'СКРИПТ-КИДДИ'}</div>
          <div className="mt-1 h-1.5 w-48 max-w-full bg-white/10"><div className="h-full bg-[#00ff9d]" style={{ width: `${Math.min(100, (game.xp / xpForLevel(game.level)) * 100)}%` }} /></div>
          <div className="font-mono text-[10px] text-[#5b6b85]">{game.xp} / {xpForLevel(game.level)} XP</div>
        </div>
        <div className="ml-auto text-right">
          <div className="font-mono text-[10px] text-[#5b6b85]">КАПИТАЛ</div>
          <div className="font-mono text-lg font-bold text-[#ffe600] glow-yellow">{fmtDollarsFull(total)}</div>
          <div className="font-mono text-[10px] text-[#8fa3bd]">${minerIncome.toFixed(2)}/мин майнинг</div>
        </div>
      </div>
      <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-4">
        <div className="border border-white/10 bg-black/40 p-2.5 text-center"><div className="font-mono text-xl font-bold text-[#00ff9d]">{game.totalHacked}</div><div className="font-mono text-[10px] text-[#5b6b85]">ВЗЛОМОВ</div></div>
        <div className="border border-white/10 bg-black/40 p-2.5 text-center"><div className="font-mono text-xl font-bold text-[#00e5ff]">{game.miners.length}</div><div className="font-mono text-[10px] text-[#5b6b85]">МАЙНЕРОВ</div></div>
        <div className="border border-white/10 bg-black/40 p-2.5 text-center"><div className="font-mono text-xl font-bold text-[#ffe600]">{game.totalTrades}</div><div className="font-mono text-[10px] text-[#5b6b85]">СДЕЛОК</div></div>
        <div className="border border-white/10 bg-black/40 p-2.5 text-center"><div className="font-mono text-xl font-bold text-[#ff2d78]">{game.completedLessons.length}</div><div className="font-mono text-[10px] text-[#5b6b85]">УРОКОВ</div></div>
      </div>
      <div className="mb-2 mt-3 font-mono text-[11px] font-bold tracking-widest text-[#8fa3bd]">ДОСТИЖЕНИЯ · {ach.filter(a => a.ok).length}/{ach.length}</div>
      <div className="grid gap-1.5 sm:grid-cols-2">
        {ach.map((a, i) => (
          <div key={i} className={cn('flex items-center gap-2 border p-2', a.ok ? 'border-[#ffe600]/40 bg-[#ffe600]/5' : 'border-white/10 opacity-50')}>
            <a.icon size={17} className={a.ok ? 'text-[#ffe600]' : 'text-[#5b6b85]'} />
            <div><div className="font-mono text-[11px] font-bold text-white">{a.ok ? '🏆 ' : ''}{a.n}</div><div className="font-mono text-[10px] text-[#5b6b85]">{a.d}</div></div>
          </div>
        ))}
      </div>
    </div>
  );
}

// ============ MAIN APP ============
type WinId = 'hack' | 'miner' | 'trade' | 'upgrade' | 'learn' | 'files' | 'profile';

const WIN_META: Record<WinId, { title: string; color: string; icon: React.ReactNode }> = {
  hack: { title: 'NEON_HACK // ТЕРМИНАЛ ВЗЛОМА', color: '#00ff9d', icon: <Terminal size={14} /> },
  miner: { title: 'МАЙНИНГ-ФЕРМА', color: '#00e5ff', icon: <Pickaxe size={14} /> },
  trade: { title: 'БИРЖА DARKEX', color: '#ffe600', icon: <TrendingUp size={14} /> },
  upgrade: { title: 'ЧЁРНЫЙ РЫНОК // АПГРЕЙДЫ', color: '#ff9f43', icon: <ArrowUpCircle size={14} /> },
  learn: { title: 'ШКОЛА PYTHON', color: '#ff2d78', icon: <GraduationCap size={14} /> },
  files: { title: 'ФАЙЛЫ // /home/ghost', color: '#00e5ff', icon: <FolderOpen size={14} /> },
  profile: { title: 'ПРОФИЛЬ ХАКЕРА', color: '#ffe600', icon: <User size={14} /> },
};

export default function App() {
  const [screen, setScreen] = useState<'start' | 'boot' | 'desktop'>('start');
  const [game, setGame] = useState<GameState>(DEFAULT_STATE);
  const [hasSave, setHasSave] = useState(false);
  const [openWins, setOpenWins] = useState<WinId[]>([]);
  const [zOrder, setZOrder] = useState<WinId[]>([]);
  const [missionId, setMissionId] = useState(1);
  const [prices, setPrices] = useState<Record<CryptoId, number>>(() => Object.fromEntries(CRYPTOS.map(c => [c.id, c.basePrice])) as Record<CryptoId, number>);
  const [history, setHistory] = useState<Record<CryptoId, number[]>>(() => Object.fromEntries(CRYPTOS.map(c => [c.id, Array.from({ length: 30 }, () => c.basePrice * (1 + (Math.random() - 0.5) * 0.02))])) as Record<CryptoId, number[]>);
  const [toasts, setToasts] = useState<Toast[]>([]);
  const [soundOn, setSoundOn] = useState(true);
  const [clock, setClock] = useState('');
  const [levelUpShow, setLevelUpShow] = useState<number | null>(null);
  const [showOnboard, setShowOnboard] = useState(false);
  const [startMenu, setStartMenu] = useState(false);
  const toastId = useRef(0);

  // load save
  useEffect(() => {
    try {
      const raw = localStorage.getItem(SAVE_KEY);
      if (raw) { setGame({ ...DEFAULT_STATE, ...JSON.parse(raw) }); setHasSave(true); }
    } catch { /* ignore */ }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // persist
  useEffect(() => {
    if (screen === 'desktop') localStorage.setItem(SAVE_KEY, JSON.stringify(game));
  }, [game, screen]);

  // clock
  useEffect(() => {
    const f = () => setClock(new Date().toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' }));
    f();
    const t = setInterval(f, 10000);
    return () => clearInterval(t);
  }, []);

  // price ticker
  useEffect(() => {
    if (screen !== 'desktop') return;
    const t = setInterval(() => {
      setPrices(prev => {
        const next = { ...prev };
        CRYPTOS.forEach(c => {
          const drift = (Math.random() - 0.5) * 2 * c.volatility + Math.sin(Date.now() / 60000 + c.basePrice) * 0.002;
          next[c.id] = Math.max(c.basePrice * 0.5, Math.min(c.basePrice * 2, prev[c.id] * (1 + drift)));
        });
        return next;
      });
      setHistory(prev => {
        const next = { ...prev };
        CRYPTOS.forEach(c => {
          next[c.id] = [...prev[c.id].slice(-39)];
        });
        return next;
      });
    }, 2500);
    return () => clearInterval(t);
  }, [screen]);

  useEffect(() => {
    setHistory(prev => {
      const next = { ...prev };
      (Object.keys(prices) as CryptoId[]).forEach(k => {
        if (prev[k][prev[k].length - 1] !== prices[k]) next[k] = [...prev[k].slice(-39), prices[k]];
      });
      return next;
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [prices]);

  // mining income tick
  const minerIncomePerMin = game.miners.reduce((s, m) => s + MINER_BASE_RATE[m.crypto] * MINER_EFF_MULT[game.upgrades.minerEff] * 60 * prices[m.crypto], 0);
  useEffect(() => {
    if (screen !== 'desktop' || game.miners.length === 0) return;
    const t = setInterval(() => {
      setGame(prev => {
        const crypto = { ...prev.crypto };
        const miners = prev.miners.map(m => {
          const gain = MINER_BASE_RATE[m.crypto] * MINER_EFF_MULT[prev.upgrades.minerEff];
          crypto[m.crypto] += gain;
          return { ...m, earned: m.earned + gain };
        });
        return { ...prev, crypto, miners };
      });
    }, 1000);
    return () => clearInterval(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [screen, game.miners.length, game.upgrades.minerEff]);

  const notify = useCallback((title: string, desc: string, kind: Toast['kind'] = 'info') => {
    const id = ++toastId.current;
    setToasts(prev => [...prev.slice(-3), { id, title, desc, kind }]);
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 4200);
  }, []);

  const addXp = useCallback((amount: number) => {
    setGame(prev => {
      let { xp, level } = prev;
      const bonus = 1 + STEALTH_XP[prev.upgrades.stealth];
      xp += Math.round(amount * bonus);
      let leveled = false;
      while (xp >= xpForLevel(level)) { xp -= xpForLevel(level); level++; leveled = true; }
      if (leveled) {
        setTimeout(() => {
          setLevelUpShow(level);
          const newly = CRYPTOS.find(c => c.unlockLevel === level);
          notify(`Уровень ${level}!`, newly ? `Разблокирована монета ${newly.name} (${newly.id})!` : 'Новые возможности открыты.', 'gold');
          if (soundOn) { playBeep(523, 0.12); setTimeout(() => playBeep(659, 0.12), 130); setTimeout(() => playBeep(784, 0.2), 260); }
        }, 300);
      }
      return { ...prev, xp, level };
    });
  }, [notify, soundOn]);

  const openWin = (id: WinId) => {
    setOpenWins(prev => (prev.includes(id) ? prev : [...prev, id]));
    setZOrder(prev => [...prev.filter(w => w !== id), id]);
    if (soundOn) playBeep(550, 0.05, 'sine', 0.03);
  };
  const closeWin = (id: WinId) => {
    setOpenWins(prev => prev.filter(w => w !== id));
    setZOrder(prev => prev.filter(w => w !== id));
  };
  const focusWin = (id: WinId) => setZOrder(prev => [...prev.filter(w => w !== id), id]);

  const handleHackSuccess = (m: Mission, _stars: number) => {
    const bonus = 1 + HACK_REWARD_BONUS[game.upgrades.hackSpeed];
    setGame(prev => ({
      ...prev,
      crypto: { ...prev.crypto, [m.rewardCrypto]: prev.crypto[m.rewardCrypto] + m.rewardAmount * bonus },
      dollars: prev.dollars + m.rewardDollars,
      totalEarnedDollars: prev.totalEarnedDollars + m.rewardDollars,
      totalHacked: prev.totalHacked + 1,
      completedMissions: [...prev.completedMissions, m.id],
    }));
    addXp(m.rewardXp);
    notify('Взлом успешен!', `+${(m.rewardAmount * bonus).toFixed(4)} ${m.rewardCrypto} · +$${m.rewardDollars} · +${m.rewardXp} XP`, 'ok');
    if (m.id === 8) notify('ТЫ — ЛЕГЕНДА!', 'Все цели взломаны. Дата-центр твой. Майни и богатей!', 'gold');
  };

  const handleInstallMiner = (missionId: number, crypto: CryptoId) => {
    const cost = 120 + game.miners.length * 80;
    if (game.dollars < cost) { notify('Нет денег', 'Продай крипту на бирже.', 'err'); return; }
    const m = MISSIONS.find(x => x.id === missionId)!;
    const miner: Miner = { id: `${Date.now()}`, pcName: m.targetName, ip: m.targetIp, crypto, earned: 0, startedAt: Date.now() };
    setGame(prev => ({ ...prev, dollars: prev.dollars - cost, miners: [...prev.miners, miner] }));
    addXp(30);
    notify('Майнер установлен', `${m.targetName} теперь майнит ${crypto}`, 'ok');
    if (soundOn) playBeep(500, 0.08);
  };

  const handleTrade = (c: CryptoId, usd: number, isBuy: boolean) => {
    const fee = STEALTH_FEE[game.upgrades.stealth];
    if (isBuy) {
      if (usd > game.dollars || usd <= 0) return;
      const got = (usd / prices[c]) * (1 - fee);
      setGame(prev => ({ ...prev, dollars: prev.dollars - usd, crypto: { ...prev.crypto, [c]: prev.crypto[c] + got }, totalTrades: prev.totalTrades + 1 }));
      addXp(10);
      notify('Покупка', `Куплено ${fmtCrypto(got)} ${c} за ${fmtDollarsFull(usd)}`, 'info');
    } else {
      const need = usd / prices[c];
      if (need > game.crypto[c] || usd <= 0) return;
      const got = usd * (1 - fee);
      setGame(prev => ({ ...prev, dollars: prev.dollars + got, crypto: { ...prev.crypto, [c]: prev.crypto[c] - need }, totalTrades: prev.totalTrades + 1, totalEarnedDollars: prev.totalEarnedDollars + got }));
      addXp(10);
      notify('Продажа', `Продано ${fmtCrypto(need)} ${c} → ${fmtDollarsFull(got)}`, 'ok');
    }
    if (soundOn) playBeep(760, 0.07);
  };

  const handleBuyUpgrade = (id: string) => {
    const u = UPGRADES.find(x => x.id === id)!;
    const lvl = game.upgrades[id];
    const cost = u.costs[lvl];
    if (game.dollars < cost) { notify('Не хватает долларов', `Нужно ${fmtDollarsFull(cost)}`, 'err'); return; }
    setGame(prev => ({ ...prev, dollars: prev.dollars - cost, upgrades: { ...prev.upgrades, [id]: lvl + 1 } }));
    notify('Апгрейд куплен', `${u.name} → уровень ${lvl + 1}`, 'gold');
    if (soundOn) { playBeep(600, 0.08); setTimeout(() => playBeep(900, 0.1), 100); }
    if (id === 'codeLib') {
      const names = ['', 'extract, drain', 'bypass, decrypt', 'install_miner, spoof', 'quantum_brute, ghost'];
      notify('Новые функции!', `Открыты: ${names[lvl + 1]}`, 'info');
    }
  };

  const handleLesson = (id: number, xp: number) => {
    setGame(prev => ({ ...prev, completedLessons: [...prev.completedLessons, id], dollars: prev.dollars + 40 }));
    addXp(xp);
    notify('Урок пройден!', `+${xp} XP · +$40 стипендия`, 'ok');
  };

  const resetGame = () => {
    localStorage.removeItem(SAVE_KEY);
    setGame(DEFAULT_STATE);
    setHasSave(false);
    setOpenWins([]);
    setPrices(Object.fromEntries(CRYPTOS.map(c => [c.id, c.basePrice])) as Record<CryptoId, number>);
    setScreen('start');
  };

  const startGame = () => {
    setScreen('boot');
  };

  if (screen === 'start') return <StartScreen onStart={startGame} hasSave={hasSave} onReset={resetGame} />;
  if (screen === 'boot') return <BootScreen onDone={() => { setScreen('desktop'); setOpenWins(['hack']); setZOrder(['hack']); if (!hasSave) setShowOnboard(true); }} />;

  const mission = MISSIONS.find(m => m.id === missionId)!;
  const icons: { id: WinId; label: string; icon: React.ReactNode; color: string; badge?: string }[] = [
    { id: 'hack', label: 'Хак-терминал', icon: <Terminal size={26} />, color: '#00ff9d', badge: `${game.completedMissions.length}/${MISSIONS.length}` },
    { id: 'miner', label: 'Майнеры', icon: <Pickaxe size={26} />, color: '#00e5ff', badge: game.miners.length > 0 ? `${game.miners.length}` : undefined },
    { id: 'trade', label: 'Биржа', icon: <TrendingUp size={26} />, color: '#ffe600' },
    { id: 'upgrade', label: 'Апгрейды', icon: <ArrowUpCircle size={26} />, color: '#ff9f43' },
    { id: 'learn', label: 'Школа Python', icon: <GraduationCap size={26} />, color: '#ff2d78', badge: `${game.completedLessons.length}/${LESSONS.length}` },
    { id: 'files', label: 'Файлы', icon: <FolderOpen size={26} />, color: '#00e5ff' },
    { id: 'profile', label: 'Профиль', icon: <User size={26} />, color: '#ffe600' },
  ];

  return (
    <div className="cyber-grid relative h-screen w-screen overflow-hidden">
      <MatrixBg opacity={0.12} />
      <div className="scan-beam" />

      {/* TOP BAR */}
      <div className="absolute left-0 right-0 top-0 z-40 flex items-center gap-2 border-b border-[#00ff9d]/25 bg-[#04070f]/95 px-2 py-1.5 backdrop-blur sm:gap-3 sm:px-4">
        <span className="flex items-center gap-1.5 font-mono text-[11px] font-bold text-[#00ff9d] sm:text-xs"><Terminal size={14} /> NEON_OS</span>
        <div className="hidden items-center gap-2 overflow-hidden font-mono text-[11px] md:flex">
          {CRYPTOS.map(c => game.level >= c.unlockLevel && (
            <span key={c.id} className="flex items-center gap-1 border border-white/10 bg-black/50 px-2 py-0.5">
              <span style={{ color: c.color }}>{c.icon}</span>
              <span className="text-white">{fmtPrice(prices[c.id])}</span>
              <span className="text-[#8fa3bd]">{fmtCrypto(game.crypto[c.id])}</span>
            </span>
          ))}
        </div>
        <div className="ml-auto flex items-center gap-2 font-mono text-[11px] sm:text-xs">
          <span className="hidden items-center gap-1 border border-[#00ff9d]/40 bg-[#00ff9d]/10 px-2 py-0.5 text-[#00ff9d] sm:flex"><Zap size={12} /> ур.{game.level} · {game.xp}/{xpForLevel(game.level)} XP</span>
          <span className="flex items-center gap-1 border border-[#ffe600]/40 bg-[#ffe600]/10 px-2 py-0.5 font-bold text-[#ffe600]"><DollarSign size={12} /> {fmtDollars(game.dollars)}</span>
          <span className="hidden items-center gap-3 text-[#5b6b85] sm:flex"><Wifi size={13} /><span>{clock}</span></span>
        </div>
      </div>

      {/* DESKTOP ICONS */}
      <div className="absolute left-3 top-14 z-10 flex flex-col gap-1 sm:left-5 sm:top-16">
        {icons.map(ic => (
          <button key={ic.id} onClick={() => openWin(ic.id)} onDoubleClick={() => openWin(ic.id)} className="group flex w-24 flex-col items-center gap-1 p-2 transition hover:bg-white/5">
            <span className="relative flex h-12 w-12 items-center justify-center border bg-[#0a111f]/90 transition group-hover:scale-105" style={{ borderColor: ic.color + '66', color: ic.color, boxShadow: `0 0 16px ${ic.color}22` }}>
              {ic.icon}
              {ic.badge && <span className="absolute -right-1.5 -top-1.5 bg-[#ff2d78] px-1 font-mono text-[9px] font-bold text-black" style={{ background: ic.color, color: '#000' }}>{ic.badge}</span>}
              {openWins.includes(ic.id) && <span className="absolute -bottom-1 h-0.5 w-6" style={{ background: ic.color }} />}
            </span>
            <span className="text-center font-mono text-[10px] leading-tight text-[#d7e3f4] [text-shadow:0_1px_4px_black]">{ic.label}</span>
          </button>
        ))}
      </div>

      {/* desktop hint */}
      {openWins.length === 0 && (
        <div className="pointer-events-none absolute inset-0 z-0 flex items-center justify-center">
          <div className="text-center">
            <div className="font-display text-4xl font-black text-white/5 sm:text-6xl" style={{ fontFamily: 'Unbounded' }}>GHOST<br />NETWORK</div>
            <p className="mt-2 font-mono text-xs text-[#3a4a63]">кликни по иконке слева, чтобы открыть программу</p>
          </div>
        </div>
      )}

      {/* WINDOWS */}
      <AnimatePresence>
        {openWins.map(id => (
          <WindowFrame key={id} id={id} title={WIN_META[id].title} icon={WIN_META[id].icon} color={WIN_META[id].color} z={10 + zOrder.indexOf(id)} onClose={() => closeWin(id)} onFocus={() => focusWin(id)} wide={id === 'hack' || id === 'learn'}>
            {id === 'hack' && <HackWindow mission={mission} setMissionId={setMissionId} game={game} onHackSuccess={handleHackSuccess} soundOn={soundOn} />}
            {id === 'miner' && <MinerWindow game={game} onInstall={handleInstallMiner} onRemove={(mid) => setGame(p => ({ ...p, miners: p.miners.filter(m => m.id !== mid) }))} minerIncome={minerIncomePerMin} />}
            {id === 'trade' && <TradeWindow game={game} prices={prices} history={history} onTrade={handleTrade} fee={STEALTH_FEE[game.upgrades.stealth]} />}
            {id === 'upgrade' && <UpgradeWindow game={game} onBuy={handleBuyUpgrade} />}
            {id === 'learn' && <LearnWindow game={game} onCompleteLesson={handleLesson} />}
            {id === 'files' && <FilesWindow game={game} />}
            {id === 'profile' && <ProfileWindow game={game} prices={prices} minerIncome={minerIncomePerMin} />}
          </WindowFrame>
        ))}
      </AnimatePresence>

      {/* TASKBAR */}
      <div className="absolute bottom-0 left-0 right-0 z-40 border-t border-[#00ff9d]/25 bg-[#04070f]/95 backdrop-blur">
        <div className="flex items-center gap-1.5 px-2 py-1.5">
          <div className="relative">
            <button onClick={() => setStartMenu(!startMenu)} className={cn('flex items-center gap-1.5 px-3 py-1.5 font-mono text-[11px] font-bold tracking-wider transition', startMenu ? 'bg-[#00ff9d] text-black' : 'bg-[#00ff9d]/15 text-[#00ff9d] hover:bg-[#00ff9d]/25')}>
              <Power size={13} /> GHOST
            </button>
            {startMenu && (
              <div className="absolute bottom-full left-0 mb-1 w-52 border border-[#00ff9d]/40 bg-[#070d1a] p-1.5 shadow-2xl">
                <button onClick={() => { setSoundOn(!soundOn); }} className="flex w-full items-center gap-2 px-2.5 py-2 font-mono text-[11px] text-[#d7e3f4] hover:bg-white/5">{soundOn ? <Volume2 size={14} /> : <VolumeX size={14} />} Звук: {soundOn ? 'ВКЛ' : 'ВЫКЛ'}</button>
                <button onClick={() => { setShowOnboard(true); setStartMenu(false); }} className="flex w-full items-center gap-2 px-2.5 py-2 font-mono text-[11px] text-[#d7e3f4] hover:bg-white/5"><BookOpen size={14} /> Обучение заново</button>
                <button onClick={resetGame} className="flex w-full items-center gap-2 px-2.5 py-2 font-mono text-[11px] text-[#ff2d78] hover:bg-[#ff2d78]/10"><RotateCcw size={14} /> Сбросить прогресс</button>
                <button onClick={() => setScreen('start')} className="flex w-full items-center gap-2 px-2.5 py-2 font-mono text-[11px] text-[#d7e3f4] hover:bg-white/5"><X size={14} /> В главное меню</button>
              </div>
            )}
          </div>
          <div className="flex flex-1 gap-1 overflow-x-auto">
            {openWins.map(id => (
              <button key={id} onClick={() => closeWin(id)} className="flex shrink-0 items-center gap-1.5 border px-2.5 py-1.5 font-mono text-[10px]" style={{ borderColor: WIN_META[id].color + '55', color: WIN_META[id].color, background: '#0a111f' }}>
                {WIN_META[id].icon}<span className="hidden sm:inline">{WIN_META[id].title.split('//')[0]}</span><X size={11} className="opacity-50" />
              </button>
            ))}
            {openWins.length === 0 && <span className="px-2 py-1.5 font-mono text-[10px] text-[#3a4a63]">нет открытых окон</span>}
          </div>
          <div className="hidden items-center gap-2 font-mono text-[10px] text-[#5b6b85] md:flex">
            {game.miners.length > 0 && <span className="flex items-center gap-1 text-[#00ff9d]"><Pickaxe size={11} /> +${minerIncomePerMin.toFixed(2)}/мин</span>}
            <span className="flex items-center gap-1"><Coins size={11} /> {fmtCrypto(game.crypto.BTC)} BTC</span>
            <span>{clock}</span>
          </div>
        </div>
      </div>

      {/* TOASTS */}
      <div className="absolute bottom-16 right-3 z-50 flex w-72 flex-col gap-2">
        <AnimatePresence>
          {toasts.map(t => (
            <motion.div key={t.id} initial={{ opacity: 0, x: 60 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: 60 }} className={cn('border bg-[#070d1a]/95 p-3 backdrop-blur',
              t.kind === 'ok' && 'border-[#00ff9d]/50',
              t.kind === 'err' && 'border-[#ff2d78]/50',
              t.kind === 'info' && 'border-[#00e5ff]/50',
              t.kind === 'gold' && 'border-[#ffe600]/50')}>
              <div className={cn('font-mono text-[12px] font-bold', t.kind === 'ok' && 'text-[#00ff9d]', t.kind === 'err' && 'text-[#ff2d78]', t.kind === 'info' && 'text-[#00e5ff]', t.kind === 'gold' && 'text-[#ffe600]')}>{t.title}</div>
              <div className="mt-0.5 text-[12px] text-[#b9c7dd]">{t.desc}</div>
            </motion.div>
          ))}
        </AnimatePresence>
      </div>

      {/* LEVEL UP MODAL */}
      <AnimatePresence>
        {levelUpShow !== null && (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="fixed inset-0 z-[60] flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm" onClick={() => setLevelUpShow(null)}>
            <motion.div initial={{ scale: 0.85, y: 24 }} animate={{ scale: 1, y: 0 }} className="box-glow-green w-full max-w-sm border border-[#ffe600]/60 bg-[#070d1a] p-6 text-center" onClick={e => e.stopPropagation()}>
              <Trophy size={44} className="mx-auto text-[#ffe600]" />
              <h3 className="mt-2 font-display text-2xl font-black text-[#ffe600] glow-yellow" style={{ fontFamily: 'Unbounded' }}>LEVEL {levelUpShow}</h3>
              <p className="mt-2 text-sm text-[#b9c7dd]">
                {CRYPTOS.find(c => c.unlockLevel === levelUpShow) ? <>Разблокирована <b className="text-white">{CRYPTOS.find(c => c.unlockLevel === levelUpShow)!.name}</b>! Новые миссии и майнеры ждут.</> : 'Ты становишься сильнее. Продолжай взламывать!'}
              </p>
              <button onClick={() => setLevelUpShow(null)} className="mt-4 w-full bg-[#ffe600] py-2.5 font-mono text-xs font-bold text-black hover:bg-[#fff36b]">ЗАБРАТЬ НАГРАДУ</button>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* ONBOARDING */}
      <AnimatePresence>
        {showOnboard && (
          <Onboarding onClose={() => setShowOnboard(false)} onOpen={(w: WinId) => { openWin(w); }} />
        )}
      </AnimatePresence>
    </div>
  );
}

function Onboarding({ onClose, onOpen }: { onClose: () => void; onOpen: (w: WinId) => void }) {
  const [step, setStep] = useState(0);
  const steps = [
    { t: 'Добро пожаловать, ghost!', d: 'Это NeonOS — твоя хакерская ОС. Слева — иконки программ, сверху — твои деньги и крипта, снизу — панель задач.', icon: <Ghost size={30} className="text-[#00ff9d]" /> },
    { t: 'Шаг 1: Взломай первую цель', d: 'Открой «Хак-терминал». Там миссии, которые шаг за шагом учат Python: функции, переменные, циклы, условия. Просто следуй подсказкам и жми «Запустить».', icon: <Terminal size={30} className="text-[#00ff9d]" />, go: 'hack' as WinId, goLabel: 'Открыть терминал' },
    { t: 'Шаг 2: Ставь майнеры', d: 'Каждый взломанный комп — источник пассивного дохода. Открой «Майнеры» и установи риг — крипта будет капать каждую секунду.', icon: <Pickaxe size={30} className="text-[#00e5ff]" />, go: 'miner' as WinId, goLabel: 'Открыть майнеры' },
    { t: 'Шаг 3: Торгуй и качайся', d: 'Продавай крипту за доллары на «Бирже», а доллары трать на «Апгрейды»: скорость взлома, мощность майнинга и новые функции кода. «Школа Python» даст теорию и XP.', icon: <TrendingUp size={30} className="text-[#ffe600]" />, go: 'trade' as WinId, goLabel: 'Открыть биржу' },
  ];
  const s = steps[step];
  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="fixed inset-0 z-[60] flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm">
      <motion.div initial={{ y: 24, scale: 0.95 }} animate={{ y: 0, scale: 1 }} className="box-glow-cyan w-full max-w-md border border-[#00e5ff]/50 bg-[#070d1a] p-6">
        <div className="flex items-center gap-3">
          {s.icon}
          <div>
            <div className="font-mono text-[10px] tracking-widest text-[#5b6b85]">ОБУЧЕНИЕ · {step + 1}/{steps.length}</div>
            <h3 className="font-mono text-base font-bold text-white">{s.t}</h3>
          </div>
        </div>
        <p className="mt-3 text-sm leading-relaxed text-[#b9c7dd]">{s.d}</p>
        <div className="mb-4 mt-3 flex gap-1">{steps.map((_, i) => <div key={i} className={cn('h-1 flex-1', i <= step ? 'bg-[#00e5ff]' : 'bg-white/10')} />)}</div>
        <div className="flex gap-2">
          {step > 0 && <button onClick={() => setStep(step - 1)} className="flex items-center gap-1 border border-white/20 px-4 py-2 font-mono text-xs text-[#8fa3bd] hover:text-white"><ChevronLeft size={14} /> Назад</button>}
          <div className="flex-1" />
          {s.go && <button onClick={() => { onOpen(s.go!); }} className="border border-[#00ff9d]/50 bg-[#00ff9d]/10 px-4 py-2 font-mono text-xs font-bold text-[#00ff9d] hover:bg-[#00ff9d]/20">{s.goLabel}</button>}
          {step < steps.length - 1 ? (
            <button onClick={() => setStep(step + 1)} className="flex items-center gap-1 bg-[#00e5ff] px-4 py-2 font-mono text-xs font-bold text-black hover:bg-[#7df3ff]">Далее <ChevronRight size={14} /></button>
          ) : (
            <button onClick={onClose} className="bg-[#00ff9d] px-5 py-2 font-mono text-xs font-bold text-black hover:bg-[#5cffb8]">НАЧАТЬ ВЗЛОМ!</button>
          )}
        </div>
        <button onClick={onClose} className="mt-3 w-full font-mono text-[10px] text-[#3a4a63] hover:text-white">пропустить обучение</button>
      </motion.div>
    </motion.div>
  );
}
