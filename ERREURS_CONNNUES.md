# Erreurs connnues

Version documentaire : 1.3  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : ACTIF  
Responsable logique : Qualité  
Documents liés : `ETAT_ACTUEL_PROJET.md`, `REGISTRE_DES_RISQUES.md`, `SUIVI_DEVELOPPEMENT.md`

> Le nom contient volontairement trois « n » afin de respecter le nom exact imposé par la mission.

## Sommaire

1. Règles du registre
2. Anomalies confirmées
3. Limitations techniques connues
4. Modèle d’anomalie
5. Cycle de vie

## 1. Règles du registre

Une anomalie n’est inscrite comme confirmée qu’après observation reproductible ou preuve technique.
Un risque hypothétique reste dans le registre des risques. Une limitation inhérente aux protocoles
n’est pas présentée comme un bug. Les anciennes anomalies ne sont pas effacées : elles passent à
`CORRIGÉE`, `DUPLIQUÉE`, `NON REPRODUCTIBLE`, `ACCEPTÉE` ou `ABANDONNÉE` avec justification.

## 2. Anomalies confirmées

Une anomalie active du dispositif de preuve est confirmée ci-dessous. Le prototype Python possède
trois tests observés et le socle C# treize scénarios observés, couverture insuffisante pour conclure
à l’absence de bugs. Deux erreurs initiales de harnais Python (`mkdir` sans `exist_ok`) ont été
corrigées et restent conservées dans `SUIVI_DEVELOPPEMENT.md`.

### BUG-001 — Le test de redirection ne prouve pas la revalidation sécurisée

- Gravité : MAJEURE pour la preuve, pas une corruption observée du moteur.
- Version/environnement : socle C# 0.1.0, Windows, harnais exécutable.
- Observation : le scénario nommé `redirect is followed and revalidated` construit l’analyseur avec
  `AllowAllUriSafetyValidator`.
- Résultat attendu : observer deux validations et refuser une redirection public→privé.
- Résultat obtenu : seul le suivi manuel et l’URL finale sont prouvés.
- Solution temporaire : conserver R-004 ouvert et ne pas exposer le transfert C# à des URL externes.
- Tâche/risque/test : M-003, R-004, scénario redirection T-017.
- Correction : remplacement du validateur permissif par un validateur observé ; ajout d’un test à
  deux sauts et d’un test prouvant que la cible refusée ne reçoit aucune connexion.
- Preuve : `dotnet test`, 14/14 tests réussis le 2026-08-03, dont les deux scénarios de redirection.
- Statut : CORRIGÉE le 2026-08-03. La limitation distincte LIM-008 reste ouverte jusqu’à ADR-026.

### BUG-002 — Dépendance SQLite native vulnérable lors de la première restauration

- Gravité : ÉLEVÉE, détectée avant utilisation ou distribution.
- Observation : l’audit NuGet a refusé `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 avec
  GHSA-2m69-gcr7-jv3q.
- Correction : épingle directe du bundle 2.1.12, régénération des verrous et nouvel audit transitif.
- Preuve : restauration réussie, build/tests réussis et audit final sans paquet vulnérable signalé.
- Statut : CORRIGÉE le 2026-08-03 ; surveillance continue via R-024.

## 3. Limitations techniques connues

| ID | Limitation | Conséquence | Réponse prévue |
|---|---|---|---|
| LIM-001 | Serveur refusant tout accès partiel | Vraie reprise réseau impossible | Retransmission annoncée ou redémarrage |
| LIM-002 | Retransmission depuis zéro | Données déjà reçues consommées à nouveau | Affichage honnête et comparaison progressive |
| LIM-003 | Identité distante insuffisante | Mélange impossible à exclure | Arrêt sûr et nouvelle destination |
| LIM-004 | Lien temporaire/session expirée | Requête refusée malgré fichier partiel | Nouveau lien légitime puis revalidation |
| LIM-005 | Prototype à connexion unique | Pas d’accélération segmentée | Tâches de segmentation à venir |
| LIM-006 | Migration SQLite v1→v2 seulement | Une montée additive est prouvée, interruption/rollback non | Tests interruption, backup et rollback |
| LIM-007 | Pas d’interface Windows | Usage CLI seulement | Revue architecture avant développement UI |
| LIM-008 | Proxy/NAT64 non validés | Protection SSRF limitée au profil direct | Profils explicites et tests dédiés |
| LIM-009 | Chaîne de récupération seulement diagnostique | Chaîne coordonnée, mais aucun fichier tronqué ni transfert repris | Revalidation sous verrou puis réparation/reprise testées |
| LIM-010 | Fautes de durabilité simulées dans le même processus | RÉSOLUE le 2026-08-04 par sept terminaisons subprocess sur un/deux blocs et restauration parent | Étendre à une erreur pendant écriture ; panne électrique reste distincte |

## 4. Modèle d’anomalie

```markdown
## BUG-XXX — Titre
- Description :
- Gravité : BLOQUANTE / CRITIQUE / MAJEURE / MINEURE
- Priorité : CRITIQUE / HAUTE / NORMALE / BASSE
- Version et environnement :
- Fréquence :
- Préconditions et données :
- Étapes de reproduction :
- Résultat attendu :
- Résultat obtenu :
- Preuves et logs expurgés :
- Solution temporaire :
- Cause probable/confirmée :
- Tâche, risque et test liés :
- Statut et date :
```

## 5. Cycle de vie

`NOUVELLE → CONFIRMÉE → EN COURS → À VÉRIFIER → CORRIGÉE`. Toute correction exige un test qui
échoue avant et réussit après, plus un test de non-régression adapté. Une anomalie de corruption ou
de sécurité bloque la publication jusqu’à décision explicite.

## LIM-011 — Finalisation partielle au même volume

- Statut : CONFIRMÉE — 2026-08-10.
- Effet : le move même volume, SHA-256, réparation non ambiguë et trois crashs subprocess sont présents,
  mais la copie inter-volume et l’exclusion mutuelle du futur hôte manquent.
- Contournement : ne pas présenter la finalisation comme complète et bloquer volumes/collisions ambigus.
- Clôture : protocole inter-volume et pannes matérielles ADR-029 validés.

## LIM-012 — Empreinte officielle distante non acquise automatiquement

- Statut : CONFIRMÉE — 2026-08-11.
- Effet : le SHA-256 local prouve la stabilité avant/après move et réparation, mais ne garantit pas
  l’authenticité distante sans valeur attendue issue d’une source de confiance.
- Contournement : fournir explicitement une empreinte attendue à la finalisation lorsqu’elle existe.
- Clôture : protocole versionné d’acquisition/validation d’un hash officiel avec tests adverses.

## BUG-002 — Dossiers source `Downloads` ignorés par Git

- Statut : CORRIGÉE — 2026-08-11.
- Cause : la règle non ancrée `downloads/` correspondait aussi aux dossiers C# `Downloads` sous
  Windows, dont le système de fichiers est insensible à la casse.
- Effet : plusieurs fichiers Domain/Application existaient et étaient testés localement mais
  n’étaient pas présents dans les commits GitHub initiaux.
- Correction : règle remplacée par `/downloads/`, limitée au dossier de données à la racine ; tous
  les fichiers source concernés sont ajoutés au présent commit.
