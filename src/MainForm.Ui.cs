using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace WiiUAchievements
{
    // Custom-drawn interface in the style of the Wii U system menus: light striped backdrop,
    // rounded cards, pill buttons with GamePad glyphs and a cyan selection cursor. Layout uses
    // a 96-DPI logical canvas; every frame is drawn in OnPaint and animated by a 15 ms timer that
    // stops itself when nothing moves (ambient motion pauses while the game has focus).
    public sealed partial class MainForm
    {
        enum Kind { Button, Primary, Icon, Tab, Tile, GameTile, Toggle, Field }
        sealed class Widget
        {
            public string Id, Name, Help, Glyph;
            public Kind Kind;
            public RectangleF Bounds;
            public bool Enabled, Visible, Checked;
            public int Layer;
            public Action Click;
            public float Hover, Press, Value = -1;
            public Achievement Achievement;
            public TextBox Box;
        }

        const float TopH = 64, BottomH = 76, Pad = 20, BannerH = 196, TabsH = 40, TileH = 108, Gap = 14, SheetW = 440, DrawerH = 260, CelebrationTime = 2.2f;
        static readonly CultureInfo Italian = MakeCulture();
        readonly Dictionary<string, Widget> widgets = new Dictionary<string, Widget>();
        readonly List<Widget> visible = new List<Widget>();
        readonly System.Windows.Forms.Timer animation = new System.Windows.Forms.Timer();
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly OceanBanner banner = new OceanBanner();
        readonly Dictionary<string, float> celebrations = new Dictionary<string, float>();
        Bitmap measureBitmap, backdrop;
        Graphics measure;
        Widget hovered, pressed, focused;
        Achievement detail;
        float scale = 1, lastFrame, ambientTime, sheet, sheetTarget, drawer, drawerTarget, dialog, dialogTarget;
        float scroll, scrollTarget, logScroll, tabX = -1, tabW, shownRatio, shownRupees = -1, contentHeight, introStart = -10;
        int filter, categoryFilter, chapterFilter, unreadLogs;
        bool focusVisible, cloudPage;
        RectangleF bannerRect, gridRect, drawerRect, sheetRect, dialogRect, statusPill;

        float W { get { return ClientSize.Width / scale; } }
        // Game look for banner-side content; buttons, bars and settings keep the system style.
        // The Games catalog is a system screen and keeps the neutral style.
        GameStyle Style { get { return libraryOpen ? GameStyle.Neutral : GameStyle.For(selectedGame.Id); } }
        GameStyle backdropStyle;
        int shownVideoFrame = -1;
        VideoBanner video;
        bool videoOpened;
        VideoBanner BannerVideo
        {
            get
            {
                if (!videoOpened) { videoOpened = true; video = GameStyle.For(selectedGame.Id).OpenBannerVideo(); }
                return video != null && !video.Failed ? video : null;
            }
        }
        void CloseBannerVideo() { if (video != null) video.Dispose(); video = null; videoOpened = false; }
        float H { get { return ClientSize.Height / scale; } }
        int ActiveLayer { get { return dialogTarget > 0 ? 2 : sheetTarget > 0 ? 1 : 0; } }

        static CultureInfo MakeCulture()
        {
            try { return new CultureInfo("it-IT"); } catch (Exception) { return CultureInfo.InvariantCulture; }
        }

        void BuildUi()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            // Desktop DPI, read without creating this window's handle before its start position is applied.
            try { using (var g = Graphics.FromHwnd(IntPtr.Zero)) scale = Math.Max(1f, g.DpiX / 96f); } catch (Exception) { scale = 1; }
            measureBitmap = new Bitmap(1, 1);
            measure = Graphics.FromImage(measureBitmap);
            measure.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            foreach (var box in new[] { cemuPath, player, cloudEmail, cloudPassword, cloudProject, cloudApiKey })
            {
                box.BorderStyle = BorderStyle.None; box.BackColor = Color.White; box.ForeColor = Theme.Ink;
                box.Font = new Font("Segoe UI", 10.5f); box.Visible = false; box.TabStop = false;
                box.GotFocus += delegate { Invalidate(); };
                box.LostFocus += delegate { Invalidate(); };
                box.TextChanged += delegate { Invalidate(); };
                Controls.Add(box);
            }
            cemuPath.AccessibleName = "Percorso di Cemu.exe"; player.AccessibleName = "Profilo locale"; player.MaxLength = 64;
            cloudEmail.AccessibleName = "Email account cloud"; cloudPassword.AccessibleName = "Password account cloud";
            cloudProject.AccessibleName = "Project ID Firebase"; cloudApiKey.AccessibleName = "Web API key Firebase";
            cloudPassword.UseSystemPasswordChar = true;
            animation.Interval = 15;
            animation.Tick += Animate;
            Activated += delegate { Kick(); };
            Deactivate += delegate { Kick(); };
            Resize += delegate { ClearTileImages(); ClearLayers(); Arrange(); PlaceFields(); Kick(); };
            Shown += delegate { introStart = (float)clock.Elapsed.TotalSeconds; Kick(); };
            try { Icon = MakeIcon(); } catch (Exception) { }
            Disposed += delegate { animation.Dispose(); banner.Dispose(); CloseBannerVideo(); ClearTileImages(); ClearLayers(); if (backdrop != null) backdrop.Dispose(); measure.Dispose(); measureBitmap.Dispose(); };
        }
        static Icon MakeIcon()
        {
            using (var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Theme.Fill(g, new RectangleF(1, 1, 30, 30), 8, Color.FromArgb(76, 204, 250), Theme.AccentDark);
                    Theme.Trophy(g, new RectangleF(5, 4, 22, 22));
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        // ---------------------------------------------------------------- layout
        SizeF Measure(string text, Font font) { return Theme.Measure(measure, text, font); }
        Widget Put(string id, Kind kind, RectangleF bounds, string name, Action click, bool enabled, int layer)
        {
            Widget w;
            if (!widgets.TryGetValue(id, out w)) widgets[id] = w = new Widget { Id = id };
            w.Kind = kind; w.Bounds = bounds; w.Name = name; w.Click = click; w.Enabled = enabled; w.Layer = layer;
            w.Visible = true; w.Glyph = null; w.Help = null; w.Box = null; w.Achievement = null; w.Checked = false;
            visible.Add(w);
            return w;
        }
        float ButtonWidth(string text, string glyph) { return Measure(text, Theme.Font(15, Weight.Bold)).Width + (glyph != null ? 30 : 0) + 40; }
        float AddButton(string id, Kind kind, string text, string glyph, float x, float y, Action click, bool enabled, int layer, float height = 44, float width = 0)
        {
            if (width <= 0) width = ButtonWidth(text, glyph);
            Put(id, kind, new RectangleF(x, y, width, height), text, click, enabled, layer).Glyph = glyph;
            return x + width;
        }
        List<Achievement> Filtered()
        {
            return CategoryRules().Where(a => filter == 0 || (filter == 3 ? a.IsPlanned :
                !a.IsPlanned && (progress != null && progress.Contains(a.Id)) == (filter == 1))).ToList();
        }
        IEnumerable<Achievement> CategoryRules()
        {
            var chapters = Chapters();
            return rules.Where(a => (categoryFilter == 0 || a.Category == AchievementSet.Categories[categoryFilter - 1]) &&
                (chapterFilter == 0 || a.Chapter == chapters[chapterFilter - 1]));
        }
        List<string> Chapters() { return rules.Select(a => a.Chapter).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList(); }
        void NextChapter()
        {
            chapterFilter = (chapterFilter + 1) % (Chapters().Count + 1);
            scroll = scrollTarget = 0; introStart = -10; focused = null; Invalidate(); Kick();
        }
        void NextCategory()
        {
            categoryFilter = (categoryFilter + 1) % (AchievementSet.Categories.Count + 1);
            scroll = scrollTarget = 0; introStart = -10; focused = null; Invalidate(); Kick();
        }

        void Arrange()
        {
            foreach (var item in widgets.Values) item.Visible = false;
            visible.Clear();
            float w = W, h = H;
            Put("settings", Kind.Icon, new RectangleF(w - Pad - 92, 12, 40, 40), "Impostazioni", OpenSheet, true, 0).Help = "Percorso di Cemu, profilo e notifiche";
            Put("log", Kind.Icon, new RectangleF(w - Pad - 40, 12, 40, 40), "Registro attività", ToggleDrawer, true, 0).Help = unreadLogs + " nuove voci";
            AddButton("games", Kind.Button, "Giochi", null, w - Pad - 202, 16, ToggleLibrary, true, 0, 32, 100);
            if (!libraryOpen)
            {
                bannerRect = new RectangleF(Pad, TopH + 16, w - Pad * 2, BannerH);

                float tabsY = bannerRect.Bottom + 16, x = Pad + 4;
                var categoryRules = CategoryRules().ToList();
                int unlocked = categoryRules.Count(a => !a.IsPlanned && progress != null && progress.Contains(a.Id));
                int pending = categoryRules.Count(a => a.IsPlanned);
                string[] names = { "Tutti", "Sbloccati", "Da sbloccare", "Non attivi" };
                int[] counts = { categoryRules.Count, unlocked, categoryRules.Count - pending - unlocked, pending };
                for (int i = 0; i < 4; i++)
                {
                    int index = i;
                    string count = counts[i].ToString(CultureInfo.InvariantCulture);
                    float tw = Measure(names[i], Theme.Font(14.5f, Weight.Bold)).Width + Measure(count, Theme.Font(12, Weight.Heavy)).Width + 52;
                    var tab = Put("tab" + i, Kind.Tab, new RectangleF(x, tabsY + 4, tw, TabsH - 8), names[i], () => SetFilter(index), true, 0);
                    tab.Checked = filter == i; tab.Glyph = count; tab.Help = count + " obiettivi";
                    x += tw;
                }

                AddButton("category", Kind.Button, "Categoria: " + (categoryFilter == 0 ? "Tutte" : AchievementSet.Categories[categoryFilter - 1]), "C",
                    Pad + 4, tabsY + TabsH + 6, NextCategory, true, 0, 30, 270);
                AddButton("chapter", Kind.Button, "Tappa: " + (chapterFilter == 0 ? "Tutte" : Chapters()[chapterFilter - 1]), "F",
                    Pad + 284, tabsY + TabsH + 6, NextChapter, true, 0, 30, 270);
                gridRect = new RectangleF(Pad - 8, tabsY + TabsH + 44, w - Pad * 2 + 16, h - BottomH - (tabsY + TabsH + 44));
                var items = Filtered();
                float inner = gridRect.Width - 16;
                int cols = Math.Max(1, (int)((inner + Gap) / (316 + Gap)));
                float tileW = (inner - Gap * (cols - 1)) / cols;
                int rows = (items.Count + cols - 1) / cols;
                contentHeight = rows * (TileH + Gap) - Gap + 24;
                float maxScroll = Math.Max(0, contentHeight - gridRect.Height);
                scrollTarget = Math.Max(0, Math.Min(maxScroll, scrollTarget));
                scroll = Math.Max(0, Math.Min(maxScroll, scroll));
                for (int i = 0; i < items.Count; i++)
                {
                    var a = items[i];
                    var r = new RectangleF(gridRect.X + 8 + (i % cols) * (tileW + Gap), gridRect.Y + 12 + (i / cols) * (TileH + Gap) - scroll, tileW, TileH);
                    var tile = Put("tile:" + a.Id, Kind.Tile, r, a.Title, () => OpenDetail(a), true, 0);
                    tile.Achievement = a; tile.Help = a.Chapter + ". " + a.Category + ". " + Art.TierName(arts[a.Id].Tier) + ". " + a.Description + (a.IsPlanned ? " Da verificare. " + a.Requirement : a.Experimental ? " Sperimentale: rilevamento implementato, da verificare giocando." : "");
                }

                float by = h - BottomH + 16;
                x = AddButton("connect", Kind.Primary, "Collega Cemu", "A", Pad, by, Connect, CanConnect, 0) + 10;
                x = AddButton("disconnect", Kind.Button, "Scollega", "Y", x, by, () => Disconnect("Sessione scollegata."), CanDisconnect, 0) + 10;
                if (CanDisconnect) AddButton("pause", Kind.Button, paused ? "Riprendi" : "Pausa", "P", x, by, TogglePause, memory != null || demo, 0);
                else AddButton("demo", Kind.Button, "Prova demo", "X", x, by, StartDemo, CanConnect, 0);
            }
            else ArrangeLibrary(w, h);

            drawerRect = new RectangleF(Pad, h - BottomH - DrawerH * Theme.EaseOut(drawer), w - Pad * 2, DrawerH + 24);
            if (drawer > 0) Put("log-close", Kind.Icon, new RectangleF(drawerRect.Right - 54, drawerRect.Y + 14, 36, 36), "Chiudi registro", CloseDrawer, true, 0);

            sheetRect = new RectangleF(w - SheetW * Theme.EaseOut(sheet), 0, SheetW, h);
            if (sheet > 0)
            {
                float sx = sheetRect.X;
                AddButton("back", Kind.Button, "Indietro", "B", sx + SheetW - 24 - ButtonWidth("Indietro", "B"), 13, CloseSheet, true, 1, 38);
                if (cloudPage)
                {
                    bool signedIn = cloud != null && cloud.IsSignedIn;
                    bool canAuthenticate = !cloudBusy && !signedIn;
                    string[] ids = { "field-project", "field-apikey", "field-email", "field-pass" };
                    string[] labels = { "Project ID Firebase", "Web API key", "Email account", "Password" };
                    TextBox[] boxes = { cloudProject, cloudApiKey, cloudEmail, cloudPassword };
                    for (int i = 0; i < boxes.Length; i++) { var box = boxes[i]; Put(ids[i], Kind.Field, new RectangleF(sx + 24, 134 + i * 78, SheetW - 48, 40), labels[i], () => FocusField(box), canAuthenticate, 1).Box = box; }
                    float bw = (SheetW - 72) / 3;
                    AddButton("cloud-signup", Kind.Button, "Registrati", null, sx + 24, 450, CloudSignUp, canAuthenticate, 1, 40, bw);
                    AddButton("cloud-login", Kind.Button, "Accedi", null, sx + 36 + bw, 450, CloudLogin, canAuthenticate, 1, 40, bw);
                    AddButton("cloud-logout", Kind.Button, "Esci", null, sx + 48 + bw * 2, 450, CloudLogout, !cloudBusy && signedIn, 1, 40, bw);
                }
                else
                {
                Put("field-cemu", Kind.Field, new RectangleF(sx + 24, 134, SheetW - 48 - 112, 40), "Percorso di Cemu.exe", () => FocusField(cemuPath), FieldsEditable, 1).Box = cemuPath;
                AddButton("browse", Kind.Button, "Sfoglia…", null, sx + SheetW - 24 - 102, 134, Browse, FieldsEditable, 1, 40, 102);
                Put("field-player", Kind.Field, new RectangleF(sx + 24, 212, SheetW - 48, 40), "Profilo locale", () => FocusField(player), FieldsEditable, 1).Box = player;
                Put("sound", Kind.Toggle, new RectangleF(sx + 24, 352, SheetW - 48, 46), "Suono di sblocco", ToggleSound, true, 1).Checked = soundOn;
                Put("ambient", Kind.Toggle, new RectangleF(sx + 24, 402, SheetW - 48, 46), "Animazioni del banner", ToggleAmbient, true, 1).Checked = ambientOn;
                AddButton("test", Kind.Button, "Prova notifica", null, sx + 24, 462, TestToast, true, 1, 40);
                AddButton("cloud-page", Kind.Button, "Account cloud…", null, sx + SheetW - 196, 462, OpenCloudPage, true, 1, 40, 172);
                }
            }
            if (dialog > 0 && detail != null)
            {
                dialogRect = new RectangleF((w - 520) / 2, Math.Max(12, (h - 448) / 2), 520, 448);
                AddButton("close", Kind.Primary, "Chiudi", "B", dialogRect.X + (520 - ButtonWidth("Chiudi", "B")) / 2, dialogRect.Bottom - 68, CloseDialog, true, 2);
            }
        }
        void PlaceFields()
        {
            bool show = sheet >= 1 && sheetTarget >= 1;
            foreach (var box in new[] { cemuPath, player, cloudEmail, cloudPassword, cloudProject, cloudApiKey })
            {
                Widget w;
                string id = box == cemuPath ? "field-cemu" : box == player ? "field-player" : box == cloudEmail ? "field-email" : box == cloudPassword ? "field-pass" : box == cloudProject ? "field-project" : "field-apikey";
                if (show && widgets.TryGetValue(id, out w) && w.Visible)
                {
                    var r = w.Bounds;
                    int height = box.PreferredHeight;
                    var device = new Rectangle((int)Math.Round((r.X + 13) * scale), (int)Math.Round(r.Y * scale + (r.Height * scale - height) / 2), (int)((r.Width - 26) * scale), height);
                    if (box.Bounds != device) box.Bounds = device;
                    box.ReadOnly = box == cemuPath || box == player ? !FieldsEditable : cloudBusy || (cloud != null && cloud.IsSignedIn);
                    box.BackColor = box.ReadOnly ? Color.FromArgb(240, 243, 245) : Color.White;
                    if (!box.Visible) box.Visible = true;
                }
                else if (box.Visible)
                {
                    if (box.Focused) { ActiveControl = null; Focus(); }
                    box.Visible = false;
                }
            }
        }

        // ---------------------------------------------------------------- animation
        void Kick()
        {
            if (animation.Enabled || IsDisposed) return;
            lastFrame = (float)clock.Elapsed.TotalSeconds;
            animation.Start();
        }
        static bool Step(ref float value, float target, float delta)
        {
            if (value == target) return false;
            value = value < target ? Math.Min(target, value + delta) : Math.Max(target, value - delta);
            return true;
        }
        Rectangle Device(RectangleF r)
        {
            return Rectangle.FromLTRB((int)Math.Floor(r.Left * scale) - 2, (int)Math.Floor(r.Top * scale) - 2, (int)Math.Ceiling(r.Right * scale) + 2, (int)Math.Ceiling(r.Bottom * scale) + 2);
        }
        void Animate(object sender, EventArgs e)
        {
            float now = (float)clock.Elapsed.TotalSeconds, dt = Math.Max(0.001f, Math.Min(0.05f, now - lastFrame));
            lastFrame = now;
            // Repaint only what moved: a whole-window repaint at ultrawide sizes costs several
            // frames' budget. "busy" means the layout of overlapping layers is changing.
            bool busy = false;
            var dirty = new List<RectangleF>();
            foreach (var w in widgets.Values)
            {
                float hv = Theme.Approach(w.Hover, w == hovered && w.Enabled && w.Visible ? 1 : 0, dt, 16);
                float pr = Theme.Approach(w.Press, w == pressed && w.Enabled ? 1 : 0, dt, 30);
                if (hv != w.Hover || pr != w.Press)
                {
                    w.Hover = hv; w.Press = pr;
                    if (w.Visible) dirty.Add(Theme.Inflate(w.Bounds, 24, 24));
                }
                if (w.Kind != Kind.Toggle || !w.Visible) continue;
                float v = w.Value < 0 ? (w.Checked ? 1 : 0) : Theme.Approach(w.Value, w.Checked ? 1 : 0, dt, 14);
                if (v != w.Value) { w.Value = v; dirty.Add(Theme.Inflate(w.Bounds, 8, 8)); }
            }
            float sheetBefore = sheet;
            busy |= Step(ref sheet, sheetTarget, dt / 0.32f);
            busy |= Step(ref drawer, drawerTarget, dt / 0.3f);
            busy |= Step(ref dialog, dialogTarget, dt / 0.34f);
            if (dialog >= 1 && detail != null && progress != null && progress.Contains(detail.Id)) dirty.Add(Theme.Inflate(dialogRect, 44, 44));
            float s = Theme.Approach(scroll, scrollTarget, dt, 14);
            if (Math.Abs(s - scrollTarget) < 0.3f) s = scrollTarget;
            bool scrolled = s != scroll;
            if (scrolled) scroll = s;
            Widget tab;
            if (widgets.TryGetValue("tab" + filter, out tab))
            {
                if (tabX < 0) { tabX = tab.Bounds.X; tabW = tab.Bounds.Width; }
                float nx = Theme.Approach(tabX, tab.Bounds.X, dt, 18), nw = Theme.Approach(tabW, tab.Bounds.Width, dt, 18);
                if (Math.Abs(nx - tab.Bounds.X) < 0.3f) nx = tab.Bounds.X;
                if (Math.Abs(nw - tab.Bounds.Width) < 0.3f) nw = tab.Bounds.Width;
                if (nx != tabX || nw != tabW) { tabX = nx; tabW = nw; dirty.Add(new RectangleF(0, bannerRect.Bottom, W, TabsH + 24)); }
            }
            float ratio = ActiveCount == 0 ? 0 : UnlockedCount / (float)ActiveCount;
            float nr = Theme.Approach(shownRatio, ratio, dt, 4.5f);
            if (nr != shownRatio) { shownRatio = nr; dirty.Add(Theme.Inflate(bannerRect, 22, 22)); }
            float rupees = current == null ? 0 : current.Rupees;
            if (shownRupees < 0) shownRupees = rupees;
            float nrp = Theme.Approach(shownRupees, rupees, dt, 7);
            if (Math.Abs(nrp - rupees) < 0.5f) nrp = rupees;
            if (nrp != shownRupees) { shownRupees = nrp; dirty.Add(Theme.Inflate(bannerRect, 22, 22)); }
            var gridBand = new RectangleF(0, gridRect.Y, W, gridRect.Height);
            if (now - introStart < 1.4f) dirty.Add(libraryOpen ? new RectangleF(0, 0, W, H) : gridBand);
            if (celebrations.Count > 0)
            {
                foreach (var id in celebrations.Where(c => now - c.Value > CelebrationTime).Select(c => c.Key).ToList()) celebrations.Remove(id);
                dirty.Add(gridBand);
            }
            if (sheet != sheetBefore || sheet >= 1) { Arrange(); PlaceFields(); }
            bool ambient = ambientOn && WindowState != FormWindowState.Minimized && ActiveForm == this;
            var playing = BannerVideo;
            if (playing != null) playing.SetPlaying(ambient);
            if (ambient) ambientTime += dt;
            if (busy) { Invalidate(); return; }
            // One paint covers the bounding box of everything invalidated: while the grid
            // scrolls, the banner waits instead of stretching the box over the whole window.
            if (scrolled) dirty.Add(gridBand);
            bool idle = dirty.Count == 0;
            foreach (var r in dirty) Invalidate(Device(r));
            if (scrolled) return;
            if (ambient)
            {
                // The banner only changes when the video has a new frame (or the drawn sea moves).
                int frames = playing != null && playing.HasFrame ? playing.Frames : -1;
                if (frames < 0 || frames != shownVideoFrame || libraryOpen)
                {
                    shownVideoFrame = frames;
                    Invalidate(Device(Theme.Inflate(libraryOpen ? libraryGridRect : bannerRect, 4, 4)));
                }
                if (statusKind == StatusKind.Live || statusKind == StatusKind.Demo || statusKind == StatusKind.Busy) Invalidate(Device(Theme.Inflate(statusPill, 4, 4)));
                idle = false;
            }
            if (focusVisible && focused != null && focused.Visible && ContainsFocus) { Invalidate(Device(Theme.Inflate(focused.Bounds, 16, 16))); idle = false; }
            if (idle) animation.Stop();
        }
        void Celebrate(Achievement a) { celebrations[a.Id] = (float)clock.Elapsed.TotalSeconds; Kick(); }
        float Celebration(string id, float t)
        {
            float start;
            if (!celebrations.TryGetValue(id, out start)) return -1;
            float u = (t - start) / CelebrationTime;
            return u >= 1 ? -1 : Math.Max(0, u);
        }
        float FocusStrength(Widget w, float t)
        {
            if (!focusVisible || w != focused || !ContainsFocus) return 0;
            return 0.75f + 0.25f * (float)Math.Sin(t * 5);
        }

        // ---------------------------------------------------------------- painting
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var c = e.ClipRectangle;
            Render(e.Graphics, new RectangleF(c.X / scale, c.Y / scale, c.Width / scale, c.Height / scale));
        }
        // Used by the window and by the --preview rendering check of our own UI.
        public void SavePreview(string path)
        {
            using (var bitmap = new Bitmap(Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height)))
            {
                using (var g = Graphics.FromImage(bitmap)) Render(g, new RectangleF(0, 0, W, H));
                bitmap.Save(path, ImageFormat.Png);
            }
        }
        void Render(Graphics g, RectangleF clip)
        {
            Arrange();
            float t = (float)clock.Elapsed.TotalSeconds;
            DrawBackdrop(g, clip);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.ScaleTransform(scale, scale);
            if (libraryOpen) DrawLibrary(g, t);
            else
            {
                if (clip.IntersectsWith(gridRect)) DrawGrid(g, clip, t);
                if (clip.IntersectsWith(new RectangleF(0, bannerRect.Bottom, W, TabsH + 24))) DrawTabs(g, t);
                if (clip.IntersectsWith(Theme.Inflate(bannerRect, 22, 22))) DrawBanner(g);
            }
            if (clip.IntersectsWith(new RectangleF(0, 0, W, TopH + 10))) DrawTopBar(g, t);
            if (drawer > 0) DrawDrawer(g, t);
            if (clip.IntersectsWith(new RectangleF(0, H - BottomH - 12, W, BottomH + 12))) DrawBottomBar(g, t, clip.Bottom > H - BottomH + 12);
            if (sheet > 0) DrawSheet(g, t);
            if (dialog > 0 && detail != null) DrawDialog(g, t);
        }
        void DrawBackdrop(Graphics g, RectangleF clip)
        {
            var size = ClientSize;
            if (size.Width < 1 || size.Height < 1) return;
            if (backdrop == null || backdrop.Size != size || backdropStyle != Style)
            {
                backdropStyle = Style;
                if (backdrop != null) backdrop.Dispose();
                backdrop = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
                using (var b = Graphics.FromImage(backdrop))
                {
                    b.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var brush = new LinearGradientBrush(new Rectangle(0, -1, size.Width, size.Height + 2), Style.BackTop, Style.BackBottom, 90f))
                        b.FillRectangle(brush, 0, 0, size.Width, size.Height);
                    Blob(b, new PointF(size.Width * 0.04f, size.Height * 0.42f), size.Width * 0.5f, Color.FromArgb(46, Style.Glow));
                    Blob(b, new PointF(size.Width * 0.98f, size.Height * 0.95f), size.Width * 0.55f, Color.FromArgb(40, Style.Accent));
                    if (!Style.Themed)
                        using (var pen = new Pen(Color.FromArgb(10, 0, 50, 80), 1.3f * scale))
                            for (float d = -size.Height; d < size.Width; d += 8 * scale) b.DrawLine(pen, d, size.Height, d + size.Height, 0);
                    Style.DecorateBackdrop(b, size, scale);
                }
            }
            var mode = g.CompositingMode;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            var area = Rectangle.Intersect(Device(clip), new Rectangle(0, 0, size.Width, size.Height));
            if (area.Width > 0 && area.Height > 0) g.DrawImage(backdrop, area, area.X, area.Y, area.Width, area.Height, GraphicsUnit.Pixel);
            g.CompositingMode = mode;
            g.InterpolationMode = InterpolationMode.Bilinear;
        }
        static void Blob(Graphics g, PointF c, float radius, Color color)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(Theme.Square(c, radius * 2));
                using (var b = new PathGradientBrush(path) { CenterColor = color, SurroundColors = new[] { Color.FromArgb(0, color) } }) g.FillPath(b, path);
            }
        }

        string StatusShort()
        {
            switch (statusKind)
            {
                case StatusKind.Live: return "Collegato";
                case StatusKind.Demo: return "Demo attiva";
                case StatusKind.Paused: return "In pausa";
                case StatusKind.Busy: return "Collegamento…";
                case StatusKind.Error: return "Errore";
                default: return "Non collegato";
            }
        }
        Color StatusColor()
        {
            switch (statusKind)
            {
                case StatusKind.Live: return Theme.Success;
                case StatusKind.Demo: return Theme.Warning;
                case StatusKind.Paused: return Theme.Warning;
                case StatusKind.Busy: return Theme.Accent;
                case StatusKind.Error: return Theme.Danger;
                default: return Color.FromArgb(160, 170, 178);
            }
        }

        void DrawTopBar(Graphics g, float t)
        {
            float w = W;
            var style = Style;
            Layer(g, "top-bar", new RectangleF(0, 0, w, TopH + 8), style.GameId, b =>
            {
                using (var brush = new LinearGradientBrush(new RectangleF(0, -1, w, TopH + 2), style.BarTop, style.BarBottom, 90f)) b.FillRectangle(brush, 0, 0, w, TopH);
                using (var brush = new LinearGradientBrush(new RectangleF(0, TopH - 1, w, 10), Color.FromArgb(28, 20, 44, 64), Color.FromArgb(0, 20, 44, 64), 90f)) b.FillRectangle(brush, 0, TopH, w, 8);
                using (var pen = new Pen(style.BarLine)) b.DrawLine(pen, 0, TopH - 0.5f, w, TopH - 0.5f);
                style.DecorateTopBar(b, w, TopH);
            });
            var avatar = new RectangleF(Pad, 12, 40, 40);
            using (var b = new SolidBrush(Color.FromArgb(30, 20, 44, 64))) g.FillEllipse(b, Theme.Inflate(avatar, 3, 3).X, avatar.Y - 1, avatar.Width + 6, avatar.Height + 6);
            using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, Theme.Inflate(avatar, 3, 3));
            Theme.Avatar(g, avatar);
            string name = String.IsNullOrWhiteSpace(player.Text) ? "Profilo senza nome" : player.Text.Trim();
            var nameFont = Theme.Font(16, Weight.Bold);
            float nameW = Math.Min(230, Measure(name, nameFont).Width + 4);
            Theme.Text(g, name, nameFont, Theme.Ink, new RectangleF(Pad + 54, 12, 230, 22));
            Theme.Text(g, demo ? "Simulazione separata" : "Profilo locale", Theme.Font(12, Weight.Regular), style.Muted, new RectangleF(Pad + 54, 35, 230, 18));

            var settings = widgets["settings"];
            string label = StatusShort();
            var pillFont = Theme.Font(13.5f, Weight.Bold);
            float pw = Measure(label, pillFont).Width + 46;
            statusPill = new RectangleF(widgets["games"].Bounds.X - 14 - pw, 16, pw, 32);
            Theme.Fill(g, statusPill, 16, style.ButtonTop, style.ButtonBottom);
            Theme.Stroke(g, statusPill, 16, style.Themed ? style.ButtonEdge : Theme.Line, 1);
            var dot = new PointF(statusPill.X + 18, statusPill.Y + 16);
            Color dc = StatusColor();
            if (statusKind == StatusKind.Live || statusKind == StatusKind.Demo || statusKind == StatusKind.Busy)
            {
                float f = (t * 0.8f) % 1f;
                using (var b = new SolidBrush(Color.FromArgb((int)(120 * (1 - f)), dc))) g.FillEllipse(b, Theme.Square(dot, 10 + 14 * f));
            }
            using (var b = new SolidBrush(dc)) g.FillEllipse(b, Theme.Square(dot, 10));
            Theme.Text(g, label, pillFont, Theme.Ink, new RectangleF(statusPill.X + 31, statusPill.Y, pw - 34, 32), StringAlignment.Near, StringAlignment.Center, false);

            const string title = "Wii U Achievements";
            var titleFont = Theme.Font(20, Weight.Heavy);
            float tw = Measure(title, titleFont).Width + 36, tx = (w - tw) / 2;
            if (tx > Pad + 54 + nameW + 16 && tx + tw < statusPill.X - 16)
            {
                if (style.Themed && !style.Crest(g, new PointF(tx + 14 + tw / 2, TopH - 1.5f), 190, 1)) style.Crest(g, new PointF(tx + 14 + tw / 2, TopH - 1), 15);
                Theme.Trophy(g, new RectangleF(tx, 17, 28, 28));
                Theme.Text(g, title, titleFont, Theme.Ink, new RectangleF(tx + 36, 14, tw, 34), StringAlignment.Near, StringAlignment.Center, false);
            }
            DrawButton(g, widgets["games"], t);
            DrawIconButton(g, settings, t, (gg, r, c) => Theme.Gear(gg, r, c));
            var log = widgets["log"];
            DrawIconButton(g, log, t, (gg, r, c) => Theme.ListIcon(gg, r, c));
            if (unreadLogs > 0)
            {
                string n = unreadLogs > 99 ? "99+" : unreadLogs.ToString(CultureInfo.InvariantCulture);
                var f = Theme.Font(11, Weight.Heavy);
                float bw = Math.Max(20, Measure(n, f).Width + 10);
                var badge = new RectangleF(log.Bounds.Right - bw + 6, log.Bounds.Y - 4, bw, 20);
                Theme.Fill(g, Theme.Inflate(badge, 1.5f, 1.5f), 11.5f, Color.White, Color.White);
                Theme.Fill(g, badge, 10, Color.FromArgb(244, 96, 104), Theme.Danger);
                Theme.Text(g, n, f, Color.White, badge, StringAlignment.Center, StringAlignment.Center, false);
            }
        }
        void DrawIconButton(Graphics g, Widget w, float t, Action<Graphics, RectangleF, Color> icon)
        {
            var r = w.Bounds;
            if (w.Press > 0) r = Theme.Inflate(r, -1.5f * w.Press, -1.5f * w.Press);
            float hv = w.Hover;
            using (var b = new SolidBrush(Color.FromArgb(28 + (int)(26 * hv), 20, 44, 64))) g.FillEllipse(b, r.X, r.Y + 2, r.Width, r.Height);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(r);
                using (var b = new LinearGradientBrush(Theme.Inflate(r, 0, 1), Style.ButtonTop, Style.ButtonBottom, 90f)) g.FillPath(b, path);
            }
            using (var pen = new Pen(Theme.Mix(Style.Themed ? Style.ButtonEdge : Color.FromArgb(196, 205, 212), Style.Accent, hv), (Style.Themed ? 1.4f : 1) + hv)) g.DrawEllipse(pen, r);
            icon(g, Theme.Inflate(r, -r.Width * 0.27f, -r.Height * 0.27f), Theme.Mix(Style.Themed ? Style.GlyphFill : Color.FromArgb(108, 118, 126), Style.Accent, hv));
            Theme.Cursor(g, r, r.Width / 2, FocusStrength(w, t));
        }

        // Device-resolution bitmaps rebuilt only when their key changes; frames copy them 1:1.
        sealed class LayerImage { public Bitmap Bitmap; public string Key; }
        readonly Dictionary<string, LayerImage> layers = new Dictionary<string, LayerImage>();
        int layerBuilds;
        void ClearLayers() { foreach (var layer in layers.Values) layer.Bitmap.Dispose(); layers.Clear(); }
        void Layer(Graphics g, string id, RectangleF area, string key, Action<Graphics> paint)
        {
            key += string.Format(CultureInfo.InvariantCulture, "|{0:0.0},{1:0.0},{2:0.0},{3:0.0}@{4}", area.X, area.Y, area.Width, area.Height, scale);
            LayerImage layer;
            if (!layers.TryGetValue(id, out layer) || layer.Key != key)
            {
                if (layer != null) layer.Bitmap.Dispose();
                var bitmap = new Bitmap(Math.Max(1, (int)Math.Ceiling(area.Width * scale)), Math.Max(1, (int)Math.Ceiling(area.Height * scale)), PixelFormat.Format32bppPArgb);
                try
                {
                    using (var b = Graphics.FromImage(bitmap))
                    {
                        // Text over transparency must use greyscale antialiasing (ClearType needs a backdrop).
                        b.SmoothingMode = SmoothingMode.AntiAlias; b.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; b.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        b.ScaleTransform(scale, scale); b.TranslateTransform(-area.X, -area.Y);
                        paint(b);
                    }
                }
                catch { bitmap.Dispose(); layers.Remove(id); throw; }
                layers[id] = layer = new LayerImage { Bitmap = bitmap, Key = key };
                layerBuilds++;
            }
            var interpolation = g.InterpolationMode; var offset = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(layer.Bitmap, (float)Math.Round(area.X * scale) / scale, (float)Math.Round(area.Y * scale) / scale, layer.Bitmap.Width / scale, layer.Bitmap.Height / scale);
            g.InterpolationMode = interpolation; g.PixelOffsetMode = offset;
        }

        void DrawBanner(Graphics g)
        {
            var r = bannerRect;
            Layer(g, "banner-shadow", Theme.Inflate(r, 24, 24), "shadow", b => Theme.Shadow(b, r, 22, 14, 80, 6));
            var playing = BannerVideo;
            bool drawn = false;
            // A video hiccup must never break painting: fall back to the drawn sea for that frame.
            try { drawn = playing != null && playing.Draw(g, r, 22); } catch (Exception) { drawn = false; }
            if (!drawn) banner.Draw(g, r, 22, ambientTime, scale);
            // Everything above the moving picture changes only with the session state.
            string key = Style.GameId + drawn + statusKind + statusTitle + "|" + statusDetail + "|" + selectedGame.Id + "|" + UnlockedCount + "/" + ActiveCount + "|" +
                shownRatio.ToString("0.0000", CultureInfo.InvariantCulture) + "|" + (current == null ? "-" : Math.Round(shownRupees) + "," + current.Health + "," + current.MaxHealth + "," + EarnedPoints);
            Layer(g, "banner-overlay", Theme.Inflate(r, 12, 12), key, b => DrawBannerOverlay(b, drawn));
        }
        void DrawBannerOverlay(Graphics g, bool drawn)
        {
            var r = bannerRect;
            if (drawn)
            {
                // Soft sea-blue veil on the left keeps the title readable over any time of day.
                var state = g.Save();
                using (var clip = Theme.Round(r, 22)) g.SetClip(clip, CombineMode.Intersect);
                using (var b = new LinearGradientBrush(new RectangleF(r.X - 1, r.Y, r.Width * 0.62f + 2, r.Height), Color.FromArgb(150, Style.Deep), Color.FromArgb(0, Style.Deep), 0f))
                    g.FillRectangle(b, r.X, r.Y, r.Width * 0.62f, r.Height);
                g.Restore(state);
            }
            Theme.Stroke(g, Theme.Inflate(r, -0.5f, -0.5f), 22, Color.FromArgb(80, 255, 255, 255), 1.2f);
            if (Style.Themed)
            {
                using (var pen = new Pen(Color.FromArgb(200, 46, 32, 26), 4.5f)) using (var path = Theme.Round(Theme.Inflate(r, 1.5f, 1.5f), 23.5f)) g.DrawPath(pen, path);
                using (var b = new LinearGradientBrush(Theme.Inflate(r, 2, 2), Theme.Light(Style.Trim, 0.35f), Style.TrimDark, 90f))
                using (var pen = new Pen(b, 2.6f)) using (var path = Theme.Round(Theme.Inflate(r, 1.5f, 1.5f), 23.5f)) g.DrawPath(pen, path);
                Style.Corners(g, r, 46, 5);
            }
            float tx = r.X + 28, textW = r.Width - 28 - 240;
            Theme.ShadowText(g, selectedGame.Title, Theme.Font(31, Weight.Heavy), Color.White, new RectangleF(tx, r.Y + 18, textW, 44), StringAlignment.Near, StringAlignment.Near, false, 90);
            Theme.ShadowText(g, selectedGame.Profiles[0].Label + "  ·  Cemu " + selectedGame.Profiles[0].CemuVersion + "  ·  Playthrough TEST", Theme.Font(13.5f, Weight.Bold), Color.FromArgb(225, 255, 255, 255), new RectangleF(tx + 1, r.Y + 61, textW, 20), StringAlignment.Near, StringAlignment.Near, false, 70);
            var dot = new PointF(tx + 7, r.Y + 101);
            using (var b = new SolidBrush(Color.FromArgb(235, 255, 255, 255))) g.FillEllipse(b, Theme.Square(dot, 14));
            using (var b = new SolidBrush(StatusColor())) g.FillEllipse(b, Theme.Square(dot, 9));
            Theme.ShadowText(g, statusTitle, Theme.Font(16, Weight.Heavy), Color.White, new RectangleF(tx + 21, r.Y + 90, textW - 21, 24), StringAlignment.Near, StringAlignment.Near, false, 80);
            bool chips = current != null;
            Theme.ShadowText(g, statusDetail, Theme.Font(13, Weight.Bold), Color.FromArgb(232, 242, 250, 255), new RectangleF(tx + 21, r.Y + 113, textW - 21, chips ? 20 : 64), StringAlignment.Near, StringAlignment.Near, !chips, 70);
            if (chips) DrawChips(g, tx - 4, r.Bottom - 56);
            DrawRing(g, new PointF(r.Right - 120, r.Y + r.Height / 2));
        }
        static string Hearts(int quarters)
        {
            string[] parts = { "", "¼", "½", "¾" };
            int whole = quarters / 4, rest = quarters % 4;
            return whole == 0 && rest > 0 ? parts[rest] : whole + parts[rest];
        }
        void DrawChips(Graphics g, float x, float y)
        {
            const float h = 42;
            x = Chip(g, x, y, h, 22, 26, (gg, r) => Theme.Gem(gg, r, Theme.GemColor(0), 0), ((int)Math.Round(shownRupees)).ToString(CultureInfo.InvariantCulture), "rupie") + 10;
            int max = Math.Max(1, current.MaxHealth / 4), perRow = Math.Min(10, max);
            float size = max > 10 ? 12 : 15, step = size + 2.5f, iconW = perRow * step - 2.5f, iconH = max > 10 ? size * 2 + 3 : size;
            int health = current.Health;
            x = Chip(g, x, y, h, iconW, iconH, (gg, r) =>
            {
                for (int k = 0; k < max; k++)
                {
                    var cell = new RectangleF(r.X + (k % 10) * step, r.Y + (k / 10) * (size + 3), size, size);
                    Theme.HeartQuarters(gg, cell, Math.Max(0, Math.Min(4, health - k * 4)));
                }
            }, Hearts(current.Health) + "/" + max, "cuori") + 10;
            Chip(g, x, y, h, 22, 22, (gg, r) => Theme.Star(gg, r, Color.FromArgb(255, 228, 118), Color.FromArgb(226, 152, 20)), EarnedPoints.ToString(CultureInfo.InvariantCulture), "punti");
        }
        float Chip(Graphics g, float x, float y, float h, float iconW, float iconH, Action<Graphics, RectangleF> icon, string value, string label)
        {
            var vf = Theme.Font(20, Weight.Heavy);
            var lf = Theme.Font(12, Weight.Bold);
            float vw = Measure(value, vf).Width, lw = Measure(label, lf).Width;
            float w = 14 + iconW + 9 + vw + 5 + lw + 16;
            var r = new RectangleF(x, y, w, h);
            Theme.Shadow(g, r, h / 2, 6, 60, 2);
            Theme.Fill(g, r, h / 2, Color.FromArgb(244, Style.ButtonTop), Color.FromArgb(232, Style.Themed ? Style.ButtonBottom : Color.FromArgb(234, 243, 248)));
            if (Style.Themed) Theme.Stroke(g, r, h / 2, Style.ButtonEdge, 1.3f);
            icon(g, new RectangleF(x + 14, y + (h - iconH) / 2, iconW, iconH));
            Theme.Text(g, value, vf, Theme.Ink, new RectangleF(x + 14 + iconW + 9, y, vw + 6, h), StringAlignment.Near, StringAlignment.Center, false);
            Theme.Text(g, label, lf, Style.Muted, new RectangleF(x + 14 + iconW + 9 + vw + 5, y + 3, lw + 6, h), StringAlignment.Near, StringAlignment.Center, false);
            return x + w;
        }
        void DrawRing(Graphics g, PointF c)
        {
            const float R = 78;
            var disc = Theme.Square(c, R * 2);
            Theme.Shadow(g, disc, R, 10, 70, 4);
            using (var b = new LinearGradientBrush(Theme.Inflate(disc, 0, 1), Color.FromArgb(246, Style.CardTop), Color.FromArgb(238, Style.Themed ? Style.CardBottom : Color.FromArgb(231, 241, 246)), 90f)) g.FillEllipse(b, disc);
            Style.CompassRing(g, c, R);
            var arc = Theme.Square(c, 124);
            using (var pen = new Pen(Style.Themed ? Style.TrackTop : Color.FromArgb(217, 227, 234), 12)) g.DrawEllipse(pen, arc);
            bool full = shownRatio >= 0.999f;
            float sweep = 360 * shownRatio;
            if (sweep > 0.5f)
                using (var brush = new LinearGradientBrush(Theme.Inflate(arc, 8, 8), full ? Color.FromArgb(255, 220, 90) : Style.Glow, full ? Color.FromArgb(226, 146, 16) : Style.AccentDark, 45f))
                using (var pen = new Pen(brush, 12) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(pen, arc, -90, Math.Max(1, sweep));
            Theme.Text(g, ((int)Math.Round(shownRatio * 100)).ToString(CultureInfo.InvariantCulture) + "%", Theme.Font(30, Weight.Heavy), Theme.Ink, new RectangleF(c.X - 60, c.Y - 34, 120, 42), StringAlignment.Center, StringAlignment.Center, false);
            Theme.Text(g, UnlockedCount + " di " + ActiveCount, Theme.Font(13, Weight.Bold), Style.Muted, new RectangleF(c.X - 60, c.Y + 6, 120, 20), StringAlignment.Center, StringAlignment.Center, false);
            Theme.Text(g, "ATTIVI", Theme.Font(10.5f, Weight.Heavy), Style.Accent, new RectangleF(c.X - 60, c.Y + 24, 120, 16), StringAlignment.Center, StringAlignment.Center, false);
        }

        void DrawTabs(Graphics g, float t)
        {
            var t0 = widgets["tab0"].Bounds;
            var t2 = widgets["tab3"].Bounds;
            var box = new RectangleF(t0.X - 4, t0.Y - 4, t2.Right - t0.X + 8, t0.Height + 8);
            var style = Style;
            Layer(g, "tab-track", Theme.Inflate(box, 2, 2), style.GameId, b =>
            {
                Theme.Fill(b, box, box.Height / 2, style.TrackTop, style.TrackBottom);
                style.Parchment(b, box, box.Height / 2, 0.5f);
                Theme.Stroke(b, box, box.Height / 2, style.TrackEdge, style.Themed ? 1.3f : 1);
            });
            var sel = widgets["tab" + filter].Bounds;
            if (tabX < 0) { tabX = sel.X; tabW = sel.Width; }
            var highlight = new RectangleF(tabX, sel.Y, tabW, sel.Height);
            Theme.Shadow(g, highlight, highlight.Height / 2, 4, 54, 1.5f);
            Theme.Fill(g, highlight, highlight.Height / 2, style.ButtonTop, style.Themed ? Theme.Light(style.ButtonBottom, 0.4f) : Color.FromArgb(242, 246, 249));
            if (style.Themed) Theme.Stroke(g, highlight, highlight.Height / 2, style.Trim, 1.6f);
            var nameFont = Theme.Font(14.5f, Weight.Bold);
            var countFont = Theme.Font(12, Weight.Heavy);
            for (int i = 0; i < 4; i++)
            {
                var w = widgets["tab" + i];
                bool on = filter == i;
                float nw = Measure(w.Name, nameFont).Width, cw = Measure(w.Glyph, countFont).Width + 14;
                float x = w.Bounds.X + (w.Bounds.Width - (nw + 8 + cw)) / 2;
                Color ink = on ? style.AccentDark : Theme.Mix(style.Muted, Theme.Ink, w.Hover);
                Theme.Text(g, w.Name, nameFont, ink, new RectangleF(x, w.Bounds.Y, nw + 6, w.Bounds.Height), StringAlignment.Near, StringAlignment.Center, false);
                var bubble = new RectangleF(x + nw + 8, w.Bounds.Y + (w.Bounds.Height - 20) / 2, cw, 20);
                Theme.Fill(g, bubble, 10, on ? style.BubbleTop : style.BubbleOffTop, on ? style.BubbleBottom : style.BubbleOffBottom);
                Theme.Text(g, w.Glyph, countFont, Color.White, bubble, StringAlignment.Center, StringAlignment.Center, false);
                Theme.Cursor(g, w.Bounds, w.Bounds.Height / 2, FocusStrength(w, t));
            }
            int total = rules.Where(a => !a.IsPlanned).Sum(a => a.Points);
            string summary = "Punti locali  " + EarnedPoints + " / " + total;
            var f = Theme.Font(14, Weight.Bold);
            float sw = Measure(summary, f).Width + 4, sx = W - Pad - sw;
            if (sx - 26 > box.Right + 16)
            {
                Theme.Star(g, new RectangleF(sx - 26, box.Y + 11, 19, 19), Color.FromArgb(255, 228, 118), Color.FromArgb(226, 152, 20));
                Theme.Text(g, summary, f, style.Muted, new RectangleF(sx, box.Y, sw, box.Height), StringAlignment.Near, StringAlignment.Center, false);
            }
            DrawButton(g, widgets["category"], t);
            DrawButton(g, widgets["chapter"], t);
            Theme.Text(g, rules.Count(a => a.Experimental) + " sperimentali · " + (rules.Count - ActiveCount) + " non attivi", Theme.Font(11, Weight.Bold), style.Muted,
                new RectangleF(Pad + 568, box.Bottom + 6, W - Pad * 2 - 568, 30));
        }

        void DrawGrid(Graphics g, RectangleF clip, float t)
        {
            var state = g.Save();
            g.SetClip(new RectangleF(0, gridRect.Y, W, gridRect.Height), CombineMode.Intersect);
            int inView = visible.Count(v => v.Kind == Kind.Tile && v.Bounds.Bottom >= gridRect.Y - TileH && v.Bounds.Y <= gridRect.Bottom + TileH);
            tileCacheBudget = Math.Min(TileCacheMaximum, Math.Max(TileCacheLimit, inView * 2 + 8));
            int count = 0, drawn = 0;
            foreach (var w in visible)
            {
                if (w.Kind != Kind.Tile) continue;
                count++;
                if (w.Bounds.Bottom < gridRect.Y - 12 || w.Bounds.Y > gridRect.Bottom + 12 || !clip.IntersectsWith(Theme.Inflate(w.Bounds, 18, 18))) continue;
                // Staggered zoom-in, like icons appearing on the Wii U Menu.
                float appear = (t - introStart - drawn++ * 0.045f) / 0.42f;
                if (appear <= 0) continue;
                if (appear >= 1) { DrawTile(g, w, t); continue; }
                var tileState = g.Save();
                var c = Theme.Center(w.Bounds);
                float k = 0.55f + 0.45f * Theme.EaseBack(appear);
                g.TranslateTransform(c.X, c.Y + 18 * (1 - Theme.EaseOut(appear))); g.ScaleTransform(k, k); g.TranslateTransform(-c.X, -c.Y);
                DrawTile(g, w, t);
                g.Restore(tileState);
            }
            if (count == 0)
            {
                var c = Theme.Center(gridRect);
                bool none = filter == 1;
                var trophy = Theme.Square(new PointF(c.X, c.Y - 30), 64);
                if (none) using (var b = new SolidBrush(Color.FromArgb(40, 20, 44, 64))) g.FillEllipse(b, Theme.Inflate(trophy, 10, 10));
                Theme.Trophy(g, trophy);
                Theme.Text(g, none ? "Nessun obiettivo sbloccato, per ora" : "Nessun obiettivo con questi filtri", Theme.Font(18, Weight.Heavy), Theme.Ink, new RectangleF(gridRect.X, c.Y + 12, gridRect.Width, 28), StringAlignment.Center, StringAlignment.Center, false);
                Theme.Text(g, "Cambia categoria o filtro per vedere gli altri obiettivi.", Theme.Font(14, Weight.Regular), Theme.Muted, new RectangleF(gridRect.X, c.Y + 40, gridRect.Width, 22), StringAlignment.Center, StringAlignment.Center, false);
            }
            float top = gridRect.Y / H, bottom = gridRect.Bottom / H;
            Color fadeTop = Theme.Mix(Style.BackTop, Style.BackBottom, top), fadeBottom = Theme.Mix(Style.BackTop, Style.BackBottom, bottom);
            if (scroll > 1)
                using (var b = new LinearGradientBrush(new RectangleF(0, gridRect.Y - 1, W, 14), fadeTop, Color.FromArgb(0, fadeTop), 90f)) g.FillRectangle(b, 0, gridRect.Y, W, 12);
            if (contentHeight - scroll > gridRect.Height + 1)
                using (var b = new LinearGradientBrush(new RectangleF(0, gridRect.Bottom - 13, W, 14), Color.FromArgb(0, fadeBottom), fadeBottom, 90f)) g.FillRectangle(b, 0, gridRect.Bottom - 12, W, 12);
            g.Restore(state);
            if (contentHeight > gridRect.Height + 1)
            {
                var track = new RectangleF(W - 9, gridRect.Y + 8, 5, gridRect.Height - 16);
                float thumbH = Math.Max(36, track.Height * gridRect.Height / contentHeight);
                float thumbY = track.Y + (track.Height - thumbH) * (scroll / Math.Max(1, contentHeight - gridRect.Height));
                Theme.Fill(g, track, 2.5f, Color.FromArgb(22, 20, 44, 64), Color.FromArgb(22, 20, 44, 64));
                Theme.Fill(g, new RectangleF(track.X, thumbY, track.Width, thumbH), 2.5f, Color.FromArgb(150, Style.Accent), Color.FromArgb(170, Style.AccentDark));
            }
        }
        void DrawTileVector(Graphics g, Widget w, float t, bool drawFooter)
        {
            var a = w.Achievement;
            var art = arts[a.Id];
            var unlock = progress == null || a.IsPlanned ? null : progress.Get(a.Id);
            bool done = unlock != null;
            float party = Celebration(a.Id, t);
            float cursor = Math.Max(w.Hover, FocusStrength(w, t));
            var style = Style;
            var r = w.Bounds;
            r.Offset(0, -2.5f * w.Hover + 1.5f * w.Press);
            Theme.Shadow(g, r, 16, 7 + 5 * cursor, 54 + (int)(34 * cursor), 3 + 2 * cursor);
            Theme.Fill(g, r, 16, done ? style.CardTop : style.CardLockedTop, done ? Theme.Mix(style.CardBottom, style.Soft, style.Themed ? 0.15f : 0.4f) : style.CardLockedBottom);
            style.DecorateCard(g, r, 16, false);
            Color tier = Theme.TierColor(art.Tier), glow = Theme.TierGlow(art.Tier);
            using (var path = Theme.Round(r, 16))
            {
                var state = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                if (done)
                {
                    // Rarity wash with the game's motif, like a themed header on the Wii U Menu.
                    using (var b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 1, r.Width, 56), Color.FromArgb(78, tier), Color.FromArgb(0, tier), 90f)) g.FillRectangle(b, r.X, r.Y, r.Width, 54);
                    style.Pattern(g, new RectangleF(r.X + 96, r.Y, r.Width - 96, 30), 0, Color.FromArgb(40, 255, 255, 255));
                }
                if (party >= 0)
                {
                    float u = Theme.EaseInOut(Theme.Clamp01((party - 0.2f) / 0.55f)), sx = r.X - 90 + (r.Width + 180) * u;
                    using (var band = new GraphicsPath())
                    {
                        band.AddPolygon(new[] { new PointF(sx, r.Y), new PointF(sx + 60, r.Y), new PointF(sx + 20, r.Bottom), new PointF(sx - 40, r.Bottom) });
                        Theme.Fill(g, band, Color.FromArgb((int)(130 * (1 - party)), 255, 255, 255));
                    }
                }
                g.Restore(state);
            }
            Theme.Stroke(g, r, 16, done ? Theme.Mix(tier, Color.White, 0.3f) : style.CardEdge, done ? 1.5f : style.Themed ? 1.2f : 1);
            const float ms = 74;
            var mc = new PointF(r.X + 14 + ms / 2, r.Y + 46);
            float pop = 1 + 0.05f * cursor, flip = 1;
            bool grey = !done;
            if (party >= 0)
            {
                // Coin turn from the grey medal to the coloured one, then a soft pop and a burst.
                float turn = Theme.Clamp01(party / 0.3f);
                flip = Math.Abs((float)Math.Cos(Math.PI * turn));
                grey = turn < 0.5f;
                pop = 1 + 0.2f * (float)Math.Sin(Math.PI * Theme.Clamp01((party - 0.3f) / 0.35f));
                if (!grey)
                {
                    Theme.Rays(g, mc, 66, party * 160, Color.FromArgb((int)(180 * (1 - party)), glow));
                    float ring = Theme.Clamp01((party - 0.15f) / 0.5f);
                    if (ring < 1)
                        using (var pen = new Pen(Color.FromArgb((int)(210 * (1 - ring)), glow), 0.5f + 3 * (1 - ring)))
                            g.DrawEllipse(pen, Theme.Square(mc, ms * (0.95f + 0.75f * Theme.EaseOut(ring))));
                }
            }
            var medal = Theme.Square(mc, ms * pop);
            var turnState = g.Save();
            if (flip < 0.999f) { g.TranslateTransform(mc.X, mc.Y); g.ScaleTransform(Math.Max(0.03f, flip), 1); g.TranslateTransform(-mc.X, -mc.Y); }
            Theme.Medal(g, medal, art, grey ? 0.88f : 0);
            g.Restore(turnState);
            if (party < 0 || party > 0.3f) Theme.StatusBadge(g, Theme.Square(mc, ms), done);
            Theme.TierRibbon(g, new RectangleF(mc.X - 31, r.Y + 87, 62, 15), art.Tier, Art.TierName(art.Tier).ToUpperInvariant(), done ? 1 : 0.8f);
            float tx = r.X + 102, tw = r.Right - 16 - tx;
            string pts = a.IsPlanned ? "?" : "+" + a.Points + (a.Experimental ? "*" : "");
            var pf = Theme.Font(13, Weight.Heavy);
            float pw = Measure(pts, pf).Width + 18;
            var pill = new RectangleF(r.Right - 14 - pw, r.Y + 14, pw, 22);
            Theme.Fill(g, pill, 11, done ? Theme.Light(tier, 0.12f) : Color.FromArgb(206, 212, 218), done ? Theme.Dark(tier, 0.18f) : Color.FromArgb(188, 196, 203));
            Theme.Text(g, pts, pf, Color.White, pill, StringAlignment.Center, StringAlignment.Center, false);
            Theme.Text(g, a.Title, Theme.Font(16.5f, Weight.Heavy), done ? Theme.Ink : Theme.Mix(Theme.Ink, Theme.Muted, 0.3f), new RectangleF(tx, r.Y + 13, pill.X - 8 - tx, 24));
            Theme.Text(g, a.Description, Theme.Font(13, Weight.Regular), style.Muted, new RectangleF(tx, r.Y + 37, tw, 40), StringAlignment.Near, StringAlignment.Near, true);
            if (drawFooter) DrawProgress(g, a, unlock, new RectangleF(tx, r.Y + 79, tw, 18), Theme.Font(12.5f, Weight.Bold), false);
            Theme.Cursor(g, r, 16, cursor);
            // Earned gold and platinum cards wear the gold corners.
            if (done && art.Tier >= Tier.Gold && style.HasUiArt) style.Corners(g, r, 24, 4);
            if (party >= 0)
            {
                Theme.Stroke(g, Theme.Inflate(r, 1, 1), 17, Color.FromArgb((int)(230 * (1 - party)), glow), 2.5f);
                float burst = Theme.Clamp01((party - 0.28f) / 0.72f);
                if (burst > 0)
                    for (int i = 0; i < 10; i++)
                    {
                        double ang = i * Math.PI * 2 / 10 + 0.4;
                        float d = 30 + 46 * Theme.EaseOut(burst);
                        var p = new PointF(mc.X + (float)Math.Cos(ang) * d, mc.Y + (float)Math.Sin(ang) * d * 0.75f);
                        Theme.Sparkle(g, p, 5.5f * (1 - burst) + 1, Color.FromArgb((int)(255 * (1 - burst)), i % 2 == 0 ? glow : Color.White));
                    }
            }
        }
        void DrawProgress(Graphics g, Achievement a, Unlock unlock, RectangleF r, Font font, bool centered)
        {
            if (a.IsPlanned)
            {
                Theme.Text(g, centered ? "Non attivo · rilevamento da verificare" : "Non attivo · " + a.Category, font, Theme.Warning, r,
                    centered ? StringAlignment.Center : StringAlignment.Near, StringAlignment.Center, false);
                return;
            }
            if (unlock != null)
            {
                string text = "Registrato il " + unlock.Utc.ToLocalTime().ToString("d MMMM 'alle' HH:mm", Italian);
                float x = centered ? r.X + (r.Width - Measure(text, font).Width - 22) / 2 : r.X;
                var check = new RectangleF(x, r.Y + (r.Height - 16) / 2, 16, 16);
                using (var b = new SolidBrush(Theme.Success)) g.FillEllipse(b, check);
                Theme.Check(g, Theme.Inflate(check, -2, -2), Color.White, 2);
                Theme.Text(g, text, font, Theme.Success, new RectangleF(x + 22, r.Y, r.Right - x - 22, r.Height), StringAlignment.Near, StringAlignment.Center, false);
                return;
            }
            if (current == null)
            {
                string text = a.Experimental ? "Sperimentale · da ottenere" : paused ? "Registrazione in pausa" : demo ? "In attesa di dati simulati" : centered ? "Da sbloccare · collega Cemu per vedere i progressi" : "Da sbloccare";
                float x = centered ? r.X + (r.Width - Measure(text, font).Width - 20) / 2 : r.X;
                Theme.Lock(g, new RectangleF(x, r.Y + (r.Height - 15) / 2, 14, 15), Theme.Faint);
                Theme.Text(g, text, font, Theme.Faint, new RectangleF(x + 20, r.Y, r.Right - x - 20, r.Height), StringAlignment.Near, StringAlignment.Center, false);
                return;
            }
            if (!current.HasValue(a.Field))
            {
                Theme.Text(g, "Dato non disponibile", font, Theme.Warning, r,
                    centered ? StringAlignment.Center : StringAlignment.Near, StringAlignment.Center, false);
                return;
            }
            int value = current.Value(a.Field);
            var evaluation = engine == null ? null : engine.Status(a.Id);
            if (evaluation != null && evaluation.Phase == AchievementPhase.AlreadySatisfied)
            {
                // The engine never awards progress that was already present when monitoring started.
                string text = a.Field == "rupees" || a.Field == "max_health" ? "Soglia già raggiunta all'avvio" : "Già presente all'avvio";
                float x = centered ? r.X + (r.Width - Measure(text, font).Width) / 2 : r.X;
                Theme.Text(g, text, font, Theme.Warning, new RectangleF(x, r.Y, r.Right - x, r.Height), StringAlignment.Near, StringAlignment.Center, false);
                return;
            }
            bool hearts = a.Field == "max_health";
            string label = evaluation != null && evaluation.Phase == AchievementPhase.Stabilizing ? "Sincronizzazione…" :
                evaluation != null && evaluation.Phase == AchievementPhase.Confirming ? "Verifica " + evaluation.Confirmations + "/" + AchievementEngine.RequiredSamples :
                a.Threshold == 1 && a.Field != "rupees" ? (a.Experimental ? "Test · da ottenere" : "Da ottenere") : hearts ? Hearts(value) + " / " + (a.Threshold / 4) + " cuori" : value + " / " + a.Threshold;
            float lw = Measure(label, font).Width + 6;
            var bar = new RectangleF(r.X, r.Y + r.Height / 2 - 4, r.Width - lw - 10, 8);
            if (bar.Width < 8) { Theme.Text(g, label, font, Theme.Ink, r); return; }
            Theme.Fill(g, bar, 4, Color.FromArgb(214, 223, 230), Color.FromArgb(228, 234, 239));
            float ratio = Theme.Clamp01(value / (float)a.Threshold);
            if (ratio > 0) Theme.Fill(g, new RectangleF(bar.X, bar.Y, Math.Max(8, bar.Width * ratio), bar.Height), 4, Style.Glow, Style.Accent);
            Theme.Text(g, label, font, Theme.Ink, new RectangleF(bar.Right + 10, r.Y, lw, r.Height), StringAlignment.Near, StringAlignment.Center, false);
        }

        void DrawBottomBar(Graphics g, float t, bool contents = true)
        {
            float w = W, y = H - BottomH;
            using (var b = new LinearGradientBrush(new RectangleF(0, y - 10, w, 11), Color.FromArgb(0, 20, 44, 64), Color.FromArgb(22, 20, 44, 64), 90f)) g.FillRectangle(b, 0, y - 9, w, 9);
            var style = Style;
            Layer(g, "bottom-bar", new RectangleF(0, y - 12, w, BottomH + 12), style.GameId, b =>
            {
                using (var brush = new LinearGradientBrush(new RectangleF(0, y - 1, w, BottomH + 2), style.FootTop, style.FootBottom, 90f)) b.FillRectangle(brush, 0, y, w, BottomH);
                using (var pen = new Pen(style.FootLine)) b.DrawLine(pen, 0, y + 0.5f, w, y + 0.5f);
                using (var pen = new Pen(Color.White)) b.DrawLine(pen, 0, y + 1.5f, w, y + 1.5f);
                style.DecorateFooter(b, new RectangleF(0, y, w, BottomH), 0);
            });
            if (!contents) return;
            if (libraryOpen)
            {
                DrawButton(g, widgets["library-back"], t);
                Theme.Text(g, "Gioco selezionato: " + selectedGame.Title, Theme.Font(13, Weight.Bold), Theme.Muted,
                    new RectangleF(widgets["library-back"].Bounds.Right + 24, y + 12, w - widgets["library-back"].Bounds.Right - 48, 42), StringAlignment.Near, StringAlignment.Center, true);
                return;
            }
            string last = CanDisconnect ? "pause" : "demo";
            foreach (var id in new[] { "connect", "disconnect", last }) DrawButton(g, widgets[id], t);
            float hx = widgets[last].Bounds.Right + 26;
            if (w - Pad - hx > 120) Theme.Text(g, hint, Theme.Font(13, Weight.Regular), style.Muted, new RectangleF(hx, y + 10, w - Pad - hx, BottomH - 18), StringAlignment.Near, StringAlignment.Center, true);
        }
        void DrawButton(Graphics g, Widget w, float t)
        {
            bool primary = w.Kind == Kind.Primary;
            float hv = w.Enabled ? w.Hover : 0;
            var r = w.Bounds;
            if (w.Press > 0) r.Offset(0, 1.5f * w.Press);
            float radius = r.Height / 2;
            if (w.Enabled) Theme.Shadow(g, r, radius, 5 + 3 * hv, (int)(46 + 30 * hv), 2.5f - w.Press);
            Color top, bottom, edge, ink, glyph, glyphInk;
            var style = Style;
            if (primary)
            {
                top = Theme.Mix(style.PrimaryTop, Theme.Light(style.PrimaryTop, 0.3f), hv); bottom = Theme.Mix(style.PrimaryBottom, Theme.Dark(style.PrimaryBottom, 0.15f), w.Press);
                edge = Theme.Mix(style.PrimaryEdge, style.Themed ? style.Trim : style.PrimaryEdge, hv); ink = style.PrimaryInk; glyph = style.PrimaryGlyph; glyphInk = style.PrimaryGlyphInk;
            }
            else
            {
                top = style.ButtonTop; bottom = Theme.Mix(style.ButtonBottom, Theme.Dark(style.ButtonBottom, 0.06f), w.Press);
                edge = Theme.Mix(style.ButtonEdge, style.Themed ? style.Trim : Theme.Accent, hv); ink = style.ButtonInk; glyph = style.GlyphFill; glyphInk = style.GlyphInk;
            }
            if (!w.Enabled && primary)
            {
                top = Color.FromArgb(178, 212, 228); bottom = Color.FromArgb(152, 192, 212); edge = Color.FromArgb(140, 180, 200);
                ink = Color.FromArgb(240, 255, 255, 255); glyph = Color.FromArgb(220, 255, 255, 255); glyphInk = Color.FromArgb(140, 180, 200);
            }
            else if (!w.Enabled)
            {
                top = Color.FromArgb(244, 246, 248); bottom = Color.FromArgb(230, 234, 238); edge = Color.FromArgb(208, 214, 219);
                ink = Color.FromArgb(160, 168, 175); glyph = Color.FromArgb(196, 203, 209); glyphInk = Color.White;
            }
            Theme.Fill(g, r, radius, top, bottom);
            using (var gloss = Theme.Round(new RectangleF(r.X + 3, r.Y + 2, r.Width - 6, r.Height * 0.46f), radius - 3))
                Theme.Fill(g, gloss, Color.FromArgb(primary ? 56 : 110, 255, 255, 255));
            Theme.Stroke(g, r, radius, edge, style.Themed ? 1.6f + 0.4f * hv : primary || hv > 0.05f ? 1.4f : 1.1f);
            var font = Theme.Font(15, Weight.Bold);
            float textW = Measure(w.Name, font).Width + 2, content = textW + (w.Glyph != null ? 30 : 0), x = r.X + (r.Width - content) / 2;
            if (w.Glyph != null)
            {
                Theme.Glyph(g, new RectangleF(x, r.Y + (r.Height - 24) / 2, 24, 24), w.Glyph, glyph, glyphInk);
                x += 30;
            }
            Theme.Text(g, w.Name, font, ink, new RectangleF(x, r.Y, textW + 4, r.Height), StringAlignment.Near, StringAlignment.Center, false);
            Theme.Cursor(g, r, radius, FocusStrength(w, t));
        }

        void DrawDrawer(Graphics g, float t)
        {
            var r = drawerRect;
            Theme.Shadow(g, r, 18, 14, 90, -2);
            using (var path = Theme.TopRound(r, 18)) Theme.Fill(g, path, Style.Drawer);
            if (Style.Themed) Style.WaveBandCached(g, new RectangleF(r.X + 18, r.Y - 7, r.Width - 36, 9), 0, 1);
            using (var path = Theme.TopRound(new RectangleF(r.X, r.Y, r.Width, 62), 18))
            using (var b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 1, r.Width, 64), Color.FromArgb(60, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f)) g.FillPath(b, path);
            Theme.Text(g, "Registro attività", Theme.Font(17, Weight.Heavy), Color.White, new RectangleF(r.X + 22, r.Y + 14, 320, 26));
            Theme.Text(g, logLines.Count == 1 ? "1 voce" : logLines.Count + " voci", Theme.Font(12.5f, Weight.Bold), Color.FromArgb(150, 172, 188), new RectangleF(r.X + 22, r.Y + 39, 320, 18));
            Widget close;
            if (widgets.TryGetValue("log-close", out close) && close.Visible)
                DrawIconButton(g, close, t, (gg, b, c) =>
                {
                    using (var pen = new Pen(c, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        gg.DrawLine(pen, b.X + 3, b.Y + 3, b.Right - 3, b.Bottom - 3);
                        gg.DrawLine(pen, b.Right - 3, b.Y + 3, b.X + 3, b.Bottom - 3);
                    }
                });
            var area = new RectangleF(r.X + 22, r.Y + 68, r.Width - 44, DrawerH - 80);
            const float lineH = 20;
            int fit = Math.Max(1, (int)(area.Height / lineH));
            logScroll = Math.Max(0, Math.Min(Math.Max(0, logLines.Count - fit), logScroll));
            if (logLines.Count == 0) Theme.Text(g, "Nessuna attività registrata in questa sessione.", Theme.Font(13.5f, Weight.Regular), Color.FromArgb(150, 172, 188), area);
            var mono = Theme.Mono(13);
            int last = logLines.Count - 1 - (int)logScroll;
            for (int k = 0; k < fit && last - k >= 0; k++)
            {
                string line = logLines[last - k];
                float y = area.Bottom - (k + 1) * lineH;
                int split = Math.Min(line.Length, 8);
                Theme.Text(g, line.Substring(0, split), mono, Color.FromArgb(110, 214, 255), new RectangleF(area.X, y, 80, lineH));
                Theme.Text(g, line.Substring(split).Trim(), mono, Color.FromArgb(218, 228, 236), new RectangleF(area.X + 78, y, area.Width - 78, lineH));
            }
        }

        void DrawSheet(Graphics g, float t)
        {
            float e = Theme.EaseOut(sheet);
            using (var b = new SolidBrush(Color.FromArgb((int)(90 * e), 14, 28, 42))) g.FillRectangle(b, 0, 0, W, H);
            var r = sheetRect;
            using (var b = new LinearGradientBrush(new RectangleF(r.X - 19, 0, 20, H), Color.FromArgb(0, 10, 20, 30), Color.FromArgb(60, 10, 20, 30), 0f)) g.FillRectangle(b, r.X - 18, 0, 18, H);
            var style = Style;
            using (var b = new LinearGradientBrush(new RectangleF(r.X, -1, r.Width, H + 2), style.SheetTop, style.SheetBottom, 90f)) g.FillRectangle(b, r);
            using (var b = new LinearGradientBrush(new RectangleF(r.X, -1, r.Width, 66), style.BarTop, style.BarBottom, 90f)) g.FillRectangle(b, r.X, 0, r.Width, 64);
            using (var pen = new Pen(style.BarLine)) g.DrawLine(pen, r.X, 63.5f, r.Right, 63.5f);
            if (style.Themed)
            {
                style.Parchment(g, r, 0, 0.5f);
                style.TrimLine(g, r.X, r.Right, 63, 2.2f);
                using (var pen = new Pen(Color.FromArgb(200, 46, 32, 26), 4.4f)) g.DrawLine(pen, r.X + 1.5f, 0, r.X + 1.5f, H);
                using (var b = new LinearGradientBrush(new RectangleF(r.X - 2, 0, 6, H), Theme.Light(style.Trim, 0.35f), style.TrimDark, 0f))
                using (var pen = new Pen(b, 2.4f)) g.DrawLine(pen, r.X + 1.5f, 0, r.X + 1.5f, H);
                var state = g.Save();
                g.SetClip(r, CombineMode.Intersect);
                style.WaveBandCached(g, new RectangleF(r.X, H - 13, r.Width, 15), 0.6f, 0.5f);
                g.Restore(state);
                style.Crest(g, new PointF(r.X + r.Width / 2, 63), 200, 1);
            }
            float sx = r.X + 24, cw = SheetW - 48;
            Theme.Gear(g, new RectangleF(sx, 20, 24, 24), style.Accent);
            Theme.Text(g, "Impostazioni", Theme.Font(22, Weight.Heavy), Theme.Ink, new RectangleF(sx + 34, 0, 220, 64), StringAlignment.Near, StringAlignment.Center, false);
            DrawButton(g, widgets["back"], t);

            if (cloudPage)
            {
                Section(g, "ACCOUNT CLOUD · OPZIONALE", sx, 84);
                string[] ids = { "field-project", "field-apikey", "field-email", "field-pass" };
                string[] labels = { "Project ID Firebase", "Web API key", "Email", "Password" };
                for (int i = 0; i < ids.Length; i++)
                {
                    Theme.Text(g, labels[i], Theme.Font(14, Weight.Bold), Theme.Ink, new RectangleF(sx, 108 + i * 78, cw, 22));
                    DrawField(g, widgets[ids[i]], t);
                }
                DrawButton(g, widgets["cloud-signup"], t); DrawButton(g, widgets["cloud-login"], t); DrawButton(g, widgets["cloud-logout"], t);
                Theme.Text(g, cloudMessage, Theme.Font(13, Weight.Bold), Theme.Muted, new RectangleF(sx, 508, cw, 60), StringAlignment.Near, StringAlignment.Near, true);
                Theme.Text(g, cloud != null && cloud.IsSignedIn ? "Account connesso. I progressi si sincronizzano automaticamente. Per usare un altro account, premi Esci." : "Accedi o registrati per sincronizzare i progressi. I progressi restano locali anche senza rete.", Theme.Font(12, Weight.Regular), Theme.Muted, new RectangleF(sx, 578, cw, 80), StringAlignment.Near, StringAlignment.Near, true);
                return;
            }
            Section(g, "COLLEGAMENTO", sx, 84);
            Theme.Text(g, "Percorso di Cemu.exe", Theme.Font(14, Weight.Bold), Theme.Ink, new RectangleF(sx, 108, cw, 22));
            DrawField(g, widgets["field-cemu"], t);
            DrawButton(g, widgets["browse"], t);
            Theme.Text(g, "Profilo locale", Theme.Font(14, Weight.Bold), Theme.Ink, new RectangleF(sx, 186, cw, 22));
            DrawField(g, widgets["field-player"], t);
            Theme.Text(g, FieldsEditable ? "Ogni profilo ha progressi separati: usane uno nuovo per una nuova partita." : "Scollega Cemu o la demo per modificare percorso e profilo.",
                Theme.Font(13, Weight.Regular), FieldsEditable ? Theme.Muted : Theme.Warning, new RectangleF(sx, 262, cw, 40), StringAlignment.Near, StringAlignment.Near, true);

            Section(g, "NOTIFICHE", sx, 326);
            DrawToggle(g, widgets["sound"], "Jingle originale sintetizzato", t);
            DrawToggle(g, widgets["ambient"], "Si fermano mentre giochi in Cemu", t);
            DrawButton(g, widgets["test"], t);
            DrawButton(g, widgets["cloud-page"], t);

            Section(g, "CONFIGURAZIONE SUPPORTATA", sx, 526);
            var info = new RectangleF(sx, 550, cw, 76);
            Theme.Fill(g, info, 12, style.CardTop, style.Themed ? style.CardBottom : Color.FromArgb(246, 249, 250));
            Theme.Stroke(g, info, 12, style.Themed ? style.CardEdge : Theme.Line, 1);
            var f = Theme.Font(13, Weight.Regular);
            Theme.Text(g, selectedGame.Title + " · " + selectedGame.Profiles[0].Label + " · Cemu " + selectedGame.Profiles[0].CemuVersion, Theme.Font(13.5f, Weight.Bold), Theme.Ink, new RectangleF(info.X + 14, info.Y + 10, info.Width - 28, 20));
            Theme.Text(g, "Title ID " + selectedGame.Profiles[0].TitleId, f, Theme.Muted, new RectangleF(info.X + 14, info.Y + 31, info.Width - 28, 18));
            Theme.Text(g, "RPX " + selectedGame.Profiles[0].RpxHash + " · sola lettura della memoria", f, Theme.Muted, new RectangleF(info.X + 14, info.Y + 50, info.Width - 28, 18));
            Theme.Text(g, "Icone fornite dal proprietario del progetto; grafica e suoni in stile Wii U generati dal codice. Font Nunito © The Nunito Project Authors, SIL OFL 1.1.",
                Theme.Font(11.5f, Weight.Regular), Theme.Faint, new RectangleF(sx, Math.Max(636, H - 62), cw, 40), StringAlignment.Near, StringAlignment.Near, true);
        }
        static void Section(Graphics g, string text, float x, float y)
        {
            Theme.Text(g, text, Theme.Font(12, Weight.Heavy), Theme.Accent, new RectangleF(x, y, 360, 18));
        }
        static string FieldDisplayText(TextBox box) { return box.UseSystemPasswordChar ? new string('●', box.TextLength) : box.Text; }
        void DrawField(Graphics g, Widget w, float t)
        {
            var r = w.Bounds;
            bool active = w.Box.Focused;
            Theme.Fill(g, r, 10, w.Enabled ? Color.White : Color.FromArgb(240, 243, 245), w.Enabled ? Color.White : Color.FromArgb(240, 243, 245));
            Theme.Stroke(g, r, 10, active ? Theme.Accent : Theme.Mix(Color.FromArgb(192, 201, 209), Theme.Accent, w.Hover * 0.7f), active ? 2 : 1.2f);
            if (active) Theme.Cursor(g, r, 10, 0.55f);
            if (!w.Box.Visible) Theme.Text(g, FieldDisplayText(w.Box), Theme.Font(14, Weight.Regular), w.Enabled ? Theme.Ink : Theme.Muted, Theme.Inflate(r, -13, 0), StringAlignment.Near, StringAlignment.Center, false);
            Theme.Cursor(g, r, 10, FocusStrength(w, t));
        }
        void DrawToggle(Graphics g, Widget w, string help, float t)
        {
            var r = w.Bounds;
            Theme.Fill(g, r, 12, Theme.Mix(Style.CardTop, Style.Soft, w.Hover * 0.6f), Theme.Mix(Style.Themed ? Style.CardBottom : Color.FromArgb(246, 249, 250), Style.Soft, w.Hover * 0.6f));
            Theme.Stroke(g, r, 12, Style.Themed ? Style.CardEdge : Theme.Line, 1);
            Theme.Text(g, w.Name, Theme.Font(14.5f, Weight.Bold), Theme.Ink, new RectangleF(r.X + 14, r.Y + 5, r.Width - 90, 20));
            Theme.Text(g, help, Theme.Font(12, Weight.Regular), Theme.Muted, new RectangleF(r.X + 14, r.Y + 24, r.Width - 90, 18));
            var track = new RectangleF(r.Right - 66, r.Y + (r.Height - 28) / 2, 52, 28);
            float on = w.Value < 0 ? (w.Checked ? 1 : 0) : w.Value;
            Theme.Fill(g, track, 14, Theme.Mix(Style.Themed ? Style.BubbleOffTop : Color.FromArgb(200, 208, 214), Style.Glow, on), Theme.Mix(Style.Themed ? Style.BubbleOffBottom : Color.FromArgb(184, 193, 200), Style.AccentDark, on));
            var knob = Theme.Square(new PointF(track.X + 14 + 24 * on, track.Y + 14), 22);
            using (var b = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) g.FillEllipse(b, knob.X, knob.Y + 1.5f, knob.Width, knob.Height);
            using (var b = new LinearGradientBrush(Theme.Inflate(knob, 0, 1), Color.White, Color.FromArgb(232, 237, 241), 90f)) g.FillEllipse(b, knob);
            Theme.Cursor(g, r, 12, FocusStrength(w, t));
        }

        void DrawDialog(Graphics g, float t)
        {
            float e = Theme.EaseOut(dialog);
            using (var b = new SolidBrush(Color.FromArgb((int)(120 * e), 14, 28, 42))) g.FillRectangle(b, 0, 0, W, H);
            var r = dialogRect;
            var c = Theme.Center(r);
            var a = detail;
            var art = arts[a.Id];
            var unlock = progress == null || a.IsPlanned ? null : progress.Get(a.Id);
            bool done = unlock != null;
            var state = g.Save();
            float s = 0.86f + 0.14f * Theme.EaseBack(dialog);
            g.TranslateTransform(c.X, c.Y); g.ScaleTransform(s, s); g.TranslateTransform(-c.X, -c.Y);
            Theme.Shadow(g, r, 26, 24, 130, 10);
            Theme.Fill(g, r, 26, Style.CardTop, Style.Themed ? Style.CardBottom : Color.FromArgb(236, 242, 246));
            Color tier = Theme.TierColor(art.Tier), glow = Theme.TierGlow(art.Tier);
            using (var path = Theme.Round(r, 26))
            {
                var inner = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                using (var b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 1, r.Width, 182), Color.FromArgb(done ? 110 : 50, done ? tier : Theme.Faint), Color.FromArgb(0, tier), 90f))
                    g.FillRectangle(b, r.X, r.Y, r.Width, 180);
                if (done) Style.Pattern(g, new RectangleF(r.X, r.Y + 4, r.Width, 60), t * 0.6f, Color.FromArgb(46, 255, 255, 255));
                g.Restore(inner);
            }
            Theme.Stroke(g, r, 26, done ? Theme.Mix(tier, Color.White, 0.35f) : Color.FromArgb(214, 222, 228), done ? 1.5f : 1);
            Style.Parchment(g, r, 26, 0.55f);
            Style.DecoratePanel(g, r, 26, true);
            // The medal arrives just after the card and floats gently once earned.
            float enter = Theme.EaseBack(Theme.Clamp01((dialog - 0.2f) / 0.8f));
            var mc = new PointF(c.X, r.Y + 100 + (done ? (float)Math.Sin(t * 1.7f) * 2.5f : 0));
            float size = 140 * (0.55f + 0.45f * enter);
            if (done) Theme.Rays(g, mc, 128, t * 14, Color.FromArgb((int)(150 * enter), glow));
            Theme.Medal(g, Theme.Square(mc, size), art, done ? 0 : 0.88f);
            Theme.StatusBadge(g, Theme.Square(mc, size), done);
            Theme.Text(g, a.Title, Theme.Font(26, Weight.Heavy), Theme.Ink, new RectangleF(r.X + 24, r.Y + 176, r.Width - 48, 38), StringAlignment.Center, StringAlignment.Center, false);
            string rank = Art.TierName(art.Tier).ToUpperInvariant();
            string pill = a.IsPlanned ? "Non attivo · Punti proposti: " + a.Points :
                "+" + a.Points + (a.Points == 1 ? " punto" : " punti") + (a.Experimental ? " · Sperimentale" : "");
            var pf = Theme.Font(13.5f, Weight.Heavy);
            float rw = Measure(rank, Theme.Font(15, Weight.Heavy)).Width + 30, pw = Measure(pill, pf).Width + 26, row = c.X - (rw + 8 + pw) / 2;
            Theme.TierRibbon(g, new RectangleF(row, r.Y + 221, rw, 26), art.Tier, rank, 1);
            var pr = new RectangleF(row + rw + 8, r.Y + 220, pw, 28);
            Theme.Fill(g, pr, 14, done ? Theme.Light(tier, 0.12f) : Color.FromArgb(200, 208, 214), done ? Theme.Dark(tier, 0.2f) : Color.FromArgb(182, 191, 198));
            Theme.Text(g, pill, pf, Color.White, pr, StringAlignment.Center, StringAlignment.Center, false);
            Theme.Text(g, a.Description, Theme.Font(15.5f, Weight.Regular), Style.Muted, new RectangleF(r.X + 48, r.Y + 264, r.Width - 96, 48), StringAlignment.Center, StringAlignment.Near, true);
            DrawProgress(g, a, unlock, new RectangleF(r.X + 70, r.Y + 322, r.Width - 140, 26), Theme.Font(14.5f, Weight.Bold), true);
            if (a.IsPlanned) Theme.Text(g, a.Requirement, Theme.Font(11, Weight.Regular), Theme.Muted,
                new RectangleF(r.X + 30, r.Y + 350, r.Width - 60, 30), StringAlignment.Center, StringAlignment.Near, true);
            else Theme.Text(g, a.Chapter + (a.Experimental ? " · Implementato, da verificare durante la partita" : ""), Theme.Font(11, Weight.Regular), Theme.Muted,
                new RectangleF(r.X + 30, r.Y + 350, r.Width - 60, 30), StringAlignment.Center, StringAlignment.Near, true);
            DrawButton(g, widgets["close"], t);
            g.Restore(state);
        }

        // ---------------------------------------------------------------- input
        PointF Logical(Point p) { return new PointF(p.X / scale, p.Y / scale); }
        Widget HitTest(Point device)
        {
            Arrange();
            var p = Logical(device);
            int layer = ActiveLayer;
            bool overDrawer = drawer > 0 && drawerRect.Contains(p) && p.Y < H - BottomH;
            for (int i = visible.Count - 1; i >= 0; i--)
            {
                var w = visible[i];
                if (w.Layer != layer || !w.Bounds.Contains(p)) continue;
                if (overDrawer && w.Id != "log-close") continue;
                if (w.Kind == Kind.Tile && !gridRect.Contains(p)) continue;
                if (w.Kind == Kind.GameTile && !libraryGridRect.Contains(p)) continue;
                return w;
            }
            return null;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var w = HitTest(e.Location);
            if (w == hovered) return;
            hovered = w;
            Cursor = w != null && w.Enabled ? Cursors.Hand : Cursors.Default;
            Kick();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovered = null; Kick();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            focusVisible = false;
            pressed = HitTest(e.Location);
            var p = Logical(e.Location);
            if ((pressed == null || pressed.Kind != Kind.Field) && ActiveControl is TextBox) { ActiveControl = null; Focus(); }
            if (pressed == null)
            {
                if (dialogTarget > 0) { if (!dialogRect.Contains(p)) CloseDialog(); }
                else if (sheetTarget > 0 && !sheetRect.Contains(p)) CloseSheet();
            }
            Kick();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            var w = HitTest(e.Location);
            var down = pressed;
            pressed = null;
            if (w != null && w == down) Press(w);
            Kick();
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var p = Logical(e.Location);
            if (drawer > 0 && drawerRect.Contains(p) && p.Y < H - BottomH) logScroll += e.Delta / 120f * 3;
            else if (ActiveLayer == 0 && libraryOpen) libraryScroll -= e.Delta / 120f * 96;
            else if (ActiveLayer == 0) scrollTarget -= e.Delta / 120f * 96;
            if (ActiveLayer == 0 && !libraryOpen && !(drawer > 0 && drawerRect.Contains(p)))
                Invalidate(Device(new RectangleF(0, gridRect.Y, W, gridRect.Height)));
            else Invalidate();
            Kick();
        }
        void Press(Widget w)
        {
            if (w == null || !w.Enabled || w.Click == null) return;
            w.Click();
            Invalidate(); Kick();
        }
        List<Widget> Focusables()
        {
            Arrange();
            int layer = ActiveLayer;
            return visible.Where(w => w.Layer == layer && w.Enabled && (w.Kind != Kind.Tile || !(drawer > 0 && drawerRect.IntersectsWith(w.Bounds)))).ToList();
        }
        void SetFocus(Widget w)
        {
            focused = w; focusVisible = true;
            if (w.Kind == Kind.Field && w.Box != null && w.Box.Visible) w.Box.Focus();
            else if (ActiveControl != null) { ActiveControl = null; Focus(); }
            if (w.Kind == Kind.GameTile)
            {
                if (w.Bounds.Top < libraryGridRect.Top + 8) libraryScroll -= libraryGridRect.Top + 8 - w.Bounds.Top;
                else if (w.Bounds.Bottom > libraryGridRect.Bottom - 8) libraryScroll += w.Bounds.Bottom - libraryGridRect.Bottom + 8;
            }
            if (w.Kind == Kind.Tile)
            {
                var b = w.Bounds;
                if (b.Y < gridRect.Y + 8) scrollTarget -= gridRect.Y + 12 - b.Y;
                else if (b.Bottom > gridRect.Bottom - 8) scrollTarget += b.Bottom - gridRect.Bottom + 14;
            }
            Invalidate(); Kick();
        }
        void MoveFocus(int direction)
        {
            var list = Focusables();
            if (list.Count == 0) return;
            int index = focused == null ? -1 : list.IndexOf(focused);
            if (index < 0) index = direction > 0 ? -1 : 0;
            SetFocus(list[((index + direction) % list.Count + list.Count) % list.Count]);
        }
        void Navigate(Keys key)
        {
            var list = Focusables();
            if (list.Count == 0) return;
            if (focused == null || !list.Contains(focused) || !focusVisible) { SetFocus(list.FirstOrDefault(w => w.Kind == Kind.Tile || w.Kind == Kind.GameTile) ?? list[0]); return; }
            var from = Theme.Center(focused.Bounds);
            float dx = key == Keys.Left ? -1 : key == Keys.Right ? 1 : 0, dy = key == Keys.Up ? -1 : key == Keys.Down ? 1 : 0;
            Widget best = null;
            float bestScore = float.MaxValue;
            foreach (var w in list)
            {
                if (w == focused) continue;
                var c = Theme.Center(w.Bounds);
                float vx = c.X - from.X, vy = c.Y - from.Y, along = vx * dx + vy * dy;
                if (along <= 1) continue;
                float score = along + (Math.Abs(vx * dy) + Math.Abs(vy * dx)) * 2.5f;
                if (score < bestScore) { bestScore = score; best = w; }
            }
            if (best != null) SetFocus(best);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            bool modified = (keyData & (Keys.Control | Keys.Alt)) != 0, shift = (keyData & Keys.Shift) != 0;
            bool typing = ActiveControl is TextBox && ActiveControl.Focused;
            if (!modified && key == Keys.Tab) { MoveFocus(shift ? -1 : 1); return true; }
            if (!modified && key == Keys.Escape && CloseTop()) return true;
            if (typing)
            {
                if (key == Keys.Enter) { MoveFocus(1); return true; }
                return base.ProcessCmdKey(ref msg, keyData);
            }
            if (modified || shift) return base.ProcessCmdKey(ref msg, keyData);
            switch (key)
            {
                case Keys.Left: case Keys.Right: case Keys.Up: case Keys.Down: Navigate(key); return true;
                case Keys.Enter: case Keys.Space:
                    if (focused != null && focused.Visible && focused.Layer == ActiveLayer) Press(focused);
                    return true;
                case Keys.B: case Keys.Back: CloseTop(); return true;
                case Keys.A: if (!libraryOpen && ActiveLayer == 0 && CanConnect) Connect(); return true;
                case Keys.X: if (!libraryOpen && ActiveLayer == 0 && CanConnect) StartDemo(); return true;
                case Keys.C: if (!libraryOpen && ActiveLayer == 0) NextCategory(); return true;
                case Keys.F: if (!libraryOpen && ActiveLayer == 0) NextChapter(); return true;
                case Keys.Y: if (ActiveLayer == 0 && CanDisconnect) Disconnect("Sessione scollegata."); return true;
                case Keys.P: if (!libraryOpen && ActiveLayer == 0 && CanDisconnect) TogglePause(); return true;
                case Keys.G: if (ActiveLayer == 0) ToggleLibrary(); return true;
                case Keys.PageDown: if (ActiveLayer == 0) { if (libraryOpen) libraryScroll += libraryGridRect.Height * 0.8f; else scrollTarget += gridRect.Height * 0.8f; Kick(); } return true;
                case Keys.PageUp: if (ActiveLayer == 0) { if (libraryOpen) libraryScroll -= libraryGridRect.Height * 0.8f; else scrollTarget -= gridRect.Height * 0.8f; Kick(); } return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---------------------------------------------------------------- actions
        void OpenCloudPage() { cloudPage = true; focused = hovered = null; Arrange(); PlaceFields(); Invalidate(); Kick(); }
        void OpenSheet() { cloudPage = false; sheetTarget = 1; hovered = null; focused = null; Kick(); }
        void CloseSheet() { if (cloudPage) { cloudPage = false; cloudPassword.Clear(); Arrange(); PlaceFields(); Invalidate(); return; } cloudPassword.Clear(); sheetTarget = 0; focused = null; PlaceFields(); Kick(); }
        void ToggleDrawer() { drawerTarget = drawerTarget > 0 ? 0 : 1; if (drawerTarget > 0) { unreadLogs = 0; logScroll = 0; } Kick(); }
        void CloseDrawer() { drawerTarget = 0; Kick(); }
        void OpenDetail(Achievement a) { detail = a; dialogTarget = 1; hovered = null; Kick(); }
        void CloseDialog() { dialogTarget = 0; Kick(); }
        bool CloseTop()
        {
            if (dialogTarget > 0) CloseDialog();
            else if (sheetTarget > 0) CloseSheet();
            else if (drawerTarget > 0) CloseDrawer();
            else if (libraryOpen) CloseLibrary();
            else return false;
            return true;
        }
        void SetFilter(int value)
        {
            if (filter == value) return;
            filter = value; scrollTarget = 0; scroll = 0;
            introStart = (float)clock.Elapsed.TotalSeconds;
            Invalidate(); Kick();
        }
        void FocusField(TextBox box) { if (box.Visible) box.Focus(); }
        void Browse()
        {
            using (var picker = new OpenFileDialog { Filter = "Cemu|Cemu.exe", Title = "Seleziona Cemu.exe" })
                if (picker.ShowDialog(this) == DialogResult.OK) cemuPath.Text = picker.FileName;
        }
        void ToggleSound() { soundOn = !soundOn; TrySaveSettings(); if (soundOn) Chime.Play(); }
        void ToggleAmbient() { ambientOn = !ambientOn; TrySaveSettings(); }
        void TrySaveSettings()
        {
            try { SaveSettings(); } catch (Exception ex) { Log("Impostazioni non salvate: " + ex.Message); }
        }
        int testTier;
        void TestToast()
        {
            // Each press previews the next rarity, preferring an objective with the owner's artwork.
            var tier = (AchievementRarity)(testTier++ % 3);
            var sample = rules.Where(a => a.Rarity == tier).OrderByDescending(a => arts[a.Id].HasIcon).ThenByDescending(a => a.Points).FirstOrDefault()
                ?? rules.OrderByDescending(a => a.Points).First();
            toasts.Enqueue(new PendingToast { Achievement = sample, Header = "ANTEPRIMA · NOTIFICA DI PROVA" });
            PumpToasts();
        }

        // ---------------------------------------------------------------- accessibility
        string Describe(Widget w)
        {
            if (w.Kind == Kind.Tile && w.Achievement != null)
            {
                if (w.Achievement.IsPlanned) return w.Help + " Obiettivo non attivo: nessuno sblocco automatico.";
                var unlock = progress == null ? null : progress.Get(w.Achievement.Id);
                return w.Help + " " + w.Achievement.Points + " punti. " +
                    (unlock != null ? "Sbloccato il " + unlock.Utc.ToLocalTime().ToString("g", Italian) + "." : "Da sbloccare.");
            }
            return w.Help;
        }
        protected override AccessibleObject CreateAccessibilityInstance() { return new SurfaceAccessible(this); }
        sealed class SurfaceAccessible : ControlAccessibleObject
        {
            readonly MainForm form;
            public SurfaceAccessible(MainForm owner) : base(owner) { form = owner; }
            List<Widget> Items()
            {
                form.Arrange();
                int layer = form.ActiveLayer;
                return form.visible.Where(w => w.Layer == layer).ToList();
            }
            public override int GetChildCount() { return Items().Count; }
            public override AccessibleObject GetChild(int index)
            {
                var items = Items();
                if (index < 0 || index >= items.Count) return null;
                var w = items[index];
                if (w.Box != null && w.Box.Visible) return w.Box.AccessibilityObject;
                return new WidgetAccessible(form, w, this);
            }
        }
        sealed class WidgetAccessible : AccessibleObject
        {
            readonly MainForm form;
            readonly Widget widget;
            readonly AccessibleObject parent;
            public WidgetAccessible(MainForm form, Widget widget, AccessibleObject parent) { this.form = form; this.widget = widget; this.parent = parent; }
            public override string Name { get { return widget.Name; } set { } }
            public override string Description { get { return form.Describe(widget); } }
            public override AccessibleObject Parent { get { return parent; } }
            public override Rectangle Bounds { get { return form.RectangleToScreen(form.Device(widget.Bounds)); } }
            public override string DefaultAction { get { return widget.Kind == Kind.Toggle ? "Attiva o disattiva" : "Premi"; } }
            public override AccessibleRole Role
            {
                get
                {
                    switch (widget.Kind)
                    {
                        case Kind.Tile: case Kind.GameTile: return AccessibleRole.ListItem;
                        case Kind.Tab: return AccessibleRole.PageTab;
                        case Kind.Toggle: return AccessibleRole.CheckButton;
                        case Kind.Field: return AccessibleRole.Text;
                        default: return AccessibleRole.PushButton;
                    }
                }
            }
            public override AccessibleStates State
            {
                get
                {
                    var s = AccessibleStates.Focusable;
                    if (!widget.Enabled) s |= AccessibleStates.Unavailable;
                    if (widget.Checked) s |= AccessibleStates.Checked;
                    if (form.focused == widget) s |= AccessibleStates.Focused;
                    return s;
                }
            }
            public override void DoDefaultAction() { form.BeginInvoke(new Action(() => form.Press(widget))); }
        }
    }
}
