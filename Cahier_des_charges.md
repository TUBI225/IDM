# Cahier des charges — IDM Engine

Version documentaire : 2.1  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : BASELINE G0 APPROUVÉE — EXIGENCES À AFFINER PAR JALON  
Responsable logique : Propriétaire produit  
Documents liés : `FEUILLE_DE_ROUTE.md`, `ARCHITECTURE_TECHNIQUE.md`, `SECURITE.md`, `PERFORMANCES.md`

## Sommaire

1. Vision et principes
2. Utilisateurs et périmètre
3. Exigences fonctionnelles
4. Reprise renforcée
5. Cas d’utilisation
6. Exigences non fonctionnelles
7. Interface et exploitation
8. Contraintes légales et exclusions
9. Critères d’acceptation

## Vision

Construire pour Windows un système fiable de transfert HTTP/HTTPS capable d’observer le serveur,
de choisir une stratégie, de conserver une progression récupérable et de reprendre sans mélanger
deux versions d’un fichier. L’intégrité prime sur la vitesse.

## Utilisateurs visés

Utilisateurs Windows téléchargeant des fichiers volumineux ou sur des connexions instables.

## Exigences fonctionnelles

- Analyser l’URL, les redirections, la taille, le nom, le type MIME, `ETag`, `Last-Modified` et
  le support réel des plages.
- Télécharger d’abord en connexion unique, puis ajouter ultérieurement la segmentation adaptative.
- Écrire dans un fichier temporaire et mémoriser dans SQLite uniquement les octets synchronisés.
- Permettre pause, reconnexion et reprise après fermeture ou redémarrage.
- Refuser une reprise lorsque l’identité distante ou la zone de recouvrement ne correspond pas.
- Vérifier taille, lisibilité et empreinte avant finalisation atomique.
- Expliquer les états et erreurs sans exposer de secrets.
- Ajouter ultérieurement file d’attente, priorités, limitation de débit, interface Windows et
  intégration Chrome/Edge.

## Contraintes et limites

- Protocoles initiaux : HTTP et HTTPS uniquement.
- Aucun contournement d’authentification, de DRM, de limitation payante ou de protection technique.
- Les adresses locales/privées sont interdites par défaut.
- Un serveur qui refuse `Range` ne peut pas être forcé à reprendre réellement.
- État des implémentations : le prototype Python possède un moteur CLI expérimental à connexion
  unique avec reprise partielle ; le produit C# cible possède le domaine, l’analyse et le transfert
  HTTP neuf, un writer temporaire durable et un dépôt SQLite v3. Le chemin temporaire et l’identité
  distante sont persistés. Une réconciliation de démarrage en lecture seule classe maintenant les
  métadonnées ou temporaires absents et les longueurs plus courtes, égales ou plus longues que le
  checkpoint. Une seconde réconciliation réanalyse le distant par une sonde d’en-têtes, compare URL
  finale expurgée, taille, ETag, Last-Modified et capacité Range, puis classe correspondance, preuve
  insuffisante, perte de capacité ou contradiction. Un évaluateur pur combine maintenant les deux
  diagnostics dans une décision unique : tous les motifs de blocage sont cumulés et seul un temporaire
  exactement au checkpoint avec un distant compatible passe à l’étape de recouvrement. Un vérificateur
  compare désormais en lecture seule jusqu’à 64 Kio avant la position sûre au moyen de plages locale
  et HTTP exactement bornées ; il distingue absence de recouvrement nécessaire, correspondance,
  divergence et changement local concurrent. La chaîne diagnostique est coordonnée et un banc
  injecte des fautes déterministes et tue maintenant un subprocess autour de
  `flush → checkpoint SQLite`, sur un puis deux blocs, ainsi qu’avant l’appel disque du second bloc.
  Une tâche au checkpoint exact peut maintenant être reprise par plage HTTP après recouvrement ; la
  finalisation persiste `Finalizing`, renomme sans écraser sur le même volume, persiste `Completed`
  et répare prudemment l’état si un seul chemin subsiste. Trois arrêts subprocess prouvent les états
  après intention, après move et après commit final. Le SHA-256 est calculé en streaming avant
  `Finalizing`, persisté et revérifié pendant la réparation ; une empreinte attendue optionnelle est
  comparée avant mutation. Les collisions sont refusées par défaut ou résolues explicitement par un
  suffixe sans écrasement. Une autre racine utilise un transit local au volume cible, copie, flush,
  SHA-256 puis move local avant suppression de la source. Le crash pendant une écriture, le hash
  officiel distant, le redémarrage Windows, l’essai sur deux volumes physiques et l’interface restent.

## Critères de réussite du premier jalon

1. Un fichier servi avec des plages HTTP peut être interrompu puis repris depuis une position sûre.
2. Les octets finaux correspondent exactement à la source du test.
3. Une modification distante détectable interdit le mélange.
4. La progression et les métadonnées survivent dans SQLite.
5. Les tests et la documentation permanente reflètent le résultat réel.

## Exigences non encore validées

- Crash réel du processus et redémarrage Windows sur un gros fichier : non exécuté.
- Coupure réseau réelle, disque plein et liens expirés : non exécutés.
- Objectifs chiffrés de débit, mémoire, CPU et démarrage : à définir et mesurer.

## 3. Catalogue fonctionnel cible

### 3.1 Acquisition et analyse

Le produit accepte une URL manuelle, une commande depuis l’extension ou une tâche existante. Avant
toute écriture, il valide protocole, hôte, destination et doublons ; suit les redirections autorisées ;
collecte taille, nom, MIME, validateurs et route ; puis sonde raisonnablement les plages. Une taille
inconnue impose le flux simple sans préallocation aveugle. Un contenu transformé empêche la
segmentation si les positions ne sont pas stables.

### 3.2 Transfert, ressources et organisation

- Mode simple pour petit fichier, taille inconnue ou serveur incompatible.
- Mode segmenté après validation de plages indépendantes ; redistribution dynamique avec minimum.
- Connexions adaptées au gain mesuré, aux erreurs, au disque et aux limites par domaine.
- Pause, reprise, annulation contrôlée, arrêt ordonné, file, priorités et planification.
- Limites de débit globales, par tâche et par domaine ; mode « navigation confortable ».
- Dossier et catégorie, recherche/historique, collisions explicites et doublons détectés.
- Suppression distinguant retrait de l’historique et destruction confirmée des données.

### 3.3 Windows, navigateur et maintenance

L’application cible Windows 10/11, fonctionne sans élévation pour l’usage courant, expose une UI
accessible et résiste à la veille/fermeture de session. Chrome/Edge utilisent Native Messaging ; la
capture est désactivable par site/type. Cookies et en-têtes sensibles exigent consentement, portée
minimale et stockage protégé. Installation, désinstallation et mise à jour signée doivent préserver
les tâches, sauvegarder avant migration et proposer un retour arrière.

## 4. Reprise renforcée — ordre normatif

Native `Range` → sondages courts → URL finale autorisée → nouveau lien légitime → recouvrement →
retransmission contrôlée → arrêt sûr. Chaque niveau produit une décision vérifiable sans secret.
Trois ou quatre sondages courts au maximum sont recommandés ; `429` et `Retry-After` suspendent les
tests. Une réponse `206` sans `Content-Range` exact n’est jamais écrite. La retransmission annonce
son coût réseau. L’identité privilégie hash officiel/ETag fort, puis taille et empreintes ; un nom
seul ne fournit aucune confiance et aucun signal faible n’annule un validateur fort contradictoire.

## 5. Cas d’utilisation et réactions attendues

| Cas | Réaction attendue | Interdiction |
|---|---|---|
| Petit fichier | Une connexion, finalisation vérifiée | Segmenter sans bénéfice |
| Taille inconnue | Flux simple croissant | Préallouer une taille supposée |
| `Accept-Ranges` absent | Sondage court autorisé | Conclure sans test |
| Range reçoit `200` | Désactiver vraie reprise | Écrire le corps à l’offset |
| Faux `206` | Serveur non fiable | Faire confiance au statut seul |
| Wi-Fi/changement réseau | Checkpoint, backoff, reprise | Relance immédiate illimitée |
| `429`/`503` | Attendre et réduire la charge | Changer artificiellement d’identité |
| Crash/redémarrage | Réconcilier base/disque/distant | Reprendre à une position incertaine |
| Disque plein/retiré | Suspendre et préserver l’état | Avancer la progression |
| Distant modifié | Arrêt et nouvelle destination | Mélanger deux versions |
| Lien/cookie expiré | Renouvellement légitime | Fabriquer un jeton |
| Collision finale | Demander une règle | Écrasement silencieux |
| Antivirus bloque | Expliquer et préserver | Désactiver la protection |

## 6. Exigences non fonctionnelles

- Intégrité : couverture des plages et confirmation disque avant base.
- Sécurité : moindre privilège, secrets protégés, entrées validées, logs expurgés.
- Performance : mémoire bornée par buffers, débit utile mesuré, UI réactive.
- Compatibilité : Windows 10/11 visés ; build minimal exact, architectures et matrice de supports à
  décider avant le POC WinUI et la distribution. NTFS et supports amovibles restent à tester.
- Maintenabilité : couches testables, migrations, ADR et dépendances explicites.
- Accessibilité : clavier, lecteur d’écran, contraste et information non limitée à la couleur.
- Confidentialité : aucune télémétrie ni abonnement obligatoire pour les fonctions essentielles.

## 7. Interface et exploitation

Chaque tâche affiche état, octets confirmés/total, vitesse lissée, estimation identifiée, stratégie
et action disponible. Une erreur expose cause probable, conséquence, données conservées, prochaine
tentative et action utilisateur. Les opérations destructrices sont distinctes et confirmées.

## 8. Exclusions

Périmètre initial sans BitTorrent, extraction protégée, DRM contourné, jeton falsifié, rotation d’IP,
partage de compte ou exploitation serveur. Les droits sur le fichier et conditions du service restent
applicables.

## 9. Critères d’acceptation majeurs

| Domaine | Critère avant version stable |
|---|---|
| Reprise | Scénarios critiques réussis à 1/25/50/90/99 % |
| Intégrité | Hash source/final identique, aucune plage absente ou incohérente |
| Crash | Arrêts forcés répétés sans base en avance ni corruption |
| HTTP incorrect | Aucun corps incohérent écrit pour `200`, faux `206`, mauvaise taille |
| Volume | Essais 1/10/100 Gio selon banc ; mémoire bornée publiée |
| Sécurité | Menaces/SSRF/secrets revus, aucun critique caché |
| Installation | Installation, mise à jour, rollback et désinstallation testés |

## 10. Catalogue normatif et traçable des exigences

Les termes **DOIT**, **NE DOIT PAS** et **DEVRAIT** sont normatifs. Chaque exigence possède une preuve
attendue. Une modification conserve l’ancien ID et documente la révision.

| ID | Exigence normative | Priorité | Preuve d’acceptation |
|---|---|---|---|
| F-001 | Accepter une URL HTTP/HTTPS manuelle valide | Critique | URL valide/invalide |
| F-002 | Suivre et mémoriser les redirections autorisées | Critique | Chaîne 3xx contrôlée |
| F-003 | Extraire nom, taille, MIME et validateurs disponibles | Haute | Matrice d’en-têtes |
| F-004 | Sonder `Range` sans dépendre d’`Accept-Ranges` | Critique | PR-024 à PR-027 |
| F-005 | Ne jamais écrire sans `206` et `Content-Range` exacts | Critique | PR-025/026/027 |
| F-006 | Utiliser le mode simple si segmenter est dangereux | Critique | Taille inconnue/dynamique |
| F-007 | Segmenter uniquement des plages disjointes et bornées | Critique | PR-070/071/072 |
| F-008 | Adapter les connexions au gain et aux limites | Normale | Benchmark et 429 |
| F-009 | Permettre pause, reprise et annulation récupérables | Critique | PR-004/030/035 |
| F-010 | Reprendre après fermeture/crash/redémarrage si sûr | Critique | PR-031/032/033 |
| F-011 | Appliquer les sept niveaux de reprise autorisés | Critique | Test de chaque branche |
| F-012 | Expliquer le coût de la retransmission | Haute | PR-062 et test UI |
| F-013 | Arrêter et préserver si l’identité est incertaine | Critique | PR-052/061 |
| F-014 | Accepter un nouveau lien légitime après validation | Haute | PR-050/051/052 |
| F-015 | Synchroniser les octets avant confirmation en base | Critique | Crash aux frontières I/O |
| F-016 | Réconcilier plages, base et fichier au démarrage | Critique | Matrice récupération |
| F-017 | Vérifier couverture, taille et hash avant final | Critique | Trous/hash/taille |
| F-018 | Donner le nom final seulement après validation | Critique | PR-034/043 |
| F-019 | Détecter doublons/collisions sans écraser | Haute | URL/ETag/destination |
| F-020 | Séparer historique et suppression du fichier | Haute | Confirmation/retour |
| F-021 | Fournir file, priorités et planification persistantes | Normale | Équité/redémarrage |
| F-022 | Limiter débit global, tâche et domaine | Normale | Mesures de précision |
| F-023 | Afficher état exact et octets confirmés | Haute | Projection UI |
| F-024 | Fournir erreurs actionnables et logs expurgés | Haute | Revue messages/secrets |
| F-025 | Utiliser un canal navigateur borné et authentifié | Critique | Fuzz Native Messaging |
| F-026 | Ne jamais persister cookies/jetons en clair | Critique | Audit base/logs/mémoire |
| F-027 | Ne jamais auto-exécuter un téléchargement | Critique | Test exécutable |
| F-028 | Vérifier signatures et rollback des mises à jour | Critique | Q-004 |
| F-029 | Préserver les téléchargements à la désinstallation | Haute | Matrice uninstall |
| NF-001 | L’intégrité prime toujours sur le débit | Critique | Revue stratégies |
| NF-002 | La RAM ne croît pas avec la taille totale | Haute | Banc 1/10/100 Gio |
| NF-003 | Le moteur fonctionne sans UI et hors ligne localement | Haute | Headless/hors ligne |
| NF-004 | L’usage courant ne requiert pas administrateur | Haute | Compte standard |
| NF-005 | Parcours essentiels accessibles clavier/lecteur | Haute | Audit accessibilité |
| NF-006 | Aucune télémétrie/abonnement obligatoire | Haute | Revue réseau/licence |
| NF-007 | Toute migration est sauvegardable et récupérable | Critique | N-1/interruption |

## 11. Scénarios utilisateurs détaillés

### UC-001 — Téléchargement normal

L’utilisateur ajoute l’URL ; le système valide sans créer le nom final, analyse, affiche les
métadonnées, choisit une stratégie, crée le temporaire, transfère, confirme, vérifie puis renomme.
Nom absent, taille inconnue et collision sont des variantes explicites. En échec, l’état récupérable
est conservé.

### UC-002 — Reprise après crash

Au démarrage : migrations, base, temporaire et carte des segments ; position sûre la plus basse ;
réanalyse distante ; recouvrement ; reprise. Toute contradiction conduit à une action explicite,
jamais à un mélange.

État partiel au 2026-08-04 : le moteur C# coordonne maintenant inspection locale, réanalyse distante,
décision et recouvrement en lecture seule. Un blocage local évite le réseau. Réparation, reprise du
flux, crash réel et redémarrage restent à implémenter et tester avant de satisfaire complètement UC-002.

Le banc C# couvre trois interruptions simulées et sept terminaisons brutales subprocess : après
flush durable, avant commit du checkpoint et après commit, sur un bloc de 5 octets puis pendant le
second bloc d’un transfert de 70 000 octets, plus une mort avant le deuxième appel disque. Un second
processus rouvre les artefacts ; SQLite est derrière le fichier ou exactement alignée, jamais en
avance. Le crash au milieu d’une écriture avant flush durable, la panne électrique, le reboot
Windows, le disque plein et l’écriture partielle réelle restent à prouver.

### UC-003 — Lien expiré

La tâche passe `LIEN_EXPIRE` et conserve octets/identité. Le nouveau lien est analysé puis comparé :
correspondance forte, reprise ; contradiction, refus ; insuffisance, nouveau fichier ou confirmation.

### UC-004 — Disque indisponible

Le moteur cesse de lire, n’accumule pas en RAM, n’avance pas la base, ferme les handles et propose
attendre ou choisir une destination compatible sans supprimer le partiel.

### UC-005 — Capture navigateur

L’extension transmet une demande bornée. L’hôte valide extension, version, taille et origine ; les
données sensibles exigent consentement. Le moteur reçoit une commande métier, jamais une commande
système arbitraire.

## 12. Traçabilité de premier niveau

Ce tableau reste un résumé fonctionnel. La matrice individuelle autoritaire
`Exigence → tâche → ADR → risque → test → preuve` se trouve dans `FEUILLE_DE_ROUTE.md`. Les preuves
Python et C# y sont distinguées afin qu’une capacité du prototype ne valide jamais le produit cible.

| Domaine | Exigences | Tâches | Tests | Risques |
|---|---|---|---|---|
| HTTP/Range | F-001 à F-008 | M-003/009/010 | PR-020 à 028, 070 à 072 | R-003/015 |
| Reprise | F-009 à F-018, NF-001 | M-006 à 012 | PR-004 à 006, 030 à 062 | R-001/002/011/013 |
| Organisation | F-019 à F-024 | M-013 à 015, W-001 à 003 | UI à détailler | R-008/014/021 |
| Navigateur | F-025 à F-027 | B-001/002, Q-002 | PR-053 + fuzz | R-004/005/016 |
| Livraison | F-028/029, NF-004/007 | Q-004 | install/migration | R-017/019/020 |
| Qualité | NF-002/003/005/006 | Q-001 à 005 | bancs/audits | R-009/010/014 |

### Note historique remplacée — état de preuve F-003 à F-005 au 2026-08-03

Le moteur C# extrait taille, nom proposé, MIME, ETag et Last-Modified avec `ResponseHeadersRead`.
Les tests valident un `206 bytes 0-0/length`, le repli sur `200` et le rejet d’un `206` décalé.
Cette photographie initiale est conservée pour l’historique mais remplacée par la feuille de route,
le protocole et l’état actuel. Une tranche ultérieure a ajouté des preuves partielles sur redirection,
429, 503, annulation et validation SSRF ; le rebinding et la reprise C# restent non prouvés.
