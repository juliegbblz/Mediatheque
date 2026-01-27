using Mediatheque.Data;
using System;

namespace Mediatheque.ViewModel
{
    /// <summary>
    /// Gère la logique métier d'une séance (validation horaire) et la notification de l'interface.
    /// </summary>
    public class EntrainementViewModel : ViewModelBase
    {
        private readonly Entrainement _modele;

        public EntrainementViewModel(Entrainement modele)
        {
            _modele = modele;
        }

        public Entrainement Modele => _modele;

        #region Propriétés de Catégorie
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
                    _modele.CategorieActiviteId = value?.Id ?? 0;

                    OnPropertyChanged(nameof(Categorie));
                    OnPropertyChanged(nameof(Couleur));
                    OnPropertyChanged(nameof(NomCategorie));
                }
            }
        }
        #endregion

        #region Propriétés Métier
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

                // Sécurité : ajuste la durée si l'horaire est déplacé trop près de minuit
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
                // Limite la durée pour que la séance ne dépasse jamais minuit
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
        #endregion

        /// <summary>
        /// Calcule le nombre de minutes restantes jusqu'à la fin de la journée (23:59:59).
        /// </summary>
        private int GetMinutesUntilMidnight(DateTime dt)
        {
            var midnight = dt.Date.AddDays(1);
            return (int)Math.Max(0, (midnight - dt).TotalMinutes);
        }
    }

    /// <summary>
    /// Calcule les coordonnées de rendu (X, Y, Taille) pour l'affichage graphique dans le calendrier.
    /// </summary>
    public class EntrainementViewModelAvecPosition
    {
        public EntrainementViewModel Entrainement { get; }

        // Propriétés de rendu pour le Canvas XAML
        public double PositionX { get; }
        public double PositionY { get; }
        public double Largeur { get; }
        public double Hauteur { get; }

        // Raccourcis de données
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

            // Calcul du jour (Lundi = 0)
            int jourSemaine = ((int)entrainement.DateHeure.DayOfWeek + 6) % 7;

            // Calcul horizontal (gestion des chevauchements de séances)
            double largeurDisponible = largeurColonne / colonnesTotal;
            Largeur = largeurDisponible - 4;
            PositionX = (jourSemaine * largeurColonne) + (colonneIndex * largeurDisponible);

            // Calcul vertical (position par rapport à l'heure d'ouverture du planning)
            double heureDecimale = entrainement.DateHeure.Hour + entrainement.DateHeure.Minute / 60.0;
            double heuresDepuisDebutAffichage = heureDecimale - heureDebut;
            PositionY = Math.Max(0, heuresDepuisDebutAffichage * hauteurHeure);

            // Hauteur proportionnelle au temps (ex: 1h = 60px)
            Hauteur = (entrainement.DureeMinutes / 60.0) * hauteurHeure - 4;
        }
    }
}