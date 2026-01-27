using Mediatheque.Data;
using Mediatheque.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Mediatheque.ViewModel
{
   
    /// Interface de communication entre le ViewModel et la Vue (MainWindow).

    public interface IMainView
    {
        void InformationAjout();
        bool ConfirmationSuppression();
        void AlerteDepassementMinuit();
    }

    /// ViewModel principal gérant la logique du calendrier et les opérations CRUD sur les entraînements.
   
    public class MainViewModel : ViewModelBase
    {
        private readonly MediathequeContext _context;
        private readonly IMainView _view;

        private DateTime _debutSemaine;
        private const int HEURE_DEBUT = 6; // Le planning commence désormais à 06:00
        private const int HAUTEUR_HEURE = 60; // Ratio de 60px pour 1 heure
        private const double LARGEUR_COLONNE_BASE = 95;

        public ObservableCollection<EntrainementViewModel> Entrainements { get; } = new ObservableCollection<EntrainementViewModel>();

        private double _largeurTotalePlanning;
        public double LargeurTotalePlanning
        {
            get => _largeurTotalePlanning;
            set
            {
                if (_largeurTotalePlanning != value)
                {
                    _largeurTotalePlanning = value;
                    OnPropertyChanged(nameof(LargeurTotalePlanning));
                    // Force la recalcul des positions X lors du redimensionnement de la fenêtre
                    OnPropertyChanged(nameof(EntrainementsSemaine));
                }
            }
        }

        // --- Détection du jour actuel pour mise en évidence dans l'UI ---
        public bool EstAujourdhuiLundi => DateTime.Today == DateLundi.Date;
        public bool EstAujourdhuiMardi => DateTime.Today == DateMardi.Date;
        public bool EstAujourdhuiMercredi => DateTime.Today == DateMercredi.Date;
        public bool EstAujourdhuiJeudi => DateTime.Today == DateJeudi.Date;
        public bool EstAujourdhuiVendredi => DateTime.Today == DateVendredi.Date;
        public bool EstAujourdhuiSamedi => DateTime.Today == DateSamedi.Date;
        public bool EstAujourdhuiDimanche => DateTime.Today == DateDimanche.Date;

        private EntrainementViewModel? _selectionEntrainement;
        public EntrainementViewModel? SelectionEntrainement
        {
            get => _selectionEntrainement;
            set
            {
                _selectionEntrainement = value;
                OnPropertyChanged(nameof(SelectionEntrainement), nameof(SupprimerEntrainementActif));
            }
        }

        public MainViewModel(MediathequeContext context, IMainView view)
        {
            _context = context;
            _view = view;
            _debutSemaine = GetDebutSemaine(DateTime.Now);

            // Chargement initial des données depuis SQLite
            foreach (var e in _context.Entrainements)
            {
                Entrainements.Add(new EntrainementViewModel(e));
            }

            Entrainements.CollectionChanged += (s, e) => NotifierChangementPlanning();
        }

        private DateTime GetDebutSemaine(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        // --- Propriétés de gestion des dates de la semaine ---
        public string SemaineAffichee => $"Semaine du {_debutSemaine:dd/MM/yyyy} au {_debutSemaine.AddDays(6):dd/MM/yyyy}";
        public DateTime DateLundi => _debutSemaine;
        public DateTime DateMardi => _debutSemaine.AddDays(1);
        public DateTime DateMercredi => _debutSemaine.AddDays(2);
        public DateTime DateJeudi => _debutSemaine.AddDays(3);
        public DateTime DateVendredi => _debutSemaine.AddDays(4);
        public DateTime DateSamedi => _debutSemaine.AddDays(5);
        public DateTime DateDimanche => _debutSemaine.AddDays(6);

     
        /// Liste les entraînements de la semaine actuelle calculés avec leurs coordonnées graphiques.
      
        public IEnumerable<EntrainementViewModelAvecPosition> EntrainementsSemaine
        {
            get
            {
                double largeurUneColonne = LargeurTotalePlanning > 0 ? (LargeurTotalePlanning / 7) : LARGEUR_COLONNE_BASE;
                var finSemaine = _debutSemaine.AddDays(7);

                var inWeek = Entrainements
                    .Where(e => e.DateHeure >= _debutSemaine && e.DateHeure < finSemaine)
                    .OrderBy(e => e.DateHeure)
                    .ToList();

                var result = new List<EntrainementViewModelAvecPosition>();
                var groupedByDay = inWeek.GroupBy(e => ((int)e.DateHeure.DayOfWeek + 6) % 7);

                foreach (var dayGroup in groupedByDay)
                {
                    var dayList = dayGroup.ToList();

                    foreach (var entr in dayList)
                    {
                        // Gestion des collisions : regrouper ceux qui ont lieu à la même heure
                        var collisions = dayList.Where(other =>
                            entr.DateHeure < other.DateHeure.AddMinutes(other.DureeMinutes) &&
                            entr.DateHeure.AddMinutes(entr.DureeMinutes) > other.DateHeure).ToList();

                        int totalCount = collisions.Count;
                        
                        int currentIdx = collisions.IndexOf(entr);

                        result.Add(new EntrainementViewModelAvecPosition(
                            entr, _debutSemaine, HEURE_DEBUT, HAUTEUR_HEURE,
                            largeurUneColonne, currentIdx, totalCount));
                    }
                }
                return result;
            }
        }

        public IEnumerable<EntrainementViewModel> EntrainementsOrdonnes => Entrainements.OrderBy(e => e.DateHeure);

        // --- Navigation temporelle ---
        public ICommand SemainePrecedenteCommand => new RelayCommand(() => ChangerSemaine(-7));
        public ICommand SemaineSuivanteCommand => new RelayCommand(() => ChangerSemaine(7));
        public ICommand AujourdhuiCommand => new RelayCommand(() => { _debutSemaine = GetDebutSemaine(DateTime.Now); RafraichirTout(); });

        private void ChangerSemaine(int jours)
        {
            _debutSemaine = _debutSemaine.AddDays(jours);
            RafraichirTout();
        }

        private void RafraichirTout()
        {
            OnPropertyChanged(nameof(SemaineAffichee), nameof(EntrainementsSemaine));
            OnPropertyChanged(nameof(DateLundi), nameof(DateMardi), nameof(DateMercredi), nameof(DateJeudi), nameof(DateVendredi), nameof(DateSamedi), nameof(DateDimanche));
            OnPropertyChanged(nameof(EstAujourdhuiLundi), nameof(EstAujourdhuiMardi), nameof(EstAujourdhuiMercredi), nameof(EstAujourdhuiJeudi), nameof(EstAujourdhuiVendredi), nameof(EstAujourdhuiSamedi), nameof(EstAujourdhuiDimanche));
        }

        // --- Gestion des Entraînements (CRUD) ---

      
        /// Prépare un nouvel entraînement sans l'ajouter immédiatement à la base.

        public ICommand AjouterEntrainementCommand => new RelayCommand(() => {
            var dateDefaut = (DateTime.Today >= _debutSemaine && DateTime.Today < _debutSemaine.AddDays(7)) ? DateTime.Today : _debutSemaine;
            var e = new Entrainement
            {
                Activite = "Nouvel entraînement",
                DateHeure = dateDefaut.Date.AddHours(18),
                Lieu = "Gymnase",
                DureeMinutes = 60
            };
            SelectionEntrainement = new EntrainementViewModel(e);
        });

       
        /// Valide la création et l'ajoute officiellement au contexte et à la collection.
     
        public ICommand ValiderAjoutCommand => new RelayCommand(() => {
            if (SelectionEntrainement == null) return;

            if (ClampDurationToMidnight(SelectionEntrainement)) _view.AlerteDepassementMinuit();

            var modele = new Entrainement
            {
                Activite = SelectionEntrainement.Activite,
                DateHeure = SelectionEntrainement.DateHeure,
                Lieu = SelectionEntrainement.Lieu,
                DureeMinutes = SelectionEntrainement.DureeMinutes
            };

            _context.Entrainements.Add(modele);
            Entrainements.Add(new EntrainementViewModel(modele));
            SelectionEntrainement = null;
            _view.InformationAjout();
        });

        public bool ValiderAjoutActif => SelectionEntrainement != null && !Entrainements.Contains(SelectionEntrainement);

        public ICommand TerminerEditionCommand => new RelayCommand(() => {
            if (SelectionEntrainement != null && Entrainements.Contains(SelectionEntrainement))
            {
                if (ClampDurationToMidnight(SelectionEntrainement)) _view.AlerteDepassementMinuit();
                _context.SaveChanges();
            }
            SelectionEntrainement = null;
            NotifierChangementPlanning();
        });

        public ICommand SupprimerEntrainementCommand => new RelayCommand(() => {
            if (SelectionEntrainement == null || !_view.ConfirmationSuppression()) return;

            var modele = SelectionEntrainement.Modele;
            if (modele.Id != 0)
            {
                _context.Entrainements.Remove(modele);
                _context.SaveChanges();
            }
            Entrainements.Remove(SelectionEntrainement);
            SelectionEntrainement = null;
        });

        public bool SupprimerEntrainementActif => _selectionEntrainement != null;

        // --- Déplacement rapide ---
        public ICommand DeplacerEntrainementPlusTardCommand => new RelayCommand(() => ModifierHeure(1));
        public ICommand DeplacerEntrainementPlusTotCommand => new RelayCommand(() => ModifierHeure(-1));

        private void ModifierHeure(int delta)
        {
            if (SelectionEntrainement == null) return;
            SelectionEntrainement.DateHeure = SelectionEntrainement.DateHeure.AddHours(delta);
            if (ClampDurationToMidnight(SelectionEntrainement)) _view.AlerteDepassementMinuit();
            NotifierChangementPlanning();
        }

        public ICommand SelectionnerEntrainementCommand => new RelayCommand<EntrainementViewModelAvecPosition>(e => {
            if (e != null) SelectionEntrainement = e.Entrainement;
        });

        public void Enregistrer() => _context.SaveChanges();

        public void NotifierChangementPlanning()
        {
            OnPropertyChanged(nameof(EntrainementsSemaine), nameof(EntrainementsOrdonnes));
        }

       
        /// Force la durée à se terminer au plus tard à 23:59.
       
        private bool ClampDurationToMidnight(EntrainementViewModel vm)
        {
            var minuit = vm.DateHeure.Date.AddDays(1);
            int minutesMax = (int)Math.Max(0, (minuit - vm.DateHeure).TotalMinutes);

            if (vm.DureeMinutes > minutesMax)
            {
                vm.DureeMinutes = minutesMax;
                return true;
            }
            return false;
        }
    }
}