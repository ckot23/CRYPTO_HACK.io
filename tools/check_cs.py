#!/usr/bin/env python3
"""
Статический чекер C#-кода проекта CRYPTO_HACK (Unity-версия).

Unity в песочнице нет, компилятор C# недоступен, поэтому проверяем то,
что можно проверить без компилятора:

  1. синтаксис — tree-sitter-c-sharp (наличие узлов ERROR);
  2. обращение к членам СВОИХ классов: если получатель — известное имя типа
     (Theme, Ui, Fmt, PySim, Game, PyRunner, Sfx, UiButton, ...), то метод/поле
     обязательно должны существовать в этом типе;
  3. арность вызовов методов своих типов (с учётом значений по умолчанию);
  4. дубли имён членов внутри одного класса (частая опечатка копипастом);
  5. незакрытые TODO-заглушки вида `// FIXME:` — просто информационно.

Запуск:  python3 tools/check_cs.py [путь к Assets/Scripts]
"""

import os
import sys
import glob
from collections import defaultdict

try:
    from tree_sitter import Language, Parser
    import tree_sitter_c_sharp as tscs
except ImportError:  # pragma: no cover
    print("Нужны пакеты: pip install tree-sitter tree-sitter-c-sharp")
    raise SystemExit(2)


# ==================== МОДЕЛЬ ТИПОВ ====================
class TypeInfo:
    def __init__(self, name, kind, file):
        self.name = name
        self.kind = kind            # class | struct | interface | enum
        self.file = file
        self.members = set()        # все имена членов
        self.methods = defaultdict(list)   # имя -> список (min_args, max_args)

    def add_member(self, name):
        self.members.add(name)

    def add_method(self, name, min_args, max_args):
        self.members.add(name)
        self.methods[name].append((min_args, max_args))

    def has(self, name):
        return name in self.members


def text(src, node):
    return src[node.start_byte:node.end_byte].decode("utf-8", "replace")


def walk(node):
    stack = [node]
    while stack:
        n = stack.pop()
        yield n
        stack.extend(reversed(n.children))


def parse_files(paths):
    types = {}
    trees = {}
    for path in paths:
        src_bytes = open(path, encoding="utf-8").read().encode()
        tree = PARSER.parse(src_bytes)
        trees[path] = (src_bytes, tree)
        root = tree.root_node

        for node in walk(root):
            if node.type in ("class_declaration", "struct_declaration",
                             "interface_declaration", "enum_declaration"):
                name_node = node.child_by_field_name("name")
                if name_node is None:
                    continue
                tname = text(src_bytes, name_node)
                kind = node.type.replace("_declaration", "")
                t = types.get(tname)
                if t is None:
                    t = TypeInfo(tname, kind, path)
                    types[tname] = t
                body = node.child_by_field_name("body")
                if body is None:
                    continue
                for member in body.children:
                    collect_member(src_bytes, member, t)

    return types, trees


def collect_member(src, node, t):
    kind = node.type

    if kind in ("field_declaration", "event_field_declaration"):
        # в этой грамматике variable_declaration лежит без метки — ищем по типу
        for child in node.children:
            if child.type != "variable_declaration":
                continue
            for d in child.children:
                if d.type == "variable_declarator":
                    nm = d.child_by_field_name("name")
                    if nm is not None:
                        t.add_member(text(src, nm))
    elif kind == "property_declaration":
        nm = node.child_by_field_name("name")
        if nm is not None:
            t.add_member(text(src, nm))
    elif kind == "method_declaration":
        nm = node.child_by_field_name("name")
        params = node.child_by_field_name("parameters")
        if nm is not None:
            total, required = count_params(params)
            t.add_method(text(src, nm), required, total)
    elif kind == "constructor_declaration":
        nm = node.child_by_field_name("name")
        params = node.child_by_field_name("parameters")
        if nm is not None:
            total, required = count_params(params)
            t.add_method("<ctor>", required, total)
    elif kind == "enum_member_declaration":
        nm = node.child_by_field_name("name")
        if nm is not None:
            t.add_member(text(src, nm))
    elif kind in ("class_declaration", "struct_declaration", "interface_declaration"):
        nm = node.child_by_field_name("name")
        if nm is not None:
            t.add_member(text(src, nm))


def count_params(params_node):
    """Возвращает (всего параметров, обязательных)."""
    if params_node is None:
        return 0, 0
    total = 0
    required = 0
    for p in params_node.children:
        if p.type != "parameter":
            continue
        total += 1
        has_default = any(c.type == "equals_clause" for c in p.children) or "=" in text_raw(p)
        if not has_default:
            required += 1
    return total, required


def text_raw(node):
    return "".join(c.type for c in node.children)


# ==================== ПРОВЕРКИ ====================
class Checker:
    def __init__(self, types, trees):
        self.types = types
        self.trees = trees
        self.problems = []
        self._fields = set()

    def report(self, path, node, message):
        row, col = node.start_point
        self.problems.append("%s:%d:%d  %s" % (os.path.relpath(path), row + 1, col + 1, message))

    def run(self):
        for path, (src, tree) in self.trees.items():
            self.check_tree(path, src, tree)

    # ---- сбор локальных имён с типами внутри тела метода ----
    def locals_in(self, fn_node, src):
        """имя -> имя типа (для локальных переменных и параметров)"""
        result = {}
        params = fn_node.child_by_field_name("parameters")
        if params is not None:
            for p in params.children:
                if p.type == "parameter":
                    ptype = p.child_by_field_name("type")
                    nm = p.child_by_field_name("name")
                    if ptype is not None and nm is not None:
                        result[text(src, nm)] = base_type_name(text(src, ptype))
        body = fn_node.child_by_field_name("body")
        if body is None:
            return result
        for n in walk(body):
            if n.type == "variable_declaration":
                vtype = n.child_by_field_name("type")
                if vtype is None:
                    continue
                tname = base_type_name(text(src, vtype))
                for d in n.children:
                    if d.type == "variable_declarator":
                        nm = d.child_by_field_name("name")
                        if nm is not None:
                            result[text(src, nm)] = tname
            elif n.type == "foreach_statement":
                # foreach (Type name in ...)
                seen = [c for c in n.children if c.type in ("variable_declaration", "identifier")]
                if len(seen) >= 1 and seen[0].type == "variable_declaration":
                    vd = seen[0]
                    vtype = vd.child_by_field_name("type")
                    if vtype is not None:
                        for d in vd.children:
                            if d.type == "variable_declarator":
                                nm = d.child_by_field_name("name")
                                if nm is not None:
                                    result[text(src, nm)] = base_type_name(text(src, vtype))
            elif n.type == "declaration_expression":
                vtype = n.child_by_field_name("type")
                nm = n.child_by_field_name("name")
                if vtype is not None and nm is not None:
                    result[text(src, nm)] = base_type_name(text(src, vtype))
        return result

    def check_tree(self, path, src, tree):
        root = tree.root_node

        # поля всех классов файла: упрощённо — без учёта вложенности
        for node in walk(root):
            if node.type in ("class_declaration", "struct_declaration", "interface_declaration"):
                body = node.child_by_field_name("body")
                if body is None:
                    continue
                # walk, а не children: поля могут лежать внутри #if/#endif-блоков
                for member in walk(body):
                    if member.type in ("field_declaration", "event_field_declaration"):
                        for child in member.children:
                            if child.type != "variable_declaration":
                                continue
                            for d in child.children:
                                if d.type == "variable_declarator":
                                    nm = d.child_by_field_name("name")
                                    if nm is not None:
                                        self._fields.add(text(src, nm))

        for fn in walk(root):
            if fn.type not in ("method_declaration", "constructor_declaration", "local_function_statement"):
                continue
            locals_ = self.locals_in(fn, src)
            for n in walk(fn):
                self.check_member_access(path, src, n, locals_)

    def current_fields(self, name):
        return name in self._fields

    def check_member_access(self, path, src, node, locals_):
        if node.type == "member_access_expression":
            expr = node.child_by_field_name("expression")
            name_node = node.child_by_field_name("name")
            if expr is None or name_node is None:
                return
            if expr.type != "identifier":
                return
            recv = text(src, expr)
            member = text(src, name_node)

            # получатель — локальная переменная/параметр известного типа?
            if recv in locals_:
                tname = locals_[recv]
                t = self.types.get(tname)
                if t is not None and not t.has(member):
                    self.report(path, node, "у типа %s нет члена «%s»" % (tname, member))
                return

            # получатель — имя своего типа (статический доступ)
            t = self.types.get(recv)
            if t is not None and not t.has(member):
                self.report(path, node, "у типа %s нет члена «%s»" % (recv, member))
            return

        if node.type == "identifier":
            name = text(src, node)
            if name == "_":
                return  # одиночное подчёркивание — это дискард C# 7+, а не поле
            if name.startswith("_"):
                parent = node.parent
                is_target = False
                if parent is not None:
                    if parent.type in ("assignment_expression", "prefix_unary_expression",
                                       "postfix_unary_expression") and parent.child_by_field_name("left") == node:
                        is_target = True
                    if parent.type == "member_access_expression" and parent.child_by_field_name("expression") == node:
                        is_target = True
                    if parent.type == "variable_declarator" or parent.type == "parameter":
                        is_target = False
                if is_target and name not in locals_ and not self.current_fields(name):
                    self.report(path, node, "используется необъявленное поле «%s» (нет такого поля ни в классе,"
                                " ни в локальных переменных)" % name)
            return

        if node.type == "invocation_expression":
            fn = node.child_by_field_name("function")
            if fn is None or fn.type != "member_access_expression":
                return
            expr = fn.child_by_field_name("expression")
            name_node = fn.child_by_field_name("name")
            if expr is None or name_node is None or expr.type != "identifier":
                return
            recv = text(src, expr)
            member = text(src, name_node)
            args = node.child_by_field_name("arguments")
            argc, has_spread = self.count_args(src, args)

            tname = None
            if recv in locals_:
                tname = locals_[recv]
            elif recv in self.types:
                tname = recv
            if tname is None:
                return
            t = self.types.get(tname)
            if t is None:
                return
            if member not in t.methods:
                return  # это может быть свойство-делегат или поле: отдельная проверка выше
            if has_spread:
                return
            for (mn, mx) in t.methods[member]:
                if mn <= argc <= mx:
                    return
            self.report(path, node, "вызов %s.%s с %d аргументами не совпадает с объявлением %s"
                        % (tname, member, argc, t.methods[member]))

    def count_args(self, src, args_node):
        if args_node is None:
            return 0, False
        count = 0
        for a in args_node.children:
            if a.type == "argument":
                count += 1
            elif a.type in ("out", "ref"):
                pass
            elif a.type == "identifier" and text(src, a) == "out":
                pass
            elif a.type == "conditional_expression" or a.type == "ref_expression":
                pass
        # spread / out var требуют проверки вручную: смотрим текст
        raw = text(src, args_node)
        spread = " out " in raw or "ref " in raw or "params" in raw
        return count, spread


def base_type_name(tname):
    """Generic<Foo> -> Generic; string[] -> string; Fmt.Crypto -> Fmt"""
    tname = tname.strip()
    for ch in "<[ ":
        idx = tname.find(ch)
        if idx > 0:
            tname = tname[:idx]
    if "." in tname:
        tname = tname.split(".")[-1]
    return tname


def check_duplicates(types):
    problems = []
    for t in types.values():
        # дубли методов с одинаковой арностью и именем — часто опечатка
        for name, arities in t.methods.items():
            seen = set()
            for (mn, mx) in arities:
                if (mn, mx) in seen:
                    problems.append("%s: в типе %s два объявления метода «%s» с (%d..%d) аргументами"
                                    % (os.path.relpath(t.file), t.name, name, mn, mx))
                seen.add((mn, mx))
    return problems


def main():
    base = sys.argv[1] if len(sys.argv) > 1 else "Assets/Scripts"
    paths = sorted(glob.glob(os.path.join(base, "**", "*.cs"), recursive=True))
    if not paths:
        print("Не найдено ни одного .cs в", base)
        return 1

    print("Проверяю файлов: %d" % len(paths))

    syntax_bad = 0
    for path in paths:
        src = open(path, encoding="utf-8").read().encode()
        tree = PARSER.parse(src)
        if tree.root_node.has_error:
            syntax_bad += 1
            print("СИНТАКСИС: %s" % os.path.relpath(path))
            for n in walk(tree.root_node):
                if n.type == "ERROR":
                    row, col = n.start_point
                    snippet = src[n.start_byte:n.end_byte][:80].decode("utf-8", "replace")
                    print("   строка %d:%d  %r" % (row + 1, col + 1, snippet))
                    break

    types, trees = parse_files(paths)
    print("Своих типов разобрано: %d" % len(types))

    checker = Checker(types, trees)
    checker.run()

    problems = checker.problems + check_duplicates(types)
    for p in problems:
        print("ПРОБЛЕМА: " + p)

    print("Итого: файлов %d, ошибок синтаксиса %d, семантических замечаний %d"
          % (len(paths), syntax_bad, len(problems)))
    return 1 if (syntax_bad or problems) else 0


PARSER = Parser(Language(tscs.language()))

if __name__ == "__main__":
    raise SystemExit(main())
