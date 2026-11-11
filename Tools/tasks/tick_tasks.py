#!/usr/bin/env python3
"""Verify a task, then tick it in docs/task.md.

  tick_tasks.py --complete ID [ID...]   run each task's checks; tick [x] only if all pass
  tick_tasks.py --check                 validate docs/task.md (ids unique, `after:` ids exist, no cycles)

A task can list prerequisites at the end of its line: `(after: M0-01, M0-05)`.
--complete refuses a task until every prerequisite is [x] (a [~] prerequisite is not enough).

Checks live in Tools/tasks/verify.json (a list of shell commands per task id).
Commands under "_always" (for example the docs link check) run first on every --complete; if one fails, nothing is ticked.
All commands must exit 0 for [x]. Exit code 3 from a command means "cannot run here"
(e.g. no Unity installed): the task becomes [~] (code done, not verified). A task with
no entry can reach [~] at most. A failed check leaves the task untouched and this
script exits 2. A task never moves backwards.
"""
import argparse, json, os, re, subprocess, sys

AFTER = re.compile(r'\(after: (?P<ids>[^)]*)\)\s*$')
TASK_LINE = re.compile(r'^(?P<pre>- \[)(?P<mark>[ x~])(?P<post>\] \*\*)(?P<id>[A-Z][A-Z0-9]*-\d+)(?P<rest>\*\*.*)$')


def run_always(verify_file):
    """Run the global checks listed under "_always" in verify.json (for example the docs link check).
    Return True if they all pass. They run once per --complete call, before any task is verified."""
    try:
        with open(verify_file, encoding='utf-8') as f:
            cmds = json.load(f).get('_always', [])
    except FileNotFoundError:
        return True
    for cmd in cmds:
        if subprocess.run(cmd, shell=True).returncode != 0:
            print('GLOBAL CHECK FAILED, nothing was ticked: ' + cmd)
            return False
    return True


def verify_task(tid, verify_file):
    """Return 'pass', 'unavailable' or 'fail', printing the reason."""
    try:
        with open(verify_file, encoding='utf-8') as f:
            cmds = json.load(f).get(tid)
    except FileNotFoundError:
        cmds = None
    if not cmds:
        print('%s: no automated check defined, so it cannot be marked done' % tid)
        return 'unavailable'
    for cmd in cmds:
        rc = subprocess.run(cmd, shell=True).returncode
        if rc == 3:
            print('%s: check could not run here: %s' % (tid, cmd))
            return 'unavailable'
        if rc != 0:
            print('%s: FAILED (exit %d): %s' % (tid, rc, cmd))
            return 'fail'
    print('%s: verification passed' % tid)
    return 'pass'


def parse_tasks(lines):
    """Return ordered list of (id, mark, [prerequisite ids]). Raises ValueError on duplicates."""
    tasks, seen = [], set()
    for line in lines:
        m = TASK_LINE.match(line.rstrip('\n'))
        if not m:
            continue
        tid = m.group('id')
        if tid in seen:
            raise ValueError('duplicate task id: ' + tid)
        seen.add(tid)
        a = AFTER.search(m.group('rest'))
        deps = [d.strip().upper() for d in a.group('ids').split(',') if d.strip()] if a else []
        tasks.append((tid, m.group('mark'), deps))
    return tasks


def validate(lines):
    """Return a list of problems (empty if docs/task.md is consistent)."""
    try:
        tasks = parse_tasks(lines)
    except ValueError as e:
        return [str(e)]
    ids = {t[0] for t in tasks}
    problems = ['%s: unknown prerequisite %s' % (tid, d) for tid, _, deps in tasks for d in deps if d not in ids]
    graph = {tid: deps for tid, _, deps in tasks}
    state = {}

    def visit(n, path):
        if state.get(n) == 2 or n not in graph:
            return
        if state.get(n) == 1:
            problems.append('dependency cycle: ' + ' -> '.join(path + [n]))
            return
        state[n] = 1
        for d in graph[n]:
            visit(d, path + [n])
        state[n] = 2

    for tid in graph:
        visit(tid, [])
    return problems


def unmet(tid, tasks, completed):
    """Prerequisites of tid that are not [x] (counting ids completed earlier in this run)."""
    marks = {t[0]: t[1] for t in tasks}
    deps = next((d for i, _, d in tasks if i == tid), [])
    return [d for d in deps if marks.get(d) != 'x' and d not in completed]


def apply(lines, done, partial):
    """Set marks. Returns (new_lines, changes, unknown_ids). Never downgrades."""
    ids, changed, out = set(), [], []
    for line in lines:
        m = TASK_LINE.match(line.rstrip('\n'))
        if not m:
            out.append(line)
            continue
        tid, mark = m.group('id'), m.group('mark')
        if tid in ids:
            raise ValueError('duplicate task id: ' + tid)
        ids.add(tid)
        new = mark
        if tid in done:
            new = 'x'
        elif tid in partial and mark == ' ':
            new = '~'
        if new != mark:
            changed.append((tid, mark, new))
            line = m.group('pre') + new + m.group('post') + tid + m.group('rest') + '\n'
        out.append(line)
    return out, changed, sorted((done | partial) - ids)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--complete', nargs='+', metavar='ID')
    ap.add_argument('--check', action='store_true')
    ap.add_argument('--task-file', default='docs/task.md')
    ap.add_argument('--verify-file', default=os.path.join(os.path.dirname(os.path.abspath(__file__)), 'verify.json'))
    a = ap.parse_args()
    if not a.complete and not a.check:
        ap.error('use --complete ID... or --check')

    with open(a.task_file, encoding='utf-8') as f:
        lines = f.readlines()

    problems = validate(lines)
    if problems:
        for p in problems:
            print('ERROR:', p, file=sys.stderr)
        return 1
    if a.check and not a.complete:
        return 0

    if a.complete and not run_always(a.verify_file):
        return 2

    tasks = parse_tasks(lines)
    order = {t[0]: n for n, t in enumerate(tasks)}
    wanted = sorted(set(i.upper() for i in (a.complete or [])), key=lambda i: order.get(i, 1 << 30))
    done, partial, failed = set(), set(), set()
    for tid in wanted:
        if tid not in order:
            print('WARNING: unknown task id: ' + tid, file=sys.stderr)
            continue
        blocked = unmet(tid, tasks, done)
        if blocked:
            print('%s: BLOCKED, finish these first: %s' % (tid, ', '.join(blocked)))
            failed.add(tid)
            continue
        r = verify_task(tid, a.verify_file)
        if r == 'pass':
            done.add(tid)
        elif r == 'unavailable':
            partial.add(tid)
        else:
            failed.add(tid)

    try:
        out, changed, unknown = apply(lines, done, partial)
    except ValueError as e:
        print('ERROR:', e, file=sys.stderr)
        return 1
    for tid in unknown:
        print('WARNING: unknown task id: ' + tid, file=sys.stderr)
    for tid, old, new in changed:
        print('%s: [%s] -> [%s]' % (tid, old, new))
    if changed:
        with open(a.task_file, 'w', encoding='utf-8') as f:
            f.writelines(out)
    return 2 if failed else 0


if __name__ == '__main__':
    sys.exit(main())
