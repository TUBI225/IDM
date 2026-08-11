# Dépendances

Version documentaire : 2.3  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : INVENTAIRE ACTIF — C# CIBLE, PYTHON RÉFÉRENCE  
Responsable logique : Architecture et sécurité  
Documents liés : `DECISIONS_ARCHITECTURE.md`, `SECURITE.md`, `ARCHITECTURE_TECHNIQUE.md`

## Sommaire

1. Dépendances observées
2. Candidats Windows
3. Critères d’adoption
4. Mise à jour et retrait

Dernière vérification : 2026-08-03

## Runtime

| Nom | Version vérifiée | Rôle | Source/licence | Obligatoire | Installation/distribution |
|---|---|---|---|---|---|
| .NET SDK/runtime | SDK 10.0.302, runtime 10.0.10 | Compiler/exécuter le produit C# | Microsoft, licences à consigner avant distribution | Oui pour la cible | SDK local `.tools`; publication utilisateur à décider |
| Python | 3.12 (runtime Codex observé) | Exécuter le prototype et ses fixtures | python.org, PSF License | Non pour le produit | Référence de développement uniquement |

Compatibilité déclarée par `pyproject.toml` : Python >= 3.11. Seule la version 3.12 fournie dans
l’environnement de développement a été exécutée.

## Bibliothèques

Le prototype Python utilise uniquement la bibliothèque standard. Le produit C# utilise la BCL .NET.
Les six projets de test utilisent `MSTest.Sdk` 4.3.2. `Microsoft.Data.Sqlite` 10.0.10 est installé
dans Persistence. `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 est épinglé explicitement pour éviter la
version transitive 2.1.11 signalée vulnérable.

## Outils observés

- Git fourni par le runtime Codex ; dépôt `main` initialisé, sans commit faute d’identité configurée.
- `unittest`, `compileall`, MSTest.Sdk 4.3.2 et Microsoft Testing Platform.

## Risques et mise à jour

- La méthode d’installation Windows destinée aux utilisateurs reste inconnue.
- La licence des bibliothèques standard suit la distribution Python.
- Toute dépendance future exige vérification de maintenance, licence, sécurité, compatibilité et
  solution sans abonnement avant ajout.

## 2. Candidats Windows à étudier — aucune adoption implicite

| Candidat | Fonction | Licence/source | Statut | Risques/alternative |
|---|---|---|---|---|
| .NET 10 LTS | Runtime cible | Microsoft, licence à vérifier avant distribution | RETENU | poids/runtime ; publication autonome ou dépendante |
| WPF | UI Windows mature | Composant .NET | ALTERNATIVE NON RETENUE | plus mature ; révision si POC WinUI échoue |
| WinUI 3 | UI Windows moderne | Windows App SDK | RETENU, VERSION À ÉPINGLER | packaging/maturité ; WPF |
| `HttpClient` | HTTP/TLS/proxy | .NET BCL | RETENU | cycle de vie/DNS/proxy à fixer par ADR-026 ; libcurl |
| SQLite | Persistance locale | Domaine public | RETENU | migrations/concurrence/crash à tester |
| `Microsoft.Data.Sqlite` | Binding ADO.NET direct | Microsoft, MIT | INSTALLÉ 10.0.10 | assets natifs/packaging ; EF Core non retenu |
| `SQLitePCLRaw.bundle_e_sqlite3` | SQLite natif embarqué | SourceGear, Apache-2.0 | INSTALLÉ 2.1.12 | CVE native, architectures et packaging |
| libcurl | Moteur HTTP alternatif | curl license | FACULTATIF | DLL/CVE/packaging ; `HttpClient` |
| Logging structuré | Diagnostics | À sélectionner | NON CHOISI | fuite/volume ; abstraction interne |
| `MSTest.Sdk` | Tests | Microsoft, MIT | RETENU 4.3.2 | dépendances transitives ; xUnit/NUnit non retenus |
| Analyseurs .NET | Qualité | Microsoft/communauté | PROPOSÉ | configuration et faux positifs |
| MSIX/MSI/autre | Installation | À comparer | NON CHOISI | signature, rollback, extensions |

Chaque fiche finale devra ajouter version épinglée, URL officielle, licence vérifiée, taille,
maintenance, CVE, installation, mise à jour/retrait, compatibilité hors ligne et propriétaire.

## Décisions de plateforme du 2026-08-03

- .NET 10 LTS : retenu ; SDK local 10.0.302 et runtime 10.0.10 vérifiés. Support Microsoft annoncé
  jusqu’au 14 novembre 2028. Installation confinée à `.tools/dotnet`, exclue de Git.
- WinUI 3/Windows App SDK : retenu pour l’App uniquement ; version exacte après restauration NuGet
  et POC. Le moteur ne dépend pas de ce paquet.
- `HttpClient`/`SocketsHttpHandler` : retenu avant toute évaluation de libcurl.
- SQLite : `Microsoft.Data.Sqlite` 10.0.10 installé en accès direct. La restauration initiale a
  détecté GHSA-2m69-gcr7-jv3q dans SQLitePCLRaw 2.1.11 ; l’override 2.1.12 a supprimé l’alerte.
- Tests : `MSTest.Sdk` 4.3.2 et Microsoft Testing Platform ; 93 scénarios distincts réussis après les
  réconciliations, la décision combinée et le recouvrement borné, sans nouvelle bibliothèque.

Sources officielles consultées : politique de support .NET, documentation Microsoft WPF/WinUI 3 et
recommandations `HttpClient`. Vérification effectuée le 2026-08-03.

## Politique NuGet décidée en G1

- Source unique autorisée : `https://api.nuget.org/v3/index.json` dans `NuGet.Config`.
- Cache isolé : `.packages/`, exclu de Git ; verrous `packages.lock.json` conservés dans Git.
- Restauration courante : `--locked-mode -p:NuGetAudit=false`, utilisable avec le cache existant.
- Actualisation intentionnelle : `eng/verify.ps1 -RefreshPackages`, suivie d’une revue des verrous.
- Audit connecté : `eng/verify.ps1 -AuditPackages`, transitifs inclus. Le 2026-08-03, aucune
  vulnérabilité n’a été signalée. Une restauration hors ligne n’équivaut jamais à cet audit.
- Télémétrie : `DOTNET_CLI_TELEMETRY_OPTOUT=1` et `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` dans le script.

Audit connecté G2 du 2026-08-03 : aucun paquet vulnérable signalé après
remplacement de SQLitePCLRaw 2.1.11 par 2.1.12. Les fichiers de verrou existent pour chaque projet.

Les projets Application.Tests et Integration.Tests ajoutés pour l’orchestrateur réutilisent
uniquement MSTest et les références de projet existantes : aucune nouvelle dépendance runtime ou
version de paquet n’a été introduite.

Les migrations SQLite v2/v3, `RemoteIdentity` et SHA-256 utilisent uniquement `Microsoft.Data.Sqlite` et la BCL
déjà verrouillés. Aucun paquet, aucune version et aucune licence n’ont changé le 2026-08-04.

`RemoteIdentityReconciler` réutilise le port d’analyse et la BCL existants. Aucun paquet, aucune
version, aucun fichier de verrou et aucune licence n’ont changé pour cette tranche.

`RecoveryDecisionEvaluator` est une fonction pure de la couche Application et réutilise uniquement
les types existants. Aucun paquet, aucune version, aucun verrou et aucune licence n’ont changé.

Les lecteurs de plages et `RecoveryOverlapVerifier` utilisent uniquement les flux, mémoires et API
HTTP de la BCL déjà retenue. Aucun paquet, version, verrou ou licence n’a changé.

`StartupRecoveryCoordinator` compose uniquement les services Application existants. Les tests
ajoutés réutilisent MSTest et les références de projet verrouillées ; aucune dépendance, version,
licence ou abonnement n’a changé.

Le banc de durabilité réutilise MSTest, `DurableTemporaryFileWriter` et `SqliteDownloadRepository`.
Aucun paquet, outil de chaos, version, verrou ou licence supplémentaire n’est introduit.

`WindowsDownloadManager.CrashTestHost` est un projet exécutable .NET interne aux tests. Il possède un
verrou généré à partir des dépendances projet déjà approuvées ; aucune version ou bibliothèque
nouvelle n’est ajoutée. `System.Diagnostics.Process` appartient à la BCL.

Aucun abonnement payant n’est nécessaire. Toute nouvelle dépendance exige licence, maintenance,
compatibilité, audit, verrou et justification avant fusion.

`System.Security.Cryptography.SHA256` appartient à la BCL .NET 10. Son utilisation streaming et
`CryptographicOperations.FixedTimeEquals` n’ajoutent aucun paquet, verrou ou licence.
