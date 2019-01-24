using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using CCGCurator.Common;
using CCGCurator.Data;
using Point = System.Drawing.Point;

namespace CCGCurator
{
    internal class CardDetection
    {
        private readonly List<Card> referenceCards;
        private Card bestMatch;

        // detecting matrix, stores detected cards to avoid fail detection
        private readonly Dictionary<string, int> bestMatches = new Dictionary<string, int>();

        private readonly double fScaleFactor;

        public CardDetection(double fScaleFactor, List<Card> referenceCards)
        {
            this.fScaleFactor = fScaleFactor;
            this.referenceCards = referenceCards;
        }

        public double BlobHeight { get; set; } = 225;
        public double BlobWidth { get; set; } = 125;
        public double DetectionThreshold { get; set; } = 10000;

        private List<MagicCard> DetectCards(Bitmap bitmap, out Bitmap detectionImage)
        {
            List<MagicCard> magicCards = new List<MagicCard>();
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
                MinHeight = Convert.ToInt32(BlobHeight * fScaleFactor),
                MinWidth = Convert.ToInt32(BlobWidth * fScaleFactor)
            };

            blobCounter.ProcessImage(bitmapData);
            var blobs = blobCounter.GetObjectsInformation();

            greyscaleImage.UnlockBits(bitmapData);

            var shapeChecker = new SimpleShapeChecker();

            var cardPositions = new List<IntPoint>();


            // Loop through detected shapes
            for (int i = 0; i < blobs.Length; i++)
            {
                var edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                List<IntPoint> corners;

                // is triangle or quadrilateral
                if (!shapeChecker.IsConvexPolygon(edgePoints, out corners))
                    continue;

                var subType = shapeChecker.CheckPolygonSubType(corners);

                // Only return 4 corner rectanges
                if ((subType != PolygonSubType.Parallelogram && subType != PolygonSubType.Rectangle) ||
                    corners.Count != 4) continue;

                corners = RotateCard(corners);

                // Prevent it from detecting the same card twice
                if(cardPositions.Any(point => corners[0].DistanceTo(point) < Convert.ToInt32(40 * fScaleFactor)))
                    continue;

                // Hack to prevent it from detecting smaller sections of the card instead of the whole card
                if (GetArea(corners) < Convert.ToInt32(DetectionThreshold * fScaleFactor)) //fScaleFactor
                    continue;

                cardPositions.Add(corners[0]);

                // Extract the card bitmap

                // Debug
                //var transformFilter = new QuadrilateralTransformation(corners, 600, 800);

                var transformFilter = new QuadrilateralTransformation(corners, Convert.ToInt32(211 * fScaleFactor), Convert.ToInt32(298 * fScaleFactor));

                var cardBitmap = transformFilter.Apply(bitmap);

                var card = new MagicCard
                {
                    corners = corners,
                    cardBitmap = cardBitmap
                };

                magicCards.Add(card);
            }

            detectionImage = CreateDetectionImage(greyscaleImage, magicCards);
            return magicCards;
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


        private Bitmap matchCard(List<MagicCard> magicCards, Bitmap captured)
        {
            var cameraBitmap = captured;
            //Console.WriteLine("matchCard() called with " +  magicCards.Count + " cards detected");

            var cardTempId = 0;
            var Phash = new pHash();

            foreach (var card in magicCards)
            {
                cardTempId++;

                //ContrastCorrection filter = new ContrastCorrection(15);
                //filter.ApplyInPlace(card.cardArtBitmap);

                // Write the image to disk to be read by the pHash library.. should really find
                // a way to pass a pointer to image data directly
                //card.cardBitmap.Save("tempCard" + cardTempId + ".jpg", ImageFormat.Jpeg);


                // Phash.ph_dct_imagehash("tempCard" + cardTempId + ".jpg", ref cardHash);
                //var cardHash = Phash.ImageHash(".\\tempCard" + cardTempId + ".jpg");
                var cardHash = Phash.ImageHash(card.cardBitmap);
                var lowestHamming = int.MaxValue;

                foreach (var referenceCard in referenceCards)
                {
                    var hamming = Phash.HammingDistance(referenceCard.pHash, cardHash);
                    if (hamming < lowestHamming)
                    {
                        lowestHamming = hamming;
                        bestMatch = referenceCard;
                    }
                }

                if (bestMatch != null)
                {
                    var g = Graphics.FromImage(captured);
                    g.DrawString(bestMatch.Name, new Font("Tahoma", 25), Brushes.Black,
                        new PointF(card.corners[0].X - 29, card.corners[0].Y - 39));
                    g.DrawString(bestMatch.Name, new Font("Tahoma", 25), Brushes.Red,
                        new PointF(card.corners[0].X - 30, card.corners[0].Y - 40));
                    g.Dispose();

                    if (bestMatches.ContainsKey(bestMatch.UUID))
                        bestMatches[bestMatch.UUID] += 1;
                    else
                        bestMatches[bestMatch.UUID] = 1;


                    var maxValue = 0;
                    string bestMatchId = null;

                    foreach (var match in bestMatches)
                        if (match.Value > maxValue)
                        {
                            maxValue = match.Value;
                            bestMatchId = match.Key;
                        }

                    if (bestMatchId != bestMatch.UUID)
                        continue;
                }

                if (bestMatch != null)
                {
                    card.referenceCard = bestMatch;


                    // highly experimental

#if NULL
                    const string tessDataDir = @".\\tessdata";

                    using (var engine = new TesseractEngine(tessDataDir, "eng", EngineMode.Default))
                    using (var image = Pix.LoadFromFile(".\\tempCard" + cardTempId + ".jpg"))
                    using (var page = engine.Process(image))
                    {
                        string text = page.GetText();
                        Console.WriteLine("DEBUG: Mean confidence: {0}", page.GetMeanConfidence());
                        Console.WriteLine("DEBUG: "+ text);
                    }
#endif
                }
            }

            return cameraBitmap;
        }

        public Card Process(Bitmap captured, out Bitmap greyscaleDetectedImage, out Bitmap previewImage)
        {
            var cards = DetectCards(captured, out greyscaleDetectedImage);
            previewImage = matchCard(cards, captured);

            return bestMatch;
        }

        private Bitmap CreateDetectionImage(Bitmap captured, List<MagicCard> cards)
        {
            var shapeDetectedImage = new Bitmap(captured.Width, captured.Height, PixelFormat.Format24bppRgb);
            var g = Graphics.FromImage(shapeDetectedImage);
            g.DrawImage(captured, 0, 0);

            var pen = new Pen(Color.Red, 5);

            foreach (var card in cards)
            {
                g.DrawPolygon(pen, ToPointsArray(card.corners));
            }

            pen.Dispose();
            g.Dispose();

            return shapeDetectedImage;
        }

        internal class MagicCard
        {
            public Bitmap cardBitmap;
            public List<IntPoint> corners;
            public Card referenceCard;
        }
    }
}