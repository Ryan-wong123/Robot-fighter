"""Cross-platform equivalent of run_unity_tests.sh; missing UNITY_PATH returns 3."""
import argparse
import os
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET

parser=argparse.ArgumentParser()
parser.add_argument('mode',choices=('EditMode','PlayMode'))
parser.add_argument('filter',nargs='?',default='')
args=parser.parse_args()
editor=os.environ.get('UNITY_PATH','')
project=Path(os.environ.get('UNITY_PROJECT_PATH','.')).resolve()
if not editor or not Path(editor).is_file() or not (project/'ProjectSettings/ProjectVersion.txt').is_file():
 print('Unity tests unavailable: set UNITY_PATH and use the project root.')
 raise SystemExit(3)
with tempfile.TemporaryDirectory(prefix='wobble-unity-tests-') as directory:
 results=Path(directory)/'results.xml'
 command=[editor,'-batchmode','-nographics','-projectPath',str(project),'-runTests','-testPlatform',args.mode,'-testResults',str(results),'-logFile','-']
 if args.filter:command+=['-testFilter',args.filter]
 completed=subprocess.run(command)
 if completed.returncode:raise SystemExit(1)
 if not results.exists():raise SystemExit('Unity returned without test results.')
 report=ET.parse(results).getroot()
 if report.get('result')!='Passed' or int(report.get('passed','0'))<=0:
  raise SystemExit('Unity test results did not establish a passing test run.')
 print('Unity tests passed:',report.get('passed'))
