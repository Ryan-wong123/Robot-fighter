#!/usr/bin/env python3
"""Small static checks used by Tools/tasks/verify.json. Exit 0 = pass, 1 = fail.

  checks.py no-pattern REGEX DIR [DIR...]   fail if REGEX appears in any .cs file
  checks.py file-exists PATH
  checks.py file-contains PATH TEXT
Missing directories are ignored. Vendor dirs (ThirdParty, Joystick Pack) are skipped.
"""
import os, re, sys

SKIP = {'ThirdParty', 'Joystick Pack', 'Library', '.git'}


def no_pattern(regex, dirs):
    rx, bad = re.compile(regex), []
    def scan(path):
        for n, line in enumerate(open(path, encoding='utf-8-sig', errors='replace'), 1):
            if rx.search(line):
                bad.append('%s:%d: %s' % (path, n, line.strip()))

    for d in dirs:
        if os.path.isfile(d):
            scan(d)
            continue
        for root, subdirs, files in os.walk(d):
            subdirs[:] = [s for s in subdirs if s not in SKIP]
            for f in files:
                if f.endswith('.cs'):
                    scan(os.path.join(root, f))
    for b in bad:
        print(b)
    return 1 if bad else 0


def main(argv):
    if len(argv) >= 3 and argv[0] == 'no-pattern':
        return no_pattern(argv[1], argv[2:])
    if len(argv) == 2 and argv[0] == 'file-exists':
        return 0 if os.path.exists(argv[1]) else 1
    if len(argv) == 3 and argv[0] == 'file-contains':
        return 0 if os.path.exists(argv[1]) and argv[2] in open(argv[1], errors='replace').read() else 1
    print(__doc__)
    return 2


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
