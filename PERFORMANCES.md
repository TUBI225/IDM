# Performances

Version documentaire : 2.2  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : OBJECTIFS PROPOSÉS, MESURES INSUFFISANTES  
Responsable logique : Responsable performances  
Documents liés : `Cahier_des_charges.md`, `ARCHITECTURE_TECHNIQUE.md`, `REGISTRE_DES_RISQUES.md`

## Sommaire

1. Principes et objectifs
2. Paramètres observés
3. Bancs d’essai
4. Métriques et seuils
5. Règles d’optimisation

## Objectifs

Les objectifs chiffrés de démarrage, mémoire, CPU, disque et débit n’ont pas encore été décidés.
Le principe actuel est que l’intégrité prime sur le débit.

## Paramètres implémentés — prototype Python uniquement

- Bloc réseau/écriture : 1 Mio.
- Checkpoint SQLite et `fsync` : tous les 4 Mio, plus fin de flux/pause.
- Zone de recouvrement : 64 Kio.
- Lecture SHA-256 finale : blocs de 1 Mio.
- Connexions par téléchargement : 1.
- Backoff : puissance de deux plafonnée à 60 secondes, avec légère gigue.

Le moteur C# transfère désormais en connexion unique avec blocs de 64 Kio et checkpoint durable à
chaque bloc. Il possède désormais un SHA-256 final streaming, sans cadence optimisée. Aucun benchmark C# de débit,
mémoire, CPU ou coût SQLite n’a été exécuté.

## Mesures observées — prototype Python

Le 2026-08-03, la suite de trois tests sur serveur HTTP local et fichier synthétique de 16 Mio a
pris 2,746 secondes au total dans l’environnement Codex Windows/Python 3.12. Cette durée inclut
création/arrêt du serveur et trois scénarios ; elle ne constitue pas un benchmark de débit.

## Tests non exécutés

- Débit HTTP/HTTPS distant : résultat inconnu.
- Mémoire maximale : résultat inconnu.
- CPU pendant SHA-256 : résultat inconnu.
- Temps de démarrage CLI : résultat inconnu.
- Fichiers de 1 Gio, 5 Gio et plus : résultat inconnu.
- Comparaison avant/après optimisation : aucune optimisation revendiquée.

## Protocole futur

Définir un environnement stable, tailles de fichiers, latence et débit réseau ; mesurer au moins
trois exécutions et publier moyenne, dispersion, CPU, mémoire et écritures disque.

## 3. Bancs d’essai proposés

Profils : SSD NVMe, HDD, disque USB ; réseau 10 Mbit/s instable, 100 Mbit/s, 1 Gbit/s faible latence ;
fichiers déterministes 1 Mio, 100 Mio, 1/10/100 Gio ; serveurs simple, Range correct, throttlé et
erreurs injectées. Relever OS/build, CPU, RAM, système de fichiers, antivirus, runtime et hash.

## 4. Métriques et seuils provisoires à valider humainement

| Métrique | Cible proposée | Alerte régression |
|---|---|---|
| Démarrage à froid UI | ≤ 2 s machine de référence | +20 % vs baseline |
| Ajout d’URL hors réseau | ≤ 100 ms | +20 % |
| RAM moteur simple | ≤ 150 Mio hors UI | +20 % ou croissance avec taille fichier |
| Buffers | bornés par connexions | toute croissance non bornée |
| CPU transfert | médiane ≤ 10 % hors hash à débit de référence | +20 % |
| Débit | ≥ 90 % du client simple de référence si serveur/disque suivent | -10 % |
| Checkpoint | perte récupérable ≤ intervalle documenté | base en avance interdite |
| UI progression | 4 à 10 mises à jour/s | blocage > 200 ms |

Ces seuils sont des propositions, pas des résultats. Mesurer trois à cinq répétitions, médiane et
p95 ; garder les données brutes et comparer même machine/configuration. Une optimisation exige
profilage, baseline, changement isolé, mesure après et test d’intégrité complet.

## Porte de performance C#

Établir la première baseline seulement après la tranche C# à connexion unique : SSD/HDD, RAM, CPU,
débit, allocations, écritures disque et coût de synchronisation. La segmentation statique puis
dynamique n’est autorisée qu’après cette baseline et une preuve d’intégrité. Aucun gain de performance
n’est revendiqué pendant G0.

## Observation G1

Les 14 tests .NET se sont exécutés en 14,782 s dans l’environnement de développement. Cette durée
mesure le harnais loopback et le runner, pas le débit du produit : elle ne constitue ni baseline ni
amélioration de performance. `MSTest.Sdk` est une dépendance de développement sans impact runtime
attendu, mais aucune publication du produit n’a encore été mesurée.

## Observation G2

Le writer emploie un buffer de 64 Kio puis `FlushAsync` et `Flush(true)` à chaque appel de l’adaptateur.
Cette politique privilégie la preuve de durabilité et n’est pas encore une cadence de checkpoint
optimisée. SQLite utilise WAL, `synchronous=FULL` et pooling désactivé pour une durée de vie explicite.
Aucun débit, coût de flush, contention SQLite, RAM ou CPU n’a été mesuré : aucune amélioration de
performance n’est revendiquée.

## Observation G2 — orchestration

Le premier orchestrateur loue un buffer de 64 Kio dans `ArrayPool<byte>` et borne donc la mémoire de
lecture à un bloc par transfert. Dans cette tranche, chaque bloc provoque un flush disque puis une
transaction SQLite ; c’est un choix de preuve d’intégrité, pas une cadence optimisée. Les 37 tests
fonctionnels ne constituent pas un benchmark. Débit, allocations, CPU, nombre réel d’écritures et
coût du flush restent NON MESURÉS ; aucune amélioration de performance n’est revendiquée.

La migration v2 ajoute six colonnes et un index partiel unique, sans nouvelle I/O dans la boucle de
checkpoint : les métadonnées sont incluses dans les transactions déjà existantes. Aucun temps de
migration, volume de base ou régression n’a été mesuré. Les 42 tests fonctionnels ne constituent pas
une mesure de performance.

La réconciliation locale ajoute une ouverture en lecture et une lecture de longueur par tâche
inspectée. Aucun temps de démarrage, coût sur grand catalogue, contention antivirus ou débit n’a été
mesuré. Les 53 tests fonctionnels ne constituent pas un benchmark et aucune amélioration de
performance n’est revendiquée.

La réconciliation distante ajoute une sonde HTTP `bytes=0-0` par tâche évaluée. Latence de démarrage,
effet sur un grand catalogue, réutilisation de connexion et charge serveur ne sont pas mesurés. Les
64 tests fonctionnels ne constituent pas un benchmark ; aucune amélioration de performance n’est
revendiquée et un futur orchestrateur devra borner la concurrence de ces sondes.

L’évaluateur combiné parcourt deux énumérations et effectue des opérations sur drapeaux, sans I/O ni
allocation proportionnelle à la taille du fichier. Aucun benchmark, temps de catalogue ou comparaison
avant/après n’a été exécuté. Les 75 tests fonctionnels ne constituent pas une mesure de performance
et aucune amélioration n’est revendiquée.

Le recouvrement alloue au maximum deux buffers de 64 Kio et effectue une lecture locale puis une
requête HTTP bornée par tâche éligible. À la position zéro, aucune I/O n’est faite. Latence, pression
GC, concurrence sur grand catalogue et coût serveur ne sont pas mesurés. Les 93 tests fonctionnels
ne constituent pas un benchmark et aucune amélioration de performance n’est revendiquée.

Le coordinateur n’ajoute pas de copie binaire : il conserve des références aux résultats typés et
évite toute I/O distante sur blocage local. Ce gain logique n’a pas été mesuré sur un catalogue réel.
Les 101 tests fonctionnels attendus ne sont pas un benchmark ; temps, mémoire et charge serveur
restent NON MESURÉS, donc aucune amélioration chiffrée n’est revendiquée.

Le banc ajoute trois scénarios d’intégration avec création, flush, transaction, fermeture et
réouverture de petits fichiers locaux. Les 104 tests fonctionnels et leurs temps de runner ne
constituent pas un benchmark du produit. Aucun objectif de débit, latence SQLite ou coût de fsync
n’a été mesuré.

Les trois scénarios subprocess ajoutent le démarrage d’un runtime .NET et une réouverture SQLite.
Leur durée de test n’est pas un temps de démarrage produit ni une mesure de performance. Les
107 tests fonctionnels ne justifient aucune revendication de débit, mémoire ou latence.

Les trois scénarios multi-blocs ajoutent seulement 70 000 octets déterministes par subprocess et
portent la suite à 110 tests fonctionnels. Les durées 16,868 s pour l’intégration et 14,214 s pour la
solution ne sont pas comparables à un benchmark de transfert ; aucun gain de performance n’est déclaré.

La frontière pré-écriture porte la suite à 111 tests fonctionnels. Ses durées 17,648 s pour
l’intégration et 14,668 s pour la solution restent des temps de test, pas des mesures du produit.

La tranche reprise/finalisation porte la suite à 122 tests fonctionnels. Elle ajoute une sonde, une
lecture de recouvrement et un flux reprenant au checkpoint ; aucune mesure de débit, coût de `fsync`,
latence du move, mémoire ou performance sur gros fichier n’a été exécutée. Aucun gain de performance
n’est revendiqué.

Les trois scénarios subprocess de finalisation portent la suite à 125 tests fonctionnels. Ils
mesurent des propriétés de durabilité et non la latence de finalisation ; aucune conclusion de
performance ne peut être tirée de leur durée.

La tranche SHA-256 porte la suite à 136 tests fonctionnels. Le `FileStream` utilise un tampon de
128 Kio et une lecture séquentielle, mais aucun profil 1/10/100 Gio, coût CPU, débit disque ou impact
sur la durée de finalisation n’a été mesuré. R-010 reste ouvert et aucune performance n’est revendiquée.

La tranche collision/inter-volume porte la suite à 147 tests fonctionnels. Une finalisation entre
volumes lit et écrit le contenu complet puis effectue deux contrôles SHA-256 côté Storage, auxquels
s’ajoute la vérification Application. Le buffer de copie est borné à 128 Kio, mais aucun temps sur
deux disques, amplification I/O, CPU, mémoire ou gros fichier n’a été mesuré. Cette preuve est
fonctionnelle et aucune performance n’est revendiquée.

La tranche empreinte distante porte la suite à 225 tests fonctionnels. Le coût ajouté est l’extraction
d’en-têtes HTTP et une colonne SQLite supplémentaire ; il n’affecte pas le chemin de transfert. Aucun
profil 1/10/100 Gio, coût CPU, débit disque ou gros fichier n’a été mesuré pour cette tranche. R-010
reste ouvert et aucune performance n’est revendiquée.

Les tranches segmentation (statique puis dynamique) portent la suite à 241 tests fonctionnels. La
segmentation dynamique découpe la ressource en chunks : chaque connexion ouvre une plage bornée par
chunk, donc le nombre de connexions simultanées reste borné par le pool de workers et le volume
transféré par connexion s'équilibre naturellement. Aucun débit, gain multi-connexions, contention de
la file ou mesure sur gros fichier n'a été mesuré ; aucune performance n'est revendiquée.

La tranche reprise renforcée (M-011) porte la suite à 256 tests fonctionnels. Le moteur des sept
niveaux est une fonction de décision pure : aucun octet, réseau ni disque supplémentaire n'est
consommé pour choisir la branche de reprise. Aucune mesure de coût du moteur, de débit, de latence de
reprise ou de gros fichier n'a été exécutée ; aucune performance n'est revendiquée et R-010 reste
ouvert.

La tranche retransmission contrôlée (M-012) porte la suite à 271 tests fonctionnels. La comparaison
continue lit chaque octet du flux distant (le serveur renvoie depuis zéro) et réécrit uniquement au
premier octet absent : le coût réseau consommé est la longueur totale du fichier, indépendamment du
travail local préservé, et il est annoncé (`EstimateCost`) avant tout consentement. Aucune mesure de
débit, de coût CPU de la comparaison, de latence ou de gros fichier n'a été exécutée ; aucune
performance n'est revendiquée et R-010 reste ouvert.
