using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using Point = System.Drawing.Point;

namespace CCGCurator
{
    internal class CardDetection
    {
        private readonly double scaleFactor;

        public CardDetection(double scaleFactor)
        {
            this.scaleFactor = scaleFactor;
        }

        public double BlobHeight { get; set; } = 225;
        public double BlobWidth { get; set; } = 125;
        public double DetectionThreshold { get; set; } = 10000;

        public List<DetectedCard> Detect(Bitmap bitmap, out Bitmap detectionImage)
        {
            var detectedCards = new List<DetectedCard>();
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

            var bitmapData = greyscaleImage.LockBits(
                new Rectangle(0, 0, greyscaleImage.Width, greyscaleImage.Height),
                ImageLockMode.ReadWrite, greyscaleImage.PixelFormat);

            var blobCounter = new BlobCounter
            {
                FilterBlobs = true,
                MinHeight = Convert.ToInt32(BlobHeight * scaleFactor),
                MinWidth = Convert.ToInt32(BlobWidth * scaleFactor)
            };

            blobCounter.ProcessImage(bitmapData);
            var blobs = blobCounter.GetObjectsInformation();

            greyscaleImage.UnlockBits(bitmapData);

            var shapeChecker = new SimpleShapeChecker();

            var cardPositions = new List<IntPoint>();


            // Loop through detected shapes
            for (var i = 0; i < blobs.Length; i++)
            {
                var edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                List<IntPoint> corners;

                // is triangle or quadrilateral
                if (!shapeChecker.IsConvexPolygon(edgePoints, out corners))
                    continue;

                var subType = shapeChecker.CheckPolygonSubType(corners);

                // Only return 4 corner rectanges
                if (subType != PolygonSubType.Parallelogram && subType != PolygonSubType.Rectangle ||
                    corners.Count != 4) continue;

                corners = RotateCard(corners);

                // Prevent it from detecting the same card twice
                if (cardPositions.Any(point => corners[0].DistanceTo(point) < Convert.ToInt32(40 * scaleFactor)))
                    continue;

                // Hack to prevent it from detecting smaller sections of the card instead of the whole card
                if (GetArea(corners) < Convert.ToInt32(DetectionThreshold * scaleFactor))
                    continue;

                cardPositions.Add(corners[0]);

                // Extract the card bitmap

                // Debug
                //var transformFilter = new QuadrilateralTransformation(corners, 600, 800);

                var transformFilter = new QuadrilateralTransformation(corners, Convert.ToInt32(211 * scaleFactor),
                    Convert.ToInt32(298 * scaleFactor));

                var cardBitmap = transformFilter.Apply(bitmap);

                var card = new DetectedCard
                {
                    Corners = corners,
                    CardBitmap = cardBitmap
                };

                detectedCards.Add(card);
            }

            detectionImage = CreateDetectionImage(greyscaleImage, detectedCards);
            return detectedCards;
        }

        private Point[] ToPointsArray(List<IntPoint> points)
        {
            var array = new Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
                array[i] = new Point(points[i].X, points[i].Y);

            return array;
        }

        private double GetArea(IList<IntPoint> vertices)
        {
            if (vertices.Count < 3)
                return 0;
            var area = GetDeterminant(vertices[vertices.Count - 1].X, vertices[vertices.Count - 1].Y, vertices[0].X,
                vertices[0].Y);
            for (var i = 1; i < vertices.Count; i++)
                area += GetDeterminant(vertices[i - 1].X, vertices[i - 1].Y, vertices[i].X, vertices[i].Y);
            return area / 2;
        }

        private double GetDeterminant(double x1, double y1, double x2, double y2)
        {
            return x1 * y2 - x2 * y1;
        }


        private List<IntPoint> RotateCard(List<IntPoint> corners)
        {
            var result = new List<IntPoint>(corners);

            var pointDistances = new float[4];

            for (var x = 0; x < result.Count; x++)
            {
                var point = result[x];

                pointDistances[x] = point.DistanceTo(x == result.Count - 1 ? result[0] : result[x + 1]);
            }

            var shortestDist = float.MaxValue;
            var shortestSide = int.MaxValue;

            for (var x = 0; x < result.Count; x++)
                if (pointDistances[x] < shortestDist)
                {
                    shortestSide = x;
                    shortestDist = pointDistances[x];
                }

            if (shortestSide != 0 && shortestSide != 2)
            {
                var endPoint = result[0];
                result.RemoveAt(0);
                result.Add(endPoint);
            }

            return result;
        }

        private Bitmap CreateDetectionImage(Bitmap captured, List<DetectedCard> cards)
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
    }
}