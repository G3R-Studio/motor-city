# Isolated checks for the shared text policy; not a Unity renderer test.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $projectRoot 'Tools/Tests/TextLayoutChecks.cs'), (Join-Path $projectRoot 'Assets/Scripts/UI/MotorCityTextLayout.cs'), (Join-Path $projectRoot 'Assets/Scripts/UI/MotorCityTextClip.cs')
[TextLayoutChecks]::Run()
Write-Output 'Fixed fonts, line-height headroom, content stability, glyph clipping and UV interpolation checks passed.'
