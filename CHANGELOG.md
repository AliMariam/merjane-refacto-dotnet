#### `fix` Commande inexistante : 404 au lieu d'un plantage 500
- **Avant :** appeler l'API avec un id de commande qui n'existe pas provoquait une `NullReferenceException`, donc une erreur 500.
- **Après :** l'API répond **404 Not Found**, la réponse normale pour une ressource absente
#### `fix` Une commande est enregistrée entièrement ou pas du tout
- **Avant :** `SaveChanges()` était appelé après chaque produit. Si le 3e produit plantait, les 2 premiers restaient enregistrés : la commande était à moitié traitée.
- **Après :** une seule sauvegarde, à la fin. Si un produit échoue, rien n'est enregistré.
#### `fix` `Order.Items` est `null`
- La liste des produits est initialisée vide : plus de `NullReferenceException` possible sur une commande sans produit.
#### `fix` Message d'erreur clair pour un produit EXPIRABLE sans date
- `Expirable product 'Milk' has no expiry date.` C'est le même type d'exception (`InvalidOperationException`), mais on comprend tout de suite le problème.
#### `test` Une couche de sécurité posée avant de toucher au code
- des tests d'intégration qui vérifient, pour chaque produit d'une commande, le stock final en base et les notifications exactes envoyées.
- avant de refactorer du code qui n'a pas de tests, on commence toujours par figer le comportement actuel.
#### `refactor` Une classe par type de produit
- une règle métier se lit dans un seul fichier court. Et, pour ajouter un type de produit, on crée une nouvelle classe et on ajoute une ligne dans `Program.cs`, sans toucher au reste.
#### `refactor` Le contrôleur ne s'occupe plus que du calls HTTP
- `OrdersController` appelle `IOrderService` et transforme le résultat en 200 ou 404. Il ne connaît plus EF Core. L'action est asynchrone et accepte un `CancellationToken`.
#### `refactor` `OrderService` orchestre le traitement
- les handlers sont rangés dans un dictionnaire indexé par type. Un produit de type inconnu ou sans type est ignoré, comme avant, et un avertissement est écrit dans les logs
#### `refactor` L'accès à la base est isolé dans un repository
- `OrderRepository` est la seule classe qui utilise EF Core. Elle charge une commande avec ses produits et sauvegarde, de façon asynchrone.
- les règles métier ne dépendent plus de la base, on peut donc les tester sans elle.
#### `refactor` La date de traitement est passée en paramètre
- `OrderService` lit l'heure une seule fois par commande et la transmet aux handlers : `Handle(product, now)`. Dans les tests, on passe simplement une date fixe.
#### `refactor` Des méthodes qui disent ce qu'elles font
- `Product` expose `IsInStock`, `DecrementStock()` et `MarkAsUnavailable()`. Les handlers les utilisent au lieu de modifier `Available` directement.
#### `refactor` Les types de produit
- Les valeurs `"NORMAL"`, `"SEASONAL"`, `"EXPIRABLE"` sont des constantes : `ProductTypes.Normal`, `ProductTypes.Seasonal`, `ProductTypes.Expirable`.
#### `refactor` Interfaces et implémentations
- La règle à suivre : `Services/` contient uniquement les interfaces ; `Services/Impl/` contient uniquement les implémentations.
#### `refactor` Un avertissement dans les logs quand un produit est ignoré
- un produit de type inconnu (`"DIGITAL"`, ou `"normal"` en minuscules) ou sans type était ignoré sans laisser une traçabilité.
#### `refactor` Des conditions nommées dans les règles SEASONAL
- `IsInSeason`, `RestockEndsAfterSeason`, `SeasonHasNotStarted` : une implémentation logique qui suit métier.
#### `refactor` Cleanup : dead code supprimé
#### `test` tests unitaires
- structure GIVEN / WHEN / THEN, date fixe, et `VerifyNoOtherCalls` pour faire échouer le test si une notification est envoyée par erreur.