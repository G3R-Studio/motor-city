@echo off
setlocal

title Motor City - AI review snapshot

echo ============================================================
echo Motor City - prepare full AI review snapshot
echo ============================================================
echo.
echo This stages Motor City-owned and generated project state.
echo Unity caches and third-party Asset Store source packages stay excluded.
echo.

where git >nul 2>nul
if errorlevel 1 (
    echo ERROR: git was not found in PATH.
    echo.
    pause
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo ERROR: run this file from inside the motor-city Git repository.
    echo Current folder:
    cd
    echo.
    pause
    exit /b 1
)

echo Staging normal project changes...
git add -A
if errorlevel 1 (
    echo.
    echo ERROR: git add -A failed.
    echo.
    pause
    exit /b 1
)

echo.
echo Staging AI-review generated state when present...
if exist "Assets\Resources\MotorCity\Environment" git add -f -- "Assets/Resources/MotorCity/Environment"
if exist "Assets\Resources\MotorCity\Environment.meta" git add -f -- "Assets/Resources/MotorCity/Environment.meta"
if exist "Assets\Resources\MotorCity\PlayerCarVisual.prefab" git add -f -- "Assets/Resources/MotorCity/PlayerCarVisual.prefab"
if exist "Assets\Resources\MotorCity\PlayerCarVisual.prefab.meta" git add -f -- "Assets/Resources/MotorCity/PlayerCarVisual.prefab.meta"
if exist "Assets\Resources\MotorCity\UI" git add -f -- "Assets/Resources/MotorCity/UI"
if exist "Assets\Resources\MotorCity\UI.meta" git add -f -- "Assets/Resources/MotorCity/UI.meta"
if exist "Assets\Resources\MotorCity\Markers" git add -f -- "Assets/Resources/MotorCity/Markers"
if exist "Assets\Resources\MotorCity\Markers.meta" git add -f -- "Assets/Resources/MotorCity/Markers.meta"
if exist "Assets\LocalGenerated" git add -f -- "Assets/LocalGenerated"
if exist "Assets\LocalGenerated.meta" git add -f -- "Assets/LocalGenerated.meta"
if exist "MotorCity_FCGSceneReport.txt" git add -f -- "MotorCity_FCGSceneReport.txt"
if exist "MotorCity_CityAssetReport.txt" git add -f -- "MotorCity_CityAssetReport.txt"

echo.
echo ============================================================
echo Snapshot source presence
echo ============================================================
if exist "Assets\Resources\MotorCity\Environment\CityVisual.prefab" (
    echo [FOUND] Assets/Resources/MotorCity/Environment/CityVisual.prefab
) else (
    echo [MISSING] Assets/Resources/MotorCity/Environment/CityVisual.prefab
)
if exist "Assets\LocalGenerated\FCG_Workbench.unity" (
    echo [FOUND] Assets/LocalGenerated/FCG_Workbench.unity
) else (
    echo [MISSING] Assets/LocalGenerated/FCG_Workbench.unity
)
if exist "Assets\Resources\MotorCity\PlayerCarVisual.prefab" (
    echo [FOUND] Assets/Resources/MotorCity/PlayerCarVisual.prefab
) else (
    echo [MISSING] Assets/Resources/MotorCity/PlayerCarVisual.prefab
)
if exist "MotorCity_FCGSceneReport.txt" (
    echo [FOUND] MotorCity_FCGSceneReport.txt
) else (
    echo [MISSING] MotorCity_FCGSceneReport.txt
)
if exist "MotorCity_CityAssetReport.txt" (
    echo [FOUND] MotorCity_CityAssetReport.txt
) else (
    echo [MISSING] MotorCity_CityAssetReport.txt
)

echo.
echo ============================================================
echo Current staged status
echo ============================================================
git status --short

echo.
echo ============================================================
echo Checking STAGED files larger than 95 MB
echo ============================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$paths = git diff --cached --name-only --diff-filter=ACMR; " ^
  "$large = foreach ($p in $paths) { if (Test-Path -LiteralPath $p -PathType Leaf) { $f = Get-Item -LiteralPath $p; if ($f.Length -gt 95MB) { $f } } }; " ^
  "if ($large) { " ^
  "  $large | Sort-Object Length -Descending | ForEach-Object { '{0,10:N1} MB  {1}' -f ($_.Length / 1MB), $_.FullName }; " ^
  "  exit 2 " ^
  "} else { " ^
  "  Write-Host 'No STAGED files over 95 MB found.'; " ^
  "  exit 0 " ^
  "}"

set "SIZECHECK=%ERRORLEVEL%"

echo.
if "%SIZECHECK%"=="2" (
    echo WARNING: one or more STAGED files are larger than 95 MB.
    echo GitHub rejects ordinary Git files over 100 MB.
    echo Those files need Git LFS or must be excluded/reduced before pushing.
) else if not "%SIZECHECK%"=="0" (
    echo WARNING: staged large-file check returned code %SIZECHECK%.
)

echo.
echo ============================================================
echo Next steps
echo ============================================================
if "%SIZECHECK%"=="2" (
    echo Do NOT push yet. Send the large-file list here first.
) else (
    echo If the staged status contains the generated snapshot files you expect:
    echo   git commit -m "Add full project snapshot for AI review"
    echo   git push origin main
)
echo.
echo Unity Library/Temp/Logs are intentionally ignored and do not need to be uploaded.
echo Nothing was committed or pushed automatically.
echo.
pause
endlocal
