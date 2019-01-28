using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace CCGCurator
{
    internal static class ImageExtensions
    {
        public static BitmapImage Convert(this Bitmap source)
        {
            using(MemoryStream memory = new MemoryStream())
            {
                source.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }
    }
}