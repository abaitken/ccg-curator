using System;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CCGCurator.Common;
using CCGCurator.Data;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace CCGCurator.ReferenceBuilder
{
    internal class MainWindowViewModel : ViewModel
    {
        private readonly Timer timer;
        private readonly BackgroundWorker worker;
        private int maximumValue = 1;
        private int progressValue;
        private DateTime? startTime;

        public MainWindowViewModel()
        {
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            timer = new Timer(200);
            timer.Elapsed += Timer_Elapsed;
            timer.Stop();
        }

        public bool CanCollectData => !worker.IsBusy;

        public string StatusText
        {
            get
            {
                if (!worker.IsBusy || startTime == null)
                    return string.Empty;

                var timeTaken = DateTime.Now - startTime.Value;
                var averageTimePerSet = TimeSpan.FromTicks((long)((double)timeTaken.Ticks / ProgressValue));
                var remaining = MaximumValue - ProgressValue;
                //var estimatedTimeRemaining = TimeSpan.FromTicks(remaining * averageTimePerSet.Ticks);
                var remainingPercentage = (double)remaining / MaximumValue;
                var estimatedTimeRemaining = TimeSpan.FromTicks((long)(timeTaken.Ticks / remainingPercentage));

                var text = new StringBuilder();
                text.AppendLine($"{FormatPercentage(ProgressValue, MaximumValue)}");
                text.AppendLine($"Started {startTime.Value}");
                text.AppendLine($"Time taken {timeTaken}");
                text.AppendLine($"Current {ProgressValue}");
                text.AppendLine($"Maximum {MaximumValue}");
                text.AppendLine($"Remaining {remaining}");
                text.AppendLine($"Time per set {averageTimePerSet}");
                text.AppendLine($"Time remaining {estimatedTimeRemaining}");
                return text.ToString();
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

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer.Stop();
            ProgressValue = 0;

            NotifyPropertyChanged(nameof(CanCollectData));
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ProgressValue));
            NotifyPropertyChanged(nameof(StatusText));
        }

        private string FormatPercentage(double value, double maximum)
        {
            return (int) ((double) ProgressValue / MaximumValue * 100) + "%";
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var applicationSettings = new ApplicationSettings();

            if (File.Exists(applicationSettings.DatabasePath))
                File.Delete(applicationSettings.DatabasePath);

            var localCardData = new LocalCardData(applicationSettings.DatabasePath);
            var remoteDataFileClient = new RemoteDataFileClient(applicationSettings);
            var remoteCardData = new RemoteCardData(remoteDataFileClient);

            var sets = remoteCardData.GetSets();

            MaximumValue = sets.Count;

            var imageSource = new DualImageSource(applicationSettings.ImagesFolder);

            var logFileName = "collection.log";
            if (File.Exists(logFileName))
                File.Delete(logFileName);
            var logger = new Logging(logFileName);

            Synchronous.ForEach(sets, set =>
            {
                try
                {
                    var cards = remoteCardData.GetCards(set);
                    localCardData.AddSet(set);

                    Synchronous.ForEach(cards, card =>
                    {
                        try
                        {
                            var image = imageSource.GetImage(card, set);

                            var imageHashing = new pHash();
                            if (image != null) card.pHash = imageHashing.ImageHash(image);
                        }
                        catch (Exception ex)
                        {
                            logger.WriteLine($"CARD={card.Name};SET={set.Code};EXCEPTION={ex.GetType()},{ex.Message}");
                        }

                        try
                        {
                            localCardData.AddCard(card, set);
                        }
                        catch (SQLiteException e4)
                        {
                            logger.WriteLine(
                                $"CARD={card.MultiverseId},{card.Name};SET={set.Code},{set.Name};EXCEPTION={e4.GetType()},{e4.Message}");
                        }
                    });
                }
                catch (JsonReaderException e1)
                {
                    logger.WriteLine($"SET={set.Code},{set.Name};EXCEPTION={e1.GetType()},{e1.Message}");
                }
                catch (WebException e2)
                {
                    logger.WriteLine($"SET={set.Code},{set.Name};EXCEPTION={e2.GetType()},{e2.Message}");
                }
                catch (SQLiteException e3)
                {
                    logger.WriteLine($"SET={set.Code},{set.Name};EXCEPTION={e3.GetType()},{e3.Message}");
                }
                finally
                {
                    Interlocked.Increment(ref progressValue);
                }
            });
            logger.Close();
            localCardData.Close();
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
    }
}