# Décisions d’architecture

Version documentaire : 2.2  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : ACTIF — DÉCISIONS ET PROPOSITIONS  
Responsable logique : Architecte principal  
Documents liés : `ARCHITECTURE_TECHNIQUE.md`, `DEPENDANCES.md`, `SECURITE.md`

## Sommaire

1. Règles ADR
2. Décisions existantes
3. Décisions cibles proposées
4. Décisions humaines requises

## ADR-001 — Python standard pour le premier moteur

- Date : 2026-08-03
- Statut : ACCEPTÉE POUR LE PROTOTYPE, À RÉÉVALUER POUR L’APPLICATION WINDOWS
- Problème : aucun runtime `.NET`, Python ou Node n’était exposé dans le PATH initial.
- Options : .NET natif Windows ; Python ; Node.js.
- Choix : Python 3.12 fourni par l’environnement, avec bibliothèque standard.
- Justification : implémentation/test immédiats sans dépendance tierce.
- Conséquences : portabilité du moteur, mais empaquetage Windows et interface native non décidés.
- Risques : le runtime fourni par Codex n’est pas une méthode de distribution utilisateur.
- Révision : avant T-010 et avant toute distribution.

## ADR-002 — SQLite comme mémoire persistante

- Date : 2026-08-03
- Statut : ACCEPTÉE
- Problème : conserver atomiquement l’état récupérable après interruption.
- Options : JSON, SQLite, base externe.
- Choix : SQLite via `sqlite3` standard.
- Avantages : transactions locales, aucun serveur, index et schéma explicite.
- Inconvénients : migrations à organiser ; concurrence à étudier.
- Conséquences : un dépôt isole les accès et le fichier temporaire reste la preuve matérielle.
- Révision : si plusieurs processus doivent écrire simultanément.

## ADR-003 — Un fichier temporaire et progression après `fsync`

- Date : 2026-08-03
- Statut : ACCEPTÉE
- Problème : empêcher la base d’annoncer des octets non persistés.
- Options : fichiers par segment ; fichier unique aléatoire ; tampon uniquement.
- Choix : fichier unique suffixé `.download`, progression enregistrée après synchronisation.
- Conséquences : reprise conservative ; future écriture segmentée devra préserver cette règle.
- Risques : coût de `fsync`, à mesurer.
- Révision : après mesures de performance représentatives.
- Mise en œuvre 2026-08-04 : la première réconciliation locale est volontairement en lecture seule
  et retourne `min(checkpoint, longueur)` comme diagnostic. Aucune troncature ou reprise n’est
  autorisée avant validation distante ; cette tranche applique la décision sans la réviser.
- Mise en œuvre test 2026-08-04 : trois fautes déterministes autour de flush/checkpoint réutilisent
  les ports existants et les vrais adaptateurs disque/SQLite. Aucune instrumentation de production
  n’est ajoutée ; ADR-003 reste inchangée et le crash brutal reste à prouver.
- Extension test 2026-08-04 : un exécutable de support est désormais tué par `Process.Kill` aux mêmes
  frontières et la restauration est réalisée par le parent. Cette preuve applique ADR-003 sans
  modifier le produit. Une seconde extension cible maintenant le deuxième bloc de 70 000 octets et
  restaure 65 536 ou 70 000 selon le commit atteint. Une mort avant le second appel disque conserve
  aussi fichier et base à 65 536. Crash pendant écriture, OS, écriture partielle et panne électrique
  restent requis.

## ADR-004 — Intégrité prioritaire sur reprise agressive

- Date : 2026-08-03
- Statut : ACCEPTÉE
- Choix : taille/validateurs distants et recouvrement binaire avant reprise ; arrêt en cas de doute.
- Conséquence : certains serveurs non conformes ne seront pas repris automatiquement.
- Révision : seulement avec une méthode déterministe et des tests prouvant l’absence de mélange.
- Mise en œuvre 2026-08-04 : la comparaison distante reste dans `Application`, ne lit que les
  métadonnées de la sonde et classe toute contradiction connue ou preuve insuffisante sans mutation.
  Un ETag fort identique, ou taille + Last-Modified identiques, constitue le seuil minimal actuel.
  Cette mise en œuvre applique ADR-004 sans créer de nouvelle décision. La composition ajoutée le
  2026-08-04 agrège tous les motifs de blocage et ne produit `ReadyForOverlapVerification` que pour
  un temporaire exactement au checkpoint et une identité distante compatible. Elle reste pure et
  ne constitue pas une autorisation de reprise ; ADR-004 n’est pas révisée. Le recouvrement ajouté
  le 2026-08-04 applique ensuite une fenêtre fermée maximale de 64 Kio, validée octet par octet sans
  mutation. Une divergence ou un changement local reste bloquant ; une correspondance n’autorise
  toujours pas la reprise avant revalidation de la future action. Le coordinateur ajouté le
  2026-08-04 applique cet ordre et court-circuite les blocages locaux avant réseau ; il ne révise pas
  ADR-004 et ne transforme aucun diagnostic en autorisation durable.

## 1. Règles ADR

Statuts : `PROPOSÉE`, `ACCEPTÉE`, `À RÉÉVALUER`, `REMPLACÉE`, `ABANDONNÉE`. Une décision remplacée
reste visible et référence sa remplaçante. Toute acceptation cible exige options, preuve, risques et
conditions de révision ; le prototype ne transforme pas automatiquement un choix en standard produit.

## 3. Décisions cibles proposées

| ADR | Sujet | Proposition | Statut | Validation requise |
|---|---|---|---|---|
| ADR-005 | Langage cible | Remplacée par ADR-021 (.NET 10 LTS) | REMPLACÉE | POC de compilation/performance restant |
| ADR-006 | UI | Remplacée par ADR-022 | REMPLACÉE | Historique conservé |
| ADR-007 | HTTP | Remplacée par ADR-023 | REMPLACÉE | Historique conservé |
| ADR-008 | Architecture | Domain/Application + ports/adaptateurs | PROPOSÉE | test d’un moteur sans UI |
| ADR-009 | État | Machine explicite persistée, transitions domaine | PROPOSÉE | matrice exhaustive |
| ADR-010 | Écriture | Fichier unique préalloué si taille fiable, accès aléatoire | PROPOSÉE | disque plein/NTFS/amovible |
| ADR-011 | Hash | SHA-256 final ; empreintes partielles versionnées | ACCEPTÉE, PARTIELLEMENT IMPLÉMENTÉE | coût restant ; empreinte distante acquise |
| ADR-012 | Secrets | DPAPI/utilisateur Windows, jamais SQLite en clair | PROPOSÉE | modèle de menace |
| ADR-013 | Browser | Native Messaging, protocole JSON borné/versionné | PROPOSÉE | revue permissions/origines |
| ADR-014 | Connexions | Départ modeste, croissance fondée sur mesures | PROPOSÉE | benchmark et 429 |
| ADR-015 | Débit | Seau à jetons hiérarchique | PROPOSÉE | précision/CPU/équité |
| ADR-016 | IA | Aucun rôle critique ; assistance facultative isolée | ACCEPTÉE | Révision si besoin non critique |
| ADR-017 | Logs | Structurés, rotation, redaction centralisée | PROPOSÉE | audit fuite de secrets |
| ADR-018 | Installation | Paquet signé, niveau utilisateur privilégié | PROPOSÉE | comparaison MSIX/MSI/autre |
| ADR-019 | Mise à jour | Manifest et binaire signés, rollback | PROPOSÉE | menace supply-chain |
| ADR-020 | Retransmission | Explicite, opt-in si coût significatif, comparaison continue | PROPOSÉE | tests de divergence |

## 4. Décisions humaines requises

Le propriétaire a validé la direction C#/.NET 10, WinUI 3 et la séparation du prototype Python lors
de G0. Restent à valider : versions Windows minimales, topologie des processus, réseau/DNS/proxy,
fournisseur SQLite, tests/NuGet, méthode d’installation, seuil de retransmission nécessitant
confirmation et politique de télémétrie (par défaut aucune).

## ADR-021 — C# et .NET 10 LTS pour le produit cible

- Date : 2026-08-03.
- Statut : ACCEPTÉE ET IMPLÉMENTÉE POUR LE MOTEUR HEADLESS ; PACKAGING RESTANT.
- Décideur : propriétaire du projet, décision technique déléguée à Codex.
- Contexte : le produit vise exclusivement Windows, doit traiter des fichiers volumineux, exécuter
  des I/O asynchrones, rester testable sans interface et être maintenu plusieurs années.
- Options étudiées : continuer Python ; C++ natif ; C#/.NET 10 ; C#/.NET 8.
- Décision : C# avec le dernier correctif supporté de .NET 10 LTS. Le moteur est composé de
  bibliothèques sans dépendance à WinUI. Le prototype Python reste une référence temporaire jusqu’à
  parité testée, puis son retrait exigera une décision séparée.
- Avantages : typage statique, `async/await`, diagnostics/outillage riches, performance adaptée aux
  flux et support LTS actif jusqu’au 14 novembre 2028.
- Inconvénients : SDK absent du poste actuel ; migration du prototype ; runtime/packaging à gérer.
- Risques : allocations et blocages si les flux sont mal conçus ; dépendance à l’écosystème .NET.
- Conséquences : `net10.0` pour le domaine/moteur et cible Windows pour l’app ; nullable activé,
  warnings traités comme erreurs, tests séparés et mesures avant toute optimisation.
- Conditions de révision : incapacité démontrée par POC à atteindre intégrité, compatibilité ou
  performance ; fin de support ; exigence multi-plateforme future.

## ADR-022 — WinUI 3 comme interface, architecture MVVM stricte

- Date : 2026-08-03.
- Statut : ACCEPTÉE PAR DÉLÉGATION DU PROPRIÉTAIRE, POC À VÉRIFIER.
- Contexte : nouvelle application Windows nécessitant interface moderne et séparation du moteur.
- Options : WPF ; WinUI 3 ; Windows Forms ; interface web embarquée.
- Décision : WinUI 3 via Windows App SDK, MVVM, composition racine dans l’application. Domain,
  Application et infrastructure ne référencent jamais `Microsoft.UI.Xaml`.
- Justification : Microsoft recommande WinUI 3 pour les nouvelles applications Windows ; il cible
  Windows 10 version 1809 et ultérieur, dont Windows 11.
- Compromis : WPF est plus ancien et mature ; WinUI ajoute packaging et dépendances. L’isolation de
  l’UI permet un remplacement futur sans réécrire le moteur.
- Conséquences : tests UI limités aux vues/adaptateurs ; ViewModels testables ; aucune I/O dans le
  thread UI ; actualisation de progression agrégée à fréquence bornée.
- Conditions de révision : échec POC de packaging/accessibilité/stabilité sur les OS retenus.

## ADR-023 — `HttpClient` partagé et flux sans mise en mémoire

- Date : 2026-08-03.
- Statut : ACCEPTÉE.
- Décision : utiliser un client long terme par profil réseau, `SocketsHttpHandler`,
  `PooledConnectionLifetime` configurable, `ResponseHeadersRead`, lecture en flux et annulation.
- Justification : évite l’épuisement des ports et renouvelle périodiquement la résolution DNS,
  conformément aux recommandations Microsoft.
- Interdictions : un `HttpClient` par segment, `ReadAsByteArrayAsync` pour gros fichiers, retry
  automatique d’une écriture non idempotente, décompression qui invalide les offsets.
- Révision : libcurl seulement si un écart reproductible et significatif reste insoluble.

## ADR-024 — Séparer le produit C# du prototype Python

- Date : 2026-08-03.
- Statut : ACCEPTÉE LORS DE LA VALIDATION G0.
- Contexte : le prototype Python possède déjà une base et des fichiers partiels incompatibles avec
  le modèle C# cible ; les documents mélangeaient leurs capacités.
- Options : partager silencieusement les données ; supprimer Python ; conserver deux piles isolées.
- Décision : C# est le produit actif. Python est une référence temporaire gelée hors correctifs de
  fixtures. Les répertoires et schémas restent distincts jusqu’à une migration explicitement conçue.
- Conséquences : aucune capacité Python ne valide C# ; une matrice de parité précède tout retrait ;
  aucun fichier partiel ou SQLite Python n’est ouvert silencieusement par C#.
- Risques : divergence temporaire R-022 et maintenance de deux fixtures.
- Révision : après parité fonctionnelle minimale et décision de migration/import/abandon.

## ADR-025 — Hôte de téléchargement utilisateur séparé

- Date : 2026-08-03.
- Statut : ACCEPTÉE ; HÔTE ASSEMBLÉ LE 2026-08-12, INSTANCE UNIQUE ET IPC RESTANTS.
- Problème : les transferts doivent survivre à la fermeture de l’interface sans donner plusieurs
  propriétaires à SQLite, aux fichiers temporaires ou à la planification.
- Options étudiées : moteur dans WinUI ; application tray unique ; service Windows ; processus
  headless par utilisateur avec WinUI cliente.
- Décision : un futur `DownloadHost` headless, lancé dans la session utilisateur, sera l’unique
  propriétaire du dépôt SQLite, des fichiers et du scheduler. WinUI sera une cliente. Il n’y aura
  ni service Windows ni élévation. Une exclusion mutuelle par utilisateur empêchera deux écrivains ;
  l’IPC local sera authentifié et limité par ACL à cet utilisateur.
- Avantages : fermeture UI sans arrêt du moteur, isolation et testabilité, privilèges minimaux.
- Inconvénients : protocole IPC versionné, cycle de vie et reconnexion à concevoir.
- Conséquences : `Application` reste une bibliothèque headless ; aucune vue ne touche au stockage.
  `DownloadOrchestrator` exécute maintenant une tâche neuve via des ports. Le processus
  `DownloadHost`, l’instance unique, la fermeture UI et la récupération restent requis avant l’UI.
- Risques : hôte orphelin, usurpation IPC, divergence de version.
- Conditions de révision : impossibilité prouvée de satisfaire le packaging ou l’accessibilité.

## ADR-026 — Session HTTP partagée et connexion liée à la validation d’adresse

- Date : 2026-08-03.
- Statut : ACCEPTÉE ; IMPLÉMENTÉE POUR LE PROFIL DIRECT, VALIDATION PROXY/NAT64 RESTANTE.
- Problème : une validation d’URL avant connexion ne suffit pas contre le rebinding DNS ; les
  redirections et profils proxy doivent conserver les mêmes garanties.
- Options étudiées : client par requête ; factory générique ; client long terme par profil ; proxy
  système implicite ou profil explicite.
- Décision : le futur hôte possède un `HttpClient`/`SocketsHttpHandler` long terme par profil réseau.
  Chaque redirection est manuelle. Le connecteur résout, filtre puis se connecte à l’adresse validée,
  de sorte que l’adresse réellement utilisée est celle contrôlée. La version initiale utilise
  `UseProxy=false`; un proxy ne pourra être activé que par un profil explicite et audité.
- Avantages : réutilisation des connexions, renouvellement DNS maîtrisé, frontière SSRF testable.
- Inconvénients : connecteur personnalisé et cas IPv4/IPv6/NAT64/proxy plus complexes.
- Conséquences : G2 injecte le client possédé par la composition et connecte chaque nouveau socket
  à une IP résolue et filtrée. Aucun support proxy ne sera affirmé avant tests dédiés.
- Risques : R-004 reste critique jusqu’aux tests de rebinding et d’adresse connectée.
- Conditions de révision : incompatibilité réseau reproductible ou garantie équivalente plus simple.

## ADR-027 — SQLite direct avec `Microsoft.Data.Sqlite`

- Date : 2026-08-03.
- Statut : ACCEPTÉE ; IMPLÉMENTATION INITIALE PARTIELLE EN G2.
- Problème : persister états, intentions et migrations sans masquer les transactions nécessaires à
  la reprise.
- Options étudiées : fichiers JSON ; SQLite natif artisanal ; EF Core ; `Microsoft.Data.Sqlite`.
- Décision : utiliser `Microsoft.Data.Sqlite` 10.0.10 en ADO.NET direct, sans EF Core. Le `DownloadHost`
  sera l’unique écrivain. Chaque connexion active `foreign_keys=ON`; le dépôt utilise WAL et
  `synchronous=FULL`. Les migrations sont ordonnées, transactionnelles, versionnées et associées à
  une empreinte ; une sauvegarde précède toute migration incompatible.
- Avantages : faible couche d’abstraction, transactions explicites, dépendance Microsoft MIT.
- Inconvénients : SQL et mapping à maintenir ; discipline de migration indispensable.
- Conséquences : `Microsoft.Data.Sqlite` 10.0.10 et le correctif natif SQLitePCLRaw 2.1.12 sont
  verrouillés. Les migrations v1/v2/v3, leurs checksums, la conservation d’une ligne v1 et un
  checkpoint avec identité sont testés ; crash/corruption/rollback restent obligatoires.
- Risques : verrouillage, disque plein, migration interrompue, écart base/disque.
- Conditions de révision : preuve que la charge ou le packaging ne respecte pas les objectifs.

## ADR-028 — MSTest.Sdk, Microsoft Testing Platform et NuGet verrouillé

- Date : 2026-08-03.
- Statut : ACCEPTÉE ET IMPLÉMENTÉE.
- Problème : le lanceur C# artisanal ne fournissait ni découverte standard, ni rapports, ni
  restauration reproductible.
- Options étudiées : xUnit, NUnit, MSTest classique, `MSTest.Sdk` avec Microsoft Testing Platform.
- Décision : `MSTest.Sdk` 4.3.2 avec Microsoft Testing Platform. Les tests Domain et Network sont
  séparés. La seule source est `nuget.org`, les paquets résident dans `.packages`, les fichiers
  `packages.lock.json` sont versionnés et la restauration normale emploie `--locked-mode`.
  La télémétrie CLI/test est désactivée. L’audit en ligne couvre les dépendances transitives ; la
  restauration hors ligne désactive seulement l’appel d’audit et ne vaut pas audit de sécurité.
- Avantages : outillage Microsoft standard, rapports/coverage disponibles, versions déterministes.
- Inconvénients : dépendances de test transitives et accès réseau requis pour actualiser/auditer.
- Conséquences : `eng/verify.ps1` est la commande canonique ; `-RefreshPackages` modifie les verrous,
  `-AuditPackages` exige le réseau. Les dépendances de test ne sont pas embarquées dans le produit.
- Risques : compromission de source, vulnérabilité transitive, télémétrie involontaire.
- Conditions de révision : maintenance insuffisante, faille non corrigeable ou incompatibilité SDK.

## ADR-029 — Finalisation par intention persistée et réparation

- Date : 2026-08-03.
- Statut : ACCEPTÉE PAR DÉLÉGATION DU PROPRIÉTAIRE ; IMPLÉMENTATION PARTIELLE.
- Problème : aucun commit atomique unique ne couvre SQLite et le renommage d’un fichier.
- Options étudiées : renommer puis mettre à jour ; mettre à jour puis renommer ; journal d’intention
  avec réparation ; copie vers une autre partition.
- Décision : synchroniser et vérifier le temporaire, persister l’état `FINALIZING` et la destination,
  renommer atomiquement sur le même volume, puis persister `COMPLETED`. Au démarrage, une routine
  idempotente inspecte base, temporaire et destination et termine ou annule prudemment chaque étape.
  Une destination sur un autre volume exige copie vers un temporaire local, synchronisation,
  vérification puis renommage ; elle n’est jamais présentée comme atomique.
- Avantages : chaque frontière de crash est observable et réparable.
- Inconvénients : états intermédiaires, nettoyage et tests de chaos supplémentaires.
- Conséquences : modèle et protocole PR-030 à PR-043 devront couvrir tous les points d’interruption.
- Risques : collision de destination, antivirus/verrou, disque plein, double réparation.
- Conditions de révision : primitive de plateforme offrant une garantie mesurée supérieure.
- Mise en œuvre 2026-08-10 : `Finalizing` est persisté avant un move même volume sans écrasement,
  puis `Completed` après succès. La réparation traite les deux états non ambigus (temporaire seul ou
  destination seule) et bloque si les deux chemins existent ou manquent. Extension du 2026-08-11 : trois
  terminaisons subprocess prouvent la réparation après commit `Finalizing`, après move et après
  commit `Completed`, sans état final ambigu ni perte du contenu.

### Extension ADR-029 — collisions et copie inter-volume (2026-08-11)

La politique par défaut refuse toute collision. `KeepBoth`, demandé explicitement, cherche le premier
suffixe `nom (n).ext` absent et persiste ce chemin avec `Finalizing`; une course ultérieure reste
bloquée par le move sans écrasement. Entre racines différentes, Storage copie vers
`.wdm-finalizing-{downloadId}.tmp` sur le volume cible, synchronise, compare SHA-256, renomme localement,
revérifie puis supprime la source. La réparation remplace un transit partiel et accepte source plus
destination uniquement pour le protocole inter-volume si les deux hashes correspondent. Les tests
simulent deux volumes ; matériel réel, panne électrique et crash subprocess pendant copie restent.

## Extension ADR-011 — SHA-256 avant finalisation (2026-08-11)

Le SHA-256 du fichier exact est calculé en streaming avant l’intention `Finalizing`. Une empreinte
attendue fournie par une source de confiance est comparée en temps constant lorsqu’elle existe. Le
hash vérifié est persisté par la migration v3 et recalculé lors d’une réparation. L’empreinte distante
est désormais acquise automatiquement depuis les en-têtes HTTP et persistée par la migration v4 dans une
colonne dédiée, distincte du hash local. Le choix SHA-256 est donc accepté pour cette frontière ; les
empreintes partielles versionnées restent à concevoir.

## Extension ADR-020 — retransmission contrôlée et ordre normatif (2026-08-12)

ADR-020 reste proposée mais est désormais implémentée à l'essentiel. M-011 applique son préalable :
`ForcedResumeEngine` (Application) décide de la branche dans l'ordre normatif du cahier des charges —
Native Range, sondages courts, URL finale autorisée, nouveau lien légitime, recouvrement, retransmission
contrôlée, arrêt sûr — sans jamais contourner une protection. M-012 ajoute `ControlledRetransmissionEngine`
(comparaison continue du flux renvoyé depuis zéro, préfixe préservé, écriture reprise au premier octet
absent, arrêt sûr à toute divergence) et `EstimateCost` qui annonce le coût réseau total et exige un
consentement explicite au-delà du seuil configurable (opt-in). Restent le consentement UI (F-012) et les
preuves de bout en bout sur serveur réel (PR-060/061/062).

## Extension ADR-025 — processus hôte assemblé (2026-08-12)

Le projet src/WindowsDownloadManager.Host (assembly idm) assemble le moteur : DownloadHost exécute
le cycle complet (ajout, stratégie simple/segmenté/dynamique, vérification, finalisation, reprise au
checkpoint, décision des sept niveaux, retransmission contrôlée, priorités, débit par bloc) et
reconstruit le planning au démarrage via ListNonTerminalAsync. La CLI dd/un/cancel câble les
adadaptateurs réels. Les frontières restantes de l'ADR restent l'instance unique par utilisateur et l'IPC
authentifié, ainsi que la politique de débit par profil.

## 5. Résultat de la porte G1

Les ADR-025 à ADR-029 sont décidées. Seule ADR-028 est complètement appliquée ; les autres imposent
les frontières des prochains travaux. G2 commence par aligner la connexion réseau sur ADR-026, puis
implémente le propriétaire de stockage défini par ADR-025/027 et la finalisation ADR-029.
