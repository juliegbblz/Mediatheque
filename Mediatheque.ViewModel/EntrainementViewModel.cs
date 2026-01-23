using Mediatheque.Data;
using System;

namespace Mediatheque.ViewModel
{
    public class EntrainementViewModel : ViewModelBase
    {
        private readonly Entrainement _modele;

        public EntrainementViewModel(Entrainement modele)
        {
            _modele = modele;
        }

        public Entrainement Modele => _modele;

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
                _modele.DureeMinutes = value;
                OnPropertyChanged(nameof(DureeMinutes));
            }
        }

        public string Description => $"{DateHeure:dd/MM/yyyy HH:mm} - {Activite} @ {Lieu}";
    }

    // Classe pour gérer le positionnement des entraînements dans le calendrier
    public class EntrainementViewModelAvecPosition
    {
        public EntrainementViewModel Entrainement { get; }
        public double PositionX { get; }
        public double PositionY { get; }
        public double Largeur { get; }
        public double Hauteur { get; }

        // Propriétés pour le binding direct
        public string Activite => Entrainement.Activite;
        public DateTime DateHeure => Entrainement.DateHeure;
        public string Lieu => Entrainement.Lieu;
        public int DureeMinutes => Entrainement.DureeMinutes;

        public EntrainementViewModelAvecPosition(
            EntrainementViewModel entrainement,
            DateTime debutSemaine,
            int heureDebut,
            int hauteurHeure,
            double largeurColonne)
        {
            Entrainement = entrainement;

            // Calculer la colonne (jour de la semaine, 0 = lundi)
            int jourSemaine = ((int)entrainement.DateHeure.DayOfWeek + 6) % 7; // Lundi = 0
            PositionX = jourSemaine * largeurColonne;

            // Calculer la position verticale selon l'heure
            double heureDecimale = entrainement.DateHeure.Hour + entrainement.DateHeure.Minute / 60.0;
            PositionY = (heureDecimale - heureDebut) * hauteurHeure;

            // Largeur = largeur de la colonne moins les marges (augmenté pour éviter le débordement)
            Largeur = largeurColonne - 8; // Augmenté de 4 à 8 pour avoir plus de marge

            // Hauteur basée sur la durée
            Hauteur = (entrainement.DureeMinutes / 60.0) * hauteurHeure - 4;
        }
    }
}