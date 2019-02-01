using System.Windows;

namespace CCGCurator.Views.Settings
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    internal partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            ViewModel = new SettingsWindowViewModel();
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewLoaded(this);
        }

        public SettingsWindowViewModel ViewModel
        {
            get => DataContext as SettingsWindowViewModel;
            set => DataContext = value;
        }

        private void Close(bool dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }

        private void Discard_OnClick(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveSettings();
            Close(true);
        }
    }
}
