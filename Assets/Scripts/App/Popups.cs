using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public class Popup
    {
        public RectTransform Root, Card, Content;
        public Action OnClose;
        public bool Closing;
        public float Width, Height;
        public void Close() { GameApp.I.ClosePopup(this); }
    }

    public partial class GameApp
    {
        // =============== popup frame ===============
        /// <summary>Dark scrim + a rounded card with a ribbon title. Add your content to popup.Content.</summary>
        public Popup OpenPopup(string title, float w, float h, bool closable = true, string ribbon = "ribbon_green", Action onClose = null)
        {
            h = Mathf.Min(h, canvasRt.rect.height - 150);
            var p = new Popup { Width = w, Height = h, OnClose = onClose };
            p.Root = UI.Stretch(UI.Node(popupLayer, "Popup_" + title));
            var scrim = UI.Solid(p.Root, new Color(0.03f, 0.05f, 0.14f, 0.72f), "Scrim", true);
            UI.Stretch(scrim.rectTransform);
            if (closable) UI.Click(scrim, p.Close, false);
            else scrim.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            p.Card = UI.Node(p.Root, "Card");
            UI.Place(p.Card, 0.5f, 0.5f, 0, -20, w, h);
            var card = UI.Sliced(p.Card, PopupBody(ribbon), 260, "Panel", true);
            UI.Stretch(card.rectTransform);
            p.Content = UI.Node(p.Card, "Content");
            UI.Stretch(p.Content, 36, 36, 36, 96);

            if (!string.IsNullOrEmpty(title))
            {
                var rb = UI.Img(p.Card, ribbon, "Title");
                rb.preserveAspect = false;
                UI.Place(rb.rectTransform, 0.5f, 1, 0, 14, Mathf.Min(w - 30, 700), 160);
                var t = UI.Label(rb.transform, title, 54, Color.white, TextAnchor.MiddleCenter, true);
                t.GetComponent<Outline>().effectColor = RibbonOutline(ribbon);
                UI.Place(t.rectTransform, 0.5f, 0.5f, 0, 14, 480, 80);
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 30; t.resizeTextMaxSize = 54;
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            }
            if (closable)
            {
                var x = UI.GlyphButton(p.Card, "g_close", Palette.Red, 84, "Close");
                UI.Place(x.rectTransform, 1, 1, -22, -22, 84, 84);
                UI.Click(x, p.Close);
            }
            popups.Add(p);
            Fx.PopIn(p.Card, 0.32f);
            return p;
        }

        static string PopupBody(string ribbon)
        {
            switch (ribbon)
            {
                case "ribbon_orange": case "ribbon_yellow": return "popup_cream";
                case "ribbon_pink": case "ribbon_red": return "popup_pink";
                case "ribbon_green": case "ribbon_teal": return "popup_mint";
                case "ribbon_purple": return "popup_lilac";
                default: return "popup";
            }
        }

        static Color RibbonOutline(string ribbon)
        {
            switch (ribbon)
            {
                case "ribbon_orange": return new Color32(0x9A, 0x4A, 0x08, 255);
                case "ribbon_yellow": return new Color32(0x9A, 0x6A, 0x00, 255);
                case "ribbon_pink": return new Color32(0x9C, 0x1F, 0x63, 255);
                case "ribbon_red": return new Color32(0x92, 0x1B, 0x2A, 255);
                case "ribbon_green": return new Color32(0x1E, 0x6B, 0x25, 255);
                case "ribbon_teal": return new Color32(0x0B, 0x6A, 0x66, 255);
                case "ribbon_purple": return new Color32(0x4C, 0x24, 0x9A, 255);
                default: return new Color32(0x12, 0x4F, 0x9A, 255);
            }
        }

        public void ClosePopup(Popup p)
        {
            if (p == null || p.Closing) return;
            p.Closing = true;
            popups.Remove(p);
            var card = p.Card;
            Fx.Tween(0.16f, t => { if (card != null) card.localScale = Vector3.one * (1 - 0.2f * t); }, () => { if (p.Root != null) Destroy(p.Root.gameObject); p.OnClose?.Invoke(); });
        }

        public void CloseAllPopups() { foreach (var p in new List<Popup>(popups)) ClosePopup(p); }
        public bool HasPopup => popups.Count > 0;

        // =============== rows used in lists ===============
        public RectTransform ListRow(RectTransform content, ref float y, float h, float w)
        {
            var row = RowAt(content, ref y, h, w);
            var bg = UI.Sliced(row, "card_a", h, "Bg");
            UI.Stretch(bg.rectTransform);
            y += 14;
            return row;
        }

        public Button SmallButton(Transform parent, string art, string text, float w, float h, Action click, int size = 40)
        {
            return UI.Pill(parent, art, text, w, h, click, size);
        }

        // =============== settings ===============
        public void ShowSettings()
        {
            var d = SaveSystem.Data;
            var p = OpenPopup("SETTINGS", 900, 1040);
            float y = 40;
            void Toggle(string label, Func<bool> get, Action<bool> set)
            {
                var row = UI.Node(p.Content, label);
                UI.Place(row, 0.5f, 1, 0, -y - 60, 800, 110);
                var t = UI.Label(row, label, 46, Palette.Ink, TextAnchor.MiddleLeft);
                UI.PlaceL(t.rectTransform, 0, 0.5f, 170, 0, 400, 70);
                var btn = UI.Pill(row, get() ? "btn_green" : "btn_grey", get() ? "ON" : "OFF", 220, 90, null, 44);
                UI.Place((RectTransform)btn.transform, 1, 0.5f, -130, 0, 220, 90);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    set(!get()); SaveSystem.Save(); Sfx.Play(Sfx.Kind.Click);
                    UI.ApplyStyle(btn.GetComponent<Image>(), get() ? "btn_green" : "btn_grey", 90);
                    UI.ButtonLabel(btn).text = get() ? "ON" : "OFF";
                });
                var ic = UI.Icon(row, label == "Sound" ? "sound_on" : label == "Haptics" ? "xp" : "bell", 100, "Ic");
                UI.Place(ic.rectTransform, 0, 0.5f, 90, 0, 100, 100);
                y += 130;
            }
            Toggle("Sound", () => d.sound, v => d.sound = v);
            Toggle("Haptics", () => d.haptics, v => d.haptics = v);
            Toggle("Alerts", () => d.notifOn, v => { d.notifOn = v; if (v) NotificationService.AskPermission(); });

            var priv = UI.Pill(p.Content, "btn_blue", "PRIVACY POLICY", 520, 96, () => Application.OpenURL("https://sid-sys.github.io/wordquestdaily-privacy/"), 40);
            UI.Place((RectTransform)priv.transform, 0.5f, 0, 0, 120, 520, 96);
            var how = UI.Pill(p.Content, "btn_yellow", "HOW TO PLAY", 520, 96, () => { p.Close(); ShowHowTo(); }, 40);
            UI.Place((RectTransform)how.transform, 0.5f, 0, 0, 250, 520, 96);
            var about = UI.Label(p.Content, "Word Quest Daily  v" + Application.version, 28, Palette.InkSoft);
            UI.Place(about.rectTransform, 0.5f, 0, 0, 36, 800, 40);
        }

        public void ShowHowTo()
        {
            var p = OpenPopup("HOW TO PLAY", 900, 1000);
            string[] lines =
            {
                "Swipe across letters to find the words on the list.",
                "Words go up, down, sideways and diagonal.",
                "A hidden MYSTERY WORD (shown as ??????) is worth a bonus star.",
                "Finish without power-ups for the third star.",
                "No timer. Take your time!",
            };
            float y = 30;
            foreach (var l in lines)
            {
                var t = UI.Wrapped(p.Content, l, 40, Palette.Ink, 780, TextAnchor.UpperCenter);
                UI.Place(t.rectTransform, 0.5f, 1, 0, -y - 70, 780, 140);
                y += 150;
            }
            var ok = UI.Pill(p.Content, "btn_green", "GOT IT", 420, 110, p.Close, 50);
            UI.Place((RectTransform)ok.transform, 0.5f, 0, 0, 50, 420, 110);
        }

        // =============== daily reward ===============
        public void ShowDailyReward()
        {
            var p = OpenPopup("DAILY REWARD", 980, 1180, true, "ribbon_pink");
            int day = Progress.DailyRewardDayIndex();
            bool can = Progress.CanClaimDailyReward();
            var sub = UI.Label(p.Content, can ? "Come back every day for bigger gifts!" : "Come back tomorrow for the next gift", 36, Palette.InkSoft);
            UI.Place(sub.rectTransform, 0.5f, 1, 0, -50, 860, 50);

            float tw = 214, th = 290, gx = 18, gy = 22;
            var tiles = new List<RectTransform>();
            for (int i = 0; i < 7; i++)
            {
                int col = i < 4 ? i : i - 4, row = i < 4 ? 0 : 1;
                float rowW = i < 4 ? 4 * tw + 3 * gx : 3 * tw + 2 * gx;
                float x = -rowW / 2 + tw / 2 + col * (tw + gx);
                var tile = UI.Sliced(p.Content, i == 6 ? "card_b" : "card_a", 200, "Day" + (i + 1), false);
                bool current = i == day && can, done = i < day || (i == day && !can);
                if (current) UI.ApplyStyle(tile, "card_gold", 200);
                if (done) UI.ApplyStyle(tile, "card_green", 200);
                UI.Place(tile.rectTransform, 0.5f, 1, x, -150 - row * (th + gy) - th / 2, tw, th);
                var head = UI.Label(tile.transform, "DAY " + (i + 1), 28, Palette.Ink);
                UI.Place(head.rectTransform, 0.5f, 1, 0, -44, 120, 36);
                var r = Economy.Daily[i];
                string art = r.Power >= 0 && r.Coins == 0 ? PowerUps.Art[r.Power] : i == 6 ? "chest" : r.Coins >= 250 ? "coin_bag" : r.Coins >= 150 ? "coin_stack" : "coin";
                var ic = UI.Icon(tile.transform, art, 108, "Icon");
                UI.Place(ic.rectTransform, 0.5f, 1, 0, -146, 108, 108);
                var lab = UI.Label(tile.transform, r.Label, 24, Palette.Ink);
                lab.horizontalOverflow = HorizontalWrapMode.Wrap;
                UI.Place(lab.rectTransform, 0.5f, 1, 0, -236, 176, 60);
                if (done) { var ck = UI.Icon(tile.transform, "check", 42, "Done"); UI.Place(ck.rectTransform, 1, 1, -34, -40, 42, 42); }
                if (current) { tiles.Add(tile.rectTransform); tile.rectTransform.localScale = Vector3.one; }
            }
            var claim = UI.Pill(p.Content, can ? "btn_green" : "btn_grey", can ? "CLAIM" : "COME BACK TOMORROW", 620, 120, null, can ? 56 : 38);
            UI.Place((RectTransform)claim.transform, 0.5f, 0, 0, 50, 620, 120);
            claim.onClick.RemoveAllListeners();
            claim.onClick.AddListener(() =>
            {
                if (!Progress.CanClaimDailyReward()) { p.Close(); return; }
                HoldCoins(1.8f);
                var rw = Progress.ClaimDailyReward();
                Sfx.Play(Sfx.Kind.Coin);
                var from = Vector2.zero;
                Fx.Fly(fxLayer, from, CoinTarget(), rw.Power >= 0 && rw.Coins == 0 ? PowerUps.Art[rw.Power] : "coin", rw.Coins > 0 ? 10 : 1, () => Sfx.Play(Sfx.Kind.Coin, 1f + UnityEngine.Random.value * 0.2f, 0.5f), () => { RefreshCoinPills(); p.Close(); Toast("Reward collected: " + rw.Label, Palette.Yellow); });
                claim.interactable = false;
            });
        }
    }
}
