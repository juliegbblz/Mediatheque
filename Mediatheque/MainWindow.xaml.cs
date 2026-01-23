using Mediatheque.Data;
using Mediatheque.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mediatheque
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainView
    {
        private MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            DbContextOptions options = new DbContextOptionsBuilder()
                .UseSqlite("Data Source=mediatheque.db")
                .Options;

            MediathequeContext context = new MediathequeContext(options);
            context.Database.EnsureCreated();

            _vm = new MainViewModel(context, this);
            DataContext = _vm;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _vm.Enregistrer();
        }

        public void InformationAjout()
        {
            MessageBox.Show("Entrainement ajouté");
        }

        public bool ConfirmationSuppression()
        {
            return MessageBox.Show("Confirmez-vous la suppression ?", Title,
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        // Ajoutez cette propriété à la classe MainWindow pour corriger l'erreur CS0103
        public dynamic? SelectionEntrainement
        {
            get => _vm?.SelectionEntrainement;
        }

        private void HeureDebut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectionEntrainement == null) return;

            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                int heure = Convert.ToInt32(selectedItem.Tag);
                var dateActuelle = SelectionEntrainement.DateHeure;
                SelectionEntrainement.DateHeure = new DateTime(
                    dateActuelle.Year,
                    dateActuelle.Month,
                    dateActuelle.Day,
                    heure,
                    dateActuelle.Minute,
                    0);
            }
        }

        private void MinuteDebut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectionEntrainement == null) return;

            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                int minute = Convert.ToInt32(selectedItem.Tag);
                var dateActuelle = SelectionEntrainement.DateHeure;
                SelectionEntrainement.DateHeure = new DateTime(
                    dateActuelle.Year,
                    dateActuelle.Month,
                    dateActuelle.Day,
                    dateActuelle.Hour,
                    minute,
                    0);
            }
        }
    }
}