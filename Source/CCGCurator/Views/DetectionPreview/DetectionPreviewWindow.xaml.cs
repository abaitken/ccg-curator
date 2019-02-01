using System.Windows;

namespace CCGCurator.Views.DetectionPreview
{
    /// <summary>
    /// Interaction logic for DetectionPreviewWindow.xaml
    /// </summary>
    internal partial class DetectionPreviewWindow : Window
    {
        public DetectionPreviewWindow()
        {
            ViewModel = new DetectionPreviewWindowViewModel();
            InitializeComponent();
        }

        public DetectionPreviewWindowViewModel ViewModel
        {
            get => DataContext as DetectionPreviewWindowViewModel;
            set => DataContext = value;
        }
    }
}
