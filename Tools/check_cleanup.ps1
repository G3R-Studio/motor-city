# PowerShell 7: run from anywhere inside this checkout. These are source/stub
# checks, not a substitute for a Unity compilation or Play Mode profiling.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll'), (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
    $files = @(Get-ChildItem Assets,Packages -Recurse -Filter '*.cs' | Select-Object -ExpandProperty FullName)
    foreach ($symbols in @(@('UNITY_EDITOR'), @(), @('UNITY_WEBGL'))) {
        $options = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithPreprocessorSymbols([string[]]$symbols)
        foreach ($file in $files) {
            $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($file), $options)
            $errors = @($tree.GetDiagnostics() | Where-Object Severity -eq Error)
            if ($errors.Count) { throw "$file : $errors" }
        }
    }
    Write-Output "All $($files.Count) C# sources passed syntax checks (Editor, native player, WebGL)."
    # Phase 9 removed the costly realtime city reflection-probe grid and its
    # capture runner. The old ReflectionCaptureChecks fixture exercised only
    # that deleted class; instead guard against accidentally restoring it.
    $retiredRunner = 'Assets/Scripts/World/CityReflectionProbeCaptureRunner.cs'
    if (Test-Path -LiteralPath $retiredRunner) {
        throw "Retired city reflection-probe capture runner was restored."
    }
    foreach ($runtimeFile in @(
        'Assets/Scripts/World/CityAssetRuntimeInstaller.cs'
        'Assets/Scripts/World/DayNightCycleController.cs'
    )) {
        $runtimeSource = [IO.File]::ReadAllText((Join-Path $projectRoot $runtimeFile))
        if ($runtimeSource.Contains('InstallCityReflectionProbes') -or
            $runtimeSource.Contains('CityReflectionProbeCaptureRunner')) {
            throw "Retired realtime city reflection capture reference in $runtimeFile"
        }
    }
    Write-Output 'Phase 9 retired city reflection-probe capture regression check passed.'

    Add-Type -Path Tools/Tests/CityFieldCacheChecks.cs,Assets/Scripts/World/AuthoredCityFieldCache.cs
    [CityFieldCacheChecks]::Run()
    # VehicleLampMaterialUtility now resolves wheel roles through the shared
    # VehicleVisualRoleUtility. Compile both real helpers with the Unity stubs,
    # rather than testing an obsolete stand-alone copy of wheel detection.
    Add-Type -Path @(
        'Tools/Tests/VehicleLampUtilityChecks.cs'
        'Assets/Scripts/World/VehicleLampMaterialUtility.cs'
        'Assets/Scripts/Vehicle/VehicleVisualRoleUtility.cs'
    )
    [VehicleLampUtilityChecks]::Run()
    Write-Output 'Shared field cache and lamp material/UV/projection checks passed.'

    # Compile the changed cosmetic dispatch methods directly from source.
    $source = [IO.File]::ReadAllText((Join-Path $projectRoot 'Assets/Scripts/Gameplay/VehicleCustomizationSystem.cs'))
    $methods = foreach ($name in @('CycleBodyColor','CycleWheelStyle','CycleNeon','Changed','ApplyAll','ClearStaleVehiclePropertyBlocks','RebuildNeon')) {
        $match = [regex]::Match($source, '        (?:public|private) void ' + $name + '\([^)]*\)\s*\{')
        if (!$match.Success) { throw "Missing cosmetic method: $name" }
        $offset = $match.Index + $match.Length
        $depth = 1
        while ($depth -gt 0) {
            if ($source[$offset] -eq '{') { $depth++ }
            if ($source[$offset] -eq '}') { $depth-- }
            $offset++
        }
        $source.Substring($match.Index, $offset - $match.Index)
    }
    $template = [IO.File]::ReadAllText((Join-Path $projectRoot 'Tools/Tests/CustomizationDispatchChecks.template'))
    # Each Add-Type compilation is separate, but all its types remain loaded
    # inside this PowerShell process. Do not define UnityEngine.Transform or
    # any other UnityEngine stub for the second time: the customization
    # template uses MotorCityCleanupTests.Customization to avoid collisions.
    # Its local doubles compile with the seven extracted REAL methods.
    Add-Type -TypeDefinition $template.Replace('/* SOURCE_METHODS */', ($methods -join "`n"))
    [CustomizationChecks]::Run()
    Write-Output 'Cosmetic dispatch, property-block cleanup, save/events, vehicle initialization and color-cycle checks passed.'
    Write-Output 'All cleanup syntax and source checks passed.'
}
finally {
    Pop-Location
}
