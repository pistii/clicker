using GameOverlay.Drawing;
using GameOverlay.Windows;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kattolgatos
{
    public class RectangleOverlay : IDisposable
    {
        public GraphicsWindow _window;
        private readonly Dictionary<string, SolidBrush> _brushes;
        private IKeyboardMouseEvents _mouseHook;

        float LEFT, RIGHT, TOP, BOTTOM;
        public Rectangle rect;

        public event EventHandler<MouseEventArgs> MouseDown;

        public RectangleOverlay(Rectangle rectangle)
        {
            _brushes = new Dictionary<string, SolidBrush>();

            rect = new Rectangle(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
            var gfx = new Graphics()
            {
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true
            };

            _window = new GraphicsWindow(0, 0, 800, 800, gfx)
            {
                FPS = 30,
                IsTopmost = true,
                IsVisible = true
                
            };

            _window.DestroyGraphics += _window_DestroyGraphics;
            _window.DrawGraphics += (sender, e) => _window_DrawGraphics(sender, e, rect);
            _window.SetupGraphics += _window_SetupGraphics;

            // Set up mouse hook to capture mouse events globally
            _mouseHook = Hook.GlobalEvents();
            _mouseHook.MouseMove += OverlayMouseMove;
            _mouseHook.MouseDown += OverlayMouseDown;
            _mouseHook.MouseUp += OverlayMouseUp;
        }

        private bool _isDragging;
        private Point _dragOffset;
        private bool _isRunning;

        public bool IsRunning
        {
            get { return _isRunning; }
            set { _isRunning = value; }
        }

        private void OverlayMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                // Update the overlay position based on the mouse movement
                _window.X = e.X + (int)_dragOffset.X;
                _window.Y = e.Y + (int)_dragOffset.Y;
                _window.Move(_window.X, _window.Y);
            }
            //TODO: resizeable option if user clicks on the left side of the rect
            //if (LeftResize)
            //{
            //    rect.Left += e.X;
            //}
        }

        bool LeftResize = false;
        private void OverlayMouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left && 
                e.X > rect.Left && e.X < rect.Right &&
                e.Y < rect.Bottom && e.Y > rect.Top && IsRunning)
            {
                if (rect.Left+5 < e.X || e.X > rect.Left -5)
                {
                    rect.Left = e.X;
                }
                // Store the initial mouse position and calculate the offset from the overlay position
                _window.Pause();

                _isDragging = true;
                _dragOffset = new Point(_window.X - e.X, _window.Y - e.Y);

                //Belső értéket kapja meg, ahol keresi a halat
                LEFT = e.X - rect.Left;
                RIGHT = rect.Right - e.X;
                TOP = e.Y - rect.Top;
                BOTTOM = rect.Bottom - e.Y;
            }
        }

        private void OverlayMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left &&  _isDragging)
            {
                _isDragging = false;
                rect.Left = e.X -LEFT;
                rect.Right = e.X + RIGHT;
                rect.Top = e.Y - TOP;
                rect.Bottom = e.Y + BOTTOM;
            }
            LeftResize = false;
        }


        private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;
             
            if (e.RecreateResources)
            {
                foreach (var pair in _brushes) pair.Value.Dispose();
            }
            _brushes["green"] = gfx.CreateSolidBrush(0, 255, 0);
            if (e.RecreateResources) return;
        }

        private bool updating = false;
        public void _refreshGraphics()
        {
            if (!updating)
            {
                updating = true;
                Thread.Sleep(100);
                _window.Recreate();
            }
            updating = false;
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            foreach (var pair in _brushes) pair.Value.Dispose();
        }

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e, Rectangle getRect)
        {
            var gfx = e.Graphics;
            gfx.DrawRectangle(_brushes["green"], getRect, 1.0f);
        }

        public void Run()
        {
            _window.Dispose();
            Task.Run(() => _window.Create());
            Task.Run(() => _window.Join());
        }

        ~RectangleOverlay()
        {
            Dispose(false);
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue) {
                _window.IsRunning = false;
                _window.Dispose();
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
