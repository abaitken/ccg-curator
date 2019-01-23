using CCGCurator.Common;
using CCGCurator.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace CCGCurator.ReferenceBuilder
{
    class MainWindowViewModel : ViewModel
    {
        BackgroundWorker worker;
        private int progressValue;
        private int maximumValue = 1;
        private readonly System.Timers.Timer timer;
        private DateTime? startTime;

        public MainWindowViewModel()
        {
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            timer = new System.Timers.Timer(200);
            timer.Elapsed += Timer_Elapsed;
            timer.Stop();
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

        public bool CanCollectData
        {
            get
            {
                return !worker.IsBusy;
            }
        }

        private string FormatPercentage(double value, double maximum)
        {
            return ((int)(((double)ProgressValue / MaximumValue) * 100)).ToString() + "%";
        }

        public string StatusText
        {
            get
            {
                if (!worker.IsBusy || startTime == null)
                    return string.Empty;

                return $"Started {startTime.Value}; Time taken {DateTime.Now - startTime.Value}; {FormatPercentage(ProgressValue, MaximumValue)}";
            }
        }

        public int ProgressValue
        {
            get
            {
                return progressValue;
            }

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
            get
            {
                return maximumValue;
            }

            set
            {
                if (maximumValue == value)
                    return;
                maximumValue = value;
                NotifyPropertyChanged();
            }
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
            sets = (from set in sets
                   where set.Code.Equals("MIR") || set.Code.Equals("ORI")
                   select set).ToList();

            MaximumValue = sets.Count;

            CardImageSource imageSource = new DualSource(applicationSettings.ImagesFolder);
            var imageHashing = new pHash();


            Parallel.ForEach(sets, set =>
            {
                try
                {
                    var cards = remoteCardData.GetCards(set);
                    localCardData.AddSet(set);

                    Parallel.ForEach(cards, card =>
                    {
                        try
                        {
                            var image = imageSource.GetImage(card, set);

                            if (image != null)
                                card.pHash = imageHashing.ImageHash(image);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.WriteLine($"CARD={card.Name};SET={set.Code};EXCEPTION={ex.GetType()},{ex.Message}");
                        }

                        try
                        {
                            localCardData.AddCard(card, set);
                        }
                        catch (System.Data.SQLite.SQLiteException e4)
                        {
                            Debug.WriteLine($"CARD={card.MultiverseId},{card.Name};SET={set.Code},{set.Name};EXCEPTION={e4.GetType()},{e4.Message}");
                        }
                    });
                }
                catch (Newtonsoft.Json.JsonReaderException e1)
                {
                    Debug.WriteLine($"SET={set.Code},{set.Name};EXCEPTION={e1.GetType()},{e1.Message}");
                }
                catch (System.Net.WebException e2)
                {
                    Debug.WriteLine($"SET={set.Code},{set.Name};EXCEPTION={e2.GetType()},{e2.Message}");
                }
                catch (System.Data.SQLite.SQLiteException e3)
                {
                    Debug.WriteLine($"SET={set.Code},{set.Name};EXCEPTION={e3.GetType()},{e3.Message}");
                }
                finally
                {
                    Interlocked.Increment(ref progressValue);
                }
            });

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
