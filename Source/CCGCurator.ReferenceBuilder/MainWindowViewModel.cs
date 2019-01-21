using CCGCurator.Common;
using CCGCurator.Data;
using System.Collections.Generic;

namespace CCGCurator.ReferenceBuilder
{
    class MainWindowViewModel : ViewModel
    {
        private readonly LocalCardData localCardData;
        private readonly RemoteCardData remoteCardData;

        public MainWindowViewModel()
        {
            var applicationSettings = new ApplicationSettings();
            localCardData = new LocalCardData(applicationSettings.DatabasePath);
            remoteCardData = new RemoteCardData();
        }

        public IEnumerable<Set> Sets
        {
            get
            {
                return remoteCardData.GetSets();
            }
        }
    }
}
