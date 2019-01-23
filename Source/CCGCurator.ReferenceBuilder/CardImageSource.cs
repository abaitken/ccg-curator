using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using CCGCurator.Common;
using CCGCurator.Data;

namespace CCGCurator.ReferenceBuilder
{
    abstract class CardImageSource
    {
        internal abstract Bitmap GetImage(Card card, Set set);
    }
    class DiskImageSource : CardImageSource
    {
        private string imagesFolder;
        private FileSystemHelper fileSystemHelper;

        public DiskImageSource(string imagesFolder)
        {
            this.imagesFolder = imagesFolder;
            fileSystemHelper = new FileSystemHelper();
        }

        internal override Bitmap GetImage(Card card, Set set)
        {
            var setFileName = fileSystemHelper.IsInvalidFileName(set.Code) ? "set_" + set.Code : set.Code;

            return
                LoadImage(Path.Combine(imagesFolder, card.MultiverseId + ".jpg")) ??
                LoadImage(Path.Combine(imagesFolder, setFileName, card.Name + ".full.jpg")) ??                   
                null;
        }

        private Bitmap LoadImage(string imagePath)
        {
            if (File.Exists(imagePath))
                return (Bitmap)Image.FromFile(imagePath);
            return null;
        }

        internal string FindImage(Card card, Set set)
        {
            var setFileName = fileSystemHelper.IsInvalidFileName(set.Code) ? "set_" + set.Code : set.Code;

            var imagePath = Path.Combine(imagesFolder, card.MultiverseId + ".jpg");
            if (File.Exists(imagePath))
                return imagePath;
            imagePath = Path.Combine(imagesFolder, setFileName, card.Name + ".full.jpg");
            if (File.Exists(imagePath))
                return imagePath;
                return null;
        }
    }

    class WebImageSource : CardImageSource
    {
        internal override Bitmap GetImage(Card card, Set set)
        {
            WebRequest request = WebRequest.Create($"http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={card.MultiverseId}&type=card");
            WebResponse resp = request.GetResponse();
            Stream responseStream = resp.GetResponseStream();
            Bitmap bitmap = new Bitmap(responseStream);
            responseStream.Dispose();
            return bitmap;
        }
    }

    class DualSource : CardImageSource
    {
        private readonly string imagesFolder;
        private DiskImageSource diskSource;
        private WebImageSource webSource;

        public DualSource(string imagesFolder)
        {
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
            
            if(result != null)
            {
                var imagePath = Path.Combine(imagesFolder, card.MultiverseId + ".jpg");
                result.Save(imagePath, ImageFormat.Jpeg);
            }

            return result;
        }
    }
}
