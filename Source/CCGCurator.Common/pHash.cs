using System.IO;
using System.Drawing;
using Shipwreck.Phash;
using Shipwreck.Phash.Bitmaps;

namespace CCGCurator.ReferenceBuilder
{
    public class pHash
    {
        public string ImageHash(string filename)
        {
            if (!File.Exists(filename))
                return string.Empty;

            var bitmap = (Bitmap)Image.FromFile(filename);
            var hash = ImagePhash.ComputeDigest(bitmap.ToLuminanceImage());
            return hash.ToString();
        }
    }
}
