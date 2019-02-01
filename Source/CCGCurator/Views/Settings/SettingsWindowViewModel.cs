using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CCGCurator.Common;

namespace CCGCurator.Views.Settings
{
    class SettingsWindowViewModel : ViewModel
    {
        private IList<ImageFeed> imageFeeds;
        private ImageFeed selectedImageFeed;
        private int rotationDegrees;

        public IList<ImageFeed> ImageFeeds
        {
            get => imageFeeds;
            set
            {
                imageFeeds = value;
                NotifyPropertyChanged();
            }
        }

        public ImageFeed SelectedImageFeed
        {
            get => selectedImageFeed;
            set
            {
                if (selectedImageFeed == value)
                    return;
                selectedImageFeed = value;
                NotifyPropertyChanged();
            }
        }


        public int RotationDegrees
        {
            get => rotationDegrees;
            set
            {
                if (rotationDegrees == value)
                    return;

                rotationDegrees = value;
                NotifyPropertyChanged();
            }
        }

        private ISettings Settings => Properties.Settings.Default;

        public void SaveSettings()
        {
            Settings.RotationDegrees = RotationDegrees;
            Settings.WebcamIndex = SelectedImageFeed.FilterIndex;
            Settings.Save();
        }

        protected override void OnViewLoaded(Window window)
        {
            base.OnViewLoaded(window);

            Task.Run(() =>
            {
                ImageFeeds = ImageCapture.ImageFeeds();
                RotationDegrees = SettingsHelper.ValidateRotation(Settings.RotationDegrees);
                SelectedImageFeed =
                    imageFeeds[SettingsHelper.ValidateWebCamIndex(Settings.WebcamIndex, imageFeeds.Count)];
            });
        }
    }
}
