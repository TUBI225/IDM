# FAQ technique

Version documentaire : 1.1  
Date de création : 2026-08-03  
Dernière mise à jour : 2026-08-04  
Statut : PROPOSÉ POUR REVUE  
Responsable logique : Architecture et maintenance documentaire  
Documents liés : `Cahier_des_charges.md`, `ARCHITECTURE_TECHNIQUE.md`, `SECURITE.md`, `INSTRUCTIONS_IA.md`

## Sommaire

1. Produit et légalité
2. Architecture et technologies
3. Reprise et intégrité
4. Sécurité et navigateur
5. Développement et gouvernance

## 1. Produit et légalité

### 1.1 Pourquoi ne pas copier Internet Download Manager ?

Le projet s’inspire uniquement du besoin général : gérer et reprendre des téléchargements. Copier
du code, une marque, une interface reconnaissable ou des mécanismes propriétaires créerait des
risques juridiques et empêcherait une architecture maîtrisée. Windows Download Manager doit avoir
son identité, ses composants, son ergonomie et ses tests propres. Voir le cahier des charges.

### 1.2 Le logiciel peut-il forcer tous les serveurs à reprendre ?

Non. `Range` est une demande. Le serveur peut répondre `206`, ignorer la plage avec `200`, ou la
refuser. Le logiciel peut tester raisonnablement les capacités, suivre une redirection autorisée,
accepter un nouveau lien légitime ou retransmettre depuis zéro en comparant les octets. Il ne peut
pas obliger le serveur à fournir un accès partiel. Voir `PROTOCOLE_TEST_REPRISE.md`.

### 1.3 Qu’est-ce que la retransmission contrôlée ?

Le serveur renvoie depuis zéro ; le moteur compare le nouveau flux avec les octets locaux et ne
recommence l’écriture qu’au premier octet absent. Cela protège le travail local mais ne réduit pas
les données réseau déjà consommées. Toute différence provoque un arrêt sûr.

## 2. Architecture et technologies

### 2.1 Pourquoi C# et .NET 10 ont-ils été retenus ?

Ils offrent une intégration Windows, un typage statique, `async/await`, `HttpClient` et des outils de
diagnostic adaptés aux flux volumineux. ADR-021 les a retenus et le socle compile. Le prototype Python
est maintenant une référence séparée, conservée jusqu’à parité minimale ; il n’est plus le produit cible.

### 2.2 Pourquoi WinUI 3 et non WPF ?

ADR-022 a retenu WinUI 3 pour une nouvelle application Windows, avec MVVM et moteur indépendant.
WPF reste l’alternative si le POC WinUI échoue sur packaging, accessibilité, stabilité ou versions
Windows supportées. Le POC reste obligatoire avant développement visuel important.

### 2.3 Pourquoi SQLite ?

SQLite fonctionne hors ligne, ne nécessite aucun service, fournit transactions et index et convient
à un état local. Il ne remplace pas le fichier temporaire : la base décrit les zones confirmées,
le disque contient les octets. Les migrations et la concurrence doivent être explicitement gérées.

### 2.4 Pourquoi séparer moteur et interface ?

Le moteur doit continuer, se tester et récupérer un état sans fenêtre graphique. La séparation
évite que la fermeture de l’interface interrompe mal une écriture et permet à une CLI, une UI ou un
hôte navigateur de commander le même domaine via des contrats contrôlés.

### 2.5 `HttpClient` ou libcurl ?

`HttpClient` est l’option native à étudier en premier pour .NET. libcurl est une alternative si des
écarts de protocoles, proxy ou performance sont prouvés par des tests. Ajouter libcurl accroît le
packaging, la surface de vulnérabilités et la maintenance native.

## 3. Reprise et intégrité

### 3.1 Pourquoi un fichier temporaire ?

Il empêche l’utilisateur et les autres programmes de confondre un contenu partiel avec un fichier
final. Le renommage atomique n’intervient qu’après fermeture des écritures et vérification.

### 3.2 Pourquoi 100 % ne signifie-t-il pas `TERMINÉ` ?

Le réseau peut avoir livré le nombre attendu d’octets alors qu’une écriture manque, qu’un segment se
chevauche mal ou que le fichier n’est pas synchronisé. Après 100 %, il reste `VÉRIFICATION`, puis
`FINALISATION`, puis seulement `TERMINÉ`.

### 3.3 Pourquoi `Accept-Ranges` ne suffit-il pas ?

Cet en-tête peut manquer sur un serveur compatible ou être annoncé par un serveur/proxy défaillant.
Un sondage court et la validation stricte de `206` et `Content-Range` apportent la preuve réelle.

### 3.4 Comment reprendre après un crash ?

Relire SQLite, inspecter le fichier temporaire, choisir la position sûre la plus basse, invalider les
zones incertaines, réanalyser le distant, comparer une zone de recouvrement puis reprendre. Un crash
réel doit être testé ; la simulation actuellement réussie ne suffit pas à valider le produit.

### 3.5 Pourquoi l’IA n’est-elle pas nécessaire ?

Positions, plages, hash, états et écritures sont déterministes. Une IA peut expliquer un journal,
mais ne doit jamais confirmer l’identité, écrire, supprimer ou finaliser un fichier.

## 4. Sécurité et navigateur

### 4.1 Comment protéger cookies et jetons ?

Ne collecter que le strict nécessaire, limiter leur durée et leur origine, chiffrer avec une
protection liée à l’utilisateur Windows (DPAPI à étudier), ne jamais les journaliser et les effacer
quand ils ne sont plus requis. La base ne doit pas stocker un secret en clair.

### 4.2 Pourquoi Native Messaging ?

Une extension ne doit pas ouvrir librement un port local. Native Messaging fournit un canal encadré
par le navigateur. L’hôte doit valider origine, taille et schéma de chaque message, et ne jamais
exécuter une commande arbitraire transmise par l’extension.

### 4.3 Comment tester une coupure ?

Utiliser un serveur et un proxy de test contrôlés, interrompre à une position connue, conserver logs
et hash, restaurer le réseau, puis comparer chaque octet final. Ne pas se contenter du statut UI.

## 5. Développement et gouvernance

### 5.1 Comment ajouter une dépendance ?

Établir le besoin, étudier une solution interne et des alternatives, vérifier source officielle,
licence, maintenance, vulnérabilités, compatibilité, poids et fonctionnement hors ligne. Documenter
avant intégration dans `DEPENDANCES.md`, `SECURITE.md` et l’ADR approprié.

### 5.2 Quand une tâche est-elle terminée ?

Quand le besoin, le code, les tests requis, la sécurité, les données, les risques et la documentation
sont cohérents et vérifiés. Compiler ne suffit pas. Tout test non exécuté impose `PARTIEL` ou
`À VÉRIFIER` selon son importance.

### 5.3 Comment reprendre le projet dans une nouvelle conversation ?

Lire `INSTRUCTIONS_IA.md`, puis les documents dans l’ordre imposé, vérifier Git et le code réel,
identifier la tâche et annoncer les tests. La conversation n’est jamais la source principale de
vérité.

### 5.4 Quelle pile représente l’état réel ?

Le C#/.NET est le produit actif. Python est une référence temporaire avec ses propres données et
preuves. Une fonction présente uniquement en Python doit être annoncée comme « prototype », jamais
comme capacité C#. `ETAT_ACTUEL_PROJET.md` est la photographie autoritaire de cette séparation.

### 5.5 Pourquoi un hôte séparé plutôt que le moteur dans WinUI ?

Pour qu’une fermeture de fenêtre n’interrompe pas les transferts et qu’un seul processus possède
SQLite et les fichiers. Il reste dans la session utilisateur, sans service élevé.

### 5.6 Pourquoi MSTest.Sdk et `Microsoft.Data.Sqlite` ?

MSTest.Sdk fournit le runner Microsoft standard et une restauration verrouillable. Le fournisseur
SQLite retenu permet des transactions SQL explicites sans ajouter EF Core. Les deux sont maintenant
installés ; SQLite possède une migration v2 additive testée depuis v1, mais crash et rollback ne
sont pas encore exécutés.

### 5.7 Que contient réellement G2 ?

Le profil réseau direct connecte le socket à l’IP filtrée, le writer crée exclusivement puis
synchronise le temporaire, et SQLite sauvegarde l’état minimal. L’orchestrateur relie maintenant ces
briques pour un téléchargement neuf et l’intégration prouve le checkpoint exact. Il ne sait pas
encore reprendre une tâche persistée ni finaliser : crash, réparation et ADR-029 restent la prochaine
étape.

### 5.9 Que persiste maintenant SQLite pour préparer une reprise ?

Le chemin temporaire et une identité minimale : URL finale expurgée, taille, ETag, Last-Modified et
capacité Range. Ils sont enregistrés ensemble avant création du fichier. Une ancienne ligne v1 reste
lisible sans ces champs ; elle ne devient pas automatiquement reprenable. La prochaine étape doit
encore comparer base, fichier et distant avant toute écriture. La partie locale est maintenant
diagnostiquée en lecture seule : absent, plus court, égal ou plus long que le checkpoint. Ce résultat
ne tronque rien. La partie distante compare désormais URL finale expurgée, taille, ETag,
Last-Modified et Range avec une sonde sans flux de transfert. Un évaluateur pur compose désormais les
deux diagnostics, cumule tous les motifs de blocage et ne laisse passer que le couple fichier exact +
distant compatible vers une future vérification de recouvrement. Cette décision n’autorise toujours
ni écriture, ni troncature, ni reprise. Le recouvrement est maintenant réalisé en lecture seule sur
une fenêtre maximale de 64 Kio : le fichier local et une plage HTTP fermée sont comparés octet par
octet. Une divergence bloque ; une correspondance reste à coordonner et revalider avant toute action.
Le coordinateur exécute désormais cette chaîne dans l’ordre et s’arrête avant réseau si le disque ou
les métadonnées locales suffisent à bloquer. Son statut final reste un diagnostic : même
`OverlapMatched` ne déclenche ni troncature, ni écriture, ni reprise du transfert.

### 5.10 Que prouve le banc de fautes flush/SQLite ?

Il utilise le vrai writer durable et le vrai dépôt SQLite, puis injecte une exception après flush,
avant commit ou après commit. Après réouverture, la base reste derrière le fichier ou exactement
alignée, jamais en avance. Il ne s’agit pas encore d’un crash réel : un test subprocess terminé
brutalement est la prochaine preuve requise.

Le hôte subprocess est maintenant présent : il est tué après flush, avant commit ou après commit,
et le parent restaure séparément SQLite et le fichier. Cette preuve valide un crash de processus sur
un bloc de 5 octets. Une seconde série tue pendant le deuxième bloc d’un contenu de 70 000 octets :
le premier checkpoint de 65 536 est conservé jusqu’au second commit, puis 70 000 est restauré après
commit. Une mort avant le deuxième appel disque conserve aussi fichier, checkpoint et contenu à
65 536. Cela ne prouve toujours pas une mort pendant l’écriture, un reboot ou une panne électrique.

### 5.8 Pourquoi SQLitePCLRaw 2.1.12 est-il épinglé explicitement ?

`Microsoft.Data.Sqlite` 10.0.10 amenait initialement la version 2.1.11, bloquée par l’audit pour une
vulnérabilité élevée. L’épingle 2.1.12 et les verrous empêchent son retour silencieux.

### 5.9 Le moteur C# reprend-il maintenant un téléchargement interrompu ?

Oui, dans le cas sûr actuellement couvert : temporaire exactement au checkpoint, identité distante
compatible, support Range conservé et recouvrement identique. Il reprend au checkpoint, confirme
chaque bloc après flush puis passe à `Verifying`. Toute divergence reste bloquante et sans mutation.

### 5.12 La finalisation est-elle complètement résistante aux crashs ?

Partiellement. Le moteur persiste `Finalizing`, renomme sans écraser sur le même volume et persiste
`Completed`. Trois arrêts subprocess prouvent la réparation après intention, après move et après
commit final. Le hash final, les copies inter-volumes, le reboot Windows et les pannes matérielles
restent à prouver.
