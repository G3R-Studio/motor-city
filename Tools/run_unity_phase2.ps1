param(
    [switch]$BuildWebGL,
    [string]$UnityPath = $env:UNITY_EDITOR_PATH
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe'
}
if (!(Test-Path -LiteralPath $UnityPath)) {
    throw "Unity Editor not found at '$UnityPath'. Pass -UnityPath or set UNITY_EDITOR_PATH."
}
$report = Join-Path $root 'Temp/MotorCityAudit/UnityPhase2'
New-Item -ItemType Directory -Path $report -Force | Out-Null
$env:MOTORCITY_PHASE2_REPORT_DIR = $report
$log = Join-Path $report 'unity-batch.log'
$expected = if ($BuildWebGL) { 'unity-build.txt' } else { 'unity-compile.txt' }
Remove-Item -LiteralPath (Join-Path $report $expected) -Force -ErrorAction SilentlyContinue
$method = if ($BuildWebGL) {
    'MotorCity.EditorTools.MotorCityPhase2BatchGate.BuildWebGL'
} else {
    'MotorCity.EditorTools.MotorCityPhase2BatchGate.Validate'
}
Write-Host "Unity Phase 2 gate: $method"
Write-Host "Editor: $UnityPath"
# Unity.exe is a Windows GUI-subsystem executable; direct invocation from pwsh
# can leave LASTEXITCODE unset. Start-Process gives a reliable exit code.
$argsUnity = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', ('"' + $root + '"'),
    '-executeMethod', $method,
    '-logFile', ('"' + $log + '"')
)
$process = Start-Process -FilePath $UnityPath -ArgumentList $argsUnity -Wait -PassThru
$code = $process.ExitCode
if ($code -ne 0) {
    Write-Host "Unity exited with code $code. Last log lines:"
    if (Test-Path $log) { Get-Content $log -Tail 90 }
    throw "Unity Phase 2 validation failed."
}
if (!(Test-Path (Join-Path $report $expected))) {
    if (Test-Path $log) { Get-Content $log -Tail 90 }
    throw "Unity exited 0 but required report '$expected' is missing."
}
Get-Content (Join-Path $report $expected)
Write-Host "Unity Phase 2 gate PASSED. Reports: $report"
