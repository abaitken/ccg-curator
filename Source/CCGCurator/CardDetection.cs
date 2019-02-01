using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using CCGCurator.Views;
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
            var imageTools = new ImageTools();
            var detectedCards = new List<DetectedCard>();
            // Greyscale
            var greyscaleImage = imageTools.GreyscaleEdgeDetectionImage(bitmap);

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

                // is triangle or quadrilateral
                if (!shapeChecker.IsConvexPolygon(edgePoints, out var corners))
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
                
                var cardBitmap = imageTools.GetDetectedCardImage(corners, bitmap, scaleFactor);

                var card = new DetectedCard
                {
                    Corners = corners,
                    CardBitmap = cardBitmap
                };

                detectedCards.Add(card);
            }

            detectionImage = greyscaleImage;
            return detectedCards;
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
    }
}