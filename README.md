# Gestionnaire d'Entraînements Sportifs - BDS

**Auteurs :** Zilberberg Julie - Di Loreto Charlotte  
**Promotion :** 4A IA2R  
**Année Universitaire :** 2025/2026

---

## Note sur le projet
Bien que l'application soit dédiée à la gestion d'entraînements sportifs, le projet conserve le nom Mediatheque. Cela est parce que nous avons utilisé la structure technique et le modèle de données du TD "Médiathèque” comme socle de développement. Ce modèle a ensuite été adapté et étendu pour répondre aux besoins spécifiques de ce devoir maison, notamment pour la gestion du calendrier et des séances de sport.

## Thème choisi
Le projet est une application de gestion de planning sportif hebdomadaire du bureau des sports de Polytech Nancy. L'objectif est de permettre à un utilisateur de visualiser, d'organiser et de planifier les séances d'entraînements de manière ergonomique via une interface de calendrier dynamique et intuitive.

## Modèle de données
L'application s'appuie sur une base de données **SQLite** gérée via **Entity Framework Core**.

### Entité `Entrainement`
* **Id** : Identifiant unique (auto-incrémenté).
* **Activite** : Nom de la discipline (ex: Musculation, Natation).
* **Lieu** : Endroit de la séance (ex: Gymnase, Stade).
* **DateHeure** : Date et heure de début.
* **DureeMinutes** : Durée de l'entraînement.
* **CategorieActiviteId** : Clé étrangère liant l'entraînement à une catégorie.

### Entité `CategorieActivite`
Les activités sont classées par types pour une meilleure organisation visuelle :
* **Nom** : Libellé de la catégorie (ex: Sport de combat, Endurance, Force).
* **Couleur** : Code hexadécimal associé pour l'affichage différencié dans le calendrier.

## Fonctionnalités réalisées

### Affichage et Navigation
* **Calendrier hebdomadaire** : Affichage des séances sur une grille de 7 jours avec une échelle temporelle débutant à **06:00**.
* **Navigation temporelle** : Commandes pour passer à la semaine suivante, précédente ou revenir instantanément à la semaine actuelle.
* **Positionnement dynamique** : Calcul automatique de la position (X, Y) et de la hauteur des blocs en fonction de l’horaire et de la durée.
* **Mise en évidence** : Détection automatique et marquage visuel du jour courant dans l'en-tête.

### Gestion des Entraînements (CRUD)
* **Ajout** : Interface de création de nouvelles séances via un formulaire réactif.
* **Édition** : Modification complète des informations d’une séance existante.
* **Suppression** : Retrait définitif d’une séance avec système de **confirmation de sécurité**.
* **Persistance** : Sauvegarde et chargement des données depuis la base SQLite.

### Logique Métier & Ergonomie
* **Gestion des collisions** : Algorithme de détection des chevauchements d’horaires. En cas de conflit, la largeur des blocs s'ajuste automatiquement pour que toutes les séances restent lisibles.
* **Contraintes horaires** : 
    * Limitation automatique de la durée pour empêcher un entraînement de déborder sur le jour suivant (limite à minuit).
    * Blocage des saisies avant l'heure d'ouverture (06:00).
* **Déplacement rapide** : Boutons de raccourcis permettant d'avancer ou de reculer l'heure d'une séance d'un simple clic (+1h / -1h).
