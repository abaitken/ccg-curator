using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CCGCurator.Common;
using CCGCurator.Data;

namespace CCGCurator
{
    internal class MainWindowViewModel : ViewModel
    {
        private readonly ImageCapture imageCapture;
        private CardDetection cardDetection;
        private IDictionary<string, ActionCommand> commands;
        private ICollectionView detectedCardsView;
        private Bitmap filteredPreviewImage;
        private IList<ImageFeed> imageFeeds;
        private bool isFoil;
        private Bitmap previewImage;
        public List<Card> referenceCards = new List<Card>();
        private IdentifiedCardCounter selectedIdentifiedCardCounter;
        private ImageFeed selectedImageFeed;
        private SetFilter selectedSetFilter;
        private IEnumerable<SetFilter> setFilters;
        private ViewModelState viewModelState;

        public MainWindowViewModel()
        {
            DetectedCards = new List<IdentifiedCardCounter>();
            imageCapture = new ImageCapture();
            imageCapture.ImageCaptured += ImageCapture_ImageCaptured;
        }

        private ISettings Settings => Properties.Settings.Default;

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


        public int RotationDegrees
        {
            get => imageCapture.RotationDegrees;
            set
            {
                if (imageCapture.RotationDegrees == value)
                    return;

                imageCapture.RotationDegrees = value;
                NotifyPropertyChanged();
            }
        }

        public IdentifiedCardCounter SelectedIdentifiedCardCounter
        {
            get => selectedIdentifiedCardCounter;
            set
            {
                if (selectedIdentifiedCardCounter == value)
                    return;

                selectedIdentifiedCardCounter = value;
                NotifyPropertyChanged();
                RefreshCommands();
            }
        }

        public bool IsFoil
        {
            get => isFoil;
            set
            {
                if (isFoil == value)
                    return;

                isFoil = value;
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

        protected override void OnViewLoaded(Window window)
        {
            base.OnViewLoaded(window);

            Task.Run(() => { LoadData(); });
            Task.Run(() => { RecreateDetectedCardsView(); });

            Task.Run(() =>
            {
                ImageFeeds = imageCapture.Init();

                var previousIndex = Settings.WebcamIndex;
                if (previousIndex >= ImageFeeds.Count)
                    previousIndex = 0;

                RotationDegrees = ValidateRotation(Settings.RotationDegrees);
                SelectedImageFeed = ImageFeeds[previousIndex];
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
                new ActionCommand("Add", OnAdd, /*() => SelectedIdentifiedCardCounter != null, */
                    new KeyGesture(Key.A, ModifierKeys.Control))
            };
            Commands = commands.ToDictionary(k => k.Key, v => v);

            foreach (var command in commands)
            {
                window.InputBindings.Add(command.CreateInputBinding());
                window.CommandBindings.Add(command.CreateCommandBinding());
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
            var detectedCard = (IdentifiedCardCounter) parameter ?? SelectedIdentifiedCardCounter;
            var collectedCard = new CollectedCard(Guid.NewGuid(), detectedCard.Card, CardQuality.Unspecified, IsFoil);
            collectionData.Add(collectedCard);
            collectionData.Close();
            OnClear(null);
            IsFoil = false;
        }

        private void OnClear(object obj)
        {
            DetectedCards = new List<IdentifiedCardCounter>();
            RecreateDetectedCardsView();
        }

        private void StopCapturing()
        {
            imageCapture.StopCapturing();
            cardDetection = null;
        }

        private void StartCapturing()
        {
            StopCapturing();
            imageCapture.StartCapturing(SelectedImageFeed);

            var fScaleFactor = Convert.ToDouble(imageCapture.FrameSize.Height) / 480;
            cardDetection = new CardDetection(fScaleFactor);
        }

        private void ImageCapture_ImageCaptured(object sender, CaptureEvent e)
        {
            if (cardDetection == null)
                return;


            var captured = e.CapturedImage;
            var fromSet = SelectedSetFilter ?? SetFilter.All;

            var cards = cardDetection.Detect(captured, out var filtered);

            var cardIdentification = new CardIdentification();
            var identifiedCards = cardIdentification.Identify(cards, referenceCards, fromSet);
            var preview = CreatePreviewImage(identifiedCards, captured);

            FilteredPreviewImage = filtered;
            PreviewImage = preview;

            UpdateDetectedCards(identifiedCards);
        }

        public Bitmap CreatePreviewImage(List<IdentifiedCard> matches, Bitmap captured)
        {
            var resultImage = (Bitmap) captured.Clone();
            var g = Graphics.FromImage(resultImage);
            var font = new Font("Tahoma", 25);
            foreach (var item in matches)
            {
                var corners = item.Corners;
                var card = item.Card;
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
                var existingItem = DetectedCards.FirstOrDefault(i => i.Card == identifiedCard.Card);
                var itemExists = existingItem != null;
                var item = !itemExists ? new IdentifiedCardCounter(identifiedCard.Card) : existingItem;

                if (itemExists)
                    item.Occurrences++;
                else
                    DetectedCards.Add(item);
            }

            Application.Current.Dispatcher.Invoke(() => { DetectedCardsView.Refresh(); });
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
    }
}