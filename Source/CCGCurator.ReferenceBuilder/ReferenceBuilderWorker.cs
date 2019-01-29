using System.Collections.Generic;
using System.IO;
using System.Threading;
using CCGCurator.Data;

namespace CCGCurator.ReferenceBuilder
{
    class ReferenceBuilderWorker
    {
        private int currentItem;

        public void DoWork(IList<Set> sets, IList<DataAction> actions, string imageCacheFolder)
        {
            var applicationSettings = new ApplicationSettings();

            var localCardData = new LocalCardData(applicationSettings.DetectionDataPath);
            var remoteCardData = new RemoteCardData(new RemoteDataFileClient(applicationSettings));
            
            var imageSource = new DualImageSource(imageCacheFolder);
            var logger = new Logging();

            Synchronous.ForEach(actions, action =>
            {
                try
                {
                    action.Execute(localCardData, logger, imageSource, remoteCardData);
                }
                finally
                {
                    Interlocked.Increment(ref currentItem);
                }
            });

            logger.Close();
            localCardData.Close();
        }

        public int CurrentItem => currentItem;
    }
}
