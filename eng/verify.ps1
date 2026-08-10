[CmdletBinding()]
param(
    [switch] $RefreshPackages,
    [switch] $AuditPackages
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
$solution = Join-Path $projectRoot 'WindowsDownloadManager.slnx'
$nugetConfig = Join-Path $projectRoot 'NuGet.Config'

if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf)) {
    throw "SDK .NET local absent : $dotnet"
}

$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.dotnet-cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:TESTINGPLATFORM_TELEMETRY_OPTOUT = '1'
$env:APPDATA = Join-Path $projectRoot '.build-appdata'
New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME, $env:APPDATA | Out-Null

function Invoke-Checked {
    param(
        [Parameter(Mandatory)] [string] $Label,
        [Parameter(Mandatory)] [scriptblock] $Action
    )

    Write-Host "==> $Label"
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$Label a échoué avec le code $LASTEXITCODE."
    }
}

Push-Location $projectRoot
try {
    if ($RefreshPackages) {
        Invoke-Checked 'Restauration et actualisation des verrous NuGet' {
            & $dotnet restore $solution --configfile $nugetConfig --force-evaluate
        }
    }
    else {
        Invoke-Checked 'Restauration reproductible hors ligne' {
            & $dotnet restore $solution --configfile $nugetConfig --locked-mode -p:NuGetAudit=false
        }
    }

    Invoke-Checked 'Compilation Release' {
        & $dotnet build $solution -c Release --no-restore
    }
    Invoke-Checked 'Tests .NET' {
        & $dotnet test $solution -c Release --no-build --no-restore
    }
    Invoke-Checked 'Formatage .NET' {
        & $dotnet format $solution --verify-no-changes --no-restore --verbosity minimal
    }
    Invoke-Checked 'Contrôle documentaire' {
        & (Join-Path $PSScriptRoot 'verify-documentation.ps1')
    }

    if ($AuditPackages) {
        Invoke-Checked 'Audit NuGet en ligne' {
            & $dotnet package list --project $solution --vulnerable --include-transitive --configfile $nugetConfig --no-restore
        }
    }
}
finally {
    Pop-Location
}
