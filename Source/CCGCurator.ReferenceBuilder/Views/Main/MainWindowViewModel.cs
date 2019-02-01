using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Data;
using CCGCurator.Common;
using CCGCurator.Data;
using CCGCurator.Data.Model;
using CCGCurator.Data.ReferenceData;
using CCGCurator.ReferenceBuilder.Actions;
using CCGCurator.ReferenceBuilder.Data;
using CCGCurator.ReferenceBuilder.Model;

namespace CCGCurator.ReferenceBuilder.Views.Main
{
    internal class MainWindowViewModel : ViewModel, IDataActionsNotifier
    {
        private readonly ReferenceBuilderWorker referenceBuilderWorker;
        private readonly Timer timer;
        private readonly BackgroundWorker worker;
        private string filterText;
        private string imageCachePath;
        private int maximumValue = 1;
        private ObservableCollection<DataAction> pendingActions = new ObservableCollection<DataAction>();
        private int progressValue;
        private IList<SetInfo> setInfo = new List<SetInfo>();
        private ICollectionView setInfoCollectionView;
        private IList<Set> sets;
        private List<Set> setsInDatabase;
        private DateTime? startTime;
        private bool viewLoaded;

        public MainWindowViewModel()
        {
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            timer = new Timer(200);
            timer.Elapsed += Timer_Elapsed;
            timer.Stop();
            referenceBuilderWorker = new ReferenceBuilderWorker();
        }

        public ViewModelState ViewModelState
        {
            get
            {
                if (worker.IsBusy)
                    return ViewModelState.Processing;

                if (sets == null)
                    return ViewModelState.FetchingData;

                return ViewModelState.Ready;
            }
        }

        public string StatusText
        {
            get
            {
                if (!worker.IsBusy || startTime == null)
                    return string.Empty;

                var timeTaken = DateTime.Now - startTime.Value;
                var averageTimePerSet = ProgressValue == 0
                    ? timeTaken
                    : TimeSpan.FromTicks((long) ((double) timeTaken.Ticks / ProgressValue));
                var remaining = MaximumValue - ProgressValue;

                var text = new StringBuilder();
                text.AppendLine($"{FormatPercentage(ProgressValue, MaximumValue)}");
                text.AppendLine($"Started {startTime.Value}");
                text.AppendLine($"Time taken {timeTaken}");
                text.AppendLine($"Current {ProgressValue}");
                text.AppendLine($"Maximum {MaximumValue}");
                text.AppendLine($"Remaining {remaining}");
                text.AppendLine($"Time per set {averageTimePerSet}");
                return text.ToString();
            }
        }

        public IList<SetInfo> SetInfo
        {
            get => setInfo;
            set
            {
                setInfo = value;
                NotifyPropertyChanged();
            }
        }

        public ICollectionView SetInfoCollectionView
        {
            get => setInfoCollectionView;
            set
            {
                setInfoCollectionView = value;
                NotifyPropertyChanged();
            }
        }

        public int ProgressValue
        {
            get => progressValue;

            set
            {
                if (progressValue == value)
                    return;
                progressValue = value;
                NotifyPropertyChanged();
            }
        }

        public int MaximumValue
        {
            get => maximumValue;

            set
            {
                if (maximumValue == value)
                    return;
                maximumValue = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<DataAction> PendingActions
        {
            get => pendingActions;
            set
            {
                if (pendingActions == value)
                    return;
                pendingActions = value;
                NotifyPropertyChanged();
            }
        }

        private ISettings Settings => Properties.Settings.Default;

        public string ImageCachePath
        {
            get { return imageCachePath; }
            set
            {
                if (imageCachePath == value)
                    return;

                imageCachePath = value;
                NotifyPropertyChanged();
            }
        }

        public void Update(Set set, bool include)
        {
            var actionInstance = PendingActions.FirstOrDefault(i => i.Set.Code.Equals(set.Code));
            PendingActions.Remove(actionInstance);

            var databaseContainsSet = setsInDatabase.FirstOrDefault(i => i.Code.Equals(set.Code)) != null;
            if (databaseContainsSet && !include)
            {
                PendingActions.Add(new DeleteAction(set));
            }

            if (!databaseContainsSet && include)
            {
                PendingActions.Add(new AddAction(set));
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer.Stop();

            NotifyPropertyChanged(nameof(StatusText));
            UpdateCurrentSetView();
            NotifyPropertyChanged(nameof(ViewModelState));
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ProgressValue = referenceBuilderWorker.CurrentItem;
            NotifyPropertyChanged(nameof(StatusText));
        }

        private string FormatPercentage(double value, double maximum)
        {
            return (int) ((double) ProgressValue / MaximumValue * 100) + "%";
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            ProgressValue = 0;
            MaximumValue = PendingActions.Count;
            referenceBuilderWorker.DoWork(sets, PendingActions, ImageCachePath);
            ProgressValue = MaximumValue;
        }

        internal void CollectData()
        {
            if (ViewModelState == ViewModelState.Ready)
            {
                ProgressValue = 0;
                worker.RunWorkerAsync();
                NotifyPropertyChanged(nameof(ViewModelState));
                timer.Start();
                startTime = DateTime.Now;
                NotifyPropertyChanged(nameof(StatusText));
            }
        }

        public void ViewLoaded()
        {
            if (viewLoaded)
                return;
            viewLoaded = true;

            ResolveImageCachePath();
            UpdateCurrentSetView();
        }

        private void ResolveImageCachePath()
        {
            var applicationSettings = new ApplicationSettings();

            ImageCachePath =
                string.IsNullOrWhiteSpace(Settings.ImageCachePath) || !Directory.Exists(Settings.ImageCachePath)
                    ? applicationSettings.DefaultImageCacheFolder
                    : Settings.ImageCachePath;
        }

        private void UpdateCurrentSetView()
        {
            PendingActions.Clear();
            Task.Run(() =>
            {
                var applicationSettings = new ApplicationSettings();
                var remoteDataFileClient = new RemoteDataFileClient(applicationSettings);
                var remoteCardData = new RemoteCardData(remoteDataFileClient);

                sets = remoteCardData.GetSets();

                var localCardData = new LocalCardData(applicationSettings.DetectionDataPath);
                setsInDatabase = localCardData.GetSets().ToList();

                var setInfo = new List<SetInfo>();

                foreach (var set in sets)
                {
                    var inDatabase = setsInDatabase.Any(i => i.Code.Equals(set.Code));
                    setInfo.Add(new SetInfo(set, inDatabase, this));
                }

                SetInfo = setInfo;
                var collectionView = CollectionViewSource.GetDefaultView(SetInfo);
                collectionView.Filter = SetListBoxFilter;
                SetInfoCollectionView = collectionView;
                NotifyPropertyChanged(nameof(ViewModelState));
            });
        }

        private bool SetListBoxFilter(object obj)
        {
            if (string.IsNullOrEmpty(filterText))
            {
                return true;
            }

            var value = obj as SetInfo;

            if (value == null)
                return false;

            return value.Name.Contains(filterText);
        }

        public void UpdateFilter(string filterText)
        {
            this.filterText = filterText;
            SetInfoCollectionView.Refresh();
        }

        public void ViewClosing()
        {
            Settings.ImageCachePath = ImageCachePath;
            Settings.Save();
        }
    }
}