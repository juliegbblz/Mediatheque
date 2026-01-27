using Mediatheque.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Mediatheque.ViewModel
{
    public interface IMainView
    {
        void InformationAjout();

        bool ConfirmationSuppression();
    }

    public class MainViewModel : ViewModelBase
    {
        private readonly MediathequeContext _context;
        private readonly IMainView _view;

        private DateTime _debutSemaine;
        private const int HEURE_DEBUT = 6; // 06:00
        private const int HAUTEUR_HEURE = 60; // 60 pixels par heure
        private const double LARGEUR_COLONNE_BASE = 95; // Largeur réelle d'une colonne (~94.17 pixels)

        public ObservableCollection<EntrainementViewModel> Entrainements { get; }
            = new ObservableCollection<EntrainementViewModel>();


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
                    // On force le recalcul des positions des rectangles
                    OnPropertyChanged(nameof(EntrainementsSemaine));
                }
            }
        }

        // Propriétés pour détecter le jour actuel
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

            // Initialiser à la semaine actuelle (lundi)
            _debutSemaine = GetDebutSemaine(DateTime.Now);

            foreach (var e in _context.Entrainements)
            {
                Entrainements.Add(new EntrainementViewModel(e));
            }

            // S'abonner aux changements pour mettre à jour l'affichage
            Entrainements.CollectionChanged += (s, e) => OnPropertyChanged(
                nameof(EntrainementsOrdonnes),
                nameof(EntrainementsSemaine));
        }

        // Obtenir le lundi de la semaine contenant la date donnée
        private DateTime GetDebutSemaine(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        // --- Propriétés pour l'affichage de la semaine ---

        public string SemaineAffichee =>
            $"Semaine du {_debutSemaine:dd/MM/yyyy} au {_debutSemaine.AddDays(6):dd/MM/yyyy}";

        public DateTime DateLundi => _debutSemaine;
        public DateTime DateMardi => _debutSemaine.AddDays(1);
        public DateTime DateMercredi => _debutSemaine.AddDays(2);
        public DateTime DateJeudi => _debutSemaine.AddDays(3);
        public DateTime DateVendredi => _debutSemaine.AddDays(4);
        public DateTime DateSamedi => _debutSemaine.AddDays(5);
        public DateTime DateDimanche => _debutSemaine.AddDays(6);

        // Entraînements de la semaine avec positionnement
        public IEnumerable<EntrainementViewModelAvecPosition> EntrainementsSemaine
        {
            get
            {

                double largeurUneColonne = LargeurTotalePlanning > 0
                    ? (LargeurTotalePlanning / 7)
                    : LARGEUR_COLONNE_BASE;

                var finSemaine = _debutSemaine.AddDays(7);
                var inWeek = Entrainements
                    .Where(e => e.DateHeure >= _debutSemaine && e.DateHeure < finSemaine)
                    .OrderBy(e => e.DateHeure)
                    .ToList();

                var result = new List<EntrainementViewModelAvecPosition>();

                var groupedByDay = inWeek.GroupBy(e => ((int)e.DateHeure.DayOfWeek + 6) % 7);

                foreach (var dayGroup in groupedByDay)
                {
                    var sameStartGroups = dayGroup.GroupBy(e => e.DateHeure).ToList();

                    foreach (var group in sameStartGroups)
                    {
                        int totalCount = group.Count();
                        int currentIdx = 0;

                        foreach (var entr in group)
                        {
                            result.Add(new EntrainementViewModelAvecPosition(
                                entr,
                                _debutSemaine,
                                HEURE_DEBUT,
                                HAUTEUR_HEURE,
                                largeurUneColonne,         
                                colonneIndex: currentIdx,
                                colonnesTotal: totalCount));

                            currentIdx++;
                        }
                    }
                }

                return result;
            }
        }


        // Tous les entraînements ordonnés par date
        public IEnumerable<EntrainementViewModel> EntrainementsOrdonnes =>
            Entrainements.OrderBy(e => e.DateHeure);

        // --- Commandes de navigation de semaine ---

        public ICommand SemainePrecedenteCommand => new RelayCommand(SemainePrecedente);

        public void SemainePrecedente()
        {
            _debutSemaine = _debutSemaine.AddDays(-7);
            OnPropertyChanged(
                nameof(SemaineAffichee),
                nameof(DateLundi), nameof(DateMardi), nameof(DateMercredi),
                nameof(DateJeudi), nameof(DateVendredi), nameof(DateSamedi), nameof(DateDimanche),
                nameof(EstAujourdhuiLundi), nameof(EstAujourdhuiMardi), nameof(EstAujourdhuiMercredi),
                nameof(EstAujourdhuiJeudi), nameof(EstAujourdhuiVendredi), nameof(EstAujourdhuiSamedi), nameof(EstAujourdhuiDimanche),
                nameof(EntrainementsSemaine));
        }

        public ICommand SemaineSuivanteCommand => new RelayCommand(SemaineSuivante);

        public void SemaineSuivante()
        {
            _debutSemaine = _debutSemaine.AddDays(7);
            OnPropertyChanged(
                nameof(SemaineAffichee),
                nameof(DateLundi), nameof(DateMardi), nameof(DateMercredi),
                nameof(DateJeudi), nameof(DateVendredi), nameof(DateSamedi), nameof(DateDimanche),
                nameof(EstAujourdhuiLundi), nameof(EstAujourdhuiMardi), nameof(EstAujourdhuiMercredi),
                nameof(EstAujourdhuiJeudi), nameof(EstAujourdhuiVendredi), nameof(EstAujourdhuiSamedi), nameof(EstAujourdhuiDimanche),
                nameof(EntrainementsSemaine));
        }

        public ICommand AujourdhuiCommand => new RelayCommand(Aujourdhui);

        public void Aujourdhui()
        {
            _debutSemaine = GetDebutSemaine(DateTime.Now);
            OnPropertyChanged(
                nameof(SemaineAffichee),
                nameof(DateLundi), nameof(DateMardi), nameof(DateMercredi),
                nameof(DateJeudi), nameof(DateVendredi), nameof(DateSamedi), nameof(DateDimanche),
                nameof(EstAujourdhuiLundi), nameof(EstAujourdhuiMardi), nameof(EstAujourdhuiMercredi),
                nameof(EstAujourdhuiJeudi), nameof(EstAujourdhuiVendredi), nameof(EstAujourdhuiSamedi), nameof(EstAujourdhuiDimanche),
                nameof(EntrainementsSemaine));
        }

        // --- Entrainements : ajout / suppression / deplacement ---

        public ICommand AjouterEntrainementCommand => new RelayCommand(AjouterEntrainement);

        public void AjouterEntrainement()
        {
            // Utiliser le lundi de la semaine affichée pour créer l'entraînement aujourd'hui ou dans la semaine affichée
            var dateDefaut = DateTime.Today >= _debutSemaine && DateTime.Today < _debutSemaine.AddDays(7) 
                ? DateTime.Today 
                : _debutSemaine;

            var maintenantUtc = DateTime.SpecifyKind(dateDefaut.Date.AddHours(18), DateTimeKind.Utc);

            var e = new Entrainement
            {
                Activite = "Nouvel entraînement",
                DateHeure = new DateTime(dateDefaut.Year, dateDefaut.Month, dateDefaut.Day, 18, 0, 0),
                Lieu = "Gymnase",
                DureeMinutes = 60
            };

            // NE PAS ajouter à la base de données ni à la collection
            // Juste créer un ViewModel temporaire pour l'édition
            var viewModel = new EntrainementViewModel(e);
            SelectionEntrainement = viewModel;
        }

        public ICommand ValiderAjoutCommand => new RelayCommand(ValiderAjout);

        public void ValiderAjout()
        {
            if (SelectionEntrainement == null) return;

            // 1. CRÉER UN NOUVEAU modèle avec les valeurs du temporaire
            var nouvelEntrainement = new Entrainement
            {
                Activite = SelectionEntrainement.Activite,
                DateHeure = SelectionEntrainement.DateHeure,
                Lieu = SelectionEntrainement.Lieu,
                DureeMinutes = SelectionEntrainement.DureeMinutes
            };

            // 2. Ajouter le NOUVEAU à la base
            _context.Entrainements.Add(nouvelEntrainement);

            // 3. Créer un NOUVEAU ViewModel pour la collection (pas le temporaire !)
            var nouveauViewModel = new EntrainementViewModel(nouvelEntrainement);
            Entrainements.Add(nouveauViewModel);

            // 4. Nettoyer : desélectionner l'ancien temporaire
            SelectionEntrainement = null;

            // 5. Naviguer vers la bonne semaine (utilise le nouveau)
            _debutSemaine = GetDebutSemaine(nouvelEntrainement.DateHeure);
            OnPropertyChanged(
                nameof(SemaineAffichee),
                nameof(DateLundi), nameof(DateMardi), nameof(DateMercredi),
                nameof(DateJeudi), nameof(DateVendredi), nameof(DateSamedi), nameof(DateDimanche),
                nameof(EstAujourdhuiLundi), nameof(EstAujourdhuiMardi), nameof(EstAujourdhuiMercredi),
                nameof(EstAujourdhuiJeudi), nameof(EstAujourdhuiVendredi), nameof(EstAujourdhuiSamedi), nameof(EstAujourdhuiDimanche),
                nameof(EntrainementsSemaine));

            _view.InformationAjout();
        }


        public bool ValiderAjoutActif => SelectionEntrainement != null && !Entrainements.Contains(SelectionEntrainement);

        public ICommand TerminerEditionCommand => new RelayCommand(TerminerEdition);

        public void TerminerEdition()
        {
            if (SelectionEntrainement != null)
            {
                // Sauvegarde automatique des changements (si déjà dans la collection)
                if (Entrainements.Contains(SelectionEntrainement))
                {
                    _context.SaveChanges();
                }

                // ❌ DÉSELECTIONNE l'entraînement
                SelectionEntrainement = null;

                // Rafraîchit l'affichage
                OnPropertyChanged(nameof(EntrainementsSemaine));
            }
        }


        public ICommand SupprimerEntrainementCommand => new RelayCommand(SupprimerEntrainement);

        public void SupprimerEntrainement()
        {
            if (SelectionEntrainement == null) return;
            if (!_view.ConfirmationSuppression()) return;

            var modele = SelectionEntrainement.Modele;
            var entry = _context.Entry(modele);

            // Cas A : L'entraînement n'a jamais été enregistré en base (Id = 0)
            if (modele.Id == 0)
            {
                // On le détache simplement du contexte pour qu'il ne tente pas de l'insérer au prochain SaveChanges
                entry.State = EntityState.Detached;
            }
            // Cas B : L'entraînement existe déjà en base de données
            else
            {
                _context.Entrainements.Remove(modele);
                _context.SaveChanges(); // Optionnel : à faire ici ou via un bouton "Enregistrer" global
            }

            // Mise à jour de la liste locale (Interface)
            Entrainements.Remove(SelectionEntrainement);
            SelectionEntrainement = null;

            OnPropertyChanged(nameof(EntrainementsSemaine));
        }

        public bool SupprimerEntrainementActif => _selectionEntrainement != null;

        // Déplacer l'entraînement d'une heure plus tard
        public ICommand DeplacerEntrainementPlusTardCommand => new RelayCommand(DeplacerPlusTard);

        public void DeplacerPlusTard()
        {
            if (SelectionEntrainement == null) return;
            SelectionEntrainement.DateHeure = SelectionEntrainement.DateHeure.AddHours(1);
            OnPropertyChanged(nameof(EntrainementsSemaine), nameof(EntrainementsOrdonnes));
        }

        // Déplacer l'entraînement d'une heure plus tôt
        public ICommand DeplacerEntrainementPlusTotCommand => new RelayCommand(DeplacerPlusTot);

        public void DeplacerPlusTot()
        {
            if (SelectionEntrainement == null) return;
            SelectionEntrainement.DateHeure = SelectionEntrainement.DateHeure.AddHours(-1);
            OnPropertyChanged(nameof(EntrainementsSemaine), nameof(EntrainementsOrdonnes));
        }

        // Sélectionner un entraînement depuis le calendrier
        public ICommand SelectionnerEntrainementCommand => new RelayCommand<EntrainementViewModelAvecPosition>(SelectionnerEntrainement);

        public void SelectionnerEntrainement(EntrainementViewModelAvecPosition? entrainement)
        {
            if (entrainement != null)
            {
                SelectionEntrainement = entrainement.Entrainement;
            }
        }

        // Commande et méthode pour enregistrer les changements dans la base
        public ICommand EnregistrerCommand => new RelayCommand(Enregistrer);

        public void Enregistrer()
        {
            _context.SaveChanges();
        }

        public void NotifierChangementPlanning()
        {
            OnPropertyChanged(nameof(EntrainementsSemaine));
            OnPropertyChanged(nameof(EntrainementsOrdonnes));
        }

    }

}