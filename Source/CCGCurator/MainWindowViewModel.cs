using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CCGCurator.Common;
using CCGCurator.Data;
using DirectX.Capture;

namespace CCGCurator
{
    internal class MainWindowViewModel : ViewModel
    {
        private static readonly object _locker = new object();
        private Filters cameraFilters;
        private Capture capture;
        private Control captureBox;

        private bool capturing;
        private CardDetection cardDetection;

        private PictureBox filteredBox;
        private IEnumerable<ImageFeed> imageFeeds;
        private PictureBox previewBox;
        public List<Card> referenceCards = new List<Card>();
        private ImageFeed selectedImageFeed;
        private bool viewLoaded;

        public MainWindowViewModel()
        {
            DetectedCards = new ObservableCollection<Card>();
        }

        private ISettings Settings => Properties.Settings.Default;

        public IEnumerable<ImageFeed> ImageFeeds
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
                StartCapturing();
            }
        }

        public ObservableCollection<Card> DetectedCards { get; }

        internal void Closing()
        {
            StopCapturing();

            Settings.WebcamIndex = SelectedImageFeed.FilterIndex;
            Settings.Save();
        }

        internal void ViewLoaded(PictureBox previewBox, PictureBox filteredBox)
        {
            if (viewLoaded)
                return;

            viewLoaded = true;
            this.previewBox = previewBox;
            this.filteredBox = filteredBox;
            captureBox = new PictureBox();
            LoadSourceCards();
            cameraFilters = new Filters();
            var imageFeeds = new List<ImageFeed>();
            for (var i = 0; i < cameraFilters.VideoInputDevices.Count; i++)
                imageFeeds.Add(new ImageFeed(cameraFilters.VideoInputDevices[i].Name, i));
            ImageFeeds = imageFeeds;

            var previousIndex = Settings.WebcamIndex;
            if (previousIndex >= imageFeeds.Count)
                previousIndex = 0;

            SelectedImageFeed = imageFeeds[previousIndex];
        }

        private Size CalculateCaptureFrameSize(Size maxSize)
        {
            if (maxSize.Height >= 768)
                return new Size(1024, 768);
            if (maxSize.Height > 480)
                return new Size(800, 600);
            return new Size(640, 480);
        }

        private void StopCapturing()
        {
            capturing = false;
            if (capture != null)
            {
                capture.Stop();
                capture.PreviewWindow = null;
                capture.FrameEvent2 -= CaptureDone;
                capture.Dispose();
                capture = null;
                cardDetection = null;
            }
        }

        private void StartCapturing()
        {
            StopCapturing();
            if (SelectedImageFeed == null)
                return;
            capture = new Capture(cameraFilters.VideoInputDevices[SelectedImageFeed.FilterIndex],
                cameraFilters.AudioInputDevices[0]);
            if (capture.VideoCaps == null)
                return;
            capture.FrameSize = CalculateCaptureFrameSize(capture.VideoCaps.MaxFrameSize);

            var fScaleFactor = Convert.ToDouble(capture.FrameSize.Height) / 480;
            cardDetection = new CardDetection(fScaleFactor);
            capture.PreviewWindow = captureBox;
            capture.FrameEvent2 += CaptureDone;
            capture.GrapImg();
            capturing = true;
        }

        private void CaptureDone(Bitmap captured)
        {
            if (cardDetection == null || !capturing)
                return;

            List<Card> detectedCards;

            lock (_locker)
            {
                detectedCards = cardDetection.Process(captured, out var filtered, out var preview, referenceCards);
                filteredBox.Image = filtered;
                previewBox.Image = preview;
            }

            if (detectedCards.Any())
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var detectedCard in detectedCards)
                        if (!DetectedCards.Contains(detectedCard))
                            DetectedCards.Add(detectedCard);
                });
        }

        private void LoadSourceCards()
        {
            Task.Run(() =>
            {
                var localCardData = new LocalCardData(new ApplicationSettings().CardDataPath);
                referenceCards = new List<Card>(localCardData.GetCardsWithHashes());
            });
        }
    }
}