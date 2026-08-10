# Rapport de création de la documentation

Date : 2026-08-03  
Périmètre : 16 documents permanents ; ce rapport est un livrable séparé.

> **Rapport historique.** Les nombres et décisions ci-dessous décrivent la création initiale. Ils ne
> constituent pas l’état courant. Consulter `ETAT_ACTUEL_PROJET.md` et `FEUILLE_DE_ROUTE.md`.

## Résultats

- Nombre de fichiers permanents créés ou présents et audités : 16.
- Nombre total de lignes : 1 943 dans les 16 documents permanents au contrôle final.
- Nombre approximatif de mots : 11 674 dans les 16 documents permanents au contrôle final.
- Documents les plus volumineux lors de la mesure intermédiaire : `SUIVI_DEVELOPPEMENT.md`,
  `Cahier_des_charges.md`, `FAQ_TECHNIQUE.md`, `PROTOCOLE_TEST_REPRISE.md`.
- Décisions initiales recensées : 20 ADR, dont plusieurs propositions soumises à revue.
- Risques recensés : 21 ; critiques notamment intégrité, disque/base, SSRF, secrets et migrations.
- Nombre d’identifiants de tâches : 43.
- Nombre d’identifiants de scénarios de reprise : 42.

## Décisions humaines nécessaires

1. Nom final et identité du produit.
2. C#/.NET comme cible et sort du prototype Python.
3. WPF ou WinUI 3, versions minimales de Windows.
4. Fournisseur SQLite .NET, framework de test et journalisation.
5. Format d’installation, signature et mise à jour.
6. Politique de télémétrie (proposition : aucune par défaut), secrets et rétention.
7. Seuils de performance et consentement pour retransmission coûteuse.

## Cohérence documentaire

Audit structurel réussi : 16/16 fichiers non vides avec métadonnées, sommaire et liens. Les notions
de prototype observé et cible proposée sont séparées. Les IDs de tâches, tests, ADR et risques ont
été comptés sans doublons au sein de leur catégorie. La cohérence métier finale reste `À VÉRIFIER`
jusqu’à revue humaine ; aucun résultat de test cible n’a été inventé.

## Prochaine étape recommandée

Effectuer D-001, revue humaine des exigences et ADR. Ne pas ajouter de code fonctionnel avant cette
validation. Après arbitrage, mettre à jour les documents concernés et seulement ensuite préparer un
plan d’implémentation du moteur cible.

## Révision après demande d’exhaustivité

Le propriétaire a demandé une documentation réellement complète. L’audit a conclu que le corpus
initial, bien que structuré, restait trop condensé. T-015 est donc `PARTIEL`. Un catalogue de
36 exigences normatives, cinq cas utilisateurs et une traçabilité de premier niveau ont été ajoutés,
ainsi qu’un dictionnaire détaillé des entités critiques. Restent : ADR-005 à ADR-020 en fiches
complètes, dictionnaire colonne par colonne de toutes les tables, tests UI/sécurité/install/performance
et matrice de traçabilité exhaustive. Aucun état `TERMINÉ` n’est revendiqué pour cette extension.

## Addendum G0 — 2026-08-03

L’audit humain a validé la stratégie de remise en cohérence. Les piles Python/C# sont désormais
séparées, les objectifs parents `T-*` sont exclus du comptage opérationnel et une matrice individuelle
des 36 exigences est ajoutée. Les décisions techniques encore ouvertes sont transférées à G1 ; ce
rapport reste un instantané historique non normatif.
