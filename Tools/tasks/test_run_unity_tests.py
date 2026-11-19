"""Exercise reporting with synthetic XML; never launches Unity."""
import contextlib
import io
from pathlib import Path
import tempfile
import unittest
import run_unity_tests as runner

class Reports(unittest.TestCase):
    def check_report(self, xml, code=0, log=''):
        with tempfile.TemporaryDirectory() as directory:
            result=Path(directory)/'suite.xml'; editor=Path(directory)/'Editor.log'
            if xml is not None: result.write_text(xml,encoding='utf-8')
            editor.write_text(log,encoding='utf-8')
            output=io.StringIO()
            with contextlib.redirect_stdout(output): status=runner.report_results(result,editor,code)
            self.assertTrue(editor.exists())
            if xml is not None:self.assertTrue(result.exists())
            return status,output.getvalue()

    def test_real_pass_requires_zero_exit_and_positive_test_count(self):
        self.assertEqual(self.check_report('<test-run result="Passed" passed="2"/>')[0],0)
        self.assertEqual(self.check_report('<test-run result="Passed" passed="0"/>')[0],1)
        self.assertEqual(self.check_report('<test-run result="Passed" passed="2"/>',code=1)[0],1)

    def test_failed_test_name_message_and_stack_are_visible(self):
        status,output=self.check_report('<test-run result="Failed" passed="0"><test-case fullname="Rig.Pose" result="Failed"><failure><message>Hand missing</message><stack-trace>Rig.cs:12</stack-trace></failure></test-case></test-run>',code=2)
        self.assertEqual(status,1)
        for text in ('Rig.Pose','Hand missing','Rig.cs:12'):self.assertIn(text,output)

    def test_missing_report_prints_editor_errors(self):
        status,output=self.check_report(None,code=1,log='Compiler error CS2001: missing source')
        self.assertEqual(status,1);self.assertIn('CS2001',output)

    def test_invalid_xml_cannot_pass(self):
        self.assertEqual(self.check_report('<broken>')[0],1)

    def test_suite_setup_failure_is_reported(self):
        status,output=self.check_report('<test-run result="Failed" passed="0"><test-suite><failure><message>Setup failed</message></failure></test-suite></test-run>')
        self.assertEqual(status,1);self.assertIn('Setup failed',output)

if __name__=='__main__':unittest.main()
