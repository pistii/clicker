namespace Kattolgatos.ViewModels
{
    using NHotkey.WindowsForms;
    using NHotkey;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Drawing.Imaging;
    using System.Drawing;
    using System.Media;
    using System.Threading;
    using System.Threading.Tasks;
    using static Kattolgatos.Data.KeyCodes;
    using System.Windows.Forms;
    using System.Windows;
    using MessageBox = System.Windows.Forms.MessageBox;
    using Point = System.Drawing.Point;
    using Color = System.Drawing.Color;
    using Graphics = System.Drawing.Graphics;
    using Rectangle = System.Drawing.Rectangle;
    using System.IO;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using static Kattolgatos.Data.User32;
    using InputType = Data.User32.InputType;
    using System.Runtime.InteropServices;

    //TODO:
    //tip: hasonlítsa össze az élő halat a döglöttel hallal és ha az jobban stimmel akkor ne nyissa
    // check pm isn't working
    // trade always plays sound
    //play sound for a specific time
    //possibility for fishing with fishbook
    //read bait quantity by pic
    //inventory location 
    //bug with the overlay, doesn't exactly clicks into the rect size. Possible solution is to store at only one place 

    public class PecaViewmodel : ObservableObject
    {
        //PC-n 7 - 7 - 8 + 15 - 7 + 120
        private static int rectLeft = SystemInformation.VirtualScreen.Width / 8 + 180; // 400
        private static int rectTop = SystemInformation.VirtualScreen.Height / 8 + 100; // 400
        private static int rectRight = (SystemInformation.VirtualScreen.Width / 8) + 300; // 500
        private static int rectBottom = (SystemInformation.VirtualScreen.Height / 8 + 100) + 115; // 500
        
        //The rect displayed on the screen
        static GameOverlay.Drawing.Rectangle rectangle = new GameOverlay.Drawing.Rectangle(rectLeft, rectTop, rectRight, rectBottom);
        private readonly RectangleOverlay overlay = new RectangleOverlay(rectangle);
        private string _textbox = "#345E81";
        private int _ID;
        private int _delay = 0;
        public ushort[] coord;
        int smallFishCounter = 0;

        const int rounds = 3; // how many times check the small fish
        //duration of the fishing
        long duration = 22 * 1000; //20 mp

        //Props for the minifish item detection
        int fullWidth = SystemInformation.VirtualScreen.Width / 2;
        int fullHeight = (int)(SystemInformation.VirtualScreen.Height / 1.5);
        ScreenCapture screenCapture;
        Rectangle rectOfInventory;

        private bool _placedBaitAndStartedFishing = false;

        //feltételezzük f1-el kezd
        int currentQuantity;
        string currentPressBtn;
        DirectXKeyStrokes keystroke;

        #region PROPS

        public IRelayCommand OnButtonSetRectangle { get; }
        public IRelayCommand OnButtonStartFishing { get; }
        //fishing in general, since user presses fishing button
        private bool _fishing = true;
        public bool fishing {
            get 
            {
                return _fishing;
            }
            set { _fishing = value; } } 
        public bool placedBaitAndStartedFishing
        {
            get { return _placedBaitAndStartedFishing; }
            set { _placedBaitAndStartedFishing = value; }
        }
        public int setDelay
        {
            get { return _delay; }
            set { _delay = value; }
        }
        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }
        public string TextBox
        {
            get { return _textbox; }
            set
            {
                _textbox = value;
            }
        }

        private ObservableCollection<Process> _processes = new ObservableCollection<Process>();
        public ObservableCollection<Process> AvailableProcesses
        {
            get { return _processes; }
            set { SetProperty(ref _processes, value); }
        }

        #endregion

        public PecaViewmodel()
        {
            GameOverlay.TimerService.EnableHighPrecisionTimers();
            searchProcess();
            //Hotkey to stop fishing
            HotkeyManager.Current.AddOrReplace("StopFishing", Keys.F8, StopFishing);
            HotkeyManager.Current.AddOrReplace("StartFishing", Keys.F9, StartFishing);
            OnButtonStartFishing = new RelayCommand(ButtonStartFishing);
            OnButtonSetRectangle = new RelayCommand(ButtonSetRectangle);
            screenCapture = new ScreenCapture();
            rectOfInventory = new Rectangle()
            {
                X = 0,//1195, //SystemInformation.VirtualScreen.Height / 2,
                Y = 0,//745, //(int) (SystemInformation.VirtualScreen.Height / 1.5),
                Height = SystemInformation.VirtualScreen.Height,
                Width = SystemInformation.VirtualScreen.Width
            };
            currentPressBtn = "F1";
            keystroke = DirectXKeyStrokes.DIK_F1;
            currentQuantity = TextBoxF1;
        }

        private void StopFishing(object sender, HotkeyEventArgs e)
        {
            e.Handled = true;
            fishing = false;
        }
        private void StartFishing(object sender, HotkeyEventArgs e)
        {
            StartFishing();
            fishing = true;
            e.Handled = true;
        }

        private void ButtonSetRectangle()
        {
            if (!overlay.IsRunning)
            {
                overlay.IsRunning = true;
                overlay.Run();
            }

            if (!overlay._window.IsVisible)
            {
                overlay._window.IsVisible = true;
            }
            else
            {
                overlay._window.IsVisible = false;
            }
        }

        private void ButtonStartFishing()
        {
            overlay._window.IsVisible = false;
            fishing = true;
            StartFishing();
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

        public void OnsearchProcess(object sender, RoutedEventArgs e)
        {
            searchProcess();
        }

        private void Click()
        {
            if (_delay > 0)
            {
                Thread.Sleep(_delay);
            }
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        private void RightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public void ClickAtPos(int x, int y, string leftClick)
        {
            SetCursorPos(x, y);
            if (leftClick == "left")
            {
                Point point = new Point(x, y);

                if (ColorsAreClose(GetColorAt(point), ColorTranslator.FromHtml(_textbox)))
                    Click();
            }
            else
            {
                Thread.Sleep(120);
                RightClick();
            }
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
            using (Bitmap bitmap = new Bitmap(550, 350))
            {
                try
                {
                    //Create new graphics objects that can capture to graphics object
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        //Screenshot moment screen content to graphics obj
                        if (bitmap != null)
                        {
                            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                        }
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
                                ClickAtPos(x, y, "left");
                                return true;
                            }
                        }
                    }
                    return false;
                }
                catch (Exception e)
                {
                    return false;
                }
            }

        }


        private void StartFishing()
        {
            Process p = Process.GetProcessById(ID);
            IntPtr h = p.MainWindowHandle;
            Stopwatch stopwatch = new Stopwatch();
            int fishOpenTimes = -1;



            //Run task in the background
            Task.Run(() =>
            {
                while (fishing)
                {
                    if (GetForegroundWindow() != h)
                    {
                        SetForegroundWindow(h);
                        Thread.Sleep(300);

                        Rectangle myRect = new Rectangle();

                        RECT rct;

                        if (!GetWindowRect(new HandleRef(this, p.Handle), out rct))
                        {
                            MessageBox.Show("ERROR");
                            return;
                        }
                        myRect.X = rct.Left;
                        myRect.Y = rct.Top;
                        myRect.Width = rct.Right - rct.Left;
                        myRect.Height = rct.Bottom - rct.Top;
                        MessageBox.Show(myRect.X.ToString(), myRect.Y.ToString());
                    }

                    if (fishOpenTimes <= -1 || fishOpenTimes >= 3)
                    {
                        OpenFishesIfSelected();
                        if (PMChkBox) CheckPM(); // Alerts
                        if (ChkTrade) //CheckTrade(); //Alerts
                        fishOpenTimes = 0;
                    }

                    if (!placedBaitAndStartedFishing)
                    {
                        placedBaitAndStartedFishing = true;
                        PlaceBaitOnRod(keystroke);
                        Thread.Sleep(250);
                        StartToFish();

                    }
                    else
                    {
                        stopwatch.Start();

                        if (currentQuantity > 0)
                        {
                            while (placedBaitAndStartedFishing)
                            {
                                SearchPixel(_textbox);
                                if (stopwatch.ElapsedMilliseconds >= duration)
                                {
                                    fishOpenTimes++;
                                    stopwatch.Restart();
                                    currentQuantity--;
                                    placedBaitAndStartedFishing = false; //stopped fishing
                                }
                            }
                        }
                        else
                        {
                            if (currentPressBtn == "F3")
                            {
                                currentPressBtn = "F4";
                                currentQuantity = BaitBtnF4Qt;
                                keystroke = DirectXKeyStrokes.DIK_F4;
                            }
                            if (currentPressBtn == "F2")
                            {
                                currentPressBtn = "F3";
                                currentQuantity = TextBoxF3;
                                keystroke = DirectXKeyStrokes.DIK_F3;
                            }
                            if (currentPressBtn == "F1")
                            {
                                currentPressBtn = "F2";
                                currentQuantity = TextBoxF2;
                                keystroke = DirectXKeyStrokes.DIK_F2;
                            }
                            // no bait
                            else
                            {
                                if (CheckLogout)
                                {
                                    fishing = false;
                                    Logout();
                                }
                                else
                                {
                                    fishing = false;
                                    MessageBox.Show("No bait");
                                }
                                currentPressBtn = "F1";
                                currentQuantity = TextBoxF1;
                                keystroke = DirectXKeyStrokes.DIK_F1;
                            }
                        }
                    }
                }
            });
        }



        private void PlaceBaitOnRod(DirectXKeyStrokes key)
        {
            if (smallFishCounter >= rounds && Kishal)
            {
                var smallFishCord = FishCoordInInventory("Resources/img/justFish.jpg", 0.7);

                //if there minifish search them again!
                if (smallFishCord != null) //Comes from clickatfish
                {
                    ClickOnFish(smallFishCord);
                    smallFishCounter = 3;
                }
                else
                {
                    smallFishCounter = 0;
                    SendKey(key, false, InputType.Keyboard);
                    Thread.Sleep(300);
                    SendKey(key, true, InputType.Keyboard);
                    Thread.Sleep(300);
                }
            }
            //Nem talált kishalat, tegyen fel más csalit
            else
            {
                SendKey(key, false, InputType.Keyboard);
                Thread.Sleep(300);
                SendKey(key, true, InputType.Keyboard);
                Thread.Sleep(300);
            }

            smallFishCounter++;
        }

        private static void StartToFish()
        {
            SendKey(DirectXKeyStrokes.DIK_SPACE, false, InputType.Keyboard);
            Thread.Sleep(300);
            SendKey(DirectXKeyStrokes.DIK_SPACE, true, InputType.Keyboard);
            Thread.Sleep(500);
        }

        private void OpenFishesIfSelected()
        {
            int treshold = 30;
            Stopwatch stopwatch = new Stopwatch();
            List<ushort[]> coords = new List<ushort[]>(); //List of the clicked fishes
            Dictionary<string, bool> dict = new Dictionary<string, bool>
            {
                {"Resources/img/data/sullo.png", Sullo},
                {"Resources/img/data/fogas1.png", Fogas},
                {"Resources/img/data/mandarinhal.png", Mandarinhal},
                {"Resources/img/data/tenchi.png", Tenchi},
                {"Resources/img/data/vorosszarnyu.png", Vorosszarnyu},
                {"Resources/img/data/pisztrang.png", Pisztrang},
                {"Resources/img/data/sebes.png", Sebes},
                {"Resources/img/data/harcsa.png", Harcsa}
            };

            stopwatch.Start();
            coords.Clear();
            foreach (var fish in dict)
            {
                if (fish.Value)
                {
                    ushort[] hasCord = FishCoordInInventory(fish.Key);
                    //Checks the path
                    if (fish.Key.Contains("mandarinhal")) hasCord = FishCoordInInventory(fish.Key, 0.7);
                    if (fish.Key.Contains("harcsa")) hasCord = FishCoordInInventory(fish.Key, 0.95);

                    if (hasCord != null)
                    {
                        int minValueX = hasCord[0] - treshold;
                        int maxValueX = hasCord[0] + treshold;
                        int minValueY = hasCord[1] - treshold;
                        int maxValueY = hasCord[1] + treshold;
                        if (!coords.Contains(hasCord))
                        {
                            if (minValueX <= hasCord[0] && maxValueX >= hasCord[0] && minValueY <= hasCord[1] && maxValueY >= hasCord[1])
                            {
                                coords.Add(hasCord);
                                ClickOnFish(hasCord);
                            }
                        }
                        ClickOnFish(hasCord);
                    }
                }
            }
        }

        public void ClickOnFish(ushort[] coordinates)
        {
            if (coordinates != null)
                ClickAtPos(coordinates[0] + 5, coordinates[1] + 5, "right");
        }

        public ushort[] FishCoordInInventory(string fishLocation, double treshold = 0.9)
        {
            ManualResetEvent syncEvent = new ManualResetEvent(false);

            Thread thread1 = new Thread(
                () =>
                {
                    coord = screenCapture.FindPicture(fishLocation, treshold, rectOfInventory);
                    syncEvent.Set();
                });
            thread1.Start();
            thread1.Join();
            thread1.Abort();

            return coord;
        }

        private void AlertMe()
        {
            PlayWav(Properties.Resources.alarm, true);
        }

        // The player making the current sound.
        private SoundPlayer Player = null;

        // Dispose of the current player and
        // play the indicated WAV file.
        private void PlayWav(Stream stream, bool play_looping)
        {
            // Stop the player if it is running.
            if (Player != null)
            {
                Player.Stop();
                Player.Dispose();
                Player = null;
            }

            // If we have no stream, we're done.
            if (stream == null) return;

            // Make the new player for the WAV stream.
            Player = new SoundPlayer(stream);

            // Play.
            if (play_looping)
                Player.PlayLooping();
            else
                Player.Play();
        }


        private void CheckPM()
        {
            //msg Check
            Point msgLoc = new Point(972, 238);
            Color color = Color.FromArgb(246, 244, 247);
            if (ColorsAreClose(color, GetColorAt(msgLoc), 3))
            {
                AlertMe();
            }
        }

        bool gotColor = false;
        Color colorbeforeTrade;
        Point tradeLocation = new Point(665, 437);

        private void CheckTrade()
        {
            //Read the color
            if (!gotColor)
            {
                colorbeforeTrade = GetColorAt(tradeLocation);
                gotColor = true;
            }
            else //compare the two color
            {
                Color colorAfterTrade = GetColorAt(tradeLocation);
                if (!ColorsAreClose(colorbeforeTrade, colorAfterTrade, 14))
                {
                    AlertMe();
                }
            }
        }

        private static void Logout()
        {
            List<DirectXKeyStrokes> msg = new List<DirectXKeyStrokes>
            {
                DirectXKeyStrokes.DIK_RETURN,
                DirectXKeyStrokes.DIK_LSHIFT,
                DirectXKeyStrokes.DIK_6,
                DirectXKeyStrokes.DIK_L,
                DirectXKeyStrokes.DIK_O,
                DirectXKeyStrokes.DIK_G,
                DirectXKeyStrokes.DIK_O,
                DirectXKeyStrokes.DIK_U,
                DirectXKeyStrokes.DIK_T,
                DirectXKeyStrokes.DIK_RETURN
            };

            foreach (var keypress in msg)
            {
                SendKey(keypress, false, InputType.Keyboard);
                Thread.Sleep(300);
                if (keypress != DirectXKeyStrokes.DIK_LSHIFT)
                {
                    SendKey(keypress, true, InputType.Keyboard);
                    Thread.Sleep(300);
                }
                if (keypress == DirectXKeyStrokes.DIK_6)
                {
                    SendKey(DirectXKeyStrokes.DIK_LSHIFT, true, InputType.Keyboard);
                }
            }
        }

        #region Checkbox Properties
        //Button F1
        private bool _chkBoxF1;
        public bool ChkBoxF1
        {
            get { return _chkBoxF1; }
            set
            {
                _chkBoxF1 = value;
            }
        }
        //Button F2
        private bool _chkBoxF2;
        public bool ChkBoxF2
        {
            get { return _chkBoxF2; }
            set
            {
                _chkBoxF2 = value;
            }
        }
        //Button F3
        private bool _chkBoxF3;
        public bool ChkBoxF3
        {
            get { return _chkBoxF3; }
            set
            {
                _chkBoxF3 = value;
            }
        }
        //Button F4
        private bool _chkBoxF4;
        public bool ChkBoxF4
        {
            get { return _chkBoxF4; }
            set
            {
                _chkBoxF4 = value;
            }
        }

        //TextBox for bait and quiantites
        #region bait quantites

        private int BaitBtnF1Qt = 200;
        private int BaitBtnF2Qt = 200;
        private int BaitBtnF3Qt = 200;
        private int BaitBtnF4Qt = 200;

        public int TextBoxF1
        {
            get { return BaitBtnF1Qt; }
            set
            {
                if (value > 200)
                {
                    BaitBtnF1Qt = 200;
                    currentQuantity = 200;
                }
                else
                {
                    currentQuantity = value;
                    BaitBtnF1Qt = value;
                }
            }
        }
        public int TextBoxF2
        {
            get { return BaitBtnF2Qt; }
            set
            {
                if (value > 200)
                {
                    BaitBtnF2Qt = 200;
                    currentQuantity = 200;
                }
                else
                {
                    currentQuantity = value;
                    BaitBtnF2Qt = value;
                }
            }
        }
        public int TextBoxF3
        {
            get { return BaitBtnF3Qt; }
            set
            {
                if (value > 200)
                {
                    BaitBtnF3Qt = 200;
                    currentQuantity = 200;
                }
                else
                {
                    currentQuantity = value;
                    BaitBtnF3Qt = value;
                }
            }
        }
        public int TextBoxF4
        {
            get { return BaitBtnF4Qt; }
            set
            {
                if (value > 200)
                {
                    BaitBtnF4Qt = 200;
                    currentQuantity = 200;
                }
                else
                {
                    currentQuantity = value;
                    BaitBtnF4Qt = value;
                }
            }
        }
        //END OF TEXTBOX
        #endregion

        //OPENING FISHES TEXTBOX AND CHECKBOX PROPERTIES
        #region open fishes 
        private bool _kishal = true;
        public bool Kishal
        {
            get { return _kishal; }
            set { _kishal = value; }
        }
        private bool _sullo;
        public bool Sullo
        {
            get { return _sullo; }
            set { _sullo = value; }
        }

        private bool _fogas;
        public bool Fogas
        {
            get { return _fogas; }
            set { _fogas = value; }
        }

        private bool _ponty;
        public bool Ponty
        {
            get { return _ponty; }
            set { _ponty = value; }
        }

        private bool _mandarinhal;
        public bool Mandarinhal
        {
            get { return _mandarinhal; }
            set { _mandarinhal = value; }
        }

        private bool _tenchi;
        public bool Tenchi
        {
            get { return _tenchi; }
            set { _tenchi = value; }
        }

        private bool _vorosszarnyu;
        public bool Vorosszarnyu
        {
            get { return _vorosszarnyu; }
            set { _vorosszarnyu = value; }
        }
        private bool _pisztrang;
        public bool Pisztrang
        {
            get { return _pisztrang; }
            set { _pisztrang = value; }
        }

        private bool _sebes;
        public bool Sebes
        {
            get { return _sebes; }
            set { _sebes = value; }
        }

        private bool _harcsa;
        public bool Harcsa
        {
            get { return _harcsa; }
            set { _harcsa = value; }
        }

        private bool _all;

        public bool IsCheckedAll
        {
            get { return _all; }
            set
            {
                _all = value;
                _sullo = value;
                _fogas = value;
                _ponty = value;
                _mandarinhal = value;
                _tenchi = value;
                _vorosszarnyu = value;
                _pisztrang = value;
                _sebes = value;
                _harcsa = value;
            }
        }


        private bool _checkLogout = false;
        public bool CheckLogout
        {
            get { return _checkLogout; }
            set
            {
                _checkLogout = value;
            }
        }

        private bool _pmChkBox;
        public bool PMChkBox
        {
            get { return _pmChkBox; }
            set
            {
                _pmChkBox = value;
            }
        }

        private bool _chkTrade;
        public bool ChkTrade
        {
            get { return _chkTrade; }
            set
            {
                _chkTrade = value;
            }
        }

        #endregion
        #endregion
    }
}
