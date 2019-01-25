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

        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public string DataFolder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CCGCurator");
            }
        }

        public string CardDataPath
        {
            get
            {
                return Path.Combine(DataFolder, "cards.sqlite");
            }
        }

        public string CatalogDataPath
        {
            get
            {
                return Path.Combine(DataFolder, "catalog.sqlite");
            }
        }

        public string SetDataCache
        {
            get
            {
                return Path.Combine(DataFolder, "SetDataCache");
            }
        }

        public string DefaultImageCacheFolder
        {
            get
            {
                return Path.Combine(DataFolder, "ImageCache");
            }
        }
    }
}
