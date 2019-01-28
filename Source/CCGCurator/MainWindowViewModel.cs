using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using CCGCurator.Common;
using CCGCurator.Data;
using DirectX.Capture;
using Application = System.Windows.Application;
using Size = System.Drawing.Size;

namespace CCGCurator
{
    internal class MainWindowViewModel : ViewModel
    {
        private static readonly object captureThreadLocker = new object();
        private Filters cameraFilters;
        private Capture capture;
        private Control captureBox;

        private bool capturing;
        private CardDetection cardDetection;


        private IDictionary<string, ICommand> commands;


        private ICollectionView detectedCardsView;


        private Bitmap filteredPreviewImage;

        private IEnumerable<ImageFeed> imageFeeds;


        private Bitmap previewImage;
        public List<Card> referenceCards = new List<Card>();
        private ImageFeed selectedImageFeed;


        private SetFilter selectedSetFilter;


        private IEnumerable<SetFilter> setFilters;
        private bool viewLoaded;


        private ViewModelState viewModelState;

        public MainWindowViewModel()
        {
            DetectedCards = new List<DetectedCard>();
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

        public SetFilter SelectedSetFilter
        {
            get => selectedSetFilter;
            set
            {
                if (selectedSetFilter == value)
                    return;

                selectedSetFilter = value;
                NotifyPropertyChanged();
            }
        }

        public IEnumerable<SetFilter> SetFilters
        {
            get => setFilters;
            set
            {
                if (setFilters == value)
                    return;

                setFilters = value;
                NotifyPropertyChanged();
            }
        }

        private List<DetectedCard> DetectedCards { get; set; }

        public Bitmap PreviewImage
        {
            get => previewImage;
            set
            {
                if (previewImage == value)
                    return;

                previewImage = value;
                NotifyPropertyChanged();
            }
        }

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

        public ICollectionView DetectedCardsView
        {
            get => detectedCardsView;
            set
            {
                if (detectedCardsView == value)
                    return;

                detectedCardsView = value;
                NotifyPropertyChanged();
            }
        }

        public ViewModelState ViewModelState
        {
            get => viewModelState;
            set
            {
                if (viewModelState == value)
                    return;

                viewModelState = value;
                NotifyPropertyChanged();
            }
        }

        public IDictionary<string, ICommand> Commands
        {
            get => commands;
            set
            {
                if (commands == value)
                    return;

                commands = value;
                NotifyPropertyChanged();
            }
        }

        internal void Closing()
        {
            StopCapturing();

            Settings.WebcamIndex = SelectedImageFeed.FilterIndex;
            Settings.Save();
        }

        internal void ViewLoaded(Window window)
        {
            if (viewLoaded)
                return;

            viewLoaded = true;

            Task.Run(() => { LoadData(); });
            Task.Run(() => { RecreateDetectedCardsView(); });

            Task.Run(() =>
            {
                captureBox = new PictureBox();
                cameraFilters = new Filters();
                var imageFeeds = new List<ImageFeed>();
                for (var i = 0; i < cameraFilters.VideoInputDevices.Count; i++)
                    imageFeeds.Add(new ImageFeed(cameraFilters.VideoInputDevices[i].Name, i));
                ImageFeeds = imageFeeds;

                var previousIndex = Settings.WebcamIndex;
                if (previousIndex >= imageFeeds.Count)
                    previousIndex = 0;

                SelectedImageFeed = imageFeeds[previousIndex];
                ViewModelState = ViewModelState.Ready;
            });
            SetupBindings(window);
        }

        private void RecreateDetectedCardsView()
        {
            var collectionView = CollectionViewSource.GetDefaultView(DetectedCards);
            collectionView.SortDescriptions.Add(new SortDescription(DetectedCard.OccurrencesPropertyName,
                ListSortDirection.Descending));
            DetectedCardsView = collectionView;
        }


        private void SetupBindings(Window window)
        {
            var commands = new[]
            {
                new ActionCommand("Clear", OnClear, new KeyGesture(Key.C, ModifierKeys.Control)),
                new ActionCommand("Add", OnAdd, new KeyGesture(Key.A, ModifierKeys.Control))
            };
            Commands = commands.ToDictionary(k => k.Key, v => v.Command);

            foreach (var command in commands)
            {
                window.InputBindings.Add(command.CreateInputBinding());
                window.CommandBindings.Add(command.CreateCommandBinding());
            }
        }

        private void OnAdd()
        {
        }

        private void OnClear()
        {
            DetectedCards = new List<DetectedCard>();
            RecreateDetectedCardsView();
        }

        private Size CalculateCaptureFrameSize(Size maxSize)
        {
            var resolutions = new[]
            {
                new Size(1920, 1080),
                new Size(1024, 768),
                new Size(800, 600),
                new Size(640, 480)
            };

            foreach (var resolution in resolutions)
                if (maxSize.Height >= resolution.Height)
                    return resolution;

            return resolutions.Last();
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

            lock (captureThreadLocker)
            {
                var fromSet = SelectedSetFilter ?? SetFilter.All;
                detectedCards =
                    cardDetection.Process(captured, out var filtered, out var preview, referenceCards, fromSet);
                FilteredPreviewImage = filtered;
                PreviewImage = preview;
            }

            if (detectedCards.Any())
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var detectedCard in detectedCards)
                    {
                        var existingItem = DetectedCards.FirstOrDefault(i => i.Card == detectedCard);
                        var itemExists = existingItem != null;
                        var item = !itemExists ? new DetectedCard(detectedCard) : existingItem;


                        if (itemExists)
                            item.Occurrences++;
                        else
                            DetectedCards.Add(item);

                        DetectedCardsView.Refresh();
                    }
                });
        }

        private void LoadData()
        {
            Task.Run(() =>
            {
                var localCardData = new LocalCardData(new ApplicationSettings().CardDataPath);
                referenceCards = new List<Card>(localCardData.GetCardsWithHashes());

                var sets = localCardData.GetSets();
                var allSetsFilter = SetFilter.All;
                var setFilters = new List<SetFilter>
                {
                    allSetsFilter
                };
                foreach (var set in sets) setFilters.Add(new SetFilter(set));

                SetFilters = setFilters;
                SelectedSetFilter = allSetsFilter;
            });
        }
    }
}