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
using CCGCurator.Views;
using CCGCurator.Views.DetectionPreview;

namespace CCGCurator
{
    internal class MainWindowViewModel : ViewModel
    {
        private readonly ImageCapture imageCapture;
        private CardDetection cardDetection;
        private IDictionary<string, ActionCommand> commands;
        private ICollectionView detectedCardsView;
        private bool isFoil;
        private Bitmap previewImage;
        public List<Card> referenceCards = new List<Card>();
        private IdentifiedCardCounter selectedIdentifiedCardCounter;
        private SetFilter selectedSetFilter;
        private IEnumerable<SetFilter> setFilters;
        private ViewModelState viewModelState;
        private DetectionPreviewWindow detectionPreviewWindow;

        public MainWindowViewModel()
        {
            DetectedCards = new List<IdentifiedCardCounter>();
            imageCapture = new ImageCapture();
            imageCapture.ImageCaptured += ImageCapture_ImageCaptured;
        }

        private ISettings Settings => Properties.Settings.Default;

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
                StartCapturing();
                ViewModelState = ViewModelState.Ready;
            });
            SetupBindings(window);
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
                new ActionCommand("Add", OnAdd, /*() => SelectedIdentifiedCardCounter != null, */ new KeyGesture(Key.A, ModifierKeys.Control)),
                new ActionCommand("DetectionPreview", OnOpenDetectionPreview)
            };
            Commands = commands.ToDictionary(k => k.Key, v => v);

            foreach (var command in commands)
            {
                if(command.KeyGesture != null)
                    window.InputBindings.Add(command.CreateInputBinding());
                window.CommandBindings.Add(command.CreateCommandBinding());
            }
        }

        private void OnOpenDetectionPreview(object obj)
        {
            detectionPreviewWindow = new DetectionPreviewWindow
            {
                Owner = View
            };
            detectionPreviewWindow.Closing += (sender, args) => detectionPreviewWindow = null;
            detectionPreviewWindow.Show();
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

            var imageFeeds = ImageCapture.ImageFeeds();
            imageCapture.RotationDegrees = SettingsHelper.ValidateRotation(Settings.RotationDegrees);
            var selectedImageFeed = imageFeeds[SettingsHelper.ValidateWebCamIndex(Settings.WebcamIndex, imageFeeds.Count)];
            imageCapture.StartCapturing(selectedImageFeed);

            var fScaleFactor = Convert.ToDouble(imageCapture.FrameSize.Height) / 480;
            cardDetection = new CardDetection(fScaleFactor);
        }

        private void ImageCapture_ImageCaptured(object sender, CaptureEvent e)
        {
            if (cardDetection == null)
                return;

            var captured = e.CapturedImage;
            var fromSet = SelectedSetFilter ?? SetFilter.All;

            var cards = cardDetection.Detect(captured, out var greyscaleImage);

            var cardIdentification = new CardIdentification();
            var identifiedCards = cardIdentification.Identify(cards, referenceCards, fromSet);

            PresentPreviewImage(identifiedCards, captured);
            PresentDetectionImage(identifiedCards, greyscaleImage);
            UpdateDetectedCards(identifiedCards);
        }

        private void PresentPreviewImage(List<IdentifiedCard> identifiedCards, Bitmap captured)
        {
            var imageTools = new ImageTools();

            if (!identifiedCards.Any())
            {
                PreviewImage = captured;
            }
            else if (Settings.ZoomToDetectedCard)
            {
                PreviewImage = imageTools.GetDetectedCardImage(identifiedCards[0].Corners, captured, 1);
            }
            else
            {
                PreviewImage = imageTools.DrawCardNames(identifiedCards, captured);
            }
        }

        private void PresentDetectionImage(List<IdentifiedCard> identifiedCards, Bitmap greyscaleImage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (detectionPreviewWindow == null)
                    return;
                var imageTools = new ImageTools();
                var detectionImage = imageTools.DrawDetectionBox(greyscaleImage, identifiedCards);
                detectionPreviewWindow.ViewModel.FilteredPreviewImage = detectionImage;
            });
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

        public void ReloadSettings()
        {
            if (Settings.WebcamIndex != imageCapture.ImageFeed.FilterIndex)
                StartCapturing();
            else
                imageCapture.RotationDegrees = Settings.RotationDegrees;
        }
    }
}