@echo off
setlocal

echo ============================================================
echo Motor City - prepare full AI review snapshot
echo ============================================================
echo.
echo This stages Motor City-owned and generated project state.
echo Third-party Asset Store source packages remain ignored.
echo.

git add -A
if errorlevel 1 (
    echo.
    echo git add failed.
    exit /b 1
)

echo.
echo Staged/current changes:
git status --short

echo.
echo Checking for files larger than 95 MB...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$files = Get-ChildItem -LiteralPath . -Recurse -File -Force -ErrorAction SilentlyContinue ^| Where-Object { $_.FullName -notmatch '\\.git\\' -and $_.Length -gt 95MB }; if ($files) { $files ^| Sort-Object Length -Descending ^| ForEach-Object { '{0,10:N1} MB  {1}' -f ($_.Length / 1MB), $_.FullName }; exit 2 }"

if errorlevel 2 (
    echo.
    echo WARNING: GitHub rejects ordinary Git files over 100 MB.
    echo Install/configure Git LFS or exclude/reduce the files shown above before pushing.
    echo.
) else (
    echo No files over 95 MB found.
    echo.
)

echo Review AI_PROJECT_CONTEXT.md and AI_REVIEW_PROMPT.md if needed.
echo.
echo When ready:
echo   git commit -m "Add full project snapshot for AI review"
echo   git push origin main
echo.
endlocal
