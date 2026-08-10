# Règles de codage

Version documentaire : 2.2  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : OBLIGATOIRE — C# CIBLE ACTIVE, PYTHON RÉFÉRENCE GELÉE  
Responsable logique : Responsable technique  
Documents liés : `ARCHITECTURE_TECHNIQUE.md`, `INSTRUCTIONS_IA.md`, `SECURITE.md`

## Sommaire

1. Langage et structure
2. Fiabilité et sécurité
3. Async, annulation et concurrence
4. Erreurs et logs
5. Tests, qualité et Git

## Langage et structure

- Produit actif : C# sur .NET 10, nullable activé et avertissements traités comme erreurs.
- Prototype de référence : Python 3.11 minimum, gelé hors correctifs de fixtures/parité approuvés ;
  annotations de types pour les interfaces publiques modifiées.
- Noms de code en anglais, messages utilisateur en français.
- Séparer Domain, Application et adaptateurs Network/Storage/Persistence ; aucune capacité Python ne
  doit être copiée sans contrat et test C# correspondants.
- Ne pas ajouter de dépendance sans mise à jour de `DEPENDANCES.md` et analyse sécurité/licence.

## Fiabilité

- Ne jamais avancer `confirmed_bytes` avant écriture, `flush` et `fsync`.
- Ne jamais écrire une reprise sans valider statut et `Content-Range`.
- Utiliser le fichier temporaire ; le nom final est réservé à la finalisation atomique.
- Refuser de mélanger des identités distantes incompatibles.
- Ne jamais écraser silencieusement une destination existante.

## Sécurité et journaux

- HTTP/HTTPS uniquement ; bloquer le réseau privé par défaut.
- Ne pas journaliser cookies, mots de passe, jetons ou URL signées complètes.
- Borner les messages issus d’exceptions externes.
- Aucun contournement d’accès, DRM ou limitation de service.

## Tests

- Python : tests nommés `test_<comportement>` dans `tests/`.
- C# : noms décrivant le comportement ; projets Domain/Application/Network/Storage/Persistence et
  intégration séparés.
- Une correction de reprise doit avoir un scénario reproduisant l’interruption ou l’incohérence.
- Ne déclarer un résultat que si la commande et son résultat ont été observés.
- Exécuter les vérifications de la pile modifiée. Pour C#, build Release et tests concernés ; pour
  Python, `python -m compileall -q idm tests` et `python -m unittest discover -v`.
- Utiliser MSTest/Microsoft Testing Platform ; aucun total de tests n’est codé dans un lanceur.

## Documentation obligatoire

Avant toute modification, lire les 16 documents dans l’ordre prescrit par le propriétaire. Après
toute modification, ajouter une entrée sans effacement dans `SUIVI_DEVELOPPEMENT.md`, actualiser
l’état et la feuille de route lorsque concernés, puis contrôler les 16 documents.

## 3. Async, annulation et concurrence

Toute I/O cible est asynchrone avec jeton d’annulation propagé et délai explicite. Aucun blocage
synchrone sur une tâche asynchrone dans l’UI. Un verrou protège un invariant identifié, reste court
et n’englobe ni réseau ni disque lent. Une ressource possède un propriétaire et une durée de vie
déterministes. Retries idempotents uniquement ; gigue et `Retry-After` obligatoires.

## 4. Erreurs, résultats et logs

Pas de `catch` vide, exception avalée ou booléen ambigu. Utiliser résultats typés pour erreurs métier
attendues et exceptions pour défaillances exceptionnelles. Chaque log a événement stable, task ID,
état avant/après et contexte expurgé ; jamais cookie, Authorization, mot de passe ou URL signée.

## 5. Qualité, formatage et Git

Interdits : fonction géante, logique dupliquée, valeur magique, dépendance UI→stockage, chemin concaténé
sans API dédiée, progression avant disque et `TERMINE` non vérifié. Les outils .NET précis restent à
choisir ; ils devront inclure formatage déterministe, warnings élevés, analyse nullable/sécurité et
tests unitaires/intégration. Commits atomiques `type(T-ID): description`, code et docs ensemble ;
revue obligatoire pour persistance, sécurité, concurrence et finalisation.

Le dépôt Git local est obligatoire à partir de G0. Un changement non commité doit rester clairement
identifiable dans `git status`; aucune IA ne réécrit ou ne supprime des changements qu’elle n’a pas
créés. La source NuGet unique et les verrous ADR-028 sont obligatoires ; toute alerte d’audit bloque
l’ajout ou la mise à jour concernée.

## 6. Règles C#/.NET 10 retenues

- `Nullable` et analyseurs activés ; warnings traités comme erreurs dans le code produit.
- Types immuables et `record` pour valeurs métier ; interfaces aux frontières, pas pour chaque classe.
- `async` jusqu’au bord, suffixe `Async`, `CancellationToken` obligatoire sur toute I/O longue.
- `ConfigureAwait(false)` dans les bibliothèques lorsque pertinent ; aucun `.Result`/`.Wait()` UI.
- `HttpClient` réutilisé ; flux lus avec `ResponseHeadersRead`, buffers bornés et mutualisés.
- `Span`/`Memory` seulement avec mesure ou simplification réelle ; aucune optimisation non profilée.
- Tests de transitions, invariants et récupération avant développement visuel avancé.

## Règles de tests et de paquets G1

- Utiliser `MSTest.Sdk` et Microsoft Testing Platform ; séparer les tests par frontière Domain/Network.
- Nommer les méthodes `Comportement_Condition_Résultat` et isoler tout réseau dans un serveur loopback.
- Exécuter `eng/verify.ps1`; aucun résultat n’est déclaré sans le décompte réussi/échoué/ignoré.
- Conserver les `packages.lock.json`; toute actualisation passe par `-RefreshPackages` et revue du diff.
- La télémétrie CLI et du runner reste désactivée ; l’audit connecté doit inclure les transitifs.

## Règles réseau et durabilité G2

- La composition possède le `HttpClient`; analyseurs et transferts ne le détruisent pas. Toute
  nouvelle connexion résout, filtre puis utilise directement l’IP acceptée.
- Aucun proxy implicite. Un profil proxy exige ADR, authentification et tests SSRF dédiés.
- Le writer retourne une progression confirmable seulement après écriture et flush disque réussi.
- Toute URL persistée perd query, fragment et identifiants tant qu’un coffre Windows n’est pas conçu.
- Toute migration a version et SHA-256 ; un checksum différent provoque un arrêt sûr.
- Une migration additive conserve les anciennes lignes ; une ligne d’ancienne version peut avoir
  des métadonnées nouvelles nulles, mais un ensemble de reprise partiel est rejeté comme incohérent.
- Chemin temporaire et identité distante sont persistés ensemble avant création du fichier ; un
  échec de transaction ne doit laisser aucun nouveau temporaire orphelin.
- Un téléchargement neuf prépare son temporaire par création exclusive avant toute écriture.
- Une réconciliation de démarrage commence en lecture seule et renvoie un résultat typé. Seules les
  erreurs explicites « fichier/dossier absent » peuvent produire la classification `Absent` ; verrou,
  permission et autres erreurs d’I/O doivent remonter sans mutation.
- Une position sûre calculée par `min(checkpoint, longueur)` reste un diagnostic tant que l’identité
  distante et la politique de réparation ne sont pas validées.
- Une comparaison distante de reprise utilise le port d’analyse, jamais le port de flux. Les URI
  retournées dans un diagnostic sont expurgées de query, fragment et identifiants.
- Toute valeur persistée connue devenue différente est contradictoire ; toute valeur connue devenue
  absente est une preuve insuffisante. Une URL seule ou un ETag faible seul n’autorise pas la reprise.
- La compatibilité distante exige au minimum un ETag fort identique ou le couple taille et
  Last-Modified identique ; la perte de Range possède une classification distincte.
- La composition des diagnostics de récupération reste une fonction Application pure : aucun port,
  aucune I/O et aucune mutation. Elle refuse des IDs différents et agrège tous les motifs bloquants.
- Seul `TemporaryFileMatchesCheckpoint` avec `Compatible` peut produire
  `ReadyForOverlapVerification`; ce statut autorise uniquement le prochain diagnostic de
  recouvrement, jamais une reprise ou une troncature.
- Le recouvrement lit au plus 64 Kio se terminant à la position sûre. Les ports local et distant
  doivent retourner exactement la plage demandée ; toute longueur inattendue provoque un arrêt sûr.
- La requête distante de recouvrement utilise une plage fermée, `identity`, les validateurs connus,
  les contrôles SSRF/redirections et un `206 Content-Range` exact. Le temporaire est ouvert en lecture
  sans partage d’écriture pendant la capture.
- `Match` reste un diagnostic périssable : une future mutation/reprise doit revalider sous son propre
  protocole afin de traiter la course après fermeture des lectures.
- Un coordinateur de récupération exécute strictement local → distant → décision → recouvrement.
  Il réutilise la règle pure de blocage local, court-circuite avant réseau, propage l’annulation entre
  les étapes et laisse null tout résultat d’une étape non exécutée.
- L’orchestrateur confirme un bloc dans le domaine et SQLite uniquement après la frontière retournée
  par le writer durable ; toute frontière différente de la longueur attendue est une erreur.
- Les injections de faute utilisent des décorateurs de ports limités aux projets de test. Aucun
  `if test`, délai artificiel ou arrêt volontaire ne doit contaminer le code de production. Une
  exception simulée doit être distinguée d’une terminaison brutale de processus.
- Un hôte de crash doit rester un exécutable de support non référencé par le produit, accepter
  uniquement des valeurs de frontière connues et des chemins absolus fournis par le test, disposer
  d’un délai parent borné et être terminé en cas de dépassement.
- Une frontière multi-blocs doit cibler explicitement le numéro de flush ou de checkpoint attendu,
  préserver les checkpoints antérieurs et vérifier le contenu complet restauré, pas seulement sa taille.
- Une frontière pré-écriture doit tuer avant l’appel de l’adaptateur ciblé et prouver simultanément
  longueur, contenu, checkpoint et classification ; elle ne doit pas être décrite comme écriture partielle.
- Un flux HTTP de transfert doit conserver `identity`, revalider les redirections et contrôler le
  statut, `Content-Range`, la longueur et les validateurs observés avant confirmation.

## Extension reprise/finalisation — 2026-08-10

- Une reprise ne mute qu’après un diagnostic local/distant et un recouvrement sans blocage.
- Le premier offset réseau et disque d’une reprise est toujours `confirmed_bytes`.
- `Finalizing` est persisté avant le move ; `Completed` seulement après sa réussite.
- Une finalisation ne doit jamais écraser une destination et bloque tout état de réparation ambigu.
- Un move qualifié d’atomique doit rester sur le même volume ; toute copie inter-volume est un autre protocole.
