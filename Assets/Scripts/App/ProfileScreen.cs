using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        void BuildProfile(RectTransform b)
        {
            var d = SaveSystem.Data;
            TopBar(b);
            RectTransform content;
            var sr = Scroll(b, out content);
            UI.Stretch((RectTransform)sr.transform, 0, 0, 0, 160);
            float y = 10;

            // ---- header card ----
            var hr = RowAt(content, ref y, 470);
            var card = UI.Sliced(hr, "card_b", 300, "Card"); UI.Stretch(card.rectTransform);
            var av = Avatar(hr, d.avatar, d.ring, 250);
            UI.Place(av, 0.5f, 1, 0, -170, 250, 250);
            var name = UI.Label(hr, d.name.ToUpper(), 60, Palette.Ink, TextAnchor.MiddleCenter, false);
            UI.Place(name.rectTransform, 0.5f, 1, 0, -335, 700, 76);
            var edit = UI.Icon(hr, "scroll", 60, "Edit"); UI.Place(edit.rectTransform, 0.5f, 1, name.preferredWidth / 2 + 60, -335, 60, 60);
            UI.Click(edit, ShowNameEdit);
            var bar = UI.ProgressBar(hr, 700, 50, Palette.Blue, "Xp"); UI.Place(bar.Root, 0.5f, 0, 0, 56, 700, 50); bar.Set(Progress.XpFraction);
            var bl = UI.Label(bar.Root, $"LEVEL {d.playerLevel}   {d.xp}/{Economy.XpForLevel(d.playerLevel)} XP", 30, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(bl.rectTransform);
            y += 20;

            // ---- stats ----
            var sr2 = RowAt(content, ref y, 250);
            string[] sn = { "STREAK", "GAMES", "WORDS", "PERFECT" };
            int[] sv = { Progress.EffectiveStreak(), d.gamesPlayed, d.wordsFound, d.perfectLevels };
            string[] si = { "flame", "trophy", "book", "star" };
            for (int i = 0; i < 4; i++)
            {
                var cell = UI.Sliced(sr2, "card_a", 250, "Stat" + i);
                UI.Place(cell.rectTransform, 0, 0.5f, 125 + i * 250, 0, 236, 250);
                var ic = UI.Icon(cell.transform, si[i], 90, "Ic"); UI.Place(ic.rectTransform, 0.5f, 1, 0, -70, 90, 90);
                var v = UI.Label(cell.transform, sv[i].ToString("N0"), 44, Palette.Ink); UI.Place(v.rectTransform, 0.5f, 0.5f, 0, -30, 220, 56);
                var l = UI.Label(cell.transform, sn[i], 26, Palette.InkSoft); UI.Place(l.rectTransform, 0.5f, 0, 0, 56, 220, 34);
            }
            y += 20;

            // ---- achievements ----
            Section(content, ref y, "ACHIEVEMENTS");
            for (int i = 0; i < Progress.Achievements.Length; i++)
            {
                int idx = i;
                var a = Progress.Achievements[i];
                var row = ListRow(content, ref y, 160, 1000);
                var ic = UI.Icon(row, a.Art, 120, "Medal"); UI.Place(ic.rectTransform, 0, 0.5f, 90, 0, 120, 120);
                bool claimed = d.achievementClaimed[i], ready = Progress.AchievementReady(i);
                if (!claimed && !ready) ic.color = new Color(0.6f, 0.6f, 0.6f, 0.9f);
                var n = UI.Label(row, a.Name, 40, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(n.rectTransform, 0, 1, 180, -42, 520, 50);
                var ds = UI.Label(row, a.Desc, 28, Palette.InkSoft, TextAnchor.MiddleLeft); UI.PlaceL(ds.rectTransform, 0, 1, 180, -88, 520, 38);
                int val = Mathf.Min(a.Value(d), a.Goal);
                var pb = UI.ProgressBar(row, 420, 34, Palette.Green, "P"); UI.Place(pb.Root, 0, 0, 390, 26, 420, 34); pb.Set(val / (float)a.Goal);
                var pt = UI.Label(pb.Root, $"{val}/{a.Goal}", 22, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(pt.rectTransform);
                if (ready)
                {
                    var cb = UI.Pill(row, "btn_yellow", $"+{a.Coins}", 200, 84, () =>
                    {
                        int c = Progress.ClaimAchievement(idx);
                        if (c > 0) { Sfx.Play(Sfx.Kind.Level); Toast($"+{c} coins!", Palette.Yellow); ShowTab(Tab.Profile); }
                    }, 40);
                    UI.Place((RectTransform)cb.transform, 1, 0.5f, -130, 0, 200, 84);
                }
                else if (claimed) { var ck = UI.Icon(row, "check", 76, "Done"); UI.Place(ck.rectTransform, 1, 0.5f, -80, 0, 76, 76); }
            }
            y += 10;

            // ---- avatars ----
            Section(content, ref y, "AVATAR");
            var grid = RowAt(content, ref y, 3 * 250 + 20);
            for (int i = 0; i < 12; i++)
            {
                int idx = i; bool own = d.avatarOwned[i];
                float x = ((i % 4) - 1.5f) * 250, yy = -(i / 4) * 250 - 125;
                var cell = UI.Node(grid, "Av" + i); UI.Place(cell, 0.5f, 1, x, yy, 220, 220);
                var pic = UI.Img(cell, "av_" + i, "Pic", true); UI.Stretch(pic.rectTransform, 10, 10, 10, 10);
                if (!own) pic.color = new Color(0.35f, 0.35f, 0.4f, 0.9f);
                if (d.avatar == i) { var sel = UI.Img(cell, "spark_ring", "Sel"); sel.color = Palette.Yellow; UI.Stretch(sel.rectTransform, -12, -12, -12, -12); }
                if (!own)
                {
                    var lk = UI.Icon(cell, "lock", 70, "Lock"); UI.Place(lk.rectTransform, 0.5f, 0.5f, 0, 14, 70, 70);
                    string need = i == 9 ? "30 streak" : "Lv " + Economy.AvatarLevel[i];
                    var lt = UI.Label(cell, need, 28, Color.white, TextAnchor.MiddleCenter, true); UI.Place(lt.rectTransform, 0.5f, 0.5f, 0, -36, 200, 36);
                }
                UI.Click(pic, () =>
                {
                    if (!SaveSystem.Data.avatarOwned[idx]) { Toast(idx == 9 ? "Reach a 30 day streak" : $"Reach level {Economy.AvatarLevel[idx]}", Palette.Yellow); Sfx.Play(Sfx.Kind.Error); return; }
                    SaveSystem.Data.avatar = idx; Progress.Notify(); ShowTab(Tab.Profile);
                });
            }
            y += 10;

            // ---- frames ----
            Section(content, ref y, "FRAME");
            var fr = RowAt(content, ref y, 230);
            for (int i = 0; i < 5; i++)
            {
                int idx = i; // 0 = none
                bool own = i == 0 || d.ringOwned[i - 1];
                var cell = UI.Sliced(fr, "card_a", 200, "Fr" + i, true); UI.Place(cell.rectTransform, 0, 0.5f, 100 + i * 200, 0, 190, 200);
                if (i > 0) { var im = UI.Icon(cell.transform, "ring_" + Economy.RingNames[i - 1].ToLower(), 150, "R"); UI.Place(im.rectTransform, 0.5f, 0.5f, 0, 10, 150, 150); if (!own) im.color = new Color(0.4f, 0.4f, 0.4f, 0.8f); }
                else { var t = UI.Label(cell.transform, "NONE", 34, Palette.Ink); UI.Place(t.rectTransform, 0.5f, 0.5f, 0, 10, 160, 50); }
                if (!own) { var lt = UI.Label(cell.transform, "Lv " + Economy.RingLevel[i - 1], 26, Palette.InkSoft); UI.Place(lt.rectTransform, 0.5f, 0, 0, 22, 160, 34); }
                if (d.ring == i) cell.color = new Color(1f, 0.93f, 0.55f);
                UI.Click(cell, () => { if (idx > 0 && !SaveSystem.Data.ringOwned[idx - 1]) { Toast("Not unlocked yet", Palette.Yellow); return; } SaveSystem.Data.ring = idx; Progress.Notify(); ShowTab(Tab.Profile); });
            }
            y += 10;

            // ---- board themes ----
            Section(content, ref y, "BOARD THEME");
            var th = RowAt(content, ref y, 300);
            for (int i = 0; i < 4; i++)
            {
                int idx = i; bool own = d.themeOwned[i];
                var cell = UI.Node(th, "Th" + i); UI.Place(cell, 0, 0.5f, 125 + i * 250, 0, 230, 290);
                var bg = UI.Img(cell, "bg_" + ThemeKey(i), "Bg", true); bg.preserveAspect = false; bg.type = Image.Type.Simple;
                UI.Stretch(bg.rectTransform, 0, 60, 0, 0);
                var fm = UI.Img(cell, "frame_" + ThemeKey(i), "Frame"); UI.Stretch(fm.rectTransform, -10, 50, -10, -10);
                if (!own) bg.color = new Color(0.3f, 0.3f, 0.35f, 1f);
                var nm = UI.Label(cell, own ? Economy.ThemeNames[i] : "Lv " + Economy.ThemeLevel[i], 32, d.theme == i ? Palette.Yellow : Color.white, TextAnchor.MiddleCenter, true);
                UI.Place(nm.rectTransform, 0.5f, 0, 0, 24, 220, 44);
                if (!own) { var lk = UI.Icon(cell, "lock", 70, "L"); UI.Place(lk.rectTransform, 0.5f, 0.5f, 0, 30, 70, 70); }
                UI.Click(bg, () => { if (!SaveSystem.Data.themeOwned[idx]) { Toast($"Reach level {Economy.ThemeLevel[idx]}", Palette.Yellow); return; } SaveSystem.Data.theme = idx; Progress.Notify(); ShowTab(Tab.Profile); });
            }
            y += 10;

            // ---- selection effects ----
            Section(content, ref y, "WORD EFFECT");
            var ef = RowAt(content, ref y, 130);
            for (int i = 0; i < 5; i++)
            {
                int idx = i; bool own = d.effectOwned[i];
                var btn = UI.Pill(ef, d.effect == i ? "btn_green" : own ? "btn_blue" : "btn_grey", own ? Economy.EffectNames[i] : "Lv " + Economy.EffectLevel[i], 190, 100, () =>
                {
                    if (!SaveSystem.Data.effectOwned[idx]) { Toast($"Reach level {Economy.EffectLevel[idx]}", Palette.Yellow); return; }
                    SaveSystem.Data.effect = idx; Progress.Notify(); ShowTab(Tab.Profile);
                }, 30);
                UI.Place((RectTransform)btn.transform, 0, 0.5f, 100 + i * 200, 0, 190, 100);
            }
            EndScroll(content, y);
        }

        void Section(RectTransform content, ref float y, string text)
        {
            var r = RowAt(content, ref y, 90);
            var t = UI.Label(r, text, 48, Color.white, TextAnchor.MiddleLeft, true);
            UI.PlaceL(t.rectTransform, 0, 0.5f, 20, 0, 600, 60);
        }

        void ShowNameEdit()
        {
            var p = OpenPopup("YOUR NAME", 900, 640);
            var box = UI.Sliced(p.Content, "btn_white", 110, "Box", true); UI.Place(box.rectTransform, 0.5f, 1, 0, -130, 700, 120);
            var input = box.gameObject.AddComponent<InputField>();
            var txt = UI.Label(box.transform, "", 46, Palette.Ink, TextAnchor.MiddleCenter); UI.Stretch(txt.rectTransform, 20, 0, 20, 0);
            var ph = UI.Label(box.transform, "Type your name", 42, Palette.Grey, TextAnchor.MiddleCenter); UI.Stretch(ph.rectTransform, 20, 0, 20, 0);
            input.textComponent = txt; input.placeholder = ph; input.characterLimit = 14; input.text = SaveSystem.Data.name;
            var save = UI.Pill(p.Content, "btn_green", "SAVE", 400, 110, () =>
            {
                string n = input.text.Trim();
                if (n.Length == 0) n = "Player";
                SaveSystem.Data.name = n; Progress.Notify(); p.Close(); ShowTab(Tab.Profile);
            }, 52);
            UI.Place((RectTransform)save.transform, 0.5f, 0, 0, 60, 400, 110);
        }
    }
}
