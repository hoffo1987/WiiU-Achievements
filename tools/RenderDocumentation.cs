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
            Action<string> save = name => { Pump(); f.SavePreview(Path.Combine(output, name + ".png")); };
            save("main"); Set(f, "libraryOpen", true); save("games"); Set(f, "libraryOpen", false);
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
            ((Dictionary<string, float>)Get(f, "celebrations"))["hearts-20"] = (float)((Stopwatch)Get(f, "clock")).Elapsed.TotalSeconds - 0.9f;
            f.SavePreview(Path.Combine(output, "unlock.png"));
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
            f.Close();
        }
    }
}
