using CCGCurator.Common;
using CCGCurator.Data;
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
        private PictureBox previewBox;
        private PictureBox filteredBox;
        private Filters cameraFilters;
        private IEnumerable<ImageFeed> imageFeeds;
        private Capture capture;
        CardDetection cardDetection;
        public List<Card> referenceCards = new List<Card>();
        Control captureBox;

        internal void Closing()
        {
            capture.Stop();
            capture.PreviewWindow = null;
        }

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

        internal void ViewLoaded(PictureBox previewBox, PictureBox filteredBox)
        {
            if (viewLoaded)
                return;

            viewLoaded = true;
            this.previewBox = previewBox;
            this.filteredBox = filteredBox;
            this.captureBox = new PictureBox();
            loadSourceCards();
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
            if(capture != null)
            {
                capture.Stop();
                capture = null;
                cardDetection = null;
            }
            capture = new Capture(cameraFilters.VideoInputDevices[SelectedImageFeed.FilterIndex], cameraFilters.AudioInputDevices[0]);

            var maxSize = capture.VideoCaps.MaxFrameSize;

            capture.FrameSize = new Size(640, 480);

            if (maxSize.Height > 480)
                capture.FrameSize = new Size(800, 600);

            if (maxSize.Height >= 768)
                capture.FrameSize = new Size(1024, 768);


            var fScaleFactor = Convert.ToDouble(capture.FrameSize.Height) / 480;
            cardDetection = new CardDetection(fScaleFactor, referenceCards);
            capture.PreviewWindow = captureBox;
            capture.FrameEvent2 += CaptureDone;
            capture.GrapImg();
        }



        private void CaptureDone(Bitmap captured)
        {
            Bitmap filtered;
            Bitmap preview;
            cardDetection.Process(captured, out filtered, out preview);
            filteredBox.Image = filtered;
            previewBox.Image = preview;
        }

        private void loadSourceCards()
        {
            var localCardData = new LocalCardData(new Data.ApplicationSettings().DatabasePath);
            referenceCards = new List<Card>(localCardData.GetCardsWithHashes());
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
