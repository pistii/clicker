using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Imaging;
using static Emgu.CV.ML.KNearest;
using System.IO;

namespace Kattolgatos
{
    public class ScreenCapture : IDisposable
    {
        private bool disposed = false;
        private Component component = new Component();
        private IntPtr handle;

        ushort[] arr = new ushort[2];

        public Rectangle Bounds { get; private set; }
        

        public ushort[] FindPicture(string targetImagePath, double threshold, Rectangle screenRegion)
        {
            // Capture a screenshot of the specified screen region

            try
            {

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    //MessageBox.Show("memorystream kezdet");

                    using (Bitmap screenshot = new Bitmap(screenRegion.Width, screenRegion.Height))
                    {


                        using (Graphics graphics = Graphics.FromImage(screenshot))
                        {
                            graphics.CopyFromScreen(screenRegion.Location, Point.Empty, screenRegion.Size);


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
                                            arr[0] = col;
                                            arr[1] = row;
                                            return arr;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //MessageBox.Show("memorystream vége");


                }
            }
            catch (Exception ex)
            {
                
                return null;
            }

            
            // Picture-in-picture is not detected
            return null;
        }


        public void CaptureMyScreen(bool fullscreen)
        {
            if (fullscreen)
            {
                Rectangle rect = Screen.GetBounds(Point.Empty);
                using (Bitmap bitmap = new Bitmap(rect.Width, rect.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, rect.Size);
                    }
                    // select the save location of the captured screenshot
                    bitmap.Save(@"C:/Users/Killakikitt/Desktop/Captures/Capture.png", ImageFormat.Png);

                }
            }
            else
            {
                Rectangle bounds = new Rectangle() { Height = 200, Width = 100, X = 0, Y = 0};
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                    }
                    bitmap.Save("C:/Users/Killakikitt/Desktop/Captures/part.png", ImageFormat.Jpeg);
                }
            }
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
