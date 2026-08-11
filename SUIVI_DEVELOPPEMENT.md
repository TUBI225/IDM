# Suivi du développement

Version documentaire : 2.2  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04 11:09 UTC  
Statut : JOURNAL PERMANENT — AJOUT UNIQUEMENT  
Responsable logique : Maintenance documentaire  
Documents liés : les 16 documents permanents

## Sommaire

1. Règles du journal
2. Modèle obligatoire
3. Index chronologique
4. Entrées

Ce journal fonctionne en mode « ajouter sans effacer ».

# 2026-08-03 — 18:07 — T-001/T-003 — Création du premier moteur reprenable

## Objectif

Construire le premier jalon : téléchargement HTTP/HTTPS, persistance, reprise sûre et vérification.

## État avant intervention

Le dossier de travail était vide. Les exigences étaient fournies dans deux textes joints.

## Travail effectué

Création d’un paquet Python séparant modèles, réseau, persistance, moteur et CLI. Ajout du sondage
`Range`, de SQLite, d’une progression après `fsync`, d’un recouvrement de 64 Kio, de la vérification
d’identité distante, du backoff, de la lecture SHA-256 finale et du renommage atomique.

## Fichiers créés

- `pyproject.toml`
- `README.md`
- `idm/__init__.py`
- `idm/__main__.py`
- `idm/cli.py`
- `idm/engine.py`
- `idm/models.py`
- `idm/network.py`
- `idm/persistence.py`
- `tests/__init__.py`
- `tests/test_engine.py`

## Fichiers modifiés

- Aucun fichier antérieur : dossier initialement vide.

## Fichiers supprimés

- Aucun.

## Décisions prises

Python standard pour le prototype, SQLite, fichier temporaire unique, position confirmée seulement
après synchronisation et intégrité prioritaire sur la vitesse.

## Problèmes rencontrés

`.NET`, `python` et `node` n’étaient pas dans le PATH. Deux tests ont d’abord échoué parce que le
dossier de destination existait déjà et que le harnais appelait `mkdir` sans `exist_ok=True`.

## Solutions appliquées

Utilisation du Python 3.12 fourni par le runtime Codex. Correction limitée du montage des tests,
puis relance complète.

## Tests exécutés

- Première commande `python -m unittest discover -v` : ÉCHEC, 3 tests, 1 réussi, 2 erreurs de harnais.
- Seconde commande `python -m unittest discover -v` : RÉUSSI, 3 tests, 3 réussis, 0 échec, 0 ignoré.
- `python -m compileall -q idm tests` : RÉUSSI.
- Aide CLI `python -m idm --help` : RÉUSSI.
- Crash réel, redémarrage Windows, coupure réseau et disque plein : NON EXÉCUTÉS.

## Résultats

Téléchargement complet, reprise simulée après reconstruction du moteur et refus d’un ETag modifié
ont réussi sur un serveur HTTP local et un contenu synthétique de 16 Mio.

## Risques découverts

Absence de migrations, rebinding DNS potentiel, données URL en clair, `Retry-After` absent,
distribution Windows non définie et coût de synchronisation non mesuré.

## État final de la tâche

PARTIEL

## Travail restant

Exécuter les interruptions réelles et cas dégradés ; développer les phases 5 à 12.

## Prochaine action

Créer le serveur lent contrôlable et exécuter les protocoles PR-004/PR-005.

## Commit associé

Aucun commit créé ; le dossier n’est pas un dépôt Git.

## Contrôle documentaire

Les 13 documents n’existaient pas à la fin de cette intervention initiale. Cette non-conformité a
été signalée par le propriétaire et corrigée dans l’entrée suivante.

| Document | État | Action |
|---|---|---|
| 13 documents permanents | À METTRE À JOUR | Documents absents à ce moment |

---

# 2026-08-03 — 18:07 — T-013 — Initialisation de la documentation permanente

## Objectif

Créer les 13 fichiers permanents et y consigner fidèlement l’état, les décisions, risques, tests et
travaux restants du projet.

## État avant intervention

Le code, les tests, `pyproject.toml` et `README.md` existaient. Aucun des 13 documents permanents
n’existait. Le dossier n’était pas un dépôt Git.

## Travail effectué

Audit des fichiers et de Git, création des 13 documents, reconstruction transparente de
l’historique initial, classification des tâches et inscription des tests exécutés/non exécutés.

## Fichiers créés

- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `DEPENDANCES.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`

## Fichiers modifiés

- Aucun fichier de code.

## Fichiers supprimés

- Aucun.

## Décisions prises

Ne pas déclarer le moteur terminé ; conserver `PARTIEL` pour les capacités insuffisamment testées.
Ne pas inventer de branche, commit, benchmark ou essai réel. Ne pas supprimer les caches générés
sans autorisation.

## Problèmes rencontrés

Les documents n’existaient pas et ne pouvaient donc pas être lus dans l’ordre prescrit avant cette
initialisation. Git a retourné que le dossier n’est pas un dépôt.

## Solutions appliquées

Création depuis l’état observé et les spécifications du propriétaire, avec distinctions explicites
entre implémenté, testé, partiel et non exécuté.

## Tests exécutés

- Présence et cohérence des 13 fichiers : à exécuter après sauvegarde de cette entrée.
- Tests applicatifs après modification documentaire : à exécuter après sauvegarde de cette entrée.
- Tests de sécurité spécialisés : NON EXÉCUTÉS. Résultat inconnu.
- Tests de performance spécialisés : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

À compléter dans une entrée d’addendum après les contrôles finaux, sans réécrire cet historique.

## Risques découverts

Aucun nouveau risque technique au-delà de ceux inscrits dans le registre. Risque de gouvernance
réduit par la création des documents.

## État final de la tâche

À VÉRIFIER

## Travail restant

Exécuter les contrôles finaux et ajouter leurs résultats réels en addendum.

## Prochaine action

Vérifier les 13 noms, relancer compilation/tests et contrôler les références documentaires.

## Commit associé

Aucun commit créé ; le dossier n’est pas un dépôt Git.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Vision, périmètre et critères consignés |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | Tâches T-001 à T-013 et statuts réels |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Historique initial et présente entrée |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Modules et flux implémentés |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | Dix risques enregistrés |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Six scénarios, exécutés ou non |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | État 0.1.0 et prochaine action |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | ADR-001 à ADR-004 |
| REGLES_DE_CODAGE.md | MIS À JOUR | Conventions et processus documentaire |
| DEPENDANCES.md | MIS À JOUR | Runtime et bibliothèque standard |
| MODELISATION_DONNEES.md | MIS À JOUR | Schéma SQLite et limites migrations |
| SECURITE.md | MIS À JOUR | Protections et menaces ouvertes |
| PERFORMANCES.md | MIS À JOUR | Paramètres et absence de benchmark |

---

# 2026-08-03 — 18:07 — T-013 — Addendum de validation documentaire

## Objectif

Consigner sans réécriture les contrôles réalisés après la création des 13 documents.

## État avant intervention

T-013 était à vérifier dans l’entrée précédente, en attente des contrôles finaux.

## Travail effectué

Vérification automatisée de la présence des 13 noms attendus, compilation du code et relance de la
suite d’intégration. Actualisation de l’état courant et du protocole avec le dernier résultat.

## Fichiers créés

- Aucun.

## Fichiers modifiés

- `ETAT_ACTUEL_PROJET.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `SUIVI_DEVELOPPEMENT.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

T-013 peut être marquée TERMINÉE : les 13 fichiers existent, ne sont pas vides et leur contrôle ne
laisse aucun document à mettre à jour. Les tâches du moteur restent PARTIELLES selon leurs preuves.

## Problèmes rencontrés

Aucun pendant les contrôles finaux.

## Solutions appliquées

Aucune correction de code nécessaire.

## Tests exécutés

- Contrôle PowerShell des 13 chemins : RÉUSSI, 13 attendus, 13 présents, 0 manquant.
- `python -m compileall -q idm tests` : RÉUSSI.
- `python -m unittest discover -v` : RÉUSSI, 3 exécutés, 3 réussis, 0 échec, 0 ignoré,
  durée 2,640 s, Windows/Python 3.12 Codex, 2026-08-03.
- Tests de sécurité spécialisés : NON EXÉCUTÉS. Résultat inconnu.
- Tests de performance spécialisés : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Les 13 documents existent et contiennent des données. La compilation et les trois scénarios
d’intégration n’ont signalé aucune erreur lors de cette exécution.

## Risques découverts

Aucun nouveau risque identifié.

## État final de la tâche

TERMINÉ

## Travail restant

Les tâches T-001 à T-012 restent dans leurs statuts respectifs ; la création documentaire ne valide
pas les tests de crash réel, de sécurité ou de performance.

## Prochaine action

Exécuter PR-004 et PR-005 avec interruption réelle et serveur lent contrôlable.

## Commit associé

Aucun commit créé ; le dossier n’est pas un dépôt Git.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | VÉRIFIÉ — NON CONCERNÉ | Vision inchangée |
| FEUILLE_DE_ROUTE.md | VÉRIFIÉ — NON CONCERNÉ | T-013 déjà TERMINÉE, autres statuts inchangés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Addendum de résultats ajouté |
| ARCHITECTURE_TECHNIQUE.md | VÉRIFIÉ — NON CONCERNÉ | Aucun changement technique |
| REGISTRE_DES_RISQUES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun nouveau risque |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Dernière exécution groupée ajoutée |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Derniers résultats actualisés |
| DECISIONS_ARCHITECTURE.md | VÉRIFIÉ — NON CONCERNÉ | Aucune décision nouvelle |
| REGLES_DE_CODAGE.md | VÉRIFIÉ — NON CONCERNÉ | Règles inchangées |
| DEPENDANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucune dépendance ajoutée |
| MODELISATION_DONNEES.md | VÉRIFIÉ — NON CONCERNÉ | Schéma inchangé |
| SECURITE.md | VÉRIFIÉ — NON CONCERNÉ | Aucun changement de sécurité |
| PERFORMANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun benchmark revendiqué |

---

# 2026-08-03 — 18:07 — T-014 — Documentation complète de conception Windows

## Objectif

Créer et approfondir les 16 documents imposés pour guider le développement futur, sans ajouter de
code applicatif, puis effectuer un audit croisé et produire un rapport chiffré.

## État avant intervention

Treize documents courts et un prototype Python existaient. FAQ, erreurs connues et instructions IA
manquaient. La nouvelle mission indiquait un état initial sans code, incompatible avec le dossier ;
l’état réel a été conservé et documenté.

## Travail effectué

Lecture des 13 documents dans l’ordre permanent ; création des trois fichiers manquants ; ajout des
métadonnées, sommaires, exigences, cas limites, architecture cible, machine d’états, composants,
ADR proposés, tâches, dépendances candidates, modèle de données cible, menaces, risques, bancs de
performance et scénarios de reprise. Audit des 16 structures et des identifiants. Aucun code modifié.

## Fichiers créés

- `FAQ_TECHNIQUE.md`
- `ERREURS_CONNNUES.md`
- `INSTRUCTIONS_IA.md`
- `RAPPORT_CREATION_DOCUMENTATION.md` (rapport hors liste des 16 permanents)

## Fichiers modifiés

- Les 13 documents permanents préexistants.

## Fichiers supprimés

- Aucun.

## Décisions prises

Séparer explicitement prototype observé et architecture cible. Maintenir les choix Windows majeurs
au statut proposé jusqu’à revue humaine. Ne pas prétendre que le développement n’a pas commencé.

## Problèmes rencontrés

Une passe `apply_patch` a échoué sur un contexte de ligne différent ; aucun changement partiel n’a
été appliqué. La mission demandait un état initial sans moteur alors qu’un prototype était présent.

## Solutions appliquées

Fractionnement des mises à jour par document et conservation transparente du prototype/historique.

## Tests exécutés

- Audit PowerShell des 16 fichiers : RÉUSSI, 16/16 avec métadonnées, liens et sommaire.
- Volume intermédiaire : 1 820 lignes, environ 10 946 mots avant l’entrée/rapport final.
- Audit IDs : 42 tâches, 42 tests, 20 ADR, 21 risques.
- `python -m compileall -q idm tests` : RÉUSSI, Windows/Python 3.12 Codex, 2026-08-03.
- `python -m unittest discover -v` : RÉUSSI, 3 exécutés, 3 réussis, 0 échec, 0 ignoré, 2,839 s.
- Revue humaine de fond : NON EXÉCUTÉE. Résultat inconnu.
- Tests sécurité/performance du produit cible : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Les 16 documents existent, sont non vides et couvrent tous les domaines demandés. La cohérence
structurelle est vérifiée ; l’exhaustivité métier et les choix cibles nécessitent une revue humaine.

## Risques découverts

Risque de confondre le prototype Python avec la cible C#/.NET ; risque réduit par statuts explicites.

## État final de la tâche

À VÉRIFIER

## Travail restant

Revue D-001, arbitrages humains et approfondissement des sections identifiées pendant cette revue.

## Prochaine action

Réunion/relecture du propriétaire avant tout nouveau code fonctionnel.

## Commit associé

Aucun commit créé ; le dossier n’est pas un dépôt Git.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Catalogue, cas, exigences, acceptation |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | 42 tâches et portes qualité |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Présente entrée ajoutée |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Cible, composants, états, concurrence |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | Méthode et risques R-011 à R-021 |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Catalogue PR et preuves |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Revue humaine comme prochaine étape |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | ADR-005 à ADR-020 proposés |
| REGLES_DE_CODAGE.md | MIS À JOUR | Async, erreurs, qualité, Git |
| DEPENDANCES.md | MIS À JOUR | Candidats Windows non adoptés |
| MODELISATION_DONNEES.md | MIS À JOUR | Positions, tables cibles, migrations |
| SECURITE.md | MIS À JOUR | Menaces et contrôles cibles |
| PERFORMANCES.md | MIS À JOUR | Bancs et seuils proposés |
| FAQ_TECHNIQUE.md | MIS À JOUR | FAQ créée |
| ERREURS_CONNNUES.md | MIS À JOUR | Registre et limitations créés |
| INSTRUCTIONS_IA.md | MIS À JOUR | Manuel permanent créé |

### Addendum de mesure finale T-014

Après ajout de T-014 et de la présente entrée, le contrôle final des 16 documents donne 1 943 lignes,
environ 11 674 mots, 16/16 structures conformes, 43 IDs de tâches, 42 IDs de tests, 20 ADR et
21 risques. Cette mesure remplace uniquement la mesure intermédiaire ci-dessus ; aucun historique
n’est supprimé. Le statut demeure `À VÉRIFIER` dans l’attente de D-001.

---

# 2026-08-03 — 18:07 — T-015 — Approfondissement vers une documentation exhaustive

## Objectif

Répondre à la demande explicite d’une documentation complète, reconnaître les lacunes du corpus
initial et commencer une traçabilité exploitable sans développer l’application.

## État avant intervention

Les 16 fichiers totalisaient environ 11 674 mots. Ils couvraient les thèmes demandés mais plusieurs
éléments restaient condensés : exigences sans IDs, ADR cibles en tableau, tables sans dictionnaire
détaillé et couverture des tests hors reprise insuffisante.

## Travail effectué

Lecture intégrale des 16 documents selon `INSTRUCTIONS_IA.md`. Application du skill de documentation
technique : informations utiles d’abord, publics distincts, exemples concrets et liens plutôt que
duplication. Ajout de 36 exigences normatives F/NF avec preuves, cinq cas utilisateurs, matrice de
traçabilité et dictionnaire/invariants/transactions/formats de données critiques. Ajout de T-015 et
D-005 à D-008.

## Fichiers créés

- Aucun.

## Fichiers modifiés

- `Cahier_des_charges.md`
- `MODELISATION_DONNEES.md`
- `FEUILLE_DE_ROUTE.md`
- `ETAT_ACTUEL_PROJET.md`
- `RAPPORT_CREATION_DOCUMENTATION.md`
- `SUIVI_DEVELOPPEMENT.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Ne pas considérer la présence de 16 fichiers comme une documentation complète. Utiliser `PARTIEL`
jusqu’à traçabilité exhaustive et revue humaine. Ne pas créer de documents permanents concurrents.

## Problèmes rencontrés

Une modification groupée a échoué sur un contexte de fin de fichier ; aucun changement partiel n’a
été appliqué. Les mises à jour ont été séparées par document.

## Solutions appliquées

Patches limités et ajouts conservant tout le contenu/historique antérieur.

## Tests exécutés

- Lecture des 16 documents : RÉUSSI.
- Vérification structure/traçabilité finale : à exécuter après cette entrée.
- Tests applicatifs : NON EXÉCUTÉS, aucun code modifié. Résultat antérieur inchangé.
- Revue humaine : NON EXÉCUTÉE. Résultat inconnu.

## Résultats

Traçabilité initiale créée ; exhaustivité non atteinte. Audit final : 16 documents présents,
0 référence Markdown permanente brisée détectée, 36 exigences et 48 tâches uniques. Répartition
réelle : 30 À FAIRE, 1 EN COURS, 13 PARTIEL, 3 À VÉRIFIER, 1 TERMINÉ.

## Risques découverts

Risque documentaire : déclarer « complet » sur la seule base du nombre de fichiers. Mitigation :
critères D-005 à D-008 et statut `PARTIEL`.

## État final de la tâche

PARTIEL

## Travail restant

ADR complets, colonnes de toutes les tables, scénarios de tests hors reprise, matrice exhaustive,
audit des doublons/états/modules et revue humaine.

## Prochaine action

Détailler ADR-005 à ADR-020 puis relier chaque décision aux exigences et risques.

## Commit associé

Aucun commit créé ; le dossier n’est pas un dépôt Git.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Exigences, UC et traçabilité |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | T-015 et D-005 à D-008 |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée |
| ARCHITECTURE_TECHNIQUE.md | VÉRIFIÉ — NON CONCERNÉ | Cible inchangée |
| REGISTRE_DES_RISQUES.md | VÉRIFIÉ — NON CONCERNÉ | Risque documentaire consigné ici |
| PROTOCOLE_TEST_REPRISE.md | VÉRIFIÉ — NON CONCERNÉ | Aucun résultat modifié |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Statut PARTIEL explicite |
| DECISIONS_ARCHITECTURE.md | VÉRIFIÉ — NON CONCERNÉ | À approfondir via D-006 |
| REGLES_DE_CODAGE.md | VÉRIFIÉ — NON CONCERNÉ | Règles inchangées |
| DEPENDANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun ajout |
| MODELISATION_DONNEES.md | MIS À JOUR | Dictionnaire critique et invariants |
| SECURITE.md | VÉRIFIÉ — NON CONCERNÉ | Menaces inchangées |
| PERFORMANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucune mesure nouvelle |
| FAQ_TECHNIQUE.md | VÉRIFIÉ — NON CONCERNÉ | Réponses inchangées |
| ERREURS_CONNNUES.md | VÉRIFIÉ — NON CONCERNÉ | Aucune erreur confirmée |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus respecté |

---

# 2026-08-03 — 20:37 — D-009 — Rétablissement de la source de vérité G0

## Objectif

Appliquer la stratégie G0 validée par le propriétaire : analyser les 16 documents, séparer le
prototype Python du produit C#, supprimer les contradictions de pilotage, établir une traçabilité
individuelle et préparer une prochaine action unique avant tout nouveau code fonctionnel.

## État avant intervention

Les 16 documents existaient mais mélangeaient capacités Python, socle C# et cible future. L’état
actuel annonçait simultanément une reprise SQLite et l’absence de stockage C#. La feuille de route
comptait objectifs parents et tâches détaillées ensemble, plusieurs prochaines actions se
contredisaient et le suivi contenait l’addendum T-016 puis T-017 avant l’entrée principale T-016.
Le dossier ne possédait pas de dépôt Git ni de contrôle automatique de cohérence documentaire.

## Travail effectué

- Validation humaine G0 enregistrée et D-009 créée comme tâche de remise en cohérence.
- C# déclaré produit actif ; Python déclaré référence temporaire gelée avec données séparées.
- Tâches `T-*` classées comme objectifs/tranches historiques hors comptage opérationnel.
- Statuts C# M-001/M-002 passés à PARTIEL ; M-004/M-005 remis À FAIRE.
- Tableau de bord régénéré avec 34 tâches exécutables et une seule prochaine action G1.
- Matrice individuelle des 36 exigences ajoutée dans la feuille de route.
- Architecture, ADR, dépendances, données, sécurité, risques, performances et protocole alignés.
- ADR-024 acceptée ; ADR-025 à ADR-029 inscrites comme décisions G1 proposées.
- R-023 ajouté pour l’absence de versionnement ; BUG-001 ajouté pour la preuve de redirection
  surévaluée ; R-004 recalibré sans baisse de probabilité injustifiée.
- Note corrective ajoutée ici sans déplacer ni effacer les anciennes entrées T-016/T-017.
- Contrôle `eng/verify-documentation.ps1` créé et dépôt Git local `main` initialisé.

## Fichiers créés

- `eng/verify-documentation.ps1`
- Métadonnées locales `.git/` générées par `git init -b main`.

## Fichiers modifiés

- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `DEPENDANCES.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `FAQ_TECHNIQUE.md`
- `ERREURS_CONNNUES.md`
- `INSTRUCTIONS_IA.md`
- `README.md`
- `RAPPORT_CREATION_DOCUMENTATION.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

- ADR-024 : C# est le produit actif ; Python reste isolé jusqu’à parité/migration décidée.
- Le cahier gouverne le besoin, les ADR les choix, la feuille les tâches, l’état la photographie et
  le suivi l’historique.
- Les tâches parents ne sont plus additionnées aux tâches exécutables.
- G1 doit décider processus, réseau/DNS/proxy, SQLite, tests/NuGet et finalisation avant les paquets
  ou composants concernés.
- Aucun développement UI ou segmentation avant une tranche C# séquentielle récupérable.

## Problèmes rencontrés

- Deux patches groupés ont échoué sur un contexte exact de fin de fichier/heading ; `apply_patch`
  n’a appliqué aucun fragment lors de ces échecs.
- Git ne possède aucune identité utilisateur configurée, donc aucune baseline ne peut être commitée
  honnêtement sans décision du propriétaire.

## Solutions appliquées

- Patches repris avec contextes précis et historique préservé.
- Dépôt initialisé sur `main`, mais aucun nom/email Git n’a été inventé et aucun commit créé.
- Un script sans dépendance externe contrôle désormais présence, traçabilité, IDs, statuts et liens.

## Tests exécutés

- Commande : `powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify-documentation.ps1`.
- Environnement : Windows PowerShell, Windows, 2026-08-03 20:37 UTC.
- Résultat : RÉUSSI ; 16/16 documents présents/non vides, 36/36 exigences tracées, 34 tâches
  exécutables, comptes cohérents, aucune définition d’ID dupliquée, références Markdown locales valides.
- Commande : `git status --short --branch`.
- Résultat : RÉUSSI ; dépôt sans commit sur branche `main`, fichiers non suivis observés.
- Diagnostic : `git diff --check --no-index -- NUL eng/verify-documentation.ps1` a retourné le code 1
  parce qu’un fichier nouveau diffère de `NUL`; aucune erreur d’espace n’a été signalée, seulement un
  avertissement de conversion future LF→CRLF. Ce diagnostic ne constitue pas un échec applicatif.
- Build/tests C# : NON EXÉCUTÉS. Aucun code fonctionnel modifié ; résultat courant non réobservé.
- Tests Python : NON EXÉCUTÉS. Aucun code Python modifié ; résultat courant non réobservé.
- Sécurité/performance applicatives : NON EXÉCUTÉES. Résultats inconnus.

## Résultats

La baseline G0 distingue désormais les piles, fournit une action officielle unique et rend les
comptages reproductibles. Le projet reste un socle C# partiel : aucune reprise C# n’est revendiquée.
Le dépôt Git existe mais ne possède encore ni identité configurée ni premier commit.

## Risques découverts

- R-023 : absence initiale de versionnement, réduite par l’initialisation Git mais non close sans
  baseline commitée.
- BUG-001/R-004 : le test de redirection ne prouve pas la revalidation sécurisée de chaque saut.
- Aucun autre risque nouveau ; les décisions G1 manquantes sont explicitement enregistrées.

## État final de la tâche

TERMINÉ

## Travail restant

- Configurer une identité Git légitime et créer la baseline initiale.
- Détailler/arbitrer ADR-025 à ADR-029.
- Choisir la politique NuGet et industrialiser les tests .NET.
- Terminer la frontière réseau puis développer le stockage temporaire C# durable.

## Prochaine action

Exécuter G1 : arbitrer les ADR bloquants, sécuriser la restauration NuGet et remplacer le harnais
transitoire par des tests standardisables. Ensuite seulement terminer le réseau et commencer le
writer temporaire C#.

## Commit associé

Aucun commit créé. Dépôt `main` initialisé, mais identité Git utilisateur absente.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Piles séparées, preuve datée marquée historique |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | D-009, conventions, statuts, matrice 36 exigences |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée G0 et correction chronologique ajoutées |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | États Python/C#, cycle HTTP et décisions ouvertes |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-001/2/4/8/9 révisés, R-022/023 intégrés |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Portées et preuve redirection corrigées |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Tableau de bord G0 régénéré |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | ADR-024 acceptée, file ADR-025 à 029 |
| REGLES_DE_CODAGE.md | MIS À JOUR | C# actif, Python gelé, 16 documents, Git/tests |
| DEPENDANCES.md | MIS À JOUR | Statuts .NET/HTTP et blocage NuGet clarifiés |
| MODELISATION_DONNEES.md | MIS À JOUR | Bases Python/C# séparées |
| SECURITE.md | MIS À JOUR | Contrôles par pile et porte réseau corrigés |
| PERFORMANCES.md | MIS À JOUR | Paramètres Python séparés, baseline C# absente |
| FAQ_TECHNIQUE.md | MIS À JOUR | Choix C#/WinUI et statut Python actualisés |
| ERREURS_CONNNUES.md | MIS À JOUR | BUG-001 enregistré |
| INSTRUCTIONS_IA.md | MIS À JOUR | Hiérarchie, piles, concurrence et contrôle automatique |

Contrôle final T-017 : 16/16 documents présents. Statuts recalculés : 28 À FAIRE, 1 EN COURS,
15 PARTIEL, 5 À VÉRIFIER, 1 TERMINÉ, aucun BLOQUÉ/REPORTÉ/ABANDONNÉ.

### Addendum T-016 — Installation et bootstrap réussis

L’installation officielle a dépassé le timeout, mais la vérification a confirmé le SDK 10.0.302 et
le runtime 10.0.10 complets dans `.tools/dotnet`. La première compilation a échoué avant le compilateur
car NuGet lisait `%APPDATA%` hors sandbox. `NuGet.Config` sans source et une redirection `APPDATA`
limitée au processus ont corrigé le problème.

Fichiers créés : `global.json`, `Directory.Build.props`, `NuGet.Config`, `.gitignore`,
`WindowsDownloadManager.slnx`, trois projets `src` et un projet `tests-dotnet`. `README.md` et les
documents concernés ont été actualisés. Aucun fichier supprimé.

Tests du 2026-08-03, Windows, SDK 10.0.302 : restauration RÉUSSIE ; build Release RÉUSSI avec
0 avertissement et 0 erreur ; tests RÉUSSIS, 4 réussis, 0 échec. WinUI 3, SQLite, transfert réel,
sécurité spécialisée et performances : NON EXÉCUTÉS, résultats inconnus.

État révisé de T-016 : `PARTIEL`. Prochaine action : analyseur HTTP en streaming avec serveur
d’intégration contrôlé, avant stockage et UI.

Contrôle final relancé après documentation : build Release RÉUSSI en 7,55 s, 0 avertissement,
0 erreur ; 4 tests réussis, 0 échec. Statuts recalculés : 28 À FAIRE, 1 EN COURS, 14 PARTIEL,
5 À VÉRIFIER, 1 TERMINÉ, 0 BLOQUÉ/REPORTÉ/ABANDONNÉ.

Contrôle documentaire : feuille de route, suivi, architecture, risques, état, dépendances et README
mis à jour ; cahier, protocole, données, sécurité, performances, FAQ, erreurs et instructions IA
vérifiés non concernés ; décisions/règles déjà actualisées dans l’entrée principale T-016.

---

# 2026-08-03 — 19:36 — T-017 — Analyse HTTP streaming et validation Range

## Objectif

Analyser une ressource HTTP sans charger son corps et décider de façon sûre si les plages sont
réellement utilisables.

## État avant intervention

La fabrique `RangeRequestFactory` existait, mais aucun appel `HttpClient` C#, aucune extraction de
métadonnées et aucun test HTTP réel .NET n’existaient.

## Travail effectué

Extension de `RemoteResourceInfo`. Création de `HttpRemoteResourceAnalyzer` et de
`InvalidRangeResponseException`. Envoi streaming `bytes=0-0`, extraction des métadonnées, repli sûr
sur `200` et validation stricte de `Content-Range`. Création d’un serveur TCP loopback à usage test
et conversion du harnais en tests asynchrones.

## Fichiers créés

- `src/WindowsDownloadManager.Network/Http/HttpRemoteResourceAnalyzer.cs`
- `src/WindowsDownloadManager.Network/Http/InvalidRangeResponseException.cs`
- `tests-dotnet/WindowsDownloadManager.Domain.Tests/LoopbackHttpServer.cs`

## Fichiers modifiés

- `IRemoteResourceAnalyzer.cs`, test `Program.cs`, README et documents concernés.

## Fichiers supprimés

- Aucun ; `Program.cs` a été remplacé en place par sa version asynchrone.

## Décisions prises

Tests domaine rapides plus intégration HTTP locale. Aucun paquet de test tiers avant choix formel.
Un `200` n’est pas une erreur mais désactive les plages ; un `206` incohérent est une erreur de
protocole avant toute écriture.

## Problèmes rencontrés

Deux patches documentaires groupés ont échoué sur des contextes exacts différents ; aucun changement
partiel n’a été appliqué.

## Solutions appliquées

Fractionnement des mises à jour sans effacer l’historique.

## Tests exécutés

- Build Release .NET : RÉUSSI, 0 avertissement, 0 erreur, 24,26 s.
- Harnais .NET : RÉUSSI, 7 réussis, 0 échec, 0 ignoré.
- Python `compileall` : RÉUSSI.
- Python `unittest discover -v` : RÉUSSI, 3 réussis, 0 échec, 0 ignoré, 2,757 s.
- Redirections, 429/5xx, annulation, SSRF : NON EXÉCUTÉS. Résultats inconnus.
- Sécurité/performance spécialisées : NON EXÉCUTÉES. Résultats inconnus.

## Résultats

Le chemin `206` valide, le repli `200` et le rejet d’un faux `206` sont prouvés sur socket locale.

## Risques découverts

R-004 confirmé ouvert pour le C# : SSRF/DNS non encore implémenté. R-003 réduit, non clos.

## État final de la tâche

PARTIEL

## Travail restant

Redirections, validation URL/IP, 416, 429, 5xx, annulation, limites d’en-têtes, nom RFC et corps
malformés.

## Prochaine action

Ajouter la validation URL/SSRF puis les tests redirection, 429 et erreurs temporaires.

## Commit associé

Aucun commit créé ; le dossier n’est pas un dépôt Git.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Preuves F-003 à F-005 |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | T-017 ajoutée PARTIEL |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Présente entrée |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Analyseur réel documenté |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-003/R-004 |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Résultats C# HTTP |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | 7 tests et capacité HTTP |
| DECISIONS_ARCHITECTURE.md | VÉRIFIÉ — NON CONCERNÉ | ADR-023 respectée |
| REGLES_DE_CODAGE.md | VÉRIFIÉ — NON CONCERNÉ | Règles async respectées |
| DEPENDANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun paquet ajouté |
| MODELISATION_DONNEES.md | MIS À JOUR | Projection `RemoteResourceInfo` |
| SECURITE.md | MIS À JOUR | SSRF manquant explicité |
| PERFORMANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun benchmark revendiqué |
| FAQ_TECHNIQUE.md | VÉRIFIÉ — NON CONCERNÉ | Non concerné |
| ERREURS_CONNNUES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun bug confirmé |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus respecté |

---

# 2026-08-03 — 19:36 — T-017 — Sécurisation URL, redirections et erreurs HTTP

## Objectif

Fermer les principales frontières réseau avant tout téléchargement sur disque.

## État avant intervention

Le sondage 200/206 fonctionnait, mais les redirections, SSRF, 416, 429, 5xx et l’annulation restaient
non testés. Un `HttpClient` injecté pouvait suivre une redirection automatiquement avant validation.

## Travail effectué

Ajout du port `IUriSafetyValidator`, de `PublicHttpUriSafetyValidator`, `UnsafeUriException` et
`RemoteHttpException`. Redirections manuelles limitées à dix, validation avant chaque requête,
classification temporaire et `Retry-After`, ressource vide 416, annulation propagée. L’analyseur
possède un handler sans redirection/décompression automatique.

## Fichiers créés

- `IUriSafetyValidator.cs`
- `PublicHttpUriSafetyValidator.cs`
- `UnsafeUriException.cs`
- `RemoteHttpException.cs`

## Fichiers modifiés

- Analyseur HTTP, serveur loopback, harnais de tests et documents concernés.

## Fichiers supprimés

- Aucun.

## Décisions prises

Validation conservatrice des IP publiques ; validation de chaque saut ; handler possédé par
l’analyseur pour empêcher un auto-redirect non contrôlé.

## Problèmes rencontrés

Un risque de contournement par `HttpClient` auto-redirigé a été découvert après les premiers tests.

## Solutions appliquées

Suppression de l’injection d’un client arbitraire ; création interne d’un `SocketsHttpHandler`
sécurisé et longue durée.

## Tests exécutés

- Build Release .NET : RÉUSSI, 0 avertissement, 0 erreur, 9,51 s.
- Harnais .NET : RÉUSSI, 13 réussis, 0 échec, 0 ignoré.
- Non-régression Python : RÉUSSI, 3 réussis, 0 échec, 0 ignoré, 2,660 s.
- Rebinding DNS/proxy/NAT64/corps malformés : NON EXÉCUTÉS, résultats inconnus.

## Résultats

Loopback/réservé rejetés, redirect revalidé, 416 vide, 429 avec délai, 503 temporaire et annulation
prouvés. Aucun octet disque n’est encore écrit par le moteur C#.

## Risques découverts

Rebinding entre résolution et connexion toujours possible ; R-004 réduit mais impact critique.

## État final de la tâche

PARTIEL

## Travail restant

`ConnectCallback` anti-rebinding, proxy/IPv6/NAT64, limites de chaîne/en-têtes et corps incohérents.

## Prochaine action

Créer le stockage temporaire C# avec écriture, synchronisation et progression confirmée.

## Commit associé

Aucun commit créé ; le dossier n’est pas un dépôt Git.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | VÉRIFIÉ — NON CONCERNÉ | Exigences inchangées |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | Critères T-017 actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Présente entrée |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Flux sécurisé documenté |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | Révision R-004 |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Six preuves ajoutées |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | 13 tests et capacités |
| DECISIONS_ARCHITECTURE.md | VÉRIFIÉ — NON CONCERNÉ | ADR-023 respectée |
| REGLES_DE_CODAGE.md | VÉRIFIÉ — NON CONCERNÉ | Async/annulation respectées |
| DEPENDANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun paquet ajouté |
| MODELISATION_DONNEES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun stockage nouveau |
| SECURITE.md | MIS À JOUR | Contrôles et limite rebinding |
| PERFORMANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun benchmark revendiqué |
| FAQ_TECHNIQUE.md | VÉRIFIÉ — NON CONCERNÉ | Non concerné |
| ERREURS_CONNNUES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun bug actif confirmé |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus respecté |

---

# 2026-08-03 — 19:36 — T-016 — Choix de la plateforme cible

## Objectif

Choisir la meilleure plateforme pour piloter un code maintenable et obtenir de bonnes performances.

## État avant intervention

C#/.NET et WPF/WinUI étaient proposés sans décision. Le prototype Python existait. Aucun SDK .NET
n’était exposé dans le PATH.

## Travail effectué

Consultation des sources Microsoft actuelles, comparaison des options et adoption de C#/.NET 10 LTS,
WinUI 3 isolé par MVVM et `HttpClient` partagé/streaming. Création des ADR-021 à ADR-023, structure
cible, règles C# et mises à jour de dépendances, feuille de route, état et risques.

## Fichiers créés

- Aucun.

## Fichiers modifiés

- `DECISIONS_ARCHITECTURE.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `DEPENDANCES.md`
- `FEUILLE_DE_ROUTE.md`
- `ETAT_ACTUEL_PROJET.md`
- `REGLES_DE_CODAGE.md`
- `REGISTRE_DES_RISQUES.md`
- `SUIVI_DEVELOPPEMENT.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

.NET 10 LTS, WinUI 3/MVVM et `HttpClient` partagé. Moteur sans dépendance UI. Python conservé jusqu’à
parité testée. Décision fondée sur support Microsoft actuel et aptitude aux I/O asynchrones.

## Problèmes rencontrés

`dotnet` et `winget` sont absents ; aucune compilation C# possible avant installation locale du SDK.

## Solutions appliquées

T-016 marquée BLOQUÉE sur l’installation du SDK ; aucune source C# non compilée créée.

## Tests exécutés

- Vérification `dotnet`/`winget` : RÉUSSI pour le diagnostic, outils absents.
- Compilation C# : NON EXÉCUTÉE. Résultat inconnu.
- Tests C# : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Décision d’architecture prise et documentée ; implémentation non commencée.

## Risques découverts

R-022, divergence Python/C#, ajouté. R-009 et R-020 maintenus ouverts.

## État final de la tâche

BLOQUÉ

## Travail restant

Installer localement le SDK .NET 10, créer la solution, compiler et exécuter les tests squelette.

## Prochaine action

Obtenir l’autorisation d’installer le SDK .NET 10 dans un dossier local au projet.

## Commit associé

Aucun commit créé ; le dossier n’est pas un dépôt Git.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | VÉRIFIÉ — NON CONCERNÉ | Exigences inchangées |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | D-003/004 et T-016 |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Solution cible .NET |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-022 et état R-009/020 |
| PROTOCOLE_TEST_REPRISE.md | VÉRIFIÉ — NON CONCERNÉ | Aucun test modifié |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Décision et blocage SDK |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | ADR-021 à ADR-023 |
| REGLES_DE_CODAGE.md | MIS À JOUR | Règles C#/.NET |
| DEPENDANCES.md | MIS À JOUR | Plateforme retenue |
| MODELISATION_DONNEES.md | VÉRIFIÉ — NON CONCERNÉ | Modèle inchangé |
| SECURITE.md | VÉRIFIÉ — NON CONCERNÉ | Aucune permission nouvelle |
| PERFORMANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucune mesure revendiquée |
| FAQ_TECHNIQUE.md | VÉRIFIÉ — NON CONCERNÉ | Réponses cohérentes |
| ERREURS_CONNNUES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun bug confirmé |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus respecté |

---

## Addendum de positionnement chronologique — 2026-08-03 20:40 UTC — D-009

L’entrée complète D-009 de 20:37 a été ajoutée après un contexte Markdown non unique et apparaît
physiquement avant les anciennes entrées T-017/T-016. Aucun historique n’a été déplacé, supprimé ou
réécrit. Le présent addendum, ajouté à la fin réelle du fichier, constitue la clôture chronologique
autoritaire de D-009. Le contrôle final a de nouveau réussi : 16/16 documents, 36/36 exigences,
34 tâches exécutables, comptes/IDs/liens cohérents. Prochaine action inchangée : G1.

---

# 2026-08-03 — 22:12 UTC — D-010 — Franchir G1 : décisions, NuGet et tests standardisés

## Objectif

Décider les cinq ADR bloquantes de G1, remplacer le harnais C# artisanal par des tests standard,
verrouiller les dépendances et établir une commande de qualité reproductible avant G2.

## État avant intervention

ADR-025 à ADR-029 étaient seulement proposées. `NuGet.Config` ne déclarait aucune source, le C# se
testait avec un exécutable personnalisé de 13 scénarios, BUG-001 surévaluait la preuve de redirection
et aucun verrou de paquet ni audit transitif n’existait.

## Travail effectué

- Acceptation documentée d’ADR-025 à ADR-029 : hôte utilisateur unique, frontière réseau anti-rebind,
  SQLite direct, MSTest/NuGet verrouillé et finalisation réparable.
- Migration vers `MSTest.Sdk` 4.3.2 et Microsoft Testing Platform ; séparation Domain/Network.
- Création de 14 tests : 3 Domain et 11 Network, dont deux preuves correctives pour BUG-001.
- Source NuGet limitée à `nuget.org`, cache `.packages`, fichiers de verrou, audit transitif et
  désactivation de télémétrie CLI/test.
- Création de `eng/verify.ps1` et renforcement de `eng/verify-documentation.ps1` à 35 tâches.
- Synchronisation de la feuille de route, de l’état courant et des documents concernés.

## Fichiers créés

- `eng/verify.ps1`
- `tests-dotnet/WindowsDownloadManager.Domain.Tests/DownloadTaskTests.cs`
- `tests-dotnet/WindowsDownloadManager.Domain.Tests/packages.lock.json`
- `tests-dotnet/WindowsDownloadManager.Network.Tests/WindowsDownloadManager.Network.Tests.csproj`
- `tests-dotnet/WindowsDownloadManager.Network.Tests/HttpRemoteResourceAnalyzerTests.cs`
- `tests-dotnet/WindowsDownloadManager.Network.Tests/LoopbackHttpServer.cs`
- `tests-dotnet/WindowsDownloadManager.Network.Tests/RecordingUriSafetyValidator.cs`
- `tests-dotnet/WindowsDownloadManager.Network.Tests/packages.lock.json`

## Fichiers modifiés

- `.gitignore`, `Directory.Build.props`, `NuGet.Config`, `global.json`, `WindowsDownloadManager.slnx`
- `tests-dotnet/WindowsDownloadManager.Domain.Tests/WindowsDownloadManager.Domain.Tests.csproj`
- `eng/verify-documentation.ps1`
- `README.md`
- `FEUILLE_DE_ROUTE.md`, `SUIVI_DEVELOPPEMENT.md`, `ETAT_ACTUEL_PROJET.md`
- `ARCHITECTURE_TECHNIQUE.md`, `DECISIONS_ARCHITECTURE.md`, `REGLES_DE_CODAGE.md`
- `DEPENDANCES.md`, `MODELISATION_DONNEES.md`, `SECURITE.md`, `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`, `PERFORMANCES.md`, `FAQ_TECHNIQUE.md`, `ERREURS_CONNNUES.md`,
  `INSTRUCTIONS_IA.md`

## Fichiers supprimés

- `tests-dotnet/WindowsDownloadManager.Domain.Tests/Program.cs`
- `tests-dotnet/WindowsDownloadManager.Domain.Tests/LoopbackHttpServer.cs`

Suppression précédée d’une recherche d’utilisations ; le serveur loopback a été déplacé dans le
projet Network et le lanceur artisanal a été remplacé par la découverte MSTest.

## Décisions prises

Les cinq choix G1 sont consignés dans ADR-025 à ADR-029. `Microsoft.Data.Sqlite` 10.0.10 est retenu
mais n’est pas encore installé. La restauration hors ligne désactive l’appel d’audit uniquement ;
l’audit connecté reste une porte séparée. G2 commence par ADR-026 avant le stockage.

## Problèmes rencontrés

- La première restauration sans accès réseau a retourné des avertissements de source indisponible et
  a ignoré les projets de test : cette tentative ne constitue pas une réussite.
- Une restauration `--locked-mode` avec audit actif a échoué en `NU1900`, le service d’avis étant
  inaccessible dans l’environnement restreint.
- La commande `python` n’était pas dans le PATH ; le runtime Python fourni a dû être appelé explicitement.
- Une exécution complète de `eng/verify.ps1` a dépassé la limite de 60 s après les tests, pendant le
  formatage. Les étapes restantes ont été relancées séparément et ont réussi.

## Solutions appliquées

La restauration initiale et l’audit ont été exécutés avec l’accès réseau autorisé. Le mode courant
utilise ensuite les verrous et le cache avec `-p:NuGetAudit=false`. Le format et le contrôle
documentaire ont été relancés séparément après le timeout. Aucun résultat incomplet n’a été compté.

## Tests exécutés

- `dotnet restore WindowsDownloadManager.slnx --configfile NuGet.Config --force-evaluate` : RÉUSSI,
  5 projets restaurés.
- `dotnet restore ... --locked-mode` avec audit : ÉCHEC `NU1900`.
- `dotnet restore ... --locked-mode -p:NuGetAudit=false` : RÉUSSI, 5 projets.
- `dotnet build WindowsDownloadManager.slnx -c Release --no-restore` : RÉUSSI, 0 avertissement,
  0 erreur.
- `dotnet test WindowsDownloadManager.slnx -c Release --no-build --no-restore` : RÉUSSI,
  14 exécutés, 14 réussis, 0 échec, 0 ignoré ; dernière exécution 2,532 s.
- `dotnet format ... --verify-no-changes --no-restore` : RÉUSSI.
- `dotnet package list ... --vulnerable --include-transitive --no-restore` : RÉUSSI, aucune
  vulnérabilité signalée le 2026-08-03.
- Python `compileall` : RÉUSSI.
- Python `unittest discover -v` : RÉUSSI, 3 exécutés, 3 réussis, 0 échec, 0 ignoré, 2,118 s.
- `eng/verify-documentation.ps1` : RÉUSSI, 16/16 documents, 36/36 exigences, 35 tâches, IDs,
  comptes et liens cohérents.
- Reprise C# réelle, crash, disque plein, rebinding, proxy, IPv6/NAT64, UI, installation et
  performance produit : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

G1 est franchie : cinq décisions complètes, restauration reproductible, audit ponctuel sans alerte,
build propre et 14 tests standardisés réussis. BUG-001 est corrigée. Le produit ne télécharge encore
aucun fichier et R-004 reste critique jusqu’à l’implémentation de la connexion liée à l’adresse validée.

## Risques découverts

R-024 a été ajouté pour la chaîne d’approvisionnement des tests et la télémétrie involontaire. Il est
réduit par verrous, source unique, audit et opt-out, mais reste surveillé.

## État final de la tâche

TERMINÉ

## Travail restant

- Implémenter ADR-026 et tester rebinding/proxy/IPv4/IPv6/NAT64.
- Ajouter `Microsoft.Data.Sqlite` seulement avec M-005 et ses tests migration/crash.
- Prototyper l’instance unique et l’IPC ADR-025 ; implémenter la réparation ADR-029.
- Configurer l’identité Git puis créer une baseline atomique code et documentation.

## Prochaine action

G2 : faire posséder le client HTTP par la composition/hôte et lier validation DNS et connexion réelle,
puis créer le writer temporaire et le dépôt SQLite durable.

## Commit associé

Aucun commit créé : identité Git toujours non configurée.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | VÉRIFIÉ — NON CONCERNÉ | Besoin produit inchangé |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | D-010 terminée, G1 franchie, 35 tâches |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée D-010 ajoutée sans effacer l’historique |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Topologie, réseau, stockage et finalisation G1 |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-004 révisé et R-024 ajouté |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Preuve MSTest 14/14 et non-exécutés |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | G1, résultats et prochaine action G2 |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | ADR-025 à ADR-029 complètes |
| REGLES_DE_CODAGE.md | MIS À JOUR | Tests, verrous et télémétrie |
| DEPENDANCES.md | MIS À JOUR | MSTest installé, SQLite décidé, politique NuGet |
| MODELISATION_DONNEES.md | MIS À JOUR | Contraintes SQLite/finalisation, non implémentées |
| SECURITE.md | MIS À JOUR | Preuves redirection et chaîne NuGet |
| PERFORMANCES.md | MIS À JOUR | Durée du runner non assimilée à un benchmark |
| FAQ_TECHNIQUE.md | MIS À JOUR | Choix hôte, MSTest et SQLite expliqués |
| ERREURS_CONNNUES.md | MIS À JOUR | BUG-001 passée à CORRIGÉE |
| INSTRUCTIONS_IA.md | MIS À JOUR | Commande canonique et règles d’audit |

---

# 2026-08-03 — 22:44 UTC — M-003/M-004/M-005 — G2 réseau direct et durabilité initiale

## Objectif

Lier la connexion réseau à l’adresse réellement validée, transférer la propriété du `HttpClient` à
la composition, puis créer les premiers adaptateurs C# de fichier durable et SQLite.

## État avant intervention

Le réseau validait DNS avant `SendAsync`, mais le transport résolvait de nouveau sans liaison et
l’analyseur possédait son client. Aucun writer, assembly Storage/Persistence, schéma C# ou migration
n’existait. La suite comptait 14 tests.

## Travail effectué

- Création d’un resolver injectable, d’une politique d’adresses publiques et d’un handler dont le
  `ConnectCallback` filtre puis connecte directement l’IP acceptée ; proxy et redirects automatiques
  désactivés. `HttpRemoteResourceAnalyzer` reçoit désormais un client externe.
- Création de `ITemporaryFileWriter` et du writer positionnel : chemin absolu, écriture, flush
  asynchrone puis flush disque avant retour de la frontière confirmable.
- Création de Persistence avec `Microsoft.Data.Sqlite`, écrivain sérialisé, WAL,
  `synchronous=FULL`, clés étrangères, migration v1 transactionnelle/checksummée et restauration du
  domaine. Query, fragment et identifiants d’URL ne sont pas persistés.
- Ajout des projets et tests Network/Storage/Persistence, verrous NuGet pour les neuf projets et
  documentation G2.

## Fichiers créés

- `src/WindowsDownloadManager.Application/Abstractions/ITemporaryFileWriter.cs`
- `src/WindowsDownloadManager.Network/Http/IHostAddressResolver.cs`
- `src/WindowsDownloadManager.Network/Http/DnsHostAddressResolver.cs`
- `src/WindowsDownloadManager.Network/Http/HttpNetworkClientFactory.cs`
- `src/WindowsDownloadManager.Network/Security/INetworkAddressPolicy.cs`
- `src/WindowsDownloadManager.Network/Security/PublicNetworkAddressPolicy.cs`
- `src/WindowsDownloadManager.Storage/WindowsDownloadManager.Storage.csproj`
- `src/WindowsDownloadManager.Storage/Files/DurableTemporaryFileWriter.cs`
- `src/WindowsDownloadManager.Persistence/WindowsDownloadManager.Persistence.csproj`
- `src/WindowsDownloadManager.Persistence/Sqlite/SqliteDownloadRepository.cs`
- Projets/fichiers de tests Storage et Persistence, deux fixtures Network et fichiers
  `packages.lock.json` manquants des projets produit.

## Fichiers modifiés

- `Directory.Build.props`, `WindowsDownloadManager.slnx`
- `src/WindowsDownloadManager.Domain/Downloads/DownloadTask.cs`
- `src/WindowsDownloadManager.Network/Http/HttpRemoteResourceAnalyzer.cs`
- `src/WindowsDownloadManager.Network/Security/PublicHttpUriSafetyValidator.cs`
- `tests-dotnet/WindowsDownloadManager.Network.Tests/HttpRemoteResourceAnalyzerTests.cs`
- `tests-dotnet/WindowsDownloadManager.Network.Tests/LoopbackHttpServer.cs`
- Les fichiers de verrou NuGet concernés.
- Les 16 documents permanents vérifiés ; 15 mis à jour, `SUIVI_DEVELOPPEMENT.md` inclus.

## Fichiers supprimés

- Aucun.

## Décisions prises

Refuser tout résultat DNS mixte plutôt que choisir silencieusement une IP. Le profil initial reste
sans proxy. Le writer ne retourne qu’après `Flush(true)`. SQLite utilise un pooling désactivé pour
une durée de vie explicite. Les URL persistées sont expurgées. SQLitePCLRaw 2.1.12 est épinglé afin
d’interdire la version vulnérable 2.1.11.

## Problèmes rencontrés

- La première restauration dans le sandbox a échoué en `NU1301`/`NU1900`, faute d’accès réseau.
- La restauration connectée de `Microsoft.Data.Sqlite` a échoué en `NU1903` : dépendance transitive
  `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 affectée par GHSA-2m69-gcr7-jv3q, gravité élevée.
- Aucun test de crash réel, disque plein, proxy ou NAT64 n’était exécutable dans cette tranche.

## Solutions appliquées

L’accès réseau autorisé a permis la restauration auditée. La dépendance native a été relevée et
verrouillée à 2.1.12 ; la restauration, le build, les tests et l’audit transitif ont ensuite réussi.
L’alerte n’a jamais été contournée pour produire une réussite.

## Tests exécutés

- Baseline Network : 11 exécutés, 11 réussis, 0 échec, 0 ignoré, 1,758 s.
- Build Release final : RÉUSSI, 0 avertissement, 0 erreur, 12,04 s.
- MSTest final : 26 exécutés, 26 réussis, 0 échec, 0 ignoré, 4,383 s.
- Network : 15 réussis, dont rebinding public→loopback bloqué avant connexion, lot mixte refusé,
  politique IPv4/IPv6 et handler direct sûr.
- Storage : 3 réussis, écriture/flush, chemin relatif et annulation.
- Persistence : 5 réussis, round-trip, migration/checksum, ID absent et chemin relatif.
- Restauration `--locked-mode -p:NuGetAudit=false` : RÉUSSIE, 9 projets.
- `dotnet format --verify-no-changes` : RÉUSSI.
- Audit NuGet connecté transitif : RÉUSSI, aucun paquet vulnérable signalé après correction.
- Contrôle documentaire : RÉUSSI, 16/16 documents, 36/36 exigences, 35 tâches et liens/IDs cohérents.
- Crash entre disque/base, disque plein, corruption, migration N-1, finalisation, proxy, TLS public,
  DNS hostile réel, NAT64 et redémarrage Windows : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Le profil réseau direct résiste au rebinding simulé vers loopback. Les adaptateurs fichier et SQLite
existent et sont testés isolément. Aucun téléchargement C# de bout en bout n’existe encore ; G2 reste
PARTIELLE et M-003/M-004/M-005 restent PARTIELLES.

## Risques découverts

Incident concret R-024 : SQLitePCLRaw 2.1.11 vulnérable. Corrigé avant utilisation par 2.1.12 et
audit final. R-004 est réduit pour le profil direct mais reste critique pour proxy/NAT64. R-002 et
R-017 restent ouverts faute d’injection de crash/corruption.

## État final de la tâche

PARTIEL

## Travail restant

- Relier analyse, flux, writer, flush et dépôt dans un orchestrateur headless à connexion unique.
- Injecter crashs aux frontières disque/base et implémenter récupération/finalisation ADR-029.
- Tester disque plein, collision, corruption, migration N-1 et redémarrage Windows.
- Concevoir séparément les profils proxy/NAT64 avant de les activer.

## Prochaine action

Créer l’orchestrateur minimal qui télécharge en connexion unique dans un temporaire, confirme SQLite
après flush, puis ajouter la récupération conservative et la finalisation réparable.

## Commit associé

Aucun commit créé : identité Git non configurée et dépôt toujours sans baseline.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | État réel C# Storage/SQLite |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | M-004/M-005 PARTIEL, G2 partielle |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée G2 ajoutée sans effacement |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Network/Storage/Persistence réels |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-002/004/005/007/017/024 révisés |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Preuve 26/26 et non-exécutés |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | G2 partielle et prochaine action |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | ADR-026/027 état d’application |
| REGLES_DE_CODAGE.md | MIS À JOUR | Règles réseau, flush, URL et migration |
| DEPENDANCES.md | MIS À JOUR | SQLite installé et incident 2.1.11 |
| MODELISATION_DONNEES.md | MIS À JOUR | Schéma C# v1 réel |
| SECURITE.md | MIS À JOUR | Anti-rebind, secrets et vulnérabilité native |
| PERFORMANCES.md | MIS À JOUR | Flush/FULL sans benchmark revendiqué |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée G2 et épingle SQLitePCLRaw |
| ERREURS_CONNNUES.md | MIS À JOUR | BUG-002 corrigée, limites actualisées |
| INSTRUCTIONS_IA.md | MIS À JOUR | Alertes NU1901-NU1904 bloquantes |

---

# 2026-08-03 — 23:35 UTC — M-001/M-003/M-004/M-005 — Orchestrateur neuf durable

## Objectif

Créer l’orchestrateur headless à connexion unique reliant l’analyse HTTP, le flux distant, le fichier
temporaire durable et le dépôt SQLite, avec confirmation de progression uniquement après flush.

## État avant intervention

Les adaptateurs réseau, Storage et Persistence existaient et leurs 26 tests isolés réussissaient.
Ils n’étaient reliés par aucun cas d’usage ; aucun téléchargement C# de bout en bout, checkpoint
coordonné fichier/base ou test d’intégration n’existait.

## Travail effectué

- Ajout du port `IRemoteContentSource`, de la ressource `RemoteContentLease`, du résultat
  `DownloadRunResult` et de `DownloadOrchestrator.RunNewAsync`.
- L’orchestrateur persiste les transitions, analyse avant transfert, prépare exclusivement le
  temporaire, lit avec un buffer mutualisé de 64 Kio, flush chaque bloc, confirme sa frontière puis
  sauvegarde SQLite. Taille modifiée, corps court/long ou frontière inattendue provoquent un arrêt.
- Ajout de `HttpRemoteContentSource` : validation de chaque saut, redirections manuelles, encodage
  `identity`, `If-Match` fort ou `If-Unmodified-Since`, contrôle strict de `200/206` et
  `Content-Range`.
- Renforcement du writer : `PrepareNewAsync` avec `FileMode.CreateNew`; les écritures exigent un
  temporaire déjà préparé et ne recréent jamais silencieusement un fichier disparu.
- Ajout de projets Application.Tests et Integration.Tests et d’une preuve réelle loopback → fichier
  durable → SQLite.
- Correction d’une faute documentaire résiduelle dans `MODELISATION_DONNEES.md` et mise à jour de
  la vérité G2 sans prétendre que reprise ou finalisation existent.

## Fichiers créés

- `src/WindowsDownloadManager.Application/Abstractions/IRemoteContentSource.cs`
- `src/WindowsDownloadManager.Application/Downloads/DownloadRunResult.cs`
- `src/WindowsDownloadManager.Application/Downloads/DownloadOrchestrator.cs`
- `src/WindowsDownloadManager.Network/Http/HttpRemoteContentSource.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/WindowsDownloadManager.Application.Tests.csproj`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/DownloadOrchestratorTests.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/packages.lock.json`
- `tests-dotnet/WindowsDownloadManager.Network.Tests/HttpRemoteContentSourceTests.cs`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/WindowsDownloadManager.Integration.Tests.csproj`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DownloadOrchestratorIntegrationTests.cs`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/packages.lock.json`

## Fichiers modifiés

- `WindowsDownloadManager.slnx`
- `src/WindowsDownloadManager.Application/Abstractions/ITemporaryFileWriter.cs`
- `src/WindowsDownloadManager.Storage/Files/DurableTemporaryFileWriter.cs`
- `tests-dotnet/WindowsDownloadManager.Storage.Tests/DurableTemporaryFileWriterTests.cs`
- `ARCHITECTURE_TECHNIQUE.md`, `FEUILLE_DE_ROUTE.md`, `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`, `REGLES_DE_CODAGE.md`, `DEPENDANCES.md`
- `MODELISATION_DONNEES.md`, `SECURITE.md`, `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`, `PERFORMANCES.md`, `FAQ_TECHNIQUE.md`
- `SUIVI_DEVELOPPEMENT.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Limiter ce cas d’usage à une tâche neuve : une reprise sans chemin temporaire ni identité distante
persistés serait trompeuse. Arrêter en état `VERIFYING` après un flux exact et ne pas implémenter un
rename incomplet avant le protocole ADR-029. Créer le temporaire exclusivement afin de préserver tout
fichier préexistant. Réutiliser les dépendances existantes ; aucune nouvelle bibliothèque ajoutée.

## Problèmes rencontrés

- La première restauration verrouillée a échoué en `NU1900`, le bac à sable ne pouvant joindre le
  service d’avis NuGet. La restauration connectée autorisée a ensuite réussi.
- Le premier appel direct de `eng/verify.ps1` a été bloqué par la stratégie PowerShell avant toute
  étape projet (`PSSecurityException`).
- Crash réel, disque plein, redémarrage Windows et finalisation n’étaient pas dans cette tranche.

## Solutions appliquées

La restauration a été relancée avec accès réseau sans désactiver l’audit. La commande canonique a
été exécutée dans un nouveau processus PowerShell avec `-ExecutionPolicy Bypass`, limité à ce
processus. Les limites non traitées restent explicitement ouvertes.

## Tests exécutés

- Baseline : `dotnet test WindowsDownloadManager.slnx --no-restore` — RÉUSSI, 26 exécutés,
  26 réussis, 0 échec, 0 ignoré, 21,671 s.
- Restauration `--locked-mode` sans accès réseau — ÉCHEC `NU1900`; aucun échec du code observé.
- Restauration `--locked-mode` connectée — RÉUSSIE pour les nouveaux projets et leurs verrous.
- Build Debug — RÉUSSI, 0 avertissement, 0 erreur, 26,84 s.
- Suite Debug intermédiaire — RÉUSSIE, 36 exécutés, 36 réussis, 0 échec, 0 ignoré, 10,980 s.
- Test d’intégration isolé — RÉUSSI, 1 exécuté, 1 réussi, 0 échec, 0 ignoré, 12,259 s.
- Appel direct `eng/verify.ps1` — NON EXÉCUTÉ par PowerShell, bloqué par la stratégie locale.
- Vérification canonique finale : `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  eng/verify.ps1` — RÉUSSIE.
- Build Release final : RÉUSSI, 0 avertissement, 0 erreur, 52,04 s.
- MSTest Release final : RÉUSSI, 37 exécutés, 37 réussis, 0 échec, 0 ignoré, 24,815 s.
- `dotnet format --verify-no-changes` : RÉUSSI.
- Contrôle documentaire : RÉUSSI, 16/16 documents, 36/36 exigences, 35 tâches, comptes, IDs et
  références cohérents.
- Crash processus entre flush/SQLite, disque plein réel, corruption, migration N-1, reprise après
  redémarrage, finalisation/rename, proxy, NAT64, TLS public et performances : NON EXÉCUTÉS.
  Résultat inconnu.

## Résultats

Un téléchargement C# neuf est désormais orchestré et prouvé jusqu’à `VERIFYING`. Le test
d’intégration écrit exactement `hello` dans le temporaire, puis restaure 5 octets confirmés depuis
SQLite. Aucun octet non flushé n’est confirmé lorsque le writer échoue. G2 reste PARTIELLE : ce
chemin n’est ni une reprise après crash ni une finalisation.

## Risques découverts

Aucun nouveau risque identifié. R-002 est réduit pour l’ordre normal flush→checkpoint mais reste
ouvert aux crashs réels. R-003 est réduit pour les réponses de transfert contradictoires. R-006 reste
ouvert pour disque plein, retrait et erreur de flush réelle.

## État final de la tâche

PARTIEL

## Travail restant

- Persister le chemin temporaire et l’identité distante nécessaires à la reprise sûre.
- Réconcilier base et taille réelle du temporaire au démarrage, sans avancer la progression.
- Injecter des crashs avant/après flush et commit, puis tester disque plein et corruption.
- Implémenter l’intention `FINALIZING`, le rename même volume et la réparation ADR-029.

## Prochaine action

Étendre l’orchestrateur aux tâches persistées : restaurer la position sûre `min(base, disque)`,
réanalyser l’identité distante, reprendre seulement après validation, puis exécuter PR-032 avant la
finalisation ADR-029.

## Commit associé

Aucun commit créé : identité Git non configurée et dépôt toujours sans baseline.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | VÉRIFIÉ — NON CONCERNÉ | Besoin fonctionnel inchangé |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | G2 et M-001/M-004/F-015 actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Présente entrée ajoutée sans effacement |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Flux orchestré et contrats documentés |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-002/R-003/R-006 révisés |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Preuve Release 37/37 et non-exécutés |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Orchestrateur présent et prochaine action reprise |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Application partielle ADR-025/027 |
| REGLES_DE_CODAGE.md | MIS À JOUR | Création exclusive et ordre de checkpoint |
| DEPENDANCES.md | MIS À JOUR | Deux projets de test, aucune nouvelle dépendance runtime |
| MODELISATION_DONNEES.md | MIS À JOUR | Checkpoint v1 et données manquantes de reprise |
| SECURITE.md | MIS À JOUR | Contrôles du flux HTTP et temporaire |
| PERFORMANCES.md | MIS À JOUR | Buffer/flush décrits sans gain revendiqué |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée réelle G2 expliquée |
| ERREURS_CONNNUES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun nouveau bug confirmé |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent inchangé |

---

## 2026-08-11 — M-007/Q-001/ADR-029 — Crashs subprocess de finalisation

### Objectif

Prouver les états réparables aux trois frontières `Finalizing → move → Completed` après une
terminaison brutale réelle du processus, puis vérifier la convergence vers un fichier exact et un
état SQLite `Completed`.

### État avant intervention

La finalisation et sa réparation étaient couvertes en processus, mais aucune interruption subprocess
n’avait prouvé la persistance réelle autour du move. La baseline canonique était de 122 tests.

### Travail effectué

- Ajout des frontières `AfterFinalizingCommit`, `AfterFinalMove` et `AfterCompletedCommit` au
  `WindowsDownloadManager.CrashTestHost`.
- Terminaison après commit de l’intention, après move réel et après commit final.
- Réouverture séparée de SQLite par le parent après chaque mort du subprocess.
- Réparation avec les vrais adaptateurs lorsque SQLite reste en `Finalizing`.
- Vérification exacte du contenu, de l’absence/présence des deux chemins et de l’état final.

### Fichiers créés ou supprimés

Aucun.

### Fichiers de code modifiés

- `tests-dotnet/WindowsDownloadManager.CrashTestHost/Program.cs`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DurabilityFaultInjectionIntegrationTests.cs`

### Décisions et invariants

Aucun nouvel ADR. Le harnais applique ADR-029 sans instrumentation de production. Après intention,
le temporaire seul est déplacé par réparation. Après move, la destination seule est confirmée sans
second move. Après commit final, aucune réparation n’est lancée. Les états ambigus restent bloqués.

### Tests exécutés et résultats

- Integration ciblé Release : 19 exécutés, 19 réussis, 0 échec, 0 ignoré en 32,185 s.
- Vérification canonique `eng/verify.ps1` : restauration hors ligne RÉUSSIE ; compilation Release
  0 avertissement/0 erreur ; 125 exécutés, 125 réussis, 0 échec, 0 ignoré en 23,175 s ; formatage
  RÉUSSI ; documentation 16/16, exigences 36/36 et 35 tâches cohérentes.
- SHA-256, disque plein, antivirus/verrou, inter-volume, panne électrique et reboot Windows :
  NON EXÉCUTÉS ; résultat inconnu.

### Risques et statut réel

R-011/R-021 sont réduits mais restent ouverts. La tranche est RÉUSSIE pour les trois frontières de
processus, sans valider les pannes matérielles ou l’intégrité cryptographique finale.

### Prochaine action

Ajouter le calcul SHA-256 streaming du temporaire avant `Finalizing`, comparer une empreinte attendue
si disponible et refuser la finalisation sur divergence.

### Commit associé

Commit `test: cover finalization crash boundaries` sur `main`.

### Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Trois frontières et limites |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | M-007/Q-001/G2 et prochaine action |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Présente entrée ajoutée |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Protocole subprocess décrit |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-011/R-021 réduits |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Section 21 et preuves 125/125 |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, limites et suite |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Extension ADR-029 consignée |
| REGLES_DE_CODAGE.md | MIS À JOUR | Règle des trois frontières |
| DEPENDANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun paquet ou verrou modifié |
| MODELISATION_DONNEES.md | MIS À JOUR | Trois états disque/SQLite observés |
| SECURITE.md | MIS À JOUR | Dix frontières bornées |
| PERFORMANCES.md | MIS À JOUR | Temps fonctionnels non assimilés à un benchmark |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-011 précisée |
| FAQ_TECHNIQUE.md | MIS À JOUR | Résistance crash partielle expliquée |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent inchangé |

---

# 2026-08-04 — 01:55 UTC — M-001/M-005/M-007/M-008 — Métadonnées persistantes de reprise

## Objectif

Persister le chemin du fichier temporaire et l’identité distante minimale afin qu’une future reprise
C# puisse réconcilier base, disque et serveur sans supposition.

## État avant intervention

L’orchestrateur neuf téléchargeait et confirmait SQLite, mais `DownloadTask` ne conservait ni chemin
temporaire ni identité. Le schéma C# restait en version 1. Une réouverture restaurait seulement URL
originale expurgée, destination, état et octets confirmés ; toute reprise sûre était impossible.

## Travail effectué

- Création du record domaine `RemoteIdentity` : URL finale, taille nullable, ETag, Last-Modified et
  capacité Range.
- Extension de `DownloadTask` avec `TemporaryPath`, `RemoteIdentity`, restauration compatible et
  invariant de préparation en état `PREPARING`.
- Modification de l’orchestrateur : après analyse, chemin et identité sont sauvegardés ensemble
  avant création exclusive du temporaire. Un échec SQLite ne crée aucun fichier.
- Remplacement du mécanisme de migration unique par une séquence v1/v2 checksummée, avec refus des
  versions futures et vérification de chaque migration existante.
- Migration v2 additive : six colonnes de reprise et index unique partiel sur le chemin temporaire.
- Lecture/écriture SQLite des métadonnées, expurgation de l’URL finale et rejet d’un ensemble
  incomplet ou d’une capacité Range invalide.
- Tests de montée v1→v2 sans perte, round-trip complet, identité incomplète, ordre checkpoint/fichier
  et restauration d’intégration.
- Correction documentaire : le statut d’orchestrateur avait été placé sur ADR-021 ; il appartient
  à ADR-025. ADR-021 décrit de nouveau uniquement l’adoption du moteur .NET headless.

## Fichiers créés

- `src/WindowsDownloadManager.Domain/Downloads/RemoteIdentity.cs`

## Fichiers modifiés

- `src/WindowsDownloadManager.Domain/Downloads/DownloadTask.cs`
- `src/WindowsDownloadManager.Application/Downloads/DownloadOrchestrator.cs`
- `src/WindowsDownloadManager.Persistence/Sqlite/SqliteDownloadRepository.cs`
- `tests-dotnet/WindowsDownloadManager.Domain.Tests/DownloadTaskTests.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/DownloadOrchestratorTests.cs`
- `tests-dotnet/WindowsDownloadManager.Persistence.Tests/SqliteDownloadRepositoryTests.cs`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DownloadOrchestratorIntegrationTests.cs`
- Les documents indiqués dans le contrôle documentaire ci-dessous.

## Fichiers supprimés

- Aucun.

## Décisions prises

Conserver l’identité minimale directement dans `downloads` pour cette tranche à connexion unique,
plutôt que créer prématurément une table versionnée `remote_identities`. Utiliser une migration
additive : les lignes v1 obtiennent des valeurs NULL et restent lisibles, mais ne sont pas déclarées
reprenables. Persister chemin et identité avant création du fichier évite un temporaire sans mémoire
SQLite. L’approche suit ADR-027 sans créer de nouvelle ADR ni dépendance.

## Problèmes rencontrés

- Une incohérence documentaire antérieure attribuait le statut d’orchestrateur à ADR-021 au lieu
  d’ADR-025 ; aucun comportement de code n’était affecté.
- Aucune base utilisateur réelle n’existe dans le dépôt pour tester une migration destructive ; le
  test reconstruit explicitement un schéma v1 avec une ligne existante.
- Les tests ciblés et builds sont lents dans l’environnement, mais aucun timeout final n’a eu lieu.

## Solutions appliquées

Le statut ADR a été corrigé en préservant les décisions. Le test v1 crée une ligne, reconstruit le
schéma historique exact, applique la v2, puis vérifie données, métadonnées nulles et deux checksums.
Les erreurs de cohérence provoquent un arrêt explicite au lieu d’une restauration partielle.

## Tests exécutés

- Baseline Release : 37 exécutés, 37 réussis, 0 échec, 0 ignoré, 30,726 s.
- Build Debug après modification : RÉUSSI, 0 avertissement, 0 erreur, 98,73 s.
- Tests ciblés Debug : Domain 5/5, Persistence 7/7, Application 5/5, Integration 1/1 ;
  18 exécutés, 18 réussis, 0 échec, 0 ignoré.
- Test Application supplémentaire après ajustement : 5/5 réussis, 0 échec, 0 ignoré, 4,178 s.
- Commande canonique : `powershell.exe -NoProfile -ExecutionPolicy Bypass -File eng/verify.ps1` —
  RÉUSSIE.
- Restauration verrouillée hors ligne : RÉUSSIE, 11 projets à jour.
- Build Release final : RÉUSSI, 0 avertissement, 0 erreur, 42,55 s.
- MSTest Release final : 42 exécutés, 42 réussis, 0 échec, 0 ignoré, 18,216 s.
- Formatage .NET : RÉUSSI, aucun changement restant.
- Contrôle documentaire final après mise à jour : RÉUSSI, 16/16 documents, 36/36 exigences,
  35 tâches, comptes, identifiants et références cohérents.
- Audit NuGet connecté : NON EXÉCUTÉ, aucune dépendance ni version modifiée. Résultat courant non
  réévalué ; dernier audit observé du 2026-08-03 sans vulnérabilité signalée.
- Interruption réelle de migration, backup/rollback, corruption SQLite, réconciliation de longueur,
  recouvrement, reprise réseau, crash PR-032 et redémarrage Windows : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Une nouvelle tâche sauvegarde désormais avant création du fichier un chemin temporaire unique et une
identité distante expurgée. La réouverture SQLite restaure ces données et une base v1 est montée en
v2 sans perte dans le test. Cette tranche fournit les préconditions de reprise, pas la reprise elle-même.

## Risques découverts

Aucun nouveau risque technique distinct. R-001/R-002 sont réduits par les préconditions persistées et
l’ordre checkpoint→fichier. R-007/R-017 sont réduits pour la montée additive normale. R-005/R-016
restent ouverts car le chemin local est une donnée privée en clair et l’URL ne conserve volontairement
aucun secret réutilisable.

## État final de la tâche

PARTIEL

## Travail restant

- Ajouter un port d’inspection du temporaire et calculer `min(confirmed_bytes, taille disque)`.
- Refuser/tronquer prudemment tout surplus après journalisation et preuve de récupération.
- Réanalyser le distant et comparer l’identité persistée avant toute demande Range non nulle.
- Tester crash de migration, backup/rollback, PR-032 et redémarrage Windows.

## Prochaine action

Créer la réconciliation de démarrage en lecture seule d’abord : classifier temporaire absent, plus
court, égal ou plus long que SQLite, retourner une décision typée sans encore tronquer ni reprendre.

## Commit associé

Aucun commit créé : identité Git non configurée et dépôt toujours sans baseline.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | État réel C# et limites de reprise |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | M-001/M-005/M-007/M-008 et prochaine action |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Présente entrée ajoutée sans effacement |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Identité, ordre de préparation et migration v2 |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-001/002/005/007/016/017 révisés |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Preuves 42/42 et non-exécutés |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Schéma v2, preuve finale et prochaine action |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | ADR-021 corrigée, ADR-025/027 actualisées |
| REGLES_DE_CODAGE.md | MIS À JOUR | Invariants migration et préparation |
| DEPENDANCES.md | MIS À JOUR | Aucune nouvelle dépendance, total de tests actualisé |
| MODELISATION_DONNEES.md | MIS À JOUR | Dictionnaire complet du schéma v2 |
| SECURITE.md | MIS À JOUR | Données persistées, expurgation et unicité |
| PERFORMANCES.md | MIS À JOUR | Impact non mesuré, aucune amélioration revendiquée |
| FAQ_TECHNIQUE.md | MIS À JOUR | Contenu réel des métadonnées expliqué |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-006 révisée pour v1→v2 |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent inchangé |
# 2026-08-04 — 02:38 — F-016 — Réconciliation locale de démarrage en lecture seule

## Objectif

Créer une réconciliation de démarrage C# strictement en lecture seule qui restaure une tâche,
inspecte son temporaire et classe les métadonnées ou le fichier absents ainsi que les longueurs
inférieure, égale ou supérieure au checkpoint, sans modifier le disque, SQLite ou l’agrégat.

## État avant intervention

SQLite v2 persistait le chemin temporaire et l’identité distante avant création du fichier. Le dépôt
restaurait ces données, mais aucun service C# ne comparait `confirmed_bytes` à la longueur réelle du
temporaire. La baseline Release comptait 42 tests réussis. F-016 et M-007 étaient `PARTIEL`.

## Travail effectué

- Ajout du port `ITemporaryFileInspector` et du snapshot invariant absent/existant.
- Ajout de `StartupRecoveryReconciler` et d’un résultat typé couvrant cinq classifications.
- Calcul de `SafePosition` à `0` en cas d’absence, sinon à
  `min(ConfirmedBytes, FileLength)`.
- Ajout de l’adaptateur `ReadOnlyTemporaryFileInspector`, avec chemin absolu, ouverture
  `FileAccess.Read` et propagation des erreurs de verrou/permission/I/O.
- Ajout de tests Application, Storage et intégration SQLite → temporaire.
- Mise à jour des documents fonctionnels, techniques, sécurité, risques, tests, performance,
  limitations, FAQ, pilotage et état courant.

## Fichiers créés

- `src/WindowsDownloadManager.Application/Abstractions/ITemporaryFileInspector.cs`
- `src/WindowsDownloadManager.Application/Downloads/StartupRecoveryReconciler.cs`
- `src/WindowsDownloadManager.Application/Downloads/TemporaryFileReconciliationResult.cs`
- `src/WindowsDownloadManager.Storage/Files/ReadOnlyTemporaryFileInspector.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/StartupRecoveryReconcilerTests.cs`
- `tests-dotnet/WindowsDownloadManager.Storage.Tests/ReadOnlyTemporaryFileInspectorTests.cs`

## Fichiers modifiés

- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DownloadOrchestratorIntegrationTests.cs`
- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `ERREURS_CONNNUES.md`
- `FAQ_TECHNIQUE.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

La réconciliation reste un cas d’usage Application et dépend d’un port ; l’accès fichier appartient
à Storage. Aucun nouvel ADR n’est nécessaire : ADR-003 et ADR-004 imposent déjà la borne basse et
l’arrêt conservateur. Le mot « sûr » désigne uniquement une position diagnostique ; il n’autorise
ni troncature, ni écriture, ni reprise avant comparaison distante et politique réparatrice testée.

## Problèmes rencontrés

- Le premier lancement `dotnet` a tenté d’écrire dans `C:\Users\EMILO\.dotnet` et a échoué avant
  compilation ; l’environnement CLI n’était pas encore confiné au workspace.
- Un groupe de trois commandes de tests ciblés a dépassé 120 secondes sans résultat exploitable.
- La première compilation des nouveaux tests a échoué sur la casse du paramètre nommé
  `supportsByteRanges`.
- La première suite complète après compilation a donné 52 réussites et 1 échec : la fixture
  d’intégration tentait la transition invalide `Preparing → Downloading`.

## Solutions appliquées

- Configuration de `DOTNET_CLI_HOME`, `APPDATA` et des opt-out de télémétrie dans le workspace.
- Isolation puis relance des tests avec une limite adaptée.
- Correction du nom de paramètre selon le contrat réel de `RemoteIdentity`.
- Ajout de la transition obligatoire `Preparing → Waiting → Downloading` dans la fixture uniquement.
- Relance complète puis exécution de la commande canonique.

## Tests exécutés

- Baseline : `dotnet test WindowsDownloadManager.slnx -c Release --no-restore` avec environnement
  confiné — 42 exécutés, 42 réussis, 0 échec, 0 ignoré, 7,815 s : RÉUSSI.
- Première compilation ciblée Application — 0 test, erreur CS1739 : ÉCHEC.
- Première suite complète nouvelle — 53 exécutés, 52 réussis, 1 échec, 0 ignoré, 23,310 s : ÉCHEC.
- Non-régression après correction — 53 exécutés, 53 réussis, 0 échec, 0 ignoré, 22,709 s : RÉUSSI.
- Canonique : `powershell.exe -NoProfile -ExecutionPolicy Bypass -File eng/verify.ps1` — restauration
  hors ligne RÉUSSIE ; build Release 0 avertissement/0 erreur ; 53 exécutés, 53 réussis, 0 échec,
  0 ignoré, 16,710 s ; formatage RÉUSSI ; contrôle documentaire RÉUSSI.
- Tests de crash processus, redémarrage Windows, troncature, reprise HTTP, comparaison distante,
  reparse points/ACL et course inspection/action : NON EXÉCUTÉS. Résultat inconnu.
- Tests de performance : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Les cinq classifications locales sont déterministes et ne modifient pas la tâche. Le fichier absent
produit une position 0 ; les longueurs 4, 5 et 6 face au checkpoint 5 produisent respectivement les
positions 4, 5 et 5. L’intégration restaure 5 depuis SQLite, observe 7 sur disque et laisse fichier,
état et checkpoint inchangés. Un fichier verrouillé lève une erreur d’I/O au lieu d’être déclaré absent.

## Risques découverts

Aucun nouveau risque critique. La course possible entre inspection et future action ainsi que les
reparse points/ACL non vérifiés restent ouverts et sont rattachés à R-002, R-011, R-012, R-018 et
R-021. La lecture seule réduit le risque de mutation destructive prématurée sans clore PR-032.

## État final de la tâche

PARTIEL

## Travail restant

- Réanalyser et comparer l’identité distante sans ouvrir de flux de reprise.
- Définir les décisions conservatrices pour chaque classification locale et distante.
- Valider nature du fichier, reparse points, ACL et changement concurrent avant toute action.
- Implémenter ensuite la mutation/troncature éventuelle avec audit et tests d’arrêt injecté.
- Exécuter PR-032, redémarrage Windows et reprise réseau de bout en bout.

## Prochaine action

Ajouter une réconciliation distante en lecture seule qui compare l’identité persistée à une nouvelle
analyse et retourne une décision typée, sans écrire ni reprendre le transfert.

## Commit associé

Aucun commit créé. Le dépôt `main` ne possède toujours aucun commit et l’identité Git n’est pas
configurée dans l’état observé.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | État réel de la réconciliation locale précisé |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | F-016/M-007 et prochaine action actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée F-016 ajoutée sans effacer l’historique |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Port, adaptateur, classifications et absence de mutation documentés |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-002/R-011 et risques fichiers résiduels révisés |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Preuves 42, échec 52/53 et succès canonique 53/53 consignés |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, limites, preuves et prochaine action actualisées |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Mise en œuvre ADR-003 précisée sans nouvelle décision |
| REGLES_DE_CODAGE.md | MIS À JOUR | Règle lecture seule et propagation des erreurs ajoutée |
| DEPENDANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucune dépendance ni version modifiée |
| MODELISATION_DONNEES.md | MIS À JOUR | Diagnostic sans nouvelle donnée persistante documenté |
| SECURITE.md | MIS À JOUR | Lecture seule, erreurs explicites et limites fichiers précisées |
| PERFORMANCES.md | MIS À JOUR | Coût non mesuré et absence de revendication consignés |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-009 ajoutée |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée diagnostique expliquée |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Méthode permanente appliquée sans changement |
---

# 2026-08-04 — 11:09 — M-007/M-008/F-013 — Réconciliation distante en lecture seule

## Objectif

Réanalyser le distant et comparer la nouvelle observation à `RemoteIdentity` sans ouvrir de flux de
transfert, sans modifier le temporaire, sans changer l’agrégat et sans écrire dans SQLite.

## État avant intervention

Le chemin et l’identité distante étaient persistés en SQLite v2. La réconciliation locale classait
déjà cinq relations entre checkpoint et longueur disque. Aucune comparaison C# ne distinguait encore
identité distante compatible, preuve insuffisante, perte de Range ou contradiction. La baseline
Release comptait 53 tests réussis.

## Travail effectué

- Création de `RemoteIdentityReconciler`, dépendant uniquement d’`IRemoteResourceAnalyzer`.
- Création d’un résultat typé avec cinq statuts et des différences cumulables.
- Normalisation/expurgation des URI finales avant comparaison et exposition.
- Comparaison conservatrice d’URL finale, taille, ETag, Last-Modified et capacité Range.
- Seuil de compatibilité : ETag fort identique, ou taille + Last-Modified identiques.
- Classification d’une preuve connue disparue ou de signaux trop faibles comme insuffisants.
- Tests unitaires des branches et intégration avec une unique sonde HTTP `bytes=0-0`.
- Mise à jour du pilotage, de l’architecture, de la sécurité, des risques et des preuves.

## Fichiers créés

- `src/WindowsDownloadManager.Application/Downloads/RemoteIdentityReconciler.cs`
- `src/WindowsDownloadManager.Application/Downloads/RemoteIdentityReconciliationResult.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/RemoteIdentityReconcilerTests.cs`

## Fichiers modifiés

- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DownloadOrchestratorIntegrationTests.cs`
- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `DEPENDANCES.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `ERREURS_CONNNUES.md`
- `FAQ_TECHNIQUE.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Aucun nouvel ADR : ADR-004 impose déjà l’intégrité avant reprise. Le comparateur reste dans
`Application` et utilise le port d’analyse existant ; il ne dépend ni du port de contenu, ni de
Storage, ni de Persistence. Une différence d’un signal connu est contradictoire. Une valeur connue
devenue absente ou une URL/ETag faible sans preuve composite est insuffisante. La perte de Range est
séparée d’un changement d’identité. Le résultat observé ne conserve aucun secret d’URI.

## Problèmes rencontrés

La première exécution Application a donné 19 réussites et 1 échec. La fabrique du test remplaçait une
date volontairement absente par la date par défaut, empêchant le scénario de vérifier
`LastModifiedEvidenceMissing`. Aucun défaut du code de production n’a été observé dans cet échec.
Le premier contrôle documentaire relancé après l’ajout du journal a dépassé sa limite sans sortie ;
son résultat est inconnu et une seconde exécution avec délai étendu est nécessaire.

## Solutions appliquées

La fixture distingue désormais explicitement « date absente » de « date par défaut ». Les tests
Application, l’intégration, la non-régression complète puis la vérification canonique ont été relancés.

## Tests exécutés

- Baseline Release : `dotnet test WindowsDownloadManager.slnx -c Release --no-restore` —
  53 exécutés, 53 réussis, 0 échec, 0 ignoré, 14,822 s : RÉUSSI.
- Première exécution Application : 20 exécutés, 19 réussis, 1 échec, 0 ignoré, 11,180 s : ÉCHEC.
- Application après correction : 20 exécutés, 20 réussis, 0 échec, 0 ignoré, 13,534 s : RÉUSSI.
- Integration ciblée : 3 exécutés, 3 réussis, 0 échec, 0 ignoré, 12,006 s : RÉUSSI.
- Non-régression Release : 64 exécutés, 64 réussis, 0 échec, 0 ignoré, 13,464 s : RÉUSSI.
- Canonique : `powershell.exe -NoProfile -ExecutionPolicy Bypass -File eng/verify.ps1` — restauration
  hors ligne RÉUSSIE ; build Release 0 avertissement/0 erreur ; 64 exécutés, 64 réussis, 0 échec,
  0 ignoré, 9,801 s ; formatage RÉUSSI ; contrôle documentaire RÉUSSI.
- Premier contrôle documentaire post-journal : délai dépassé sans sortie. Résultat inconnu.
- Second contrôle documentaire post-journal : RÉUSSI en 65,8 s ; 16/16 documents, 36/36 exigences,
  35 tâches, comptes, identifiants et références cohérents.
- Recouvrement binaire, hash officiel, nouveau lien, course sonde/reprise, mutation, reprise HTTP,
  crash réel et redémarrage Windows : NON EXÉCUTÉS. Résultat inconnu.
- Tests de performance et audit NuGet connecté : NON EXÉCUTÉS. Résultat inconnu ; aucune dépendance
  ni version n’a changé.

## Résultats

Les statuts `RecoveryMetadataAbsent`, `Compatible`, `InsufficientEvidence`,
`ResumeCapabilityLost` et `Contradictory` sont testés. Les contradictions URL/taille/ETag/date sont
détectées, les preuves disparues sont refusées, un ETag faible seul reste insuffisant et les secrets
de query/fragment ne sortent pas dans le résultat. L’intégration observe exactement une sonde et
conserve le temporaire, l’état et le checkpoint inchangés.

## Risques découverts

Aucun nouveau risque distinct. R-001 est réduit pour la détection de contradictions, mais demeure
critique tant que recouvrement, hash, nouveau lien et course entre sonde et future requête ne sont pas
traités. Une correspondance diagnostique ne constitue pas une autorisation de reprise.

## État final de la tâche

PARTIEL

## Travail restant

- Composer les résultats local et distant dans une décision de récupération unique.
- Bloquer explicitement absence, contradiction, preuve insuffisante et perte de Range.
- Ajouter la vérification de recouvrement pour le seul cas compatible.
- Tester nouveau lien légitime, PR-052/061, course sonde/reprise et crash PR-032.
- N’implémenter la mutation ou la reprise qu’après ces preuves.

## Prochaine action

Créer un évaluateur de récupération en lecture seule qui combine
`TemporaryFileReconciliationResult` et `RemoteIdentityReconciliationResult` et retourne une décision
typée, sans troncature, sauvegarde ou ouverture de flux.

## Commit associé

Aucun commit créé. Le dépôt `main` reste sans commit initial et l’identité Git n’est pas configurée
dans l’état observé.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Comparaison distante et limites réelles précisées |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | M-007/M-008/F-013/F-016 et prochaine action actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée sans effacer l’historique |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Service, frontières, statuts et seuil de preuve documentés |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-001 réduit sans être clos |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Échec de fixture et preuves 64/64 consignés |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, limites, tests et prochaine action actualisés |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Mise en œuvre ADR-004 précisée sans nouvel ADR |
| REGLES_DE_CODAGE.md | MIS À JOUR | Règles de comparaison et d’expurgation ajoutées |
| DEPENDANCES.md | MIS À JOUR | 64 tests et absence de nouvelle dépendance consignés |
| MODELISATION_DONNEES.md | MIS À JOUR | Identité observée en mémoire sans écriture documentée |
| SECURITE.md | MIS À JOUR | Sonde sécurisée, expurgation et arrêt conservateur précisés |
| PERFORMANCES.md | MIS À JOUR | Coût de sonde non mesuré documenté |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-009 actualisée |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée locale/distante expliquée |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent appliqué sans changement |

---

# 2026-08-04 — 18:01 — M-007/Q-001/F-015 — Crash subprocess pendant le second bloc

## Objectif

Étendre le banc subprocess à un contenu de 70 000 octets, tuer pendant la deuxième opération après
flush, avant commit SQLite et après commit, puis vérifier les checkpoints restaurés 65 536/70 000.

## État avant intervention

Le host couvrait trois terminaisons brutales sur un seul bloc de 5 octets. Les états 0/5, 0/5 et 5/5
étaient prouvés par le parent, mais aucun checkpoint antérieur n’avait encore dû survivre au crash du
bloc suivant. La baseline canonique était de 107/107 tests réussis.

## Travail effectué

- Conservation des trois scénarios mono-bloc existants.
- Ajout d’un contenu déterministe de 70 000 octets partagé par le host et le test parent.
- Sélection explicite de la deuxième opération par comptage des flushs et checkpoints positifs.
- Ajout de trois frontières : après flush du second bloc, avant son commit et après son commit.
- Vérification par le parent du code de sortie non nul, du contenu intégral, de la longueur, de l’état
  SQLite, du checkpoint, de la classification locale et de la position sûre.
- Mise à jour des documents concernés et vérification des 16 documents permanents.

## Fichiers créés

- Aucun.

## Fichiers modifiés

- `tests-dotnet/WindowsDownloadManager.CrashTestHost/Program.cs`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DurabilityFaultInjectionIntegrationTests.cs`
- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `ERREURS_CONNNUES.md`
- `FAQ_TECHNIQUE.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Aucun nouvel ADR. Les scénarios mono-bloc sont conservés comme régression indépendante. Le contenu
multi-blocs est déterministe et les décorateurs ciblent un numéro d’opération, ce qui prouve que le
premier checkpoint a réellement été commité avant de provoquer la faute sur le second bloc. La
stratégie de test a privilégié les artefacts persistés observables plutôt que le seul code de sortie.

## Problèmes rencontrés

Deux applications documentaires groupées ont rencontré un contexte de ligne devenu différent dans
la section G2 de la feuille de route. `apply_patch` les a refusées atomiquement ; aucune modification
partielle n’a été appliquée. Aucun échec de compilation ou de test n’a été rencontré.

## Solutions appliquées

Les mises à jour documentaires ont été divisées en correctifs plus ciblés avec le contexte réel. Le
host généralise les décorateurs existants au lieu de dupliquer un second exécutable ou d’ajouter un
crochet de crash au produit.

## Tests exécutés

- Baseline canonique : 107 exécutés, 107 réussis, 0 échec, 0 ignoré.
- Integration.Tests Release : 13 exécutés, 13 réussis, 0 échec, 0 ignoré, 16,868 s — RÉUSSI.
- Non-régression solution Release : 110 exécutés, 110 réussis, 0 échec, 0 ignoré, 14,214 s — RÉUSSI.
- Vérification canonique `eng/verify.ps1` post-documentation : restauration hors ligne RÉUSSIE ;
  build Release RÉUSSI avec 0 avertissement/0 erreur ; 110 exécutés, 110 réussis, 0 échec, 0 ignoré,
  15,167 s ; formatage RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences
  et 35 tâches cohérentes.
- Crash avant flush, panne électrique, reboot Windows, disque plein, corruption SQLite et écriture
  partielle réelle : NON EXÉCUTÉS. Résultat inconnu.
- Tests de performance spécialisés : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Après flush du second bloc ou avant son commit, le fichier restauré contient exactement les 70 000
octets attendus et SQLite restaure 65 536 ; la réconciliation retourne `TemporaryFileLonger` et une
position sûre de 65 536. Après commit, fichier et checkpoint valent 70 000 et la classification est
`TemporaryFileMatchesCheckpoint`. Dans aucun scénario SQLite n’est en avance sur le fichier.

## Risques découverts

Aucun nouveau risque distinct. R-002/R-011 sont réduits pour deux checkpoints successifs sous mort
abrupte du processus. Ils restent ouverts pour crash avant flush, caches matériels, panne électrique,
reboot Windows, disque plein et écriture partielle réelle.

## État final de la tâche

PARTIEL

## Travail restant

- Tuer le processus avant l’écriture/flush du second bloc et vérifier fichier/SQLite à 65 536.
- Injecter une erreur contrôlée du writer pendant le second bloc.
- Tester ensuite disque plein, corruption/rollback SQLite, panne électrique et reboot Windows.

## Prochaine action

Ajouter une frontière subprocess avant la deuxième écriture/flush, puis vérifier que le fichier et
le checkpoint restaurés restent exactement à 65 536 octets.

## Commit associé

Aucun commit créé. Le dépôt `main` reste sans commit initial.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Preuve crash sur deux blocs et limites précisées |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | M-007, Q-001, F-010/F-015 et prochaine action actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée sans effacer l’historique ; position signalée en fin de journal |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Sélection de la seconde opération et états décrits |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-002/R-011 réduits sans clôture |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Section multi-blocs et preuves ajoutées |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, tests, risques et suite actualisés |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Extension d’application ADR-003 consignée |
| REGLES_DE_CODAGE.md | MIS À JOUR | Règle de ciblage multi-blocs ajoutée |
| DEPENDANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun paquet, version, verrou ou licence modifié |
| MODELISATION_DONNEES.md | MIS À JOUR | États 65 536/70 000 et 70 000/70 000 consignés |
| SECURITE.md | MIS À JOUR | Six frontières autorisées documentées |
| PERFORMANCES.md | MIS À JOUR | Durées fonctionnelles non assimilées à un benchmark |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-010 étendue à la preuve multi-blocs |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée et limites de la nouvelle preuve expliquées |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Règles permanentes appliquées sans changement |

---

## Rectification d’ordre du journal — 2026-08-04 15:45 UTC

L’entrée `2026-08-04 — 15:39 — M-007/M-008/F-013/F-016 — Coordinateur diagnostique de
récupération` a été insérée avant les entrées de 13:15 et 14:40 au lieu d’être physiquement ajoutée
en fin de fichier. Son horodatage 15:39 reste l’ordre chronologique autoritaire. Afin de respecter la
règle « ajouter sans effacer », l’entrée n’a été ni supprimée ni dupliquée ; cette rectification
signale explicitement sa position. Aucun contenu historique antérieur n’a été modifié.

---

# 2026-08-04 — 15:39 — M-007/M-008/F-013/F-016 — Coordinateur diagnostique de récupération

## Objectif

Créer un coordinateur en lecture seule exécutant inspection locale → analyse distante → décision →
recouvrement, avec court-circuit avant réseau lorsque le diagnostic local suffit à bloquer.

## État avant intervention

Les quatre composants existaient séparément et 93 tests réussissaient. Aucun cas d’usage unique
n’imposait leur ordre, ne matérialisait les étapes court-circuitées ni ne prouvait l’absence de réseau
sur un blocage local.

## Travail effectué

- Ajout de `StartupRecoveryCoordinator`, composé uniquement des services Application existants.
- Exposition de `EvaluateLocalBlockers` comme source pure unique pour le court-circuit et la décision.
- Ajout d’un résultat final typé conservant les seules preuves réellement calculées.
- Nom `ReconciliationBlockers` retenu pour ne pas masquer un blocage final de recouvrement.
- Propagation explicite de l’annulation entre inspection, analyse distante et recouvrement.
- Arrêt avant réseau pour tout blocage local et avant recouvrement pour toute décision distante bloquée.
- Adaptation du test d’intégration loopback pour déclencher toute la chaîne par un seul appel.
- Mise à jour de la documentation permanente concernée ; aucune dépendance ni migration ajoutée.

## Fichiers créés

- `src/WindowsDownloadManager.Application/Downloads/StartupRecoveryCoordinator.cs`
- `src/WindowsDownloadManager.Application/Downloads/StartupRecoveryAssessmentResult.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/StartupRecoveryCoordinatorTests.cs`

## Fichiers modifiés

- `src/WindowsDownloadManager.Application/Downloads/RecoveryDecisionEvaluator.cs`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DownloadOrchestratorIntegrationTests.cs`
- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `DEPENDANCES.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `ERREURS_CONNNUES.md`
- `FAQ_TECHNIQUE.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Aucun nouvel ADR. Le coordinateur applique ADR-004 et conserve les responsabilités existantes. Le
court-circuit réutilise l’évaluateur au lieu de dupliquer une matrice. Un statut `OverlapMatched`
reste explicitement diagnostique et périssable ; aucune API de reprise ou de mutation n’est exposée.

## Problèmes rencontrés

- Une première baseline n’a démarré aucun test car `APPDATA` n’était pas confiné et NuGet tentait de
  lire une configuration utilisateur inaccessible.
- La première compilation ciblée a échoué sur la casse d’un argument nommé dans une fixture.
- Une relance avec `--filter-class` a sélectionné zéro test et retourné le code 5.

## Solutions appliquées

- Relance avec `DOTNET_CLI_HOME` et `APPDATA` confinés au workspace.
- Correction limitée à la fixture (`SupportsByteRanges`).
- Exécution complète du projet Application, puis de l’intégration et de la solution sans filtre.

## Tests exécutés

- Baseline solution Release isolée : 93 exécutés, 93 réussis, 0 échec, 0 ignoré, 16,517 s — RÉUSSI.
- Baseline sans `APPDATA` confiné : 0 exécuté, résolution SDK échouée — ÉCHEC D’ENVIRONNEMENT.
- Première compilation ciblée : 0 exécuté, 1 erreur de compilation de fixture — ÉCHEC.
- Filtre ciblé incompatible : 0 exécuté, code 5 — ÉCHEC DE SÉLECTION.
- Application complet après correction initiale : 45 exécutés, 45 réussis, 0 échec, 0 ignoré,
  7,221 s — RÉUSSI.
- Intégration loopback : 4 exécutés, 4 réussis, 0 échec, 0 ignoré, 16,137 s — RÉUSSI.
- Application après ajout du cas d’annulation : 46 exécutés, 46 réussis, 0 échec, 0 ignoré,
  9,710 s — RÉUSSI.
- Non-régression solution : 101 exécutés, 101 réussis, 0 échec, 0 ignoré, 24,989 s — RÉUSSI.
- Commande canonique `eng/verify.ps1` post-documentation : restauration hors ligne RÉUSSIE ; build
  Release 0 avertissement/0 erreur ; 101 exécutés, 101 réussis, 0 échec, 0 ignoré, 13,255 s ; formatage
  RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences et 35 tâches.
- Crash réel, disque plein, reprise HTTP, troncature, redémarrage Windows, proxy/NAT64 et tests de
  performance : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Les blocages locaux et l’annulation arrêtent avant toute analyse distante. Une contradiction distante
arrête avant recouvrement. Le chemin favorable exécute une sonde puis une plage bornée et retourne
`OverlapMatched` sans modifier le fichier, l’état ou le checkpoint. Divergence, changement local et
position zéro possèdent chacun un statut final explicite.

## Risques découverts

Aucun nouveau risque distinct. R-001/R-002/R-003/R-011 sont réduits par l’ordre unique et les
courts-circuits. Course diagnostic/action, crash réel, revalidation sous verrou, réparation, hash et
reprise réseau restent ouverts.

## État final de la tâche

PARTIEL

## Travail restant

- Injecter des fautes aux frontières flush disque/checkpoint SQLite et restaurer après chaque arrêt.
- Concevoir ensuite la revalidation sous verrou et les actions réparatrices explicites.
- Implémenter et tester la reprise HTTP d’une tâche existante, le crash réel et le redémarrage.

## Prochaine action

Créer un banc d’injection de fautes aux frontières `flush disque → checkpoint SQLite` de
l’orchestrateur et prouver qu’après restauration aucun octet non durable n’est annoncé.

## Commit associé

Aucun commit créé. Le dépôt `main` reste sans commit initial.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | État partiel UC-002 précisé |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | M-007/F-013/F-016 et prochaine action actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée sans effacer l’historique |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Coordinateur, ordre et résultats documentés |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-001/R-002/R-003/R-011 révisés sans clôture |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Échecs et preuves jusqu’à 101/101 consignés |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, limites, tests et prochaine action actualisés |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Mise en œuvre ADR-004 précisée sans nouvel ADR |
| REGLES_DE_CODAGE.md | MIS À JOUR | Ordre et court-circuit rendus normatifs |
| DEPENDANCES.md | MIS À JOUR | Absence de nouvelle dépendance consignée |
| MODELISATION_DONNEES.md | MIS À JOUR | Résultat éphémère et absence de migration documentés |
| SECURITE.md | MIS À JOUR | Courts-circuits et annulation précisés |
| PERFORMANCES.md | MIS À JOUR | Coût logique non mesuré et absence de revendication consignés |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-009 actualisée |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée du coordinateur expliquée |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent appliqué sans changement |

---

# 2026-08-04 — 13:15 — M-007/F-016 — Décision combinée de récupération en lecture seule

## Objectif

Combiner `TemporaryFileReconciliationResult` et `RemoteIdentityReconciliationResult` dans une
décision unique, typée et strictement en lecture seule, sans perdre un second motif de blocage.

## État avant intervention

Les réconciliations locale et distante existaient séparément et étaient couvertes par 64 tests.
Aucun composant ne décidait encore si leurs résultats permettaient de passer au futur contrôle de
recouvrement. La prochaine action officielle de M-007/F-016 demandait cette composition.

## Travail effectué

- Création d’un résultat immuable avec statut `Blocked` ou `ReadyForOverlapVerification`.
- Création de sept motifs de blocage cumulables couvrant absence, incohérence locale, contradiction,
  preuve distante insuffisante et perte de Range.
- Création d’un évaluateur Application pur, synchrone, sans port, I/O ou mutation.
- Refus explicite de combiner deux résultats appartenant à des téléchargements différents.
- Passage favorable limité à `TemporaryFileMatchesCheckpoint` + `Compatible`.
- Ajout de onze tests unitaires couvrant la matrice et l’agrégation de plusieurs motifs.

## Fichiers créés

- `src/WindowsDownloadManager.Application/Downloads/RecoveryDecisionResult.cs`
- `src/WindowsDownloadManager.Application/Downloads/RecoveryDecisionEvaluator.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/RecoveryDecisionEvaluatorTests.cs`

## Fichiers modifiés

- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `DEPENDANCES.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `ERREURS_CONNNUES.md`
- `FAQ_TECHNIQUE.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Aucun nouvel ADR : ADR-003/004 imposent déjà la borne basse et l’arrêt conservateur. Une décision
simple fondée sur une priorité aurait masqué des problèmes simultanés ; le résultat conserve donc
une décision unique et une liste de motifs sous forme de drapeaux. Un fichier plus court ou plus long
que le checkpoint reste bloqué, car sa réparation demanderait une mutation non encore conçue.

## Problèmes rencontrés

Aucun défaut de production ou test échoué. La non-régression complète a été plus lente que la
baseline, sans mesure permettant de conclure à une régression du produit ; le runner ne constitue
pas un benchmark. La commande canonique a dépassé sa limite de 300 secondes pendant le formatage,
après restauration, build et tests réussis.

## Solutions appliquées

La logique a été limitée à une fonction pure et testée au niveau unitaire. Les deux diagnostics
originaux et la position sûre restent attachés au résultat afin de préserver les preuves pour la
prochaine étape sans relire ou modifier les ressources. Le formatage et le contrôle documentaire
ont été relancés séparément avec des limites adaptées.

## Tests exécutés

- Baseline : `dotnet test WindowsDownloadManager.slnx -c Release --no-restore` — 64 exécutés,
  64 réussis, 0 échec, 0 ignoré, 26,600 s : RÉUSSI.
- Application ciblée : `dotnet test tests-dotnet/WindowsDownloadManager.Application.Tests/
  WindowsDownloadManager.Application.Tests.csproj -c Release --no-restore` — 31 exécutés,
  31 réussis, 0 échec, 0 ignoré, 5,073 s : RÉUSSI.
- Non-régression : `dotnet test WindowsDownloadManager.slnx -c Release --no-restore` — 75 exécutés,
  75 réussis, 0 échec, 0 ignoré, 62,925 s : RÉUSSI.
- Commande canonique `eng/verify.ps1` post-documentation : restauration RÉUSSIE ; build Release
  0 avertissement/0 erreur ; 75 exécutés, 75 réussis, 0 échec, 0 ignoré, 20,019 s ; puis DÉLAI
  DÉPASSÉ à 300 s pendant le formatage. Les étapes non atteintes n’ont pas été déduites.
- Formatage isolé : `dotnet format WindowsDownloadManager.slnx --verify-no-changes --no-restore
  --verbosity minimal` — RÉUSSI en 148,7 s, aucun changement requis.
- Contrôle documentaire isolé : `eng/verify-documentation.ps1` — RÉUSSI en 38,9 s ; 16/16 documents,
  36/36 exigences, 35 tâches, comptes, identifiants et références cohérents.
- Recouvrement binaire, mutation, reprise HTTP, crash réel et redémarrage Windows : NON EXÉCUTÉS.
  Résultat inconnu.
- Tests de performance : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Les sept motifs bloquants sont déterministes et cumulables. Un temporaire au checkpoint avec un
distant compatible retourne `ReadyForOverlapVerification` et aucun motif. Tous les autres statuts
connus retournent `Blocked`. Deux blocages simultanés restent tous deux visibles et des IDs différents
provoquent une exception avant toute décision.

## Risques découverts

Aucun nouveau risque distinct. R-001/R-002/R-011 sont réduits pour la décision déterministe mais
restent ouverts tant que recouvrement, course diagnostic/action, réparation, crash et reprise réseau
ne sont pas testés.

## État final de la tâche

PARTIEL

## Travail restant

- Lire et comparer une fenêtre de recouvrement locale/distante sans mutation.
- Bloquer une divergence et conserver une preuve exploitable.
- Concevoir ensuite seulement la réparation/troncature et la reprise réseau.
- Exécuter PR-032, PR-052/061, crash réel et redémarrage Windows.

## Prochaine action

Créer un vérificateur de recouvrement binaire en lecture seule, accessible uniquement depuis
`ReadyForOverlapVerification`, sans modifier le temporaire ni confirmer de nouvel octet.

## Commit associé

Aucun commit créé. Le dépôt `main` reste sans commit initial et l’identité Git n’est pas configurée
dans l’état observé.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Décision combinée et limites réelles précisées |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | M-007/F-013/F-016 et prochaine action actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée sans effacer l’historique |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Évaluateur, matrice et frontière pure documentés |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-001/R-002/R-011 révisés sans clôture |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Baseline et preuves 75/75 consignées |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, limites, tests et prochaine action actualisés |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Mise en œuvre ADR-004 précisée sans nouvel ADR |
| REGLES_DE_CODAGE.md | MIS À JOUR | Règles de composition pure et de blocage ajoutées |
| DEPENDANCES.md | MIS À JOUR | 75 tests et absence de nouvelle dépendance consignés |
| MODELISATION_DONNEES.md | MIS À JOUR | Décision éphémère sans migration documentée |
| SECURITE.md | MIS À JOUR | Agrégation conservatrice et absence de mutation précisées |
| PERFORMANCES.md | MIS À JOUR | Coût non mesuré et absence de revendication consignés |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-009 actualisée |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée de la décision combinée expliquée |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent appliqué sans changement |

---

# 2026-08-04 — 14:40 — M-007/M-008/F-013/F-016 — Recouvrement binaire borné en lecture seule

## Objectif

Comparer une fenêtre locale et distante bornée uniquement après
`ReadyForOverlapVerification`, sans modifier le temporaire, l’agrégat ou SQLite.

## État avant intervention

Les diagnostics local/distant et leur décision combinée existaient avec 75 tests réussis. Le cas
favorable n’effectuait encore aucune comparaison d’octets et ne pouvait donc pas détecter une
divergence de contenu malgré des métadonnées compatibles.

## Travail effectué

- Ajout de ports Application pour lire une plage locale et distante exacte.
- Ajout d’un lecteur Storage qui verrouille contre les nouvelles écritures pendant la capture.
- Extension de `HttpRemoteContentSource` avec une requête Range fermée et strictement validée.
- Ajout de `RecoveryOverlapVerifier` et d’un résultat typé sans contenu binaire exposé.
- Fenêtre terminale bornée à 64 Kio ; position zéro traitée sans I/O.
- Refus des décisions bloquées avant lecture, distinction `Match`, `Mismatch` et `LocalFileChanged`.
- Tests unitaires Application/Storage/Network et intégration loopback réelle sans mutation.

## Fichiers créés

- `src/WindowsDownloadManager.Application/Abstractions/ITemporaryFileRangeReader.cs`
- `src/WindowsDownloadManager.Application/Abstractions/IRemoteRangeReader.cs`
- `src/WindowsDownloadManager.Application/Downloads/OverlapVerificationResult.cs`
- `src/WindowsDownloadManager.Application/Downloads/RecoveryOverlapVerifier.cs`
- `src/WindowsDownloadManager.Storage/Files/ReadOnlyTemporaryFileRangeReader.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/RecoveryOverlapVerifierTests.cs`
- `tests-dotnet/WindowsDownloadManager.Storage.Tests/ReadOnlyTemporaryFileRangeReaderTests.cs`

## Fichiers modifiés

- `src/WindowsDownloadManager.Network/Http/HttpRemoteContentSource.cs`
- `tests-dotnet/WindowsDownloadManager.Network.Tests/HttpRemoteContentSourceTests.cs`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DownloadOrchestratorIntegrationTests.cs`
- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `DEPENDANCES.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `ERREURS_CONNNUES.md`
- `FAQ_TECHNIQUE.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Aucun nouvel ADR : ADR-004 exige déjà le recouvrement avant reprise. Une plage HTTP fermée a été
retenue plutôt qu’un flux ouvert interrompu après 64 Kio afin de borner aussi la demande serveur.
La fenêtre se termine à la position sûre et vaut `min(64 Kio, SafePosition)`. Le résultat ne contient
pas les octets comparés. `Match` reste un diagnostic périssable, pas une autorisation de reprise.

## Problèmes rencontrés

La première suite a produit 90 réussites et 1 échec sur 91. Le serveur court était correctement
refusé, mais `HttpClient` levait `HttpIOException(ResponseEnded)` alors que le contrat du test exigeait
`EndOfStreamException`. Aucun contenu incomplet n’a été accepté.

## Solutions appliquées

Seul `HttpRequestError.ResponseEnded` est désormais normalisé en `EndOfStreamException`; les autres
erreurs HTTP/I/O continuent de remonter avec leur type d’origine. Deux tests de redirection ont ensuite
été ajoutés pour prouver la revalidation de chaque URI et l’absence de contact d’une cible refusée.

## Tests exécutés

- Baseline : `dotnet test WindowsDownloadManager.slnx -c Release --no-restore` — 75 exécutés,
  75 réussis, 0 échec, 0 ignoré, 5,560 s : RÉUSSI.
- Première suite : même commande — 91 exécutés, 90 réussis, 1 échec, 0 ignoré, 8,913 s : ÉCHEC.
- Suite après correction : même commande — 91 exécutés, 91 réussis, 0 échec, 0 ignoré, 4,844 s : RÉUSSI.
- Network ciblé : projet Network.Tests Release — 24 exécutés, 24 réussis, 0 échec, 0 ignoré,
  3,399 s : RÉUSSI.
- Non-régression finale avant documentation : 93 exécutés, 93 réussis, 0 échec, 0 ignoré,
  3,647 s : RÉUSSI.
- Commande canonique `eng/verify.ps1` post-documentation : restauration hors ligne RÉUSSIE ; build
  Release 0 avertissement/0 erreur ; 93 exécutés, 93 réussis, 0 échec, 0 ignoré, 3,694 s ; formatage
  RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences, 35 tâches,
  identifiants et références cohérents.
- Coordination complète, course après fermeture, proxy/NAT64, mutation, reprise HTTP, crash réel et
  redémarrage Windows : NON EXÉCUTÉS. Résultat inconnu.
- Tests de performance : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Une décision bloquée ou une position zéro ne provoque aucune lecture. Une décision éligible compare
exactement la fenêtre terminale, détecte la première divergence et observe un changement de longueur
locale avant toute requête distante. L’intégration effectue une sonde `bytes=0-0` puis une plage
`bytes=0-4`, compare `hello` et conserve fichier, état et checkpoint inchangés.

## Risques découverts

Aucun nouveau risque distinct. R-001/R-003/R-004 sont réduits par le recouvrement et la validation
stricte. La course après fermeture des handles, proxy/NAT64, reparse points, hash final et reprise
réelle restent ouverts ; `Match` ne doit pas être réutilisé sans revalidation.

## État final de la tâche

PARTIEL

## Travail restant

- Coordonner toute la chaîne diagnostique avec court-circuit local avant réseau.
- Produire un plan final typé sans confondre `Match` avec une autorisation durable.
- Concevoir ensuite la revalidation sous verrou, la réparation/troncature et la reprise.
- Exécuter PR-032, PR-052/061, crash réel et redémarrage Windows.

## Prochaine action

Créer un coordinateur de récupération en lecture seule qui exécute inspection locale, réanalyse
distante, décision et recouvrement dans l’ordre, puis retourne un plan final typé sans mutation.

## Commit associé

Aucun commit créé. Le dépôt `main` reste sans commit initial et l’identité Git n’est pas configurée
dans l’état observé.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Recouvrement borné et limites réelles précisés |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | M-007/M-008/F-013/F-016 et prochaine action actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée sans effacer l’historique |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Ports, adaptateurs, fenêtre et résultats documentés |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-001/R-003/R-004 révisés sans clôture |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Échec 90/91 et preuves 93/93 consignés |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, limites, tests et prochaine action actualisés |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Mise en œuvre ADR-004 étendue sans nouvel ADR |
| REGLES_DE_CODAGE.md | MIS À JOUR | Règles de plage bornée et revalidation ajoutées |
| DEPENDANCES.md | MIS À JOUR | 93 tests et absence de nouvelle dépendance consignés |
| MODELISATION_DONNEES.md | MIS À JOUR | Résultat éphémère sans migration documenté |
| SECURITE.md | MIS À JOUR | Verrou lecture, Range strict et course résiduelle précisés |
| PERFORMANCES.md | MIS À JOUR | Buffers bornés et absence de mesure consignés |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-009 actualisée |
| FAQ_TECHNIQUE.md | MIS À JOUR | Recouvrement et portée diagnostique expliqués |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent appliqué sans changement |

---

## Rectification d’ordre du journal — 2026-08-04 15:46 UTC

L’entrée `2026-08-04 — 15:39 — M-007/M-008/F-013/F-016 — Coordinateur diagnostique de
récupération` se trouve avant les entrées de 13:15 et 14:40 à cause d’une insertion documentaire
mal positionnée. Son horodatage 15:39 reste l’ordre chronologique autoritaire. Pour respecter le mode
« ajouter sans effacer », elle n’a été ni supprimée ni dupliquée. Aucun contenu historique antérieur
n’a été modifié.

---

# 2026-08-04 — 16:02 — M-007/Q-001/F-015 — Banc déterministe flush disque et checkpoint SQLite

## Objectif

Injecter des fautes aux frontières entre le flush durable du temporaire et le checkpoint SQLite,
rouvrir le dépôt, puis vérifier qu’aucun checkpoint restauré n’annonce des octets absents du disque.

## État avant intervention

L’ordre normal écriture → flush → confirmation domaine → sauvegarde SQLite était testé, avec
101/101 tests réussis. Aucune matrice d’intégration ne fermait et ne rouvrait le vrai dépôt après une
faute injectée précisément après flush, avant commit ou après commit.

## Travail effectué

- Ajout d’un banc dans `Integration.Tests`, sans crochet de panne dans le produit.
- Utilisation du vrai `DurableTemporaryFileWriter` et du vrai `SqliteDownloadRepository`.
- Décorateur writer injectant une faute après le retour de `Flush(true)`.
- Décorateur dépôt injectant une faute avant ou après le premier commit positif en téléchargement.
- Fermeture puis réouverture de SQLite et réconciliation avec le fichier temporaire réel.
- Vérification du contenu `hello`, de la longueur, de l’état et du checkpoint restaurés.
- Passage de Q-001 de À FAIRE à PARTIEL ; PR-032 reste PARTIEL et non assimilé à un crash réel.

## Fichiers créés

- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DurabilityFaultInjectionIntegrationTests.cs`

## Fichiers modifiés

- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `DEPENDANCES.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `ERREURS_CONNNUES.md`
- `FAQ_TECHNIQUE.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Aucun nouvel ADR. Les ports existants fournissent déjà les coutures nécessaires ; les décorateurs de
faute restent privés au projet de test. Une exception après commit signifie que l’appelant ignore le
succès, mais la réouverture de SQLite doit retrouver le checkpoint. Une exception avant commit doit
laisser la base derrière le fichier. Ces scénarios ne sont pas qualifiés de crash réel.

## Problèmes rencontrés

La première compilation a échoué avec six erreurs : la classe `TemporaryDirectory` partagée par le
projet d’intégration expose seulement `Path`, pas `DatabasePath`. Aucun test n’a été exécuté lors de
cette tentative et aucun code de production n’était en cause.

## Solutions appliquées

Le chemin `downloads.sqlite3` est désormais calculé explicitement dans chaque scénario. La fixture
partagée n’a pas été modifiée et aucun nouveau type redondant n’a été créé.

## Tests exécutés

- Baseline issue de la preuve canonique précédente : 101 exécutés, 101 réussis, 0 échec, 0 ignoré.
- Première compilation Integration.Tests : 0 exécuté, 6 erreurs de compilation de fixture — ÉCHEC.
- Integration.Tests après correction : 7 exécutés, 7 réussis, 0 échec, 0 ignoré, 7,184 s — RÉUSSI.
- Non-régression solution Release : 104 exécutés, 104 réussis, 0 échec, 0 ignoré, 18,718 s — RÉUSSI.
- Commande canonique `eng/verify.ps1` post-documentation : restauration hors ligne RÉUSSIE ; build
  Release 0 avertissement/0 erreur ; 104 exécutés, 104 réussis, 0 échec, 0 ignoré, 12,483 s ; formatage
  RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences et 35 tâches.
- Terminaison brutale subprocess, panne électrique, disque plein, écriture partielle et redémarrage
  Windows : NON EXÉCUTÉS. Résultat inconnu.
- Tests de performance : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Après une faute post-flush ou pré-commit, le temporaire contient exactement 5 octets mais SQLite
restaure 0 ; la réconciliation classe `TemporaryFileLonger` avec position sûre 0. Après une faute
post-commit, SQLite et le fichier restaurent exactement 5 octets et la classification est
`TemporaryFileMatchesCheckpoint`. Dans les trois scénarios, la base n’est jamais en avance.

## Risques découverts

Aucun nouveau risque distinct. R-002/R-011 sont réduits pour les exceptions déterministes avec vrais
adaptateurs. LIM-010 formalise que mort du processus, caches matériels et panne électrique restent
non prouvés.

## État final de la tâche

PARTIEL

## Travail restant

- Exécuter les mêmes frontières dans un processus enfant réellement terminé.
- Restaurer base et temporaire depuis un second processus.
- Étendre ensuite aux écritures multi-blocs, disque plein et corruption/rollback SQLite.

## Prochaine action

Créer un hôte de test subprocess, le terminer brutalement après flush, avant commit et après commit,
puis prouver la restauration externe des mêmes invariants.

## Commit associé

Aucun commit créé. Le dépôt `main` reste sans commit initial.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | État partiel du banc et limite crash réel précisés |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | Q-001, F-015, comptes et prochaine action actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée à la fin sans réordonner l’historique |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Décorateurs de test et trois frontières documentés |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-002/R-011 réduits sans clôture |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Échec initial et preuves 104/104 consignés |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, compte de statuts, limites et suite actualisés |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Mise en œuvre ADR-003 précisée sans nouvel ADR |
| REGLES_DE_CODAGE.md | MIS À JOUR | Injection limitée aux décorateurs de test |
| DEPENDANCES.md | MIS À JOUR | Absence de nouvelle dépendance consignée |
| MODELISATION_DONNEES.md | MIS À JOUR | États restaurés 0/5 et 5/5 documentés |
| SECURITE.md | MIS À JOUR | Isolation du banc et limites précisées |
| PERFORMANCES.md | MIS À JOUR | Tests fonctionnels non assimilés à un benchmark |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-010 ajoutée |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée du banc expliquée |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent appliqué sans changement |

---

# 2026-08-04 — 16:35 — M-007/Q-001/F-015 — Crash subprocess aux frontières de durabilité

## Objectif

Terminer brutalement un processus enfant après flush durable, avant commit SQLite et après commit,
puis restaurer le fichier et la base depuis le processus parent.

## État avant intervention

Trois exceptions déterministes avec vrais adaptateurs prouvaient les états 0/5, 0/5 et 5/5 avec
104/104 tests réussis. Elles restaient dans le même processus et déroulaient les `finally`, donc ne
prouvaient pas une mort abrupte du moteur.

## Travail effectué

- Création de `WindowsDownloadManager.CrashTestHost`, exécutable réservé aux tests.
- Ajout du projet à la solution et comme dépendance de build non référencée de l’intégration.
- Composition du vrai writer durable, du vrai dépôt SQLite et d’un flux mémoire mono-bloc.
- Auto-terminaison par `Process.Kill(false)` après flush, avant commit ou après commit.
- Lancement sans shell ni fenêtre, délai parent de 30 secondes et arrêt forcé en cas de dépassement.
- Restauration dans le parent et vérification du code non nul, du contenu, de l’état, du checkpoint
  et de la classification locale.
- Génération d’un verrou NuGet pour le nouveau projet sans nouvelle bibliothèque.

## Fichiers créés

- `tests-dotnet/WindowsDownloadManager.CrashTestHost/WindowsDownloadManager.CrashTestHost.csproj`
- `tests-dotnet/WindowsDownloadManager.CrashTestHost/Program.cs`
- `tests-dotnet/WindowsDownloadManager.CrashTestHost/packages.lock.json`

## Fichiers modifiés

- `WindowsDownloadManager.slnx`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/WindowsDownloadManager.Integration.Tests.csproj`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DurabilityFaultInjectionIntegrationTests.cs`
- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `DEPENDANCES.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `ERREURS_CONNNUES.md`
- `FAQ_TECHNIQUE.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Aucun nouvel ADR. Le crash reste strictement dans un exécutable de support non référencé par le
produit. `Process.Kill` a été retenu pour empêcher le déroulement normal des `finally`. Le parent ne
considère pas le seul code non nul comme preuve : il rouvre et inspecte systématiquement les artefacts.

## Problèmes rencontrés

Aucun échec de compilation ou de test. Le projet exécutable exigeait son propre verrou de restauration,
généré à partir des dépendances déjà approuvées et présentes dans le cache.

## Solutions appliquées

Le host est une dépendance de build avec `ReferenceOutputAssembly=false`. Le test retrouve la racine
par `WindowsDownloadManager.slnx`, déduit la configuration active et lance le SDK local du workspace.
Les arguments sont validés et les chemins normalisés avant toute I/O.

## Tests exécutés

- Baseline canonique : 104 exécutés, 104 réussis, 0 échec, 0 ignoré.
- Restauration du CrashTestHost avec verrou régénéré : RÉUSSIE, aucune nouvelle dépendance.
- Integration.Tests : 10 exécutés, 10 réussis, 0 échec, 0 ignoré, 28,467 s — RÉUSSI.
- Non-régression solution Release : 107 exécutés, 107 réussis, 0 échec, 0 ignoré, 28,694 s — RÉUSSI.
- Commande canonique `eng/verify.ps1` post-documentation : restauration hors ligne RÉUSSIE ; build
  Release RÉUSSI avec 0 avertissement/0 erreur ; 107 exécutés, 107 réussis, 0 échec, 0 ignoré,
  50,467 s ; formatage RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences
  et 35 tâches cohérentes.
- Crash avant flush, crash multi-blocs, panne électrique, reboot Windows, disque plein et écriture
  partielle : NON EXÉCUTÉS. Résultat inconnu.
- Tests de performance : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Les trois subprocess sortent avec un code non nul. Après flush ou avant commit, le parent retrouve
le fichier exact de 5 octets et SQLite à 0, classé `TemporaryFileLonger`. Après commit, il retrouve
5/5 et `TemporaryFileMatchesCheckpoint`. Aucune base en avance ni corruption SQLite n’est observée.

## Risques découverts

Aucun nouveau risque distinct. R-002/R-011 sont réduits par une terminaison réellement abrupte du
subprocess. Ils restent ouverts pour plusieurs checkpoints, caches matériels, panne électrique,
redémarrage Windows et écriture partielle réelle. LIM-010 est marquée résolue pour son périmètre.

## État final de la tâche

PARTIEL

## Travail restant

- Répéter les trois terminaisons sur le second bloc d’un transfert de 70 000 octets.
- Vérifier les checkpoints restaurés 65 536 et 70 000 selon le commit atteint.
- Tester ensuite avant-flush, disque plein, corruption/rollback SQLite et reboot Windows.

## Prochaine action

Étendre l’hôte subprocess à deux blocs et tuer pendant le second bloc après flush, avant checkpoint
et après checkpoint.

## Commit associé

Aucun commit créé. Le dépôt `main` reste sans commit initial.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Crash subprocess mono-bloc et limites précisés |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | Q-001/F-015 et prochaine action actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée à la fin sans réordonner l’historique |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Projet host, lancement et restauration documentés |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-002/R-011 réduits sans clôture |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Preuves 107/107 et limites PR-032 consignées |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, tests, risques et suite actualisés |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Extension de mise en œuvre ADR-003 consignée |
| REGLES_DE_CODAGE.md | MIS À JOUR | Isolation et délai du host normés |
| DEPENDANCES.md | MIS À JOUR | Projet/verrou sans nouvelle bibliothèque documentés |
| MODELISATION_DONNEES.md | MIS À JOUR | Arguments éphémères et états restaurés précisés |
| SECURITE.md | MIS À JOUR | Validation, normalisation et isolement documentés |
| PERFORMANCES.md | MIS À JOUR | Temps subprocess non assimilé à un benchmark |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-010 marquée résolue pour son périmètre |
| FAQ_TECHNIQUE.md | MIS À JOUR | Preuve subprocess et limites expliquées |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent appliqué sans changement |

---

## Rectification d’ordre du journal — 2026-08-04 18:01 UTC

L’entrée `2026-08-04 — 18:01 — M-007/Q-001/F-015 — Crash subprocess pendant le second bloc` a été
insérée avant des entrées plus anciennes lors de l’ajout par contexte Markdown répété. Son horodatage
18:01 reste l’ordre chronologique autoritaire. Conformément à la règle « ajouter sans effacer », elle
n’a été ni supprimée ni dupliquée. Sa preuve canonique finale a été actualisée dans l’entrée :
restauration hors ligne réussie, build Release 0 avertissement/0 erreur, 110/110 tests réussis en
15,167 s, formatage réussi et contrôle documentaire 16/16 réussi.

---

# 2026-08-04 — 19:30 — M-007/Q-001/F-015 — Mort avant le second appel disque

## Objectif

Tuer le subprocess avant la deuxième écriture/flush d’un transfert de 70 000 octets et vérifier que
le fichier, SQLite, le contenu et la position sûre restent exactement au premier bloc de 65 536 octets.

## État avant intervention

Six frontières subprocess couvraient le mono-bloc et le deuxième bloc après flush, avant commit et
après commit. La baseline canonique était de 110/110 tests. La frontière précédant tout appel disque
du second bloc restait non exécutée.

## Travail effectué

- Ajout de la frontière `BeforeSecondBlockWriteAndFlush` au host de crash.
- Généralisation du décorateur writer pour tuer avant ou après l’opération ciblée.
- Conservation du comptage explicite afin que le premier flush et son checkpoint soient terminés.
- Ajout d’un test parent vérifiant le préfixe complet de 65 536 octets, pas seulement sa longueur.
- Vérification après réouverture de SQLite, de l’état `Downloading`, du checkpoint, de la
  classification `TemporaryFileMatchesCheckpoint` et de la position sûre.

## Fichiers créés

- Aucun.

## Fichiers modifiés

- `tests-dotnet/WindowsDownloadManager.CrashTestHost/Program.cs`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DurabilityFaultInjectionIntegrationTests.cs`
- `Cahier_des_charges.md`
- `FEUILLE_DE_ROUTE.md`
- `SUIVI_DEVELOPPEMENT.md`
- `ARCHITECTURE_TECHNIQUE.md`
- `REGISTRE_DES_RISQUES.md`
- `PROTOCOLE_TEST_REPRISE.md`
- `ETAT_ACTUEL_PROJET.md`
- `DECISIONS_ARCHITECTURE.md`
- `REGLES_DE_CODAGE.md`
- `MODELISATION_DONNEES.md`
- `SECURITE.md`
- `PERFORMANCES.md`
- `ERREURS_CONNNUES.md`
- `FAQ_TECHNIQUE.md`

## Fichiers supprimés

- Aucun.

## Décisions prises

Aucun nouvel ADR. La nouvelle frontière tue strictement avant de déléguer au vrai writer. Elle est
donc documentée comme pré-écriture, jamais comme écriture partielle. Les six scénarios précédents
restent inchangés et servent de non-régression.

## Problèmes rencontrés

Une mise à jour documentaire groupée a été refusée atomiquement car une ligne G2 de la feuille de
route ne correspondait plus au contexte attendu. Elle a été divisée en correctifs ciblés. Aucun échec
de compilation ou de test n’a été rencontré.

## Solutions appliquées

Le décorateur compte l’appel avant toute délégation, tue sur le deuxième appel lorsque la frontière
pré-écriture est sélectionnée, et conserve le comportement post-flush pour les frontières existantes.
Le parent utilise le préfixe source attendu comme oracle exact.

## Tests exécutés

- Baseline canonique : 110 exécutés, 110 réussis, 0 échec, 0 ignoré.
- Integration.Tests Release : 14 exécutés, 14 réussis, 0 échec, 0 ignoré, 17,648 s — RÉUSSI.
- Non-régression solution Release : 111 exécutés, 111 réussis, 0 échec, 0 ignoré, 14,668 s — RÉUSSI.
- Vérification canonique `eng/verify.ps1` post-documentation : restauration hors ligne RÉUSSIE ;
  build Release RÉUSSI avec 0 avertissement/0 erreur ; 111 exécutés, 111 réussis, 0 échec, 0 ignoré,
  16,881 s ; formatage RÉUSSI ; contrôle documentaire RÉUSSI avec 16/16 documents, 36/36 exigences
  et 35 tâches cohérentes.
- Erreur/crash pendant écriture, panne électrique, reboot Windows, disque plein, corruption SQLite
  et écriture partielle réelle : NON EXÉCUTÉS. Résultat inconnu.
- Tests de performance spécialisés : NON EXÉCUTÉS. Résultat inconnu.

## Résultats

Le subprocess sort avec un code non nul avant son deuxième appel au writer. Le parent restaure un
fichier exact de 65 536 octets, identique au préfixe source, et SQLite à 65 536. La réconciliation
retourne `TemporaryFileMatchesCheckpoint` avec une position sûre de 65 536. Aucun octet du second
bloc n’est présent et la base n’est pas en avance.

## Risques découverts

Aucun nouveau risque distinct. R-002/R-011 sont réduits pour la frontière pré-écriture. Ils restent
ouverts pour erreur ou mort pendant l’écriture, écriture partielle, caches matériels, disque plein,
panne électrique, corruption SQLite et reboot Windows.

## État final de la tâche

PARTIEL

## Travail restant

- Injecter une erreur contrôlée du writer pendant le second bloc avant retour de la frontière durable.
- Vérifier SQLite à 65 536 et classifier toute queue disque éventuelle comme non confirmée.
- Tester ensuite disque plein, corruption/rollback SQLite, panne électrique et reboot Windows.

## Prochaine action

Créer un writer de test qui écrit une partie du second bloc puis échoue avant le flush/retour durable,
et vérifier que SQLite reste à 65 536 sans considérer la queue disque comme confirmée.

## Commit associé

Aucun commit créé. Le dépôt `main` reste sans commit initial.

## Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Septième crash et limite écriture partielle précisés |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | M-007, Q-001, F-010/F-015 et prochaine action actualisés |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Entrée ajoutée en fin sans effacer l’historique |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Frontière pré-écriture documentée |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | R-002/R-011 réduits sans clôture |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Section 19 et preuves ajoutées |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacité, tests, risques et prochaine action actualisés |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | Extension d’application ADR-003 consignée |
| REGLES_DE_CODAGE.md | MIS À JOUR | Distinction pré-écriture/écriture partielle ajoutée |
| DEPENDANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun paquet, verrou, version ou licence modifié |
| MODELISATION_DONNEES.md | MIS À JOUR | État restauré 65 536/65 536 consigné |
| SECURITE.md | MIS À JOUR | Sept frontières autorisées documentées |
| PERFORMANCES.md | MIS À JOUR | 111 tests fonctionnels non assimilés à un benchmark |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-010 et travail restant précisés |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée exacte de la frontière expliquée |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Règles permanentes appliquées sans changement |

---

## 2026-08-07 — Injection de faute du writer durant le second bloc (112 tests réussis)

- Statut : SUCCÈS
- Auteur : Antigravity (IA)

### Modifications apportées

1. Ajout du test d'intégration `RunNew_FaultAfterSecondBlockDurableFlush_RestoresFirstCheckpoint` dans `DurabilityFaultInjectionIntegrationTests.cs`.
2. Extension de `FaultAfterFlushWriter` pour prendre en charge un déclenchement ciblé au second bloc (`targetWriteCount: 2`).
3. Mises à jour de `StubAnalyzer` et `StubContentSource` pour accepter un contenu dynamique personnalisé dans les tests d'intégration in-process.

### Preuves et vérifications

- Execution de `powershell.exe -NoProfile -ExecutionPolicy Bypass -File eng/verify.ps1`.
- Compilation Release C# : 0 Erreur, 0 Avertissement.
- Tests .NET : **112 / 112 réussis** en 30,95 s.
- Formatage .NET : 0 modification requise.
- Contrôle documentaire : 16/16 documents présents et cohérents, 36/36 exigences normatives répertoriées, 35 tâches exécutables validées.

---

## 2026-08-10 — M-007/M-008/ADR-029 — Reprise réseau et finalisation même volume

### Objectif

Transformer la chaîne diagnostique de récupération en reprise sûre d’une tâche existante, puis
implémenter la première finalisation réparable conforme à ADR-029.

### État avant intervention

Le moteur savait télécharger une tâche neuve et diagnostiquer un checkpoint, une identité distante
et un recouvrement sans mutation. La reprise réseau, le move final et la réparation de `Finalizing`
étaient absents. La baseline canonique était de 112 tests.

### Travail effectué

- Ajout de `DownloadOrchestrator.ResumeAsync`, sérialisé dans l’instance.
- Revalidation complète avant mutation et blocage sans effet en cas de divergence.
- Reprise HTTP au checkpoint confirmé avec maintien de `flush → checkpoint SQLite`.
- Ajout de `DownloadFinalizationCoordinator` et du port `ITemporaryFileFinalizer`.
- Persistance `Finalizing`, move même volume sans écrasement, puis persistance `Completed`.
- Réparation prudente de `Finalizing` lorsque seul le temporaire ou seule la destination existe.
- Ajout de `AtomicTemporaryFileFinalizer` dans Storage.
- Mise à jour de la source de vérité et correction du README/plan d’action obsolètes.

### Fichiers créés

- `src/WindowsDownloadManager.Application/Abstractions/ITemporaryFileFinalizer.cs`
- `src/WindowsDownloadManager.Application/Downloads/DownloadResumeResult.cs`
- `src/WindowsDownloadManager.Application/Downloads/DownloadFinalizationCoordinator.cs`
- `src/WindowsDownloadManager.Storage/Files/AtomicTemporaryFileFinalizer.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/DownloadResumeTests.cs`
- `tests-dotnet/WindowsDownloadManager.Application.Tests/DownloadFinalizationCoordinatorTests.cs`
- `tests-dotnet/WindowsDownloadManager.Storage.Tests/AtomicTemporaryFileFinalizerTests.cs`

### Fichiers modifiés

- `src/WindowsDownloadManager.Application/Downloads/DownloadOrchestrator.cs`
- `tests-dotnet/WindowsDownloadManager.Integration.Tests/DownloadOrchestratorIntegrationTests.cs`
- documents permanents concernés par la reprise, la finalisation, les risques et les preuves.

### Décisions et invariants

Aucun nouvel ADR. ADR-003/004/025/029 sont appliquées partiellement. Un diagnostic ne constitue une
autorisation de mutation que s’il aboutit à un recouvrement identique ou inutile. Une finalisation
atomique signifie ici uniquement un move sur le même volume. Toute collision, ambiguïté disque ou
différence de volume provoque un arrêt sûr.

### Problèmes et solutions

La première application du correctif a expiré avant écriture dans le bac à sable ; elle a été
rejouée en correctifs plus petits. Un test utilisait initialement `TemporaryFileSnapshot.Missing`
au lieu de `Absent` ; la compilation ciblée l’a détecté et le nom public correct a été appliqué.

### Tests exécutés et résultats

- Application ciblé : 53 exécutés, 53 réussis, 0 échec, 0 ignoré.
- Storage ciblé : 17 exécutés, 17 réussis, 0 échec, 0 ignoré.
- Intégration ciblée : 16 exécutés, 16 réussis, 0 échec, 0 ignoré.
- Vérification canonique `eng/verify.ps1` : restauration hors ligne RÉUSSIE ; compilation Release
  0 avertissement/0 erreur ; 122 exécutés, 122 réussis, 0 échec, 0 ignoré en 21,736 s ; formatage
  RÉUSSI ; documentation 16/16, exigences 36/36 et 35 tâches cohérentes.
- Python : NON EXÉCUTÉ, runtime absent du PATH ; résultat courant inconnu.
- SHA-256 final, crash subprocess avant/après move, disque plein, antivirus, autre volume, panne
  électrique et reboot Windows : NON EXÉCUTÉS ; résultat inconnu.

### Risques et statut réel

R-001/R-002/R-011/R-021 sont réduits mais non clos. La reprise et la finalisation même volume sont
PARTIELLES tant que les frontières de crash, le hash final et l’exclusion inter-processus manquent.

### Prochaine action

Étendre le CrashTestHost aux frontières `Finalizing sauvegardé`, `move effectué` et `Completed
sauvegardé`, puis vérifier la réparation après réouverture de SQLite.

### Commit associé

Commit initial de la baseline G2 créé sur `main` avec l’identité locale `TUBI225
<EMILONEUFSIX@GMAIL.COM>` et publication vers `https://github.com/TUBI225/IDM.git`.

### Contrôle documentaire

| Document | État | Action |
|---|---|---|
| Cahier_des_charges.md | MIS À JOUR | Portée reprise/finalisation et limites |
| FEUILLE_DE_ROUTE.md | MIS À JOUR | Action obsolète remplacée, G2 actualisée |
| SUIVI_DEVELOPPEMENT.md | MIS À JOUR | Présente entrée ajoutée |
| ARCHITECTURE_TECHNIQUE.md | MIS À JOUR | Flux et réparations décrits |
| REGISTRE_DES_RISQUES.md | MIS À JOUR | Réductions et risques ouverts |
| PROTOCOLE_TEST_REPRISE.md | MIS À JOUR | Preuve 122/122 et limites |
| ETAT_ACTUEL_PROJET.md | MIS À JOUR | Capacités et prochaine action |
| DECISIONS_ARCHITECTURE.md | MIS À JOUR | ADR-029 partiellement appliquée |
| REGLES_DE_CODAGE.md | MIS À JOUR | Invariants reprise/finalisation |
| DEPENDANCES.md | VÉRIFIÉ — NON CONCERNÉ | Aucun paquet ou verrou modifié |
| MODELISATION_DONNEES.md | MIS À JOUR | Cycle d’états sans migration |
| SECURITE.md | MIS À JOUR | Chemins, collision, volumes et verrou |
| PERFORMANCES.md | MIS À JOUR | Tests non assimilés à un benchmark |
| ERREURS_CONNNUES.md | MIS À JOUR | LIM-011 créée |
| FAQ_TECHNIQUE.md | MIS À JOUR | Portée réelle expliquée |
| INSTRUCTIONS_IA.md | VÉRIFIÉ — NON CONCERNÉ | Processus permanent inchangé |
