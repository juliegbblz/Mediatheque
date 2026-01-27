using Mediatheque.Data;
using Mediatheque.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Linq;

namespace Mediatheque
{
    /// Logique d'interaction pour la fenêtre principale
    /// Gère l'initialisation DB, les dialogues UI et la synchronisation des contrôles de saisie
  
    public partial class MainWindow : Window, IMainView
    {
        private MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            // Configuration et création de la base de données SQLite
            var options = new DbContextOptionsBuilder()
                .UseSqlite("Data Source=mediatheque.db")
                .Options;

            var context = new MediathequeContext(options);
            context.Database.EnsureCreated();

            // Initialisation du ViewModel
            _vm = new MainViewModel(context, this);
            DataContext = _vm;

            // Abonnement au changement de sélection pour synchroniser les menus déroulants (Heure/Minute/Durée)
            _vm.PropertyChanged += Vm_PropertyChanged;
        }

        /// Synchronise les ComboBox de l'interface quand l'utilisateur sélectionne un entraînement
    
        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectionEntrainement))
            {
                Dispatcher.Invoke(() =>
                {
                    var sel = _vm.SelectionEntrainement;
                    if (sel == null) return;

                    // Synchronisation manuelle des index pour Heure, Minute et Durée
                    SyncComboByTag(HeureDebut, sel.DateHeure.Hour);
                    SyncComboByTag(MinuteDebut, sel.DateHeure.Minute);

                    if (this.FindName("DureeCombo") is ComboBox dureeCombo)
                        SyncComboByTag(dureeCombo, sel.DureeMinutes);
                });
            }
        }

      
        /// Utilitaire pour sélectionner l'item d'une ComboBox basé sur la valeur de son Tag

        private void SyncComboByTag(ComboBox combo, int value)
        {
            if (combo == null) return;
            var item = combo.Items.OfType<ComboBoxItem>()
                            .FirstOrDefault(i => i.Tag != null && Convert.ToInt32(i.Tag) == value);
            if (item != null) combo.SelectedItem = item;
        }

        private void Window_Closed(object sender, EventArgs e) => _vm.Enregistrer();

        public void InformationAjout() => MessageBox.Show("Entraînement ajouté avec succès.");

        public bool ConfirmationSuppression()
        {
            return MessageBox.Show("Confirmez-vous la suppression ?", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public void AlerteDepassementMinuit()
        {
            MessageBox.Show("Les entraînements ne peuvent pas continuer après minuit !",
                            "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // --- Gestion de la taille du planning pour le calcul du placement X ---

        private void PlanningItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm) vm.LargeurTotalePlanning = e.NewSize.Width;
        }

        private void PlanningGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm) vm.LargeurTotalePlanning = e.NewSize.Width;
        }

        // --- Événements de modification via les ComboBox ---

        private void HeureDebut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectionEntrainement != null)
            {
                if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
                {
                    int heure = Convert.ToInt32(item.Tag);
                    var d = vm.SelectionEntrainement.DateHeure;
                    vm.SelectionEntrainement.DateHeure = new DateTime(d.Year, d.Month, d.Day, heure, d.Minute, 0);

                    VerifierLimiteMinuit(vm);
                    vm.NotifierChangementPlanning();
                }
            }
        }

        private void MinuteDebut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectionEntrainement != null)
            {
                if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
                {
                    int minute = Convert.ToInt32(item.Tag);
                    var d = vm.SelectionEntrainement.DateHeure;
                    vm.SelectionEntrainement.DateHeure = new DateTime(d.Year, d.Month, d.Day, d.Hour, minute, 0);

                    VerifierLimiteMinuit(vm);
                    vm.NotifierChangementPlanning();
                }
            }
        }

        private void Duree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(DataContext is MainViewModel vm) || vm.SelectionEntrainement == null) return;

            if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
            {
                int demandee = Convert.ToInt32(item.Tag);
                var debut = vm.SelectionEntrainement.DateHeure;
                int minutesRestantes = (int)Math.Max(0, (debut.Date.AddDays(1) - debut).TotalMinutes);

                if (demandee > minutesRestantes)
                {
                    AlerteDepassementMinuit();
                    // Sélection de la durée maximale autorisée
                    var meilleureOption = combo.Items.OfType<ComboBoxItem>()
                                         .Select(i => new { Item = i, Val = Convert.ToInt32(i.Tag) })
                                         .OrderByDescending(x => x.Val)
                                         .FirstOrDefault(x => x.Val <= minutesRestantes);

                    if (meilleureOption != null)
                    {
                        combo.SelectedItem = meilleureOption.Item;
                        vm.SelectionEntrainement.DureeMinutes = meilleureOption.Val;
                    }
                }
                vm.NotifierChangementPlanning();
            }
        }

        private void VerifierLimiteMinuit(MainViewModel vm)
        {
            var debut = vm.SelectionEntrainement.DateHeure;
            int minutesRestantes = (int)Math.Max(0, (debut.Date.AddDays(1) - debut).TotalMinutes);
            if (vm.SelectionEntrainement.DureeMinutes > minutesRestantes)
            {
                vm.SelectionEntrainement.DureeMinutes = minutesRestantes;
                AlerteDepassementMinuit();
            }
        }
    }
}