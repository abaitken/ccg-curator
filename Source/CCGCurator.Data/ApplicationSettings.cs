using System;
using System.IO;

namespace CCGCurator.Data
{
    public class ApplicationSettings
    {
        public ApplicationSettings()
        {
            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);
        }

        public string DataFolder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CCGCurator");
            }
        }

        public string DatabasePath
        {
            get
            {
                return Path.Combine(DataFolder, "data.sqlite");
            }
        }
    }
}
