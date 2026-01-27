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
            double largeurColonne,
            int colonneIndex = 0,    // Ajouté pour corriger l'erreur CS1739
            int colonnesTotal = 1)   // Ajouté pour gérer le nombre d'entraînements
        {
            Entrainement = entrainement;

            // 1. Calculer la colonne de base (Lundi = 0, Mardi = 1, etc.)
            int jourSemaine = ((int)entrainement.DateHeure.DayOfWeek + 6) % 7;

            // 2. Calculer la largeur dynamique
            // On divise la largeur totale de la colonne par le nombre d'entraînements simultanés
            double largeurDisponible = largeurColonne / colonnesTotal;
            Largeur = largeurDisponible - 4; // On garde une petite marge de 4px

            // 3. Calculer la PositionX
            // (Position du jour) + (Décalage selon l'index de l'entraînement dans l'heure)
            PositionX = (jourSemaine * largeurColonne) + (colonneIndex * largeurDisponible);

            // 4. PositionY (reste identique à votre logique)
            double heureDecimale = entrainement.DateHeure.Hour + entrainement.DateHeure.Minute / 60.0;
            double heuresDepuisDebutAffichage = heureDecimale - 8.0;  
            PositionY = Math.Max(0, heuresDepuisDebutAffichage * hauteurHeure);

            // 5. Hauteur (reste identique à votre logique)
            Hauteur = (entrainement.DureeMinutes / 60.0) * hauteurHeure - 4;
        }
    }
}