param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Desktop', 'Mobile')]
    [string]$Profile,
    [string]$UnityPath = $env:UNITY_EDITOR_PATH
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe'
}
if (!(Test-Path -LiteralPath $UnityPath)) {
    throw "Unity Editor not found: '$UnityPath'"
}

$profileAsset = "Assets/Settings/Build Profiles/Web - $Profile - Release.asset"
$profileAbsolute = Join-Path $root $profileAsset
if (!(Test-Path -LiteralPath $profileAbsolute)) {
    throw "Unity Release Build Profile not found: $profileAsset"
}

$report = Join-Path $root 'Builds/Phase12'
New-Item -ItemType Directory -Path $report -Force | Out-Null
$env:MOTORCITY_PHASE12_PROFILE = $Profile
$env:MOTORCITY_PHASE12_REPORT_DIR = $report
$log = Join-Path $report "unity-phase12-$($Profile.ToLowerInvariant()).log"
$result = Join-Path $report "unity-phase12-$($Profile.ToLowerInvariant())-build.txt"
Remove-Item -LiteralPath $result -Force -ErrorAction SilentlyContinue

$argsUnity = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', ('"' + $root + '"'),
    '-activeBuildProfile', ('"' + $profileAsset + '"'),
    '-executeMethod', 'MotorCity.EditorTools.MotorCityPhase12ReleaseBuild.Build',
    '-logFile', ('"' + $log + '"')
)

Write-Host "Motor City Phase 12 - WebGL $Profile Release"
Write-Host "Profile: $profileAsset"
Write-Host "Commit: $env:GITHUB_SHA"
Write-Host "Unity: $UnityPath"
$process = Start-Process -FilePath $UnityPath -ArgumentList $argsUnity -PassThru
$started = Get-Date
while (-not $process.WaitForExit(60000)) {
    $elapsed = [int]((Get-Date) - $started).TotalMinutes
    Write-Host "Unity WebGL $Profile Release still running after $elapsed minutes. Log tail:"
    if (Test-Path -LiteralPath $log) {
        Get-Content -LiteralPath $log -Tail 6 | ForEach-Object { Write-Host $_ }
    } else {
        Write-Host 'Unity Editor log not yet available.'
    }
}
$code = $process.ExitCode
Write-Host "Unity $Profile Release exited after $([int]((Get-Date) - $started).TotalSeconds)s: $code"
if ($code -ne 0) {
    if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Tail 100 }
    throw "Phase 12 $Profile Release failed with Unity exit code $code."
}
if (!(Test-Path -LiteralPath $result)) {
    if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Tail 100 }
    throw "Unity exited 0 without required Phase 12 $Profile build report."
}
$output = Join-Path $report "WebGL-$Profile-Release"
if (!(Test-Path -LiteralPath (Join-Path $output 'index.html'))) {
    throw "Phase 12 $Profile output has no index.html"
}
$lines = Get-Content -LiteralPath $result
$lines
if (!($lines -match '^Build result: Succeeded$')) {
    throw "Phase 12 $Profile Unity build did not report Succeeded."
}
Write-Host "Phase 12 $Profile Release build PASSED. Output: $output"
