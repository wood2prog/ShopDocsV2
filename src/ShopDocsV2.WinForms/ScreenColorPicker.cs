using System.Drawing;
using System.Drawing.Drawing2D;
using ShopDocsV2.Application;

namespace ShopDocsV2.WinForms;

/// <summary>
/// Eyedropper: freezes a screenshot of every monitor, covers each monitor with a borderless overlay
/// showing it (crosshair cursor plus a magnifier), and returns the color of the pixel the user clicks.
/// Esc or right-click cancels. One overlay per monitor (rather than one spanning the virtual screen)
/// keeps each overlay on a single DPI under PerMonitorV2, so screen and client pixels stay 1:1.
/// </summary>
internal static class ScreenColorPicker
{
    public static Color? PickColor()
    {
        var virtualScreen = SystemInformation.VirtualScreen;
        using var screenshot = new Bitmap(virtualScreen.Width, virtualScreen.Height);
        using (var g = Graphics.FromImage(screenshot))
        {
            g.CopyFromScreen(virtualScreen.Location, Point.Empty, virtualScreen.Size);
        }

        Color? result = null;
        var overlays = Screen.AllScreens.Select(s => new PickerOverlay(screenshot, virtualScreen.Location, s.Bounds)).ToList();
        void CloseAll() => overlays.Where(o => !o.IsDisposed).ToList().ForEach(o => o.Close());
        foreach (var overlay in overlays)
        {
            overlay.Picked += color =>
            {
                result = color;
                CloseAll();
            };
            overlay.Cancelled += () => CloseAll();
        }

        // The rest are shown once the modal loop is running; windows shown before ShowDialog would be
        // disabled by it and couldn't receive the click.
        var primary = overlays[0];
        primary.Shown += (_, _) => overlays.Skip(1).ToList().ForEach(o => o.Show());
        primary.ShowDialog();
        overlays.ForEach(o => o.Dispose());
        return result;
    }

    private sealed class PickerOverlay : Form
    {
        private const int LoupePixels = 11;     // odd, so there's a center pixel
        private const int LoupeZoom = 10;
        private const int LoupeSize = LoupePixels * LoupeZoom;
        private const int LoupeOffset = 20;
        private const int CaptionHeight = 22;

        private readonly Bitmap _screenshot;
        private readonly Point _virtualOrigin;
        private readonly Rectangle _screenBounds;
        private Rectangle _lastLoupeRect;

        public event Action<Color>? Picked;
        public event Action? Cancelled;

        public PickerOverlay(Bitmap screenshot, Point virtualOrigin, Rectangle screenBounds)
        {
            _screenshot = screenshot;
            _virtualOrigin = virtualOrigin;
            _screenBounds = screenBounds;

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = screenBounds;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                Cancelled?.Invoke();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Invalidate(_lastLoupeRect);
            _lastLoupeRect = LoupeRect(e.Location);
            Invalidate(_lastLoupeRect);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Invalidate(_lastLoupeRect);
            _lastLoupeRect = Rectangle.Empty;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right)
            {
                Cancelled?.Invoke();
                return;
            }

            if (e.Button == MouseButtons.Left && TryGetPixel(Cursor.Position, out var color))
            {
                Picked?.Invoke(color);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Everything is painted in OnPaint from the frozen screenshot.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            // Redraw only the invalidated part of the frozen screenshot.
            var clip = e.ClipRectangle;
            var source = new Rectangle(
                clip.X + _screenBounds.X - _virtualOrigin.X,
                clip.Y + _screenBounds.Y - _virtualOrigin.Y,
                clip.Width, clip.Height);
            g.DrawImage(_screenshot, clip, source, GraphicsUnit.Pixel);

            var cursorScreen = Cursor.Position;
            if (!_screenBounds.Contains(cursorScreen) || !TryGetPixel(cursorScreen, out var color))
            {
                return;
            }

            DrawLoupe(g, PointToClient(cursorScreen), cursorScreen, color);
        }

        private void DrawLoupe(Graphics g, Point cursorClient, Point cursorScreen, Color color)
        {
            var rect = LoupeRect(cursorClient);
            var zoomRect = new Rectangle(rect.X, rect.Y, LoupeSize, LoupeSize);

            var half = LoupePixels / 2;
            var source = new Rectangle(
                cursorScreen.X - _virtualOrigin.X - half,
                cursorScreen.Y - _virtualOrigin.Y - half,
                LoupePixels, LoupePixels);
            g.FillRectangle(Brushes.Black, zoomRect);
            g.DrawImage(_screenshot, zoomRect, source, GraphicsUnit.Pixel);

            // Outline the center (picked) pixel.
            var center = new Rectangle(zoomRect.X + half * LoupeZoom, zoomRect.Y + half * LoupeZoom, LoupeZoom, LoupeZoom);
            g.DrawRectangle(Pens.White, center);
            g.DrawRectangle(Pens.Black, Rectangle.Inflate(center, 1, 1));
            g.DrawRectangle(Pens.Black, zoomRect);

            var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            var captionRect = new Rectangle(rect.X, rect.Y + LoupeSize, LoupeSize, CaptionHeight);
            using var captionBrush = new SolidBrush(color);
            var textColor = ColorTranslator.FromHtml(ColorMath.GetContrastingTextColor(hex));
            g.FillRectangle(captionBrush, captionRect);
            g.DrawRectangle(Pens.Black, captionRect);
            TextRenderer.DrawText(g, hex, Font, captionRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        /// <summary>Where the magnifier goes: below-right of the cursor, flipped when that would run off this screen.</summary>
        private Rectangle LoupeRect(Point cursorClient)
        {
            var width = LoupeSize + 1;
            var height = LoupeSize + CaptionHeight + 1;
            var x = cursorClient.X + LoupeOffset;
            var y = cursorClient.Y + LoupeOffset;
            if (x + width > ClientSize.Width)
            {
                x = cursorClient.X - LoupeOffset - width;
            }
            if (y + height > ClientSize.Height)
            {
                y = cursorClient.Y - LoupeOffset - height;
            }
            return new Rectangle(x, y, width, height);
        }

        private bool TryGetPixel(Point screenPoint, out Color color)
        {
            var x = screenPoint.X - _virtualOrigin.X;
            var y = screenPoint.Y - _virtualOrigin.Y;
            if (x < 0 || y < 0 || x >= _screenshot.Width || y >= _screenshot.Height)
            {
                color = default;
                return false;
            }

            color = _screenshot.GetPixel(x, y);
            return true;
        }
    }
}
