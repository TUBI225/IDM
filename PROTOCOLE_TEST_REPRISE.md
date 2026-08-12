# Protocole de test de reprise

Version documentaire : 2.3  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : ACTIF — COUVERTURE INITIALE PARTIELLE  
Responsable logique : Responsable qualité et récupération  
Documents liés : `Cahier_des_charges.md`, `REGISTRE_DES_RISQUES.md`, `ETAT_ACTUEL_PROJET.md`

## Sommaire

1. Règles de preuve
2. Tests exécutés
3. Catalogue non exécuté
4. Matrice de couverture
5. Modèle de compte rendu

Périmètres de preuve : `PYTHON-PROTOTYPE` et `CSHARP-CIBLE`. Aucun résultat d’une pile ne valide
l’autre. Version produit déclarée : 0.1.0 expérimentale.

Dernière exécution Python observée : 3 tests réussis. Dernière preuve C# : 93 tests réussis couvrant
Domain, Application, Network, Storage, Persistence et intégration. La persistance
des préconditions de reprise est testée, mais **aucune reprise C# de bout en bout** n’est revendiquée.

## PR-001 — Téléchargement complet et finalisation

- Objectif : vérifier réception, SHA-256 implicite du contenu attendu et renommage final.
- Conditions : serveur local `Range`, fichier synthétique de 16 Mio.
- Étapes : ajouter la tâche, exécuter le moteur, comparer SHA-256 source/destination.
- Attendu : état `TERMINE`, fichier temporaire absent, empreintes identiques.
- Obtenu : conforme.
- Preuve : `test_full_download_is_verified_and_atomically_finalized`.
- Date : 2026-08-03.
- Statut : RÉUSSI.

## PR-002 — Reprise simulée après redémarrage

- Objectif : reconstruire un moteur/dépôt et reprendre après 5 Mio confirmés.
- Conditions : fichier temporaire partiel et progression SQLite cohérente.
- Étapes : créer les 5 premiers Mio, recréer `DownloadEngine`, exécuter, comparer tout le contenu.
- Attendu : recouvrement vérifié, état `TERMINE`, contenu identique.
- Obtenu : conforme.
- Preuve : `test_restart_resumes_from_confirmed_disk_position`.
- Date : 2026-08-03.
- Statut : RÉUSSI (simulation, pas redémarrage Windows réel).

## PR-003 — Fichier distant modifié

- Objectif : empêcher le mélange après changement d’ETag.
- Conditions : 1 Mio temporaire, ETag initial puis nouvel ETag.
- Étapes : recréer l’analyse distante et lancer la reprise.
- Attendu : état `FICHIER_DISTANT_MODIFIE`, taille temporaire inchangée.
- Obtenu : conforme.
- Preuve : `test_changed_remote_file_is_never_mixed`.
- Date : 2026-08-03.
- Statut : RÉUSSI.

## PR-004 — Pause réelle par Ctrl+C

- Objectif : vérifier arrêt pendant écriture et reprise ultérieure.
- Préparation/étapes : utiliser un serveur lent, envoyer Ctrl+C pendant le flux, relancer.
- Attendu : `EN_PAUSE`, octets synchronisés, fichier final identique.
- Obtenu : test non exécuté. Résultat inconnu.
- Statut : NON EXÉCUTÉ.

## PR-005 — Crash brutal et redémarrage Windows

- Objectif : prouver la récupération avec base potentiellement en retard.
- Obtenu : test non exécuté. Résultat inconnu.
- Statut : NON EXÉCUTÉ.

## PR-006 — Coupure réseau, disque plein et plage serveur erronée

- Objectif : couvrir trois causes critiques d’interruption/incohérence.
- Obtenu : tests non exécutés. Résultats inconnus.
- Statut : NON EXÉCUTÉ.

## 1. Règles de preuve

Chaque cas utilise un fichier déterministe et conserve hash attendu/obtenu, offsets de coupure,
extraits de logs expurgés, base avant/après et version. Un test réussi exige résultat final et
invariants internes, pas seulement l’UI. Refaire au minimum aux positions 1, 25, 50, 90 et 99 %.
État initial de tous les cas ci-dessous : **NON EXÉCUTÉ — résultat inconnu**.

## 3. Catalogue détaillé des scénarios à exécuter

| ID | Injection / préconditions | Résultat attendu | Risque lié |
|---|---|---|---|
| PR-010 | Wi-Fi coupé 5 s | checkpoint, reconnexion, hash identique | R-002 |
| PR-011 | Wi-Fi coupé 10 min | attente bornée, reprise manuelle/auto | R-008 |
| PR-012 | Câble retiré | erreur temporaire sans perte | R-002 |
| PR-013 | Changement Wi-Fi→Ethernet | nouvelle connexion, identité revalidée | R-001 |
| PR-014 | Mode avion/retour | aucune boucle agressive | R-008 |
| PR-015 | Veille/sortie de veille | délais expirés classés, reprise sûre | R-002 |
| PR-016 | Batterie/arrêt brutal simulé | base jamais en avance | R-011 |
| PR-020 | Serveur lent puis bloqué | connexion bloquée seule recréée | R-015 |
| PR-021 | HTTP 500/502/503/504 | backoff et limite d’essais | R-008 |
| PR-022 | HTTP 429 + Retry-After date/délai | attente respectée | R-015 |
| PR-023 | Serveur indisponible puis retour | partiel conservé, reprise | R-008 |
| PR-024 | Taille absente/chunked | mode simple sûr | R-003 |
| PR-025 | Range demandé, réponse 200 | aucun corps écrit à l’offset | R-003 |
| PR-026 | 206 sans/mauvais Content-Range | serveur non fiable, arrêt/fallback | R-003 |
| PR-027 | Plage décalée ou corps trop long | divergence détectée | R-003 |
| PR-028 | Taille totale change | état distant modifié | R-001 |
| PR-030 | Fermeture normale | pause synchronisée | R-002 |
| PR-031 | Fin via Gestionnaire des tâches | récupération conservative | R-011 |
| PR-032 | Crash avant/après fsync/SQLite | minimum sûr et aucune corruption | R-002/R-011 |
| PR-033 | Redémarrage Windows | reprise après ouverture | R-002 |
| PR-034 | Interruption pendant finalisation | état réparable, un seul fichier valide | R-011 |
| PR-035 | Dix interruptions répétées | hash identique, pas de fuite handles | R-002 |
| PR-040 | Disque plein avant/après checkpoint | pause et base non avancée | R-006 |
| PR-041 | Disque USB retiré | état préservé, aucune boucle | R-012 |
| PR-042 | Dossier supprimé/permission retirée | destination inaccessible claire | R-012 |
| PR-043 | Fichier final apparaît avant rename | collision sans écrasement | R-021 |
| PR-050 | URL signée expirée | LIEN_EXPIRE, partiel conservé | R-005 |
| PR-051 | Nouveau lien même fichier | identité confirmée puis reprise | R-001 |
| PR-052 | Nouveau lien autre fichier même nom | reprise refusée | R-001 |
| PR-053 | Cookie/auth expiré | état dédié, aucun secret loggé | R-016 |
| PR-060 | Retransmission identique | ignorer jusqu’au manque puis finaliser | R-001 |
| PR-061 | Différence à 64 Kio/50 %/près reprise | arrêt immédiat, ancien intact | R-001 |
| PR-062 | Coût retransmission élevé | consentement/message exact | LIM-002 |
| PR-070 | Plusieurs segments coupés | carte cohérente après reprise | R-013 |
| PR-071 | Segment lent divisé | aucune plage dupliquée/manquante | R-013 |
| PR-072 | Deux écritures visant même plage | invariant/verrou bloque conflit | R-013 |

## 4. Matrice d’environnements

Exécuter sur versions Windows supportées, compte standard, SSD/HDD/USB, NTFS et système amovible
retenu, IPv4/IPv6, proxy si supporté, antivirus actif, et builds Debug/Release. Les scénarios critiques
PR-025/026/032/040/052/061/070 bloquent une version stable en cas d’échec.

## 5. Modèle de compte rendu

Pour chaque ID : objectif, priorité, préconditions, environnement exact, fichier et hash attendu,
préparation, étapes numérotées, résultat attendu, obtenu, preuves/logs, hash obtenu, statut, anomalies,
conclusion, version/date/testeur. Ne jamais recopier « RÉUSSI » d’une autre version.

## Exécution C# HTTP — 2026-08-03

- Environnement : Windows, .NET SDK 10.0.302, build Release.
- Résultat global : 7 tests, 7 réussis, 0 échec, 0 ignoré.
- PR-024 partiel : réponse `200` reconnue sans support Range — RÉUSSI.
- PR-025 : capacité désactivée, mais aucune écriture C# n’existe encore — PARTIEL.
- PR-026 partiel : `206 Content-Range bytes 1-1/1000` rejeté — RÉUSSI.
- `206` valide : taille, nom et demande exacte `bytes=0-0` vérifiés — RÉUSSI.
- PR-027, redirections, 429/5xx, annulation et SSRF : NON EXÉCUTÉS, résultats inconnus.

Preuves : `HttpRemoteResourceAnalyzer.cs`, `LoopbackHttpServer.cs` et test `Program.cs`.

## Exécution C# sécurité et erreurs HTTP — 2026-08-03

- Build Release : RÉUSSI, 0 avertissement, 0 erreur, 9,51 s.
- Tests : 13 exécutés, 13 réussis, 0 échec, 0 ignoré.
- Redirection manuelle vers un second serveur : RÉUSSI. Revalidation sécurisée de chaque saut :
  PARTIEL, car le scénario utilise `AllowAllUriSafetyValidator` et n’observe pas les deux appels.
- Loopback `127.0.0.1` et documentation `192.0.2.1` rejetés par la politique publique : RÉUSSI.
- `416 Content-Range: bytes */0` reconnu comme ressource vide : RÉUSSI.
- PR-022 partiel : `429 Retry-After: 30` classé temporaire et délai conservé : RÉUSSI.
- PR-021 partiel : `503` classé temporaire : RÉUSSI.
- Annulation pendant attente réseau : `OperationCanceledException` propagée — RÉUSSI.
- Rebinding DNS, proxy, chaîne >10, 500/502/504 séparés, corps court/long : NON EXÉCUTÉS.

Commandes de la preuve C# : restauration locale, `dotnet build` Release puis `dotnet run` du harnais,
telles que consignées dans `README.md` et `SUIVI_DEVELOPPEMENT.md`. Aucun `dotnet test` n’est
disponible actuellement. Les tests n’ont pas été relancés pendant G0 ; résultat courant non réobservé.

## 6. Règle d’exécutabilité G0

Les lignes PR-010 à PR-072 sont un inventaire, pas encore des fiches exécutables. Avant exécution,
chaque ID doit recevoir conditions initiales, préparation, étapes numérotées, résultat attendu,
commande, version, environnement et emplacement de preuve. PR-006 doit être séparé en scénarios
distincts pour coupure réseau, disque plein et plage erronée. Un état préfabriqué cohérent comme
PR-002 prouve une reprise contrôlée, pas un crash réel.

## 7. Exécution standardisée G1 — 2026-08-03

Cette preuve remplace l’état courant des anciens paragraphes de harnais ci-dessus, conservés comme
historique. Environnement : Windows, .NET SDK 10.0.302, Release, MSTest.Sdk 4.3.2 et Microsoft
Testing Platform. Commande : `dotnet test WindowsDownloadManager.slnx -c Release --no-build
--no-restore`. Résultat : 14 exécutés, 14 réussis, 0 échec, 0 ignoré, durée 14,782 s.

- Domain : 3 tests de création et transitions.
- Network : 11 tests couvrant Range exact, 206 valide, repli 200, 206 malformé, refus d’adresses,
  validations de chaque redirection, cible refusée non contactée, 416 vide, 429, 503 et annulation.
- BUG-001 : CORRIGÉE par les deux tests de redirection observables.
- Rebinding DNS, proxy, IPv6/NAT64 adverses, reprise C# après crash, disque plein et finalisation
  ADR-029 : NON EXÉCUTÉS. Résultat inconnu.

Les 3 tests Python ont aussi réussi (3/3, 0 échec, 0 ignoré, 2,118 s), mais ne prouvent que le
prototype. Aucun test de performance produit n’a été exécuté.

## 8. Exécution G2 réseau, fichier et SQLite — 2026-08-03

- Environnement : Windows, .NET SDK 10.0.302, Release, Microsoft Testing Platform.
- Commande : `dotnet test WindowsDownloadManager.slnx -c Release --no-build --no-restore`.
- Résultat final : 26 exécutés, 26 réussis, 0 échec, 0 ignoré, durée 4,383 s.
- Network, 15 tests : rebinding simulé public→loopback refusé avant connexion, lot public+privé
  refusé, IPv4/IPv6 publics acceptés par la politique et handler sans proxy/redirect automatique.
- Storage, 3 tests : écriture exacte à un offset, frontière retournée après flush, annulation avant
  création et chemin relatif refusé.
- Persistence, 5 tests : migration v1 unique/checksummée, round-trip état/progression, ID absent,
  checksum altéré refusé et chemin relatif refusé. Le secret de query n’apparaît pas dans la base.
- Domain, 3 tests : invariants et transitions existants.

PR-032, PR-034 et PR-040 ne sont pas validés par ces tests isolés. Crash entre flush et SQLite,
disque plein, corruption, migration N-1, finalisation et redémarrage Windows : NON EXÉCUTÉS.
Résultat inconnu. Proxy, DNS public hostile et NAT64 : NON EXÉCUTÉS.

## 9. Exécution G2 — orchestrateur neuf — 2026-08-03

- Environnement final : Windows, .NET SDK 10.0.302, Release, Microsoft Testing Platform.
- Commande canonique : `powershell.exe -NoProfile -ExecutionPolicy Bypass -File eng/verify.ps1`.
- Résultat final : 37 exécutés, 37 réussis, 0 échec, 0 ignoré, durée du runner 24,815 s ;
  compilation 0 avertissement/0 erreur, formatage et contrôle documentaire réussis.
- Application, 4 tests : ordre flush avant checkpoint, échec d’écriture sans progression, flux court
  conservé en état récupérable et fichier vide sans ouverture du corps réseau.
- Network, 18 tests : transfert Range `bytes=offset-`, `If-Match`, `identity`, rejet d’un Range ignoré
  et d’un offset non nul sans capacité Range, en plus des scénarios antérieurs.
- Storage, 6 tests : création exclusive vide, refus d’écrasement, refus d’écriture sans préparation,
  offset/flush, annulation et chemin relatif.
- Intégration, 1 test : sonde HTTP puis flux `hello`, temporaire exact et SQLite restaurée à 5 octets
  en état `VERIFYING`.

Ces tests prouvent l’ordre normal et plusieurs échecs simulés, pas PR-032 au sens crash. Arrêt brutal
du processus entre flush et commit, redémarrage Windows, disque plein réel, corruption SQLite,
réparation ADR-029 et rename final : NON EXÉCUTÉS. Résultat inconnu.

## 10. Exécution G2 — métadonnées de reprise et migration v2 — 2026-08-04

- Baseline Release avant modification : 37 exécutés, 37 réussis, 0 échec, 0 ignoré, 30,726 s.
- Tests ciblés Debug après modification : Domain 5/5, Persistence 7/7, Application 5/5 et
  Integration 1/1 ; 18 scénarios observés, 18 réussis, 0 échec, 0 ignoré.
- Nouveaux scénarios : préparation domaine, refus hors état, migration d’une base v1 sans perte,
  rejet d’identité partielle et absence de création du temporaire si le checkpoint échoue.
- Round-trip : chemin temporaire, URL finale expurgée, taille, ETag, Last-Modified et capacité Range
  sont restaurés après réouverture SQLite ; l’intégration loopback restaure la même identité.
- Migration interrompue par arrêt du processus, backup/rollback, corruption réelle, réconciliation
  de longueur, recouvrement et reprise réseau : NON EXÉCUTÉS. Résultat inconnu.
- Preuve Release canonique : `eng/verify.ps1` via PowerShell avec stratégie limitée au processus —
  42 exécutés, 42 réussis, 0 échec, 0 ignoré, 18,216 s ; build 0 avertissement/0 erreur, formatage et
  contrôle documentaire réussis. Ces résultats ne valident pas PR-032.

## 11. Exécution G2 — classification locale de récupération — 2026-08-04

- Objectif : inspecter sans mutation une tâche restaurée et classer la relation entre
  `confirmed_bytes` et la longueur du temporaire.
- Environnement : Windows, SDK .NET 10.0.302, Release, Microsoft Testing Platform.
- Baseline avant modification : commande `dotnet test WindowsDownloadManager.slnx -c Release
  --no-restore` avec environnement CLI confiné ; 42 exécutés, 42 réussis, 0 échec, 0 ignoré,
  durée runner 7,815 s.
- Première suite après modification : 53 exécutés, 52 réussis, 1 échec, 0 ignoré. L’échec venait de
  la fixture d’intégration qui sautait la transition obligatoire `WAITING`; aucun défaut de production
  observé. La fixture a été corrigée et l’échec est conservé ici comme preuve.
- Non-régression après correction : même commande ; 53 exécutés, 53 réussis, 0 échec, 0 ignoré,
  durée runner 22,709 s.
- Application : métadonnées absentes, temporaire absent, plus court, égal et plus long — RÉUSSI.
- Storage : absence, longueur exacte sans modification, chemin relatif, annulation et verrou exclusif
  non classé absent — RÉUSSI.
- Intégration : tâche SQLite restaurée à 5 octets et fichier long de 7 octets ; résultat `plus long`,
  position sûre 5, fichier et dépôt inchangés — RÉUSSI.
- PR-032 : PARTIEL seulement. Arrêt brutal avant/après fsync/SQLite, troncature, comparaison distante,
  course après inspection, redémarrage Windows et reprise réseau : NON EXÉCUTÉS. Résultat inconnu.
- Preuve canonique finale après documentation : `eng/verify.ps1`, 53 exécutés, 53 réussis, 0 échec,
  0 ignoré, durée runner 16,710 s ; build Release 0 avertissement/0 erreur, formatage et contrôle
  documentaire réussis (16/16 documents, 36/36 exigences, 35 tâches cohérentes).

## 12. Exécution G2 — comparaison distante diagnostique — 2026-08-04

- Objectif : réanalyser l’URL originale, comparer l’identité observée à `RemoteIdentity` et ne
  modifier ni tâche, ni SQLite, ni temporaire.
- Environnement : Windows, SDK .NET 10.0.302, Release, Microsoft Testing Platform.
- Baseline avant modification : 53 exécutés, 53 réussis, 0 échec, 0 ignoré, 14,822 s.
- Première exécution Application : 20 exécutés, 19 réussis, 1 échec, 0 ignoré. Échec de fixture :
  la fabrique remplaçait une date volontairement absente par sa valeur par défaut.
- Après correction de fixture : Application 20/20 et Integration 3/3 réussis.
- Non-régression complète : 64 exécutés, 64 réussis, 0 échec, 0 ignoré, 13,464 s.
- Cas prouvés : métadonnées absentes sans sonde ; ETag fort compatible ; couple taille/date
  compatible ; URL, taille, ETag ou date contradictoires ; preuves disparues ; ETag faible seul
  insuffisant ; perte de Range séparée ; query/fragment expurgés.
- Intégration réseau : une seule requête `Range: bytes=0-0`, aucun appel au port de contenu,
  temporaire inchangé, état et checkpoint inchangés — RÉUSSI.
- PR-052/061 : PARTIEL seulement. Recouvrement binaire, nouveau lien, course entre
  sonde et reprise, mutation et reprise réseau : NON EXÉCUTÉS. Résultat inconnu.
- Preuve canonique finale : `eng/verify.ps1`, build Release 0 avertissement/0 erreur ; 64 exécutés,
  64 réussis, 0 échec, 0 ignoré, 9,801 s ; formatage et contrôle documentaire réussis.

## 13. Exécution G2 — décision combinée de récupération — 2026-08-04

- Objectif : combiner sans I/O les diagnostics local et distant dans une décision unique et conserver
  simultanément tous les motifs bloquants.
- Environnement : Windows, SDK .NET 10.0.302, Release, Microsoft Testing Platform.
- Baseline avant modification : 64 exécutés, 64 réussis, 0 échec, 0 ignoré, 26,600 s.
- Tests Application ciblés : 31 exécutés, 31 réussis, 0 échec, 0 ignoré, 5,073 s ; onze nouveaux cas.
- Non-régression Release : 75 exécutés, 75 réussis, 0 échec, 0 ignoré, 62,925 s.
- Cas prouvés : temporaire exact + distant compatible prêt pour recouvrement ; métadonnées ou fichier
  absents ; fichier plus court ou plus long ; contradiction ; preuve insuffisante ; perte de Range ;
  deux blocages cumulés ; identifiants de téléchargements différents refusés.
- Aucune I/O n’est exécutée par l’évaluateur et les deux résultats sources sont conservés dans le
  résultat immuable. Tests de recouvrement binaire, mutation, reprise HTTP, crash réel et redémarrage
  Windows : NON EXÉCUTÉS. Résultat inconnu.
- Preuve canonique post-documentation : restauration et build réussis avec 0 avertissement/0 erreur,
  75/75 tests réussis en 20,019 s, puis délai global de 300 s dépassé pendant le formatage. Relances
  isolées : formatage RÉUSSI en 148,7 s ; contrôle documentaire RÉUSSI en 38,9 s, 16/16 documents,
  36/36 exigences et 35 tâches cohérents.

## 14. Exécution G2 — recouvrement binaire borné — 2026-08-04

- Objectif : comparer sans mutation une fenêtre locale et distante avant la position sûre, uniquement
  pour `ReadyForOverlapVerification`.
- Environnement : Windows, SDK .NET 10.0.302, Release, Microsoft Testing Platform.
- Baseline : 75 exécutés, 75 réussis, 0 échec, 0 ignoré, 5,560 s.
- Première suite après implémentation : 91 exécutés, 90 réussis, 1 échec, 0 ignoré, 8,913 s. Le corps
  HTTP tronqué était correctement refusé, mais .NET retournait `HttpIOException(ResponseEnded)` au
  lieu du contrat stable `EndOfStreamException` attendu.
- Correction : normalisation limitée à `ResponseEnded`, puis 91/91 réussis en 4,844 s.
- Network ciblé après ajout des redirections : 24/24 réussis en 3,399 s.
- Non-régression finale avant documentation : 93 exécutés, 93 réussis, 0 échec, 0 ignoré, 3,647 s.
- Cas prouvés : décision bloquée sans I/O ; position zéro sans I/O ; correspondance/divergence ;
  fenêtre terminale de 64 Kio ; changement de longueur locale ; plage distante incomplète ; lecture
  fichier exacte ; `Range` fermé et validateurs ; mauvais `Content-Range` ; corps court ; absence de
  Range ; revalidation de redirections et cible interdite non contactée.
- Intégration : sonde `bytes=0-0`, puis recouvrement `bytes=0-4`, contenu `hello` identique, temporaire,
  état et checkpoint inchangés — RÉUSSI.
- Coordination complète, course après fermeture, proxy/NAT64, mutation, reprise HTTP, crash réel et
  redémarrage Windows : NON EXÉCUTÉS. Résultat inconnu.
- Preuve canonique post-documentation : restauration hors ligne RÉUSSIE ; build Release
  0 avertissement/0 erreur ; 93 exécutés, 93 réussis, 0 échec, 0 ignoré, 3,694 s ; formatage RÉUSSI ;
  contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences et 35 tâches cohérentes.

## 15. Exécution G2 — coordinateur diagnostique — 2026-08-04

- Objectif : exécuter local → distant → décision → recouvrement avec court-circuit avant réseau et
  sans mutation.
- Environnement : Windows, SDK .NET 10.0.302, Release, Microsoft Testing Platform.
- Baseline isolée : 93 exécutés, 93 réussis, 0 échec, 0 ignoré, 16,517 s.
- Première tentative de baseline : NON EXÉCUTÉE, résolution `MSTest.Sdk` impossible car `APPDATA`
  n’était pas confiné ; aucun test n’a démarré.
- Première tentative ciblée : compilation ÉCHOUÉE, nom d’argument de fixture mal cassé ; production
  non exécutée. Correction limitée au test.
- Deuxième tentative ciblée : 0 test sélectionné, code sortie 5, filtre incompatible avec le runner.
- Application complet après correction : 45/45 puis 46/46 réussis, 0 échec, 0 ignoré.
- Intégration : 4/4 réussis ; un appel au coordinateur produit une sonde `bytes=0-0`, une plage
  `bytes=0-4`, `OverlapMatched`, et conserve fichier, état et checkpoint inchangés.
- Cas prouvés : métadonnées absentes sans disque/réseau ; fichier plus court sans réseau ;
  contradiction distante sans recouvrement ; correspondance/divergence ; changement local sans
  plage distante ; position zéro sans plage ; annulation après inspection sans réseau.
- Non-régression solution avant contrôle canonique : 101 exécutés, 101 réussis, 0 échec, 0 ignoré,
  24,989 s.
- Vérification canonique post-documentation : restauration hors ligne RÉUSSIE ; build Release
  0 avertissement/0 erreur ; 101 exécutés, 101 réussis, 0 échec, 0 ignoré, 13,255 s ; formatage
  RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences et 35 tâches.
- Crash réel, disque plein, reprise HTTP existante, troncature, redémarrage Windows, proxy/NAT64 et
  performances : NON EXÉCUTÉS. Résultat inconnu.

## 16. Exécution G2 — banc déterministe flush/checkpoint — 2026-08-04

- Objectif : injecter une faute après flush durable, avant commit SQLite et après commit, puis fermer
  et rouvrir le dépôt pour comparer checkpoint et fichier.
- Environnement : Windows, SDK .NET 10.0.302, Release, Microsoft Testing Platform ; vrai
  `DurableTemporaryFileWriter`, vrai `SqliteDownloadRepository`, contenu `hello` de 5 octets.
- Baseline : 101 exécutés, 101 réussis, 0 échec, 0 ignoré.
- Première compilation : 0 test exécuté ; 6 erreurs car la fixture partagée n’exposait pas
  `DatabasePath`. ÉCHEC consigné ; correction limitée au calcul du chemin dans le test.
- Intégration après correction : 7 exécutés, 7 réussis, 0 échec, 0 ignoré, 7,184 s.
- Non-régression solution : 104 exécutés, 104 réussis, 0 échec, 0 ignoré, 18,718 s.
- Après flush avant confirmation : fichier 5, tâche mémoire 0, SQLite restaurée 0,
  `TemporaryFileLonger`, position sûre 0 — RÉUSSI.
- Avant commit du checkpoint : fichier 5, tâche mémoire 5, SQLite restaurée 0,
  `TemporaryFileLonger`, position sûre 0 — RÉUSSI.
- Après commit du checkpoint : fichier 5, tâche mémoire 5, SQLite restaurée 5,
  `TemporaryFileMatchesCheckpoint`, position sûre 5 — RÉUSSI.
- PR-032 : PARTIEL. Terminaison brutale subprocess, panne électrique, disque plein, écriture partielle
  et redémarrage Windows : NON EXÉCUTÉS. Résultat inconnu.
- Vérification canonique post-documentation : restauration hors ligne RÉUSSIE ; build Release
  0 avertissement/0 erreur ; 104 exécutés, 104 réussis, 0 échec, 0 ignoré, 12,483 s ; formatage
  RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences et 35 tâches.

## 17. Exécution G2 — terminaison subprocess flush/checkpoint — 2026-08-04

- Objectif : tuer réellement un processus enfant après flush, avant commit SQLite et après commit,
  puis restaurer les artefacts depuis le processus parent.
- Environnement : Windows, SDK .NET 10.0.302, Release, `Process.Kill(false)`, délai parent 30 s,
  vrai writer, vrai SQLite, contenu mono-bloc `hello` de 5 octets.
- Baseline : 104 exécutés, 104 réussis, 0 échec, 0 ignoré.
- Restauration du nouveau projet et génération de son verrou : RÉUSSIES, aucune dépendance nouvelle.
- Intégration : 10 exécutés, 10 réussis, 0 échec, 0 ignoré, 28,467 s.
- Non-régression solution : 107 exécutés, 107 réussis, 0 échec, 0 ignoré, 28,694 s.
- Mort post-flush : code non nul, fichier 5, SQLite 0, `TemporaryFileLonger`, position 0 — RÉUSSI.
- Mort pré-commit : code non nul, fichier 5, SQLite 0, `TemporaryFileLonger`, position 0 — RÉUSSI.
- Mort post-commit : code non nul, fichier 5, SQLite 5, `MatchesCheckpoint`, position 5 — RÉUSSI.
- PR-032 : PARTIEL. Crash subprocess mono-bloc prouvé ; avant-flush, multi-blocs, panne électrique,
  reboot Windows, disque plein et écriture partielle : NON EXÉCUTÉS. Résultat inconnu.
- Vérification canonique post-documentation : restauration hors ligne RÉUSSIE ; build Release
  0 avertissement/0 erreur ; 107 exécutés, 107 réussis, 0 échec, 0 ignoré, 50,467 s ; formatage
  RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences et 35 tâches cohérentes.

## 18. Exécution G2 — crash subprocess pendant le second bloc — 2026-08-04

- Identifiant lié : PR-032 / Q-001 / F-015.
- Objectif : tuer le processus enfant pendant la deuxième opération d’un transfert de 70 000 octets
  et vérifier la restauration du premier ou du second checkpoint selon la frontière atteinte.
- Conditions initiales : base et temporaire absents, tâche neuve, contenu déterministe de 70 000
  octets, buffer orchestrateur de 65 536 octets, vrai writer et vrai SQLite.
- Préparation : le host sélectionne la deuxième opération par comptage explicite des flushs ou des
  checkpoints positifs ; le parent conserve le délai de 30 secondes et rouvre les artefacts.
- Après flush du second bloc : fichier 70 000, SQLite 65 536, `TemporaryFileLonger`, position sûre
  65 536 — RÉUSSI.
- Avant commit du second checkpoint : fichier 70 000, SQLite 65 536, `TemporaryFileLonger`, position
  sûre 65 536 — RÉUSSI.
- Après commit du second checkpoint : fichier 70 000, SQLite 70 000,
  `TemporaryFileMatchesCheckpoint`, position sûre 70 000 — RÉUSSI.
- Preuve de contenu : les 70 000 octets restaurés sont comparés au contenu déterministe attendu dans
  chacun des trois scénarios — RÉUSSI.
- Intégration Release : 13 exécutés, 13 réussis, 0 échec, 0 ignoré, 16,868 s.
- Non-régression solution Release : 110 exécutés, 110 réussis, 0 échec, 0 ignoré, 14,214 s.
- Vérification canonique post-documentation : restauration hors ligne RÉUSSIE ; build Release
  0 avertissement/0 erreur ; 110 exécutés, 110 réussis, 0 échec, 0 ignoré, 15,167 s ; formatage
  RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences et 35 tâches cohérentes.
- Statut PR-032 : PARTIEL. Crash avant flush, panne électrique, reboot Windows, disque plein,
  corruption SQLite et écriture partielle réelle : NON EXÉCUTÉS. Résultat inconnu.

## 19. Exécution G2 — mort avant le second appel disque — 2026-08-04

- Identifiant lié : PR-032 / Q-001 / F-015.
- Objectif : tuer le subprocess immédiatement avant le deuxième `WriteAndFlushAsync`, après commit
  du premier bloc, puis prouver que fichier et SQLite restent exactement à 65 536 octets.
- Conditions initiales : contenu déterministe de 70 000 octets, buffer de 65 536, base et temporaire
  absents, vrais adaptateurs Storage/Persistence.
- Préparation : le décorateur compte les appels writer, laisse le premier écrire/flush, puis tue avant
  de déléguer le second appel. Le parent rouvre les artefacts sous un nouveau processus.
- Résultat attendu : code non nul ; fichier 65 536 ; SQLite 65 536 ; contenu égal au préfixe source ;
  `TemporaryFileMatchesCheckpoint` ; position sûre 65 536.
- Résultat obtenu : tous les éléments attendus sont observés — RÉUSSI.
- Intégration Release : 14 exécutés, 14 réussis, 0 échec, 0 ignoré, 17,648 s.
- Non-régression solution Release : 111 exécutés, 111 réussis, 0 échec, 0 ignoré, 14,668 s.
- Vérification canonique post-documentation : restauration hors ligne RÉUSSIE ; build Release
  0 avertissement/0 erreur ; 111 exécutés, 111 réussis, 0 échec, 0 ignoré, 16,881 s ; formatage
  RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences et 35 tâches cohérentes.
- Limite : ce test tue avant l’appel disque ; il ne simule pas une écriture partielle ni une erreur
  après mutation du fichier mais avant retour de la frontière durable.

## 20. Reprise réseau et finalisation même volume — 2026-08-10

- Pile : CSHARP-CIBLE.
- Précondition : tâche v2 `Downloading`, temporaire exact au checkpoint 3, identité forte ETag/taille.
- Séquence : sonde `bytes=0-0`, recouvrement `bytes=0-2`, reprise `bytes=3-`, flush/checkpoint,
  passage `Verifying`, intention `Finalizing`, move sans écrasement, passage `Completed`.
- Oracle : contenu final exact `hello`, temporaire absent, destination présente, SQLite à 5 et
  `Completed` ; un recouvrement divergent ne produit aucune mutation.
- Tests ciblés observés avant vérification canonique : Application 53/53, Storage 17/17,
  Integration 16/16, 0 échec, 0 ignoré.
- Limites : hash SHA-256, crash subprocess avant/après move, volumes différents, disque plein,
  antivirus et reboot Windows non exécutés ; résultat inconnu.

## 21. Crashs subprocess de finalisation — 2026-08-11

- Identifiants : M-007 / Q-001 / F-018 / ADR-029.
- Pile : CSHARP-CIBLE.
- Frontière A : tuer après commit `Finalizing`, avant move. Oracle avant réparation : SQLite
  `Finalizing`, temporaire exact présent, destination absente. Oracle final : `Completed`, destination exacte.
- Frontière B : tuer après move, avant commit `Completed`. Oracle avant réparation : SQLite
  `Finalizing`, temporaire absent, destination exacte. Oracle final : `Completed` sans second move.
- Frontière C : tuer après commit `Completed`. Oracle : SQLite `Completed`, temporaire absent,
  destination exacte ; aucune réparation nécessaire.
- Test ciblé Integration Release : 19 exécutés, 19 réussis, 0 échec, 0 ignoré en 32,185 s.
- Limites : SHA-256, disque plein, verrou antivirus, copie inter-volume, panne électrique et reboot
  Windows non exécutés ; résultat inconnu.

## 22. SHA-256 avant finalisation et migration v3 — 2026-08-11

- Identifiants : M-005 / M-008 / F-017 / ADR-011 / ADR-029.
- Domaine : transition `Verifying → Finalizing` refusée sans hash ; format hexadécimal normalisé.
- Application : hash attendu identique autorisé ; divergence refusée avant intention/move ; hash
  modifié pendant réparation refusé sans mutation.
- Storage : `SHA256("hello")` produit l’empreinte canonique attendue en streaming ; annulation testée.
- Persistence : aller-retour `Finalizing` conserve le hash ; migration v2→v3 ajoute une valeur nulle
  sans perdre la tâche ; migration v1→v3 conservée.
- Intégration : finalisation et trois réparations subprocess restaurent le même hash persistant.
- Tests ciblés : Domain 9/9, Application 56/56, Storage 19/19, Persistence 9/9, Integration 19/19.
- Limites : benchmark gros fichier, inter-volume réel et panne matérielle non exécutés.

## 23. Collisions et finalisation inter-volume — 2026-08-11

- Identifiants : M-004 / M-007 / F-018 / F-019 / ADR-029.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release, Microsoft Testing Platform.
- Collision `Fail` : destination existante, temporaire et destination conservés, aucun intent/move.
- Collision `KeepBoth` : `file.bin` et `file (1).bin` occupés, `file (2).bin` choisi puis persisté.
- Inter-volume simulé par `IFileVolumeComparer` : copie de 300 000 octets, flush, hash exact,
  destination présente et source absente.
- Reprise : transit partiel remplacé ; source+destination identiques nettoyées ; destination au hash
  divergent bloque et conserve les deux fichiers ; même volume avec deux fichiers reste ambigu.
- Intégration : collision réelle sur disque, `KeepBoth`, protocole de copie forcé et round-trip SQLite
  produisent `Completed`, l’ancien fichier inchangé et le nouveau contenu exact.
- Tests ciblés : Domain 11/11, Application 58/58, Storage 24/24, Persistence 10/10,
  Integration 20/20, tous réussis.
- Vérification canonique finale `eng/verify.ps1` : 147 exécutés, 147 réussis, 0 échec, 0 ignoré en
  26,864 s ; build 0 avertissement/0 erreur, formatage et documentation réussis.
- La tranche empreinte distante du 2026-08-11 porte la suite à 164 tests : build Release 0 erreur,
  164/164 réussis, 0 échec, 0 ignoré en 1 m 09 s ; formatage sans changement ; documentation réussie.
- Le harnais inter-volume réel du 2026-08-11 (destination explicite au CrashTestHost, script
  `eng/run-intervolume-real.ps1`) ajoute deux scénarios subprocess et porte la suite à 225 tests :
  225/225 réussis, 0 échec, 0 ignoré en 53 s ; formatage sans changement ; documentation réussie.
- Deux volumes physiques, crash subprocess au milieu de copie, disque plein, retrait, antivirus,
  reparse point concurrent et panne électrique : NON EXÉCUTÉS. Résultat inconnu.

## 24. Inter-volume réel — protocole d'exécution (2026-08-11)

- Identifiants : M-004 / M-007 / F-018 / ADR-029.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- Prérequis : **deux volumes** fixes ou amovibles montés (ex. `C:` système et clé USB `E:`).
  Sans second volume, le script affiche les instructions (brancher un disque/USB, ou monter un
  VHDX via `New-VHD`/`Mount-VHD`).
- Harnais : `WindowsDownloadManager.CrashTestHost` accepte désormais un **chemin de destination
  explicite** en 5ᵉ argument (au lieu de dériver la destination du dossier du temporaire) ; le flag
  `--different-volume` reste disponible pour la simulation et peut être passé à n'importe quelle
  position. Avec destination explicite sur un second volume réel et sans flag, le comparateur
  `PathRootFileVolumeComparer` détecte les volumes distincts réels et déclenche le protocole de copie
  inter-volume réel.
- Exécution :
  ```
  powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\run-intervolume-real.ps1 -VolumeA C -VolumeB E
  ```
- Scénarios : `AfterInterVolumeStagingFlushed` et `AfterInterVolumeDestinationMoved`, chacun avec
  temporaire sur le volume source et destination sur le volume cible, puis terminaison abrupte du
  subprocess.
- Critères de validation (par scénario) :
  - exit code du subprocess non nul (crash effectif) ;
  - source absente, destination présente et de contenu exact `hello` ;
  - base SQLite restaurée en `DownloadState.Finalizing`.
- Réparation : `Finalizing → Completed` reste du ressort du futur `DownloadHost` via
  `DownloadFinalizationCoordinator.RepairAsync` ; le chemin est couvert par les tests d'intégration
  (section 23 et scénarios subprocess inter-volume simulés).
- Compléments non exécutés sur matériel réel : disque plein pendant la copie, retrait du volume en
  cours d'écriture, antivirus/verrou, reparse point concurrent et panne électrique. Résultat inconnu.

## 25. Segmentation multiple statique — M-009 (2026-08-11)

- Identifiants : M-009 / F-007 / R-013 / ADR-010.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- `SegmentPlanner.Plan(totalLength, segmentCount)` : segments ordonnés, contigus, couvrant la
  longueur exacte, sans segment vide (nombre effectif borné par la longueur).
- `SegmentPlanner.Validate` : rejette trous, chevauchements, segments vides et couverture incomplète.
- `DownloadOrchestrator.RunSegmentedAsync` : analyse, préparation ; transfert segmenté parallèle
  (une connexion par segment, écritures disque sérialisées) avec progrès contigu confirmé ; repli
  connexion unique si taille inconnue, plages non supportées ou `segmentCount == 1` ; transition
  `Verifying` après vérification de la longueur totale.
- Tests Domain : 11/11 `SegmentPlannerTests` (répartition, cas limites, validation).
- Tests Application : 7/7 `DownloadOrchestratorSegmentedTests` (assemblage exact de 70 000 octets,
  repli sans plages, un segment, longueur nulle, échec d'un segment conservant le progrès contigu à
  35 000, longueur inconnue, segmentCount invalide).
- Vérification canonique : build Release 0 erreur ; 225/225 tests réussis, 0 échec, 0 ignoré en
  53 s ; formatage sans changement ; documentation 16/16.
- Restent : plages bornées `bytes=start-end`, test d'intégration HTTP réel multi-segments et
  redistribution dynamique (M-010).

## 26. Retry des échecs transitoires — M-013 (2026-08-11)

- Identifiants : M-013 / F-008 / ADR-014.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- `HttpTransientFailureClassifier` : 429/500/502/503/504 transitoires (via
  `RemoteHttpException.IsTransient`), `HttpRequestException`/`IOException`/`TimeoutException`
  transitoires, autres échecs permanents ; `Retry-After` exposé quand présent.
- `ExponentialBackoffRetryPolicy` : tentatives bornées, backoff exponentiel, gigue 50-100 %, délai
  maximal, `Retry-After` serveur plafonné par la borne.
- Orchestrateur : politique optionnelle appliquée aux transferts connexion unique et segmentés ;
  sans politique, propagation immédiate conservée ; reprise au progrès confirmé.
- Tests : 7 `ExponentialBackoffRetryPolicyTests` ; 8 `HttpTransientFailureClassifierTests` ; 3 tests
  d'orchestrateur (retry d'un échec transitoire avec succès ensuite, propagation sans politique).
- Vérification canonique : build Release 0 erreur ; 225/225 tests réussis, 0 échec, 0 ignoré en
  53 s ; formatage sans changement ; documentation 16/16.
- Restent : ordonnancement global des retries multi-tâches (future file) et limites de connexions
  M-014.

## 27. Reprise d'un fichier segmenté interrompu — M-009 (2026-08-11)

- Identifiants : M-009 / F-007 / R-013 / ADR-010.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- `DownloadOrchestrator.ResumeSegmentedAsync` : réconciliation et recouvrement identiques à la
  reprise connexion unique, puis répartition de la portion restante
  `[ConfirmedBytes, length)` en segments contigus (offsets absolus) et transfert parallèle ;
  repli connexion unique si taille inconnue, plages non supportées ou `segmentCount == 1` ;
  transition `Verifying`.
- Sécurité : le recouvrement de chevauchement et la réconciliation d'identité bloquent sans
  mutation (mêmes invariants que la reprise simple).
- Tests : 6 `DownloadResumeSegmentedTests` (reprise multi-segments depuis un préfixe confirmé,
  repli sans plages, longueur inconnue, chevauchement bloquant, déjà complet, segmentCount
  invalide).
- Vérification canonique : build Release 0 erreur ; 225/225 tests réussis, 0 échec, 0 ignoré en
  53 s ; formatage sans changement ; documentation 16/16.
- Restent : test d'intégration HTTP réel multi-segments et redistribution dynamique (M-010).

## 28. Plages bornées `bytes=start-end` pour les segments — M-009 (2026-08-11)

- Identifiants : M-009 / F-007 / R-013 / ADR-010.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- `IRemoteBoundedContentSource.OpenBoundedReadAsync(resource, start, end)` (Application).
- `HttpRemoteContentSource.OpenBoundedReadAsync` (Network) : `Range: bytes=start-end`, validation
  stricte de la réponse bornée, redirections et validateurs standards.
- Orchestrateur : chaque segment préfère la plage bornée si la source l'implémente, sinon repli
  plage ouverte (rétrocompatible).
- Tests : `RunSegmented_WithBoundedContentSource_UsesBoundedRangesOnly` (4 segments → 4 lectures
  bornées, aucune plage ouverte) ; les tests de segmentation/reprise existants passent inchangés.
- Vérification canonique : build Release 0 erreur ; 225/225 tests réussis, 0 échec, 0 ignoré en
  57 s ; formatage sans changement ; documentation 16/16.
- Restent : intégration HTTP réelle multi-segments et redistribution dynamique (M-010).

## 29. File, priorités et limites globales — M-014 (2026-08-11)

- Identifiants : M-014 / F-021 / ADR-014.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- `DownloadScheduler` (Application) : `Submit` enfile `ScheduledDownload(id, priorité, arrivée)` ;
  `AcquireNext(now)` retourne la tâche la plus prioritaire (priorité décroissante, puis FIFO) tant
  que la limite de concurrence globale n'est pas atteinte ; `Release(id)` libère un créneau.
- Anti-famine : au-delà d'un intervalle de vieillissement, la priorité effective d'une tâche en
  attente augmente (boost par intervalle), garantissant qu'une basse priorité finit par passer.
- Tests : 7 `DownloadSchedulerTests` (priorité, FIFO, limite globale, libération, file vide,
  anti-famine, arguments invalides).
- Vérification canonique : build Release 0 erreur ; 225/225 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16.
- Restent : intégration au futur `DownloadHost` et `M-015` (débit global/tâche/domaine).

## 30. Contrôle de débit global/tâche/domaine — M-015 (2026-08-11)

- Identifiants : M-015 / F-022 / ADR-015.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- `BandwidthController` (Application) : seaux à jetons hiérarchiques global / par tâche / par
  domaine ; `AcquireAsync(taskId, domain, byteCount)` attend la plus longue des attentes nécessaires
  puis consomme les jetons.
- Réapprovisionnement paresseux (temps écoulé × débit, plafonné au burst) ; horloge et attente
  injectables pour des tests déterministes.
- Tests : 7 `BandwidthControllerTests` (sous la limite, throttling global, indépendance
  tâche/domaine, partage global, octets nuls, burst invalide).
- Vérification canonique : build Release 0 erreur ; 225/225 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16.
- Restent : mesure de débit réelle (Q-003) et intégration au futur `DownloadHost`.

## 31. Redistribution dynamique — M-010 (2026-08-12)

- Identifiants : M-010 / F-008 / ADR-014.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- `ChunkWorkQueue` (Application) : découpe `[0, longueur)` en chunks de taille fixe ; `TryAcquireNext`
  distribue atomiquement le prochain chunk (file partagée, verrou interne) ; `MarkCompleted` marque un
  chunk ; `ComputeContiguousProgress` retourne le plus long préfixe complet pour la reprise sûre.
- `DownloadOrchestrator.RunDynamicSegmentedAsync` : N connexions tirent des chunks jusqu'à épuisement
  (chaque chunk est une plage bornée via `IRemoteBoundedContentSource`, sinon repli plage ouverte) ;
  repli connexion unique si longueur inconnue ou nulle, sans plages bornées ou `connectionCount == 1` ;
  progression contiguë persistée à chaque avance.
- Redistribution par construction (file partagée) : une connexion rapide tire davantage de chunks.
- Tests : 7 `ChunkWorkQueueTests` (couverture sans trou/chevauchement, épuisement, queue résiduelle,
  partage multi-workers sans doublon, progrès contigu, arguments invalides) ; 9
  `DownloadOrchestratorDynamicTests` (assemblage exact de 10 chunks, replis connexion unique/sans plages/
  longueur inconnue, longueur nulle, échec du premier chunk à zéro, échec de chunks ultérieurs avec
  préfixe conservé, arguments invalides).
- Vérification canonique : build Release 0 erreur ; 241/241 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16.
- Restent : redistribution pilotée par vitesse de connexion, intégration HTTP réelle multi-segments et
  intégration au futur `DownloadHost`.

## 32. Sept niveaux de reprise — M-011 (2026-08-12)

- Identifiants : M-011 / F-011 / F-014 / ADR-020 (ordre normatif).
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- ForcedResumeEngine (Application) : moteur pur d'évaluation ordonnée. ForcedResumeContext porte
  uniquement des observations vérifiables ; Evaluate retourne une ForcedResumeDecision (niveau,
  action, sûreté, raison stable, état cible).
- Sept niveaux : 1 NativeRange (métadonnées présentes, identité compatible, Range observé, sans
  contradiction/preuve insuffisante/lien expiré), 2 ShortProbe (capacité inconnue), 3 AuthorizedFinalUrl
  (seule l'URL finale a changé), 4 NewLink (lien expiré + nouveau lien explicite), 5 Recovery (préalable
  de sûreté), 6 Retransmission (métadonnées absentes ou Range perdu ; contrôlée depuis M-012),
  7 SafeStop (contradiction, preuve insuffisante, aucun chemin sûr).
- Tests : 15 ForcedResumeEngineTests — chaque branche, ordre de sûreté, refus du nouveau lien
  contradictoire (PR-052), arrêt sûr, priorité de l'arrêt utilisateur, transitions légales depuis
  TestingResume, contexte null.
- Vérification canonique : build Release 0 erreur ; 256/256 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16.
- Restent : intégration au futur DownloadHost et preuves de bout en bout (PR-050/051/052).

## 33. Retransmission contrôlée — M-012 (2026-08-12)

- Identifiants : M-012 / F-012 / ADR-020 / PR-060/061/062.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- `ControlledRetransmissionEngine` (Application) : comparaison continue du flux distant (depuis zéro)
  avec les octets locaux via `ITemporaryFileRangeReader` ; préfixe identique jamais réécrit ; écriture
  reprise au premier octet absent (flush avant frontière) ; divergence → arrêt sûr immédiat, ancien
  partiel intact (PR-061).
- `EstimateCost` annonce le volume réseau total depuis zéro et les octets locaux préservés ; coût
  significatif → consentement explicite (opt-in, PR-062).
- `ForcedResumeEngine` : branche 6 sûre (`RetransmitFromZero`, `ControlledRetransmission`).
- Tests : 15 `ControlledRetransmissionEngineTests` (préfixe préservé, reprise au manque, divergences
  à 64 Kio/50 %/fin, flux court/long, suffixe obsolète, ordre durable, coût/consentement, arguments).
- Vérification canonique : build Release 0 erreur ; 271/271 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16.
- Restent : intégration au futur `DownloadHost`, consentement UI (F-012/PR-062) et preuves de bout en
  bout sur serveur réel (PR-060/061).

## 34. Assemblage du DownloadHost — ADR-025 (2026-08-12)

- Identifiants : ADR-025 / M-001 à M-015 / Q-001.
- Pile : CSHARP-CIBLE ; Windows, SDK .NET 10.0.302, Release.
- Projet `src/WindowsDownloadManager.Host` (assembly `idm`) : processus headless qui réunit les
  composants — `DownloadHost` (cycle), `DownloadStrategy` (simple/segmenté/dynamique),
  `ThrottledRemoteContentSource` (débit par bloc), `RebuildScheduleAsync` (reprise au démarrage via
  `ListNonTerminalAsync`), `Program.cs` (CLI `add`/`run`/`cancel` avec les adaptateurs réels).
- Cycle : `New` → stratégie → vérification → finalisation ; `Downloading` → reprise, sinon décision
  `ForcedResumeEngine` (retransmission contrôlée ou arrêt sûr via `Reconnecting → TestingResume`) ;
  `Verifying`/`Finalizing` → finalisation/réparation.
- Tests : 10 `DownloadHostTests`, 9 `DownloadStrategyTests`, 2 `ThrottledRemoteContentSourceTests`
  (21 au total — cycle neuf, reprise, retransmission, arrêt sûr, finalisation, réparation, annulation,
  pause, priorité, stratégie, débit).
- Vérification canonique : build Release 0 erreur ; 290/290 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16.
- Restent : instance unique par utilisateur et IPC authentifié (ADR-025), profil de débit, preuves de
  bout en bout (PR-060/061/062) et inter-volume réel.
