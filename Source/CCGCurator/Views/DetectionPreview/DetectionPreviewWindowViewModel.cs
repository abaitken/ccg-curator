using System.Drawing;
using CCGCurator.Common;

namespace CCGCurator.Views.DetectionPreview
{
    class DetectionPreviewWindowViewModel : ViewModel
    {
        private Bitmap filteredPreviewImage;

        public Bitmap FilteredPreviewImage
        {
            get => filteredPreviewImage;
            set
            {
                if (filteredPreviewImage == value)
                    return;

                filteredPreviewImage = value;
                NotifyPropertyChanged();
            }
        }
    }
}
