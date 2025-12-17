using SkiStar.repositiories;
using SkiStar.SkistarData;
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

namespace SkiStar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DbRepository _DbRepo = new DbRepository();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_register_skier(object sender, RoutedEventArgs e)
        {

        }

        private async void btn_search_skier(object sender, RoutedEventArgs e)
        {
            Skier skier = await _DbRepo.GetSKierByNameAsync(5); //Hämta data

            MessageBox.Show($"Skidåkare {skier.Firstname} {skier.Lastname} hittades");
        }
    }
}