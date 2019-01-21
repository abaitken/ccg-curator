using System.Windows;

namespace CCGCurator.ReferenceBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ViewModel = new MainWindowViewModel();
            InitializeComponent();
        }

        private MainWindowViewModel ViewModel
        {
            get { return DataContext as MainWindowViewModel; }
            set { DataContext = value; }
        }

        private void CollectData_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CollectData();
        }
    }
}
