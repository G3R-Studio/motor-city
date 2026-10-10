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
# Git fetch was previously stuck in the isolated runner checkout. We may
# replace only this CI workspace's partial .git data, never the seed repo.
$runnerPathMarker = [IO.Path]::Combine('_work', 'motor-city', 'motor-city')
if ($workspaceFull -notlike "*$runnerPathMarker*") {
    throw "Unexpected Unity CI workspace; refusing to touch its Git files: $workspaceFull"
}
$target = Join-Path $workspaceFull '.git'
if (Test-Path -LiteralPath $target) {
    Remove-Item -LiteralPath $target -Recurse -Force
}
Write-Host "Initializing independent CI Git object database from read-only local seed."
New-Item -ItemType Directory -Path $workspaceFull -Force | Out-Null
& git -C $workspaceFull init --quiet
if ($LASTEXITCODE -ne 0) { throw "CI Git initialization failed." }
# Local Git object transfer; no GitHub network requests and no writes to seed.
& git -C $workspaceFull -c protocol.file.allow=always fetch --no-tags --no-write-fetch-head -- $seedFull $Commit
if ($LASTEXITCODE -ne 0) { throw "Local object transfer from seed failed." }
& git -C $workspaceFull checkout --detach --force $Commit
if ($LASTEXITCODE -ne 0) { throw "Cannot check out requested immutable commit." }
$actual = (& git -C $workspaceFull rev-parse HEAD).Trim()
if ($actual -ine $Commit) { throw "CI checkout SHA mismatch ($actual)." }
# Preserve Unity Library cache while deleting stale untracked files.
& git -C $workspaceFull clean -ffdx -e Library/ -e Temp/
if ($LASTEXITCODE -ne 0) { throw "Isolated CI workspace cleanup failed." }
# Disallow unhydrated LFS assets in the Unity scene imports.
& git -C $workspaceFull lfs checkout
if ($LASTEXITCODE -ne 0) {
    throw "LFS hydration failed; fetch missing objects into the local seed first."
}
Write-Host "Prepared exact $Commit from read-only local Git seed."
& git -C $workspaceFull count-objects -vH
