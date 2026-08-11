# Modélisation des données

Version documentaire : 2.2  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : SCHÉMA PROTOTYPE OBSERVÉ, MODÈLE CIBLE PROPOSÉ  
Responsable logique : Responsable persistance  
Documents liés : `ARCHITECTURE_TECHNIQUE.md`, `SECURITE.md`, `DECISIONS_ARCHITECTURE.md`

## Sommaire

1. Schéma actuel
2. Sémantique des positions
3. Schéma cible
4. Contraintes et rétention
5. Migrations et récupération

## Données persistantes — prototype Python uniquement

Base par défaut : `.idm-data/downloads.sqlite3`.

Cette base n’est pas le schéma du produit C#. ADR-024 interdit au C# de l’ouvrir ou de modifier ses
fichiers partiels sans migration explicite. Le répertoire C# sera distinct et versionné après
ADR-027 ; son nom final n’est pas encore décidé.

### Table `downloads`

| Colonne | Type SQLite | Contraintes / valeur par défaut | Rôle |
|---|---|---|---|
| `id` | INTEGER | PRIMARY KEY | Identifiant local |
| `original_url` | TEXT | NOT NULL | URL donnée par l’utilisateur |
| `final_url` | TEXT | NOT NULL | URL après redirections |
| `destination` | TEXT | NOT NULL, UNIQUE | Chemin final |
| `temporary_path` | TEXT | NOT NULL | Chemin suffixé `.download` |
| `state` | TEXT | NOT NULL | Valeur de `DownloadState` |
| `total_size` | INTEGER | NULL autorisé | Taille distante connue |
| `confirmed_bytes` | INTEGER | NOT NULL, DEFAULT 0 | Octets synchronisés et reconnus |
| `etag` | TEXT | NULL autorisé | Validateur HTTP |
| `last_modified` | TEXT | NULL autorisé | Validateur HTTP secondaire |
| `attempts` | INTEGER | NOT NULL, DEFAULT 0 | Nombre d’échecs mémorisés |
| `error` | TEXT | NULL autorisé | Dernière erreur nettoyée |
| `created_at` | TEXT | DEFAULT CURRENT_TIMESTAMP | Création UTC SQLite |
| `updated_at` | TEXT | DEFAULT CURRENT_TIMESTAMP | Dernière écriture logique |

Index : `idx_downloads_state(state)`.

## Modèles en mémoire

- `RemoteInfo` : résultat d’analyse non persisté intégralement (`mime_type` et
  `supports_ranges` ne sont actuellement pas stockés).
- `DownloadTask` : projection d’une ligne `downloads`.
- `DownloadState` : énumération de la machine d’états.

## Données temporaires

Le fichier `<destination>.download` contient les octets reçus. La base peut être en retard, jamais
volontairement en avance. Au démarrage, toute zone au-delà de `min(confirmed_bytes, taille disque)`
est tronquée.

## Migrations et compatibilité

Il n’existe pas encore de système de versions/migrations. Le schéma est créé avec
`CREATE TABLE IF NOT EXISTS`, ce qui ne mettra pas à niveau une ancienne structure. Toute évolution
doit ajouter une migration testée et une stratégie de retour arrière avant publication.

## 2. Sémantique normative des positions

`requested` vient du Range ; `received` du flux ; `written` de l’appel disque réussi ; `confirmed`
après synchronisation durable ; `verified` après comparaison/hash. Toujours
`verified ≤ confirmed ≤ written ≤ received` pour une progression séquentielle. Pour les segments,
une carte de plages remplace un compteur global : additionner des octets ne prouve pas la continuité.

## 3. Schéma cible proposé

État G0 : **NON IMPLÉMENTÉ EN C#**. Les tables ci-dessous sont conceptuelles. Seules les entités
`downloads`, `segments`, `remote_identities`, `download_events` et `schema_migrations` sont candidates
pour la première tranche ; aucune création de table ne précède le choix du fournisseur et des règles
de durabilité/migration.

| Table | Objet / relations | Contraintes et index essentiels | Sensibilité/rétention |
|---|---|---|---|
| `downloads` | Tâche racine | UUID, état, destination unique active | URL potentiellement sensible ; historique paramétrable |
| `segments` | Plage `[start,end]` d’un download | FK cascade contrôlée, unique plage, index état | Conserver jusqu’à finalisation + diagnostic |
| `download_events` | Journal append-only | séquence par tâche, code/type/date indexés | Détails expurgés, rotation |
| `remote_identities` | Versions/validateurs | FK download, niveau de confiance/version règle | URLs/headers protégés |
| `retry_attempts` | Erreur et prochaine tentative | FK segment/download, index `next_at` | Rétention diagnostic bornée |
| `download_headers` | En-têtes autorisés | liste blanche, valeur chiffrée si secret | Suppression dès inutilité |
| `browser_sources` | Origine/page/extension | origine validée, extension ID | Donnée privée |
| `categories` | Règles de classement | nom unique, chemin validé | Durable |
| `settings` | Préférences versionnées | clé unique, type/schema | Durable, secrets exclus |
| `server_capabilities` | Observations temporaires | hôte + contexte + expiration | Cache, jamais vérité absolue |
| `file_verifications` | Hash/blocs/preuves | algorithme, offset, longueur | Jusqu’à purge historique |
| `recovery_sessions` | Audit de restauration | FK download, état/raison | Diagnostic borné |
| `schema_migrations` | Version appliquée | version PK, checksum | Permanent |

`segments` contient au minimum start/end inclusifs, requested/received/written/confirmed/verified,
état, tentatives, connexion propriétaire et hash partiel. Des contraintes interdisent positions
négatives, fin avant début et position au-delà de la fin. Les chevauchements exigent une règle de
vérification explicite, jamais une simple dernière écriture gagnante.

## 4. Suppression, confidentialité et sauvegarde

Supprimer une tâche active exige arrêt et confirmation. La suppression historique peut conserver le
fichier final ; la destruction du fichier est une action distincte. Les secrets sont chiffrés hors
des colonnes ordinaires et effacés à expiration. Avant migration : checkpoint WAL, sauvegarde,
espace libre, transaction, validation, puis suppression différée de la sauvegarde.

## 5. Migration et récupération

Chaque migration a numéro, checksum, sens montant, compatibilité minimale et procédure de retour.
Tester base vide, version N-1, interruption à chaque étape et données inconnues. Une migration
échouée bloque le moteur mais laisse les téléchargements/fichiers intacts et fournit une sauvegarde.

## 6. Dictionnaire détaillé des entités critiques

### 6.1 `downloads`

Clé UUID ; état, priorité, URLs protégées, destination canonique, noms proposé/final, taille nullable,
MIME, timestamps, stratégie, débit, prochaine tentative et erreur. Contraintes : taille positive,
état connu, destination active unique selon politique, aucun `TERMINE` sans vérification finale. Les
compteurs dérivés ne remplacent pas les plages.

### 6.2 `segments`

ID, FK tâche, début/fin inclusifs, positions demandée/reçue/écrite/confirmée/vérifiée, état, worker,
tentatives, hash et timestamps. Invariant : `0 ≤ start ≤ positions ≤ end+1`; un seul écrivain actif ;
version optimiste. Index `(download_id,state)` et `(download_id,start)`.

### 6.3 Identité et vérifications

`remote_identities` contient version, URL normalisée, domaine, taille, ETag fort/faible,
Last-Modified, Content-Disposition, MIME, hash officiel et date. Les redirections sont ordonnées.
`file_verifications` contient algorithme, offset, longueur, digest, source et résultat. Une valeur
sensible est chiffrée si réutilisable, hachée si seule la comparaison est requise.

### 6.4 Événements et retries

`download_events` est append-only : séquence, états avant/après, code, UTC et payload JSON borné sans
secret. `retry_attempts` contient catégorie, code HTTP/I/O, numéro, délai, `Retry-After`, échéance et
résultat. Aucun texte externe non borné ne devient log ou SQL.

### 6.5 Paramètres et capacités serveur

Les réglages ont clé connue, type, JSON validé, portée et version. Les capacités serveur ont une
expiration et un contexte (schéma, port, proxy, authentification) : elles ordonnent les sondages mais
ne dispensent jamais de valider la réponse actuelle.

## 7. Transactions et invariants

- Checkpoint : disque confirmé, puis transaction segments + événement + tâche.
- Pause : aucun worker actif, checkpoints persistés, puis `EN_PAUSE`.
- Finalisation : vérification, fichier fermé/renommé, état final réparable.
- Recovery : session ouverte avant toute troncature ou invalidation auditée.
- Échec de transaction : ancien état lisible, aucun fichier supprimé.

## 8. Formats d’échange

Native Messaging : `schemaVersion`, `messageId`, `type`, `timestamp`, `payload`, taille maximale.
Types initiaux : ajout, remplacement de lien, réponse d’état. Une version majeure inconnue est
rejetée ; l’évolution mineure suit une règle documentée. Les exports excluent secrets et historique
par défaut.

## 9. Projection distante C# actuelle

`RemoteResourceInfo` transporte URL originale/finale, taille nullable, nom proposé, MIME, ETag,
Last-Modified et capacité Range observée. Le sous-ensemble d’identité utile à la reprise est converti
en `RemoteIdentity` et persisté dans `downloads` par la migration v2. Nom proposé et MIME restent
éphémères ; aucun en-tête sensible n’est enregistré.

## 10. Compatibilité Python/C#

Les identifiants Python sont des entiers et le modèle C# cible envisage des UUID. URL, états et
sémantique de progression ne sont pas supposés compatibles. G1 doit choisir entre import contrôlé,
outil de migration ou abandon documenté. Jusqu’à cette décision : bases, temporaires, verrous et
répertoires séparés ; aucune conversion automatique ; sauvegarde obligatoire avant tout essai.

## 11. Contraintes de persistance décidées en G1

ADR-027 retient `Microsoft.Data.Sqlite` 10.0.10 en SQL direct, sans EF Core. Un seul `DownloadHost`
écrit. Toute connexion active les clés étrangères ; la base utilise WAL et `synchronous=FULL`.
`schema_migrations` devra stocker au minimum version, empreinte, date d’application et résultat. Une
migration incompatible exige sauvegarde et test de restauration. ADR-029 ajoute un état persistant
`FINALIZING` et les chemins temporaire/destination nécessaires à une réparation idempotente.
Le sous-ensemble v1 ci-dessous est maintenant implémenté ; les autres colonnes restent À VÉRIFIER.

L’orchestrateur G2 confirme `confirmed_bytes` après chaque flush durable et persiste l’état
`VERIFYING` en fin de flux exact. La migration v2 conserve maintenant chemin temporaire et identité
distante. Une réconciliation locale en lecture seule calcule `min(confirmed_bytes, taille disque)` et
classe l’écart sans persister de nouvelle donnée. Une réconciliation distante reconstruit ensuite en
mémoire une identité observée expurgée et la compare aux colonnes v2, toujours sans écriture.
La composition des diagnostics produit maintenant en mémoire une décision, des motifs cumulables et
la position sûre existante. Le recouvrement ajoute un résultat éphémère contenant statut, offset,
longueur, position sûre et longueur locale observée, mais aucun octet ni hash n’est persisté. Aucune
table, colonne, migration ou écriture n’est ajoutée. Troncature, reprise et audit persisté de
récupération restent absents.

Le coordinateur ajoute seulement `StartupRecoveryAssessmentResult`, objet éphémère qui conserve le
diagnostic local et, lorsqu’elles ont été exécutées, l’identité distante, la décision et la preuve de
recouvrement. Les champs des étapes court-circuitées sont nulls. Aucun nouveau format persistant,
champ JSON, table, colonne ou migration n’est introduit.

Le subprocess reçoit uniquement quatre arguments éphémères : frontière connue, UUID, chemin SQLite
et chemin temporaire. Aucun de ces paramètres n’est ajouté au schéma. Après terminaison mono-bloc,
les états persistants observés restent 0/5 avant commit et 5/5 après commit. Pendant le second bloc
d’un contenu de 70 000 octets, ils deviennent 65 536/70 000 avant le second commit et
70 000/70 000 après ce commit. Avant le deuxième appel disque, l’état observé est 65 536/65 536 ;
aucune table, colonne ou migration n’est ajoutée.

## 12. Schéma C# v1 réellement implémenté en G2

`schema_migrations(version INTEGER PK, checksum TEXT, applied_at TEXT)` puis
`downloads(id TEXT PK, original_url TEXT, destination_path TEXT COLLATE NOCASE,
state INTEGER, confirmed_bytes INTEGER CHECK >= 0, created_at TEXT, updated_at TEXT)` avec index
unique sur la destination. La migration 1 est transactionnelle et son texte possède un SHA-256.

Le dépôt restaure l’état et les octets confirmés, sérialise les écritures et configure WAL,
`synchronous=FULL`, `foreign_keys=ON` et un délai de verrou de 5 s. Query, fragment et identifiants
d’URL sont supprimés avant persistance. Les tables de segments, identités, événements, recovery et
finalisation ne sont pas encore créées ; aucune compatibilité N-1 n’est donc revendiquée.

## 13. Schéma C# v2 — métadonnées minimales de reprise

Migration additive et transactionnelle depuis v1 :

| Colonne | Type/contrainte | Sémantique |
|---|---|---|
| `temporary_path` | TEXT COLLATE NOCASE NULL | Chemin absolu du partiel ; unique lorsqu’il est non nul |
| `final_url` | TEXT NULL | URL finale expurgée de query, fragment et identifiants |
| `total_size` | INTEGER NULL CHECK ≥ 0 | Taille observée, inconnue si NULL |
| `etag` | TEXT NULL | ETag observé, fort ou faible conservé tel quel |
| `last_modified` | TEXT NULL | Date UTC au format rond-trip `O` |
| `supports_byte_ranges` | INTEGER NULL CHECK 0/1 | Capacité Range observée lors de l’analyse |

Une ligne v1 migrée possède ces six colonnes à NULL et reste restaurable comme tâche historique sans
métadonnées de reprise. Pour toute nouvelle préparation, `temporary_path`, `final_url` et
`supports_byte_ranges` sont obligatoirement présents ensemble ; le dépôt rejette un ensemble partiel.
L’orchestrateur enregistre cet ensemble en état `PREPARING` avant de créer le fichier. La migration
v1→v2 et la conservation d’une ligne existante sont testées ; interruption réelle et rollback de
fichier de base restent NON EXÉCUTÉS.

Le banc de fautes n’ajoute aucun schéma. Avant commit positif, une réouverture restaure
`confirmed_bytes = 0` même si le temporaire contient déjà 5 octets durables. Après commit, elle
restaure `confirmed_bytes = 5` avec un fichier de longueur 5. Les octets au-delà du checkpoint restent
une queue non confirmée à préserver ; aucune troncature automatique n’est encore implémentée.

Le scénario multi-blocs confirme la même règle au checkpoint suivant : après flush du second bloc ou
avant son commit, `confirmed_bytes = 65536` et le temporaire mesure 70 000 ; après commit, les deux
valent 70 000. Une mort avant le deuxième appel au writer restaure `confirmed_bytes = 65536` avec un
temporaire exact de 65 536 octets. Ces valeurs sont des observations de test, pas un nouveau format.

## Cycle de reprise et finalisation observé — 2026-08-10

Aucune migration n’est ajoutée. Une ligne v2 restaurée en `Downloading` conserve son chemin temporaire,
son identité et `confirmed_bytes`. Après diagnostic et recouvrement, les blocs repris font progresser
le même checkpoint durable jusqu’à `Verifying`. La finalisation réutilise les états existants :
`Verifying → Finalizing` est persisté avant le move, puis `Finalizing → Completed` après le move.
Une réparation de `Finalizing` considère temporaire seul et destination seule comme états non ambigus ;
les deux présents ou les deux absents restent bloquants.

Les terminaisons subprocess du 2026-08-11 observent les trois états attendus sans migration :
`Finalizing + temporaire seul`, `Finalizing + destination seule`, puis `Completed + destination seule`.
Les deux premiers convergent vers `Completed` après réparation ; le troisième est déjà terminal.

## Migration v3 — empreinte vérifiée (2026-08-11)

La migration additive v3 ajoute `downloads.verified_sha256 TEXT NULL`, borné à 64 caractères
hexadécimaux majuscules. Le champ reste nul avant vérification. Il est enregistré dans la même
transaction que l’état `Finalizing`, puis conservé en `Completed`. Une ligne v2 migre avec une valeur
nulle ; une ancienne intention sans hash est restaurable mais sa réparation s’arrête prudemment.
