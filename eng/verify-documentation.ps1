[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$errors = [System.Collections.Generic.List[string]]::new()

$requiredDocuments = @(
    'Cahier_des_charges.md',
    'FEUILLE_DE_ROUTE.md',
    'SUIVI_DEVELOPPEMENT.md',
    'ARCHITECTURE_TECHNIQUE.md',
    'REGISTRE_DES_RISQUES.md',
    'PROTOCOLE_TEST_REPRISE.md',
    'ETAT_ACTUEL_PROJET.md',
    'DECISIONS_ARCHITECTURE.md',
    'REGLES_DE_CODAGE.md',
    'DEPENDANCES.md',
    'MODELISATION_DONNEES.md',
    'SECURITE.md',
    'PERFORMANCES.md',
    'FAQ_TECHNIQUE.md',
    'ERREURS_CONNNUES.md',
    'INSTRUCTIONS_IA.md'
)

foreach ($document in $requiredDocuments) {
    $path = Join-Path $projectRoot $document
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Document absent: $document")
        continue
    }

    if ((Get-Item -LiteralPath $path).Length -eq 0) {
        $errors.Add("Document vide: $document")
    }
}

function Get-DefinitionIds {
    param(
        [Parameter(Mandatory)] [string] $Text,
        [Parameter(Mandatory)] [string] $Pattern
    )

    return [regex]::Matches($Text, $Pattern) | ForEach-Object { $_.Groups[1].Value }
}

function Test-UniqueDefinitions {
    param(
        [Parameter(Mandatory)] [string] $Label,
        [Parameter(Mandatory)] [string[]] $Ids
    )

    $duplicates = $Ids | Group-Object | Where-Object Count -gt 1
    foreach ($duplicate in $duplicates) {
        $errors.Add("Définition $Label dupliquée: $($duplicate.Name)")
    }
}

$requirementsText = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'Cahier_des_charges.md')
$roadmapText = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'FEUILLE_DE_ROUTE.md')
$riskText = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'REGISTRE_DES_RISQUES.md')
$protocolText = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'PROTOCOLE_TEST_REPRISE.md')
$adrText = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'DECISIONS_ARCHITECTURE.md')
$stateText = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'ETAT_ACTUEL_PROJET.md')

$requirementIds = @(
    Get-DefinitionIds $requirementsText '(?m)^\| (F-\d{3}|NF-\d{3}) \|'
)
Test-UniqueDefinitions 'exigence' $requirementIds
if ($requirementIds.Count -ne 36) {
    $errors.Add("Nombre d'exigences attendu 36, observé $($requirementIds.Count)")
}

foreach ($requirementId in $requirementIds) {
    if ($roadmapText -notmatch "(?m)^\| $([regex]::Escape($requirementId)) \|") {
        $errors.Add("Exigence absente de la matrice de traçabilité: $requirementId")
    }
}

$leafTaskIds = @(
    Get-DefinitionIds $roadmapText '(?m)^\| ((?:D|M|W|B|Q)-\d{3}) \|'
)
Test-UniqueDefinitions 'tâche exécutable' $leafTaskIds
if ($leafTaskIds.Count -ne 35) {
    $errors.Add("Nombre de tâches exécutables attendu 35, observé $($leafTaskIds.Count)")
}

$riskIds = @(
    Get-DefinitionIds $riskText '(?m)^\| (R-\d{3}) \|'
)
Test-UniqueDefinitions 'risque' $riskIds

$protocolIds = @(
    [regex]::Matches($protocolText, '(?m)^(?:## (PR-\d{3})|\| (PR-\d{3}) \|)') |
        ForEach-Object { if ($_.Groups[1].Value) { $_.Groups[1].Value } else { $_.Groups[2].Value } }
)
Test-UniqueDefinitions 'test de reprise' $protocolIds

$adrIds = @(
    [regex]::Matches($adrText, '(?m)^(?:## (ADR-\d{3})|\| (ADR-\d{3}) \|)') |
        ForEach-Object { if ($_.Groups[1].Value) { $_.Groups[1].Value } else { $_.Groups[2].Value } }
)
Test-UniqueDefinitions 'ADR' $adrIds

$statusCounts = @{}
foreach ($line in ($roadmapText -split "`r?`n")) {
    if ($line -notmatch '^\| (?:D|M|W|B|Q)-\d{3} \|') {
        continue
    }

    $columns = $line.Split('|') | ForEach-Object Trim
    $status = $columns[4]
    if (-not $statusCounts.ContainsKey($status)) {
        $statusCounts[$status] = 0
    }
    $statusCounts[$status]++
}

foreach ($status in @('À FAIRE', 'EN COURS', 'PARTIEL', 'À VÉRIFIER', 'TERMINÉ')) {
    $expected = if ($statusCounts.ContainsKey($status)) { $statusCounts[$status] } else { 0 }
    $match = [regex]::Match($stateText, "(?m)^\| $([regex]::Escape($status)) \| (\d+) \|")
    if (-not $match.Success -or [int]$match.Groups[1].Value -ne $expected) {
        $observed = if ($match.Success) { $match.Groups[1].Value } else { 'absent' }
        $errors.Add("Compte $status incohérent: feuille=$expected, état=$observed")
    }
}

foreach ($document in $requiredDocuments) {
    $text = Get-Content -Raw -LiteralPath (Join-Path $projectRoot $document)
    foreach ($reference in [regex]::Matches($text, '`([^`]+\.md)`')) {
        $target = $reference.Groups[1].Value
        if (-not (Test-Path -LiteralPath (Join-Path $projectRoot $target) -PathType Leaf)) {
            $errors.Add("Référence Markdown absente dans ${document}: $target")
        }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Documentation: 16/16 documents présents et non vides."
Write-Output "Traçabilité: 36/36 exigences présentes dans la matrice."
Write-Output "Pilotage: 35 tâches exécutables, comptes de statuts cohérents."
Write-Output "Identifiants: définitions exigences/tâches/risques/tests/ADR sans doublon."
Write-Output "Références: liens Markdown locaux vérifiés."
