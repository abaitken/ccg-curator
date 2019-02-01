using System.Windows;

namespace CCGCurator
{
    /// <summary>
    /// Interaction logic for CollectionWindow.xaml
    /// </summary>
    internal partial class CollectionWindow : Window
    {
        public CollectionWindow()
        {
            ViewModel = new CollectionWindowViewModel();
            InitializeComponent();
            Loaded += CollectionWindow_Loaded;
        }

        private void CollectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewLoaded(this);
        }

        public CollectionWindowViewModel ViewModel
        {
            get => DataContext as CollectionWindowViewModel;
            set => DataContext = value;
        }
    }
}
