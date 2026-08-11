# Sécurité

Version documentaire : 2.2  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : MODÈLE DE MENACES PROPOSÉ, PROTECTIONS PARTIELLES  
Responsable logique : Responsable sécurité  
Documents liés : `REGISTRE_DES_RISQUES.md`, `DEPENDANCES.md`, `ARCHITECTURE_TECHNIQUE.md`

## Sommaire

1. Actifs et frontières de confiance
2. Menaces réseau et fichiers
3. Secrets, navigateur et logs
4. Installation et mises à jour
5. Exigences et réponse aux incidents

## Périmètre et données sensibles

Le moteur traite des URL, chemins locaux et métadonnées HTTP. Il ne gère actuellement ni cookies,
ni identifiants, ni jetons. Les URL peuvent néanmoins contenir des signatures sensibles.

## Protections implémentées — périmètre exact

### Prototype Python

- Schémas limités à HTTP/HTTPS.
- Refus des identifiants intégrés dans l’URL.
- Résolution DNS et blocage initial des adresses non globales par défaut.
- Validation de l’URL finale après redirection, mais `urllib` peut avoir suivi la redirection avant
  cette validation : la requête vers la cible n’est donc pas prouvée sûre.
- Nettoyage des caractères Windows interdits dans le nom distant.
- Refus d’écraser un fichier temporaire ou final existant.
- Requêtes avec `Accept-Encoding: identity` afin que les positions correspondent aux octets.
- Messages HTTP sans URL complète dans `_safe_error`.
- Arrêt de reprise sur identité ou recouvrement incohérent.

### Produit C#

- Schémas absolus HTTP/HTTPS et identifiants intégrés refusés.
- Résolution et rejet conservateur des adresses privées/réservées avant chaque requête logique.
- Redirections automatiques désactivées ; chaîne manuelle bornée à dix.
- Sonde en streaming avec `Accept-Encoding: identity` et `206` strict pour `bytes 0-0`.
- Classification 416 vide, 429/5xx et annulation.

La connexion G2 utilise un `ConnectCallback` : chaque nouveau socket résout, rejette tout lot mixte
ou privé puis se connecte directement à l’IP acceptée. Le rebinding public→loopback est testé.

## Limites et menaces ouvertes

- Proxy et NAT64 restent non testés ; aucun proxy implicite n’est autorisé.
- SQLite conserve seulement l’URL sans query, fragment ni identifiants ; les secrets réutilisables
  n’ont pas encore de coffre et ne doivent donc pas être persistés.
- La migration v2 conserve aussi l’URL finale avec la même expurgation, ainsi que taille, ETag,
  Last-Modified et capacité Range. Aucun cookie, Authorization ou en-tête arbitraire n’est stocké.
- Le chemin temporaire local est persisté en clair dans la base utilisateur ; ACL Windows et
  chiffrement de la base restent à concevoir avant distribution multi-utilisateur.
- Le nom fourni explicitement par `--name` doit faire l’objet d’une validation renforcée.
- Aucune politique de permissions Windows ou chiffrement de la base n’est implémentée.
- Aucune analyse antivirus ou signature de code/installateur.
- L’audit NuGet connecté est actif ; les audits réseau, fichier et Windows complets restent à faire.

## Règles futures

Les cookies/jetons devront utiliser le stockage sécurisé Windows et être expurgés des journaux.
Toute intégration navigateur exigera consentement explicite, origine contrôlée et protocole minimal.
En cas de doute sur une identité ou un chemin, arrêter sans écrire.

## Procédure en cas de faille

Suspendre la fonction concernée, préserver les preuves non sensibles, enregistrer l’incident dans
le suivi et le registre des risques, ajouter un test de reproduction puis publier une correction
seulement après vérification.

## 1. Actifs et frontières de confiance

Actifs : fichiers utilisateur, temporaires, URLs signées, cookies/jetons futurs, base, réglages,
journaux, binaire et canal de mise à jour. Frontières : Internet↔moteur, extension↔hôte natif,
UI↔service/moteur, moteur↔disque et installateur↔système. Toute donnée franchissant une frontière est
non fiable jusqu’à validation.

## 2. Menaces et contrôles requis

| Menace | Contrôle cible | Validation |
|---|---|---|
| SSRF/rebinding DNS | IP validée à la connexion, redirects revalidés, proxy explicite | hôtes privés/mixtes/rebinding |
| Traversée de chemin | canonicalisation, dossier racine, nom séparé, liens/reparse points | corpus Windows |
| Exécutable malveillant | avertissement/type réel, jamais auto-exécuter | extensions doubles/MIME trompeur |
| Zip bomb/contenu actif | ne pas extraire automatiquement | fichiers adverses |
| Fuite de secret | DPAPI à étudier, redaction centralisée, durée minimale | tests de logs/base/crash dump |
| Extension compromise | origines/ID autorisés, schéma borné, aucune commande libre | fuzz messages |
| Dépendance compromise | versions épinglées, source officielle, SBOM/signatures | scan et revue publication |
| Mise à jour falsifiée | manifest+binaire signés, anti-downgrade, rollback | MITM/corruption/interruption |
| Élévation abusive | exécution utilisateur ; élévation seulement installateur | test compte standard |

## 3. Règles absolues

Ne jamais désactiver antivirus/pare-feu, exécuter automatiquement un téléchargement, accepter
`file:`/UNC par une URL réseau, contourner TLS/authentification/DRM, stocker un cookie en clair,
journaliser une URL signée entière, suivre une redirection vers une cible interdite ou écrire hors de
la destination validée. Une alerte d’intégrité arrête avant écriture/finalisation.

## 4. Installation, mise à jour et confidentialité

Signature de code et mises à jour, permissions minimales, manifeste de fichiers, désinstallation
sans toucher aux téléchargements par défaut. Aucune télémétrie obligatoire ; toute collecte doit être
documentée, minimale, consentie, consultable et révocable. Produire une SBOM avant distribution.

## 5. Réponse aux incidents

Classer gravité, désactiver la fonction vulnérable si possible, préserver preuves expurgées, informer
le propriétaire, corriger avec test, renouveler secrets/signatures si compromis, documenter version
affectée et vérifier l’absence de régression avant diffusion.

## État du client HTTP C# — 2026-08-03

Présent avant la seconde tranche T-017 : HTTP/HTTPS dans la fabrique, encodage `identity`, réponse
en streaming et validation stricte du sondage. La lacune SSRF décrite alors est remplacée par la mise
à jour ci-dessous. Le loopback reste autorisé uniquement via le validateur de test.

### Mise à jour réseau C#

La validation URL/DNS et la revalidation de chaque redirection sont maintenant implémentées. Le
handler interdit les redirections automatiques, évitant une requête avant contrôle applicatif.
Adresses privées, réservées, multicast, documentation et identifiants intégrés sont rejetés.

Mise à jour G2 : résolution et connexion sont liées pour toute nouvelle connexion directe. Un lot
contenant une adresse interdite est refusé en entier. Les tests couvrent IPv4/IPv6 publics de la
politique et rebinding vers loopback. Proxy, DNS public réel, TLS hostile et NAT64 restent à prouver ;
R-004 est réduit mais non clos.

## Incident de dépendance G2

La première restauration de `Microsoft.Data.Sqlite` a détecté la vulnérabilité élevée
GHSA-2m69-gcr7-jv3q dans SQLitePCLRaw 2.1.11 et a échoué grâce aux warnings traités en erreurs.
La version 2.1.12 a été épinglée, les verrous régénérés et l’audit connecté final ne signale plus de
paquet vulnérable. La version 2.1.11 ne doit pas être réintroduite.

## Porte de sécurité avant transfert C#

Avant toute utilisation d’URL externe avec écriture disque : prouver l’appel du validateur à chaque
saut, le refus public→privé, la liaison résolution/connexion, la politique proxy, les limites de
redirections/en-têtes, les noms hostiles et les réponses 200/206/416 malformées. Les tests loopback
contrôlés peuvent être utilisés pour développer le stockage avant cette porte, sans exposition externe.

## Contrôles G1

- Deux tests MSTest observent chaque saut et prouvent qu’une cible refusée ne reçoit aucune requête :
  BUG-001 est corrigée. La liaison DNS/connexion reste absente, donc R-004 demeure critique.
- NuGet est limité à la source officielle, verrouillé et mis en cache localement. L’audit connecté du
  2026-08-03, transitifs inclus, n’a signalé aucune vulnérabilité ; ce résultat n’est pas permanent.
- `eng/verify.ps1` désactive la télémétrie .NET et Microsoft Testing Platform. Les dépendances de test
  ne sont pas distribuées avec le produit.
- Le futur IPC doit authentifier l’utilisateur et appliquer des ACL ; aucun service élevé n’est prévu.

## Contrôles du flux de transfert G2

Le transfert refait la validation d’URI avant chaque requête et chaque redirection, sans redirection
automatique ni décompression. Une ressource Range exige `206` et un `Content-Range` ouvert exact ;
une ressource non-Range exige `200`. Un ETag fort observé devient `If-Match`, sinon Last-Modified
devient `If-Unmodified-Since`. Toute longueur contradictoire arrête avant `VERIFYING`.

Le temporaire d’un téléchargement neuf est créé exclusivement et ne peut ni être le chemin final ni
écraser un fichier existant. Ces protections sont testées en loopback et sur fichier local. Elles ne
valident pas encore les reparse points Windows, les ACL, le proxy, NAT64, TLS public hostile, les
limites d’en-têtes ou les noms distants adverses ; la porte d’exposition externe reste partielle.

Avant création du temporaire, l’orchestrateur persiste atomiquement le chemin et l’identité. Un
échec du checkpoint empêche la création. Le dépôt refuse une identité partielle et l’index SQLite
interdit qu’un même chemin temporaire non nul appartienne à deux tâches. Ces protections préparent
la reprise, mais ne valident pas encore la présence, la nature ou les reparse points du fichier au
redémarrage.

La réconciliation locale de démarrage ouvre maintenant le chemin absolu uniquement avec
`FileAccess.Read`. Un fichier ou dossier réellement absent est classé explicitement ; verrou,
permission et autre erreur d’I/O ne sont pas assimilés à une absence et arrêtent le diagnostic.
Le service ne modifie ni fichier, ni agrégat, ni SQLite. Nature du fichier, reparse points, ACL et
changement concurrent entre inspection et action restent non validés ; aucune action réparatrice
n’est donc encore autorisée.

La réconciliation distante réutilise la sonde réseau sécurisée : validation de chaque URI et
redirection, connexion liée à l’adresse filtrée, réponse en en-têtes et aucune ouverture du port de
contenu. Query, fragment, nom d’utilisateur et mot de passe sont retirés des URI présentes dans le
résultat. Une preuve disparue est classée insuffisante et une contradiction arrête le diagnostic.
Cette protection ne prouve pas encore proxy/NAT64, recouvrement binaire ou identité par hash.

L’évaluateur combiné n’effectue aucune I/O et ne reçoit ni URI ni chemin nouveau : il conserve les
diagnostics déjà expurgés, refuse des identifiants de tâches différents et cumule tous les motifs de
blocage. Un fichier plus court, une queue non confirmée, une preuve distante insuffisante, une
contradiction ou une perte de Range ne peut donc pas être masqué par un autre résultat. Le statut
favorable autorise seulement une future lecture de recouvrement, jamais une mutation ou une reprise.

Le recouvrement ouvre le temporaire en lecture avec partage d’écriture refusé pendant la capture et
lit au maximum 64 Kio. La requête distante est une plage fermée exacte avec `identity`, validateur
fort ou date connue, revalidation SSRF de chaque redirection et contrôle strict de `206`,
`Content-Range`, longueur et fin de corps. Une cible redirigée refusée n’est pas contactée. Aucun
octet comparé n’est exposé dans le résultat ou persisté.

Une correspondance n’élimine pas la course après fermeture des handles : une future reprise ou
mutation devra reprendre les verrous et revalider les préconditions. Proxy/NAT64, modification
distante immédiatement après la plage et reparse points restent ouverts.

Le coordinateur applique un court-circuit de sécurité après l’inspection locale : métadonnées ou
fichier absents, fichier plus court ou queue non confirmée empêchent toute sonde distante. Après une
analyse distante bloquante, aucune plage n’est lue. L’annulation est vérifiée avant chaque nouvelle
étape réseau. Le résultat ne contient ni octets, ni secret, ni autorisation durable de reprise.

Les décorateurs de faute sont privés au projet d’intégration et ne sont pas accessibles au produit.
Les chemins sont créés dans un répertoire temporaire isolé et aucune donnée sensible n’est utilisée.
Le banc ne prouve ni résistance à une corruption SQLite, ni dump de crash, ni panne matérielle.

L’hôte subprocess n’accepte que dix noms d’énumération et un UUID valide. Les chemins sont
normalisés par `Path.GetFullPath` et proviennent d’un répertoire temporaire contrôlé par le parent.
Le lancement désactive le shell, masque la fenêtre et ne transporte aucun secret. Le mécanisme de
terminaison est absent des assemblies du produit.

## Reprise et finalisation — contrôle du 2026-08-10

La reprise réexécute validation URI, analyse distante et recouvrement avant toute écriture. Le flux
repris conserve `Accept-Encoding: identity` et les validateurs HTTP. La finalisation exige des chemins
absolus, refuse l’écrasement et les volumes différents, et bloque si l’état disque est ambigu. Le
verrou actuel est limité à une instance ; l’exclusion mutuelle inter-processus du futur hôte reste
une exigence de sécurité et d’intégrité ouverte.

Les trois frontières de finalisation réutilisent exclusivement les adaptateurs réels Storage/SQLite.
Elles ne reçoivent aucun secret, refusent l’écrasement et vérifient le contenu après réparation.
