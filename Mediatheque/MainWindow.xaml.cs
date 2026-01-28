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
    /// Fenêtre principale WPF.
    /// Sert d’interface entre le ViewModel (logique applicative)
    /// et les contrôles spécifiques à WPF (ComboBox, événements, Dispatcher…).
    /// </summary>
    public partial class MainWindow : Window, IMainView
    {
        private MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            // Création manuelle du DbContext afin de contrôler explicitement
            // la configuration et le cycle de vie de la base de données
            var options = new DbContextOptionsBuilder<MediathequeContext>()
                .UseSqlite("Data Source=mediatheque.db")
                .Options;

            var context = new MediathequeContext(options);

            // Synchronise le schéma de la base avec le modèle (création / migrations)
            context.Database.Migrate();

            _vm = new MainViewModel(context, this);
            DataContext = _vm;

            // Écoute les changements de propriétés du ViewModel
            _vm.PropertyChanged += Vm_PropertyChanged;
        }

        /// <summary>
        /// Met à jour manuellement certaines ComboBox lorsque
        /// l’entraînement sélectionné change côté ViewModel.
        /// </summary>
        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectionEntrainement))
            {
                Dispatcher.Invoke(() =>
                {
                    var sel = _vm.SelectionEntrainement;
                    if (sel == null) return;

                    // Repositionne les ComboBox sur les valeurs de l’entraînement sélectionné
                    SyncComboByTag(HeureDebut, sel.DateHeure.Hour);
                    SyncComboByTag(MinuteDebut, sel.DateHeure.Minute);

                    if (this.FindName("DureeCombo") is ComboBox dureeCombo)
                        SyncComboByTag(dureeCombo, sel.DureeMinutes);
                });
            }
        }

        /// <summary>
        /// Sélectionne dans une ComboBox l’item dont la propriété Tag
        /// correspond à la valeur fournie.
        /// </summary>
        private void SyncComboByTag(ComboBox combo, int value)
        {
            if (combo == null) return;

            var item = combo.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => i.Tag != null && Convert.ToInt32(i.Tag) == value);

            if (item != null)
                combo.SelectedItem = item;
        }

        private void Window_Closed(object sender, EventArgs e) => _vm.Enregistrer();

        public void InformationAjout()
        {
            MessageBox.Show(
                "Entraînement ajouté.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        public bool ConfirmationSuppression()
        {
            return MessageBox.Show(
                "Confirmez la suppression ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            ) == MessageBoxResult.Yes;
        }

        public void AlerteDepassementMinuit()
        {
            MessageBox.Show(
                "Les entraînements ne peuvent pas dépasser minuit.",
                "Alerte",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        public void AlerteAvantHeureDebut()
        {
            MessageBox.Show(
                "Les entraînements ne peuvent pas commencer avant 06:00.",
                "Alerte",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        /// <summary>
        /// Récupère la largeur réelle du planning pour permettre
        /// le calcul dynamique du positionnement horizontal des éléments.
        /// </summary>
        private void PlanningItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.LargeurTotalePlanning = e.NewSize.Width;
        }

        private void PlanningGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.LargeurTotalePlanning = e.NewSize.Width;
        }

        /// <summary>
        /// Modifie l’heure de début de l’entraînement sélectionné
        /// à partir de la ComboBox correspondante.
        /// </summary>
        private void HeureDebut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectionEntrainement != null)
            {
                if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
                {
                    int heure = Convert.ToInt32(item.Tag);
                    var d = vm.SelectionEntrainement.DateHeure;

                    vm.SelectionEntrainement.DateHeure =
                        new DateTime(d.Year, d.Month, d.Day, heure, d.Minute, 0);

                    VerifierLimiteMinuit(vm);
                    vm.NotifierChangementPlanning();
                }
            }
        }

        /// <summary>
        /// Modifie les minutes de début de l’entraînement sélectionné.
        /// </summary>
        private void MinuteDebut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectionEntrainement != null)
            {
                if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
                {
                    int minute = Convert.ToInt32(item.Tag);
                    var d = vm.SelectionEntrainement.DateHeure;

                    vm.SelectionEntrainement.DateHeure =
                        new DateTime(d.Year, d.Month, d.Day, d.Hour, minute, 0);

                    VerifierLimiteMinuit(vm);
                    vm.NotifierChangementPlanning();
                }
            }
        }

        /// <summary>
        /// Gère la modification de la durée en empêchant
        /// tout dépassement au-delà de minuit.
        /// </summary>
        private void Duree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(DataContext is MainViewModel vm) || vm.SelectionEntrainement == null)
                return;

            if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
            {
                int demandee = Convert.ToInt32(item.Tag);
                var debut = vm.SelectionEntrainement.DateHeure;

                int minutesRestantes =
                    (int)Math.Max(0, (debut.Date.AddDays(1) - debut).TotalMinutes);

                if (demandee > minutesRestantes)
                {
                    AlerteDepassementMinuit();

                    // Recherche de la plus grande durée autorisée inférieure à la limite
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

        /// <summary>
        /// Vérifie que la durée actuelle ne provoque pas
        /// un dépassement après minuit et ajuste si nécessaire.
        /// </summary>
        private void VerifierLimiteMinuit(MainViewModel vm)
        {
            var debut = vm.SelectionEntrainement.DateHeure;

            int minutesRestantes =
                (int)Math.Max(0, (debut.Date.AddDays(1) - debut).TotalMinutes);

            if (vm.SelectionEntrainement.DureeMinutes > minutesRestantes)
            {
                vm.SelectionEntrainement.DureeMinutes = minutesRestantes;
                AlerteDepassementMinuit();
            }
        }
    }
}
