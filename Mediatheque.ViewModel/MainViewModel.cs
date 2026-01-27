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
    /// <summary>
    /// Interface définissant les interactions UI déclenchées par le ViewModel.
    /// </summary>
    public interface IMainView
    {
        void InformationAjout();
        bool ConfirmationSuppression();
        void AlerteDepassementMinuit();

        // Ajout d'une méthode pour notifier que l'entraînement ne peut pas commencer avant l'heure de début (06:00).
        // Fournit une implémentation par défaut (no-op) pour ne pas casser les implémentations existantes.
        void AlerteAvantHeureDebut()
        {
            // Implémentation par défaut : ne rien faire.
            // La Vue (MainWindow) peut redéfinir cette méthode pour afficher un popup.
        }
    }

    /// <summary>
    /// ViewModel principal gérant la logique métier du calendrier et les opérations CRUD.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly MediathequeContext _context;
        private readonly IMainView _view;

        private DateTime _debutSemaine;
        private const int HEURE_DEBUT = 6;
        private const int HAUTEUR_HEURE = 60;
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
                    OnPropertyChanged(nameof(EntrainementsSemaine));
                }
            }
        }

        // --- Détection du jour actuel ---
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

        // --- Propriétés de gestion des dates ---
        public string SemaineAffichee => $"Semaine du {_debutSemaine:dd/MM/yyyy} au {_debutSemaine.AddDays(6):dd/MM/yyyy}";
        public DateTime DateLundi => _debutSemaine;
        public DateTime DateMardi => _debutSemaine.AddDays(1);
        public DateTime DateMercredi => _debutSemaine.AddDays(2);
        public DateTime DateJeudi => _debutSemaine.AddDays(3);
        public DateTime DateVendredi => _debutSemaine.AddDays(4);
        public DateTime DateSamedi => _debutSemaine.AddDays(5);
        public DateTime DateDimanche => _debutSemaine.AddDays(6);

       
        /// Calcule les positions graphiques des entraînements en gérant les collisions par intersection de plages horaires.
    
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
                        // Détection de collision : deux entraînements se chevauchent si l'un commence avant que l'autre ne finisse.
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

      
        /// Initialise un nouvel objet d'entraînement temporaire pour l'édition.
    
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

       
        /// Enregistre l'entraînement créé dans la base de données et l'ajoute à la vue.
     
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

        
        /// Modifie l'heure de début tout en respectant les limites de la plage horaire affichée.
       
        private void ModifierHeure(int delta)
        {
            if (SelectionEntrainement == null) return;

            var currentStart = SelectionEntrainement.DateHeure;
            var newStart = currentStart.AddHours(delta);

            var dayStartLimit = newStart.Date.AddHours(HEURE_DEBUT);
            var dayEndLimit = newStart.Date.AddDays(1);
            var durationMinutes = SelectionEntrainement.DureeMinutes;
            var maxAllowedStart = dayEndLimit.AddMinutes(-durationMinutes);

            if (dayStartLimit > maxAllowedStart)
            {
                SelectionEntrainement.DureeMinutes = Math.Max(0, (int)(dayEndLimit - dayStartLimit).TotalMinutes);
                SelectionEntrainement.DateHeure = dayStartLimit;
                _view.AlerteDepassementMinuit();
                NotifierChangementPlanning();
                return;
            }

            // Si on essaie de déplacer avant 06:00, afficher un popup via la vue.
            if (newStart < dayStartLimit)
            {
                _view.AlerteAvantHeureDebut();
                return;
            }

            if (newStart > maxAllowedStart)
            {
                _view.AlerteDepassementMinuit();
                return;
            }

            SelectionEntrainement.DateHeure = newStart;
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

       
        /// Force la durée à se terminer au plus tard à minuit.
      
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