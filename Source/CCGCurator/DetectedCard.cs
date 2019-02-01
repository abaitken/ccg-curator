using System.Collections.Generic;
using System.Drawing;
using AForge;

namespace CCGCurator
{
    internal class DetectedCard
    {
        public Bitmap CardBitmap { get; set; }
        public List<IntPoint> Corners { get; set; }
    }
}