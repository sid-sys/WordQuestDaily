using System;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        /// <summary>Real-money rows of the shop: three coin bags, Remove Ads and Restore Purchases.</summary>
        partial void BuildStoreRows(RectTransform content, ref float y, float w, Popup p)
        {
            var d = SaveSystem.Data;
            Iap.Reconnect();

            var head = UI.Label(content, "COIN BAGS", 44, Palette.Ink);
            UI.Place(head.rectTransform, 0.5f, 1, 0, -(y + 40), 800, 60); y += 76;

            string[] art = { "coin", "coin_stack", "coin_bag" };
            float cw = 272, ch = 350, gap = 20;
            var row = RowAt(content, ref y, ch, w);
            for (int i = 0; i < Iap.CoinPacks.Length; i++)
            {
                string id = Iap.CoinPacks[i];
                var card = UI.Sliced(row, "card_a", ch, "Bag_" + id, false);
                UI.Place(card.rectTransform, 0.5f, 0.5f, (i - 1) * (cw + gap), 0, cw, ch);
                var pic = UI.Icon(card.transform, art[i], 120 + i * 14, "Coins");
                UI.Place(pic.rectTransform, 0.5f, 1, 0, -92, 120 + i * 14, 120 + i * 14);
                var amount = UI.Label(card.transform, Iap.CoinsIn(id).ToString("N0"), 46, Palette.Orange);
                UI.Place(amount.rectTransform, 0.5f, 1, 0, -196, 230, 56);
                var word = UI.Label(card.transform, "coins", 28, Palette.InkSoft);
                UI.Place(word.rectTransform, 0.5f, 1, 0, -236, 230, 36);
                var buy = UI.Pill(card.transform, "btn_green", Iap.Price(id), 210, 72, () => Iap.Buy(id, msg => { if (msg != null) Toast(msg, Palette.Red); }), 36);
                UI.Place((RectTransform)buy.transform, 0.5f, 0, 0, 52, 210, 72);
            }
            y += 14;

            if (!d.adsRemoved)
            {
                var r = ListRow(content, ref y, 170, w);
                var ic = UI.Icon(r, "trophy", 110, "NoAds"); UI.Place(ic.rectTransform, 0, 0.5f, 90, 0, 110, 110);
                var t = UI.Label(r, "Remove Ads", 42, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(t.rectTransform, 0, 0.5f, 170, 26, 430, 56);
                var s = UI.Label(r, "No more full-screen ads", 28, Palette.InkSoft, TextAnchor.MiddleLeft); UI.PlaceL(s.rectTransform, 0, 0.5f, 170, -26, 430, 40);
                var b = UI.Pill(r, "btn_yellow", Iap.Price(Iap.RemoveAds), 210, 92, () => Iap.Buy(Iap.RemoveAds, msg => { if (msg != null) Toast(msg, Palette.Red); }), 38);
                UI.Place((RectTransform)b.transform, 1, 0.5f, -130, 0, 210, 92);
            }

            var restore = UI.Pill(content, "btn_blue", "RESTORE PURCHASES", 480, 76, () => Iap.Restore(msg => Toast(msg, Palette.Yellow)), 32);
            UI.Place((RectTransform)restore.transform, 0.5f, 1, 0, -(y + 50), 480, 76);
            y += 110;
        }
    }
}
