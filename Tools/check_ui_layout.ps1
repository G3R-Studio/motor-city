# Isolated checks for the shared text policy; not a Unity renderer test.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $projectRoot 'Tools/Tests/TextLayoutChecks.cs'), (Join-Path $projectRoot 'Assets/Scripts/UI/MotorCityTextLayout.cs')
[TextLayoutChecks]::Run()
Write-Output 'Fixed fonts, line-height headroom, bounded text and content-change stability checks passed.'
