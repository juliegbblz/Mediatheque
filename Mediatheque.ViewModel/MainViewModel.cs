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

      
        void AlerteAvantHeureDebut()
        {
            // Implémentation par défaut : ne rien faire.
          
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

        public ObservableCollection<CategorieActivite> Categories { get; } = new();

  
        // --- Détection du jour actuel ---
        public bool EstAujourdhuiLundi => DateTime.Today == DateLundi.Date;
        public bool EstAujourdhuiMardi => DateTime.Today == DateMardi.Date;
        public bool EstAujourdhuiMercredi => DateTime.Today == DateMercredi.Date;
        public bool EstAujourdhuiJeudi => DateTime.Today == DateJeudi.Date;
        public bool EstAujourdhuiVendredi => DateTime.Today == DateVendredi.Date;
        public bool EstAujourdhuiSamedi => DateTime.Today == DateSamedi.Date;
        public bool EstAujourdhuiDimanche => DateTime.Today == DateDimanche.Date;

        private EntrainementViewModel? _selectionEntrainement;

        public bool EstEnModeEdition => SelectionEntrainement != null && Entrainements.Contains(SelectionEntrainement);
        public EntrainementViewModel? SelectionEntrainement
        {
            get => _selectionEntrainement;
            set
            {
                _selectionEntrainement = value;
                OnPropertyChanged(nameof(SelectionEntrainement));
                OnPropertyChanged(nameof(ValiderAjoutActif));
                OnPropertyChanged(nameof(EstEnModeEdition));
                // On notifie que le bouton supprimer doit aussi changer d'état
                OnPropertyChanged(nameof(SupprimerEntrainementActif));
            }
        }

        public MainViewModel(MediathequeContext context, IMainView view)
        {
            _context = context;
            _view = view;
            _debutSemaine = GetDebutSemaine(DateTime.Now);

            // 1. Charger les catégories d'abord
            Categories.Clear();
            foreach (var c in _context.Categories) Categories.Add(c);

            // 2. Charger les entraînements AVEC leur catégorie (le .Include est vital)
            var liste = _context.Entrainements.Include(e => e.Categorie).ToList();
            foreach (var e in liste)
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
            // On prend la date du jour (ou du début de semaine si on est ailleurs)
            DateTime jourBase = (DateTime.Now >= _debutSemaine && DateTime.Now < _debutSemaine.AddDays(7))
                                ? DateTime.Today
                                : _debutSemaine;

            // On force 18h systématiquement pour le nouveau bouton
            DateTime dateHeureFixe = jourBase.Date.AddHours(18);

            var e = new Entrainement
            {
                Activite = "Nouvelle séance",
                DateHeure = dateHeureFixe,
                Lieu = "Gymnase",
                DureeMinutes = 60,
                Categorie = Categories.FirstOrDefault()
            };

            if (e.Categorie != null) e.CategorieActiviteId = e.Categorie.Id;
            SelectionEntrainement = new EntrainementViewModel(e);
        }); 


        /// Enregistre l'entraînement créé dans la base de données et l'ajoute à la vue.

        public ICommand ValiderAjoutCommand => new RelayCommand(() => {
            if (SelectionEntrainement == null) return;

            if (ClampDurationToMidnight(SelectionEntrainement)) _view.AlerteDepassementMinuit();

            // On récupère le modèle qui est DÉJÀ dans le SelectionEntrainement
            var modele = SelectionEntrainement.Modele;

            modele.Id = 0;
            _context.Entrainements.Add(modele);
            _context.SaveChanges(); // On sauvegarde tout de suite pour avoir l'ID

            Entrainements.Add(SelectionEntrainement); // On ajoute le VM existant
            SelectionEntrainement = null;
            _view.InformationAjout();
        });

        public bool ValiderAjoutActif
        {
            get
            {
                // Le bouton "Valider l'ajout" doit être visible si :
                // 1. On a une sélection
                // 2. ET que cette sélection n'existe pas encore dans la liste officielle
                return SelectionEntrainement != null && !Entrainements.Contains(SelectionEntrainement);
            }
        }

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
            // 1. Vérification de sécurité
            if (SelectionEntrainement == null || !_view.ConfirmationSuppression()) return;

            var vmASupprimer = SelectionEntrainement;
            var modele = vmASupprimer.Modele;

            try
            {
                // 2. Si l'objet existe en base de données (Id != 0)
                if (modele.Id != 0)
                {
                    // On récupère l'entité fraîchement depuis le contexte pour être sûr qu'elle existe
                    var entityInDb = _context.Entrainements.Find(modele.Id);

                    if (entityInDb != null)
                    {
                        _context.Entrainements.Remove(entityInDb);
                        _context.SaveChanges();
                    }
                }

                // 3. Mise à jour de l'interface (UI)
                Entrainements.Remove(vmASupprimer);
                SelectionEntrainement = null;

                // Notifier la vue pour rafraîchir le planning graphique
                NotifierChangementPlanning();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Si l'erreur arrive quand même (ex: supprimé par un autre processus entre temps)
                // On retire simplement l'élément de la liste locale pour synchroniser l'affichage
                Entrainements.Remove(vmASupprimer);
                SelectionEntrainement = null;
                NotifierChangementPlanning();
            }
        });

        public bool SupprimerEntrainementActif => EstEnModeEdition;

        // --- Déplacement rapide ---
        public ICommand DeplacerEntrainementPlusTardCommand => new RelayCommand(() => ModifierHeure(1));
        public ICommand DeplacerEntrainementPlusTotCommand => new RelayCommand(() => ModifierHeure(-1));


        /// Modifie l'heure de début tout en respectant les limites de la plage horaire affichée.

        private void ModifierHeure(int delta)
        {
            if (SelectionEntrainement == null) return;

            // 1. Calculer le nouveau début potentiel
            DateTime nouveauDebut = SelectionEntrainement.DateHeure.AddHours(delta);

            // 2. Récupérer la date d'origine (le jour où la séance a commencé)
            DateTime jourOrigine = SelectionEntrainement.DateHeure.Date;

            // --- CAS A : On essaie de passer au lendemain (Heure >= 24:00) ---
            if (nouveauDebut.Date > jourOrigine)
            {
                _view.AlerteDepassementMinuit();
                return;
            }

            // --- CAS B : On essaie de rester sur le même jour mais avant 06:00 ---
            DateTime limiteOuverture = jourOrigine.AddHours(HEURE_DEBUT);
            if (nouveauDebut < limiteOuverture)
            {
                _view.AlerteAvantHeureDebut();
                return;
            }

            // 3. Si on passe les tests, on applique le changement
            SelectionEntrainement.DateHeure = nouveauDebut;

            // 4. Vérifier si la DURÉE (ex: 90min à 23h) fait déborder après minuit
            if (ClampDurationToMidnight(SelectionEntrainement))
            {
                _view.AlerteDepassementMinuit();
            }

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

        public ICommand AnnulerCommande => new RelayCommand(() =>
        {
            SelectionEntrainement = null;
            // On notifie le planning pour qu'il recalcule les positions 
            // et reprenne toute la largeur des colonnes
            NotifierChangementPlanning();
        });
    }
}