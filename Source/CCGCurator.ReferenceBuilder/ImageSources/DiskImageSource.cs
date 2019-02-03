using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CCGCurator.Common;
using CCGCurator.Data.Model;

namespace CCGCurator.ReferenceBuilder.ImageSources
{
    internal class DiskImageSource : CardImageSource
    {
        private readonly FileSystemHelper fileSystemHelper;
        private readonly string imagesFolder;

        public DiskImageSource(string imagesFolder)
        {
            this.imagesFolder = imagesFolder;
            fileSystemHelper = new FileSystemHelper();
        }

        internal override Bitmap GetImage(Card card, Set set)
        {
            var imagePath = FindImage(card, set);

            if (imagePath == null)
                return null;

            return (Bitmap) Image.FromFile(imagePath);
        }

        internal override Bitmap GetImage(Set set, Bitmap missing)
        {
            var imagePath = FindImage(set);

            if (imagePath == null)
            {
                imagePath = MakeSetImagePath(set);

                var setFolder = Path.GetDirectoryName(imagePath);
                if (!Directory.Exists(setFolder))
                    Directory.CreateDirectory(setFolder);

                missing.Save(imagePath, ImageFormat.Png);
                return new Bitmap(missing);
            }

            return (Bitmap)Image.FromFile(imagePath);
        }

        internal string ImagesDirectory(Set set)
        {
            var setFileName = fileSystemHelper.IsInvalidFileName(set.Code) ? "set_" + set.Code : set.Code;
            var setFolder = Path.Combine(imagesFolder, setFileName);
            return setFolder;
        }

        internal string ImageFileName(Card card)
        {
            return card.MultiverseId + ".jpg";
        }

        internal string FindImage(Card card, Set set)
        {
            var setFolder = ImagesDirectory(set);
            var imagePath = Path.Combine(setFolder, ImageFileName(card));
            if (File.Exists(imagePath))
                return imagePath;
            return null;
        }

        internal string FindImage(Set set)
        {
            var imagePath = MakeSetImagePath(set);
            if (File.Exists(imagePath))
                return imagePath;
            return null;
        }

        internal string MakeSetImagePath(Set set)
        {
            var setFolder = ImagesDirectory(set);
            var imagePath = Path.Combine(setFolder, "logo.png");
            return imagePath;
        }
    }
}