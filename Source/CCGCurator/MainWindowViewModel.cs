using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using CCGCurator.Common;
using CCGCurator.Data;
using DirectX.Capture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CCGCurator
{
    class MainWindowViewModel : ViewModel, IDisposable
    {
        private ImageFeed selectedImageFeed;
        private bool viewLoaded;
        private PictureBox previewBox;
        private PictureBox filteredBox;
        private Filters cameraFilters;
        private IEnumerable<ImageFeed> imageFeeds;
        private Bitmap cameraBitmap;
        private Capture capture;
        private double fScaleFactor;

        internal void Closing()
        {
            capture.Stop();
            capture.PreviewWindow = null;
        }

        Control captureBox;

        public IEnumerable<ImageFeed> ImageFeeds
        {
            get
            {
                return imageFeeds;
            }
            set
            {
                imageFeeds = value;
                NotifyPropertyChanged();
            }
        }

        internal void ViewLoaded(PictureBox previewBox, PictureBox filteredBox)
        {
            if (viewLoaded)
                return;

            viewLoaded = true;
            this.previewBox = previewBox;
            this.filteredBox = filteredBox;
            this.captureBox = new PictureBox();
            loadSourceCards();
            cameraFilters = new Filters();
            var imageFeeds = new List<ImageFeed>();
            for (int i = 0; i < cameraFilters.VideoInputDevices.Count; i++)
            {
                imageFeeds.Add(new ImageFeed(cameraFilters.VideoInputDevices[i].Name, i));
            }
            ImageFeeds = imageFeeds;
            SelectedImageFeed = imageFeeds[2];
        }

        public ImageFeed SelectedImageFeed
        {
            get { return selectedImageFeed; }
            set
            {
                if (selectedImageFeed == value)
                    return;
                selectedImageFeed = value;
                NotifyPropertyChanged();
                ImageFeedHasChanged();
            }
        }

        public double BlobHeight => 225;
        public double BlobWidth => 125;

        public double DectectionThreshold => 10000;

        private void ImageFeedHasChanged()
        {
            cameraBitmap = new Bitmap(800, 600);

            capture = new Capture(cameraFilters.VideoInputDevices[SelectedImageFeed.FilterIndex], cameraFilters.AudioInputDevices[0]);

            var maxSize = capture.VideoCaps.MaxFrameSize;

            capture.FrameSize = new Size(640, 480);

            if (maxSize.Height > 480)
                capture.FrameSize = new Size(800, 600);

            if (maxSize.Height >= 768)
                capture.FrameSize = new Size(1024, 768);

            fScaleFactor = Convert.ToDouble(capture.FrameSize.Height) / 480;
            capture.PreviewWindow = captureBox;
            capture.FrameEvent2 += CaptureDone;
            capture.GrapImg();
        }

        private static readonly object _locker = new object();
        private Bitmap cameraBitmapLive;


        private void detectQuads(Bitmap bitmap)
        {
            // Greyscale
            filteredBitmap = Grayscale.CommonAlgorithms.BT709.Apply(bitmap);

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
            edgeFilter.ApplyInPlace(filteredBitmap);

            // Threshhold filter
            var threshholdFilter = new Threshold(240); //180
            threshholdFilter.ApplyInPlace(filteredBitmap);

            var bitmapData = filteredBitmap.LockBits(
                new Rectangle(0, 0, filteredBitmap.Width, filteredBitmap.Height),
                ImageLockMode.ReadWrite, filteredBitmap.PixelFormat);

            var blobCounter = new BlobCounter();

            blobCounter.FilterBlobs = true;

            //possible finetuning

            blobCounter.MinHeight = Convert.ToInt32(BlobHeight * fScaleFactor); //fScaleFactor
            blobCounter.MinWidth = Convert.ToInt32(BlobWidth * fScaleFactor); //fScaleFactor

//#if DEBUG
//            Console.WriteLine("Calculate min blobsize " + blobCounter.MinWidth + "/" + blobCounter.MinHeight);
//#endif

            blobCounter.ProcessImage(bitmapData);
            var blobs = blobCounter.GetObjectsInformation();
            filteredBitmap.UnlockBits(bitmapData);

            var shapeChecker = new SimpleShapeChecker();

            var bm = new Bitmap(filteredBitmap.Width, filteredBitmap.Height, PixelFormat.Format24bppRgb);

            var g = Graphics.FromImage(bm);
            g.DrawImage(filteredBitmap, 0, 0);

            var pen = new Pen(Color.Red, 5);
            var cardPositions = new List<IntPoint>();


            // Loop through detected shapes
            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                var edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                List<IntPoint> corners;
                var sameCard = false;

                // is triangle or quadrilateral
                if (shapeChecker.IsConvexPolygon(edgePoints, out corners))
                {
                    // get sub-type
                    var subType = shapeChecker.CheckPolygonSubType(corners);

                    // Only return 4 corner rectanges
                    if ((subType == PolygonSubType.Parallelogram || subType == PolygonSubType.Rectangle) &&
                        corners.Count == 4)
                    {
                        // Check if its sideways, if so rearrange the corners so it's veritcal
                        rearrangeCorners(corners);

                        // Prevent it from detecting the same card twice
                        foreach (var point in cardPositions)
                            if (corners[0].DistanceTo(point) < Convert.ToInt32(40 * fScaleFactor)) //fScaleFactor
                                sameCard = true;

                        if (sameCard)
                            continue;

                        /*
                         *  This code seems to have an issue if scaled up with the factor:
                         */

                        // Hack to prevent it from detecting smaller sections of the card instead of the whole card
                        if (GetArea(corners) < Convert.ToInt32(DectectionThreshold * fScaleFactor)
                        ) //fScaleFactor
                            continue;

                        cardPositions.Add(corners[0]);

                        g.DrawPolygon(pen, ToPointsArray(corners));

                        // Extract the card bitmap

                        // Debug
                        //var transformFilter = new QuadrilateralTransformation(corners, 600, 800);

                        var transformFilter = new QuadrilateralTransformation(corners,
                            Convert.ToInt32(211 * fScaleFactor), Convert.ToInt32(298 * fScaleFactor));

                        var cardBitmap = transformFilter.Apply(cameraBitmap);

                        var card = new MagicCard();
                        card.corners = corners;
                        card.cardBitmap = cardBitmap;

                        magicCards.Add(card);

                        pen.Dispose();
                        g.Dispose();

                        filteredBitmap = bm;

                        return;
                    }
                }
            }

            pen.Dispose();
            g.Dispose();

            filteredBitmap = bm;
        }
        private readonly List<MagicCard> magicCards = new List<MagicCard>();

        internal class MagicCard
        {
            public Bitmap cardBitmap;
            public List<IntPoint> corners;
            public Card referenceCard;
        }

        // Move the corners a fixed amount
        /*       private void shiftCorners(List<IntPoint> corners, IntPoint point)
        {
            var xOffset = point.X - corners[0].X;
            var yOffset = point.Y - corners[0].Y;

            for (var x = 0; x < corners.Count; x++)
            {
                var point2 = corners[x];

                point2.X += xOffset;
                point2.Y += yOffset;

                corners[x] = point2;
            }
        }*/

        // Conver list of AForge.NET's points to array of .NET points
        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            var array = new System.Drawing.Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);

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


        private void rearrangeCorners(List<IntPoint> corners)
        {
            var pointDistances = new float[4];

            for (var x = 0; x < corners.Count; x++)
            {
                var point = corners[x];

                pointDistances[x] = point.DistanceTo(x == corners.Count - 1 ? corners[0] : corners[x + 1]);
            }

            var shortestDist = float.MaxValue;
            var shortestSide = int.MaxValue;

            for (var x = 0; x < corners.Count; x++)
                if (pointDistances[x] < shortestDist)
                {
                    shortestSide = x;
                    shortestDist = pointDistances[x];
                }

            if (shortestSide != 0 && shortestSide != 2)
            {
                var endPoint = corners[0];
                corners.RemoveAt(0);
                corners.Add(endPoint);
            }
        }

        private void CaptureDone(Bitmap captured)
        {
            //Debug.WriteLine("CaptureDone() called");

            lock (_locker)
            {
                magicCards.Clear();
                cameraBitmap = captured;
                cameraBitmapLive = (Bitmap)cameraBitmap.Clone();
                detectQuads(cameraBitmap);
                matchCard();

                filteredBox.Image = filteredBitmap;
                previewBox.Image = cameraBitmap;
            }
        }

        private void loadSourceCards()
        {
            var localCardData = new LocalCardData(new Data.ApplicationSettings().DatabasePath);
            referenceCards = new List<Card>(localCardData.GetCardsWithHashes());
        }

        private void matchCard()
        {
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
                Card bestMatch = null;

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
                    var g = Graphics.FromImage(cameraBitmap);
                    g.DrawString(currentMatch, new Font("Tahoma", 25), Brushes.Black,
                        new PointF(card.corners[0].X - 29, card.corners[0].Y - 39));
                    g.DrawString(currentMatch, new Font("Tahoma", 25), Brushes.Red,
                        new PointF(card.corners[0].X - 30, card.corners[0].Y - 40));
                    g.Dispose();


//#if DEBUG
//                    Console.WriteLine("DEBUG: Highest Similarity: " + bestMatch.Name + " ID: " + bestMatch.UUID);
//#endif

                    if (bestMatches.ContainsKey(bestMatch.UUID))
                        bestMatches[bestMatch.UUID] += 1;
                    else
                        bestMatches[bestMatch.UUID] = 1;

                    //Console.WriteLine("DEBUG: Checking " + bestMatches[bestMatch.cardId]);


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

                    currentMatch = bestMatch.Name;


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
        }
        public List<Card> referenceCards = new List<Card>();
        // detecting matrix, stores detected cards to avoid fail detection
        private Dictionary<string, int> bestMatches = new Dictionary<string, int>();

        private string currentMatch = "";

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private Bitmap filteredBitmap;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    capture = null;
                    captureBox.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MainWindowViewModel() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
