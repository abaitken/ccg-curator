using CCGCurator.Common;
using DirectX.Capture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace CCGCurator
{
    class MainWindowViewModel : ViewModel
    {
        private ImageFeed selectedImageFeed;
        private bool viewLoaded;
        private Filters cameraFilters;
        private IEnumerable<ImageFeed> imageFeeds;
        private Bitmap cameraBitmap;
        private Capture capture;
        private double fScaleFactor;
        Control previewBox;

        public IEnumerable<ImageFeed> ImageFeeds
        {
            get
            {
                return imageFeeds;
            }
            set
            {
                imageFeeds = value;
                NotifyPropertyChanged();
            }
        }

        internal void ViewLoaded(Control previewBox)
        {
            if (viewLoaded)
                return;

            viewLoaded = true;
            this.previewBox = previewBox;

            cameraFilters = new Filters();
            var imageFeeds = new List<ImageFeed>();
            for (int i = 0; i < cameraFilters.VideoInputDevices.Count; i++)
            {
                imageFeeds.Add(new ImageFeed(cameraFilters.VideoInputDevices[i].Name, i));
            }
            ImageFeeds = imageFeeds;
            SelectedImageFeed = imageFeeds[2];
        }

        public ImageFeed SelectedImageFeed
        {
            get { return selectedImageFeed; }
            set
            {
                if (selectedImageFeed == value)
                    return;
                selectedImageFeed = value;
                NotifyPropertyChanged();
                ImageFeedHasChanged();
            }
        }

        private void ImageFeedHasChanged()
        {
            cameraBitmap = new Bitmap(800, 600);

            capture = new Capture(cameraFilters.VideoInputDevices[SelectedImageFeed.FilterIndex], cameraFilters.AudioInputDevices[0]);

            var maxSize = capture.VideoCaps.MaxFrameSize;

            capture.FrameSize = new Size(640, 480);

            if (maxSize.Height > 480)
                capture.FrameSize = new Size(800, 600);

            if (maxSize.Height >= 768)
                capture.FrameSize = new Size(1024, 768);

            fScaleFactor = Convert.ToDouble(capture.FrameSize.Height) / 480;
            capture.PreviewWindow = previewBox;
            capture.FrameEvent2 += CaptureDone;
            capture.GrapImg();
        }

        private void CaptureDone(Bitmap BM)
        {
            Debug.WriteLine("CaptureDone() called");
        }
    }
}
