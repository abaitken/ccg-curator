using CCGCurator.Common;
using DirectX.Capture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace CCGCurator
{
    class MainWindowViewModel : ViewModel, IDisposable
    {
        private ImageFeed selectedImageFeed;
        private bool viewLoaded;
        private Filters cameraFilters;
        private IEnumerable<ImageFeed> imageFeeds;
        private Bitmap cameraBitmap;
        private Capture capture;
        private double fScaleFactor;

        internal void Closing()
        {
            capture.Stop();
            capture.PreviewWindow = null;
        }

        Control captureBox;

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
            this.captureBox = new PictureBox();

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
            capture.PreviewWindow = captureBox;
            capture.FrameEvent2 += CaptureDone;
            capture.GrapImg();
        }

        private void CaptureDone(Bitmap BM)
        {
            Debug.WriteLine("CaptureDone() called");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    capture = null;
                    captureBox.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MainWindowViewModel() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
