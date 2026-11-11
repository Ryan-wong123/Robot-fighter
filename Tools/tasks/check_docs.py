#!/usr/bin/env python3
"""Check that every relative markdown link in the project docs points at a file that exists.

Scans docs/*.md, AGENTS.md, CLAUDE.md and README.md (override with file arguments).
Links inside fenced code blocks are ignored; http(s)/mailto links and bare #anchors are skipped;
a `#section` suffix is stripped (only the file is checked).
Exit code: 0 = all links resolve, 1 = at least one broken link (listed on stdout).
"""
import glob, os, re, sys

LINK = re.compile(r'\[[^\]]*\]\(([^)\s]+)\)')


def default_files(root='.'):
    files = sorted(glob.glob(os.path.join(root, 'docs', '*.md')))
    files += [os.path.join(root, n) for n in ('AGENTS.md', 'CLAUDE.md', 'README.md') if os.path.exists(os.path.join(root, n))]
    return files


def broken_links(files):
    """Return a list of 'file:line: broken link -> target' strings."""
    problems = []
    for f in files:
        base, fence = os.path.dirname(f), False
        with open(f, encoding='utf-8') as fh:
            for n, line in enumerate(fh, 1):
                if line.lstrip().startswith('```'):
                    fence = not fence
                    continue
                if fence:
                    continue
                for m in LINK.finditer(line):
                    target = m.group(1)
                    if re.match(r'^(https?:|mailto:|#)', target):
                        continue
                    path = target.split('#', 1)[0]
                    if path and not os.path.exists(os.path.normpath(os.path.join(base, path))):
                        problems.append('%s:%d: broken link -> %s' % (f, n, target))
    return problems


def main(argv):
    files = argv or default_files()
    problems = broken_links(files)
    for p in problems:
        print(p)
    print('%d file(s) checked, %d broken link(s)' % (len(files), len(problems)))
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
