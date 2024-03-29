﻿using System.Collections.Generic;
using System.Threading;
using CCGCurator.Data;
using CCGCurator.Data.Model;
using CCGCurator.Data.ReferenceData;
using CCGCurator.ReferenceBuilder.Actions;
using CCGCurator.ReferenceBuilder.Data;
using CCGCurator.ReferenceBuilder.ImageSources;

namespace CCGCurator.ReferenceBuilder
{
    class ReferenceBuilderWorker
    {
        private int currentItem;

        public int CurrentItem => currentItem;

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
    }
}