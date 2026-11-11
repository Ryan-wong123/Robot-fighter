import os, sys, tempfile, unittest
sys.path.insert(0, os.path.dirname(__file__))
import check_docs as c


class BrokenLinks(unittest.TestCase):
    def _tree(self, files):
        d = tempfile.mkdtemp()
        self.addCleanup(__import__('shutil').rmtree, d)
        for rel, text in files.items():
            p = os.path.join(d, rel)
            os.makedirs(os.path.dirname(p), exist_ok=True)
            with open(p, 'w', encoding='utf-8') as fh:
                fh.write(text)
        return d

    def test_ok_and_broken(self):
        d = self._tree({'docs/a.md': '[b](b.md) [root](../AGENTS.md#x) [web](https://x.y) [anchor](#top)\n',
                        'docs/b.md': '[gone](missing.md)\n', 'AGENTS.md': ''})
        probs = c.broken_links([os.path.join(d, 'docs/a.md'), os.path.join(d, 'docs/b.md')])
        self.assertEqual(len(probs), 1)
        self.assertIn('missing.md', probs[0])

    def test_code_fences_ignored(self):
        d = self._tree({'docs/a.md': '```\n[x](nope.md)\n```\n'})
        self.assertEqual(c.broken_links([os.path.join(d, 'docs/a.md')]), [])


if __name__ == '__main__':
    unittest.main()
