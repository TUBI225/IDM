[CmdletBinding()]
param(
    [string]$VolumeA = "",
    [string]$VolumeB = ""
)

# Protocole inter-volume réel : téléchargement + finalisation entre deux volumes
# physiques distincts, avec terminaison abrupte du subprocess aux frontières
# AfterInterVolumeStagingFlushed et AfterInterVolumeDestinationMoved.
# Usage :
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\run-intervolume-real.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\run-intervolume-real.ps1 -VolumeA C -VolumeB D
# Prérequis : deux volumes fixes ou amovibles montés (ex. disque système C: et
# clé USB E:). Sans second volume, voir le message affiché en fin d'exécution.

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$reportLines = [System.Collections.Generic.List[string]]::new()
$reportPath = Join-Path $projectRoot 'TestResults\intervolume-real-report.txt'

function Write-Step([string]$message) {
    Write-Host $message
    $reportLines.Add($message)
}

function Find-UsableVolumes {
    param([string]$ExplicitA, [string]$ExplicitB)

    $all = Get-Volume -ErrorAction SilentlyContinue |
        Where-Object { $_.DriveLetter -and $_.DriveType -in @('Fixed', 'Removable') } |
        Sort-Object DriveLetter

    $chosenA = if ($ExplicitA) { ($all | Where-Object { $_.DriveLetter -eq $ExplicitA } | Select-Object -First 1) } else { $all | Select-Object -First 1 }
    $chosenB = if ($ExplicitB) { ($all | Where-Object { $_.DriveLetter -eq $ExplicitB } | Select-Object -First 1) } else { $all | Select-Object -Skip 1 -First 1 }

    return @($chosenA, $chosenB)
}

function Test-CrashHostReady {
    $configuration = 'Release'
    $hostAssembly = Join-Path $projectRoot "tests-dotnet\WindowsDownloadManager.CrashTestHost\bin\$configuration\net10.0\WindowsDownloadManager.CrashTestHost.dll"
    if (-not (Test-Path -LiteralPath $hostAssembly)) {
        Write-Step "==> Compilation Release de la solution (CrashTestHost absent)..."
        $env:DOTNET_CLI_HOME = Join-Path $projectRoot '.dotnet-cli'
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        $env:TESTINGPLATFORM_TELEMETRY_OPTOUT = '1'
        $env:APPDATA = Join-Path $projectRoot '.build-appdata'
        $dotnet = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
        & $dotnet build (Join-Path $projectRoot 'WindowsDownloadManager.slnx') -c Release --no-restore | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "La compilation Release a échoué (code $LASTEXITCODE)."
        }
    }

    return $hostAssembly
}

function Invoke-InterVolumeScenario {
    param(
        [string]$Boundary,
        [string]$TaskId,
        [string]$DatabasePath,
        [string]$TemporaryPath,
        [string]$DestinationPath,
        [string]$HostAssembly
    )

    $dotnet = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new($dotnet)
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $startInfo.ArgumentList.Add($HostAssembly)
    $startInfo.ArgumentList.Add($Boundary)
    $startInfo.ArgumentList.Add($TaskId)
    $startInfo.ArgumentList.Add($DatabasePath)
    $startInfo.ArgumentList.Add($TemporaryPath)
    $startInfo.ArgumentList.Add($DestinationPath)

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $process.WaitForExit(30000) | Out-Null
    if (-not $process.HasExited) {
        $process.Kill($true)
        throw "Le subprocess CrashTestHost ne s'est pas terminé en 30 secondes ($Boundary)."
    }

    return $process
}

# --- 1. Volumes -------------------------------------------------------------
$volumes = Find-UsableVolumes -ExplicitA $VolumeA -ExplicitB $VolumeB
$volumeA = $volumes[0]
$volumeB = $volumes[1]

if (-not $volumeA -or -not $volumeB) {
    Write-Host "ERREUR : deux volumes fixes ou amovibles sont requis pour ce protocole." -ForegroundColor Red
    Write-Host "Volumes détectés :"
    Get-Volume -ErrorAction SilentlyContinue | Where-Object { $_.DriveLetter } | ForEach-Object {
        Write-Host "  $($_.DriveLetter): $($_.DriveType) $($_.FileSystemLabel)"
    }
    Write-Host ""
    Write-Host "Solutions :"
    Write-Host "  1. Brancher un second disque ou une clé USB, puis relancer avec :"
    Write-Host "     powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\run-intervolume-real.ps1 -VolumeA C -VolumeB E"
    Write-Host "  2. Créer un disque virtuel monté (PowerShell administrateur) :"
    Write-Host "     New-VHD -Path C:\idm-test\volB.vhdx -SizeBytes 1GB -Fixed | Mount-VHD"
    Write-Host "     $d = Get-Disk | Where-Object Location -like '*vhdx*'; Initialize-Disk -Number `$d.Number"
    Write-Host "     New-Partition -DiskNumber `$d.Number -AssignDriveLetter -UseMaximumSize | Format-Volume -FileSystem NTFS"
    exit 1
}

Write-Step "==> Volumes retenus : $($volumeA.DriveLetter): (source) et $($volumeB.DriveLetter): (destination)"
$rootA = Join-Path "$($volumeA.DriveLetter):\" 'idm-intervolume-test'
$rootB = Join-Path "$($volumeB.DriveLetter):\" 'idm-intervolume-test'
New-Item -ItemType Directory -Force $rootA, $rootB | Out-Null

# --- 2. Harnais --------------------------------------------------------------
$hostAssembly = Test-CrashHostReady
$hostDirectory = Split-Path -Parent $hostAssembly
Add-Type -Path (Join-Path $hostDirectory 'Microsoft.Data.Sqlite.dll')

# --- 3. Scénarios -------------------------------------------------------------
$expectedContent = 'hello'
$boundaries = @('AfterInterVolumeStagingFlushed', 'AfterInterVolumeDestinationMoved')
$passed = 0

foreach ($boundary in $boundaries) {
    $taskId = [Guid]::NewGuid()
    $databasePath = Join-Path $rootA "db-$boundary.sqlite3"
    $temporaryPath = Join-Path $rootA "temp-$boundary.download"
    $destinationPath = Join-Path $rootB "fixture-$boundary.bin"

    Remove-Item -Path $databasePath, $temporaryPath, $destinationPath -Force -ErrorAction SilentlyContinue
    Write-Step "==> Scénario $boundary (taskId=$taskId)"

    $process = Invoke-InterVolumeScenario `
        -Boundary $boundary `
        -TaskId $taskId `
        -DatabasePath $databasePath `
        -TemporaryPath $temporaryPath `
        -DestinationPath $destinationPath `
        -HostAssembly $hostAssembly

    # Le subprocess doit être mort par terminaison abrupte (exit code non nul).
    $crashed = -not $process.HasExited -or $process.ExitCode -ne 0
    if (-not $crashed) {
        Write-Step "  ECHEC : le subprocess s'est terminé normalement (exit $($process.ExitCode))."
        continue
    }

    $sourceAbsent = -not (Test-Path -LiteralPath $temporaryPath)
    $destinationPresent = Test-Path -LiteralPath $destinationPath
    $destinationMatches = $false
    if ($destinationPresent) {
        $destinationMatches = ([System.IO.File]::ReadAllText($destinationPath)) -eq $expectedContent
    }

    $dbFinalizing = $false
    if (Test-Path -LiteralPath $databasePath) {
        $conn = [Microsoft.Data.Sqlite.SqliteConnection]::new("Data Source=$databasePath;Mode=ReadOnly")
        try {
            $conn.Open()
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = 'SELECT state FROM downloads WHERE id = $id'
            $cmd.Parameters.AddWithValue('$id', $taskId.ToString('D')) | Out-Null
            $state = $cmd.ExecuteScalar()
            $dbFinalizing = ($state -is [long]) -and ($state -eq 13) # DownloadState.Finalizing
        }
        finally {
            $conn.Dispose()
        }
    }

    if ($sourceAbsent -and $destinationPresent -and $destinationMatches -and $dbFinalizing) {
        $passed++
        Write-Step "  OK : source absente, destination '$expectedContent', état Finalizing en SQLite."
    }
    else {
        Write-Step "  ECHEC : sourceAbsent=$sourceAbsent destinationPresent=$destinationPresent destinationMatches=$destinationMatches dbFinalizing=$dbFinalizing"
    }
}

# --- 4. Rapport -------------------------------------------------------------
Write-Step ""
Write-Step "==> Résultat : $passed/$($boundaries.Count) scénarios inter-volume réels validés."
Write-Step "La réparation Finalizing->Completed doit être déclenchée par le futur hôte ;"
Write-Step "elle est couverte par DownloadFinalizationCoordinator.RepairAsync (tests d'intégration)."
New-Item -ItemType Directory -Force (Split-Path $reportPath) | Out-Null
$reportLines | Out-File -FilePath $reportPath -Encoding utf8
Write-Step "Rapport écrit : $reportPath"

if ($passed -ne $boundaries.Count) {
    exit 1
}
