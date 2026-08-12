# État actuel du projet

Version documentaire : 2.6
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-12  
Statut : TABLEAU DE BORD ACTIF — G2 PARTIELLE  
Responsable logique : Chef de projet  
Documents liés : `FEUILLE_DE_ROUTE.md`, `SUIVI_DEVELOPPEMENT.md`, `ERREURS_CONNNUES.md`

## 1. Identité et état général

- Nom de travail : IDM Engine / Windows Download Manager.
- Version produit : 0.1.0 expérimentale.
- Branche Git : `main`, dépôt local relié à `https://github.com/TUBI225/IDM.git`.
- Dernier commit : commit initial de la baseline G2, publié le 2026-08-11.
- État général : **SOCLE C# PARTIEL, NON UTILISABLE ENCORE COMME GESTIONNAIRE COMPLET**.
- Porte actuelle : G2 partielle ; reprise réseau, SHA-256 persisté, collisions explicites,
  finalisation même/inter-volume simulée et empreinte distante officielle présents ; chaos matériel
  et banc inter-volume réel restent à construire.

Le projet contient deux piles distinctes. Le prototype Python est une référence temporaire de
comportement. Le produit actif cible est le moteur C#/.NET 10. Une preuve Python ne valide pas une
fonction C# et les données persistantes des deux piles ne doivent pas être partagées sans migration.

## 2. Charge opérationnelle

Les statistiques comptent uniquement les 35 tâches exécutables `D/M/W/B/Q`. Les objectifs parents
et tranches historiques `T-*` sont exclus afin d’éviter le double comptage.

| Statut | Nombre |
|---|---:|
| À FAIRE | 10 |
| EN COURS | 1 |
| PARTIEL | 21 |
| À VÉRIFIER | 1 |
| TERMINÉ | 2 |
| BLOQUÉ / REPORTÉ / ABANDONNÉ | 0 |

## 3. Produit C# cible — réellement présent

- SDK local .NET 10.0.302 et runtime 10.0.10.
- Solution headless avec `Domain`, `Application`, `Network`, `Storage` et `Persistence`.
- Agrégat `DownloadTask`, états et matrice initiale de transitions.
- Ports de dépôt, analyse distante, flux distant, validation d’URI et fichier temporaire.
- Analyse HTTP streaming `bytes=0-0`, métadonnées, repli prudent sur `200` et validation stricte
  d’un `206` de sondage.
- Redirections manuelles, classification 416/429/5xx, annulation et blocage conservateur des
  adresses privées/réservées.
- Six projets MSTest séparent Domain, Application, Network, Storage, Persistence et intégration ;
  290 tests distincts réussissent après l'assemblage du `DownloadHost` (ADR-025).
- NuGet est limité à `nuget.org`, mis en cache localement et verrouillé par projet.
- Connexion socket liée à l’IP filtrée, client HTTP injecté, proxy désactivé et rebinding loopback bloqué.
- Writer positionnel avec flush disque et dépôt SQLite v4 : migrations v1/v2/v3/v4 checksummées, WAL,
  `synchronous=FULL`, écrivain sérialisé et URL persistée sans query/fragment.
- Orchestrateur headless neuf : analyse, création exclusive du temporaire, flux Range, blocs 64 Kio,
  flush avant checkpoint SQLite et arrêt en `VERIFYING` avant toute finalisation.
- Reprise headless d’une tâche `Downloading` : diagnostic local/distant, recouvrement borné,
  ouverture HTTP au checkpoint et confirmation durable bloc par bloc.
- Finalisation même volume : longueur vérifiée, intention `Finalizing` persistée, move sans
  écrasement, état `Completed`, et réparation si un seul des deux chemins subsiste au redémarrage.
- SHA-256 calculé en streaming avant `Finalizing`, comparé en temps constant à une valeur attendue
  optionnelle, persisté avec l’intention puis recalculé avant toute réparation.
- Test d’intégration réel sur loopback : `hello` écrit durablement, 5 octets restaurés depuis SQLite.
- Chemin temporaire et `RemoteIdentity` (URL finale expurgée, taille, ETag, Last-Modified, Range)
  enregistrés avant la création du temporaire et restaurés après réouverture.
- Réconciliation locale en lecture seule : métadonnées/temporaire absents et longueurs plus courte,
  égale ou plus longue classés ; position diagnostique `min(checkpoint, longueur)` sans mutation.
- Réconciliation distante en lecture seule : URL expurgée, taille, ETag, Last-Modified et Range
  comparés ; compatibilité, preuve insuffisante, perte de capacité et contradiction distinguées.
- Évaluateur pur de récupération : motifs local/distant cumulés, IDs différents refusés et seul le
  couple temporaire exact + distant compatible déclaré prêt pour le futur recouvrement.
- Vérification de recouvrement : fenêtre terminale maximale de 64 Kio lue sans mutation, requête HTTP
  fermée et strictement validée, divergence et changement local distingués.
- Coordinateur de démarrage : inspection locale → court-circuit éventuel avant réseau → analyse
  distante → décision → recouvrement ; résultat final typé et preuves intermédiaires conservées.
- Banc de fautes de durabilité : vrai writer avec `Flush(true)`, vrai SQLite puis restauration et
  réconciliation après faute injectée après flush, avant commit ou après commit.
- Hôte de crash : exécutable de test séparé tué aux trois frontières ; le processus parent rouvre
  SQLite et le temporaire puis vérifie les états 0/5, 0/5 et 5/5.
- Extension multi-blocs : pendant le second bloc d’un contenu déterministe de 70 000 octets, les
  états restaurés sont 65 536/70 000 après flush et avant commit, puis 70 000/70 000 après commit.
- Frontière pré-écriture : une mort avant le deuxième appel au writer restaure fichier et SQLite à
  65 536, avec contenu exact et `TemporaryFileMatchesCheckpoint`.
- Dernière vérification canonique post-documentation : restauration hors ligne réussie ; build Release
  0 avertissement/0 erreur ; 107/107 tests réussis en 50,467 s ; formatage conforme ; contrôle
  documentaire réussi avec 16/16 documents, 36/36 exigences et 35 tâches cohérentes.

- Segmentation dynamique (M-010) : `ChunkWorkQueue` découpe la ressource en chunks ; chaque connexion
  pioche le prochain chunk atomiquement, la progression contiguë est persistée à chaque avance et la
  redistribution est auto-équilibrée (une connexion rapide travaille plus) ; repli connexion unique
  si longueur inconnue, nulle ou sans plages bornées.

### Limites C# actuelles

- La sérialisation est limitée à l’instance d’orchestrateur/coordinateur ; l’exclusion mutuelle du
  futur `DownloadHost`, la découverte d’un hash officiel distant et les crashs matériels restent absents.
- Aucun projet WinUI, extension navigateur ou installateur ; l’ordonnancement, la segmentation
  (statique et dynamique) et le contrôle de débit vivent dans le moteur headless.
- Le rebinding vers loopback est bloqué ; proxy, NAT64/IPv6 adverses, TLS public, limites d’en-têtes
  et corps HTTP malformés restent incomplets.
- Les deux sauts de redirection sont observés et le socket utilise l’adresse filtrée. Les profils
  proxy et environnements NAT64 restent non validés.

## 4. Prototype Python — référence temporaire, non produit cible

Le prototype Python possède une CLI à connexion unique, un fichier `.download`, SQLite, checkpoints
après synchronisation, recouvrement de 64 Kio, contrôle de taille/ETag/Last-Modified et finalisation.
Trois tests locaux couvrent téléchargement complet, reprise simulée et ETag modifié.

Limites : pause réelle, crash réel, redémarrage Windows, disque plein, sécurité de redirection avant
connexion et migration vers C# non prouvés. Le prototype ne doit plus recevoir de nouvelles fonctions,
sauf correction nécessaire à une fixture de parité approuvée.

## 5. Risques et anomalies prioritaires

- R-001 : comparaison, recouvrement, reprise et SHA-256 final local présents ; empreinte distante
  acquise depuis les en-têtes HTTP et vérifiée à la finalisation (mode strict par défaut) ; courses
  inter-processus restent absents.
- R-002/R-011 : ordre flush→checkpoint prouvé par exceptions et sept terminaisons subprocess jusqu’à
  la frontière pré-écriture du second bloc ; erreur pendant écriture, panne électrique et écriture
  partielle restent.
- R-004 : rebinding loopback bloqué à la connexion ; proxy/NAT64 restent ouverts.
- R-017 : migration additive v1→v2 vérifiée ; interruption, corruption et rollback restent ouverts.
- R-022 : divergence Python/C# ouverte.
- R-023 : dépôt Git maintenant initialisé ; baseline non commitée faute d’identité Git configurée.
- BUG-001 : CORRIGÉE par deux tests MSTest ; R-004 reste ouvert pour le rebinding transport.

## 6. Derniers tests observés

### C#/.NET — dernière preuve G2 du 2026-08-04

- Commande canonique : `powershell.exe -NoProfile -ExecutionPolicy Bypass -File eng/verify.ps1`
  avec `DOTNET_CLI_HOME` et `APPDATA` confinés au workspace.
- Environnement : Windows, SDK .NET 10.0.302.
- Baseline avant modification : 75 exécutés, 75 réussis, 0 échec, 0 ignoré, 5,560 s.
- Première suite : 91 exécutés, 90 réussis, 1 échec, 0 ignoré, 8,913 s ; type d’exception de corps
  tronqué normalisé sans relâcher le refus.
- Après correction : 91/91 réussis en 4,844 s ; Network ciblé 24/24 en 3,399 s.
- Non-régression finale avant documentation : 93 exécutés, 93 réussis, 0 échec, 0 ignoré, 3,647 s.
- Portée nouvelle : matrice Application, lecteur Storage exact, plages HTTP strictes/redirections et
  intégration réelle prouvant deux requêtes bornées sans mutation.
- Canonique post-documentation : restauration hors ligne réussie ; build Release 0 avertissement/
  0 erreur ; 93 exécutés, 93 réussis, 0 échec, 0 ignoré, 3,694 s ; formatage et contrôle documentaire
  réussis avec 16/16 documents, 36/36 exigences et 35 tâches cohérentes.
- Baseline de cette tranche : 93/93 réussis. Application après ajout : 46/46 réussis ; intégration
  loopback : 4/4 réussis ; non-régression solution : 101/101 réussis en 24,989 s.
- Canonique post-documentation : restauration hors ligne réussie ; build Release 0 avertissement/
  0 erreur ; 101 exécutés, 101 réussis, 0 échec, 0 ignoré, 13,255 s ; formatage et contrôle
  0 erreur ; 101 exécutés, 101 réussis, 0 échec, 0 ignoré, 13,255 s ; formatage et contrôle documentaire réussis avec 16/16 documents, 36/36 exigences et 35 tâches cohérentes.
- Nouvelle tranche : intégration 7/7 réussie ; non-régression solution 104/104 réussie en 18,718 s.
  Vérification canonique post-documentation : restauration hors ligne réussie ; build Release
  0 avertissement/0 erreur ; 104/104 réussis en 12,483 s ; formatage et contrôle documentaire réussis.
- Hôte subprocess : intégration 10/10 réussie ; non-régression solution 107/107 réussie en 28,694 s.
  Vérification canonique post-documentation : restauration hors ligne réussie ; build Release
  0 avertissement/0 erreur ; 107/107 réussis en 50,467 s ; formatage et contrôle documentaire réussis.
- Extension deux blocs : intégration 13/13 réussie en 16,868 s ; non-régression solution 110/110
  réussie en 14,214 s. Vérification canonique post-documentation : restauration hors ligne réussie ;
  build Release 0 avertissement/0 erreur ; 110/110 réussis en 15,167 s ; formatage et contrôle
  documentaire réussis.
- Faute d'écriture durant le second bloc : intégration 14/14 réussie ; non-régression solution 112/112
  réussie en 30,956 s. Vérification canonique post-documentation : restauration hors ligne réussie ;
  build Release 0 avertissement/0 erreur ; 112/112 réussis ; formatage et contrôle documentaire réussis.
- Reprise réseau d’une tâche existante, mutation réparatrice, panne électrique, écriture partielle et reboot Windows : NON EXÉCUTÉS. Résultat inconnu.

### Python — preuve G1 du 2026-08-03

- Commande : `python -m unittest discover -v`.
- Environnement : Windows, Python 3.12 fourni par Codex.
- Résultat consigné : 3 exécutés, 3 réussis, 0 échec, 0 ignoré en 2,118 s.
- Portée : prototype uniquement.

### Vérifications complémentaires G2

- Format .NET : réussi, aucun changement requis.
- Audit NuGet en ligne, dépendances transitives incluses : aucune vulnérabilité signalée.
- Tests de reprise C# réelle, crash, disque plein, proxy/NAT64, UI, installation et performance :
  NON EXÉCUTÉS. Résultat inconnu.

## 7. Décisions actives et décisions manquantes

Actives : ADR-021 à ADR-029. ADR-026 est appliquée au profil direct ; ADR-027 possède une première
implémentation v2. ADR-025 possède désormais son processus hôte assemblé (`DownloadHost` + CLI `idm`) ;
l'instance unique par utilisateur et l'IPC authentifié restent. ADR-029 reste sans finalisation
complète. Versions Windows minimales et packaging restent à décider.

## 8. Prochaine action officielle unique

Le `DownloadHost` (ADR-025) est assemblé : processus headless `idm` qui réunit ajout, stratégie
simple/segmenté/dynamique, vérification, finalisation, reprise, décision des sept niveaux,
retransmission contrôlée, priorités et contrôle de débit ; la suite compte 290 tests. La prochaine
étape consiste aux preuves de bout en bout sur serveur réel (PR-060/061/062) et/ou à l'instance unique
et l'IPC authentifié (frontières ADR-025) avant l'UI.

## 9. Preuve collision et inter-volume — 2026-08-11

- `Fail` refuse la destination existante sans sauvegarde ni I/O finale ; `KeepBoth` sélectionne et
  persiste le premier suffixe disponible.
- Même volume : move sans écrasement. Autre volume : transit lié à l’UUID sur la cible, copie bornée,
  flush disque, SHA-256, move local, seconde vérification et suppression tardive de la source.
- Réparation testée pour transit partiel, source et destination identiques, destination divergente et
  nom suffixé restauré depuis SQLite.
- Vérification canonique : restauration hors ligne réussie ; build Release 0 avertissement/0 erreur ;
  225 exécutés, 225 réussis, 0 échec, 0 ignoré en 53 s ; formatage réussi ; documentation 16/16,
  exigences 36/36 et 35 tâches cohérentes.
- Deux volumes physiques, crash subprocess pendant la copie, disque plein, retrait, antivirus,
  reparse point concurrent et performance gros fichier : NON EXÉCUTÉS. Résultat inconnu.

## 10. Correction d'intégrité de l'empreinte distante — 2026-08-11

L'audit a relevé que l'empreinte serveur (`RemoteIdentity.Sha256`) n'était jamais persistée en SQLite et
était confondue avec le hash local vérifié (`verified_sha256`). La correction ajoute la migration v4 et la
colonne dédiée `remote_sha256`, alimentée par `SaveAsync` et restaurée par `FindAsync`. Le hash local reste
dans `verified_sha256`. Le coordinateur de finalisation applique désormais la validation stricte par défaut
(`allowForcedBypass: false`) ; le forçage reste possible explicitement pour l'utilisateur. Six tests
ajoutés couvrent le rond-trip SQLite des deux empreintes, le chemin par défaut via `RemoteIdentity.Sha256`
et le décodage base64url des en-têtes.

## 11. Segmentation dynamique (M-010) — 2026-08-12

- `ChunkWorkQueue` découpe `[0, longueur)` en chunks de taille fixe ; `TryAcquireNext` distribue
  atomiquement le prochain chunk (file partagée) ; `MarkCompleted` marque un chunk et
  `ComputeContiguousProgress` retourne le plus long préfixe complet, préservant la reprise sûre.
- `DownloadOrchestrator.RunDynamicSegmentedAsync` : N connexions tirent des chunks jusqu'à épuisement ;
  repli connexion unique si longueur inconnue ou nulle, sans plages bornées ou `connectionCount == 1` ;
  checkpoint persistant à chaque avancée contiguë.
- Tests : 7 `ChunkWorkQueueTests` et 9 `DownloadOrchestratorDynamicTests` (assemblage exact de 10 chunks,
  replis, longueur nulle, échec du premier chunk, échec de chunks ultérieurs, arguments invalides).
- Vérification canonique : build Release 0 erreur ; 241/241 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16, exigences 36/36 et 35 tâches cohérentes.
- Restent : redistribution pilotée par vitesse de connexion, intégration au futur `DownloadHost` et
  mesure de débit réelle (Q-003).

## 12. Sept niveaux de reprise (M-011) — 2026-08-12

- `ForcedResumeEngine` (Application) : moteur pur d'évaluation ordonnée des sept niveaux du cahier des
  charges — Native `Range` → sondages courts → URL finale autorisée → nouveau lien légitime →
  recouvrement → retransmission contrôlée → arrêt sûr. `ForcedResumeContext` reçoit les observations
  vérifiables ; `Evaluate` retourne une `ForcedResumeDecision` immuable (niveau, action, sûreté, raison
  stable, état cible de la machine).
- Jamais de force : la reprise native exige métadonnées présentes, identité compatible, Range observé et
  l'absence de contradiction, de preuve insuffisante et de lien expiré ; un nom seul ne fournit aucune
  confiance (nouveau lien contradictoire refusé, PR-052) ; le recouvrement précède toute reprise réseau.
  La retransmission contrôlée (M-012) est signalée mais retombe en arrêt sûr tant qu'elle n'est pas
  implémentée.
- Transitions cibles légales depuis `TestingResume` (sonde, renouvellement, retransmission, fichier
  distant modifié, échec permanent) vérifiées par la matrice.
- Tests : 15 `ForcedResumeEngineTests` (chaque niveau, ordre de sûreté, refus du lien contradictoire,
  arrêt sûr, priorité de l'arrêt utilisateur, transitions légales, contexte null).
- Vérification canonique : build Release 0 erreur ; 256/256 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16, exigences 36/36 et 35 tâches cohérentes.
- Restent : intégration au futur `DownloadHost`, retransmission réelle (M-012) et preuves de bout en
  bout (PR-050/051/052).

## 13. Retransmission contrôlée (M-012) — 2026-08-12

- `ControlledRetransmissionEngine` (Application) : quand le serveur renvoie depuis zéro, comparaison
  continue du flux distant aux octets locaux via `ITemporaryFileRangeReader`. Un préfixe identique
  n'est jamais réécrit ; au premier octet absent, l'écriture reprend via `ITemporaryFileWriter`
  (flush avant toute frontière retournée) ; toute divergence provoque un arrêt sûr immédiat avec
  `DivergenceOffset`, l'ancien partiel intact.
- Cas couverts : flux identique, extension locale, divergence à 64 Kio/50 %/fin, flux plus court
  (`RemoteEndedEarly`) ou plus long (`ExceededAnnouncedLength`) que l'annoncé, suffixe local obsolète,
  longueur inconnue, flux vide.
- `EstimateCost` annonce le volume réseau total depuis zéro et les octets locaux préservés ; un coût
  significatif exige un consentement (opt-in, PR-062).
- `ForcedResumeEngine` : la branche Retransmission devient sûre (`RetransmitFromZero`,
  `ControlledRetransmission`) — les deux tests M-011 correspondants mis à jour.
- Tests : 15 `ControlledRetransmissionEngineTests`.
- Vérification canonique : build Release 0 erreur ; 271/271 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16, exigences 36/36 et 35 tâches cohérentes.
- Restent : intégration au futur `DownloadHost`, consentement UI (F-012/PR-062) et preuves de bout en
  bout sur serveur réel (PR-060/061).

## 14. Assemblage du DownloadHost (ADR-025) — 2026-08-12

- Nouveau projet exécutable `src/WindowsDownloadManager.Host` (assembly `idm`) et son projet de tests.
- `DownloadHost` : cycle complet via ports injectés — `AddAsync`, `RunPendingAsync` (planning
  reconstruit au démarrage), `CancelAsync`, `PauseAsync`, `DisposeAsync` (possède le dépôt).
- `ListNonTerminalAsync` ajoutée au dépôt (défaut vide, surchargée par SQLite avec un `SELECT` des
  états hors `Completed`/`Cancelled`) pour la reprise au démarrage.
- `DownloadStrategy` (simple/segmenté/dynamique) et `ThrottledRemoteContentSource` (débit par bloc).
- Cycle : `New` → stratégie → `Verifying` → finalisation ; `Downloading` → reprise, sinon décision
  `ForcedResumeEngine` (retransmission ou arrêt sûr via `Reconnecting → TestingResume`) ;
  `Verifying`/`Finalizing` → finalisation/réparation.
- `Program.cs` : CLI `add`/`run`/`cancel` avec les adaptateurs réels (anti-rebind + SSRF, Storage
  durable, SQLite v4, base via `IDM_DB`).
- Tests : 10 `DownloadHostTests`, 7 `DownloadStrategyTests`, 2 `ThrottledRemoteContentSourceTests`.
- Vérification canonique : build Release 0 erreur ; 290/290 tests réussis, 0 échec, 0 ignoré ;
  formatage sans changement ; documentation 16/16, exigences 36/36 et 35 tâches cohérentes.
- Restent : instance unique par utilisateur et IPC authentifié (ADR-025), politique de débit par
  profil, preuves de bout en bout (PR-060/061/062) et inter-volume réel.
