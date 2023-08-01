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
    using System.Windows.Media.Media3D;
    using OpenCV.Net;

    //TODO:
    //tip: hasonlítsa össze az élő halat a döglöttel hallal és ha az jobban stimmel akkor ne nyissa
    // check pm isn't working
    // trade always plays sound
    //play sound for a specific time
    //possibility for fishing with fishbook
    //read bait quantity by pic
    //bug with the overlay, doesn't exactly clicks into the rect size. Possible solution is to store at only one place 
    //delay
    public class PecaViewmodel : ObservableObject
    {
        //PC-n 7 - 7 - 8 + 15 - 7 + 120
        private static int rectLeft = SystemInformation.VirtualScreen.Width / 8 + 180; // 400
        private static int rectTop = SystemInformation.VirtualScreen.Height / 8 + 100; // 400
        private static int rectRight = (SystemInformation.VirtualScreen.Width / 8) + 300; // 500
        private static int rectBottom = (SystemInformation.VirtualScreen.Height / 8 + 100) + 115; // 500
        private RECT window_size = new RECT();

        //The rect displayed on the screen
        static GameOverlay.Drawing.Rectangle rectangle = new GameOverlay.Drawing.Rectangle(rectLeft, rectTop, rectRight, rectBottom);
        private readonly RectangleOverlay overlay = new RectangleOverlay(rectangle);
        private string fishColor = "#345E81";
        private int _ID;
        private int _delay = 0;
        public ushort[] coord;
        int fishBait_qt = 0;

        const int rounds = 3; // how many times check the small fish
        //duration of the fishing
        long duration = 22 * 1000; //20 mp

        ScreenCapture screenCapture;
        Rectangle rectOfInventory;
        /// <summary>
        /// This is the middle playground area,
        /// Doesn't have to check the this for opening fishes
        /// </summary>
        Rectangle notInventory;

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
            get { return fishColor; }
            set
            {
                fishColor = value;
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
            
            currentPressBtn = "F1";
            keystroke = DirectXKeyStrokes.DIK_F1;
            currentQuantity = TextBoxF1;
            startStopBtn = "GreenYellow";
            startStopText = "Start fishing";
        }

        private void StopFishing(object sender, HotkeyEventArgs e)
        {
            e.Handled = true;
            StopFishing();
            
        }
        private void StartFishing(object sender, HotkeyEventArgs e)
        {
            ButtonStartFishing();
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
            StartStopBtn = "Red";
            StartStopText = "Stop fishing";
            StartFishing();
        }

        private void StopFishing()
        {
            StartStopBtn = "GreenYellow";
            StartStopText = "Start fishing";
            fishing = false;
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
            if (_delay > 0) Thread.Sleep(_delay);
            
            SetCursorPos(x, y);
            if (leftClick == "left")
            {
                Point point = new Point(x, y);

                if (ColorsAreClose(GetColorAt(point), ColorTranslator.FromHtml(fishColor)))
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

        private bool SearchPixel()
        {
            using (Bitmap bitmap = new Bitmap(550, 350)) //550, 350
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
                        for (int x = (int)rectangle.Left; x < rectangle.Right; x++)
                        {
                            for (int y = (int)rectangle.Top; y < rectangle.Bottom; y++)
                            {
                                Color currentpixelcolor = bitmap.GetPixel(x, y);
                                if (ColorsAreClose(currentpixelcolor, ColorTranslator.FromHtml(fishColor)))
                                {
                                    SetCursorPos(x, y);
                                    ClickAtPos(x, y, "left");
                                    return true;
                                }
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
            bool hasInventory = false;
            int bottomPadding = 60; //the money and icons in the right left corner height in pixels
            Rectangle inventorySize = new Rectangle() { //size in 768x1024
                Width = 175,
                Height = 345
            };

            //Run task in the background
            Task.Run(() =>
            {
            while (fishing)
            {
                if (GetForegroundWindow() != h)
                {
                    SetForegroundWindow(h);
                    Thread.Sleep(300);
                    if (!hasInventory)
                    {
                        var d = GetWindowRect(h, out window_size);
                        if (d) //if has size
                        {
                            rectOfInventory = new Rectangle()
                            {
                                X = window_size.Right - inventorySize.Width,
                                Width = window_size.Right,
                                Y = window_size.Bottom - bottomPadding - inventorySize.Height,
                                Height = rectOfInventory.Y + inventorySize.Height,
                            };
                            notInventory = new Rectangle()
                            {
                                X = window_size.Left,
                                Width = (ushort) (window_size.Right - inventorySize.Width),
                                Y = window_size.Top,
                                Height = window_size.Top + inventorySize.Top + inventorySize.Height
                            };
                            hasInventory = true;
                        }
                        else
                        { //set search area fullscreen
                            rectOfInventory = new Rectangle()
                            {
                                X = 0,
                                Width = SystemInformation.VirtualScreen.Width,
                                Y = 0,
                                Height = SystemInformation.VirtualScreen.Height,
                            };
                        }
                    }
                }
                    if (fishOpenTimes <= -1 || fishOpenTimes >= 1)
                    {
                        Task.WaitAll(OpenFishesIfSelected());
                        if (PMChkBox) CheckPM(); // Alerts
                        if (ChkTrade) //CheckTrade(); //Alerts
                        fishOpenTimes = 0;
                    }

                    if (!placedBaitAndStartedFishing)
                    {
                        PlaceBaitOnRod(keystroke);
                        placedBaitAndStartedFishing = true;
                        Thread.Sleep(250);
                    }
                    else
                    {
                        StartToFish();
                        stopwatch.Start();

                        if (currentQuantity > 0)
                        {
                            while (placedBaitAndStartedFishing)
                            {
                                SearchPixel();
                                if (stopwatch.ElapsedMilliseconds >= duration)
                                {
                                    fishOpenTimes++;
                                    stopwatch.Restart();
                                    placedBaitAndStartedFishing = false; //stopped fishing
                                }
                            }
                        }
                        else
                        {
                            if (currentPressBtn == "F3" && ChkBoxF4)
                            {
                                currentPressBtn = "F4";
                                currentQuantity = TextBoxF4;
                                keystroke = DirectXKeyStrokes.DIK_F4;
                            }
                            if (currentPressBtn == "F2" && ChkBoxF3)
                            {
                                currentPressBtn = "F3";
                                currentQuantity = TextBoxF3;
                                keystroke = DirectXKeyStrokes.DIK_F3;
                            }
                            if (currentPressBtn == "F1" && ChkBoxF2)
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
                                    Thread.Sleep(20000);
                                    Logout();
                                    StopFishing();
                                }
                                else
                                {
                                    //probably out of all baits
                                    StopFishing();
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
            if (fishBait_qt >= rounds && Kishal)
            {
                var smallFishCord = FishCoordInInventory("Resources/img/justFish.jpg", 0.7);
                var garnelaCord = FishCoordInInventory("Resources/img/data/garnela.jpg", 8);
                bool clicked = false;

                //if there minifish search them again!
                if (smallFishCord != null || 
                    garnelaCord != null) //Comes from clickatfish
                {
                    if (smallFishCord != null && !clicked)
                    {
                        ClickOnFish(smallFishCord);
                        fishBait_qt = 3;
                        clicked = true;
                        return;
                    } else
                    {
                        if (garnelaCord != null && !clicked)
                        {
                            ClickOnFish(garnelaCord);
                            fishBait_qt = 3;
                            clicked = true;
                            return;
                        }
                    }
                }
                else
                {
                    fishBait_qt = 0;
                    currentQuantity--;
                    SendKey(key, false, InputType.Keyboard);
                    Thread.Sleep(300);
                    SendKey(key, true, InputType.Keyboard);
                    Thread.Sleep(300);
                }
            }
            else   //Nem talált kishalat, tegyen fel más csalit
            {
                currentQuantity--;
                SendKey(key, false, InputType.Keyboard);
                Thread.Sleep(300);
                SendKey(key, true, InputType.Keyboard);
                Thread.Sleep(300);
            }

            fishBait_qt++;
        }

        private static void StartToFish()
        {
            SendKey(DirectXKeyStrokes.DIK_SPACE, false, InputType.Keyboard);
            Thread.Sleep(300);
            SendKey(DirectXKeyStrokes.DIK_SPACE, true, InputType.Keyboard);
            Thread.Sleep(500);
        }

        private Task OpenFishesIfSelected()
        {
            int treshold = 30;
            Stopwatch stopwatch = new Stopwatch();
            List<ushort[]> coords = new List<ushort[]>(); //List of the clicked fishes
            Dictionary<string, bool> dict = new Dictionary<string, bool>
            {
                {"Resources/img/data/sullo.png", Sullo},
                {"Resources/img/data/fogas1.png", Fogas},
                {"Resources/img/data/ponty.png", Ponty},
                {"Resources/img/data/mandarinhal.png", Mandarinhal},
                {"Resources/img/data/tenchi.png", Tenchi},
                {"Resources/img/data/vorosszarnyu.png", Vorosszarnyu},
                {"Resources/img/data/pisztrang.png", Pisztrang},
                {"Resources/img/data/sebes1.png", Sebes},
                {"Resources/img/data/sebes2.png", Sebes},
                {"Resources/img/data/harcsa.png", Harcsa},
                {"Resources/img/data/amur.png", Amur},
                {"Resources/img/data/lazac.png", Lazac},
                {"Resources/img/data/suger.png", Suger},
                {"Resources/img/data/szivarvanyos.png", Szivarvanyos},
                {"Resources/img/data/angolna.png", Angolna},
            };

            stopwatch.Start();
            coords.Clear();
            foreach (var fish in dict)
            {
                if (fish.Value)
                {
                    ushort[] hasCord = FishCoordInInventory(fish.Key);
                    //Checks the path
                    if (fish.Key.Contains("fogas1") || 
                        fish.Key.Contains("amur") || 
                        fish.Key.Contains("suger1"))
                    {
                        hasCord = FishCoordInInventory(fish.Key, 0.8);
                    }

                    if (fish.Key.Contains("ponty") ||
                        fish.Key.Contains("lazac") ||
                        fish.Key.Contains("mandarinhal")
                        )
                    {
                        hasCord = FishCoordInInventory(fish.Key, 0.7);
                    }

                    if (fish.Key.Contains("angolna"))
                        hasCord = FishCoordInInventory(fish.Key, 0.65);
                    if (fish.Key.Contains("sebes1") ||
                        fish.Key.Contains("sebes2"))
                        hasCord = FishCoordInInventory(fish.Key, 0.55);


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
                            }
                        }
                        ClickOnFish(hasCord);
                    }
                }
            }
            return Task.CompletedTask;
        }

        public void ClickOnFish(ushort[] coordinates)
        {
            if (coordinates != null)
                ClickAtPos(coordinates[0] + 5, coordinates[1] + 5, "right");
        }

        public ushort[] FishCoordInInventory(string fishLocation, double treshold = 0.9)
        {
            ManualResetEvent syncEvent = new ManualResetEvent(false);
            Thread thread1 = new Thread(() =>
            {

                coord = screenCapture.FindPicture(fishLocation, treshold, rectOfInventory);
                syncEvent.Set();
            });
            thread1.Start();
            syncEvent.WaitOne();
            thread1.Join();
            thread1.Abort();

            if (coord != null)
            {
                coord[0] += (ushort)notInventory.Width;
                coord[1] += (ushort)notInventory.Height;
            }
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
            Point msgLoc;
            if (window_size.Right < 1030 && window_size.Bottom < 800)
            {
                msgLoc = new Point((int)(window_size.Right/1.05), (int)(window_size.Bottom/3.22)); // X = fullwidth / 1.05 || Y = fullheight / 3.22
            }
            else
            {
                msgLoc = new Point((int)(window_size.Right / 1.04), (int)(window_size.Bottom / 3.2));
            }

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
        #endregion

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

        private bool _lazac;
        public bool Lazac
        {
            get { return _lazac; }
            set { _lazac = value; }
        }

        private bool _amur;
        public bool Amur
        {
            get { return _amur; }
            set { _amur = value; }
        }

        private bool _suger;
        public bool Suger
        {
            get { return _suger; }
            set { _suger = value; }
        }

        private bool _szivarvanyos;
        public bool Szivarvanyos
        {
            get { return _szivarvanyos; }
            set { _szivarvanyos = value; }
        }

        private bool _angolna;
        public bool Angolna
        {
            get { return _angolna; }
            set { _angolna = value; }
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
                _amur = value;
                _lazac = value;
                _suger = value;
                _szivarvanyos = value;
                _angolna = value;
            }
        }
        #endregion


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
        private string startStopBtn;
        private string startStopText;
        public string StartStopBtn { 
            get { return startStopBtn; }
            set
            {
                SetProperty(ref startStopBtn, value);
            }
        }
        public string StartStopText { 
            get { return startStopText; }
            set
            {
                SetProperty(ref startStopText, value);
            }
        }
    }

    
}
