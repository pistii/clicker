using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Windows.Controls;
using NHotkey;
using System.Diagnostics;
using Point = System.Drawing.Point;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using NHotkey.WindowsForms;
using static Kattolgatos.KeyCodes;
using System.Drawing;
using Color = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;
using Rectangle = System.Drawing.Rectangle;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using static Kattolgatos.ScreenCapture;

namespace Kattolgatos
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Key codes
        private const UInt32 MOUSEEVENTF_LEFTDOWN = 0x02;
        private const UInt32 MOUSEEVENTF_LEFTUP = 0x04;

        public const int KEYEVENTF_KEYDOWN = 0x100;
        public const int KEYEVENTF_KEYUP = 0x101;
        
        #region //DLL IMPORTS
        //mouse and keyboard events
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

        [DllImport("User32.dll")]
        private static extern int SetForegroundWindow(IntPtr point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32")]
        public static extern int SetCursorPos(int x, int y);
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref System.Drawing.Point lpPoint);


        //for reading pixel from the screen
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        #region Input implement and input structures

        //sending keyboard event
        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public HardwareInput hi;
        }

        public struct Input
        {
            public int type;
            public InputUnion u;
        }

        [Flags]
        public enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        public enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008
        }
        [Flags]
        public enum MouseEventF
        {
            Absolute = 0x8000,
            HWheel = 0x01000,
            Move = 0x0001,
            MoveNoCoalesce = 0x2000,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            VirtualDesk = 0x4000,
            Wheel = 0x0800,
            XDown = 0x0080,
            XUp = 0x0100
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
        #endregion
        #endregion

        //PC-n 7 - 7 - 8 + 15 - 7 + 120
        private static int rectLeft = SystemInformation.VirtualScreen.Width / 8 + 170; // 400
        private static int rectTop = SystemInformation.VirtualScreen.Height / 8 + 100; // 400
        private static int rectRight = (SystemInformation.VirtualScreen.Width / 8) + 300; // 500
        private static int rectBottom = (SystemInformation.VirtualScreen.Height / 8 + 100) + 120; // 500


        //The rect displayed on the screen
        static GameOverlay.Drawing.Rectangle rectangle = new GameOverlay.Drawing.Rectangle(rectLeft, rectTop, rectRight, rectBottom);
        private RectangleOverlay overlay = new RectangleOverlay(rectangle);
        private bool overlayRunning = false;

        //fishing in general, since user presses fishing button
        public bool fishing { get; set; }

        private string _textbox = "#345E81";
        public string TextBox
        {
            get { return _textbox; }
            set
            {
                _textbox = value;
            }
        }

        private int _ID;
        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }
        ObservableCollection<Process> _processes = new ObservableCollection<Process>();
        public ObservableCollection<Process> AvailableProcesses
        {
            get { return _processes; }
            set { }
        }

        public MainWindow()
        {
            InitializeComponent();
            GameOverlay.TimerService.EnableHighPrecisionTimers();
            searchProcess();
            //Hotkey to stop fishing
            HotkeyManager.Current.AddOrReplace("StopFishing", Keys.F8, StopFishing);
            HotkeyManager.Current.AddOrReplace("StartFishing", Keys.F9, StartFishing);

            ScreenCapture screenCapture = new ScreenCapture();
            Rectangle rectOfInventory = new Rectangle()
            {
                Height = 390,
                Width = 170,
                X = 0,
                Y = 0,
            };

            ushort[] coord = screenCapture.FindPicture("img/smallFish.jpg", 0.8, rectOfInventory);
            if (coord != null)
            {
            ClickAtPos(coord[0], coord[1]);
            }

        }

        private void StopFishing(object sender, HotkeyEventArgs e)
        {
            e.Handled = true;
            fishing = false;
        }
        private void StartFishing(object sender, HotkeyEventArgs e)
        {
            StartFishing();
            e.Handled = true;
        }

        private void searchProcess()
        {
            Process[] localAll = Process.GetProcesses();

            foreach (Process AvailableProcess in localAll)
            {
                if (!string.IsNullOrEmpty(AvailableProcess.MainWindowTitle))
                {
                    AvailableProcesses.Add(AvailableProcess);
                }
            }
        }

        private void Click()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public void ClickAtPos(int x, int y)
        {
            SetCursorPos(x, y);
            Thread.Sleep(20);
            Click();
        }

        private Color GetColorAt(Point location)
        {
            Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            return screenPixel.GetPixel(0, 0);
        }

        private bool ColorsAreClose(Color a, Color z, int threshold = 10)
        {
            int r = (int)a.R - z.R,
                g = (int)a.G - z.G,
                b = (int)a.B - z.B;
            return (r * r + g * g + b * b) <= threshold * threshold;
        }

        private bool SearchPixel(string hexcode)
        {
            //Create an empty bitmap with the size of the current screen
            //Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            try
            {
                Thread.Sleep(25);

                Bitmap bitmap = new Bitmap(1000, 1000);
                Thread.Sleep(25);

                //Create new graphics objects that can capture to graphics object
                Graphics graphics = Graphics.FromImage(bitmap as System.Drawing.Image);
                //Screenshot moment screen content to graphics obj
                Thread.Sleep(25);
                if (bitmap != null)
                {
                    graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                }

                Color desiredPixelColor = ColorTranslator.FromHtml(hexcode);

                int rgb = desiredPixelColor.ToArgb();
                uint.TryParse(rgb.ToString(), out uint color);

                for (int x = (int)rectangle.Left; x < rectangle.Right; x++)
                {
                    for (int y = (int)rectangle.Top; y < rectangle.Bottom; y++)
                    {
                        Color currentpixelcolor = bitmap.GetPixel(x, y);
                        if (ColorsAreClose(currentpixelcolor, desiredPixelColor))
                        {
                            ClickAtPos(x, y);
                            return true;
                        }
                    }
                }
                bitmap.Dispose();
                return false;
            }
            catch (Exception e)
            {
                return false;
            }

        }

        public Point? SearchForColor(Bitmap image, uint color)
        {
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

            //works for 32-bit pixel format only
            int ymin = rect.Top, ymax = Math.Min(rect.Bottom, image.Height);
            int xmin = rect.Left, xmax = Math.Max(rect.Right, image.Width) - 1;

            int strideInPixels = data.Stride / 4; //4 bytes per pixel
            unsafe
            {
                uint* dataPointer = (uint*)data.Scan0;
                for (int y = ymin; y < ymax; y++)
                    for (int x = xmin; x < xmax; x++)
                    {
                        //works independently of the data.Stride sign
                        uint* pixelPointer = dataPointer + y * strideInPixels + x;
                        uint pixel = *pixelPointer;
                        bool found = pixel == color;
                        if (found)
                        {

                            image.UnlockBits(data);
                            return new Point(x, y);

                        }
                    }
            }
            image.UnlockBits(data);
            return null;
        }


        private void SetPixel(object sender, HotkeyEventArgs e)
        {
            Point cursor = new Point();
            GetCursorPos(ref cursor);
            var color = GetColorAt(cursor);

            TextBox = ColorTranslator.ToHtml(color);
        }

        private void RectLeft(object sender, HotkeyEventArgs args)
        {
            if (args.Name == "rectLeftIn")
            {
                overlay.rect.Left += 10;
                rectLeft += 10;
                args.Handled = true;

            }
            else if(args.Name == "rectLeftOut")
            {
                overlay.rect.Left -= 10;
                rectLeft -= 10;
                args.Handled = true;

            }
            overlay._refreshGraphics();
        }
        private void RectUp(object sender, HotkeyEventArgs args)
        {
            if (args.Name == "rectUpIn")
            {
                overlay.rect.Top += 10;
                rectTop += 10;
                args.Handled = true;

            }
            else if(args.Name == "rectUpOut")
            {
                overlay.rect.Top -= 10;
                rectTop -= 10;
                args.Handled = true;

            }
            overlay._refreshGraphics();
        }
        private void RectRight(object sender, HotkeyEventArgs args)
        {
            if (args.Name == "rectRightIn")
            {
                overlay.rect.Right -= 10;
                rectRight -= 10;
                args.Handled = true;

            }
            else if (args.Name == "rectRightOut")
            {
                overlay.rect.Right += 10;
                rectRight += 10;
                args.Handled = true;

            }
            overlay._refreshGraphics();
        }
        private void RectDown(object sender, HotkeyEventArgs args)
        {
            if (args.Name == "rectDownIn")
            {
                overlay.rect.Bottom -= 10;
                rectBottom -= 10;
                args.Handled = true;
            }
            else if (args.Name == "rectDownOut")
            {
                overlay.rect.Bottom += 10;
                rectBottom += 10;
                args.Handled = true;

            }
            overlay._refreshGraphics();
        }

        private async void OnButtonSetRectangle(object sender, RoutedEventArgs e)
        {
            //Increment the rectangle of the box
            HotkeyManager.Current.AddOrReplace("rectLeftOut", Keys.Left, RectLeft);
            HotkeyManager.Current.AddOrReplace("rectUpOut", Keys.Up, RectUp);
            HotkeyManager.Current.AddOrReplace("rectRightOut", Keys.Right, RectRight);
            HotkeyManager.Current.AddOrReplace("rectDownOut", Keys.Down, RectDown);

            //Shrink the rectangle of the box
            HotkeyManager.Current.AddOrReplace("rectLeftIn", Keys.NumPad4, RectLeft);
            HotkeyManager.Current.AddOrReplace("rectUpIn", Keys.NumPad8, RectUp);
            HotkeyManager.Current.AddOrReplace("rectRightIn", Keys.NumPad6, RectRight);
            HotkeyManager.Current.AddOrReplace("rectDownIn", Keys.NumPad2, RectDown);


            //TODO: újra kattintásnál disposeolja az eventet
            if (!overlayRunning)
            {
                await Task.Run(() => overlay.Run());
                overlayRunning = true;
            }
            else
            {
                //Remove hotkeys
                HotkeyManager.Current.Remove("rectLeftOut");
                HotkeyManager.Current.Remove("rectUpOut");
                HotkeyManager.Current.Remove("rectRightOut");
                HotkeyManager.Current.Remove("rectDownOut");
                HotkeyManager.Current.Remove("rectLeftIn");
                HotkeyManager.Current.Remove("rectUpIn");
                HotkeyManager.Current.Remove("rectRightIn");
                HotkeyManager.Current.Remove("rectDownIn");

                await Task.Run(() => overlay.Dispose());
                overlayRunning = false;
            }
        }

        private void OnsearchProcess(object sender, RoutedEventArgs e)
        {
            searchProcess();
        }

        /// <summary>
        /// Identifies if the users started to fishing
        /// </summary>
        /// <returns></returns>
        ///
        private bool fishingMenuVisible()
        {
            Color menubar_color = ColorTranslator.FromHtml("#491b0b");
            var p = new Point() {
                X = (int)rectangle.Left,
                Y = (int)rectangle.Top - 70
            };
            var color = GetColorAt(p);
            //Thread.Sleep(200);
            while (menubar_color != null)
            {
                //Checks if the menubar is visible
                if (ColorsAreClose(menubar_color, color, 20))
                {
                    return true;
                }
                return false;
            }
            return false;
        }


        /// <summary>
        /// Sends a directx key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="KeyUp"></param>
        /// <param name="inputType"></param>
        private static void SendKey(KeyCodes.DirectXKeyStrokes key, bool KeyUp, InputType inputType)
        {
            uint flagtosend;
            if (KeyUp)
            {
                flagtosend = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode);
            }
            else
            {
                flagtosend = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode);
            }

            Input[] inputs =
            {
            new Input
            {
                type = (int) inputType,
                u = new InputUnion
                {
                    ki = new KeyboardInput
                    {
                        wVk = 0,
                        wScan = (ushort) key,
                        dwFlags = flagtosend,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            }
        };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }

        private void StartFishing()
        {
            fishing = true;

            Process p = Process.GetProcessById(ID);
            IntPtr h = p.MainWindowHandle;
            Stopwatch stopwatch = new Stopwatch();

            long duration = 20 * 1000; //20 mp
            //Run task in the background
            Task.Run(() =>
            {
                while (fishing)
                {
                    if (GetForegroundWindow() != h)
                        SetForegroundWindow(h);
                    Thread.Sleep(300);
                    if (!placedBaitAndStartedFishing)
                    {
                        placedBaitAndStartedFishing = true;
                        PlaceBaitOnRod();
                        Thread.Sleep(300);
                        StartToFish();
                        Thread.Sleep(300);
                    }
                    if (placedBaitAndStartedFishing)
                    {
                        stopwatch.Start();
                        if (ChkBoxF1 && BaitBtnF1Qt >= 0)
                        {
                            while (placedBaitAndStartedFishing)
                            {
                                SearchPixel(_textbox);
                                if (stopwatch.ElapsedMilliseconds >= duration)
                                {
                                    stopwatch.Restart();
                                    BaitBtnF1Qt--;
                                    placedBaitAndStartedFishing = false; //stopped fishing
                                }
                            }
                        }
                        else
                        {
                            if (ChkBoxF2 && BaitBtnF2Qt >= 0)
                            {
                                while (placedBaitAndStartedFishing)
                                {
                                    SearchPixel(_textbox);
                                    if (stopwatch.ElapsedMilliseconds >= duration)
                                    {
                                        stopwatch.Restart();
                                        BaitBtnF2Qt--;
                                        placedBaitAndStartedFishing = false; //stopped fishing
                                    }
                                }
                            }
                            else
                            {
                                if (ChkBoxF3 && BaitBtnF3Qt >= 0)
                                {
                                    while (placedBaitAndStartedFishing)
                                    {
                                        SearchPixel(_textbox);
                                        if (stopwatch.ElapsedMilliseconds >= duration)
                                        {
                                            stopwatch.Restart();
                                            BaitBtnF2Qt--;
                                            placedBaitAndStartedFishing = false; //stopped fishing
                                        }
                                    }
                                }
                                else
                                {
                                    if (ChkBoxF4 && BaitBtnF4Qt >= 0)
                                    {
                                        while (placedBaitAndStartedFishing)
                                        {
                                            SearchPixel(_textbox);
                                            if (stopwatch.ElapsedMilliseconds >= duration)
                                            {
                                                stopwatch.Restart();
                                                BaitBtnF4Qt--;
                                                placedBaitAndStartedFishing = false; //stopped fishing
                                            }
                                        }
                                    }
                                    // no bait
                                    else
                                    {
                                        fishing = false;
                                        MessageBox.Show("No bait!");
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }
        private void OnButtonStartFishing(object sender, RoutedEventArgs e)
        {
            StartFishing();
        }


        private bool _placedBaitAndStartedFishing = false;
        public bool placedBaitAndStartedFishing { get { return _placedBaitAndStartedFishing; } set { _placedBaitAndStartedFishing = value; } }
        private static void PlaceBaitOnRod()
        {
            SendKey(DirectXKeyStrokes.DIK_F1, false, InputType.Keyboard);
            Thread.Sleep(300);
            SendKey(DirectXKeyStrokes.DIK_F1, true, InputType.Keyboard);
            Thread.Sleep(300);
        }

        private static void StartToFish() { 
            SendKey(DirectXKeyStrokes.DIK_SPACE, false, InputType.Keyboard);
            Thread.Sleep(300);
                SendKey(DirectXKeyStrokes.DIK_SPACE, true, InputType.Keyboard);
            Thread.Sleep(500);
        }

        public static void miniFish()
        {

            Mat pic2 = CvInvoke.Imread("/img/without.jpg");
            Mat pic1 = CvInvoke.Imread("/img/minifish.jpg");


            //CvInvoke.Resize(pic1, pic1, new System.Drawing.Size(0, 0), .7d, .7d);
            //CvInvoke.Resize(pic2, pic2, new System.Drawing.Size(0, 0), .7d, .7d);

            
            Mat template = new Mat("I:/ClickerProject/Clicker/Kattolgatos/img/without.jpg");
           
            Mat template1 = new Mat("I:/ClickerProject/Clicker/Kattolgatos/img/with.jpg");

            CvInvoke.MatchTemplate(template, template1, template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed);


            double min = 0.0d;
            double max = 0.0d;

            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MinMaxLoc(template, ref min, ref max, ref minLoc, ref maxLoc);
            CvInvoke.Threshold(template, template, 0.7, 1, Emgu.CV.CvEnum.ThresholdType.ToZero);


            CvInvoke.Imshow("template out", template);
            CvInvoke.WaitKey();
        }

       

        #region Checkbox Properties
        //Button F1
        private bool _chkBoxF1;
        public bool ChkBoxF1
        {
            get { return _chkBoxF1; }
            set { _chkBoxF1 = value; }
        }
        //Button F2
        private bool _chkBoxF2;
        public bool ChkBoxF2
        {
            get { return _chkBoxF2; }
            set { _chkBoxF2 = value; }
        }
        //Button F3
        private bool _chkBoxF3;
        public bool ChkBoxF3
        {
            get { return _chkBoxF3; }
            set { _chkBoxF3 = value; }
        }
        //Button F4
        private bool _chkBoxF4;
        public bool ChkBoxF4
        {
            get { return _chkBoxF4; }
            set { _chkBoxF4 = value; }
        }

        //TextBox values
        #region bait quantites

        private int BaitBtnF1Qt = 200;
        private int BaitBtnF2Qt = 0;
        private int BaitBtnF3Qt = 0;
        private int BaitBtnF4Qt = 0;
        
        public int TextBoxF1
        {
            get { return BaitBtnF1Qt; }
            set {
                if (value > 200)
                {
                    BaitBtnF1Qt = 200;
                } 
                else
                    BaitBtnF1Qt = value;
            }
        }
        public int TextBoxF2
        {
            get { return BaitBtnF2Qt; }
            set {
                if (value > 200)
                {
                    BaitBtnF2Qt = 200;
                }
                else
                    BaitBtnF2Qt = value; 
            }
        }
        public int TextBoxF3
        {
            get { return BaitBtnF3Qt; }
            set { 
                if (value > 200)
                {
                    BaitBtnF3Qt = 200;
                }
                else
                    BaitBtnF3Qt = value; 
            }
        }

        public int TextBoxF4
        {
            get { return BaitBtnF4Qt; }
            set { 
                if (value > 200)
                {
                    BaitBtnF4Qt = 200;
                }
                else
                    BaitBtnF4Qt = value; 
            }
        }
        #endregion
        #endregion

    }
}
