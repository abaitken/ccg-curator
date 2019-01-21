using CCGCurator.Common;
using CCGCurator.Data;
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
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ProgressValue));
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
            var fileSystemHelper = new FileSystemHelper();

            //var sets = remoteCardData.GetSets();
            var sets = new List<Set>
            {
                new Set("ORI", "Origins", 0)
            };

            MaximumValue = sets.Count;

            var imageHashing = new pHash();


            Parallel.ForEach(sets, set =>
            {
                try
                {
                    var cards = remoteCardData.GetCards(set);
                    localCardData.AddSet(set);
                    var setFileName = fileSystemHelper.IsInvalidFileName(set.Code) ? "set_" + set.Code : set.Code;

                    Parallel.ForEach(cards, card =>
                    {
                        try
                        {
                            var imagePath = Path.Combine(applicationSettings.ImagesFolder, setFileName, card.Name + ".full.jpg");
                            card.pHash = imageHashing.ImageHash(imagePath);
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
            if (!worker.IsBusy)
            {
                ProgressValue = 0;
                worker.RunWorkerAsync();
                timer.Start();
            }
        }
    }
}
