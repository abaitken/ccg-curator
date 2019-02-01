using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
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
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;
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


        private IDictionary<string, ActionCommand> commands;


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
            DetectedCards = new List<IdentifiedCardCounter>();
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

        private List<IdentifiedCardCounter> DetectedCards { get; set; }

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

        public IDictionary<string, ActionCommand> Commands
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
            Settings.RotationDegrees = RotationDegrees;
            Settings.WebcamIndex = SelectedImageFeed.FilterIndex;
            Settings.Save();

            // hack - https://stackoverflow.com/questions/38528908/stopping-imediacontrol-never-ends
            if (!Task.Run(() => StopCapturing()).Wait(TimeSpan.FromSeconds(5)))
            {
                MessageBox.Show("Could not stop the capturing stream. The application will now forcibly close.");
                Environment.FailFast("Could not stop the capturing stream.");
            }
        }

        internal void ViewLoaded(Window window)
        {
            if (viewLoaded)
                return;

            viewLoaded = true;

            Task.Run(() => { LoadData(); });
            Task.Run(() => { RecreateDetectedCardsView(); });
            Task.Run(() => { LoadCardCollection(); });

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

                RotationDegrees = ValidateRotation(Settings.RotationDegrees);
                SelectedImageFeed = imageFeeds[previousIndex];
                ViewModelState = ViewModelState.Ready;
            });
            SetupBindings(window);
        }

        private int ValidateRotation(int settingsRotationDegrees)
        {
            var validValues = EnumerableExtensions.Range(-180, 180, 90);
            if (validValues.Contains(settingsRotationDegrees))
                return settingsRotationDegrees;
            return 0;
        }

        private void LoadCardCollection()
        {
            var applicationSettings = new ApplicationSettings();
            var collectionData = new CardCollection(applicationSettings.CollectionDataPath);
            cardCollection = new List<CollectedCard>(collectionData.GetCollection());
            collectionData.Close();


            var collectionView = CollectionViewSource.GetDefaultView(cardCollection);
            CardCollectionCollectionView = collectionView;
        }


        ICollectionView cardCollectionCollectionView;
        public ICollectionView CardCollectionCollectionView
        {
            get { return cardCollectionCollectionView; }
            set
            {
                if (cardCollectionCollectionView == value)
                    return;

                cardCollectionCollectionView = value;
                NotifyPropertyChanged();
            }
        }


        private void RecreateDetectedCardsView()
        {
            var collectionView = CollectionViewSource.GetDefaultView(DetectedCards);
            collectionView.SortDescriptions.Add(new SortDescription(IdentifiedCardCounter.OccurrencesPropertyName,
                ListSortDirection.Descending));
            DetectedCardsView = collectionView;
        }


        private void SetupBindings(Window window)
        {
            var commands = new[]
            {
                new ActionCommand("Clear", OnClear, new KeyGesture(Key.C, ModifierKeys.Control)),
                new ActionCommand("Add", OnAdd, /*() => SelectedIdentifiedCardCounter != null, */new KeyGesture(Key.A, ModifierKeys.Control))
            };
            Commands = commands.ToDictionary(k => k.Key, v => v);

            foreach (var command in commands)
            {
                window.InputBindings.Add(command.CreateInputBinding());
                window.CommandBindings.Add(command.CreateCommandBinding());
            }
        }


        int rotationDegrees;
        public int RotationDegrees
        {
            get { return rotationDegrees; }
            set
            {
                if (rotationDegrees == value)
                    return;

                rotationDegrees = value;
                NotifyPropertyChanged();
            }
        }


        IdentifiedCardCounter selectedIdentifiedCardCounter;
        private List<CollectedCard> cardCollection;

        public IdentifiedCardCounter SelectedIdentifiedCardCounter
        {
            get { return selectedIdentifiedCardCounter; }
            set
            {
                if (selectedIdentifiedCardCounter == value)
                    return;

                selectedIdentifiedCardCounter = value;
                NotifyPropertyChanged();
                RefreshCommands();
            }
        }

        private void RefreshCommands()
        {
            //foreach (var command in Commands) command.Value.RaiseCanExecuteChanged();
        }

        private void OnAdd(object parameter)
        {
            if (SelectedIdentifiedCardCounter == null && parameter == null)
                return;

            var applicationSettings = new ApplicationSettings();
            var collectionData = new CardCollection(applicationSettings.CollectionDataPath);
            var detectedCard = (IdentifiedCardCounter)parameter ?? SelectedIdentifiedCardCounter;
            var collectedCard = new CollectedCard(Guid.NewGuid(), detectedCard.Card, CardQuality.Unspecified, IsFoil);
            collectionData.Add(collectedCard);
            cardCollection.Add(collectedCard);
            CardCollectionCollectionView.Refresh();
            collectionData.Close();
            OnClear(null);
            IsFoil = false;
        }

        private void OnClear(object obj)
        {
            DetectedCards = new List<IdentifiedCardCounter>();
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

            lock (captureThreadLocker)
            {
                captured = RotateImage(captured);
                var fromSet = SelectedSetFilter ?? SetFilter.All;

                var cards = cardDetection.Detect(captured, out var filtered);

                var cardIdentification = new CardIdentification();
                var identifiedCards = cardIdentification.Identify(cards, referenceCards, fromSet);
                var preview = CreatePreviewImage(identifiedCards, captured);
                
                FilteredPreviewImage = filtered;
                PreviewImage = preview;

                UpdateDetectedCards(identifiedCards);
            }
        }

        public Bitmap CreatePreviewImage(List<IdentifiedCard> matches, Bitmap captured)
        {
            var resultImage = (Bitmap)captured.Clone();
            var g = Graphics.FromImage(resultImage);
            var font = new Font("Tahoma", 25);
            foreach (var item in matches)
            {
                var corners = item.corners;
                var card = item.card;
                //ContrastCorrection filter = new ContrastCorrection(15);
                //filter.ApplyInPlace(card.cardArtBitmap);
                g.DrawString(card.Name, font, Brushes.Black,
                    new PointF(corners[0].X - 29, corners[0].Y - 39));
                g.DrawString(card.Name, font, Brushes.Red,
                    new PointF(corners[0].X - 30, corners[0].Y - 40));
            }

            g.Dispose();

            return resultImage;
        }

        private void UpdateDetectedCards(List<IdentifiedCard> identifiedCards)
        {
            if (!identifiedCards.Any()) return;

            foreach (var identifiedCard in identifiedCards)
            {
                var existingItem = DetectedCards.FirstOrDefault(i => i.Card == identifiedCard.card);
                var itemExists = existingItem != null;
                var item = !itemExists ? new IdentifiedCardCounter(identifiedCard.card) : existingItem;

                if (itemExists)
                    item.Occurrences++;
                else
                    DetectedCards.Add(item);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                DetectedCardsView.Refresh();
            });
        }

        private Bitmap RotateImage(Bitmap original)
        {
            if (RotationDegrees == 0)
                return original;

            var center = new PointF((float)original.Width / 2, (float)original.Height / 2);
            var result = new Bitmap(original.Width, original.Height, original.PixelFormat);

            result.SetResolution(original.HorizontalResolution, original.VerticalResolution);

            using (var g = Graphics.FromImage(result))
            {
                var matrix = new Matrix();

                matrix.RotateAt(RotationDegrees, center);

                g.Transform = matrix;
                g.DrawImage(original, new Point());
            }

            return result;
        }

        private void LoadData()
        {
            Task.Run(() =>
            {
                var localCardData = new LocalCardData(new ApplicationSettings().DetectionDataPath);
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


        bool isFoil;
        public bool IsFoil
        {
            get { return isFoil; }
            set
            {
                if (isFoil == value)
                    return;

                isFoil = value;
                NotifyPropertyChanged();
            }
        }

    }
}