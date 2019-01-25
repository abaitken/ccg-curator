using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Data;
using CCGCurator.Common;
using CCGCurator.Data;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace CCGCurator.ReferenceBuilder
{
    internal class MainWindowViewModel : ViewModel, IDataActionsNotifier
    {
        private readonly Timer timer;
        private readonly BackgroundWorker worker;
        private ObservableCollection<DataAction> pendingActions = new ObservableCollection<DataAction>();
        private int maximumValue = 1;
        private int progressValue;
        private IList<SetInfo> setInfo = new List<SetInfo>();
        private DateTime? startTime;
        private bool viewLoaded;
        private IList<Set> sets;
        private List<Set> setsInDatabase;
        private ICollectionView setInfoCollectionView;
        private string filterText;
        private readonly ReferenceBuilderWorker referenceBuilderWorker;

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

        public bool CanCollectData => !worker.IsBusy;

        public string StatusText
        {
            get
            {
                if (!worker.IsBusy || startTime == null)
                    return string.Empty;

                var timeTaken = DateTime.Now - startTime.Value;
                var averageTimePerSet = TimeSpan.FromTicks((long) ((double) timeTaken.Ticks / ProgressValue));
                var remaining = MaximumValue - ProgressValue;
                //var estimatedTimeRemaining = TimeSpan.FromTicks(remaining * averageTimePerSet.Ticks);
                //var remainingPercentage = (double) remaining / MaximumValue;
                //var estimatedTimeRemaining = TimeSpan.FromTicks((long) (timeTaken.Ticks / remainingPercentage));

                var text = new StringBuilder();
                text.AppendLine($"{FormatPercentage(ProgressValue, MaximumValue)}");
                text.AppendLine($"Started {startTime.Value}");
                text.AppendLine($"Time taken {timeTaken}");
                text.AppendLine($"Current {ProgressValue}");
                text.AppendLine($"Maximum {MaximumValue}");
                text.AppendLine($"Remaining {remaining}");
                text.AppendLine($"Time per set {averageTimePerSet}");
                //text.AppendLine($"Time remaining {estimatedTimeRemaining}");
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

            UpdateCurrentSetView();
            NotifyPropertyChanged(nameof(CanCollectData));
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
            referenceBuilderWorker.DoWork(sets, PendingActions);
            ProgressValue = MaximumValue;
        }

        internal void CollectData()
        {
            if (CanCollectData)
            {
                ProgressValue = 0;
                worker.RunWorkerAsync();
                NotifyPropertyChanged(nameof(CanCollectData));
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
            UpdateCurrentSetView();
        }

        private void UpdateCurrentSetView()
        {
            var applicationSettings = new ApplicationSettings();
            var remoteDataFileClient = new RemoteDataFileClient(applicationSettings);
            var remoteCardData = new RemoteCardData(remoteDataFileClient);

            sets = remoteCardData.GetSets();

            var localCardData = new LocalCardData(applicationSettings.DatabasePath);
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
            PendingActions.Clear();
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
    }
}