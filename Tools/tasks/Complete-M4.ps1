param([string]$UnityPath, [string[]]$Tasks = @('M4-01','M4-02','M4-03','M4-04','M4-05','M4-06','M4-07','M4-08','M4-10','M4-11','M4-12','M4-13','M4-14','M4-15','M4-16'))
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $projectRoot
$versionLine = Get-Content -LiteralPath 'ProjectSettings/ProjectVersion.txt' | Select-String '^m_EditorVersion: '
$editorVersion = ($versionLine.ToString() -replace '^m_EditorVersion: ', '').Trim()
if (-not $UnityPath) {
    $UnityPath = Join-Path ${env:ProgramFiles} "Unity/Hub/Editor/$editorVersion/Editor/Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity $editorVersion was not found. Supply -UnityPath with the matching Unity.exe path."
}
# Never close or kill the owner's running Editor. A stale lock file is harmless if it is unlocked.
$lockPath = Join-Path $projectRoot 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $lockPath) {
    try {
        $lockStream = [IO.File]::Open($lockPath, 'Open', 'ReadWrite', 'None')
        $lockStream.Dispose()
    } catch {
        throw 'Save your work and close this project in Unity, then rerun this command.'
    }
}
$pythonCommand = Get-Command python -ErrorAction Stop
$env:UNITY_PATH = [IO.Path]::GetFullPath($UnityPath)
$env:UNITY_PROJECT_PATH = $projectRoot
Write-Host 'Running the configured content checks and Unity suites. This can take several minutes.'
& $pythonCommand.Source 'Tools/tasks/tick_tasks.py' --complete @Tasks
if ($LASTEXITCODE -ne 0) {
    throw 'Some checks failed or prerequisites remain blocked. See the output above; passing tasks were ticked by the verifier.'
}
Write-Host 'All requested M4 tasks passed and are checked off in docs/task.md.'
