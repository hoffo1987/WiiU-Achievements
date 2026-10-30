using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;

namespace WiiUAchievements
{
    public sealed partial class MainForm
    {
        sealed class TileImage : IDisposable
        {
            public Bitmap Bitmap;
            public SizeF Size;
            public float Scale;
            public bool Unlocked;
            public string Footer;
            public long Used;
            public void Dispose() { Bitmap.Dispose(); }
        }
        // Minimum budget; DrawGrid raises it to twice the cards in view (ultrawide/fullscreen).
        const int TileCacheLimit = 48, TileCacheMaximum = 400;
        int tileCacheBudget = TileCacheLimit;
        const float TileBleed = 20;
        readonly Dictionary<string, TileImage> tileImages = new Dictionary<string, TileImage>();
        long tileImageUse;
        int tileImageBuilds;

        void ClearTileImages()
        {
            foreach (var entry in tileImages.Values) entry.Dispose();
            tileImages.Clear();
        }

        void DrawTile(Graphics g, Widget w, float t)
        {
            // Hover, keyboard focus and unlock celebrations retain the vector path.
            // The other cards reuse static pixels; a footer fed by live samples is drawn every frame.
            if (w.Hover > 0.002f || w.Press > 0.002f || FocusStrength(w, t) > 0.002f || Celebration(w.Achievement.Id, t) >= 0)
            {
                DrawTileVector(g, w, t, true);
                return;
            }
            var a = w.Achievement;
            var unlock = progress == null || a.IsPlanned ? null : progress.Get(a.Id);
            bool done = unlock != null;
            string footer = StaticFooter(a, unlock);
            TileImage entry;
            if (tileImages.TryGetValue(a.Id, out entry) &&
                (entry.Size != w.Bounds.Size || entry.Scale != scale || entry.Unlocked != done || entry.Footer != footer))
            {
                entry.Dispose(); tileImages.Remove(a.Id); entry = null;
            }
            if (entry == null)
            {
                while (tileImages.Count >= tileCacheBudget)
                {
                    var oldest = tileImages.OrderBy(pair => pair.Value.Used).First();
                    oldest.Value.Dispose(); tileImages.Remove(oldest.Key);
                }
                var bitmap = new Bitmap((int)Math.Ceiling((w.Bounds.Width + TileBleed * 2) * scale),
                    (int)Math.Ceiling((w.Bounds.Height + TileBleed * 2) * scale), PixelFormat.Format32bppPArgb);
                try
                {
                    using (var surface = Graphics.FromImage(bitmap))
                    {
                        surface.SmoothingMode = SmoothingMode.AntiAlias;
                        surface.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                        surface.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        surface.ScaleTransform(scale, scale);
                        DrawTileVector(surface, new Widget { Achievement = a,
                            Bounds = new RectangleF(TileBleed, TileBleed, w.Bounds.Width, w.Bounds.Height) }, t, footer != null);
                    }
                }
                catch { bitmap.Dispose(); throw; }
                entry = new TileImage { Bitmap = bitmap, Size = w.Bounds.Size, Scale = scale, Unlocked = done, Footer = footer };
                tileImages.Add(a.Id, entry); tileImageBuilds++;
            }
            entry.Used = ++tileImageUse;
            var r = w.Bounds;
            r.X = (float)Math.Round(r.X * scale) / scale;
            r.Y = (float)Math.Round(r.Y * scale) / scale;
            // Pixel-aligned 1:1 copy: no resampling needed (unless the intro is zooming the card).
            var interpolation = g.InterpolationMode; var offset = g.PixelOffsetMode; var quality = g.CompositingQuality;
            using (var m = g.Transform)
                if (Math.Abs(m.Elements[0] - scale) < 0.001f && m.Elements[1] == 0)
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half; g.CompositingQuality = CompositingQuality.HighSpeed;
                }
            g.DrawImage(entry.Bitmap, r.X - TileBleed, r.Y - TileBleed,
                entry.Bitmap.Width / scale, entry.Bitmap.Height / scale);
            g.InterpolationMode = interpolation; g.PixelOffsetMode = offset; g.CompositingQuality = quality;
            if (footer == null)
                DrawProgress(g, a, unlock, new RectangleF(r.X + 102, r.Y + 79, r.Width - 118, 18),
                    Theme.Font(12.5f, Weight.Bold), false);
        }
        // Footers that do not depend on live samples (pending, unlocked, disconnected) are
        // part of the cached card; the key changes whenever their text would change.
        string StaticFooter(Achievement a, Unlock unlock)
        {
            if (a.IsPlanned) return "planned";
            if (unlock != null) return "unlocked " + unlock.Utc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (current == null) return "idle " + a.Experimental + paused + demo;
            return null;
        }
    }
}
