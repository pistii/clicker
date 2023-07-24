using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading;
using System.Windows;
using static Kattolgatos.Data.KeyCodes;
using Rectangle = System.Drawing.Rectangle;

namespace Kattolgatos.ViewModels
{
    public class EnergiaFarmViewModel : ObservableObject
    {
        public EnergiaFarmViewModel()
        {
        

        }

        //readonly ScreenCapture screenCapture = new ScreenCapture();
        //readonly Data.KeyCodes key;

        //Rectangle rectOfInventory = new Rectangle()
        //{
        //    X = 0,//1195, //SystemInformation.VirtualScreen.Height / 2,
        //    Y = 0,//745, //(int) (SystemInformation.VirtualScreen.Height / 1.5),
        //    Height = System.Windows.Forms.SystemInformation.VirtualScreen.Height,
        //    Width = System.Windows.Forms.SystemInformation.VirtualScreen.Width
        //};

        //bool isStarted;
        //ushort[] coord;
        //readonly int _delay = 0;

        //public void Start(object sender, RoutedEventArgs e)
        //{
        //    isStarted = true;
        //    var item = FindItem("/Resources/img/tor2.jpg");
        //    if (item != null)
        //    {
        //        ClickAtPos(item[0] + 5, item[1] + 5);
        //    }
        //}

        //private void Click()
        //{
        //    if (_delay > 0)
        //    {
        //        Thread.Sleep(_delay);
        //    }
        //    Data.User32.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        //    Thread.Sleep(100);
        //    Data.User32.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        //}

        //private void RightClick()
        //{
        //    Data.User32.mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        //    Thread.Sleep(100);
        //    Data.User32.mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        //}

        //public void ClickAtPos(int x, int y, bool leftClick = true)
        //{
        //    Data.User32.SetCursorPos(x, y);
        //    Thread.Sleep(20);
        //    if (leftClick) Click();
        //    else RightClick();
        //}

        //public void Stop(object sender, RoutedEventArgs e)
        //{
        //    isStarted = false;
        //    MessageBox.Show("Stopped");
        //}

        //public ushort[] FindItem(string itemLocation, double treshold = 0.8)
        //{
        //    ManualResetEvent syncEvent = new ManualResetEvent(false);

        //    Thread thread1 = new Thread(
        //        () =>
        //        {
        //            coord = screenCapture.FindPicture(itemLocation, treshold, rectOfInventory);
        //            syncEvent.Set();
        //        });
        //    thread1.Start();
        //    thread1.Join();
        //    thread1.Abort();

        //    return coord;
        //}
    }
}
