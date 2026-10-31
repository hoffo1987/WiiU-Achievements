using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using WiiUAchievements;

// Offscreen, synthetic-only documentation renderer. Never connects or signs in.
class RenderDocumentation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(MainForm f, string name, object value) { typeof(MainForm).GetField(name, Private).SetValue(f, value); }
    static object Get(MainForm f, string name) { return typeof(MainForm).GetField(name, Private).GetValue(f); }
    static void Pump() { for (int i = 0; i < 40; i++) { Application.DoEvents(); Thread.Sleep(15); } }
    [STAThread] static void Main(string[] args)
    {
        string output = Path.GetFullPath(args[0]), state = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using (var f = new MainForm(false, false, state))
        {
            Set(f, "soundOn", false); Set(f, "ambientOn", false);
            f.Show(); f.Location = new Point(-20000, -20000);
            ((TextBox)Get(f, "player")).Text = "Profilo dimostrativo";
            ((TextBox)Get(f, "cemuPath")).Text = @"C:\Emulators\Cemu\Cemu.exe";
            var rules = (List<Achievement>)Get(f, "rules");
            var progress = new ProgressStore(Path.Combine(state, "synthetic-progress"), "documentation", "synthetic");
            progress.AwardMany(new[] { "story-heros-clothes", "story-telescope", "wallet-10", "hearts-20" }, new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc));
            Set(f, "progress", progress); Set(f, "shownRatio", 4f / 70); Set(f, "introStart", -10f);
            Pump();
            // Filter changes start the grid entrance; documentation frames show the settled layout.
            Action<string> save = name => { Set(f, "introStart", -10f); Set(f, "tabX", -1f); Pump(); f.SavePreview(Path.Combine(output, name + ".png")); };
            Action<string, object[]> call = (name, values) => typeof(MainForm).GetMethod(name, Private).Invoke(f, values);
            var search = (TextBox)Get(f, "search");
            save("main");
            // Hover feedback: a lifted card with the selection outline and a highlighted menu button.
            var hoverWidgets = (System.Collections.IDictionary)Get(f, "widgets");
            foreach (var id in new[] { "tile:story-hero-sword", "chapter" })
            {
                object w = hoverWidgets[id];
                w.GetType().GetField("Hover").SetValue(w, 1f);
            }
            f.SavePreview(Path.Combine(output, "hover.png"));
            foreach (var id in new[] { "tile:story-hero-sword", "chapter" }) { object w = hoverWidgets[id]; w.GetType().GetField("Hover").SetValue(w, 0f); }
            call("OpenMenu", new object[] { "category" }); Set(f, "menuOpen", 1f); save("menu-category"); call("CloseMenu", null); Set(f, "menuOpen", 0f); Set(f, "menu", null);
            call("OpenMenu", new object[] { "chapter" }); Set(f, "menuOpen", 1f); save("menu-chapter"); call("CloseMenu", null); Set(f, "menuOpen", 0f); Set(f, "menu", null);
            call("OpenMenu", new object[] { "more" }); Set(f, "menuOpen", 1f); save("menu-more"); call("CloseMenu", null); Set(f, "menuOpen", 0f); Set(f, "menu", null);
            search.Text = "cuore"; save("search");
            search.Text = "zattera volante"; save("search-empty"); search.Text = "";
            Set(f, "sortMode", 1); save("sorted-rarity"); Set(f, "filter", 1); Pump();
            // A gold card in view (rarity order) celebrates; frames at fixed offsets show the medal turn.
            var clock = (Stopwatch)Get(f, "clock");
            var celebrations = (Dictionary<string, float>)Get(f, "celebrations");
            var widgets = (System.Collections.IDictionary)Get(f, "widgets");
            float[] offsets = { 0.08f, 0.22f, 0.45f, 0.9f };
            using (var strip = new Bitmap(4 * 380, 166))
            {
                using (var g = Graphics.FromImage(strip))
                {
                    g.Clear(Theme.BackTop);
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        celebrations["hearts-20"] = (float)clock.Elapsed.TotalSeconds - offsets[i] * 2.2f;
                        string frame = Path.Combine(state, "celebration-" + i + ".png");
                        f.SavePreview(frame);
                        object tile = widgets["tile:hearts-20"];
                        var r = (RectangleF)tile.GetType().GetField("Bounds").GetValue(tile);
                        using (var full = new Bitmap(frame))
                            g.DrawImage(full, new Rectangle(i * 380, 0, 380, 166), new Rectangle((int)r.X - 14, (int)r.Y - 17, 380, 166), GraphicsUnit.Pixel);
                    }
                }
                strip.Save(Path.Combine(output, "celebration.png"));
            }
            celebrations.Clear(); Set(f, "sortMode", 0); Set(f, "filter", 0);
            // Supported window sizes: minimum, full HD (single-row toolbar) and ultrawide (halved).
            var original = f.ClientSize;
            f.ClientSize = new Size(900, 680); save("main-small");
            call("OpenMenu", new object[] { "chapter" }); Set(f, "menuOpen", 1f); save("menu-small"); call("CloseMenu", null); Set(f, "menuOpen", 0f); Set(f, "menu", null);
            Set(f, "detail", rules.Single(a => a.Id == "story-zelda")); Set(f, "dialog", 1f); Set(f, "dialogTarget", 1f); save("detail-small");
            Set(f, "dialog", 0f); Set(f, "dialogTarget", 0f); Set(f, "detail", null);
            f.ClientSize = new Size(1920, 1080); save("main-wide");
            f.ClientSize = new Size(3440, 1440); Set(f, "introStart", -10f); Pump();
            string ultra = Path.Combine(state, "ultrawide.png");
            f.SavePreview(ultra);
            using (var full = new Bitmap(ultra))
            using (var half = new Bitmap(1720, 720))
            {
                using (var g = Graphics.FromImage(half)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(full, 0, 0, 1720, 720); }
                half.Save(Path.Combine(output, "main-ultrawide.png"));
            }
            f.ClientSize = original; Pump();
            Set(f, "libraryOpen", true); save("games"); Set(f, "libraryOpen", false);
            Set(f, "sheet", 1f); Set(f, "sheetTarget", 1f); save("settings"); Set(f, "sheet", 0f); Set(f, "sheetTarget", 0f);
            Set(f, "detail", rules.Single(a => a.Id == "hearts-20")); Set(f, "dialog", 1f); Set(f, "dialogTarget", 1f); save("detail");
            Set(f, "detail", rules.Single(a => a.Id == "story-hero-sword")); save("detail-locked");
            Set(f, "dialog", 0f); Set(f, "dialogTarget", 0f); Set(f, "detail", null);
            Set(f, "categoryFilter", 6); save("growth"); Set(f, "categoryFilter", 0);
            ((List<string>)Get(f, "logLines")).Clear();
            ((List<string>)Get(f, "logLines")).Add("12:00:00  Anteprima dimostrativa, nessun collegamento a Cemu.");
            Set(f, "drawer", 1f); Set(f, "drawerTarget", 1f); save("drawer"); Set(f, "drawer", 0f); Set(f, "drawerTarget", 0f);
            Set(f, "demo", true); Set(f, "paused", true); Set(f, "recording", false);
            var statusField = typeof(MainForm).GetField("statusKind", Private);
            statusField.SetValue(f, Enum.Parse(statusField.FieldType, "Paused"));
            Set(f, "hint", "Pausa dimostrativa. Nessuna partita collegata.");
            Set(f, "statusTitle", "Registrazione in pausa"); Set(f, "statusDetail", "Dati dimostrativi, nessuna partita collegata."); save("paused");
            Set(f, "demo", false); Set(f, "paused", false); statusField.SetValue(f, Enum.Parse(statusField.FieldType, "Idle"));
            Set(f, "hint", "Anteprima dimostrativa. Nessuna partita collegata."); Set(f, "statusTitle", "Anteprima dello sblocco");
            // The unlocked tab in rarity order keeps the celebrating gold card in view.
            Set(f, "filter", 1); Set(f, "sortMode", 1); Set(f, "introStart", -10f); Set(f, "tabX", -1f); Pump();
            ((Dictionary<string, float>)Get(f, "celebrations"))["hearts-20"] = (float)((Stopwatch)Get(f, "clock")).Elapsed.TotalSeconds - 0.9f;
            f.SavePreview(Path.Combine(output, "unlock.png"));
            Set(f, "filter", 0); Set(f, "sortMode", 0);
            foreach (var tier in new[] { Tier.Bronze, Tier.Silver, Tier.Gold })
            {
                var a = rules.First(r => (Tier)r.Rarity == tier && !r.IsPlanned);
                using (var b = ToastForm.Render(a, Art.For(a, rules, GameCatalog.WindWakerId), "OBIETTIVO SBLOCCATO", 1.6f, 1))
                    b.Save(Path.Combine(output, tier == Tier.Gold ? "toast.png" : "toast-" + tier.ToString().ToLowerInvariant() + ".png"));
            }
            var gold = rules.Single(a => a.Id == "hearts-20");
            float[] times = { 0.2f, 0.65f, 1.2f, 2.2f };
            using (var first = ToastForm.Render(gold, Art.For(gold, rules, GameCatalog.WindWakerId), "OBIETTIVO SBLOCCATO", times[0], 1))
            using (var sheet = new Bitmap(first.Width, first.Height * times.Length))
            {
                using (var g = Graphics.FromImage(sheet)) for (int i = 0; i < times.Length; i++)
                    using (var b = ToastForm.Render(gold, Art.For(gold, rules, GameCatalog.WindWakerId), "OBIETTIVO SBLOCCATO", times[i], 1)) g.DrawImageUnscaled(b, 0, i * first.Height);
                sheet.Save(Path.Combine(output, "toast-timeline.png"));
            }
            foreach (bool locked in new[] { false, true })
                using (var sheet = new Bitmap(1120, ((rules.Count + 9) / 10) * 142))
                {
                    using (var g = Graphics.FromImage(sheet))
                    {
                        g.Clear(Theme.BackTop); g.SmoothingMode = SmoothingMode.AntiAlias;
                        for (int i = 0; i < rules.Count; i++)
                        {
                            float x = i % 10 * 112, y = i / 10 * 142;
                            Theme.Medal(g, new RectangleF(x + 8, y + 4, 96, 96), Art.For(rules[i], rules, GameCatalog.WindWakerId), locked ? 0.88f : 0);
                            Theme.Text(g, rules[i].Title, Theme.Font(10, Weight.Bold), Theme.Ink, new RectangleF(x + 3, y + 101, 106, 38), StringAlignment.Center, StringAlignment.Near, true);
                        }
                    }
                    sheet.Save(Path.Combine(output, locked ? "medals-locked.png" : "medals.png"));
                }
            // The player-facing list of executable objectives lives beside the screenshots folder.
            if (String.Equals(Path.GetFileName(output), "screenshots", StringComparison.OrdinalIgnoreCase))
                WriteAvailableList(rules, Path.Combine(Path.GetDirectoryName(output), "ACHIEVEMENTS-DISPONIBILI.txt"));
            f.Close();
        }
    }
    // Regenerated with the screenshots so titles, chapters and rarities always match the catalog.
    static void WriteAvailableList(List<Achievement> rules, string path)
    {
        var active = rules.Where(a => !a.IsPlanned).ToList();
        var text = new System.Text.StringBuilder();
        Func<Tier, string> rarity = t => Art.TierName(t);
        text.AppendLine("WII U ACHIEVEMENTS - THE WIND WAKER HD");
        text.AppendLine("Achievement disponibili nella build di sviluppo");
        text.AppendLine();
        text.AppendLine(active.Count + " obiettivi eseguibili: " + active.Count(a => !a.Experimental) + " attivi di base e " + active.Count(a => a.Experimental) + " con rilevamento sperimentale.");
        text.AppendLine("I " + (rules.Count - active.Count) + " obiettivi ancora non implementati sono esclusi da questo elenco.");
        text.AppendLine("Rarità degli obiettivi eseguibili: " + String.Join(" | ", new[] { Tier.Bronze, Tier.Silver, Tier.Gold, Tier.Platinum }
            .Where(t => active.Any(a => (Tier)a.Rarity == t)).Select(t => rarity(t) + ": " + active.Count(a => (Tier)a.Rarity == t))) + ".");
        text.AppendLine();
        text.AppendLine("COME USARE QUESTO ELENCO");
        text.AppendLine("Configurazione supportata: Cemu 2.6, Wind Waker HD EUR v0, RPX cdc68acd.");
        text.AppendLine("Puoi lasciare l'app collegata: caricamenti e selezione del file sono gestiti automaticamente.");
        text.AppendLine("Le condizioni già soddisfatte nel salvataggio vengono riconosciute dopo il caricamento, con tre conferme.");
        text.AppendLine("SPERIMENTALE = il rilevamento è implementato e può sbloccare, ma attende conferma durante una partita reale.");
        text.AppendLine("ATTIVO = regola di base eseguibile; non significa che ogni soglia sia stata verificata nel gioco reale.");
        text.AppendLine("La rarità è editoriale, indipendente dai punti e dallo stato del rilevamento.");
        text.AppendLine("ATTENZIONE: sono presenti anticipazioni sulla storia e sugli oggetti.");
        int number = 0;
        foreach (var chapter in active.Select(a => a.Chapter).Distinct().OrderBy(c => c, StringComparer.Ordinal))
        {
            text.AppendLine();
            text.AppendLine(new string('=', 60));
            text.AppendLine(chapter.ToUpperInvariant());
            text.AppendLine(new string('=', 60));
            foreach (var a in active.Where(x => x.Chapter == chapter))
            {
                text.AppendLine();
                text.AppendLine((++number).ToString("00") + ". " + a.Title);
                text.AppendLine("    Obiettivo: " + a.Description);
                text.AppendLine("    Rarità: " + rarity((Tier)a.Rarity) + " | Punti: " + a.Points + " | Rilevamento: " + (a.Experimental ? "SPERIMENTALE" : "ATTIVO"));
            }
        }
        text.AppendLine();
        text.AppendLine("Fonte: regole eseguibili achievements.xml e achievements-story.xml, con testi, rarità e ordine di achievement-presentation.xml.");
        text.AppendLine("Generato da tools/render-docs.ps1. Le voci non implementate sono descritte in docs/ACHIEVEMENTS.md.");
        File.WriteAllText(path, text.ToString(), new System.Text.UTF8Encoding(true));
    }
}
