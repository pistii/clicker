using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Drawing;
using System.ComponentModel;
namespace Kattolgatos
{
    public class ScreenCapture : IDisposable
    {
        private bool disposed = false;
        private Component component = new Component();
        private IntPtr handle;

        ushort[] arr = new ushort[2];
        public bool _IsPictureInPictureDetected(string imagePath, string targetImagePath, double threshold)
        {
            // Load the images
            Mat image = CvInvoke.Imread(imagePath, ImreadModes.Color);
            Mat targetImage = CvInvoke.Imread(targetImagePath, ImreadModes.Color);

            // Convert the images to grayscale
            Mat grayImage = new Mat();
            Mat grayTargetImage = new Mat();
            CvInvoke.CvtColor(image, grayImage, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(targetImage, grayTargetImage, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            // Get the dimensions of the images
            int imageWidth = grayImage.Width;
            int imageHeight = grayImage.Height;
            int targetWidth = grayTargetImage.Width;
            int targetHeight = grayTargetImage.Height;

            // Perform sliding window search
            for (int y = 0; y < imageHeight - targetHeight; y++)
            {
                for (int x = 0; x < imageWidth - targetWidth; x++)
                {
                    // Extract the region of interest (ROI) from the main image
                    Rectangle roi = new Rectangle(x, y, targetWidth, targetHeight);
                    Mat roiImage = new Mat(grayImage, roi);

                    // Perform template matching on the ROI
                    Mat result = new Mat();
                    CvInvoke.MatchTemplate(roiImage, grayTargetImage, result, TemplateMatchingType.CcoeffNormed);

                    // Find the best match location and value
                    double minValue = 0.0d;
                    double maxValue = 0.85d;

                    Point minLocation = new Point();
                    Point maxLocation = new Point();
                    CvInvoke.MinMaxLoc(result, ref minValue, ref maxValue, ref minLocation, ref maxLocation);



                    // Check if the best match value exceeds the threshold
                    if (maxValue >= threshold)
                    {
                        // Picture-in-picture is detected
                        return true;
                    }
                }
            }

            // Picture-in-picture is not detected
            return false;
        }

        public ushort[] FindPicture(string targetImagePath, double threshold, Rectangle screenRegion)
        {
            // Capture a screenshot of the specified screen region
            Bitmap screenshot = new Bitmap(screenRegion.Width, screenRegion.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(screenRegion.Location, Point.Empty, screenRegion.Size);
            }

            // Convert the screenshot to an Emgu.CV image
            Image<Bgr, byte> emguImage = screenshot.ToImage<Bgr, byte>();

            // Load the target image
            Image<Bgr, byte> targetImage = new Image<Bgr, byte>(targetImagePath);

            // Convert the images to grayscale
            Image<Gray, byte> grayScreenImage = emguImage.Convert<Gray, byte>();
            Image<Gray, byte> grayTargetImage = targetImage.Convert<Gray, byte>();
                        
            // Perform template matching
            using (Image<Gray, float> result = grayScreenImage.MatchTemplate(grayTargetImage, TemplateMatchingType.CcoeffNormed))
            {
                float[,,] matches = result.Data;

                // Check if any match exceeds the threshold
                for (ushort row = 0; row < matches.GetLength(0); row++)
                {
                    for (ushort col = 0; col < matches.GetLength(1); col++)
                    {
                        double matchValue = matches[row, col, 0];
                        
                        if (matchValue >= threshold)
                        {
                            // Picture-in-picture is detected
                            arr[0] = row;
                            arr[1] = col;
                            return arr;
                        }
                    }
                }
            }

            // Picture-in-picture is not detected
            return null;
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    component.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                CloseHandle(handle);
                handle = IntPtr.Zero;

                // Note disposing has been done.
                disposed = true;
            }
        }

        [System.Runtime.InteropServices.DllImport("Kernel32")]
        private extern static Boolean CloseHandle(IntPtr handle);

        // Use C# finalizer syntax for finalization code.
        // This finalizer will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide finalizer in types derived from this class.
        ~ScreenCapture()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(disposing: false) is optimal in terms of
            // readability and maintainability.
            Dispose(disposing: false);
        }
    }
}
