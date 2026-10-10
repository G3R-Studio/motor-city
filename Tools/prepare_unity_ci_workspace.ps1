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
# The developer clone is read-only. New CI commits may not be present
# locally; fetch only the missing delta into the runner-owned Git database.
# Git fetch was previously stuck in the isolated runner checkout. We may
# replace only this CI workspace's partial .git data, never the seed repo.
$runnerPathMarker = [IO.Path]::Combine('_work', 'motor-city', 'motor-city')
if ($workspaceFull -notlike "*$runnerPathMarker*") {
    throw "Unexpected Unity CI workspace; refusing to touch its Git files: $workspaceFull"
}
# Reuse only the CI Git object database. Do not delete the user's source,
# and do not remove the CI Library cache between validations.
$target = Join-Path $workspaceFull '.git'
Write-Host "Initializing independent CI Git object database from read-only local seed."
New-Item -ItemType Directory -Path $workspaceFull -Force | Out-Null
& git -C $workspaceFull init --quiet
if ($LASTEXITCODE -ne 0) { throw "CI Git initialization failed." }
# Local Git object transfer; no GitHub network requests and no writes to seed.
& git -C $workspaceFull -c protocol.file.allow=always fetch --no-tags -- $seedFull HEAD
if ($LASTEXITCODE -ne 0) { throw "Local object transfer from seed failed." }
& git -C $workspaceFull cat-file -e "$Commit^{commit}" 2>$null
if ($LASTEXITCODE -ne 0) {
    if (-not $Repository -or $Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
        throw "Missing valid GITHUB_REPOSITORY for remote incremental fetch."
    }
    Write-Host "Requested SHA absent locally; fetching incremental objects into isolated CI cache."
    $remote = "https://github.com/$Repository.git"
    $env:GIT_HTTP_LOW_SPEED_LIMIT = '1024'
    $env:GIT_HTTP_LOW_SPEED_TIME = '60'
    # A full-history local base avoids GitHub's expensive --depth=1 pack.
    # The overall step has a separate 10-minute Actions timeout.
    & git -C $workspaceFull -c protocol.version=2 fetch --no-tags -- $remote $Commit
    if ($LASTEXITCODE -ne 0) { throw "Incremental remote fetch failed." }
}
$oldSmudge = $env:GIT_LFS_SKIP_SMUDGE
$env:GIT_LFS_SKIP_SMUDGE = '1'
try {
    & git -C $workspaceFull checkout --detach --force $Commit
} finally {
    $env:GIT_LFS_SKIP_SMUDGE = $oldSmudge
}
if ($LASTEXITCODE -ne 0) { throw "Cannot check out requested immutable commit." }
$actual = (& git -C $workspaceFull rev-parse HEAD).Trim()
if ($actual -ine $Commit) { throw "CI checkout SHA mismatch ($actual)." }
# Preserve Unity Library cache while deleting stale untracked files.
& git -C $workspaceFull clean -ffdx -e Library/ -e Temp/
if ($LASTEXITCODE -ne 0) { throw "Isolated CI workspace cleanup failed." }
# Locally populate Git LFS object cache as well (plain Git fetch does not
# transfer LFS media). No network calls and no writes to the seed.
$seedLfs = Join-Path $seedFull '.git/lfs/objects'
$workspaceLfs = Join-Path $workspaceFull '.git/lfs/objects'
if (Test-Path -LiteralPath $seedLfs) {
    New-Item -ItemType Directory -Path $workspaceLfs -Force | Out-Null
    & robocopy $seedLfs $workspaceLfs /E /XO /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Local Git LFS object cache transfer failed." }
}
& git -C $workspaceFull lfs checkout
if ($LASTEXITCODE -ne 0) { throw "Local Git LFS hydration failed." }
$unhydrated = & git -C $workspaceFull lfs ls-files |
    Where-Object { $_ -match '^[0-9a-f]+\\s+-\\s' }
if ($unhydrated) {
    throw "Unhydrated LFS pointers remain in CI workspace. Seed must hydrate its LFS files first."
}
Write-Host "Prepared exact $Commit from read-only local Git seed."
& git -C $workspaceFull count-objects -vH
