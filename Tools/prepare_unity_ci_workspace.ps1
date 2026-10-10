param(
    [Parameter(Mandatory=$true)][string]$Commit,
    [string]$Repository = $env:GITHUB_REPOSITORY,
    [string]$Workspace = $env:GITHUB_WORKSPACE,
    [string]$ReadOnlySeed = 'D:\GitHub\motor-city'
)
$ErrorActionPreference = 'Stop'
if ($Commit -notmatch '^[0-9a-fA-F]{40}$') { throw "Expected immutable commit SHA." }
if (-not $Workspace) { throw "GITHUB_WORKSPACE is missing." }
$workspaceFull = [IO.Path]::GetFullPath($Workspace)
$seedFull = [IO.Path]::GetFullPath($ReadOnlySeed)
if ($workspaceFull.TrimEnd('\') -ieq $seedFull.TrimEnd('\')) {
    throw "CI workspace MUST NOT point to the developer's working repository."
}
if (-not (Test-Path -LiteralPath $seedFull -PathType Container)) {
    throw "Local read-only Git seed not found at $seedFull. Do not use an unverified network fallback."
}
$inside = & git -C $seedFull rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0 -or [IO.Path]::GetFullPath($inside).TrimEnd('\') -ine $seedFull.TrimEnd('\')) {
    throw "Read-only Git seed is not a valid repository at $seedFull."
}
# cat-file reads the local object database only, and never fetches or updates
# branches, the index, or working files in the developer's repository.
& git -C $seedFull cat-file -e "$Commit^{commit}" 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "Commit $Commit is missing from the local seed. Fetch it into $seedFull yourself first (git fetch origin), then re-run CI. CI will not write to that repository."
}
$target = Join-Path $workspaceFull '.git'
if (Test-Path -LiteralPath $target) {
    throw "CI Git directory already exists. Refusing to mix incomplete network checkout with local seed; clean only the isolated runner workspace manually."
}
Write-Host "Creating independent CI Git object database from read-only local seed."
New-Item -ItemType Directory -Path $workspaceFull -Force | Out-Null
# --no-hardlinks copies objects: no permanent alternates or hardlinks into
# the developer repository; no user working files or index are modified.
& git clone --local --no-hardlinks --no-checkout -- $seedFull $workspaceFull
if ($LASTEXITCODE -ne 0) { throw "Local Git clone failed." }
& git -C $workspaceFull checkout --detach --force $Commit
if ($LASTEXITCODE -ne 0) { throw "Cannot check out requested immutable commit." }
$actual = (& git -C $workspaceFull rev-parse HEAD).Trim()
if ($actual -ine $Commit) { throw "CI checkout SHA mismatch ($actual)." }
# Detect Git LFS pointers: never silently validate a Unity project with
# placeholder binary assets.
$lfs = & git -C $workspaceFull lfs ls-files 2>$null
if ($LASTEXITCODE -eq 0 -and $lfs) {
    Write-Host "Repository contains LFS-managed assets; verifying hydration."
    & git -C $workspaceFull lfs checkout
    if ($LASTEXITCODE -ne 0) { throw "Unable to hydrate LFS objects from local cache." }
    $missing = & git -C $workspaceFull lfs ls-files 2>$null | Where-Object { $_ -match '^\s*[0-9a-f]+\s+-\s+' }
    if ($missing) { throw "Some LFS objects are unavailable locally. Refusing incomplete Unity audit." }
}
Write-Host "Prepared exact $Commit from read-only local Git seed."
& git -C $workspaceFull count-objects -vH
