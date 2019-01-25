using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CCGCurator.Common;
using CCGCurator.Data;

namespace CCGCurator.ReferenceBuilder
{
    internal class DualImageSource : CardImageSource
    {
        private readonly string imagesFolder;
        private readonly DiskImageSource diskSource;
        private readonly WebImageSource webSource;
        private readonly FileSystemHelper fileSystemHelper;

        public DualImageSource(string imagesFolder)
        {
            fileSystemHelper = new FileSystemHelper();
            diskSource = new DiskImageSource(imagesFolder);
            webSource = new WebImageSource();
            this.imagesFolder = imagesFolder;
        }

        internal override Bitmap GetImage(Card card, Set set)
        {
            var result = diskSource.GetImage(card, set);
            if (result != null)
                return result;

            result = webSource.GetImage(card, set);

            if (result != null)
            {
                var setFolder = diskSource.ImagesDirectory(set);
                if (!Directory.Exists(setFolder))
                    Directory.CreateDirectory(setFolder);
                var imagePath = Path.Combine(setFolder, diskSource.ImageFileName(card));
                result.Save(imagePath, ImageFormat.Jpeg);
            }

            return result;
        }
    }
}