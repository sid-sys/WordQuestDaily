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
            var p = new Popup { Width = w, Height = h, OnClose = onClose };
            p.Root = UI.Stretch(UI.Node(popupLayer, "Popup_" + title));
            var scrim = UI.Solid(p.Root, new Color(0.03f, 0.05f, 0.14f, 0.72f), "Scrim", true);
            UI.Stretch(scrim.rectTransform);
            if (closable) UI.Click(scrim, p.Close, false);
            else scrim.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            p.Card = UI.Node(p.Root, "Card");
            UI.Place(p.Card, 0.5f, 0.5f, 0, -20, w, h);
            var card = UI.Sliced(p.Card, "popup", 260, "Panel", true);
            UI.Stretch(card.rectTransform);
            p.Content = UI.Node(p.Card, "Content");
            UI.Stretch(p.Content, 36, 36, 36, 96);

            if (!string.IsNullOrEmpty(title))
            {
                var rb = UI.Img(p.Card, ribbon, "Title");
                rb.preserveAspect = false;
                UI.Place(rb.rectTransform, 0.5f, 1, 0, 6, Mathf.Min(w - 40, 720), 130);
                var t = UI.Label(rb.transform, title, 54, Color.white, TextAnchor.MiddleCenter, true);
                UI.Place(t.rectTransform, 0.5f, 0.5f, 0, 6, 560, 90);
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 30; t.resizeTextMaxSize = 54;
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            }
            if (closable)
            {
                var x = UI.Icon(p.Card, "rb_close", 96, "Close");
                UI.Place(x.rectTransform, 1, 1, -16, -16, 96, 96);
                UI.Click(x, p.Close);
            }
            popups.Add(p);
            Fx.PopIn(p.Card, 0.32f);
            return p;
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
            var p = OpenPopup("SETTINGS", 900, 900);
            float y = 40;
            void Toggle(string label, Func<bool> get, Action<bool> set)
            {
                var row = UI.Node(p.Content, label);
                UI.Place(row, 0.5f, 1, 0, -y - 60, 800, 110);
                var t = UI.Label(row, label, 46, Palette.Ink, TextAnchor.MiddleLeft);
                UI.Place(t.rectTransform, 0, 0.5f, 240, 0, 460, 70);
                var btn = UI.Pill(row, get() ? "btn_green" : "btn_grey", get() ? "ON" : "OFF", 220, 90, null, 44);
                UI.Place((RectTransform)btn.transform, 1, 0.5f, -130, 0, 220, 90);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    set(!get()); SaveSystem.Save(); Sfx.Play(Sfx.Kind.Click);
                    btn.GetComponent<Image>().sprite = Art.Get(get() ? "btn_green" : "btn_grey");
                    UI.ButtonLabel(btn).text = get() ? "ON" : "OFF";
                });
                var ic = UI.Icon(row, label == "Sound" ? "sound_on" : "bell", 100, "Ic");
                UI.Place(ic.rectTransform, 0, 0.5f, 90, 0, 100, 100);
                y += 130;
            }
            Toggle("Sound", () => d.sound, v => d.sound = v);
            Toggle("Haptics", () => d.haptics, v => d.haptics = v);

            var about = UI.Label(p.Content, "Word Quest Daily  v" + Application.version, 32, Palette.InkSoft);
            UI.Place(about.rectTransform, 0.5f, 0, 0, 200, 800, 44);
            var priv = UI.Pill(p.Content, "btn_blue", "PRIVACY POLICY", 520, 96, () => Application.OpenURL("https://sid-sys.github.io/wordquestdaily-privacy/"), 40);
            UI.Place((RectTransform)priv.transform, 0.5f, 0, 0, 110, 520, 96);
            var how = UI.Pill(p.Content, "btn_yellow", "HOW TO PLAY", 520, 96, () => { p.Close(); ShowHowTo(); }, 40);
            UI.Place((RectTransform)how.transform, 0.5f, 0, 0, 270, 520, 96);
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

            float tw = 270, th = 300, gx = 24, gy = 24;
            var tiles = new List<RectTransform>();
            for (int i = 0; i < 7; i++)
            {
                int col = i < 4 ? i : i - 4, row = i < 4 ? 0 : 1;
                float rowW = i < 4 ? 4 * tw + 3 * gx : 3 * tw + 2 * gx;
                float x = -rowW / 2 + tw / 2 + col * (tw + gx);
                var tile = UI.Sliced(p.Content, i == 6 ? "card_b" : "card_a", 200, "Day" + (i + 1), false);
                bool current = i == day && can, done = i < day || (i == day && !can);
                if (current) tile.color = new Color(1f, 0.93f, 0.55f);
                if (done) tile.color = new Color(0.8f, 0.95f, 0.8f);
                UI.Place(tile.rectTransform, 0.5f, 1, x, -150 - row * (th + gy) - th / 2, tw, th);
                var head = UI.Label(tile.transform, "DAY " + (i + 1), 32, Palette.Ink);
                UI.Place(head.rectTransform, 0.5f, 1, 0, -34, 240, 40);
                var r = Economy.Daily[i];
                string art = r.Power >= 0 && r.Coins == 0 ? PowerUps.Art[r.Power] : i == 6 ? "chest" : r.Coins >= 250 ? "coin_bag" : r.Coins >= 150 ? "coin_stack" : "coin";
                var ic = UI.Icon(tile.transform, art, 130, "Icon");
                UI.Place(ic.rectTransform, 0.5f, 0.5f, 0, 8, 130, 130);
                var lab = UI.Label(tile.transform, r.Label, 28, Palette.Ink);
                lab.horizontalOverflow = HorizontalWrapMode.Wrap;
                UI.Place(lab.rectTransform, 0.5f, 0, 0, 40, 240, 60);
                if (done) { var ck = UI.Icon(tile.transform, "check", 70, "Done"); UI.Place(ck.rectTransform, 1, 1, -30, -30, 70, 70); }
                if (current) { tiles.Add(tile.rectTransform); tile.rectTransform.localScale = Vector3.one; }
            }
            var claim = UI.Pill(p.Content, can ? "btn_green" : "btn_grey", can ? "CLAIM" : "COME BACK TOMORROW", 620, 120, null, can ? 56 : 38);
            UI.Place((RectTransform)claim.transform, 0.5f, 0, 0, 50, 620, 120);
            claim.onClick.RemoveAllListeners();
            claim.onClick.AddListener(() =>
            {
                if (!Progress.CanClaimDailyReward()) { p.Close(); return; }
                var rw = Progress.ClaimDailyReward();
                Sfx.Play(Sfx.Kind.Coin);
                var from = Vector2.zero;
                Fx.Fly(fxLayer, from, CoinTarget(), rw.Power >= 0 && rw.Coins == 0 ? PowerUps.Art[rw.Power] : "coin", rw.Coins > 0 ? 10 : 1, () => Sfx.Play(Sfx.Kind.Coin, 1f + UnityEngine.Random.value * 0.2f, 0.5f), () => { RefreshCoinPills(); p.Close(); Toast("Reward collected: " + rw.Label, Palette.Yellow); });
                claim.interactable = false;
            });
        }

        // =============== shop (power-ups + free coins) ===============
        public void ShowShop(PowerUp? focus = null, Action onClose = null)
        {
            var p = OpenPopup("SHOP", 980, 1500, true, "ribbon_green", onClose);
            var coins = CoinPill(p.Card, false);
            UI.Place(coins, 0.5f, 1, 0, -170, 300, 82);
            RectTransform content;
            var sr = Scroll(p.Content, out content);
            UI.Stretch((RectTransform)sr.transform, 0, 0, 0, 90);
            BuildShopRows(p, content);
            p.Card.name = "ShopCard";
        }

        void BuildShopRows(Popup p, RectTransform content)
        {
            foreach (Transform c in content) Destroy(c.gameObject);
            var d = SaveSystem.Data;
            float y = 10, w = 880;
            Progress.RollAdDay();

            // Free coins for watching a video
            {
                var row = ListRow(content, ref y, 170, w);
                var ic = UI.Icon(row, "gift", 120, "Gift"); UI.Place(ic.rectTransform, 0, 0.5f, 90, 0, 120, 120);
                var t = UI.Label(row, $"Free {Economy.AdCoins} coins", 42, Palette.Ink, TextAnchor.MiddleLeft);
                UI.Place(t.rectTransform, 0, 0.5f, 400, 28, 420, 56);
                var s = UI.Label(row, $"{Progress.AdCoinClaimsLeft()} left today", 30, Palette.InkSoft, TextAnchor.MiddleLeft);
                UI.Place(s.rectTransform, 0, 0.5f, 400, -28, 420, 44);
                bool ok = Progress.AdCoinClaimsLeft() > 0;
                var b = UI.Pill(row, ok ? "btn_yellow" : "btn_grey", ok ? "WATCH" : "DONE", 220, 96, () =>
                {
                    if (Progress.AdCoinClaimsLeft() <= 0) return;
                    Ads.ShowRewarded(AdPlacement.Coins, done =>
                    {
                        if (!done) { Toast("No ad ready. Try again soon.", Palette.Red); return; }
                        SaveSystem.Data.rewardedCoinAdsToday++;
                        Progress.AddCoins(Economy.AdCoins);
                        Sfx.Play(Sfx.Kind.Coin);
                        Toast($"+{Economy.AdCoins} coins!", Palette.Yellow);
                        if (p.Content != null) BuildShopRows(p, content);
                    });
                }, 38);
                UI.Place((RectTransform)b.transform, 1, 0.5f, -140, 0, 220, 96);
            }

            BuildStoreRows(content, ref y, w, p);

            var head = UI.Label(content, "POWER-UPS", 44, Palette.Ink);
            UI.Place(head.rectTransform, 0.5f, 1, 0, -(y + 40), 800, 60); y += 80;
            foreach (var pu in PowerUps.All)
            {
                var pw = pu; int i = (int)pu;
                var row = ListRow(content, ref y, 190, w);
                var ic = UI.Icon(row, PowerUps.Art[i], 130, "Icon"); UI.Place(ic.rectTransform, 0, 0.5f, 90, 0, 130, 130);
                var n = UI.Label(row, $"{PowerUps.Names[i]}  x{PowerUps.Count(d, pu)}", 40, Palette.Ink, TextAnchor.MiddleLeft);
                UI.Place(n.rectTransform, 0, 0.5f, 430, 36, 500, 52);
                var ds = UI.Label(row, PowerUps.Info[i], 26, Palette.InkSoft, TextAnchor.MiddleLeft);
                UI.Place(ds.rectTransform, 0, 0.5f, 430, -8, 520, 40);
                var mx = UI.Label(row, $"Max {PowerUps.Max[i]}", 24, Palette.InkSoft, TextAnchor.MiddleLeft);
                UI.Place(mx.rectTransform, 0, 0.5f, 430, -44, 520, 34);
                bool full = PowerUps.IsFull(d, pu), afford = d.coins >= PowerUps.Cost[i];
                var b = UI.Pill(row, full ? "btn_grey" : afford ? "btn_green" : "btn_grey", full ? "FULL" : "", 230, 100, () =>
                {
                    if (PowerUps.IsFull(d, pw)) { Toast("You have the maximum", Palette.Yellow); return; }
                    if (!Progress.Spend(PowerUps.Cost[(int)pw])) { Toast("Not enough coins. Play levels or watch an ad!", Palette.Red); Sfx.Play(Sfx.Kind.Error); return; }
                    PowerUps.Add(d, pw); Progress.Notify(); Sfx.Play(Sfx.Kind.Coin);
                    BuildShopRows(p, content);
                    RefreshCoinPills();
                }, 44);
                UI.Place((RectTransform)b.transform, 1, 0.5f, -140, 0, 230, 100);
                if (!full)
                {
                    var c = UI.Icon(b.transform, "coin", 60, "C"); UI.Place(c.rectTransform, 0, 0.5f, 46, 3, 60, 60);
                    var pr = UI.Label(b.transform, PowerUps.Cost[i].ToString(), 42, Color.white, TextAnchor.MiddleCenter, true);
                    UI.Place(pr.rectTransform, 0.5f, 0.5f, 26, 3, 130, 60);
                    UI.ButtonLabel(b).text = "";
                    UI.ButtonLabel(b).gameObject.SetActive(false);
                }
            }
            EndScroll(content, y);
        }

        partial void BuildStoreRows(RectTransform content, ref float y, float w, Popup p);
    }
}
