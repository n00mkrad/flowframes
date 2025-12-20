# Go to script directory
Set-Location $PSScriptRoot

# Define paths and tools
$winDir = ${env:WINDIR}
$progFiles64 = ${env:ProgramW6432}
# $msBuildPath = [IO.Path]::Combine($progFiles64, "Microsoft Visual Studio", "2022", "Community", "MSBuild", "Current", "Bin", "MSBuild.exe")
$msBuildPath = [IO.Path]::Combine($progFiles64, "Microsoft Visual Studio", "18", "Insiders", "MSBuild", "Current", "Bin", "MSBuild.exe")
$solutionRoot = $PSScriptRoot
$ilRepackPath = [IO.Path]::Combine($solutionRoot, "packages", "ILRepack.2.0.34", "tools", "ILRepack.exe")
$solutionPath = [IO.Path]::Combine($solutionPath, "Flowframes.sln")
$outputDir = [IO.Path]::Combine($solutionRoot, "bin")
$packedDir = [IO.Path]::Combine($outputDir, "packed")

# Check if files exist
if (Test-Path $msBuildPath) { Write-Host "Found MSBuild at $msBuildPath" } else { Write-Host "MSBuild not found! ($msBuildPath)"; exit 1 }
if (Test-Path $ilRepackPath) { Write-Host "Found ILRepack at $ilRepackPath" } else { Write-Host "ILRepack not found! ($ilRepackPath)"; exit 1 }
if (Test-Path $solutionPath) { Write-Host "Found solution at $solutionPath" } else { Write-Host "Solution not found! ($solutionPath)"; exit 1 }

# Ensure output directory exists
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir
}

# Define configurations and platforms
$configurations = @("Release", "ConsoleRelease")
$assemblyNames = @("Flowframes", "FlowframesCmd")
$buildDirs = @("RelGui", "RelCmd")

# Function to build and repack
function Build-And-Repack ($config, $assemblyName, $buildDir) {
    # Build the project
    & $msBuildPath $solutionPath /p:Configuration=$config /p:Platform="x64" /m
    if ($LASTEXITCODE -ne 0) { 
        Write-Error "Build failed for configuration: $config"
        return
    }

    # ILRepack to merge all assemblies into a single executable
    $targetDir = "$outputDir\$buildDir"
	
	# Rename files matching *Native*.dll to .xdll to exclude them from repacking
    Get-ChildItem $targetDir -Filter "*Native*.dll" | Rename-Item -NewName { $_.Name -replace '\.dll$', '.xdll' }
	
    $mainAssembly = "$targetDir\Flowframes.exe"
    $outputAssembly = "$packedDir\$assemblyName.exe"
	Write-Host "------------"
    $target = "winexe"
    if ($assemblyName -like "*Cmd") { $target = "exe" }
    & $ilRepackPath /out:$outputAssembly /target:$target /targetplatform:v4,"$winDir\Microsoft.NET\Framework64\v4.0.30319" /union /wildcards "$mainAssembly" "$targetDir\*.dll"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Repacking failed for configuration: $config"
    } else {
        Write-Host "Successfully repacked: $outputAssembly"
    }
	
    # Delete everything in $targetDir except .xdll files
    Get-ChildItem $targetDir -Exclude "*.xdll" | Remove-Item -Force

	# Reverse the renaming from .xdll back to .dll
    Get-ChildItem $targetDir -Filter "*.xdll" | Rename-Item -NewName { $_.Name -replace '\.xdll$', '.dll' }
}

# Remove $packedDir
if (Test-Path $packedDir) {
	Remove-Item -Recurse -Force $packedDir
}

# Process each configuration
foreach ($index in 0..1) {
    Build-And-Repack -config $configurations[$index] -assemblyName $assemblyNames[$index] -buildDir $buildDirs[$index]
}

Write-Host "Build and repack process completed."

# Delete files other than .exe
Get-ChildItem $packedDir -Exclude "*.exe" | Remove-Item -Force

# Copy all files matching "*Native*.dll" from first $buildDirs directory to $packedDir
Get-ChildItem "$outputDir\$($buildDirs[0])" -Filter "*Native*.dll" | Copy-Item -Destination $packedDir

# Remove buildDirs
foreach ($buildDir in $buildDirs) {
	Remove-Item -Recurse -Force "$outputDir\$buildDir"
}

Write-Host "All done."