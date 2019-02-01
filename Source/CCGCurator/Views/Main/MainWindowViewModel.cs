using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CCGCurator.Common;
using CCGCurator.Data;
using CCGCurator.Data.Model;
using CCGCurator.Data.PersonalCollection;
using CCGCurator.Data.ReferenceData;
using CCGCurator.Views.Collection;
using CCGCurator.Views.CommandModel;
using CCGCurator.Views.DetectionPreview;
using CCGCurator.Views.Settings;

namespace CCGCurator.Views.Main
{
    internal class MainWindowViewModel : ViewModel
    {
        private readonly ImageCapture imageCapture;
        private CardDetection cardDetection;
        private IDictionary<string, CommandModelBase> commands;
        private bool isFoil;
        private Bitmap previewImage;
        public List<Card> referenceCards = new List<Card>();
        private SetFilter selectedSetFilter;
        private IEnumerable<SetFilter> setFilters;
        private ViewModelState viewModelState;
        private DetectionPreviewWindow detectionPreviewWindow;

        public MainWindowViewModel()
        {
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
                setFilters = value;
                NotifyPropertyChanged();
            }
        }

        private List<IdentifiedCardCounter> DetectedCards { get; set; } = new List<IdentifiedCardCounter>();
        private List<Card> IgnoreCards { get; set; } = new List<Card>();

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

        public IDictionary<string, CommandModelBase> Commands
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

            Task.Run(() =>
            {
                StartCapturing();
                ViewModelState = ViewModelState.Ready;
            });
            SetupBindings(window);
        }

        private void SetupBindings(Window window)
        {
            var commandList = new[]
            {
                new { Key = "DetectionPreview" , Command = (CommandModelBase)new ActionCommand(OnOpenDetectionPreview), Gesture = (Key?)null },
                new { Key = "MyCollection" , Command = (CommandModelBase)new ActionCommand(OpenMyCollection), Gesture = (Key?)null },
                new { Key = "OpenSettings" , Command = (CommandModelBase)new ActionCommand(OpenSettings), Gesture = (Key?)null },
                new { Key = "AddCard" , Command = (CommandModelBase)new ActionCommand(OnAdd), Gesture = (Key?)Key.Y },
                new { Key = "Reset" , Command = (CommandModelBase)new ActionCommand(ResetCurrentDetection), Gesture = (Key?)Key.R },
                new { Key = "IgnoreCard" , Command = (CommandModelBase)new ActionCommand(IgnoreCard), Gesture = (Key?)Key.N },
                new { Key = "Foil" , Command = (CommandModelBase)new ActionCommand(ToggleFoil), Gesture = (Key?)Key.F },
            };

            Commands = commandList.ToDictionary(k => k.Key, v => v.Command);

            foreach (var item in commandList)
            {
                var command = item.Command;
                var key = item.Gesture;
                if(key.HasValue)
                    window.InputBindings.Add(command.CreateKeyBinding(key.Value));
                window.CommandBindings.Add(command.CreateCommandBinding());
            }
        }

        private void ToggleFoil()
        {
            IsFoil = !IsFoil;
        }

        private void OnOpenDetectionPreview()
        {
            detectionPreviewWindow = new DetectionPreviewWindow
            {
                Owner = View
            };
            detectionPreviewWindow.Closing += (sender, args) => detectionPreviewWindow = null;
            detectionPreviewWindow.Show();
        }
        
        private void OpenMyCollection()
        {
            var collectionWindow = new CollectionWindow { Owner = View };
            collectionWindow.Show();
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow { Owner = View };
            var result = settingsWindow.ShowDialog();
            if (result.HasValue && result.Value)
                ReloadSettings();
        }

        private void OnAdd()
        {
            var topRankedDetection = TopRankedDetection;
            if (topRankedDetection == null)
                return;

            var applicationSettings = new ApplicationSettings();
            var collectionData = new CardCollection(applicationSettings.CollectionDataPath);
            var collectedCard = new CollectedCard(Guid.NewGuid(), topRankedDetection, CardQuality.Unspecified, IsFoil);
            collectionData.Add(collectedCard);
            collectionData.Close();
            ResetCurrentDetection();
            IsFoil = false;
        }

        private void IgnoreCard()
        {
            var topRankedDetection = TopRankedDetection;
            if (topRankedDetection == null)
                return;
            IgnoreCards.Add(topRankedDetection);

            var itemToRemove = DetectedCards.FirstOrDefault(i => i.Card == topRankedDetection);
            if (itemToRemove != null)
                DetectedCards.Remove(itemToRemove);
            NotifyPropertyChanged(nameof(TopRankedDetection));
        }

        private void ResetCurrentDetection()
        {
            DetectedCards = new List<IdentifiedCardCounter>();
            IgnoreCards = new List<Card>();
            NotifyPropertyChanged(nameof(TopRankedDetection));
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

            var detectedCards = cardDetection.Detect(captured, out var greyscaleImage);

            var cardIdentification = new CardIdentification();
            var identifiedCards = cardIdentification.Identify(detectedCards, referenceCards, fromSet, IgnoreCards);

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
                PreviewImage = imageTools.DrawCardNames(new[]{ identifiedCards.First() }, captured);
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
            NotifyPropertyChanged(nameof(TopRankedDetection));
        }

        public Card TopRankedDetection
        {
            get
            {
                if (DetectedCards.Count == 0)
                    return null;

                int maxOccurence = 0;
                Card topRankCard = null;
                foreach (var card in DetectedCards)
                {
                    if (topRankCard != null && card.Occurrences <= maxOccurence)
                        continue;
                    maxOccurence = card.Occurrences;
                    topRankCard = card.Card;
                }

                return topRankCard;
            }
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

        private void ReloadSettings()
        {
            if (Settings.WebcamIndex != imageCapture.ImageFeed.FilterIndex)
                StartCapturing();
            else
                imageCapture.RotationDegrees = Settings.RotationDegrees;
        }
    }
}