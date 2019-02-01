using System.ComponentModel;
using System.Windows;
using CCGCurator.Views.Settings;

namespace CCGCurator
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        public MainWindow()
        {
            ViewModel = new MainWindowViewModel();
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        public MainWindowViewModel ViewModel
        {
            get => DataContext as MainWindowViewModel;
            set => DataContext = value;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            ViewModel.Closing();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewLoaded(this);
        }

        private void OpenCollection_OnClick(object sender, RoutedEventArgs e)
        {
            var collectionWindow = new CollectionWindow {Owner = this};
            collectionWindow.Show();
        }

        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow {Owner = this};
            var result = settingsWindow.ShowDialog();
            if (result.HasValue && result.Value)
                ViewModel.ReloadSettings();
        }
    }
}