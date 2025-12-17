using SkiDataSimulator.Models;
using SkiDataSimulator.Repositories;
using SkiDataSimulator.Simulation;
using SkidataWpf.Models;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace SkiDataSimulator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int SimulatedSkierCount = 15;
    private readonly DbRepository _dbRepository;
    private readonly SkiSimulator _simulator;
    int? _currentSkierId;
    Skier _currentSkier;
    SkiPass _skipass;
    int _skipassId;
    




    public MainWindow()
    {
        InitializeComponent();
        _dbRepository = new DbRepository();
        _simulator = new SkiSimulator(_dbRepository);

    }

    /// <summary>
    /// Hämtar all data som ska finnas i comboboxarna och anropar respektive funktion för respektive combobox
    /// </summary>
    private async void FillComboboxes()
    {

        List<Destination> destinations = await _dbRepository.GetAllDestinations();
        List<Resort> resorts = await _dbRepository.GetAllResorts();
        List<Lift> lifts = await _dbRepository.GetAllLifts();
        List<Skier> skierData = await _dbRepository.GetSKierByNameAsync(txtfirstname.Text, txtlastname.Text);


        destinations.Insert(0, new Models.Destination { Id = -1, Name = "Välj destination" });
        resorts.Insert(0, new Models.Resort { Id = -1, Name = "Välj Resort" });
        lifts.Insert(0, new Models.Lift { Id = -1, Name = "Välj lift" });
        skierData.Insert(0, new Models.Skier { Id = -1, Firstname = "Välj skidåkare" });


        FillCombobox_Destination_Resort_Lift<Destination>(CbDestination, destinations);
        FillCombobox_Destination_Resort_Lift<Resort>(CbResort, resorts);
        FillCombobox_Destination_Resort_Lift<Lift>(CbLift, lifts);
        FillComboboxSkier<Skier>(CbSkiers, skierData);
    }

    /// <summary>
    /// Fyller på comboboxar för lift, resort och destination
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cb"></param>
    /// <param name="list"></param>
    private async void FillCombobox_Destination_Resort_Lift<T>(ComboBox cb, List<T> list)
    {
        cb.ItemsSource = list;
        cb.DisplayMemberPath = "Name";
        cb.SelectedValuePath = "Id";
        cb.SelectedIndex = 0;
    }
    
    /// <summary>
    /// Fyller på combobox för valda skidåkare
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cb"></param>
    /// <param name="list"></param>
    private async void FillComboboxSkier<T>(ComboBox cb, List<T> list)
    {
        cb.ItemsSource = list;
        cb.DisplayMemberPath = "Fullname";
        cb.SelectedValuePath = "Id";
        cb.SelectedIndex = 0;
    }

    /// <summary>
    /// Metod som sortarar övriga komboboxar vid vald destination
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void cbDestination_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CbDestination.SelectedItem == null)
            return;
        Destination selectedDestion = (Destination)CbDestination.SelectedItem;
        int destinationId = selectedDestion.Id;
        List<Resort> sortedResorts = await _dbRepository.GetAllResortsFiltered(destinationId);
        FillCombobox_Destination_Resort_Lift<Resort>(CbResort, sortedResorts);

    }

    /// <summary>
    /// Metod som sorterar liftar utefter vald resort
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void cbResort_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CbResort.SelectedItem == null)
            return;
        Resort selectedResort = (Resort)CbResort.SelectedItem;
        int resortId = selectedResort.Id;
        List<Lift> sortedLifts = await _dbRepository.GetAllLiftsFiltered(resortId);
        FillCombobox_Destination_Resort_Lift<Lift>(CbLift, sortedLifts);

    }

    /// <summary>
    /// Knapp som skickar vidare till funktioner som simulerar åk för dagen
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btn_Simulate_Day(object sender, RoutedEventArgs e)
    {
        try
        {
            btnSimulateDay.IsEnabled = false;
            DateTime today = DateTime.Today;
            List<SkiPass> skiPasses = await _dbRepository.GetRandomSkiPassesAsync(SimulatedSkierCount);

            List<SkiRun> skiRuns = await _simulator.SimulateDayForAllSkipassesAsync(skiPasses, today);
            await _dbRepository.SaveSkiRunsAsync(skiRuns);

            ShowMessage("Simulering slutförd och data sparat!", "Info");

        }
        catch (Exception ex)
        {
            ShowMessage($"Ett fel inträffade: {ex.Message}", "Fel", MessageBoxImage.Error);
        }
        finally
        {
            btnSimulateDay.IsEnabled = true;
        }
    }

    /// <summary>
    /// Knapp som skickar vidare till funktioner som simulerar åk för säsonger
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btn_Simulate_Season(object sender, RoutedEventArgs e)
    {
        try
        {
            btnSimulateSeason.IsEnabled = false;
            DateTime dayInSeason = new(2023, 1, 12); // kommer till exempel ge dig säsongen 23/24
            List<SkiPass> skiPasses = await _dbRepository.GetRandomSkiPassesAsync(SimulatedSkierCount);
            List<SkiRun> skiRuns = await _simulator.SimulateSeasonAsync(skiPasses, dayInSeason);
            await _dbRepository.SaveSkiRunsAsync(skiRuns);
            ShowMessage("Simulering slutförd och data sparat!", "Info");
        }
        catch (Exception ex)
        {
            ShowMessage($"Ett fel inträffade: {ex.Message}", "Fel", MessageBoxImage.Error);
        }
        finally
        {
            btnSimulateSeason.IsEnabled = true;
        }
    }

    /// <summary>
    /// Knapp som hämtar namn från gränssnittet via combobox-funktionen
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btn_Search_Skier(object sender, RoutedEventArgs e)
    {
        try
        {
            FillComboboxes();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Det gick inte att hitta skidåkare. Felmeddelande: {ex.Message}");
        }
    }

    /// <summary>
    /// Knapp som skapar en ny skidåkare i databasen och skriver ut ett meddelenade om det lyckades eller ej
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btn_Register_New_Skier(object sender, RoutedEventArgs e)
    {
        try
        {
            if (txtfirstname.Text.Length > 0 || txtlastname.Text.Length > 0)
            {
                Skier skier = new Skier { Firstname = txtfirstname.Text, Lastname = txtlastname.Text };
                await _dbRepository.CreateNewSkier(skier);
                txtaddedskier.Content = $"Skidåkare {skier.Firstname} {skier.Lastname} \nhar lagts till";
            }

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Det gick inte att lägga till skidåkaren. Felmeddelande: {ex.Message}");
        }

    }

    /// <summary>
    /// Knapp som skickar vidare till funktion som valdierar om skidåkare har åk. Om den har det returneras en fråga om 
    /// skidåkaren vill radera sina åk, om skidåkaren inte har några registrerade åk anropas funktion som raderar skidåkaren
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btn_Delete_Skier(object sender, RoutedEventArgs e)
    {
        
        try
        {
            Skier selectedSkier = (Skier)CbSkiers.SelectedItem;
            bool skierIsDeleted = await _dbRepository.ValidateSkiRun(selectedSkier);

            if (skierIsDeleted == true)
            {
                // https://www.youtube.com/watch?v=VZ2cp8mBgvs Källa till messageboxfunktion
                MessageBoxResult r = MessageBox.Show($"Det finns registrerade åk, vill du radera alla åk?", "Bekräftelse", MessageBoxButton.YesNo);
                if (r == MessageBoxResult.No)
                {
                    txtaddedskier.Content = "Du har inte raderat ," +
                        "" +
                        "\nnågonting";

                }
                else if (r == MessageBoxResult.Yes)
                {
                    bool skiRunsIsDeleted = await _dbRepository.DeleteAllSkiRuns(selectedSkier);
                    await _dbRepository.DeleteSkier(selectedSkier);
                    if (skiRunsIsDeleted)
                    {
                        
                        txtaddedskier.Content = $"Skidåkare och tillhörande \n åk har raderats";
                    }

                }
            }
        }
        catch (Exception ex)
        {

            MessageBox.Show($"Det gick inte att radera åkare. Felmeddelande: {ex.Message}");
        }
    }

    /// <summary>
    /// knapp som skickar in en skidåkare och ett liftkort från gränssnittet, anropar funktion som registrerar köp
    /// i databasen och returnerar en bekräftelse på om köpet genomfördes eller inte
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btn_Buy_Skipass(object sender, RoutedEventArgs e)
    {
        try
        {
            SkiPass skipass = new SkiPass { CardNumber = txtCardNumber.Text, Start_date = DateTime.Parse(txtStartdate.Text), End_date = DateTime.Parse(txtEnddate.Text), DestinationId = CbDestination.SelectedIndex };
            Skier selectedSkier = (Skier)CbSkiers.SelectedItem;
            bool isSkipassBought = await _dbRepository.BuySkipass(selectedSkier, skipass);
            if (isSkipassBought)
                MessageBox.Show($"Du har nu köpt ett liftkort till åkaren");

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Det gick inte att köpa liftkort. Felmeddelande: {ex.Message}");
        }
    }

    /// <summary>
    /// Knapp som skickar in information från valda items från komboboxar och textrutor från gränssnittet
    /// validerar om skidåkaren har ett giltligt liftkort om i så fall registrerar ett åk
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btn_Register_Skirun(object sender, RoutedEventArgs e)
    {
        try
        {
            Resort selectedResort = (Resort)CbResort.SelectedItem;
            int resortId = selectedResort.Id;
            string cardNumber = txtCardNumber.Text;
            DateTime todaysDate = DateTime.Parse(txtTodaysdate.Text);
            SkiRun skirun = new SkiRun();
            Lift selectedLift = (Lift)CbLift.SelectedItem;
            int liftId = selectedLift.Id;

            bool isSkipassValid = await _dbRepository.ValidateSkipass(resortId, cardNumber, todaysDate);
            if (isSkipassValid)
            {

                MessageBox.Show($"Ditt liftkort är giltligt. Välkommen!");

                SkiPass skipass = new SkiPass();
                int skiPassId = await _dbRepository.GetSkiPassId(txtCardNumber.Text);
                bool sucessfullSkirun = await _dbRepository.RegisterNewSkirun(skiPassId, liftId);

                if (sucessfullSkirun)
                    MessageBox.Show($"Tack för ditt åk :)");
            }
            else
            {
                MessageBox.Show($"Ditt liftkort är inte giltligt :(");
            }

        }
        catch (Exception ex)
        {

            MessageBox.Show($"Det gick inte att registrera åket. Felmeddelande: {ex.Message}");
        }

    }

    /// <summary>
    /// Knapp som fyller på information från databasen från vald skidåkare från comboboxen och skriver in all information i gränssnittet
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void btn_Get_More_Info(object sender, RoutedEventArgs e)
    {
        try
        {
            Skier selectedSkier = (Skier)CbSkiers.SelectedItem;
            (var endDate, long totalDays, long totalCountries) = await _dbRepository.GetAllInfoSkier(selectedSkier);
            txtMoreInfo.Text = ($"Namn: {selectedSkier.Firstname} {selectedSkier.Lastname}\nSlutdatum liftkort: {endDate} \nTotalt antal skiddagar för säsongen: {totalDays}\nTotalt antal besökta länder: {totalCountries}");

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Det gick inte hämta all information om skidåkaren, då det saknas information. Felmeddelande: {ex.Message}");
        }
    }

    /// <summary>
    /// Informationsruta att simulering är genomförd
    /// </summary>
    /// <param name="message"></param>
    /// <param name="title"></param>
    /// <param name="icon"></param>
    private void ShowMessage(string message, string title, MessageBoxImage icon = MessageBoxImage.Information)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, icon);
    }

}

