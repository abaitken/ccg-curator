using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CCGCurator.Data.Model;

namespace CCGCurator.ReferenceBuilder.ImageSources
{
    internal class DualImageSource : CardImageSource
    {
        private readonly DiskImageSource diskSource;
        private readonly WebImageSource webSource;

        public DualImageSource(string imagesFolder)
        {
            diskSource = new DiskImageSource(imagesFolder);
            webSource = new WebImageSource();
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

        internal override Bitmap GetImage(Set set, Bitmap missing)
        {
            var result = diskSource.GetImage(set, missing);
            if (result != null)
                return result;

            result = webSource.GetImage(set, missing);

            if (result != null)
            {
                var imagePath = diskSource.MakeSetImagePath(set);

                var setFolder = Path.GetDirectoryName(imagePath);
                if (!Directory.Exists(setFolder))
                    Directory.CreateDirectory(setFolder);

                result.Save(imagePath, ImageFormat.Png);
            }

            return result;
        }
    }
}