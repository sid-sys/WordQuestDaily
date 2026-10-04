using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>The shop: bundle cards (fixed contents), Remove Ads, free coins for an ad, power-ups for coins. Used as a tab and as a popup.</summary>
    public partial class GameApp
    {
        // =============== entry points ===============
        public void ShowShop(PowerUp? focus = null, Action onClose = null)
        {
            var p = OpenPopup("SHOP", 1000, 1560, true, "ribbon_green", onClose);
            var coins = CoinPill(p.Card, false);
            UI.Place(coins, 0.5f, 1, 0, -184, 320, 92);
            RectTransform content;
            var sr = Scroll(p.Content, out content);
            var srt = (RectTransform)sr.transform; srt.offsetMin = Vector2.zero; srt.offsetMax = new Vector2(0, -90);
            BuildShopContent(content, 920);
        }

        void BuildShopTab(RectTransform b)
        {
            var title = UI.Label(b, "Shop", 96, Color.white, TextAnchor.MiddleCenter, true);
            title.GetComponent<Outline>().effectColor = new Color32(0x12, 0x4A, 0xA8, 255); title.GetComponent<Outline>().effectDistance = new Vector2(5, -5);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1); title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(120, -22); title.rectTransform.sizeDelta = new Vector2(500, 120);
            var cp = CoinPill(b);
            cp.anchorMin = cp.anchorMax = new Vector2(0, 1); cp.pivot = new Vector2(0.5f, 1); cp.anchoredPosition = new Vector2(20 + 160, -30); cp.sizeDelta = new Vector2(320, 92);
            var sg = UI.GlyphButton(b, "g_gear", new Color32(0xE0, 0x62, 0x4A, 255), 90, "Gear", 0.6f);
            UI.Place(sg.rectTransform, 1, 1, -70, -76, 90, 90); UI.Click(sg, ShowSettings);
            RectTransform content;
            var sr = Scroll(b, out content);
            ((RectTransform)sr.transform).offsetMax = new Vector2(0, -150);
            BuildShopContent(content, 980);
        }

        // =============== content ===============
        void BuildShopContent(RectTransform content, float w)
        {
            Action rebuild = null;
            rebuild = () => { foreach (Transform c in content) Destroy(c.gameObject); BuildShopContent(content, w); };
            Iap.Reconnect();
            var d = SaveSystem.Data;
            Progress.RollAdDay();
            float y = 14;

            // ---- bundles ----
            string[] frames = { "bund_orange", "bund_red", "bund_blue", "bund_purple" };
            for (int i = 0; i < Bundles.All.Length; i++) BundleCard(content, ref y, Bundles.All[i], frames[i % frames.Length], w);

            // ---- Remove Ads ----
            if (!d.adsRemoved)
            {
                var row = ListRow(content, ref y, 200, w);
                var ic = UI.Icon(row, "noads_badge", 130, "NoAds"); UI.Place(ic.rectTransform, 0, 0.5f, 100, 0, 130, 130);
                var t = UI.Label(row, "Remove Ads", 46, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(t.rectTransform, 0, 0.5f, 190, 34, 400, 60);
                var s = UI.Label(row, "No more forced ads", 28, Palette.InkSoft, TextAnchor.MiddleLeft); UI.PlaceL(s.rectTransform, 0, 0.5f, 190, -22, 400, 40);
                var b = UI.Pill(row, "btn_green", Iap.Price(Iap.RemoveAds), 250, 96, ShowAdFree, 40);
                UI.Place((RectTransform)b.transform, 1, 0.5f, -160, 0, 250, 96);
            }

            // ---- free coins for a video ----
            {
                var row = ListRow(content, ref y, 190, w);
                var ic = UI.Icon(row, "gift", 130, "Gift"); UI.Place(ic.rectTransform, 0, 0.5f, 100, 0, 130, 130);
                var t = UI.Label(row, $"Free {Economy.AdCoins} coins", 46, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(t.rectTransform, 0, 0.5f, 190, 30, 420, 56);
                var s = UI.Label(row, $"{Progress.AdCoinClaimsLeft()} left today", 30, Palette.InkSoft, TextAnchor.MiddleLeft); UI.PlaceL(s.rectTransform, 0, 0.5f, 190, -26, 420, 42);
                bool ok = Progress.AdCoinClaimsLeft() > 0;
                var b = UI.Pill(row, ok ? "btn_yellow" : "btn_grey", ok ? "WATCH" : "DONE", 250, 96, () =>
                {
                    if (Progress.AdCoinClaimsLeft() <= 0) return;
                    Ads.ShowRewarded(AdPlacement.Coins, done =>
                    {
                        if (!done) { Toast("No ad ready. Try again soon.", Palette.Red); return; }
                        SaveSystem.Data.rewardedCoinAdsToday++;
                        RewardCoins(Economy.AdCoins);
                        Sfx.Play(Sfx.Kind.Coin);
                        rebuild();
                    });
                }, 40);
                UI.Place((RectTransform)b.transform, 1, 0.5f, -160, 0, 250, 96);
            }

            // ---- power-ups for coins ----
            var head = UI.Label(content, "POWER-UPS", 52, Color.white, TextAnchor.MiddleCenter, true);
            head.GetComponent<Outline>().effectColor = new Color32(0x12, 0x4A, 0xA8, 255);
            UI.Place(head.rectTransform, 0.5f, 1, 0, -(y + 46), 800, 70); y += 96;
            foreach (var pu in PowerUps.All)
            {
                var pw = pu; int i = (int)pu;
                var row = ListRow(content, ref y, 190, w);
                var ic = UI.Icon(row, PowerUps.Art[i], 130, "Icon"); UI.Place(ic.rectTransform, 0, 0.5f, 100, 0, 130, 130);
                var n = UI.Label(row, $"{PowerUps.Names[i]}  x{PowerUps.Count(d, pu)}", 42, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(n.rectTransform, 0, 0.5f, 185, 40, 430, 52);
                var ds = UI.Label(row, PowerUps.Info[i], 26, Palette.InkSoft, TextAnchor.MiddleLeft); UI.PlaceL(ds.rectTransform, 0, 0.5f, 185, -4, 430, 38);
                var mx = UI.Label(row, $"Max {PowerUps.Max[i]}", 24, Palette.InkSoft, TextAnchor.MiddleLeft); UI.PlaceL(mx.rectTransform, 0, 0.5f, 185, -42, 430, 34);
                bool full = PowerUps.IsFull(d, pu), afford = d.coins >= PowerUps.Cost[i];
                var bt = UI.Pill(row, full ? "btn_grey" : afford ? "btn_green" : "btn_grey", full ? "FULL" : "", 250, 100, () =>
                {
                    if (PowerUps.IsFull(d, pw)) { Toast("You have the maximum", Palette.Yellow); return; }
                    if (!Progress.Spend(PowerUps.Cost[(int)pw])) { Toast("Not enough coins. Play levels or watch an ad!", Palette.Red); Sfx.Play(Sfx.Kind.Error); return; }
                    PowerUps.Add(d, pw); Progress.Notify(); Sfx.Play(Sfx.Kind.Coin);
                    rebuild();
                }, 44);
                UI.Place((RectTransform)bt.transform, 1, 0.5f, -160, 0, 250, 100);
                if (!full)
                {
                    var c = UI.Icon(bt.transform, "coin", 60, "C"); UI.Place(c.rectTransform, 0, 0.5f, 56, 3, 60, 60);
                    var pr = UI.Label(bt.transform, PowerUps.Cost[i].ToString(), 42, Color.white, TextAnchor.MiddleCenter, true);
                    pr.GetComponent<Outline>().effectColor = UI.PillOutline("btn_green");
                    UI.Place(pr.rectTransform, 0.5f, 0.5f, 28, 3, 130, 60);
                    UI.ButtonLabel(bt).gameObject.SetActive(false);
                }
            }

            var restore = UI.Pill(content, "btn_blue", "RESTORE PURCHASES", 520, 80, () => Iap.Restore(msg => Toast(msg, Palette.Yellow)), 32);
            UI.Place((RectTransform)restore.transform, 0.5f, 1, 0, -(y + 56), 520, 80); y += 130;
            EndScroll(content, y);
        }

        /// <summary>One bundle card: coin pile + amount on the left, power-up icons in the middle, name strip and price button at the bottom.</summary>
        void BundleCard(RectTransform content, ref float y, Bundle bd, string frame, float w)
        {
            const float h = 380;
            var row = RowAt(content, ref y, h + 22, w);
            var card = UI.Sliced(row, frame, h, "Bundle_" + bd.Id, false);
            UI.Place(card.rectTransform, 0.5f, 1, 0, -h / 2 - 8, w, h);
            // coin pile + amount
            var pile = UI.Icon(card.transform, bd.Art, 170, "Pile"); UI.Place(pile.rectTransform, 0, 1, 130, -104, 200, 170);
            var amount = UI.Label(card.transform, bd.Coins.ToString("N0"), 58, Color.white, TextAnchor.MiddleCenter, true);
            amount.GetComponent<Outline>().effectColor = new Color32(0x4A, 0x24, 0x08, 255);
            UI.Place(amount.rectTransform, 0, 1, 130, -216, 250, 66);
            // power-ups inset
            var inset = UI.Sliced(card.transform, "card_inset", 190, "Inset");
            UI.Place(inset.rectTransform, 1, 1, -(w - 270 - 24) / 2 - 24, -128, w - 270 - 24, 208);
            int shown = 0; for (int i = 0; i < 5; i++) if (bd.Powers[i] > 0) shown++;
            float cell = (w - 270 - 24 - 20) / Mathf.Max(3, shown), x0 = -(shown - 1) * cell / 2f;
            int k = 0;
            for (int i = 0; i < 5; i++)
            {
                if (bd.Powers[i] <= 0) continue;
                var ic = UI.Icon(inset.transform, PowerUps.Art[i], 92, "P" + i); UI.Place(ic.rectTransform, 0.5f, 0.5f, x0 + k * cell, 36, 92, 92);
                var cnt = UI.Label(inset.transform, "x" + bd.Powers[i], 38, new Color32(0x6B, 0x3A, 0x0E, 255), TextAnchor.MiddleCenter);
                UI.Place(cnt.rectTransform, 0.5f, 0.5f, x0 + k * cell, -34, 110, 44);
                k++;
            }
            if (bd.ExclusiveEffect >= 0)
            {
                var ex = UI.Label(inset.transform, "+ exclusive " + Economy.EffectNames[bd.ExclusiveEffect] + " effect", 24, new Color32(0x7A, 0x2E, 0xC0, 255), TextAnchor.MiddleCenter);
                UI.Place(ex.rectTransform, 0.5f, 0.5f, 0, -80, 420, 30);
            }
            // footer: name + price
            var strip = UI.Sliced(card.transform, bd.Strip, 92, "Strip");
            strip.rectTransform.anchorMin = new Vector2(0, 0); strip.rectTransform.anchorMax = new Vector2(1, 0); strip.rectTransform.pivot = new Vector2(0.5f, 0);
            strip.rectTransform.offsetMin = new Vector2(18, 16); strip.rectTransform.offsetMax = new Vector2(-18, 108);
            var nm = UI.Label(strip.transform, bd.Name, 44, Color.white, TextAnchor.MiddleLeft, true);
            nm.GetComponent<Outline>().effectColor = UI.PillOutline(bd.Strip);
            UI.PlaceL(nm.rectTransform, 0, 0.5f, 44, 0, 460, 60);
            string id = bd.Id;
            var buy = UI.Pill(strip.transform, "btn_green", Iap.Price(id), 300, 70, () => Iap.Buy(id, msg => { if (msg != null) Toast(msg, Palette.Red); }), 38);
            UI.Place((RectTransform)buy.transform, 1, 0.5f, -180, 0, 300, 70);
            if (!string.IsNullOrEmpty(bd.Ribbon))
            {
                var tag = UI.Sliced(card.transform, "btn_red", 52, "Tag");
                UI.Place(tag.rectTransform, 0, 1, 130, 6, 260, 52);
                var tl = UI.Label(tag.transform, bd.Ribbon, 26, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(tl.rectTransform, 0, 2, 0, 0);
                tl.GetComponent<Outline>().effectColor = UI.PillOutline("btn_red");
            }
        }

        // =============== a purchase just arrived ===============
        public void BundleDelivered(Bundle b, bool firstKitOnly)
        {
            HoldCoins(1.8f);
            Progress.GrantBundle(b, firstKitOnly);
            Sfx.Play(Sfx.Kind.Level);
            Fx.Fly(fxLayer, Vector2.zero, CoinTarget(), "coin", 12, () => Sfx.Play(Sfx.Kind.Coin, 1f + UnityEngine.Random.value * 0.3f, 0.4f), null, 64f, 0.95f, 0.05f);
            if (b.Coins > 0) Fx.FloatText(fxLayer, new Vector2(0, 120), "+" + b.Coins.ToString("N0") + " coins", Palette.Yellow, 60);
        }

        // =============== Remove Ads (image 4 style) ===============
        public void ShowAdFree()
        {
            var d = SaveSystem.Data;
            var p = OpenPopup("AD-FREE PACK!", 980, 1500, true, "ribbon_purple");
            var c = p.Content;
            var mascot = UI.Icon(c, "mascot", 400, "Mascot"); UI.Place(mascot.rectTransform, 0.5f, 1, 0, -250, 400, 400);
            var l1 = UI.Label(c, "Remove forced ads forever!", 48, Palette.Ink); UI.Place(l1.rectTransform, 0.5f, 1, 0, -520, 860, 64);
            var l2 = UI.Label(c, "You can still watch optional ads for rewards.", 28, Palette.InkSoft); UI.Place(l2.rectTransform, 0.5f, 1, 0, -580, 860, 40);

            var panel = UI.Round(c, new Color32(0x6E, 0x3F, 0xD0, 255), 40, "Offers");
            panel.rectTransform.anchorMin = new Vector2(0, 0); panel.rectTransform.anchorMax = new Vector2(1, 0); panel.rectTransform.pivot = new Vector2(0.5f, 0);
            panel.rectTransform.offsetMin = new Vector2(10, 10); panel.rectTransform.offsetMax = new Vector2(-10, 720);
            // left: ads only
            var left = UI.Sliced(panel.transform, "card_blue", 470, "Only");
            left.rectTransform.anchorMin = left.rectTransform.anchorMax = new Vector2(0, 1); left.rectTransform.pivot = new Vector2(0, 1);
            left.rectTransform.anchoredPosition = new Vector2(24, -24); left.rectTransform.sizeDelta = new Vector2(380, 560);
            var badge = UI.Icon(left.transform, "noads_badge", 250, "Badge"); UI.Place(badge.rectTransform, 0.5f, 1, 0, -170, 250, 250);
            var lt = UI.Label(left.transform, "No ads", 44, Palette.Ink); UI.Place(lt.rectTransform, 0.5f, 0, 0, 190, 360, 56);
            var lb = UI.Pill(left.transform, "btn_green", Iap.Price(Iap.RemoveAds), 330, 100, () => { Iap.Buy(Iap.RemoveAds, m => { if (m != null) Toast(m, Palette.Red); }); p.Close(); }, 44);
            UI.Place((RectTransform)lb.transform, 0.5f, 0, 0, 56, 330, 100);
            // right: ads + kit
            var right = UI.Sliced(panel.transform, "bund_orange", 470, "Plus");
            right.rectTransform.anchorMin = right.rectTransform.anchorMax = new Vector2(1, 1); right.rectTransform.pivot = new Vector2(1, 1);
            right.rectTransform.anchoredPosition = new Vector2(-24, -24); right.rectTransform.sizeDelta = new Vector2(470, 560);
            var tag = UI.Sliced(right.transform, "btn_red", 52, "Tag"); UI.Place(tag.rectTransform, 0.5f, 1, 0, 8, 240, 52);
            var tg = UI.Label(tag.transform, "BEST VALUE", 26, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(tg.rectTransform, 0, 2, 0, 0); tg.GetComponent<Outline>().effectColor = UI.PillOutline("btn_red");
            var b2 = UI.Icon(right.transform, "noads_badge", 130, "B2"); UI.Place(b2.rectTransform, 0, 1, 100, -120, 130, 130);
            var plus = UI.Label(right.transform, "+", 70, new Color32(0x6B, 0x3A, 0x0E, 255)); UI.Place(plus.rectTransform, 0.5f, 1, -6, -120, 60, 70);
            var cn = UI.Icon(right.transform, "coin_stack", 120, "Coins"); UI.Place(cn.rectTransform, 1, 1, -100, -120, 120, 120);
            var kit = Bundles.AdFreeKit;
            var ct = UI.Label(right.transform, kit.Coins.ToString("N0") + " coins", 38, new Color32(0x6B, 0x3A, 0x0E, 255)); UI.Place(ct.rectTransform, 0.5f, 1, 0, -230, 440, 50);
            int shown = 0; for (int i = 0; i < 5; i++) if (kit.Powers[i] > 0) shown++;
            float cell = 100f, x0 = -(shown - 1) * cell / 2f; int k = 0;
            for (int i = 0; i < 5; i++)
            {
                if (kit.Powers[i] <= 0) continue;
                var ic = UI.Icon(right.transform, PowerUps.Art[i], 70, "P" + i); UI.Place(ic.rectTransform, 0.5f, 1, x0 + k * cell, -312, 70, 70);
                var cc = UI.Label(right.transform, "x" + kit.Powers[i], 30, new Color32(0x6B, 0x3A, 0x0E, 255)); UI.Place(cc.rectTransform, 0.5f, 1, x0 + k * cell, -366, 90, 36);
                k++;
            }
            var rb = UI.Pill(right.transform, "btn_green", Iap.Price(Bundles.AdFreePlus), 360, 100, () => { Iap.Buy(Bundles.AdFreePlus, m => { if (m != null) Toast(m, Palette.Red); }); p.Close(); }, 44);
            UI.Place((RectTransform)rb.transform, 0.5f, 0, 0, 56, 360, 100);
            var foot = UI.Label(panel.transform, "One-time purchase. Restore it any time from the Shop.", 26, Color.white, TextAnchor.MiddleCenter, true);
            foot.rectTransform.anchorMin = foot.rectTransform.anchorMax = new Vector2(0.5f, 0); foot.rectTransform.anchoredPosition = new Vector2(0, 52); foot.rectTransform.sizeDelta = new Vector2(860, 40);
        }
    }
}
