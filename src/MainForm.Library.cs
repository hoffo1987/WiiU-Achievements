using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace WiiUAchievements
{
    // Uses the existing theme, sea artwork, rounded cards and input/accessibility
    // system. This is a catalog, not a promise of support for unvalidated games.
    public sealed partial class MainForm
    {
        bool libraryOpen;
        float libraryScroll;
        RectangleF libraryGridRect;

        void ToggleLibrary()
        {
            if (libraryOpen) { CloseLibrary(); return; }
            libraryOpen = true; libraryScroll = 0;
            drawer = drawerTarget = 0; dialog = dialogTarget = 0; detail = null;
            focused = hovered = pressed = null;
            Invalidate(); Kick();
        }
        void CloseLibrary()
        {
            libraryOpen = false; focused = hovered = pressed = null;
            Invalidate(); Kick();
        }
        void ArrangeLibrary(float width, float height)
        {
            libraryGridRect = new RectangleF(Pad - 8, TopH + 116, width - Pad * 2 + 16, Math.Max(1, height - BottomH - TopH - 128));
            int cols = Math.Max(1, (int)((libraryGridRect.Width - 16 + Gap) / (360 + Gap)));
            float cardWidth = (libraryGridRect.Width - 16 - Gap * (cols - 1)) / cols;
            const float cardHeight = 274;
            int rows = (GameCatalog.Games.Count + cols - 1) / cols;
            libraryScroll = Math.Max(0, Math.Min(libraryScroll, Math.Max(0, rows * (cardHeight + Gap) + 16 - libraryGridRect.Height)));
            for (int i = 0; i < GameCatalog.Games.Count; i++)
            {
                var game = GameCatalog.Games[i];
                var bounds = new RectangleF(libraryGridRect.X + 8 + i % cols * (cardWidth + Gap),
                    libraryGridRect.Y + 8 + i / cols * (cardHeight + Gap) - libraryScroll, cardWidth, cardHeight);
                var tile = Put("game:" + game.Id, Kind.GameTile, bounds, game.Title,
                    () => { try { SelectGame(game); } catch (Exception ex) { Log("Gioco non aperto: " + ex.Message); } },
                    game.Available && (game == selectedGame || CanConnect), 0);
                tile.Checked = game == selectedGame;
                tile.Help = game.Description + (game.Available ? " " + String.Join("; ", game.Profiles.Select(p => p.Label + ", Cemu " + p.CemuVersion)) : " Non disponibile.");
                if (game.Available && game != selectedGame && !CanConnect) tile.Help += " Scollega la sessione prima di cambiare gioco.";
            }
            AddButton("library-back", Kind.Button, "Obiettivi", "B", Pad, height - BottomH + 16, CloseLibrary, true, 0);
        }
        void DrawLibrary(Graphics g, float time)
        {
            Theme.Text(g, "Giochi", Theme.Font(30, Weight.Heavy), Theme.Ink, new RectangleF(Pad + 4, TopH + 20, W - Pad * 2, 42));
            Theme.Text(g, "Scegli un gioco per aprire i suoi obiettivi.", Theme.Font(15, Weight.Bold), Theme.Muted, new RectangleF(Pad + 4, TopH + 68, W - Pad * 2, 24));
            Theme.Text(g, "Altri giochi non sono ancora disponibili.", Theme.Font(13, Weight.Regular), Theme.Muted, new RectangleF(Pad + 4, TopH + 94, W - Pad * 2, 22));
            var state = g.Save();
            g.SetClip(libraryGridRect, CombineMode.Intersect);
            foreach (var game in GameCatalog.Games)
            {
                var widget = widgets["game:" + game.Id];
                var r = widget.Bounds;
                if (!libraryGridRect.IntersectsWith(r)) continue;
                Theme.Shadow(g, r, 20, 8, 42, 3);
                Theme.Fill(g, r, 20, Color.White, Color.FromArgb(239, 244, 247));
                var inner = g.Save();
                using (var clip = Theme.Round(r, 20)) g.SetClip(clip, CombineMode.Intersect);
                var art = new RectangleF(r.X, r.Y, r.Width, 132);
                if (game.Id == GameCatalog.WindWakerId)
                {
                    bool drawn = false;
                    var playing = game == selectedGame ? BannerVideo : null;
                    try { drawn = playing != null && playing.Draw(g, art, 0); } catch (Exception) { drawn = false; }
                    if (!drawn) banner.Draw(g, art, 0, ambientTime, scale);
                    var gameStyle = GameStyle.For(game.Id);
                    if (!gameStyle.DrawArt(g, "badge.png", new RectangleF(r.X + (r.Width - 108) / 2, r.Y + 12, 108, 108), 1))
                        Theme.ShadowText(g, game.Title, Theme.Font(24, Weight.Heavy), Color.White,
                            new RectangleF(r.X + 22, r.Y + 45, r.Width - 44, 36), StringAlignment.Center, StringAlignment.Center, false, 100);
                }
                else
                {
                    Theme.Fill(g, art, 0, Color.FromArgb(228, 239, 246), Color.FromArgb(209, 224, 234));
                    DrawLibraryIcon(g, new RectangleF(r.X + 24, r.Y + 42, 48, 48));
                    Theme.Text(g, game.Title, Theme.Font(24, Weight.Heavy), Theme.Muted,
                        new RectangleF(r.X + 88, r.Y + 48, r.Width - 110, 38));
                }
                g.Restore(inner);
                Theme.Stroke(g, r, 20, widget.Checked ? Theme.Accent : Theme.Line, widget.Checked ? 1.8f : 1);
                string badge = game.Available ? (widget.Checked ? "Selezionato" : "Disponibile") : "Non disponibile";
                var badgeFont = Theme.Font(11.5f, Weight.Heavy);
                float badgeWidth = Measure(badge, badgeFont).Width + 24;
                var badgeRect = new RectangleF(r.X + 18, r.Y + 12, badgeWidth, 24);
                Theme.Fill(g, badgeRect, 12, Color.White, Color.FromArgb(238, 246, 250));
                Theme.Text(g, badge, badgeFont, game.Available ? Theme.AccentDark : Theme.Muted, badgeRect, StringAlignment.Center, StringAlignment.Center, false);
                string configuration = game.Available ? String.Join("  ·  ", game.Profiles.Select(p => p.Label + " · Cemu " + p.CemuVersion)) : "Nuovi titoli in futuro";
                if (game.Available) Theme.Text(g, game.Title, Theme.Font(20, Weight.Heavy), Theme.Ink, new RectangleF(r.X + 22, r.Y + 143, r.Width - 44, 29));
                Theme.Text(g, configuration, Theme.Font(12, Weight.Bold), Theme.Muted, new RectangleF(r.X + 22, r.Y + 177, r.Width - 44, 20));
                Theme.Text(g, game.Description, Theme.Font(12.5f, Weight.Regular), Theme.Muted,
                    new RectangleF(r.X + 22, r.Y + 205, r.Width - 44, 38), StringAlignment.Near, StringAlignment.Near, true);
                if (game.Available)
                    Theme.Text(g, widget.Checked ? "Apri obiettivi  ›" : widget.Enabled ? "Seleziona gioco  ›" : "Scollega la sessione per cambiare gioco",
                        Theme.Font(12.5f, Weight.Bold), widget.Enabled ? Theme.AccentDark : Theme.Muted,
                        new RectangleF(r.X + 22, r.Bottom - 28, r.Width - 44, 20));
                if (widget.Enabled) Theme.Cursor(g, r, 20, Math.Max(FocusStrength(widget, time), widget.Hover * 0.55f));
            }
            g.Restore(state);
        }
        static void DrawLibraryIcon(Graphics g, RectangleF r)
        {
            float cell = r.Width * 0.42f, step = r.Width - cell;
            for (int i = 0; i < 4; i++)
            {
                var box = new RectangleF(r.X + i % 2 * step, r.Y + i / 2 * step, cell, cell);
                Theme.Fill(g, box, 5, Color.White, Color.FromArgb(230, 239, 245));
                Theme.Stroke(g, box, 5, Color.FromArgb(153, 177, 194), 1.2f);
            }
        }
    }
}
