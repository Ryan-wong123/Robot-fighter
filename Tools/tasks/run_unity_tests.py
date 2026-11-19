"""Run native Unity tests and preserve XML/editor logs, including failed runs."""
import argparse
from datetime import datetime, timezone
import os
from pathlib import Path
import re
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET


def report_results(results, editor_log, returncode):
    print('Unity exit code:', returncode)
    print('Unity log:', editor_log)
    print('Test report:', results)
    if results.exists():
        try:
            report = ET.parse(results).getroot()
        except ET.ParseError as error:
            print('Invalid test report:', error)
            return 1
        failed = report.findall(".//test-case[@result='Failed']")
        for case in failed[:10]:
            print('FAILED:', case.get('fullname', case.get('name', 'unknown test')))
            print((case.findtext('failure/message') or 'No failure message').strip())
            stack = case.findtext('failure/stack-trace') or ''
            if stack: print('\n'.join(stack.strip().splitlines()[:8]))
        if not failed:
            for failure in report.findall('.//failure')[:3]:
                print((failure.findtext('message') or 'Suite setup failed').strip())
        if returncode == 0 and report.get('result') == 'Passed' and int(report.get('passed', '0')) > 0:
            print('Unity tests passed:', report.get('passed'))
            return 0
        print('Unity tests did not establish a passing run.')
    else:
        print('Unity did not produce test results; check the retained editor log.')
    if editor_log.exists():
        lines = editor_log.read_text(encoding='utf-8', errors='replace').splitlines()
        errors = [index for index, line in enumerate(lines)
                  if any(word in line.lower() for word in ('error ', 'exception', 'failed', 'aborting'))]
        for index in errors[-5:]:
            print('\n'.join(lines[max(0, index-1):min(len(lines), index+5)]))
        if not errors: print('\n'.join(lines[-20:]))
    return 1


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=('EditMode', 'PlayMode'))
    parser.add_argument('filter', nargs='?', default='')
    args = parser.parse_args()
    editor = os.environ.get('UNITY_PATH', '')
    project = Path(os.environ.get('UNITY_PROJECT_PATH', '.')).resolve()
    if not editor or not Path(editor).is_file() or not (project/'ProjectSettings/ProjectVersion.txt').is_file():
        print('Unity tests unavailable: set UNITY_PATH and use the project root.')
        return 3
    label = re.sub(r'[^A-Za-z0-9._-]', '_', args.filter or 'All')
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ') + '-' + uuid.uuid4().hex[:8]
    directory = project/'Logs'/'TaskVerification'/stamp
    directory.mkdir(parents=True)
    results = directory/(label + '.xml')
    editor_log = directory/(label + '.Editor.log')
    command = [editor, '-batchmode', '-nographics', '-projectPath', str(project), '-runTests',
               '-testPlatform', args.mode, '-testResults', str(results), '-logFile', str(editor_log)]
    if args.filter: command += ['-testFilter', args.filter]
    print('Running Unity suite:', args.filter or 'All', flush=True)
    print('Retaining results in:', directory, flush=True)
    try:
        completed = subprocess.run(command)
    except OSError as error:
        print('Could not launch Unity:', error)
        return 1
    return report_results(results, editor_log, completed.returncode)


if __name__ == '__main__':
    sys.exit(main())
