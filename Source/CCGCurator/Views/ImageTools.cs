using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using AForge;
using AForge.Imaging.Filters;
using Point = System.Drawing.Point;

namespace CCGCurator.Views
{
    class ImageTools
    {
        private Point[] ToPointsArray(List<IntPoint> points)
        {
            var array = new Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
                array[i] = new Point(points[i].X, points[i].Y);

            return array;
        }

        public Bitmap GreyscaleEdgeDetectionImage(Bitmap bitmap)
        {
            // Greyscale
            var greyscaleImage = Grayscale.CommonAlgorithms.BT709.Apply(bitmap);

            // Contrast - try to sharpen edges
            //ContrastStretch filter = new ContrastStretch();
            //filter.ApplyInPlace(filteredBitmap);

            // edge filter 
            // This filters accepts 8 bpp grayscale images for processing

            //Alternatives:
            //DifferenceEdgeDetector edgeFilter = new DifferenceEdgeDetector();
            //HomogenityEdgeDetector filter = new HomogenityEdgeDetector();
            //CannyEdgeDetector filter = new CannyEdgeDetector( );

            var edgeFilter = new SobelEdgeDetector();
            edgeFilter.ApplyInPlace(greyscaleImage);

            var threshholdFilter = new Threshold(240); //180
            threshholdFilter.ApplyInPlace(greyscaleImage);

            return greyscaleImage;
        }

        public Bitmap DrawDetectionBox(Bitmap captured, List<IdentifiedCard> cards)
        {
            var shapeDetectedImage = new Bitmap(captured.Width, captured.Height, PixelFormat.Format24bppRgb);
            var g = Graphics.FromImage(shapeDetectedImage);
            g.DrawImage(captured, 0, 0);

            var pen = new Pen(Color.Red, 5);

            foreach (var card in cards) g.DrawPolygon(pen, ToPointsArray(card.Corners));

            pen.Dispose();
            g.Dispose();

            return shapeDetectedImage;
        }

        public Bitmap GetDetectedCardImage(List<IntPoint> corners, Bitmap captured, double scaleFactor)
        {
            // Extract the card bitmap

            // Debug
            //var transformFilter = new QuadrilateralTransformation(corners, 600, 800);

            var transformFilter = new QuadrilateralTransformation(corners, Convert.ToInt32(211 * scaleFactor),
                Convert.ToInt32(298 * scaleFactor));

            var cardBitmap = transformFilter.Apply(captured);
            return cardBitmap;
        }

        public Bitmap DrawCardNames(IEnumerable<IdentifiedCard> matches, Bitmap captured)
        {
            var identifiedCards = matches.ToList();
            if (!identifiedCards.Any())
                return captured;

            var resultImage = (Bitmap)captured.Clone();
            var g = Graphics.FromImage(resultImage);
            var font = new Font("Tahoma", 25);
            foreach (var item in identifiedCards)
            {
                var corners = item.Corners;
                var card = item.Card;
                //ContrastCorrection filter = new ContrastCorrection(15);
                //filter.ApplyInPlace(card.cardArtBitmap);
                g.DrawString(card.Name, font, Brushes.Black,
                    new PointF(corners[0].X - 29, corners[0].Y - 39));
                g.DrawString(card.Name, font, Brushes.Red,
                    new PointF(corners[0].X - 30, corners[0].Y - 40));
            }

            g.Dispose();

            return resultImage;
        }
    }
}
