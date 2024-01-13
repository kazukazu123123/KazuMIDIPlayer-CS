using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace KazuMIDIPlayer
{
    public class Window
    {
        internal static readonly int windowTitlebarHeight = 20;

        private static int lastId = 0;
        private static int lastZOrder = 0;

        public int Id { get; } = ++lastId;
        public int ZOrder { get; set; } = ++lastZOrder;
        public required string Title { get; set; }
        public bool IsActive { get; set; } = false;

        public event Action<Graphics, int, int, int, int>? DrawGraphicsEvent;
        public Rectangle Rectangle { get; set; }
        public Graphics? WindowGraphic { get; private set; } = null;

        public static int GetLastZOrder()
        {
            return lastZOrder;
        }

        internal static int GetWindowTitlebarHeight()
        {
            return windowTitlebarHeight;
        }

        internal protected void SetGraphics(Graphics graphics)
        {
            WindowGraphic = graphics;
        }

        internal protected void DrawGraphics()
        {
            if (WindowGraphic != null)
            {
                GraphicsState state = WindowGraphic.Save();
                WindowGraphic.SetClip(Rectangle);

                // Adjust the starting coordinates to avoid overlapping with the title bar
                int startX = Rectangle.Left;
                int startY = Rectangle.Top + GetWindowTitlebarHeight();

                // Ensure the drawing remains within the bounds of the window
                int maxWidth = Rectangle.Width;
                int maxHeight = Rectangle.Height - GetWindowTitlebarHeight();

                // Raise the DrawGraphicsEvent with adjusted coordinates
                DrawGraphicsEvent?.Invoke(WindowGraphic, startX, startY, maxWidth, maxHeight);

                WindowGraphic.Restore(state);
            }
        }
    }

    internal class WindowManager
    {
        private Rectangle maxBounds;
        private Point lastMousePos;
        private Point lastWindowPos;
        private bool isDragging;
        private Window? draggingWindow;

        private static readonly Color windowTitlebarColor = Color.FromArgb(64, 128, 0);
        private static readonly Color windowBackgroundColor = Color_AdjustBrightness(windowTitlebarColor, 0.8);
        private readonly int minWidth = 100;
        private readonly int minHeight = 40;
        private readonly int borderThickness = 1;

        private readonly List<Window> windowList = [];

        private static Color Color_AdjustBrightness(Color originalColor, double factor)
        {
            int red = (int)(originalColor.R * factor);
            int green = (int)(originalColor.G * factor);
            int blue = (int)(originalColor.B * factor);

            red = Math.Max(0, Math.Min(255, red));
            green = Math.Max(0, Math.Min(255, green));
            blue = Math.Max(0, Math.Min(255, blue));

            return Color.FromArgb(red, green, blue);
        }

        public Window NewWindow(string title, int x, int y, int w, int h)
        {
            Window window = new()
            {
                Title = title,
                Rectangle = new Rectangle(
                    Math.Max(x, 0),
                    Math.Max(y, 0),
                    Math.Max(w, minWidth),
                    Math.Max(h, minHeight) + Window.GetWindowTitlebarHeight()
                )
            };
            windowList.Add(window);

            ActivateWindow(window.Id);

            return window;
        }

        public void ActivateWindow(int windowId)
        {
            Window? windowToActivate = GetWindow(windowId);

            if (windowToActivate != null)
            {
                // Get the maximum ZOrder and set it for the activated window
                int newZOrder = windowList.Max(w => w.ZOrder) + 1;
                windowToActivate.ZOrder = newZOrder;

                // Deactivate all other windows
                foreach (var window in windowList)
                {
                    window.IsActive = false;
                }

                // Activate the specified window
                windowToActivate.IsActive = true;
            }
        }

        public void DeactivateAllWindows()
        {
            foreach (var window in windowList)
            {
                window.IsActive = false;
            }
        }

        public void DrawWindow(Graphics mainGraphics)
        {
            Font font = new("MS Gothic", 10);
            SolidBrush windowBackgroundBrush = new(windowBackgroundColor);
            Color windowTitlebarActiveColor = windowTitlebarColor;
            Color windowTitlebarInactiveColor = Color_AdjustBrightness(windowTitlebarColor, 0.7);
            Color windowBorderColor = Color_AdjustBrightness(windowTitlebarColor, 0.6);
            SolidBrush windowTitlebarActiveBrush = new(windowTitlebarActiveColor);
            SolidBrush windowTitlebarInActiveBrush = new(windowTitlebarInactiveColor);

            SolidBrush windowBorderBrush = new(windowBorderColor);
            Pen windowBorderPen = new(windowBorderColor);
            Pen redPen = new(Color.Red);

            var orderedWindows = windowList.OrderBy(window => window.ZOrder);

            foreach (var window in orderedWindows)
            {
                // Set the graphics object for the window
                window.SetGraphics(mainGraphics);

                // Window border
                mainGraphics.FillRectangle(windowBorderBrush, window.Rectangle.Left - borderThickness, window.Rectangle.Top - borderThickness, window.Rectangle.Width + borderThickness * 2, window.Rectangle.Height + borderThickness * 2);

                // Background
                //windowBackgroundBrush
                mainGraphics.FillRectangle(Brushes.Green, window.Rectangle.Left, window.Rectangle.Top, window.Rectangle.Width, window.Rectangle.Height);

                // Titlebar
                if (window.IsActive)
                {
                    mainGraphics.FillRectangle(windowTitlebarActiveBrush, window.Rectangle.Left, window.Rectangle.Top, window.Rectangle.Width, Window.GetWindowTitlebarHeight());
                }
                else
                {
                    mainGraphics.FillRectangle(windowTitlebarInActiveBrush, window.Rectangle.Left, window.Rectangle.Top, window.Rectangle.Width, Window.GetWindowTitlebarHeight());
                }

                // Titlebar border line
                mainGraphics.DrawLine(windowBorderPen, window.Rectangle.Left, window.Rectangle.Top + Window.GetWindowTitlebarHeight(), window.Rectangle.Left + window.Rectangle.Width, window.Rectangle.Top + Window.GetWindowTitlebarHeight());

                // Draw window title
                mainGraphics.DrawString(window.Title, font, Brushes.White, window.Rectangle.Left + 3, window.Rectangle.Top + 5);

                window.DrawGraphics();
            }

            windowBackgroundBrush.Dispose();
            windowTitlebarActiveBrush.Dispose();
            windowTitlebarInActiveBrush.Dispose();
            windowBorderBrush.Dispose();
            windowBorderPen.Dispose();
            redPen.Dispose();
            font.Dispose();
        }

        public void MouseDown(Point mouseLocation, Rectangle bounds)
        {
            maxBounds = bounds;

            DeactivateAllWindows();

            var orderedWindows = windowList.OrderByDescending(window => window.ZOrder);

            foreach (var window in orderedWindows)
            {
                Rectangle titlebarRect = new(
                    window.Rectangle.Left,
                    window.Rectangle.Top,
                    window.Rectangle.Width,
                    Window.GetWindowTitlebarHeight()
                );

                if (titlebarRect.Contains(mouseLocation))
                {
                    ActivateWindow(window.Id);
                    lastMousePos = mouseLocation;
                    lastWindowPos = new Point(window.Rectangle.Left, window.Rectangle.Top);
                    isDragging = true;
                    draggingWindow = window;
                    break;
                }

                Rectangle windowRect = new(
                    window.Rectangle.Left - borderThickness,
                    window.Rectangle.Top - borderThickness,
                    window.Rectangle.Width + borderThickness * 2,
                    window.Rectangle.Height + borderThickness * 2
                );

                if (windowRect.Contains(mouseLocation))
                {
                    ActivateWindow(window.Id);
                    break;
                }
            }
        }

        public void MouseMove(Point mouseLocation)
        {
            if (isDragging && draggingWindow != null)
            {
                draggingWindow.Rectangle = new Rectangle(
                    Math.Min(Math.Max(mouseLocation.X - maxBounds.Left, maxBounds.Left), maxBounds.Width - maxBounds.Left) - (lastMousePos.X - lastWindowPos.X),
                    Math.Min(Math.Max(mouseLocation.Y - maxBounds.Top, maxBounds.Top), maxBounds.Height - maxBounds.Top) - (lastMousePos.Y - lastWindowPos.Y),
                    draggingWindow.Rectangle.Width,
                    draggingWindow.Rectangle.Height
                );
            }
        }

        public void MouseUp()
        {
            isDragging = false;
            draggingWindow = null;
        }

        public void ResizeWindow(int windowId, int newWidth, int newHeight)
        {
            Window? windowToResize = GetWindow(windowId);

            if (windowToResize != null && (windowToResize.Rectangle.Width != newWidth && windowToResize.Rectangle.Height != newHeight))
            {
                // Resize the window while keeping it within the minimum size limits
                windowToResize.Rectangle = new Rectangle(
                    windowToResize.Rectangle.X,
                    windowToResize.Rectangle.Y,
                    Math.Max(newWidth, minWidth),
                    Math.Max(newHeight, minHeight) + Window.GetWindowTitlebarHeight()
                );
            }
        }

        public Window? GetWindow(int windowId)
        {
            return windowList.FirstOrDefault(window => window.Id == windowId);
        }
    }
}
