import json, os, sys, tempfile, unittest
sys.path.insert(0, os.path.dirname(__file__))
import tick_tasks as t

LINES = ['- [ ] **A-1** one\n', '- [ ] **A-2** two\n', '- [x] **A-3** three\n']


class Apply(unittest.TestCase):
    def test_done_and_partial(self):
        out, changed, _ = t.apply(LINES, {'A-1'}, {'A-2'})
        self.assertEqual(out[0], '- [x] **A-1** one\n')
        self.assertEqual(out[1], '- [~] **A-2** two\n')

    def test_never_downgrades(self):
        out, _, _ = t.apply(LINES, set(), {'A-3'})
        self.assertEqual(out[2], '- [x] **A-3** three\n')

    def test_unknown_reported(self):
        self.assertEqual(t.apply(LINES, {'Z-1'}, set())[2], ['Z-1'])

    def test_duplicate_id_rejected(self):
        with self.assertRaises(ValueError):
            t.apply(LINES + ['- [ ] **A-1** dup\n'], set(), set())


class Deps(unittest.TestCase):
    L = ['- [x] **A-1** one\n', '- [~] **A-2** two (after: A-1)\n', '- [ ] **A-3** three (after: A-1, A-2)\n']

    def test_parse(self):
        self.assertEqual(t.parse_tasks(self.L)[2], ('A-3', ' ', ['A-1', 'A-2']))

    def test_partial_prerequisite_is_not_enough(self):
        tasks = t.parse_tasks(self.L)
        self.assertEqual(t.unmet('A-3', tasks, set()), ['A-2'])
        self.assertEqual(t.unmet('A-3', tasks, {'A-2'}), [])  # completed earlier in the same run

    def test_validate(self):
        self.assertEqual(t.validate(self.L), [])
        self.assertTrue(t.validate(self.L + ['- [ ] **A-4** x (after: Z-9)\n']))
        cyc = ['- [ ] **B-1** a (after: B-2)\n', '- [ ] **B-2** b (after: B-1)\n']
        self.assertTrue(any('cycle' in p for p in t.validate(cyc)))


class Always(unittest.TestCase):
    def _vf(self, data):
        f = tempfile.NamedTemporaryFile('w', suffix='.json', delete=False)
        json.dump(data, f); f.close()
        self.addCleanup(os.remove, f.name)
        return f.name

    def test_always_pass_fail_and_missing(self):
        self.assertTrue(t.run_always(self._vf({'_always': ['true']})))
        self.assertFalse(t.run_always(self._vf({'_always': ['true', 'false']})))
        self.assertTrue(t.run_always(self._vf({})))
        self.assertTrue(t.run_always('/nonexistent/verify.json'))


class Verify(unittest.TestCase):
    def test_pass_fail_unavailable_missing(self):
        f = tempfile.NamedTemporaryFile('w', suffix='.json', delete=False)
        json.dump({'A-1': ['true'], 'A-2': ['false'], 'A-3': ['true', 'exit 3']}, f)
        f.close()
        self.addCleanup(os.remove, f.name)
        self.assertEqual(t.verify_task('A-1', f.name), 'pass')
        self.assertEqual(t.verify_task('A-2', f.name), 'fail')
        self.assertEqual(t.verify_task('A-3', f.name), 'unavailable')
        self.assertEqual(t.verify_task('A-9', f.name), 'unavailable')


if __name__ == '__main__':
    unittest.main()
