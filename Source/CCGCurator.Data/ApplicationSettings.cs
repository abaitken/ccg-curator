using System;
using System.IO;

namespace CCGCurator.Data
{
    public class ApplicationSettings
    {
        public ApplicationSettings()
        {
            CreateDirectory(DataFolder);
            CreateDirectory(SetDataCache);
            CreateDirectory(DefaultImageCacheFolder);
        }

        public string DataFolder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CCGCurator");
            }
        }

        public string DetectionDataPath
        {
            get { return Path.Combine(DataFolder, "detectiondata.sqlite"); }
        }

        public string CollectionDataPath
        {
            get { return Path.Combine(DataFolder, "collection.sqlite"); }
        }

        public string SetDataCache
        {
            get { return Path.Combine(DataFolder, "SetDataCache"); }
        }

        public string DefaultImageCacheFolder
        {
            get { return Path.Combine(DataFolder, "ImageCache"); }
        }

        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}