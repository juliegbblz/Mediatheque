using Mediatheque.Data;
using System;

namespace Mediatheque.ViewModel
{
    /// <summary>
    /// ViewModel représentant un entraînement.
    /// Encapsule le modèle de données et applique les règles métier
    /// tout en notifiant l’interface des changements.
    /// </summary>
    public class EntrainementViewModel : ViewModelBase
    {
        private readonly Entrainement _modele;

        public EntrainementViewModel(Entrainement modele)
        {
            _modele = modele;
        }

        /// <summary>
        /// Accès direct au modèle EF sous-jacent.
        /// Utile pour les opérations de persistance.
        /// </summary>
        public Entrainement Modele => _modele;

        #region Catégorie

        /// <summary>
        /// Couleur associée à la catégorie (fallback gris si absente).
        /// </summary>
        public string Couleur => Modele.Categorie?.CouleurHex ?? "#808080";

        /// <summary>
        /// Nom lisible de la catégorie.
        /// </summary>
        public string NomCategorie => Modele.Categorie?.Nom ?? "Sans catégorie";

        /// <summary>
        /// Catégorie sélectionnée pour l’entraînement.
        /// Met à jour l’ID relationnel côté modèle.
        /// </summary>
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

        #region Propriétés métier

        /// <summary>
        /// Nom ou type d’activité pratiquée.
        /// </summary>
        public string Activite
        {
            get => _modele.Activite;
            set
            {
                _modele.Activite = value;
                OnPropertyChanged(nameof(Activite), nameof(Description));
            }
        }

        /// <summary>
        /// Date et heure de début de la séance.
        /// Préserve l’heure existante lorsqu’un DatePicker renvoie minuit.
        /// </summary>
        public DateTime DateHeure
        {
            get => _modele.DateHeure;
            set
            {
                if (_modele.DateHeure != value)
                {
                    if (value.TimeOfDay == TimeSpan.Zero &&
                        _modele.DateHeure.TimeOfDay != TimeSpan.Zero)
                    {
                        _modele.DateHeure = value.Date.Add(_modele.DateHeure.TimeOfDay);
                    }
                    else
                    {
                        _modele.DateHeure = value;
                    }

                    OnPropertyChanged(nameof(DateHeure));
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        /// <summary>
        /// Lieu de l’entraînement.
        /// </summary>
        public string Lieu
        {
            get => _modele.Lieu;
            set
            {
                _modele.Lieu = value;
                OnPropertyChanged(nameof(Lieu));
            }
        }

        /// <summary>
        /// Durée en minutes.
        /// Est automatiquement bornée pour éviter tout dépassement après minuit.
        /// </summary>
        public int DureeMinutes
        {
            get => _modele.DureeMinutes;
            set
            {
                int maxMinutes = GetMinutesUntilMidnight(_modele.DateHeure);
                int clamped = Math.Min(Math.Max(0, value), maxMinutes);

                if (_modele.DureeMinutes != clamped)
                {
                    _modele.DureeMinutes = clamped;
                    OnPropertyChanged(nameof(DureeMinutes));
                }
            }
        }

        /// <summary>
        /// Force la notification de toutes les propriétés.
        /// Utile lors de rafraîchissements globaux de l’affichage.
        /// </summary>
        public void NotifierTout()
        {
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Texte synthétique utilisé dans les listes ou info-bulles.
        /// </summary>
        public string Description =>
            $"{DateHeure:dd/MM/yyyy HH:mm} - {Activite} @ {Lieu}";

        #endregion

        /// <summary>
        /// Calcule le nombre maximal de minutes possibles
        /// avant la fin de la journée.
        /// </summary>
        private int GetMinutesUntilMidnight(DateTime dt)
        {
            var midnight = dt.Date.AddDays(1);
            return (int)Math.Max(0, (midnight - dt).TotalMinutes);
        }
    }

    /// <summary>
    /// Enveloppe graphique d’un entraînement.
    /// Fournit les coordonnées et dimensions nécessaires
    /// à l’affichage dans un planning (Canvas).
    /// </summary>
    public class EntrainementViewModelAvecPosition
    {
        public EntrainementViewModel Entrainement { get; }

        /// <summary>
        /// Coordonnées calculées pour le rendu XAML.
        /// </summary>
        public double PositionX { get; }
        public double PositionY { get; }
        public double Largeur { get; }
        public double Hauteur { get; }

        /// <summary>
        /// Accès direct aux données utiles pour le binding.
        /// </summary>
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

            // Jour de la semaine normalisé (lundi = 0)
            int jourSemaine =
                ((int)entrainement.DateHeure.DayOfWeek + 6) % 7;

            // Répartition horizontale pour gérer les chevauchements
            double largeurDisponible = largeurColonne / colonnesTotal;
            Largeur = largeurDisponible - 4;
            PositionX =
                (jourSemaine * largeurColonne) +
                (colonneIndex * largeurDisponible);

            // Position verticale relative à l’heure de début du planning
            double heureDecimale =
                entrainement.DateHeure.Hour +
                entrainement.DateHeure.Minute / 60.0;

            double heuresDepuisDebutAffichage =
                heureDecimale - heureDebut;

            PositionY =
                Math.Max(0, heuresDepuisDebutAffichage * hauteurHeure);

            // Hauteur proportionnelle à la durée
            Hauteur =
                (entrainement.DureeMinutes / 60.0) * hauteurHeure - 4;
        }
    }
}
