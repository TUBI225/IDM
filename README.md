# IDM Engine — socle C# et prototype de référence

> Le produit cible est désormais développé en C#/.NET 10. Le prototype Python décrit plus bas est
> conservé temporairement comme référence de comportement et de tests.

## Socle .NET 10

Le SDK est installé localement dans `.tools/dotnet`. Compilation reproductible :

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet-cli"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:TESTINGPLATFORM_TELEMETRY_OPTOUT = "1"
$env:APPDATA = "$PWD\.build-appdata"
New-Item -ItemType Directory -Force "$env:APPDATA\NuGet" | Out-Null
.\.tools\dotnet\dotnet.exe restore .\WindowsDownloadManager.slnx --configfile .\NuGet.Config --locked-mode -p:NuGetAudit=false
.\.tools\dotnet\dotnet.exe build .\WindowsDownloadManager.slnx -c Release --no-restore
.\.tools\dotnet\dotnet.exe test .\WindowsDownloadManager.slnx -c Release --no-build --no-restore
```

Le moteur C# sonde `bytes=0-0`, lie chaque nouvelle connexion directe à l’IP filtrée, extrait les
métadonnées sans charger le corps et valide strictement `206`. Il possède un writer temporaire
durable, un dépôt SQLite v3 et un orchestrateur headless. Une tâche interrompue compatible peut être
réconciliée, contrôlée par recouvrement, reprise depuis son checkpoint confirmé puis finalisée par
intention persistée. La finalisation renomme atomiquement sur le même volume ou copie vers un transit
du volume cible, le synchronise, vérifie son SHA-256 puis effectue un renommage local.

Ce dépôt contient le socle déterministe d’un gestionnaire de téléchargements HTTP/HTTPS fiable.
Il vise la première preuve du cahier des charges : reprendre après un arrêt de l’application sans
recommencer un fichier compatible avec les plages HTTP et sans mélanger deux versions distantes.

## Capacités du prototype Python — référence temporaire

- analyse réelle de `Range` avec une requête `bytes=0-0` ;
- redirections HTTP suivies et URL finale mémorisée ;
- blocage par défaut des adresses privées/locales (protection SSRF) ;
- progression confirmée après `flush` et synchronisation disque, puis enregistrée dans SQLite ;
- reprise avec une zone de recouvrement de 64 Kio comparée octet par octet ;
- identité distante vérifiée par taille, `ETag` et `Last-Modified` ;
- temporisation progressive en cas d’erreur temporaire ;
- vérification de taille et lecture SHA-256 avant renommage atomique ;
- pause sûre avec `Ctrl+C` et reprise lors d’une exécution ultérieure.

Cette version utilise une connexion unique. La segmentation multiple, l’ordonnanceur global,
l’interface Windows et l’extension de navigateur appartiennent aux jalons suivants.

Ces capacités ne sont pas encore celles du moteur C#. Python est gelé comme référence de parité et
utilise ses propres données. Le C# ne doit jamais ouvrir silencieusement sa base ou ses temporaires.

## Capacités C# réellement présentes

- domaine et machine d’états initiaux ;
- ports Application ;
- analyse HTTP streaming et métadonnées ;
- redirections manuelles, validation URI/DNS préalable et classification 416/429/5xx ;
- transfert neuf et reprise à connexion unique avec ordre `flush disque → checkpoint SQLite` ;
- finalisation sans écrasement et réparation de l’état `Finalizing` ;
- collisions explicites : refus par défaut ou conservation sous `nom (n).ext` ;
- SHA-256 streaming persisté avant `Finalizing` et revérifié pendant toute réparation ;
- empreinte distante SHA-256 extraite des en-têtes HTTP (`Content-Digest`, `Digest`, `x-checksum-sha256`,
  `x-goog-hash`, `x-amz-checksum-sha256`), persistée dans une colonne dédiée et vérifiée à la finalisation
  (mode strict par défaut, forçage explicite possible) ;
- copie inter-volume vérifiée et réparable via un fichier de transit réservé ;
- segmentation multiple statique : planneur de plages disjointes/couvrantes, transfert segmenté
  parallèle (une connexion par segment) et repli connexion unique ;
- 225 tests .NET de domaine, application, réseau, stockage, persistance et intégration.

Restent notamment à intégrer les essais sur
deux volumes physiques, le reboot Windows, la reprise segmentée, l’interface et l’installateur.

## Exécution

Avec Python 3.11 ou plus récent, depuis la racine du projet :

```powershell
python -m idm add "https://example.com/large-file.iso" --output "C:\Downloads"
python -m idm run 1
python -m idm list
```

Interrompre `run` avec `Ctrl+C` place la tâche en `EN_PAUSE` après synchronisation des octets
reçus. Relancer `python -m idm run 1` pour reprendre.

Les données de contrôle se trouvent par défaut dans `.idm-data/downloads.sqlite3`. Pour les
tests avec un serveur local, ajouter `--allow-private` avant la sous-commande.

## Tests

```powershell
python -m unittest discover -v
```

Les tests lancent un serveur HTTP local et couvrent le téléchargement complet, la reprise après
redémarrage simulé et le refus sûr d’un fichier distant modifié.

Contrôle documentaire G0 :

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify-documentation.ps1
```

Ce contrôle vérifie les 16 fichiers permanents, la matrice des 36 exigences, les 35 tâches
exécutables, les comptes de statuts, les définitions d’ID et les références Markdown locales.

Vérification complète reproductible :

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1
```

Lors d’une mise à jour volontaire des paquets, utiliser `-RefreshPackages`, relire les fichiers de
verrou puis exécuter `-AuditPackages` avec le réseau. Le script désactive la télémétrie .NET/test.
