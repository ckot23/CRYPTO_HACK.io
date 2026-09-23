export type CryptoId = 'BTC' | 'ETH' | 'XMR' | 'SOL';

export interface CryptoInfo {
  id: CryptoId;
  name: string;
  fullName: string;
  icon: string;
  color: string;
  basePrice: number;
  unlockLevel: number;
  desc: string;
  volatility: number;
}

export const CRYPTOS: CryptoInfo[] = [
  { id: 'BTC', name: 'Bitcoin', fullName: 'Bitcoin', icon: '₿', color: '#ff9f43', basePrice: 67400, unlockLevel: 1, desc: 'Первая и самая дорогая крипта. Идеальна для старта.', volatility: 0.012 },
  { id: 'ETH', name: 'Ethereum', fullName: 'Ethereum', icon: 'Ξ', color: '#00e5ff', basePrice: 3520, unlockLevel: 2, desc: 'Умные контракты. Стабильный доход со взломов.', volatility: 0.018 },
  { id: 'XMR', name: 'Monero', fullName: 'Monero', icon: 'ɱ', color: '#ff2d78', basePrice: 128, unlockLevel: 3, desc: 'Анонимная монета хакеров. Высокая волатильность.', volatility: 0.028 },
  { id: 'SOL', name: 'Solana', fullName: 'Solana', icon: '◎', color: '#00ff9d', basePrice: 172, unlockLevel: 4, desc: 'Сверхбыстрые транзакции. Джекпот для профи.', volatility: 0.035 },
];

export interface Mission {
  id: number;
  title: string;
  targetName: string;
  targetIp: string;
  os: string;
  security: number;
  difficulty: 'ЛЕГКО' | 'СРЕДНЕ' | 'СЛОЖНО' | 'ЭКСПЕРТ';
  concept: string;
  conceptDesc: string;
  briefing: string;
  task: string;
  starterCode: string;
  solution: string;
  hints: string[];
  requiredPatterns: string[];
  rewardCrypto: CryptoId;
  rewardAmount: number;
  rewardDollars: number;
  rewardXp: number;
  requiredCodeLib: number;
  requiredLevel: number;
  theory: string[];
}

export const MISSIONS: Mission[] = [
  {
    id: 1,
    title: 'Первый скан',
    targetName: 'Домашний ПК школьника',
    targetIp: '192.168.4.12',
    os: 'Win10 Home',
    security: 5,
    difficulty: 'ЛЕГКО',
    concept: 'Функции и вызовы: scan()',
    conceptDesc: 'В Python функция — это команда. Чтобы её запустить, напиши имя и скобки ().',
    briefing: 'Новичок, добро пожаловать в сеть. Начнём с простого: какой-то школьник хранит 0.0012 BTC на домашнем компе и даже пароль не сменил. Твоя задача — просканировать сеть и найти его машину.',
    task: 'Вызови функцию scan(), чтобы найти устройства в сети.',
    starterCode: '# Твоя первая хакерская команда\n# Вызови функцию scan() — просто напиши её имя и скобки\n\n',
    solution: 'scan()',
    hints: [
      'Функции в Python вызываются так: имя_функции()',
      'Просто напиши на новой строке: scan()',
      'Нажми «ЗАПУСТИТЬ», когда напишешь команду.',
    ],
    requiredPatterns: ['scan\\s*\\(\\s*\\)'],
    rewardCrypto: 'BTC',
    rewardAmount: 0.0012,
    rewardDollars: 50,
    rewardXp: 120,
    requiredCodeLib: 0,
    requiredLevel: 1,
    theory: [
      'Функция — это готовый блок кода, который выполняет действие.',
      'scan() — наша хакерская функция. Она сканирует сеть и возвращает список устройств.',
      'Скобки () означают «выполнить». Без них Python просто посмотрит на функцию, но не запустит.',
    ],
  },
  {
    id: 2,
    title: 'Переменные',
    targetName: 'Офисный ноутбук бухгалтера',
    targetIp: '10.0.2.44',
    os: 'Win11 Pro',
    security: 12,
    difficulty: 'ЛЕГКО',
    concept: 'Переменные: target = ...',
    conceptDesc: 'Переменная — это коробка с именем, где хранится значение. Создаётся через =.',
    briefing: 'Нашёл сеть? Красава. Теперь бухгалтер с ETH-кошельком. IP его ноута постоянно меняется, поэтому сохрани результат сканирования в переменную, а потом используй её.',
    task: '1) Сохрани результат scan() в переменную target\n2) Выведи её через print(target)',
    starterCode: '# Переменная хранит данные. Синтаксис: имя = значение\n# Шаг 1: сохрани скан в переменную target\n# Шаг 2: выведи её командой print(target)\n\n',
    solution: 'target = scan()\nprint(target)',
    hints: [
      'Первая строка: target = scan()',
      'Вторая строка: print(target) — без кавычек, ведь это переменная!',
      'print("target") и print(target) — разные вещи. Кавычки = текст, без кавычек = переменная.',
    ],
    requiredPatterns: ['target\\s*=\\s*scan\\s*\\(', 'print\\s*\\(\\s*target\\s*\\)'],
    rewardCrypto: 'BTC',
    rewardAmount: 0.0021,
    rewardDollars: 80,
    rewardXp: 150,
    requiredCodeLib: 0,
    requiredLevel: 1,
    theory: [
      'Переменная создаётся знаком =. Слева имя, справа значение.',
      'target = scan() — «положи то, что нашёл scan(), в коробку target».',
      'print() выводит на экран. Внутри можно писать текст в кавычках или имя переменной без кавычек.',
    ],
  },
  {
    id: 3,
    title: 'Подключение',
    targetName: 'Криптообменник «Быстрые деньги»',
    targetIp: '172.16.8.101',
    os: 'Ubuntu Server',
    security: 22,
    difficulty: 'ЛЕГКО',
    concept: 'Аргументы функций: connect(ip)',
    conceptDesc: 'Функции могут принимать данные — аргументы. Они пишутся в скобках.',
    briefing: 'Переменные освоил. Теперь настоящее проникновение: сервер мелкого обменника. Нужно подключиться к нему через connect(). Функция ждёт IP-адрес — передай его как аргумент.',
    task: '1) Сохрани IP "172.16.8.101" в переменную ip\n2) Подключись: connect(ip)',
    starterCode: '# Строки (текст) пишутся в кавычках: "текст"\n# Сохрани IP в переменную ip, затем подключись\n\n',
    solution: 'ip = "172.16.8.101"\nconnect(ip)',
    hints: [
      'Строка — это текст в кавычках: ip = "172.16.8.101"',
      'Затем вызови connect(ip) — передаём переменную внутрь скобок.',
      'Можно и так: connect("172.16.8.101") — но через переменную правильнее!',
    ],
    requiredPatterns: ['ip\\s*=\\s*["\']172\\.16\\.8\\.101["\']', 'connect\\s*\\('],
    rewardCrypto: 'BTC',
    rewardAmount: 0.0035,
    rewardDollars: 120,
    rewardXp: 180,
    requiredCodeLib: 0,
    requiredLevel: 1,
    theory: [
      'Аргумент — данные, которые мы передаём функции внутрь скобок.',
      'connect(ip) означает: «функция connect, работай с тем, что лежит в ip».',
      'Строки всегда в кавычках " ". Числа — без кавычек.',
    ],
  },
  {
    id: 4,
    title: 'Цикл взлома',
    targetName: 'Майнинг-ферма в гараже',
    targetIp: '192.168.77.20',
    os: 'HiveOS',
    security: 35,
    difficulty: 'СРЕДНЕ',
    concept: 'Цикл for + range()',
    conceptDesc: 'Цикл for повторяет код N раз. range(5) даёт числа 0..4.',
    briefing: 'А вот и серьёзная цель — гаражная ферма с ETH. Пароль — одна цифра от 0 до 4. Вручную подбирать долго, поэтому напишем цикл: пусть Python переберёт все варианты командой brute().',
    task: 'Напиши цикл, который 5 раз вызовет brute(i):\nfor i in range(5):\n    brute(i)',
    starterCode: '# Цикл повторяет код. Обрати внимание на : и отступ!\n# for i in range(5):\n#     brute(i)\n\n',
    solution: 'for i in range(5):\n    brute(i)',
    hints: [
      'Первая строка: for i in range(5): — не забудь двоеточие!',
      'Вторая строка С ОТСТУПОМ (4 пробела): brute(i)',
      'Отступ в Python — это святое. Он показывает, что внутри цикла.',
    ],
    requiredPatterns: ['for\\s+\\w+\\s+in\\s+range\\s*\\(\\s*5\\s*\\)', 'brute\\s*\\('],
    rewardCrypto: 'ETH',
    rewardAmount: 0.045,
    rewardDollars: 200,
    rewardXp: 250,
    requiredCodeLib: 0,
    requiredLevel: 1,
    theory: [
      'for i in range(5): — «повтори 5 раз, каждый раз кладя счётчик в i».',
      'Двоеточие : говорит: «дальше блок кода». Блок пишется с отступом.',
      'brute(i) попробует пароль = i. Цикл переберёт 0,1,2,3,4 и найдёт верный!',
    ],
  },
  {
    id: 5,
    title: 'Условие доступа',
    targetName: 'Ноутбук криптотрейдера',
    targetIp: '10.10.5.9',
    os: 'macOS Sonoma',
    security: 48,
    difficulty: 'СРЕДНЕ',
    concept: 'Условия: if / else',
    conceptDesc: 'if проверяет условие. Если правда — выполняет блок, иначе — блок else.',
    briefing: 'Трейдер-неудачник хранит сид-фразу в файле. Его защита проверяет ключ: если ключ верный — открывает кошелёк. Напиши проверку через if, чтобы извлечь ETH только при верном ключе.',
    task: 'Проверь переменную key:\nесли key == "neon-77" — вызови extract(),\nиначе — print("Доступ запрещён")',
    starterCode: 'key = "neon-77"\n\n# Напиши проверку:\n# if key == "neon-77":\n#     extract()\n# else:\n#     print("Доступ запрещён")\n\n',
    solution: 'if key == "neon-77":\n    extract()\nelse:\n    print("Доступ запрещён")',
    hints: [
      'Сравнение — это ДВА знака равно: ==. Один = — это присваивание!',
      'Строка: if key == "neon-77":',
      'Не забудь отступы и блок else: с двоеточием.',
    ],
    requiredPatterns: ['if\\s+key\\s*==', 'extract\\s*\\(\\s*\\)', 'else\\s*:'],
    rewardCrypto: 'ETH',
    rewardAmount: 0.08,
    rewardDollars: 320,
    rewardXp: 320,
    requiredCodeLib: 1,
    requiredLevel: 2,
    theory: [
      '== сравнивает: key == "neon-77" это вопрос «равно ли?». Ответ: True/False.',
      'if True: — блок выполнится. if False: — пропустится.',
      'else: — «во всех остальных случаях». Идеально для обработки ошибок.',
    ],
  },
  {
    id: 6,
    title: 'Список кошельков',
    targetName: 'Сервер NFT-маркетплейса',
    targetIp: '45.89.12.200',
    os: 'Debian 12',
    security: 60,
    difficulty: 'СРЕДНЕ',
    concept: 'Списки: wallets = [...]',
    conceptDesc: 'Список хранит много значений. Создаётся квадратными скобками.',
    briefing: 'Куш! На сервере NFT-маркетплейса сразу три кошелька с Monero. Сохрани их адреса в список и обойди циклом, извлекая монеты из каждого через drain().',
    task: '1) Создай список: wallets = ["0xA1", "0xB2", "0xC3"]\n2) Циклом обойди и вызови drain(w) для каждого',
    starterCode: '# Список: wallets = ["0xA1", "0xB2", "0xC3"]\n# Затем цикл for w in wallets: с drain(w) внутри\n\n',
    solution: 'wallets = ["0xA1", "0xB2", "0xC3"]\nfor w in wallets:\n    drain(w)',
    hints: [
      'Список в квадратных скобках, элементы через запятую.',
      'for w in wallets: — «для каждого кошелька w из списка wallets».',
      'Внутри цикла с отступом: drain(w)',
    ],
    requiredPatterns: ['wallets\\s*=\\s*\\[', 'for\\s+\\w+\\s+in\\s+wallets', 'drain\\s*\\('],
    rewardCrypto: 'XMR',
    rewardAmount: 1.4,
    rewardDollars: 450,
    rewardXp: 400,
    requiredCodeLib: 1,
    requiredLevel: 2,
    theory: [
      'Список — упорядоченный набор: wallets = ["a", "b", "c"].',
      'for w in wallets: перебирает элементы по очереди.',
      'Комбинация «список + цикл» — главный инструмент хакера. Один код — сто целей!',
    ],
  },
  {
    id: 7,
    title: 'Своя функция',
    targetName: 'Холодный кошелёк кита',
    targetIp: '77.41.9.66',
    os: 'Tails OS',
    security: 78,
    difficulty: 'СЛОЖНО',
    concept: 'Функции: def myhack():',
    conceptDesc: 'def создаёт твою собственную функцию. Это автоматизация высшего уровня.',
    briefing: 'Кит — владелец огромного кошелька. Его защита требует тройной обход: bypass(), decrypt(), extract(). Чтобы не писать три строки каждый раз, упакуй их в свою функцию взлома kraken() и вызови её.',
    task: 'Создай функцию kraken() с тремя командами внутри:\nbypass(), decrypt(), extract()\nЗатем вызови kraken()',
    starterCode: '# def имя(): — создаёт функцию\n# Внутри с отступом пиши команды\n# После — вызови её\n\n',
    solution: 'def kraken():\n    bypass()\n    decrypt()\n    extract()\n\nkraken()',
    hints: [
      'Первая строка: def kraken():',
      'Три строки с отступом: bypass(), decrypt(), extract()',
      'В конце БЕЗ отступа вызови: kraken()',
    ],
    requiredPatterns: ['def\\s+kraken\\s*\\(', 'bypass\\s*\\(', 'decrypt\\s*\\(', 'extract\\s*\\('],
    rewardCrypto: 'XMR',
    rewardAmount: 2.8,
    rewardDollars: 700,
    rewardXp: 550,
    requiredCodeLib: 2,
    requiredLevel: 3,
    theory: [
      'def kraken(): — «запомни набор действий под именем kraken».',
      'Функция не выполняется при создании — только при вызове kraken().',
      'Хакеры пишут функции один раз, а используют сотни раз. Это и есть автоматизация!',
    ],
  },
  {
    id: 8,
    title: 'Бесконечный майнинг',
    targetName: 'Дата-центр «Север-7»',
    targetIp: '185.220.70.4',
    os: 'Proxmox Cluster',
    security: 92,
    difficulty: 'ЭКСПЕРТ',
    concept: 'while + установка майнера',
    conceptDesc: 'while повторяется, пока условие истинно. Идеален для майнинга.',
    briefing: 'Финальная цель — дата-центр. Здесь не просто воруем, а ставим вечный майнер SOL. Напиши цикл while, который держит соединение и ставит майнер командой install_miner(). После этого — ты легенда даркнета.',
    task: 'Напиши:\nmining = True\nwhile mining:\n    install_miner()\n    mining = False',
    starterCode: 'mining = True\n\n# while условие: — повторяется, пока True\n# Внутри: install_miner() и выключи флаг\n\n',
    solution: 'mining = True\nwhile mining:\n    install_miner()\n    mining = False',
    hints: [
      'while mining: — цикл работает, пока mining равно True.',
      'Внутри: install_miner()',
      'И mining = False — чтобы цикл остановился (иначе бесконечность!).',
    ],
    requiredPatterns: ['while\\s+mining', 'install_miner\\s*\\(', 'mining\\s*=\\s*False'],
    rewardCrypto: 'SOL',
    rewardAmount: 3.2,
    rewardDollars: 1200,
    rewardXp: 800,
    requiredCodeLib: 3,
    requiredLevel: 4,
    theory: [
      'while — цикл с условием. for — «N раз», while — «пока верно».',
      'Бесконечный цикл while True: без выхода — зависнет. Поэтому меняем флаг.',
      'install_miner() — твоя золотая жила: майнер капает крипту каждую секунду!',
    ],
  },
];

export interface Upgrade {
  id: string;
  name: string;
  desc: string;
  icon: string;
  maxLevel: number;
  costs: number[];
  effects: string[];
}

export const UPGRADES: Upgrade[] = [
  {
    id: 'hackSpeed',
    name: 'Оверклок взлома',
    desc: 'Разгоняет выполнение скриптов и увеличивает награду за взлом.',
    icon: '⚡',
    maxLevel: 5,
    costs: [200, 500, 1200, 3000, 7500],
    effects: ['x1.0 скорость', 'x1.3 скорость +5% награда', 'x1.7 скорость +10% награда', 'x2.2 скорость +20% награда', 'x3.0 скорость +35% награда', 'x4.0 скорость +50% награда'],
  },
  {
    id: 'minerEff',
    name: 'Майнинг-ядро',
    desc: 'Увеличивает доход всех майнеров и скорость добычи.',
    icon: '⛏',
    maxLevel: 5,
    costs: [300, 800, 1800, 4000, 9000],
    effects: ['x1.0 добыча', 'x1.5 добыча', 'x2.2 добыча', 'x3.2 добыча', 'x4.5 добыча', 'x6.0 добыча'],
  },
  {
    id: 'codeLib',
    name: 'Библиотека кода',
    desc: 'Открывает новые хакерские функции. Требуется для сложных миссий!',
    icon: '📚',
    maxLevel: 4,
    costs: [400, 1500, 4500, 10000],
    effects: ['scan, connect, brute, print', '+ extract, drain', '+ bypass, decrypt', '+ install_miner, spoof', '+ quantum_brute, ghost'],
  },
  {
    id: 'stealth',
    name: 'Стелс-модуль',
    desc: 'Снижает комиссию биржи и даёт бонусный опыт за взломы.',
    icon: '👻',
    maxLevel: 4,
    costs: [250, 700, 2000, 5500],
    effects: ['5% комиссия, +0% XP', '4% комиссия, +10% XP', '3% комиссия, +20% XP', '2% комиссия, +35% XP', '0% комиссия, +50% XP'],
  },
];

export interface Lesson {
  id: number;
  title: string;
  subtitle: string;
  duration: string;
  xp: number;
  content: { heading: string; text: string; code?: string }[];
  quiz: { q: string; options: string[]; answer: number }[];
}

export const LESSONS: Lesson[] = [
  {
    id: 1,
    title: 'Что такое Python?',
    subtitle: 'Знакомство с языком хакеров',
    duration: '5 мин',
    xp: 60,
    content: [
      { heading: 'Почему Python?', text: 'Python — самый популярный язык у хакеров и пентестеров. Он читается почти как английский, поэтому его учат первым. Instagram, YouTube и NASA — всё на Python.' },
      { heading: 'Первая команда', text: 'Команда print() выводит текст на экран. Текст в кавычках называется строкой.', code: 'print("Привет, хакер!")\n# Выведет: Привет, хакер!' },
      { heading: 'Комментарии', text: 'Строки с # — это комментарии. Python их игнорирует, а люди читают как заметки.', code: '# Это заметка, она не выполнится\nprint("А это выполнится")' },
    ],
    quiz: [
      { q: 'Что выведет print("1337")?', options: ['1337', 'print', 'Ошибку'], answer: 0 },
      { q: 'Для чего нужен символ #?', options: ['Для взлома', 'Для комментариев', 'Для умножения'], answer: 1 },
    ],
  },
  {
    id: 2,
    title: 'Переменные и типы',
    subtitle: 'Коробки для данных',
    duration: '7 мин',
    xp: 80,
    content: [
      { heading: 'Переменная = коробка', text: 'Переменная хранит значение под именем. Создаётся через знак =.', code: 'nickname = "ghost"\nlevel = 5\nbalance = 0.05' },
      { heading: 'Типы данных', text: 'str — строка "текст", int — целое число 42, float — дробное 3.14, bool — True/False.', code: 'name = "neo"      # str\ncoins = 10        # int\nbtc = 0.002       # float\nhacked = True    # bool' },
      { heading: 'Перезапись', text: 'Значение можно менять. Старое стирается, новое записывается.', code: 'cash = 100\ncash = 250  # теперь 250\nprint(cash)' },
    ],
    quiz: [
      { q: 'Какой тип у значения "hello"?', options: ['int', 'str', 'bool'], answer: 1 },
      { q: 'Что будет в x после: x = 5, x = 9?', options: ['5', '9', '14'], answer: 1 },
    ],
  },
  {
    id: 3,
    title: 'Условия if/else',
    subtitle: 'Принятие решений',
    duration: '8 мин',
    xp: 100,
    content: [
      { heading: 'Логика проверки', text: 'if проверяет условие. Если оно истинно (True) — код внутри выполняется.', code: 'password = "qwerty"\nif password == "qwerty":\n    print("Доступ разрешён!")' },
      { heading: '== против =', text: 'Одна из главных ошибок новичков! = присваивает, == сравнивает. Запомни навсегда.', code: 'x = 5       # положить 5 в x\nx == 5      # правда ли, что x равно 5? -> True' },
      { heading: 'Ветка else', text: 'else — запасной план: выполняется, когда условие ложно.', code: 'if balance > 100:\n    print("Богач!")\nelse:\n    print("Нужно больше взломов...")' },
    ],
    quiz: [
      { q: 'Чем == отличается от =?', options: ['Ничем', '== сравнивает, = присваивает', '== присваивает'], answer: 1 },
      { q: 'Когда выполняется блок else?', options: ['Всегда', 'Когда if ложен', 'Никогда'], answer: 1 },
    ],
  },
  {
    id: 4,
    title: 'Циклы for и while',
    subtitle: 'Автоматизация рутины',
    duration: '10 мин',
    xp: 120,
    content: [
      { heading: 'Цикл for', text: 'for повторяет код для каждого элемента. range(5) даёт 0,1,2,3,4.', code: 'for i in range(5):\n    print(i)\n# Выведет 0 1 2 3 4' },
      { heading: 'Цикл while', text: 'while крутится, пока условие истинно. Осторожно с бесконечностью!', code: 'tries = 0\nwhile tries < 3:\n    print("Попытка", tries)\n    tries = tries + 1' },
      { heading: 'Хакерский перебор', text: 'Цикл + функция взлома = брутфорс. Именно так ломают слабые пароли.', code: 'for pin in range(10000):\n    hack(pin)  # пробуем каждый PIN' },
    ],
    quiz: [
      { q: 'Сколько раз выполнится for i in range(4)?', options: ['3', '4', '5'], answer: 1 },
      { q: 'Чем опасен while True без break?', options: ['Ничем', 'Бесконечным циклом', 'Ошибкой синтаксиса'], answer: 1 },
    ],
  },
  {
    id: 5,
    title: 'Списки и функции',
    subtitle: 'Арсенал профи',
    duration: '10 мин',
    xp: 150,
    content: [
      { heading: 'Списки', text: 'Список хранит много значений в квадратных скобках.', code: 'wallets = ["0xA1", "0xB2", "0xC3"]\nprint(wallets[0])  # первый элемент' },
      { heading: 'Свои функции', text: 'def создаёт функцию. Пишешь один раз — вызываешь везде.', code: 'def pwn(target):\n    connect(target)\n    extract()\n\npwn("192.168.1.1")' },
      { heading: 'Комбо хакера', text: 'Список целей + цикл + функция = взлом сотни кошельков одной программой.', code: 'def rob(w):\n    connect(w)\n    drain(w)\n\nfor w in wallets:\n    rob(w)' },
    ],
    quiz: [
      { q: 'Как создать список?', options: ['(1,2,3)', '[1,2,3]', '{1,2,3}'], answer: 1 },
      { q: 'Что делает def?', options: ['Удаляет функцию', 'Создаёт функцию', 'Запускает цикл'], answer: 1 },
    ],
  },
];

export const HACKER_QUOTES = [
  'Код — это оружие. Учись владеть им.',
  'Пока они спят — ты майнишь.',
  'Слабый пароль — подарок хакеру.',
  'Логика сильнее удачи.',
  'Каждый взлом начинается с print().',
];

export function xpForLevel(level: number): number {
  return level * 300;
}
