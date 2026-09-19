@echo off
setlocal

title Motor City - AI review snapshot

echo ============================================================
echo Motor City - prepare full AI review snapshot
echo ============================================================
echo.
echo This stages Motor City-owned and generated project state.
echo Third-party Asset Store source packages remain ignored.
echo.

where git >nul 2>nul
if errorlevel 1 (
    echo ERROR: git was not found in PATH.
    echo Open Git Bash or reinstall Git with command-line support.
    echo.
    pause
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo ERROR: this file must be run from inside the motor-city Git repository.
    echo Current folder:
    cd
    echo.
    pause
    exit /b 1
)

echo Staging current project files...
git add -A
if errorlevel 1 (
    echo.
    echo ERROR: git add -A failed.
    echo.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo Current staged/untracked status
echo ============================================================
git status --short

echo.
echo ============================================================
echo Checking for files larger than 95 MB
echo ============================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$files = Get-ChildItem -LiteralPath . -Recurse -File -Force -ErrorAction SilentlyContinue; " ^
  "$large = $files | Where-Object { $_.FullName -notmatch '\\.git\\' -and $_.Length -gt 95MB }; " ^
  "if ($large) { " ^
  "  $large | Sort-Object Length -Descending | ForEach-Object { " ^
  "    '{0,10:N1} MB  {1}' -f ($_.Length / 1MB), $_.FullName " ^
  "  }; " ^
  "  exit 2 " ^
  "} else { " ^
  "  Write-Host 'No files over 95 MB found.'; " ^
  "  exit 0 " ^
  "}"

set "SIZECHECK=%ERRORLEVEL%"

echo.
if "%SIZECHECK%"=="2" (
    echo WARNING: one or more files are larger than 95 MB.
    echo GitHub rejects ordinary Git files over 100 MB.
    echo Use Git LFS or exclude/reduce those files before pushing.
) else if not "%SIZECHECK%"=="0" (
    echo WARNING: the large-file check returned code %SIZECHECK%.
    echo You can still inspect the staged files with: git status
)

echo.
echo ============================================================
echo Next steps
echo ============================================================
echo Review AI_PROJECT_CONTEXT.md and AI_REVIEW_PROMPT.md if needed.
echo.
echo When ready, run:
echo   git commit -m "Add full project snapshot for AI review"
echo   git push origin main
echo.
echo Nothing was committed or pushed automatically.
echo.
pause
endlocal
