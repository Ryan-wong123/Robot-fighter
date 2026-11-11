import os, struct, sys, tempfile, unittest, zlib
sys.path.insert(0, os.path.dirname(__file__))
import check_concept_art as c


def make_png(path, width, height=4):
    def chunk(tag, data):
        body = tag + data
        return struct.pack('>I', len(data)) + body + struct.pack('>I', zlib.crc32(body) & 0xffffffff)
    raw = b''.join(b'\x00' + b'\xff\xff\xff' * width for _ in range(height))
    with open(path, 'wb') as f:
        f.write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 2, 0, 0, 0))
                + chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b''))


class Concept(unittest.TestCase):
    def setUp(self):
        self.root = tempfile.mkdtemp()
        self.addCleanup(__import__('shutil').rmtree, self.root)
        self.exp = {'maps': ['a_colorkey.png']}
        os.makedirs(os.path.join(self.root, 'docs', 'images', 'maps', 'concept'))

    def write(self, rel, text):
        with open(os.path.join(self.root, rel), 'w', encoding='utf-8') as f:
            f.write(text)

    def test_complete_set_passes(self):
        make_png(os.path.join(self.root, 'docs/images/maps/concept/a_colorkey.png'), 2048)
        self.write('docs/images/maps/concept/SOURCES.md', '## a_colorkey.png\n')
        self.write('docs/maps.md', '![x](images/maps/concept/a_colorkey.png)\n')
        self.assertEqual(c.problems(self.root, self.exp), [])

    def test_reports_missing_narrow_unsourced_unlinked(self):
        self.assertTrue(any('missing file' in p for p in c.problems(self.root, self.exp)))
        make_png(os.path.join(self.root, 'docs/images/maps/concept/a_colorkey.png'), 1000)
        probs = c.problems(self.root, self.exp)
        self.assertTrue(any('too narrow' in p for p in probs))
        self.assertTrue(any('no SOURCES.md entry' in p for p in probs))
        self.assertTrue(any('not linked' in p for p in probs))

    def test_link_from_wrong_doc_fails(self):
        make_png(os.path.join(self.root, 'docs/images/maps/concept/a_colorkey.png'), 2048)
        self.write('docs/images/maps/concept/SOURCES.md', '## a_colorkey.png\n')
        self.write('docs/characters.md', '![x](images/maps/concept/a_colorkey.png)\n')
        self.assertTrue(any('not linked from docs/maps.md' in p for p in c.problems(self.root, self.exp)))

    def test_not_a_png(self):
        self.write('docs/images/maps/concept/a_colorkey.png', 'hello')
        self.assertTrue(any('not a valid PNG' in p for p in c.problems(self.root, self.exp)))


if __name__ == '__main__':
    unittest.main()
