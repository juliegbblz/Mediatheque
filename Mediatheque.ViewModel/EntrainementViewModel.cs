    using Mediatheque.Data;
    using System;

    namespace Mediatheque.ViewModel
    {
   
        /// ViewModel représentant une séance d'entraînement.
        /// Gère la validation métier (limite de minuit) et la notification de l'UI.
  
        public class EntrainementViewModel : ViewModelBase
        {
            private readonly Entrainement _modele;

            public EntrainementViewModel(Entrainement modele)
            {
                _modele = modele;
            }

            public Entrainement Modele => _modele;

            public string Couleur => Modele.Categorie?.CouleurHex ?? "#808080";
            public string NomCategorie => Modele.Categorie?.Nom ?? "Sans catégorie";

        public CategorieActivite Categorie
        {
            get => _modele.Categorie;
            set
            {
                if (_modele.Categorie != value)
                {
                    _modele.Categorie = value;
                    // On met à jour l'ID pour la base de données
                    _modele.CategorieActiviteId = value?.Id ?? 0;

                    // On notifie TOUT ce qui dépend de la catégorie
                    OnPropertyChanged(nameof(Categorie));
                    OnPropertyChanged(nameof(Couleur));
                    OnPropertyChanged(nameof(NomCategorie));
                }
            }
        }
        public string Activite
            {
                get => _modele.Activite;
                set
                {
                    _modele.Activite = value;
                    OnPropertyChanged(nameof(Activite), nameof(Description));
                }
            }

            public DateTime DateHeure
            {
                get => _modele.DateHeure;
                set
                {
                    _modele.DateHeure = value;

                    // Ajustement automatique de la durée si le nouvel horaire dépasse minuit
                    int maxMinutes = GetMinutesUntilMidnight(_modele.DateHeure);
                    if (_modele.DureeMinutes > maxMinutes)
                    {
                        _modele.DureeMinutes = maxMinutes;
                        OnPropertyChanged(nameof(DureeMinutes));
                    }

                    OnPropertyChanged(nameof(DateHeure), nameof(Description));
                }
            }

            public string Lieu
            {
                get => _modele.Lieu;
                set
                {
                    _modele.Lieu = value;
                    OnPropertyChanged(nameof(Lieu));
                }
            }

            public int DureeMinutes
            {
                get => _modele.DureeMinutes;
                set
                {
                    // Empêche techniquement la séance de déborder sur le jour suivant
                    int maxMinutes = GetMinutesUntilMidnight(_modele.DateHeure);
                    int clamped = Math.Min(Math.Max(0, value), maxMinutes);

                    if (_modele.DureeMinutes != clamped)
                    {
                        _modele.DureeMinutes = clamped;
                        OnPropertyChanged(nameof(DureeMinutes));
                    }
                }
            }

            public string Description => $"{DateHeure:dd/MM/yyyy HH:mm} - {Activite} @ {Lieu}";

      
            /// Calcule le temps restant avant la fin de la journée en cours.
       
            private int GetMinutesUntilMidnight(DateTime dt)
            {
                var midnight = dt.Date.AddDays(1);
                return (int)Math.Max(0, (midnight - dt).TotalMinutes);
            }
        }

   
        /// Wrapper calculant les coordonnées graphiques (X, Y) pour l'affichage dans le Canvas du calendrier.
  
        public class EntrainementViewModelAvecPosition
        {
            public EntrainementViewModel Entrainement { get; }

            // Coordonnées et dimensions pour le rendu XAML (Canvas)
            public double PositionX { get; }
            public double PositionY { get; }
            public double Largeur { get; }
            public double Hauteur { get; }

            // Raccourcis pour le binding direct dans le template d'affichage
            public string Activite => Entrainement.Activite;
            public DateTime DateHeure => Entrainement.DateHeure;
            public string Lieu => Entrainement.Lieu;
            public int DureeMinutes => Entrainement.DureeMinutes;

            public string Couleur => Entrainement.Couleur;

        public EntrainementViewModelAvecPosition(
                EntrainementViewModel entrainement,
                DateTime debutSemaine,
                int heureDebut,
                int hauteurHeure,
                double largeurColonne,
                int colonneIndex = 0,
                int colonnesTotal = 1)
            {
                Entrainement = entrainement;

                // Placement horizontal : Calcul du jour de la semaine (0 = Lundi)
                int jourSemaine = ((int)entrainement.DateHeure.DayOfWeek + 6) % 7;

                // Calcul de la largeur : Gestion du chevauchement si plusieurs séances ont lieu en même temps
                double largeurDisponible = largeurColonne / colonnesTotal;
                Largeur = largeurDisponible - 4; // Marge de 4px pour l'esthétique

                // Position X : Colonne du jour + décalage horizontal si superposition
                PositionX = (jourSemaine * largeurColonne) + (colonneIndex * largeurDisponible);

                // Position Y : Distance par rapport à l'heure de début du planning (ex: 6h)
                double heureDecimale = entrainement.DateHeure.Hour + entrainement.DateHeure.Minute / 60.0;
                double heuresDepuisDebutAffichage = heureDecimale - heureDebut;
                PositionY = Math.Max(0, heuresDepuisDebutAffichage * hauteurHeure);

                // Hauteur : Proportionnelle à la durée de la séance (1h = hauteurHeure pixels)
                Hauteur = (entrainement.DureeMinutes / 60.0) * hauteurHeure - 4;
            }


        }

    }