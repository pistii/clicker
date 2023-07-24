using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattolgatos.Overlay
{
    using System;
    using GameOverlay.Drawing;
    using GameOverlay.Windows;
    using Gma.System.MouseKeyHook;
    using static Kattolgatos.Data.User32;

    public class OverlayExample
    {
        //private OverlayWindow _window;
        //private Graphics _graphics;
        //private bool _isDragging;
        //private Point _dragOffset;
        //private Rectangle _overlayBounds;

        //private IKeyboardMouseEvents _mousehook;
        
        //public void Run()
        //{
        //    // Create an overlay window
        //    _window = new OverlayWindow(0, 0, 800, 600)
        //    {
        //        IsTopmost = true,
        //        IsVisible = true
        //    };

        //    // Create a graphics object for drawing on the overlay
        //    _graphics = new Graphics()
        //    {
        //        MeasureFPS = true,
        //        PerPrimitiveAntiAliasing = true,
        //        TextAntiAliasing = true
        //    };

        //    // Register mouse event handlers
        //    _window.MouseMove += OverlayMouseMove;
        //    _window.MouseDown += OverlayMouseDown;
        //    _window.MouseUp += OverlayMouseUp;

        //    // Start the overlay loop
        //    _window.SetupGraphics += OverlaySetupGraphics;
        //    _window.DrawGraphics += OverlayDrawGraphics;
        //    _window.DestroyGraphics += OverlayDestroyGraphics;
        //    _window.Run();
        //}

        //private void OverlaySetupGraphics(object sender, SetupGraphicsEventArgs e)
        //{
        //    // Initialize the graphics object with the provided graphics object from the event arguments
        //    _graphics.Setup(e.Graphics);
        //}

        //private void OverlayDrawGraphics(object sender, DrawGraphicsEventArgs e)
        //{
        //    // Clear the graphics surface
        //    _graphics.Clear(Color.Transparent);

        //    // Draw your overlay content using the graphics object
        //    _graphics.DrawText(new Font("Arial", 12), Color.White, 10, 10, "Hello, Overlay!");

        //    // Draw a resizable border
        //    _graphics.DrawRectangle(_graphics.CreateSolidBrush(0, 255, 0), _overlayBounds, 2);
        //}

        //private void OverlayDestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        //{
        //    // Clean up resources when the overlay is closed
        //    _graphics.Dispose();
        //}


        //private void OverlayMouseMove(object sender, OverlayMouseEventArgs e)
        //{
        //    if (_isDragging)
        //    {
        //        // Update the overlay position based on the mouse movement
        //        _window.X = e.X + _dragOffset.X;
        //        _window.Y = e.Y + _dragOffset.Y;
        //    }
        //}

        //private void OverlayMouseDown(object sender, OverlayMouseEventArgs e)
        //{
        //    if (e.Button == OverlayMouseButton.Left)
        //    {
        //        // Store the initial mouse position and calculate the offset from the overlay position
        //        _isDragging = true;
        //        _dragOffset = new Point(_window.X - e.X, _window.Y - e.Y);
        //    }
        //}

        //private void OverlayMouseUp(object sender, OverlayMouseEventArgs e)
        //{
        //    if (e.Button == OverlayMouseButton.Left)
        //    {
        //        _isDragging = false;
        //    }
        //}
    }
}
