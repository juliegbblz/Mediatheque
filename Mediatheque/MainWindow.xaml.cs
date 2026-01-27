using Mediatheque.Data;
using Mediatheque.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Linq;

namespace Mediatheque
{

    /// <summary>
    /// Logique d'interaction de la fenêtre principale. 
    /// Assure le pont entre le ViewModel et les contrôles WPF spécifiques.
    /// </summary>
    public partial class MainWindow : Window, IMainView
    {
        private MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            // Configuration explicite avec le type du contexte
            var options = new DbContextOptionsBuilder<MediathequeContext>()
                .UseSqlite("Data Source=mediatheque.db")
                .Options;

            var context = new MediathequeContext(options);

            // Applique les migrations (crée la base si elle n'existe pas ou ajoute les colonnes manquantes)
            context.Database.Migrate();

            _vm = new MainViewModel(context, this);
            DataContext = _vm;

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

        public void InformationAjout()
        {
            MessageBox.Show("Entraînement ajouté.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ConfirmationSuppression()
        {
            return MessageBox.Show("Confirmez la suppression ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public void AlerteDepassementMinuit()
        {
            MessageBox.Show("Les entraînements ne peuvent pas dépasser minuit.", "Alerte", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void AlerteAvantHeureDebut()
        {
            MessageBox.Show("Les entraînements ne peuvent pas commencer avant 06:00.", "Alerte", MessageBoxButton.OK, MessageBoxImage.Warning);
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