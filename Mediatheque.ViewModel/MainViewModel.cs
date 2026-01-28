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
    /// Contrat minimal entre le ViewModel et la vue.
    /// Permet de déclencher des interactions UI (messages, confirmations)
    /// sans créer de dépendance directe vers WPF.
    /// </summary>
    public interface IMainView
    {
        void InformationAjout();
        bool ConfirmationSuppression();
        void AlerteDepassementMinuit();

        /// <summary>
        /// Alerte lorsque l’heure de début est antérieure à l’ouverture du planning.
        /// Implémentation optionnelle pour éviter d’imposer une méthode à la vue.
        /// </summary>
        void AlerteAvantHeureDebut()
        {
        }
    }

    /// <summary>
    /// ViewModel principal de l’application.
    /// Centralise :
    /// - la logique métier du planning hebdomadaire
    /// - la navigation temporelle
    /// - la gestion des entraînements (CRUD)
    /// - le calcul des données nécessaires à l’affichage graphique
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly MediathequeContext _context;
        private readonly IMainView _view;

        /// <summary>
        /// Lundi de la semaine actuellement affichée.
        /// Sert de référence pour tous les calculs de dates.
        /// </summary>
        private DateTime _debutSemaine;

        /// <summary>
        /// Paramètres fixes du planning graphique.
        /// </summary>
        private const int HEURE_DEBUT = 6;
        private const int HAUTEUR_HEURE = 60;
        private const double LARGEUR_COLONNE_BASE = 95;

        /// <summary>
        /// Liste observable des entraînements.
        /// Toute modification est automatiquement reflétée dans la vue.
        /// </summary>
        public ObservableCollection<EntrainementViewModel> Entrainements { get; }
            = new ObservableCollection<EntrainementViewModel>();

        private double _largeurTotalePlanning;

        /// <summary>
        /// Largeur réelle du planning, fournie dynamiquement par la vue.
        /// Utilisée pour adapter le rendu graphique lors des redimensionnements.
        /// </summary>
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

        /// <summary>
        /// Catégories d’activités disponibles (chargées depuis la base).
        /// </summary>
        public ObservableCollection<CategorieActivite> Categories { get; } = new();

        #region Détection du jour courant

        /// <summary>
        /// Propriétés utilitaires permettant de mettre en évidence
        /// le jour courant dans l’interface (ex : surbrillance).
        /// </summary>
        public bool EstAujourdhuiLundi => DateTime.Today == DateLundi.Date;
        public bool EstAujourdhuiMardi => DateTime.Today == DateMardi.Date;
        public bool EstAujourdhuiMercredi => DateTime.Today == DateMercredi.Date;
        public bool EstAujourdhuiJeudi => DateTime.Today == DateJeudi.Date;
        public bool EstAujourdhuiVendredi => DateTime.Today == DateVendredi.Date;
        public bool EstAujourdhuiSamedi => DateTime.Today == DateSamedi.Date;
        public bool EstAujourdhuiDimanche => DateTime.Today == DateDimanche.Date;

        #endregion

        private EntrainementViewModel? _selectionEntrainement;

        /// <summary>
        /// Indique si l’utilisateur est en train d’éditer
        /// un entraînement déjà existant.
        /// </summary>
        public bool EstEnModeEdition =>
            SelectionEntrainement != null &&
            Entrainements.Contains(SelectionEntrainement);

        /// <summary>
        /// Entraînement actuellement sélectionné dans l’interface.
        /// Sert de point d’entrée pour toutes les actions utilisateur.
        /// </summary>
        public EntrainementViewModel? SelectionEntrainement
        {
            get => _selectionEntrainement;
            set
            {
                _selectionEntrainement = value;
                OnPropertyChanged(nameof(SelectionEntrainement));
                OnPropertyChanged(nameof(ValiderAjoutActif));
                OnPropertyChanged(nameof(EstEnModeEdition));
                OnPropertyChanged(nameof(SupprimerEntrainementActif));
            }
        }

        public MainViewModel(MediathequeContext context, IMainView view)
        {
            _context = context;
            _view = view;
            _debutSemaine = GetDebutSemaine(DateTime.Now);

            // Chargement initial des catégories depuis la base
            Categories.Clear();
            foreach (var c in _context.Categories)
                Categories.Add(c);

            // Chargement des entraînements avec leurs relations
            var liste = _context.Entrainements
                .Include(e => e.Categorie)
                .ToList();

            foreach (var e in liste)
                Entrainements.Add(new EntrainementViewModel(e));

            // Toute modification de la collection implique un recalcul du planning
            Entrainements.CollectionChanged += (_, __) =>
                NotifierChangementPlanning();
        }

        /// <summary>
        /// Calcule le lundi correspondant à la semaine d’une date donnée.
        /// Garantit une navigation cohérente par semaines complètes.
        /// </summary>
        private DateTime GetDebutSemaine(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        #region Gestion des dates affichées

        /// <summary>
        /// Libellé textuel de la semaine affichée.
        /// </summary>
        public string SemaineAffichee =>
            $"Semaine du {_debutSemaine:dd/MM/yyyy} au {_debutSemaine.AddDays(6):dd/MM/yyyy}";

        /// <summary>
        /// Dates correspondant à chaque colonne du planning.
        /// </summary>
        public DateTime DateLundi => _debutSemaine;
        public DateTime DateMardi => _debutSemaine.AddDays(1);
        public DateTime DateMercredi => _debutSemaine.AddDays(2);
        public DateTime DateJeudi => _debutSemaine.AddDays(3);
        public DateTime DateVendredi => _debutSemaine.AddDays(4);
        public DateTime DateSamedi => _debutSemaine.AddDays(5);
        public DateTime DateDimanche => _debutSemaine.AddDays(6);

        #endregion

        /// <summary>
        /// Génère les ViewModels enrichis pour l’affichage graphique.
        /// Calcule :
        /// - la position verticale (heure)
        /// - la position horizontale (jour + chevauchements)
        /// - la taille des blocs
        /// </summary>
        public IEnumerable<EntrainementViewModelAvecPosition> EntrainementsSemaine
        {
            get
            {
                double largeurUneColonne =
                    LargeurTotalePlanning > 0
                        ? LargeurTotalePlanning / 7
                        : LARGEUR_COLONNE_BASE;

                var finSemaine = _debutSemaine.AddDays(7);

                var inWeek = Entrainements
                    .Where(e => e.DateHeure >= _debutSemaine && e.DateHeure < finSemaine)
                    .OrderBy(e => e.DateHeure)
                    .ToList();

                var result = new List<EntrainementViewModelAvecPosition>();

                // Regroupement par jour afin de gérer les collisions horaires
                var groupedByDay =
                    inWeek.GroupBy(e => ((int)e.DateHeure.DayOfWeek + 6) % 7);

                foreach (var dayGroup in groupedByDay)
                {
                    var dayList = dayGroup.ToList();

                    foreach (var entr in dayList)
                    {
                        // Détection des séances qui se chevauchent temporellement
                        var collisions = dayList.Where(other =>
                            entr.DateHeure < other.DateHeure.AddMinutes(other.DureeMinutes) &&
                            entr.DateHeure.AddMinutes(entr.DureeMinutes) > other.DateHeure)
                            .ToList();

                        int totalCount = collisions.Count;
                        int currentIdx = collisions.IndexOf(entr);

                        result.Add(new EntrainementViewModelAvecPosition(
                            entr,
                            _debutSemaine,
                            HEURE_DEBUT,
                            HAUTEUR_HEURE,
                            largeurUneColonne,
                            currentIdx,
                            totalCount));
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Entraînements triés chronologiquement,
        /// utilisés pour les listes textuelles.
        /// </summary>
        public IEnumerable<EntrainementViewModel> EntrainementsOrdonnes =>
            Entrainements.OrderBy(e => e.DateHeure);

        #region Navigation temporelle
       
        public ICommand SemainePrecedenteCommand =>
            new RelayCommand(() => ChangerSemaine(-7));

        public ICommand SemaineSuivanteCommand =>
            new RelayCommand(() => ChangerSemaine(7));

        public ICommand AujourdhuiCommand =>
            new RelayCommand(() =>
            {
                _debutSemaine = GetDebutSemaine(DateTime.Now);
                RafraichirTout();
            });

        private void ChangerSemaine(int jours)
        {
            _debutSemaine = _debutSemaine.AddDays(jours);
            RafraichirTout();
        }

        private void RafraichirTout()
        {
            OnPropertyChanged(nameof(SemaineAffichee), nameof(EntrainementsSemaine));
            OnPropertyChanged(
                nameof(DateLundi), nameof(DateMardi), nameof(DateMercredi),
                nameof(DateJeudi), nameof(DateVendredi),
                nameof(DateSamedi), nameof(DateDimanche));
            OnPropertyChanged(
                nameof(EstAujourdhuiLundi), nameof(EstAujourdhuiMardi),
                nameof(EstAujourdhuiMercredi), nameof(EstAujourdhuiJeudi),
                nameof(EstAujourdhuiVendredi), nameof(EstAujourdhuiSamedi),
                nameof(EstAujourdhuiDimanche));
        }

        #endregion

        #region CRUD Entraînements

        public ICommand AjouterEntrainementCommand => new RelayCommand(() =>
        {
            DateTime jourBase =
                (DateTime.Now >= _debutSemaine &&
                 DateTime.Now < _debutSemaine.AddDays(7))
                ? DateTime.Today
                : _debutSemaine;

            DateTime dateHeureFixe = jourBase.Date.AddHours(18);

            var e = new Entrainement
            {
                Activite = "Nouvelle séance",
                DateHeure = dateHeureFixe,
                Lieu = "Gymnase",
                DureeMinutes = 60,
                Categorie = Categories.FirstOrDefault()
            };

            if (e.Categorie != null)
                e.CategorieActiviteId = e.Categorie.Id;

            SelectionEntrainement = new EntrainementViewModel(e);
        });

        public ICommand ValiderAjoutCommand => new RelayCommand(() =>
        {
            if (SelectionEntrainement == null) return;

            if (ClampDurationToMidnight(SelectionEntrainement))
                _view.AlerteDepassementMinuit();

            var modele = SelectionEntrainement.Modele;
            modele.Id = 0;

            _context.Entrainements.Add(modele);
            _context.SaveChanges();

            Entrainements.Add(SelectionEntrainement);
            SelectionEntrainement = null;
            _view.InformationAjout();
        });

        public bool ValiderAjoutActif =>
            SelectionEntrainement != null &&
            !Entrainements.Contains(SelectionEntrainement);

        public ICommand TerminerEditionCommand => new RelayCommand(() =>
        {
            if (SelectionEntrainement != null &&
                Entrainements.Contains(SelectionEntrainement))
            {
                if (ClampDurationToMidnight(SelectionEntrainement))
                    _view.AlerteDepassementMinuit();

                _context.SaveChanges();
            }

            SelectionEntrainement = null;
            NotifierChangementPlanning();
        });

        public ICommand SupprimerEntrainementCommand => new RelayCommand(() =>
        {
            if (SelectionEntrainement == null ||
                !_view.ConfirmationSuppression())
                return;

            var vmASupprimer = SelectionEntrainement;
            var modele = vmASupprimer.Modele;

            try
            {
                if (modele.Id != 0)
                {
                    var entityInDb =
                        _context.Entrainements.Find(modele.Id);

                    if (entityInDb != null)
                    {
                        _context.Entrainements.Remove(entityInDb);
                        _context.SaveChanges();
                    }
                }

                Entrainements.Remove(vmASupprimer);
                SelectionEntrainement = null;
                NotifierChangementPlanning();
            }
            catch (DbUpdateConcurrencyException)
            {
                Entrainements.Remove(vmASupprimer);
                SelectionEntrainement = null;
                NotifierChangementPlanning();
            }
        });

        public bool SupprimerEntrainementActif => EstEnModeEdition;

        #endregion

        #region Déplacement rapide

        public ICommand DeplacerEntrainementPlusTardCommand =>
            new RelayCommand(() => ModifierHeure(1));

        public ICommand DeplacerEntrainementPlusTotCommand =>
            new RelayCommand(() => ModifierHeure(-1));

        /// <summary>
        /// Décale l’heure de début tout en respectant
        /// les limites horaires (ouverture / minuit).
        /// </summary>
        private void ModifierHeure(int delta)
        {
            if (SelectionEntrainement == null) return;

            DateTime nouveauDebut =
                SelectionEntrainement.DateHeure.AddHours(delta);

            DateTime jourOrigine =
                SelectionEntrainement.DateHeure.Date;

            if (nouveauDebut < jourOrigine.AddHours(HEURE_DEBUT))
            {
                _view.AlerteAvantHeureDebut();
                return;
            }

            if (nouveauDebut.AddMinutes(SelectionEntrainement.DureeMinutes)
                > jourOrigine.AddDays(1))
            {
                _view.AlerteDepassementMinuit();
                return;
            }

            SelectionEntrainement.DateHeure = nouveauDebut;
            NotifierChangementPlanning();
        }

        #endregion

        public ICommand SelectionnerEntrainementCommand =>
            new RelayCommand<EntrainementViewModelAvecPosition>(e =>
            {
                if (e != null)
                {
                    var m = e.Entrainement.Modele;

                    _donneesAvantEdition = new Entrainement
                    {
                        Activite = m.Activite,
                        Lieu = m.Lieu,
                        DateHeure = m.DateHeure,
                        DureeMinutes = m.DureeMinutes,
                        CategorieActiviteId = m.CategorieActiviteId,
                        Categorie = m.Categorie
                    };

                    SelectionEntrainement = e.Entrainement;
                }
            });

        public void Enregistrer() => _context.SaveChanges();

        public void NotifierChangementPlanning()
        {
            OnPropertyChanged(
                nameof(EntrainementsSemaine),
                nameof(EntrainementsOrdonnes));
        }

        /// <summary>
        /// Ajuste la durée pour garantir une fin au plus tard à minuit.
        /// </summary>
        private bool ClampDurationToMidnight(EntrainementViewModel vm)
        {
            var minuit = vm.DateHeure.Date.AddDays(1);
            int minutesMax =
                (int)Math.Max(0, (minuit - vm.DateHeure).TotalMinutes);

            if (vm.DureeMinutes > minutesMax)
            {
                vm.DureeMinutes = minutesMax;
                return true;
            }
            return false;
        }

        public ICommand AnnulerCommande => new RelayCommand(() =>
        {
            if (SelectionEntrainement != null &&
                _donneesAvantEdition != null)
            {
                var m = SelectionEntrainement.Modele;

                m.Activite = _donneesAvantEdition.Activite;
                m.Lieu = _donneesAvantEdition.Lieu;
                m.DateHeure = _donneesAvantEdition.DateHeure;
                m.DureeMinutes = _donneesAvantEdition.DureeMinutes;
                m.CategorieActiviteId = _donneesAvantEdition.CategorieActiviteId;
                m.Categorie = _donneesAvantEdition.Categorie;

                SelectionEntrainement.NotifierTout();
            }

            SelectionEntrainement = null;
            NotifierChangementPlanning();
        });

        private Entrainement _donneesAvantEdition;
    }
}
