using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace WiiUAchievements
{
    // Each game has its own look inside the neutral Wii U system chrome: the banner,
    // medal frames, card accents, progress bars and unlock popup use the game's palette
    // and artwork, while bars, buttons and settings stay common to every game.
    public sealed class GameStyle
    {
        public string GameId { get; private set; }
        public Color Accent { get; private set; }      // progress, headers, countdown
        public Color AccentDark { get; private set; }
        public Color Glow { get; private set; }
        public Color Soft { get; private set; }        // pale wash behind content
        public Color Warm { get; private set; }        // secondary highlight (sail cream)
        public Color Deep { get; private set; }        // popup panel depth
        readonly Dictionary<string, Bitmap> art = new Dictionary<string, Bitmap>(StringComparer.Ordinal);
        readonly HashSet<string> missing = new HashSet<string>(StringComparer.Ordinal);

        public static readonly GameStyle Neutral = new GameStyle
        {
            GameId = "", Accent = Theme.Accent, AccentDark = Theme.AccentDark, Glow = Theme.Glow,
            Soft = Theme.AccentSoft, Warm = Color.FromArgb(255, 236, 170), Deep = Color.FromArgb(0, 104, 168)
        };
        // The Wind Waker HD: open sea, white foam and the cream of the sail.
        public static readonly GameStyle WindWaker = new GameStyle
        {
            GameId = GameCatalog.WindWakerId, Accent = Color.FromArgb(0, 150, 206), AccentDark = Color.FromArgb(0, 92, 158),
            Glow = Color.FromArgb(86, 214, 242), Soft = Color.FromArgb(218, 243, 251), Warm = Color.FromArgb(250, 236, 196),
            Deep = Color.FromArgb(4, 70, 136)
        };

        public static GameStyle For(string gameId)
        {
            return gameId == GameCatalog.WindWakerId ? WindWaker : Neutral;
        }

        // ---- Artwork: embedded by build.ps1 from assets/games/<game>/, or read beside the EXE.
        Bitmap Load(string relative)
        {
            Bitmap bitmap;
            if (art.TryGetValue(relative, out bitmap)) return bitmap;
            if (missing.Contains(relative) || GameId.Length == 0) return null;
            try
            {
                string resource = "WiiUAchievements.Games." + GameId + "." + relative.Replace('/', '.');
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    if (stream != null) bitmap = Copy(stream);
                    else
                    {
                        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "games", GameId, relative.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(path)) using (var file = File.OpenRead(path)) bitmap = Copy(file);
                    }
                }
            }
            catch (Exception) { bitmap = null; }
            if (bitmap == null) missing.Add(relative); else art[relative] = bitmap;
            return bitmap;
        }
        static Bitmap Copy(Stream stream)
        {
            // GDI+ keeps reading the source stream lazily: copy into a private bitmap.
            using (var image = Image.FromStream(stream))
            {
                var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(bitmap)) { g.CompositingMode = CompositingMode.SourceCopy; g.DrawImage(image, 0, 0, image.Width, image.Height); }
                return bitmap;
            }
        }
        public Bitmap Icon(string achievementId) { return Load("icons/" + achievementId + ".png"); }
        public Bitmap Frame(Tier tier) { return Load("frame-" + tier.ToString().ToLowerInvariant() + ".png"); }
        public Bitmap Sky { get { return Load("sky.png"); } }
        public bool HasFrames { get { return Frame(Tier.Bronze) != null && Sky != null; } }

        // A light repeating motif for headers and the popup panel: foam curls for the sea.
        public void Pattern(Graphics g, RectangleF r, float phase, Color color)
        {
            if (color.A == 0 || r.Width < 4) return;
            if (this != WindWaker)
            {
                using (var pen = new Pen(color, 1.2f))
                    for (float x = r.X - r.Height + (phase * 12) % 12; x < r.Right; x += 12) g.DrawLine(pen, x, r.Bottom, x + r.Height, r.Y);
                return;
            }
            float step = 46, size = Math.Min(16, r.Height * 0.6f);
            float offset = (phase * 14) % step;
            using (var pen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                for (int row = 0; row * size * 1.25f < r.Height + size; row++)
                {
                    float y = r.Y + size * 0.6f + row * size * 1.25f, shift = row % 2 == 0 ? 0 : step / 2;
                    for (float x = r.X - step + offset + shift; x < r.Right + step; x += step)
                        Curl(g, pen, new PointF(x, y), size);
                }
            }
        }
        static void Curl(Graphics g, Pen pen, PointF c, float s)
        {
            // A breaking-wave curl: a long crest that rolls into a small spiral.
            using (var path = new GraphicsPath())
            {
                path.AddBezier(c.X - s * 1.3f, c.Y + s * 0.35f, c.X - s * 0.6f, c.Y + s * 0.3f, c.X - s * 0.3f, c.Y - s * 0.55f, c.X + s * 0.25f, c.Y - s * 0.5f);
                path.AddBezier(c.X + s * 0.25f, c.Y - s * 0.5f, c.X + s * 0.75f, c.Y - s * 0.45f, c.X + s * 0.75f, c.Y + s * 0.15f, c.X + s * 0.3f, c.Y + s * 0.15f);
                path.AddBezier(c.X + s * 0.3f, c.Y + s * 0.15f, c.X + s * 0.05f, c.Y + s * 0.15f, c.X + s * 0.02f, c.Y - s * 0.15f, c.X + s * 0.22f, c.Y - s * 0.18f);
                g.DrawPath(pen, path);
            }
        }
    }
}
