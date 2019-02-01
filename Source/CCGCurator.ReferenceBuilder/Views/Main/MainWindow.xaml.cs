using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace CCGCurator.ReferenceBuilder.Views.Main
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ViewModel = new MainWindowViewModel();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            InitializeComponent();
        }

        private MainWindowViewModel ViewModel
        {
            get { return DataContext as MainWindowViewModel; }
            set { DataContext = value; }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            ViewModel.ViewClosing();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewLoaded();
        }

        private void CollectData_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CollectData();
        }

        private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.UpdateFilter(SearchTextBox.Text);
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                InitialDirectory = ViewModel.ImageCachePath,
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                ViewModel.ImageCachePath = dialog.FileName;
        }
    }
}