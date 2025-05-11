param(
    [switch]$Build,
    [switch]$Clean,
    [switch]$Help
)

function Show-Help {
    Write-Host "Azure IoT Device Simulator Management Script"
    Write-Host "Usage: .\run.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Build    Build the project before running"
    Write-Host "  -Clean    Clean the build artifacts"
    Write-Host "  -Help     Show this help message"
}

# Get the script directory and project path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path -Path $scriptPath -ChildPath ".."
$projectPath = Join-Path -Path $projectDir -ChildPath "DeviceSimulator\DeviceSimulator.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Error "Project file not found at: $projectPath"
    exit 1
}

if ($Help) {
    Show-Help
    exit 0
}

if ($Clean) {
    Write-Host "Cleaning build artifacts..."
    dotnet clean $projectPath
    exit 0
}

if ($Build) {
    Write-Host "Building project..."
    dotnet build $projectPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
        exit 1
    }
}

Write-Host "Starting device simulator..."
Write-Host "Press Ctrl+C to stop the simulator"
Write-Host ""

try {
    # Change to the project directory
    Push-Location (Split-Path -Parent $projectPath)
    dotnet run
}
catch {
    Write-Error "Error running device simulator: $_"
}
finally {
    # Restore the original directory
    Pop-Location
    Write-Host "`nDevice simulator stopped."
} 