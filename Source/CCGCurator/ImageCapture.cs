using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using DirectX.Capture;

namespace CCGCurator
{
    internal class ImageCapture
    {
        private static readonly object captureThreadLocker = new object();
        private readonly Control captureBox;
        private Capture capture;
        private bool capturing;

        public ImageCapture()
        {
            captureBox = new PictureBox();
        }

        public int RotationDegrees { get; set; }
        public ImageFeed ImageFeed { get; set; }

        public Size FrameSize { get; private set; }

        public static List<ImageFeed> ImageFeeds()
        {
            var cameraFilters = new Filters();
            var imageFeeds = new List<ImageFeed>();
            for (var i = 0; i < cameraFilters.VideoInputDevices.Count; i++)
                imageFeeds.Add(new ImageFeed(cameraFilters.VideoInputDevices[i].Name, i));
            return imageFeeds;
        }

        public void StopCapturing()
        {
            capturing = false;
            if (capture != null)
            {
                capture.Stop();
                capture.PreviewWindow = null;
                capture.FrameEvent2 -= CaptureDone;
                capture.Dispose();
                capture = null;
            }
        }

        public void StartCapturing(ImageFeed feed)
        {
            StopCapturing();
            ImageFeed = feed;
            if (feed == null)
                return;
            var cameraFilters = new Filters();
            capture = new Capture(cameraFilters.VideoInputDevices[feed.FilterIndex],
                cameraFilters.AudioInputDevices[0]);
            if (capture.VideoCaps == null)
                return;

            FrameSize = CalculateCaptureFrameSize(capture.VideoCaps.MaxFrameSize);
            capture.FrameSize = FrameSize;

            capture.PreviewWindow = captureBox;
            capture.FrameEvent2 += CaptureDone;
            capture.GrapImg();
            capturing = true;
        }

        private Size CalculateCaptureFrameSize(Size maxSize)
        {
            var resolutions = new[]
            {
                new Size(1920, 1080),
                new Size(1024, 768),
                new Size(800, 600),
                new Size(640, 480)
            };

            foreach (var resolution in resolutions)
                if (maxSize.Height >= resolution.Height)
                    return resolution;

            return resolutions.Last();
        }

        public event EventHandler<CaptureEvent> ImageCaptured;

        private void CaptureDone(Bitmap captured)
        {
            if (!capturing)
                return;

            lock (captureThreadLocker)
            {
                captured = RotateImage(captured);

                ImageCaptured?.Invoke(this, new CaptureEvent(captured));
            }
        }

        private Bitmap RotateImage(Bitmap original)
        {
            if (RotationDegrees == 0)
                return original;

            var center = new PointF((float) original.Width / 2, (float) original.Height / 2);
            var result = new Bitmap(original.Width, original.Height, original.PixelFormat);

            result.SetResolution(original.HorizontalResolution, original.VerticalResolution);

            using (var g = Graphics.FromImage(result))
            {
                var matrix = new Matrix();

                matrix.RotateAt(RotationDegrees, center);

                g.Transform = matrix;
                g.DrawImage(original, new Point());
            }

            return result;
        }
    }

    internal class CaptureEvent : EventArgs
    {
        public CaptureEvent(Bitmap capturedImage)
        {
            CapturedImage = capturedImage;
        }

        public Bitmap CapturedImage { get; }
    }
}