# Feuille de route

Version documentaire : 2.2  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-03  
Statut : ACTIF  
Responsable logique : Chef de projet  
Documents liés : `Cahier_des_charges.md`, `ETAT_ACTUEL_PROJET.md`, `PROTOCOLE_TEST_REPRISE.md`

## Sommaire

1. Statuts et règles
2. Tâches existantes
3. Plan détaillé jusqu’à version stable
4. Jalons et portes de qualité
5. Prochaine action

Dernière mise à jour : 2026-08-12

## Convention de pilotage G0

- `T-001` à `T-012` sont des **objectifs parents historiques**. Leur statut est un résumé et ils ne
  sont pas comptés comme tâches exécutables afin d’éviter le double comptage.
- `D-*`, `M-*`, `W-*`, `B-*` et `Q-*` sont les tâches exécutables respectivement documentaires,
  moteur C#, Windows, navigateur et qualité/livraison.
- `T-016` et `T-017` sont des tranches historiques rattachées à `M-001/M-002` et `M-003`. Elles sont
  conservées pour la traçabilité, mais exclues du tableau de charge opérationnel.
- Toute tâche indique désormais implicitement sa pile : `M/W/B/Q = CSHARP-CIBLE`, `D = COMMUN`.
  Les preuves Python sont nommées explicitement et ne valident jamais une tâche C#.
- Le tableau de bord compte uniquement les 35 tâches exécutables du plan détaillé, hors tranches
  historiques `T-*`.

| ID | Phase / tâche | Priorité | Statut | Dépendances | Critère de validation |
|---|---|---:|---|---|---|
| T-001 | Socle HTTP/HTTPS à connexion unique | Haute | PARTIEL | Aucune | Tests local + distant, erreurs HTTP couvertes |
| T-002 | Pause et reprise dans la même session | Haute | À VÉRIFIER | T-001 | Interruption pendant un flux lent réellement testée |
| T-003 | Reprise après fermeture/redémarrage | Haute | PARTIEL | T-001 | Test automatisé simulé + crash réel + redémarrage Windows |
| T-004 | Récupération après crash et incohérence SQLite/disque | Haute | PARTIEL | T-003 | Matrice des incohérences testée |
| T-005 | Segmentation multiple | Normale | À FAIRE | T-001 à T-004 | Plages disjointes vérifiées et assemblées sans corruption |
| T-006 | Segmentation dynamique | Normale | À FAIRE | T-005 | Redistribution mesurée et stable |
| T-007 | Erreurs, `Retry-After` et reconnexions avancées | Haute | PARTIEL | T-001 | Codes temporaires/permanents et attente testés |
| T-008 | Vérification finale et identité distante renforcée | Haute | PARTIEL | T-001 | Taille, empreinte distante, recouvrement et changements testés |
| T-009 | File, priorités et limitation de débit | Normale | À FAIRE | T-005 | Arbitrage global et débit mesurés |
| T-010 | Interface Windows | Normale | À FAIRE | Moteur stable | Parcours utilisateur validé |
| T-011 | Extension Chrome/Edge | Basse | À FAIRE | T-010, sécurité | Échange autorisé et secrets protégés |
| T-012 | Durcissement, performances et installation | Haute | À FAIRE | Jalons précédents | Batterie sécurité/performance/install réussie |
| T-013 | Initialiser les 13 documents permanents | Haute | TERMINÉ | État réel du projet | 13 fichiers présents, cohérents et contrôlés |
| T-014 | Étendre et auditer les 16 documents de conception | Critique | À VÉRIFIER | T-013 | 16 structures complètes, audit croisé, rapport et revue humaine |
| T-015 | Rendre la documentation exhaustive et traçable | Critique | PARTIEL | T-014 | Chaque exigence liée à tâche, risque, test et décision applicable |

## Prochaine action recommandée

Le `DownloadHost` est assemblé (293 tests). Enchaîner les preuves de bout en bout
(PR-060/061/062), l'instance unique et l'IPC ADR-025, puis l'inter-volume réel avant l’UI Windows.

## 3. Plan détaillé jusqu’à la version stable

Chaque ligne hérite des règles de clôture : code présent, tests requis réussis, risques et documents
à jour. Les identifiants D concernent la documentation/cadrage ; M le moteur ; W Windows ; B le
navigateur ; Q la qualité et la livraison.

| ID | Titre | Priorité | Statut | Dépendances | Acceptation / tests |
|---|---|---|---|---|---|
| D-001 | Revue humaine des 16 documents | Critique | PARTIEL | T-013 | Audit G0 accepté ; décisions G1 restant à arbitrer |
| D-005 | Compléter la traçabilité exigence→preuve | Critique | EN COURS | T-015 | 100 % des exigences normatives couvertes |
| D-006 | Détailler chaque ADR proposé | Haute | PARTIEL | D-003/004 | ADR-025 à 029 complets ; ADR-005 à 020 restent à détailler |
| D-007 | Détailler chaque table et migration | Haute | PARTIEL | M-005 | Migrations v2/v3 décrites ; tables segments/événements restent |
| D-008 | Créer les fiches de tests non-reprise | Haute | PARTIEL | Cahier | 42 tests .NET standardisés ; UI, install et performance restent à formaliser |
| D-002 | Choix du nom et identité propre | Normale | À FAIRE | D-001 | Recherche marque et validation propriétaire |
| D-003 | Matrice C#/.NET vs prototype | Critique | PARTIEL | D-001 | .NET 10 retenu et compilé ; parité/migration restant à décider |
| D-004 | Choix WPF/WinUI 3 | Haute | PARTIEL | D-003 | WinUI 3 retenu ; POC accessibilité et packaging restant |
| D-009 | Rétablir la source de vérité documentaire G0 | Critique | TERMINÉ | D-001/D-005 | Piles séparées, action unique, audit croisé et addendum consignés |
| D-010 | Franchir G1 : décisions, NuGet et tests standardisés | Critique | TERMINÉ | D-009 | ADR-025 à 029 acceptées, verrous NuGet, audit et 14 tests réussis |
| M-001 | Contrats Domain/Application | Haute | PARTIEL | D-003 | `RemoteIdentity` et préparation persistable présents ; reprise/finalisation manquent |
| M-002 | Machine d’états complète | Critique | PARTIEL | M-001 | Enum/matrice initiales présentes ; transitions exhaustives manquantes |
| M-003 | Analyse HTTP et redirections | Critique | PARTIEL | M-001 | Connexion liée à l’IP validée ; proxy/TLS public/NAT64 restent à tester |
| M-004 | Stockage temporaire et préallocation C# | Critique | PARTIEL | M-001 | Création, flush, move et copie inter-volume simulée vérifiée ; disque plein/amovible physique restent |
| M-005 | Dépôt SQLite et migrations C# | Critique | PARTIEL | M-001/G1 | Migrations v1→v4 et hash testés ; interruption/rollback/corruption restent |
| M-006 | Pause dans la session | Critique | À VÉRIFIER | M-003/4/5 | PR-004 réussi |
| M-007 | Récupération fermeture/crash | Critique | PARTIEL | M-006 | Reprise et trois frontières de finalisation prouvées ; reboot reste |
| M-008 | Identité composite distante | Critique | PARTIEL | M-003 | Identité, reprise et SHA-256 comparés ; empreinte distante acquise, recouvrement binaire reste |
| M-009 | SegmentManager statique | Haute | PARTIEL | M-007/8 | Planneur, transfert segmenté, reprise segmentée et plages bornées testés ; intégration HTTP multi-segments reste |
| M-010 | Segmentation dynamique | Normale | PARTIEL | M-009 | File de chunks partagée et redistribution testées ; redistribution pilotée par vitesse et intégration hôte restent |
| M-011 | Sept niveaux de reprise | Critique | PARTIEL | M-007/8 | Moteur des sept branches testé (jamais de force) ; intégration hôte et retransmission réelle restent |
| M-012 | Retransmission contrôlée | Haute | PARTIEL | M-011 | Comparaison continue, reprise au manque et coût annoncé testés ; intégration hôte et consentement UI restent |
| M-013 | RetryManager et Retry-After | Haute | PARTIEL | M-003 | Classifieur 429/5xx, backoff exponentiel, gigue et Retry-After testés ; intégration hôte reste |
| M-014 | Scheduler et priorités | Normale | PARTIEL | M-009 | File prioritaire, limite globale et anti-famine par vieillissement testés ; intégration hôte reste |
| M-015 | BandwidthController | Normale | PARTIEL | M-014 | Seaux à jetons global/tâche/domaine testés ; mesure de débit réelle reste |
| W-001 | Shell UI séparé | Haute | À FAIRE | D-004/M-001 | aucune dépendance UI→stockage |
| W-002 | Liste, détail et commandes | Haute | À FAIRE | W-001 | parcours clavier/lecteur d’écran |
| W-003 | Notifications et erreurs | Normale | À FAIRE | W-002 | messages actionnables et expurgés |
| B-001 | Protocole Native Messaging | Haute | À FAIRE | D-001/SEC | schéma versionné et origine validée |
| B-002 | Extensions Chrome/Edge | Normale | À FAIRE | B-001 | permissions minimales et désactivation par site |
| Q-001 | Batterie reprise/chaos | Critique | PARTIEL | M-011/12 | Dix crashs subprocess transfert/finalisation prouvés ; chaos matériel reste |
| Q-002 | Audit sécurité | Critique | À FAIRE | B-002/W-003 | aucun critique ouvert non accepté |
| Q-003 | Bancs performances | Haute | À FAIRE | M-015 | seuils publiés sur profils définis |
| Q-004 | Installateur et signature | Haute | À FAIRE | W/B/Q | install/update/rollback/uninstall testés |
| Q-005 | Candidat stable | Critique | À FAIRE | Toutes | portes de qualité franchies |
| T-016 | Initialiser la solution .NET 10 modulaire | Critique | PARTIEL | ADR-021/022 | SDK installé, 3 bibliothèques compilées, 4 tests réussis ; modules/WinUI restants |
| T-017 | Analyse HTTP streaming et validation Range | Critique | PARTIEL | T-016/ADR-023 | 13 tests : redirect/SSRF/416/429/503/cancel ; rebinding et corps malformés restants |

## 4. Jalons et portes de qualité

- G0 Vérité documentaire : FRANCHIE le 2026-08-03 pour la remise en cohérence initiale ; D-001 reste
  PARTIEL jusqu’aux arbitrages G1.
- G1 Décisions et qualité : FRANCHIE le 2026-08-03 ; ADR-025 à 029 décidées, NuGet verrouillé,
  audit sans vulnérabilité détectée et 14 tests .NET standardisés réussis.
- G2 Moteur direct durable : PARTIEL le 2026-08-11 ; réseau anti-rebind, writer, SQLite v4,
  téléchargement neuf, reprise réseau, finalisation même volume et réparation `Finalizing` sont
  testés avec SHA-256, empreinte distante, collisions et copie inter-volume simulée, mais deux volumes
  physiques et panne matérielle restent.
- J1 Moteur fiable : M-002 à M-008 sans risque critique d’intégrité non traité.
- J2 Accélération : segmentation et reprise renforcée prouvées avant toute promesse de vitesse.
- J3 Produit Windows : UI, navigateur et installateur sans couplage au moteur.
- J4 Stable : sécurité, chaos, performance et rollback exécutés sur OS supportés.

## 5. Matrice de traçabilité opérationnelle G0

Cette matrice est la vue de pilotage par exigence. Le cahier des charges reste autoritaire pour le
besoin ; la feuille de route l’est pour les tâches et l’état de preuve. `PARTIEL` précise notamment
qu’une preuve Python ne valide pas encore le moteur C#.

| Exigence | Tâche C# / gouvernance | ADR | Risque | Test ou preuve | État |
|---|---|---|---|---|---|
| F-001 | M-003 | ADR-023/026 | R-004 | URL/Range avec client injecté et connexion contrôlée | PARTIEL |
| F-002 | M-003 | ADR-023/026 | R-004 | Redirections validées et rebinding loopback bloqué ; proxy/NAT64 restent | PARTIEL |
| F-003 | M-003 | ADR-023 | R-003 | Sonde `206` valide | PARTIEL |
| F-004 | M-003 | ADR-023 | R-003 | PR-024 à PR-027 | PARTIEL |
| F-005 | M-003 | ADR-023 | R-003 | PR-026/027 | PARTIEL |
| F-006 | M-003 | ADR-023 | R-003 | PR-024/025 | PARTIEL |
| F-007 | M-009 | ADR-010 | R-013 | PR-070 à PR-072 | À FAIRE |
| F-008 | M-010/M-014 | ADR-014 | R-015 | Redistribution testée ; benchmarks Q-003 et 429 réels restent | PARTIEL |
| F-009 | M-006/M-007 | ADR-003/009 | R-002/R-011 | PR-004/030/035 ; Python seulement | PARTIEL |
| F-010 | M-007 | ADR-003/009 | R-002/R-011 | Crash avant second appel disque et checkpoints prouvés ; reprise réparatrice/reboot restent | PARTIEL |
| F-011 | M-011 | ADR-020 | R-001 | Branches du moteur testées ; intégration hôte reste | PARTIEL |
| F-012 | M-012 | ADR-020 | R-001 | Coût annoncé testé ; consentement UI et preuves réelles restent | PARTIEL |
| F-013 | M-008 | ADR-004/011 | R-001 | Chaîne distante/recouvrement/hash local ; empreinte distante acquise, recouvrement binaire reste | PARTIEL |
| F-014 | M-011 | ADR-020 | R-001 | Décision nouveau lien testée ; preuves PR-050/051/052 restent | PARTIEL |
| F-015 | M-004/M-005 | ADR-003/027 | R-002/R-011 | Avant second appel disque et commits restaurés sans base en avance ; écriture partielle reste | PARTIEL |
| F-016 | M-007 | ADR-003/009 | R-002/R-017 | Diagnostics coordonnés sans mutation ; réparation/PR-032 restent | PARTIEL |
| F-017 | M-008/Q-001 | ADR-011 | R-001/R-013 | Taille et SHA-256 vérifiés ; carte officielle reste | PARTIEL |
| F-018 | M-004/M-007 | ADR-003/029 | R-011/R-021 | Move local et copie inter-volume vérifiée ; matériel/crash copie restent | PARTIEL |
| F-019 | M-004/W-002 | ADR-010/029 | R-021 | Refus par défaut et `KeepBoth` sans écrasement testés ; choix UI reste | PARTIEL |
| F-020 | W-002 | À décider | R-021 | Test suppression/historique | À FAIRE |
| F-021 | M-014 | ADR-009 | R-014/R-015 | Équité et redémarrage | À FAIRE |
| F-022 | M-015 | ADR-015 | R-014/R-015 | Q-003 | À FAIRE |
| F-023 | W-002 | ADR-009/022 | R-014 | Projection UI | À FAIRE |
| F-024 | W-003 | ADR-017 | R-005/R-016 | Revue messages/logs | À FAIRE |
| F-025 | B-001 | ADR-013 | R-004/R-016 | Fuzz Native Messaging | À FAIRE |
| F-026 | B-001/Q-002 | ADR-012 | R-005/R-016 | Audit base/logs/mémoire | À FAIRE |
| F-027 | B-002/W-003 | ADR-013 | R-016 | Test exécutable non auto-lancé | À FAIRE |
| F-028 | Q-004 | ADR-019 | R-019 | Signature/update/rollback | À FAIRE |
| F-029 | Q-004 | ADR-018 | R-021 | Matrice désinstallation | À FAIRE |
| NF-001 | M-001 à M-013 | ADR-004 | R-001/R-002/R-011 | Revue invariants | PARTIEL |
| NF-002 | Q-003 | ADR-014/015 | R-014 | Bancs 1/10/100 Gio | À FAIRE |
| NF-003 | M-001/W-001 | ADR-008/022 | R-020 | Bibliothèques headless compilées | PARTIEL |
| NF-004 | Q-004 | ADR-018 | R-020 | Compte standard | À FAIRE |
| NF-005 | W-002 | ADR-022 | R-020 | Audit accessibilité | À FAIRE |
| NF-006 | D-001/Q-002 | ADR-016 | R-016 | Audit réseau/licence | PARTIEL |
| NF-007 | M-005/Q-004 | ADR-002/019 | R-017/R-019 | Migration N-1/interruption | À FAIRE |
