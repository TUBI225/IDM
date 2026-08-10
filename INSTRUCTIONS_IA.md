# Instructions permanentes pour toute IA

Version documentaire : 1.1  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-03  
Statut : OBLIGATOIRE  
Responsable logique : Propriétaire du projet  
Documents liés : les 16 documents permanents

## Sommaire

1. Autorité documentaire
2. Ordre de lecture
3. Avant une modification
4. Pendant une modification
5. Après une modification
6. Matrice documentaire
7. Vérité, tests et Git
8. Suppressions et données
9. Modèle du suivi
10. Définition de terminé

## 1. Autorité documentaire

Les 16 fichiers Markdown imposés constituent la mémoire permanente. Ils ne doivent jamais être
supprimés, renommés, vidés ni remplacés par des résumés plus courts sans autorisation explicite.
La conversation aide à comprendre une demande mais ne remplace ni les documents ni le code réel.
En cas de contradiction, signaler le conflit et préserver l’historique.

### Hiérarchie G0 en cas de conflit

1. `INSTRUCTIONS_IA.md` gouverne la méthode de travail.
2. `Cahier_des_charges.md` gouverne le besoin cible.
3. Les ADR acceptées gouvernent les décisions techniques.
4. Les documents spécialisés gouvernent architecture, données, sécurité, risques et performances.
5. `FEUILLE_DE_ROUTE.md` gouverne tâches, dépendances, statuts et prochaine action.
6. `ETAT_ACTUEL_PROJET.md` gouverne la photographie factuelle courante.
7. `SUIVI_DEVELOPPEMENT.md` conserve l’historique, jamais le statut courant.
8. FAQ, README et rapports expliquent ; ils ne remplacent aucune source normative.

Une information plus récente ne gagne que dans son domaine autoritaire. Corriger le document courant
et ajouter un addendum au suivi ; ne jamais réécrire une ancienne preuve.

## 2. Ordre de lecture obligatoire

`INSTRUCTIONS_IA.md` est le document d’amorçage à lire en premier lorsqu’il est découvert. L’ordre
de travail normatif reste ensuite celui demandé par le propriétaire :

1. `Cahier_des_charges.md`
2. `ETAT_ACTUEL_PROJET.md`
3. `FEUILLE_DE_ROUTE.md`
4. dernières entrées de `SUIVI_DEVELOPPEMENT.md`
5. `ARCHITECTURE_TECHNIQUE.md`
6. `DECISIONS_ARCHITECTURE.md`
7. `REGLES_DE_CODAGE.md`
8. `DEPENDANCES.md`
9. `MODELISATION_DONNEES.md`
10. `SECURITE.md`
11. `REGISTRE_DES_RISQUES.md`
12. `PERFORMANCES.md`
13. `PROTOCOLE_TEST_REPRISE.md` pour toute opération interrompable
14. `ERREURS_CONNNUES.md`
15. `FAQ_TECHNIQUE.md` si une justification ou reprise de contexte est nécessaire

Le seizième document est le présent fichier d’amorçage. Les 16 doivent être contrôlés en fin de tâche.

Lire ensuite le code, les tests et l’état Git concernés. Ne jamais commencer sur une supposition.

## 3. Avant une modification

- Identifier ou créer une tâche avec objectif, dépendances, risques et critères.
- Rechercher une fonction similaire et ses utilisations ; éviter toute duplication.
- Vérifier les ADR, règles de code, dépendances, données et sécurité.
- Définir les tests nécessaires, y compris non-régression et reprise.
- Annoncer limites, hypothèses et risques. Une décision humaine manquante doit être demandée.
- Déclarer la pile concernée : `PYTHON-PROTOTYPE`, `CSHARP-CIBLE` ou `COMMUN`. Une preuve d’une pile
  ne change jamais le statut de l’autre.

## 4. Pendant une modification

- Limiter le changement à la tâche et respecter les couches.
- Ne supprimer aucun mécanisme sans recherche d’utilisations, migration, tests et retour arrière.
- Ne jamais avancer la progression avant confirmation disque.
- Ne jamais cacher une exception, un échec ou une limitation.
- Ne pas ajouter de dépendance par commodité ni contourner une protection.
- Ajouter les tests et messages utiles sans exposer les secrets.

## 5. Après une modification

1. Relire les différences et vérifier absence de duplication.
2. Exécuter formatage, analyse statique, compilation, tests ciblés et non-régression disponibles.
3. Exécuter ou marquer explicitement non exécutés les tests sécurité/performance concernés.
4. Mettre à jour obligatoirement `SUIVI_DEVELOPPEMENT.md` en ajout.
5. Actualiser `ETAT_ACTUEL_PROJET.md` et la feuille de route si une tâche change.
6. Vérifier les 16 documents et mettre à jour tous ceux concernés.
7. Indiquer problèmes ouverts, risques, prochaine action et commit réel.

Lorsque plusieurs IA travaillent en parallèle, une seule intervention désignée fusionne les mises à
jour de la feuille de route, de l’état actuel et du suivi. Chaque intervenant relit l’état du fichier
avant patch, ne réordonne jamais le suivi et signale tout conflit au lieu d’écraser le travail concurrent.

## 6. Matrice documentaire

| Changement | Documents minimaux |
|---|---|
| Toute modification | Suivi ; contrôle des 16 documents |
| Fonction/statut | Cahier si besoin, feuille de route, état, suivi |
| Module/architecture | Architecture, ADR, risques, suivi |
| Dépendance | Dépendances, sécurité, architecture, risques, suivi |
| Donnée/migration | Modélisation, architecture, sécurité, risques, suivi |
| Reprise/crash | Protocole, architecture, risques, suivi, état |
| Bug | Erreurs connues, feuille de route, risques si utile, suivi |
| Performance | Performances avec mesures, suivi |

États documentaires autorisés : `MIS À JOUR`, `VÉRIFIÉ — NON CONCERNÉ`, `À METTRE À JOUR`,
`BLOQUÉ`, `NON VÉRIFIÉ`. Aucun état final si un document reste à mettre à jour ou non vérifié.

## 7. Vérité, tests et Git

Ne jamais inventer résultat, test, commit, mesure, fichier, dépendance ou décision. Pour chaque test,
consigner commande, environnement, date, total, réussites, échecs, ignorés et erreur. Écrire
« Test non exécuté. Résultat inconnu » lorsque nécessaire. Avec Git, code et documentation liée
appartiennent au même commit ; sans dépôt, écrire « Aucun commit créé ».

À chaque G0/jalon, vérifier automatiquement si possible : présence/non-vacuité des 16 documents,
liens Markdown locaux, unicité des définitions d’ID, comptes de statuts, date de l’état actuel et
couverture `exigence→tâche→ADR→risque→test`. Un contrôle de présence seul ne vaut pas contrôle de
cohérence. Exécuter `eng/verify-documentation.ps1` lorsqu’il est disponible. Les résultats de script
sont des diagnostics et doivent être relus avant mise à jour.

## 8. Suppressions et données

Avant suppression/remplacement : rechercher les usages, dépendances et données existantes ; analyser
les conséquences ; prévoir migration, tests, sauvegarde et retour arrière ; documenter la décision.
Une apparente inutilité ne suffit jamais. Les fichiers permanents et l’historique du suivi sont
intangibles sans autorisation explicite.

## 9. Modèle obligatoire du suivi

Chaque entrée datée contient : tâche/titre, objectif, état avant, travail, fichiers créés/modifiés/
supprimés, décisions, problèmes, solutions, tests, résultats, risques, statut réel, reste, prochaine
action, commit et tableau des 16 documents. Le suivi fonctionne uniquement en ajout.

## 10. Définition de terminé

`TERMINÉ` exige besoin défini, implémentation conforme, tests nécessaires réussis, erreurs gérées,
données protégées, risques/sécurité/performance traités selon le périmètre, documentation cohérente,
aucune limitation critique cachée et prochaine action indiquée. Sinon utiliser `PARTIEL`,
`À VÉRIFIER` ou `BLOQUÉ`.

## 11. Commande de qualité canonique

Avant de conclure une modification C#, exécuter `eng/verify.ps1`. Utiliser `-RefreshPackages`
uniquement pour une mise à jour intentionnelle des verrous et `-AuditPackages` avec accès réseau.
Une restauration hors ligne emploie `--locked-mode -p:NuGetAudit=false` mais ne remplace jamais
l’audit connecté. Ne pas réactiver la télémétrie CLI ou du runner. Consigner séparément tout échec,
y compris une indisponibilité de source ou du service d’avis de vulnérabilité.

Une alerte `NU1901` à `NU1904` bloque la dépendance : ne jamais la contourner par désactivation de
l’audit pour produire une version. Chercher une version corrigée, régénérer les verrous, exécuter
l’audit connecté et documenter l’incident. SQLitePCLRaw 2.1.11 est explicitement interdit.
