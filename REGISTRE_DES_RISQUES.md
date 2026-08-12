# Registre des risques

Version documentaire : 2.3  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : ACTIF  
Responsable logique : Chef de projet et propriétaires indiqués  
Documents liés : `SECURITE.md`, `PROTOCOLE_TEST_REPRISE.md`, `FEUILLE_DE_ROUTE.md`

## Sommaire

1. Méthode de cotation
2. Registre
3. Risques additionnels
4. Revue et acceptation

| ID | Description | Probabilité | Impact | Priorité | Prévention / détection | Secours / tests | Statut |
|---|---|---|---|---|---|---|---|
| R-001 | Mélange de deux versions distantes | Moyenne | Critique | Critique | C# compare, agrège puis vérifie 64 Kio ; hash/course action absents | PR-003 Python ; PR-052/061 C# partiels | Partiellement réduit C# |
| R-002 | Base en avance sur le disque après crash | Moyenne | Critique | Critique | Ordre flush→checkpoint et préparation avant fichier testés | PR-032/040 et crash réel C# | Partiellement réduit C# |
| R-003 | Serveur annonce ou renvoie une plage incorrecte | Moyenne | Élevé | Haute | Valider `206` et début de `Content-Range` | Arrêt sûr ; cas malformés à ajouter | Ouvert |
| R-004 | SSRF vers machine/réseau privé | Faible à moyenne | Critique | Critique | Socket lié à l’IP filtrée, lot mixte refusé, proxy off | Proxy, DNS public, IPv6/NAT64 | Réduit en direct, ouvert |
| R-005 | URL signée exposée dans SQLite/logs | Faible | Élevé | Haute | Query/fragment non persistés, erreurs expurgées | Coffre Windows à concevoir | Réduit, ouvert |
| R-006 | Disque plein ou erreur `fsync` | Moyenne | Élevé | Haute | Exception et progression non avancée | Test disque plein non exécuté | Ouvert |
| R-007 | Migration SQLite incompatible | Moyenne | Élevé | Haute | v1→v2 additive, checksums et conservation de ligne testés | interruption, backup et rollback | Partiellement réduit |
| R-008 | Boucle de reconnexion inadaptée / `Retry-After` non appliqué | Moyenne | Moyen | Normale | C# conserve le délai mais aucun RetryManager ne l’applique | PR-021/022 complets | Partiel |
| R-009 | Distribution dépendante d’un runtime non installé | Élevée | Élevé | Haute | SDK développeur local seulement ; publication à décider | Installation sur machine vierge | Ouvert |
| R-010 | Performances dégradées par `fsync`/SHA-256 | Moyenne | Moyen | Normale | Mesurer avant optimisation | Ajuster checkpoints sans compromettre intégrité | Ouvert |
| R-022 | Divergence entre prototype Python et produit C# | Élevée | Élevé | Haute | ADR-024, piles/données séparées, fixtures communes | Matrice de parité avant retrait Python | Ouvert |
| R-023 | Absence de versionnement Git et retour arrière | Moyenne | Élevé | Haute | Dépôt `main` initialisé ; commits atomiques code+docs | Baseline à committer après identité Git | Réduit, non clos |
| R-024 | Dépendance compromise ou télémétrie involontaire | Faible | Élevé | Haute | source unique, verrous, audit transitif, opt-out | revue verrous et audit connecté | Incident SQLite corrigé, surveillé |

## 1. Cotation et gouvernance

Probabilité et impact : faible/moyen/élevé/critique. La criticité combine les deux et ne peut être
abaissée que par preuve. Le propriétaire logique propose une mitigation ; seul le propriétaire du
projet accepte explicitement un risque critique résiduel. Revue à chaque jalon et incident.

## 3. Risques additionnels

| ID | Catégorie/cause | Prob. | Impact | Détection/prévention | Secours, tests, tâches | Propriétaire | Statut |
|---|---|---|---|---|---|---|---|
| R-011 | Panne électrique/écriture partielle | Moy. | Critique | ordre disque→base, journal/checksum | rollback et chaos PR | Stockage | Ouvert |
| R-012 | Disque externe retiré/dossier supprimé | Moy. | Élevé | monitor + erreurs I/O | pause, nouvelle destination, PR | Stockage | Ouvert |
| R-013 | Segments dupliqués/trous/chevauchements | Moy. | Critique | carte d’intervalles et invariants | test property/chaos M-009 | Moteur | Ouvert |
| R-014 | RAM/CPU excessifs sur gros fichiers | Moy. | Élevé | buffers bornés et métriques | banc 100 Gio Q-003 | Performance | Ouvert |
| R-015 | Trop de connexions/limitation serveur | Élev. | Moyen | départ modeste, 429, quotas | réduire connexions M-014 | Réseau | Ouvert |
| R-016 | Cookies/jetons/en-têtes divulgués | Moy. | Critique | DPAPI, liste blanche, redaction | rotation et audit Q-002 | Sécurité | Ouvert |
| R-017 | SQLite corrompue/migration interrompue | Moy. | Critique | v1/v2/v3 transactionnelles, checksums, WAL/FULL | corruption/crash/rollback M-005 | Persistance | Partiellement réduit |
| R-018 | Antivirus verrouille/quarantaine | Moy. | Moyen | erreurs explicites, fermeture handles | préserver état, tests W | Windows | Ouvert |
| R-019 | Installateur/mise à jour incomplète | Moy. | Élevé | signature, transaction, rollback | réparation Q-004 | Livraison | Ouvert |
| R-020 | Incompatibilité Windows/framework | Moy. | Élevé | matrice OS/architecture | support documenté D-004 | Windows | Ouvert |
| R-021 | Suppression accidentelle | Faible | Critique | actions séparées, confirmation, corbeille | récupération et audit | Produit | Ouvert |

Chaque risque doit être relié à des tests avant clôture. « Réduit » n’est pas « résolu » tant que la
mitigation n’a pas été vérifiée dans les environnements cibles.

## Mise à jour de plateforme — 2026-08-03 — note historique remplacée par G0

- État historique R-009 : le SDK .NET 10 installé localement réduit le blocage de développement,
  mais G0 précise qu’il ne réduit pas le risque de distribution utilisateur.
- R-020 reste ouvert : WinUI 3 est choisi, mais son POC packaging/accessibilité/OS n’est pas exécuté.
- État historique R-022 : divergence temporaire entre prototype Python et moteur C#. Probabilité
  élevée, impact élevé. Prévention : mêmes fixtures, hashes et protocoles ; retrait Python seulement
  après parité démontrée. Propriétaire : architecture. Statut : OUVERT.

- R-003 est réduit, non clos : le C# rejette maintenant un `206` décalé au sondage. Corps
  trop long/court, unité incorrecte, taille changeante et autres offsets restent à tester.
- État antérieur R-004 (remplacé par la révision ci-dessous) : validation SSRF/rebinding absente.

### Révision R-004 après T-017 — remplacée par la baseline G0

La validation SSRF et les redirections manuelles sont présentes. La précédente baisse de probabilité
est annulée par G0 : le test de redirection utilise un validateur permissif et ne prouve pas le refus
public→privé ; la résolution n’est pas liée à la connexion. Probabilité maintenue moyenne, impact
critique, jusqu’aux preuves rebinding/proxy/IPv6/NAT64 prévues par ADR-026.

### Révision G1 de R-004 et R-024

Les tests MSTest observent désormais deux validations de redirection et prouvent qu’une destination
refusée ne reçoit pas de requête. BUG-001 est corrigée, mais R-004 reste prioritaire : la résolution
filtrée n’est toujours pas liée à l’adresse utilisée par le transport. R-024 est réduit par la source
NuGet unique, les verrous, la désactivation de télémétrie et l’audit transitif du 2026-08-03 sans
vulnérabilité signalée. Il reste surveillé, car cet audit est ponctuel.

## Baseline de gouvernance G0

Le périmètre de chaque risque doit désormais être indiqué comme `PYTHON`, `CSHARP` ou `COMMUN` dans
sa description ou sa révision. Une mitigation ne réduit le risque d’une pile que si le test porte sur
cette pile. Les addenda historiques restent visibles, mais le tableau principal et la révision la plus
récente constituent l’état courant. R-022 est maintenant intégré au tableau principal ; R-023 couvre
le manque de versionnement découvert pendant l’audit.

## Révision G2 — R-002, R-004, R-005, R-007, R-017 et R-024

Le profil réseau direct connecte désormais le socket à l’adresse filtrée et le test de rebinding vers
loopback réussit. R-004 reste critique pour proxy, DNS public hostile et NAT64. Le writer et SQLite
existent isolément, mais aucun crash entre flush et transaction n’a été injecté : R-002 reste ouvert.
La migration v1 est transactionnelle et checksummée ; N-1, corruption et interruption restent dues.
Les query strings/fragments ne sont pas persistés, réduisant R-005.

Incident R-024 : l’audit a bloqué SQLitePCLRaw 2.1.11 pour GHSA-2m69-gcr7-jv3q. L’épingle 2.1.12,
les nouveaux verrous et l’audit final sans alerte corrigent l’exposition observée. Le risque de chaîne
d’approvisionnement reste surveillé.

## Révision G2 — orchestrateur neuf

L’ordre `écriture → Flush(true) → ConfirmPersistedBytes → SaveAsync` est désormais imposé par
`DownloadOrchestrator` et couvert par un test unitaire d’échec du writer : aucun octet non flushé
n’est confirmé. Un test d’intégration loopback relit le temporaire et l’état SQLite exact. R-002 est
donc réduit pour l’exécution normale, mais reste ouvert : aucun arrêt de processus n’a encore été
injecté entre flush et commit, et le schéma v1 ne conserve pas le chemin temporaire.

R-003 est réduit par le rejet d’une réponse de transfert qui ignore Range, d’une longueur changée,
trop courte ou trop longue. R-006 reste ouvert : création exclusive et absence d’écrasement sont
testées, mais disque plein, retrait et erreur de flush ne le sont pas sur un vrai volume.

## Révision G2 — persistance des métadonnées de reprise

La migration v2 additive conserve les lignes v1 et ajoute chemin temporaire et identité distante.
Le test v1→v2 prouve la conservation d’une tâche et les checksums restent vérifiés. R-007/R-017 sont
réduits pour cette montée de version normale, mais interruption réelle, sauvegarde et rollback ne
sont pas testés. Une identité ou un chemin partiel est rejeté, et un checkpoint de préparation en
échec ne crée aucun fichier : R-001/R-002 sont réduits sans être clos. Le chemin local désormais
persisté augmente la donnée privée présente dans SQLite ; permissions Windows et chiffrement restent
ouverts sous R-005/R-016.

## Révision G2 — réconciliation locale en lecture seule

Les cas métadonnées absentes, temporaire absent, longueur inférieure, égale et supérieure au
checkpoint sont maintenant classés sans écrire. La position diagnostique est toujours la borne basse.
R-002 et R-011 sont réduits pour la détection locale, mais restent ouverts : aucune troncature sûre,
comparaison distante, course fichier/inspection, interruption de processus ou reprise réseau n’est
encore testée. Un verrou ou une erreur d’accès remonte au lieu d’être classé absent, ce qui réduit le
risque de décision destructive erronée. Reparse points et ACL restent ouverts sous R-012/R-018/R-021.

## Révision G2 — réconciliation distante en lecture seule

La nouvelle sonde compare l’identité persistée à une observation actuelle et agrège les différences
d’URL finale expurgée, taille, ETag et Last-Modified. Un signal connu disparu devient « preuve
insuffisante », la perte de Range est séparée et aucune branche ne modifie la tâche ou le temporaire.
R-001 est réduit pour la détection déterministe d’une contradiction, mais reste critique : aucun
recouvrement binaire, nouveau lien légitime, course entre sonde et requête de reprise
ou test PR-052/061 complet n’est encore réalisé.

## Révision G2 — décision combinée en lecture seule

La décision de récupération agrège désormais sans priorité destructive les problèmes locaux et
distants. Un checkpoint en avance, une queue locale non confirmée, une identité contradictoire, une
preuve insuffisante ou la perte de Range restent tous visibles et bloquants. Le refus d’identifiants
de tâches différents évite une composition croisée. R-001/R-002/R-011 sont réduits pour cette étape
de décision, mais restent ouverts : aucun recouvrement, course entre diagnostic et action, mutation,
crash réel ou reprise réseau n’a été testé.

## Révision G2 — recouvrement binaire borné

Une décision éligible compare maintenant octet par octet une fenêtre terminale maximale de 64 Kio.
Le fichier est verrouillé contre les nouvelles écritures pendant sa capture ; le distant exige une
plage fermée, validateurs et réponse exacte. Les tests couvrent correspondance, divergence, changement
de longueur locale, corps distant court, mauvais `Content-Range`, redirections revalidées et cible
refusée non contactée. R-001/R-003/R-004 sont réduits pour cette lecture, sans être clos : la course
après fermeture des handles, proxy/NAT64, hash final et reprise réelle restent ouverts.

## Révision G2 — coordination diagnostique et court-circuit local

La séquence locale → distante → décision → recouvrement est désormais imposée par un seul cas
d’usage Application. Les blocages locaux et l’annulation arrêtent avant réseau ; les contradictions
distantes arrêtent avant lecture de recouvrement. R-001/R-002/R-003/R-011 sont ainsi réduits pour
l’ordre diagnostique, sans être clos : crash réel, course diagnostic/action, revalidation sous
verrou, réparation, hash final et reprise HTTP restent non prouvés.

## Révision G2 — fautes déterministes flush/checkpoint

Trois fautes injectées avec vrais adaptateurs prouvent qu’après réouverture SQLite n’est jamais en
avance sur le fichier : après flush ou avant commit, la base reste à 0 face à 5 octets durables ;
après commit, base et fichier valent 5. R-002/R-011 sont réduits pour ces frontières déterministes.
Ils restent ouverts car une exception contrôlée ne reproduit ni mort du processus, ni caches
matériels, ni panne électrique, ni écriture partielle du système de fichiers.

## Révision G2 — terminaison subprocess aux frontières

Un processus séparé est maintenant tué sans dérouler les `finally`, puis les artefacts sont rouverts
par le parent. Les trois scénarios mono-bloc reproduisent les mêmes états sûrs que les exceptions.
Trois scénarios supplémentaires tuent pendant le second bloc : SQLite restaure 65 536 avant son
second commit et 70 000 après, sans jamais dépasser le fichier. R-002/R-011 sont davantage réduits ;
une septième terminaison avant le deuxième appel disque restaure exactement 65 536/65 536. Ils
restent ouverts pour mort pendant écriture, caches matériels, panne électrique, reboot Windows et
écriture partielle réelle.

## Réduction de risques — reprise/finalisation du 2026-08-10

R-001, R-002 et R-011 sont réduits par une reprise réelle au checkpoint après recouvrement et par
l’ordre durable conservé sur les nouveaux blocs. R-021 est réduit par le refus d’écrasement et des
volumes différents. ADR-029 possède une première réparation idempotente de `Finalizing`. Ces risques
restent ouverts pour concurrence inter-processus, hash final absent, antivirus/verrou, copie
inter-volume, panne électrique et reboot Windows. Les trois crashs subprocess de finalisation du
2026-08-11 réduisent R-011/R-021 : intention seule, move seul et commit final convergent vers un
fichier exact et `Completed`.

Le SHA-256 streaming persisté réduit R-001/R-011 : une modification du temporaire ou de la destination
entre vérification et réparation bloque désormais la finalisation. R-010 reste ouvert faute de
mesure CPU/débit, et R-001 reste partiel : l’empreinte distante est désormais acquise automatiquement
et vérifiée à la finalisation, mais le recouvrement binaire, les courses et la carte officielle restent.

## Réduction de risques — collisions et finalisation inter-volume du 2026-08-11

R-006/R-011/R-012/R-021 sont réduits par le refus d’écrasement, `KeepBoth` explicite, un transit local
au volume cible, le flush, deux vérifications SHA-256 et la suppression tardive de la source. Les
tests couvrent transit partiel, destination divergente, source et destination coexistantes et copie
intégrée avec SQLite. Les risques restent ouverts faute de deux volumes physiques, disque plein,
reparse point concurrent, retrait de support, antivirus, panne électrique et crash subprocess pendant copie.


## Réduction de risques — moteur des sept niveaux de reprise du 2026-08-12

R-001 est davantage réduit : ForcedResumeEngine (M-011) décide de la reprise dans l'ordre normatif et
ne force jamais — une identité contradictoire, une preuve insuffisante ou un nouveau lien non validé
refusent la reprise et tombent en arrêt sûr (PR-052). Le risque reste ouvert pour les preuves de bout en
bout (PR-050/051/052), la retransmission réelle (M-012), les courses et la carte officielle.

## Réduction de risques — retransmission contrôlée du 2026-08-12

R-001 et LIM-002 sont davantage réduits : `ControlledRetransmissionEngine` (M-012) compare le flux
renvoyé depuis zéro aux octets locaux, préserve le préfixe identique, ne réécrit qu'au premier octet
absent et s'arrête immédiatement à toute divergence (PR-061), l'ancien partiel restant intact. Le coût
réseau total est annoncé avant exécution et un coût significatif exige un consentement explicite
(PR-062). Le risque reste ouvert pour l'intégration hôte, le consentement UI et les preuves sur serveur
réel (PR-060/061/062).
