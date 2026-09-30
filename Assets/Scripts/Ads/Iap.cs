using System;
using System.Collections.Generic;
using UnityEngine;
#if WQ_IAP
using UnityEngine.Purchasing;
#endif

namespace WordQuest
{
    /// <summary>
    /// Real-money purchases (Google Play): Remove Ads and three coin bags.
    /// Product ids must match the ones in Play Console. In the Editor a fake store grants purchases instantly for testing.
    /// </summary>
    public static class Iap
    {
        public const string RemoveAds = "remove_ads";
        public static readonly string[] CoinPacks = { "coins_small", "coins_medium", "coins_large" };
        static readonly int[] PackCoins = { 500, 1500, 4000 };
        static readonly string[] FakePrices = { "$0.99", "$2.99", "$6.99" };

        public static string Status = "off";
        public static event Action PricesChanged;
        public static event Action Delivered;

        public static int CoinsIn(string id) { int i = Array.IndexOf(CoinPacks, id); return i >= 0 ? PackCoins[i] : 0; }

#if WQ_IAP
        static StoreController store;
        static readonly Dictionary<string, Product> products = new Dictionary<string, Product>();
        static Action<string> pendingProblem;
        static bool connecting;

        public static async void Init()
        {
            if (Application.isEditor) { Status = "editor fake store"; return; }
            if (store != null || connecting) return;
            connecting = true;
            try
            {
                store = UnityIAPServices.StoreController();
                store.OnPurchasePending += OnPending;
                store.OnPurchaseFailed += OnFailed;
                store.OnProductsFetched += OnFetched;
                store.OnProductsFetchFailed += f => Status = "products failed: " + f.FailureReason;
                store.OnPurchasesFetched += OnPurchasesFetched;
                store.OnStoreDisconnected += d => { Status = "store disconnected"; };
                Status = "connecting";
                await store.Connect();
                Status = "loading products";
                var defs = new List<ProductDefinition> { new ProductDefinition(RemoveAds, ProductType.NonConsumable) };
                foreach (var id in CoinPacks) defs.Add(new ProductDefinition(id, ProductType.Consumable));
                store.FetchProducts(defs);
            }
            catch (Exception e) { Status = "store error: " + e.Message; store = null; }
            connecting = false;
        }

        /// <summary>Try again if the first attempt failed (call when the shop opens).</summary>
        public static void Reconnect() { if (store == null && !connecting) Init(); }

        static void OnFetched(List<Product> list)
        {
            foreach (var p in list) products[p.definition.id] = p;
            Status = "ready (" + products.Count + " products)";
            PricesChanged?.Invoke();
            store.FetchPurchases();   // finds an earlier Remove Ads purchase after a reinstall
        }

        static void OnPurchasesFetched(Orders orders)
        {
            foreach (var o in orders.ConfirmedOrders) Grant(o.CartOrdered, false);
            foreach (var o in orders.PendingOrders) { Grant(o.CartOrdered, true); store.ConfirmPurchase(o); }
        }

        static void OnPending(PendingOrder order)
        {
            Grant(order.CartOrdered, true);
            store.ConfirmPurchase(order);
        }

        static void OnFailed(FailedOrder order)
        {
            if (order.FailureReason == PurchaseFailureReason.UserCancelled) { pendingProblem?.Invoke(null); return; }
            pendingProblem?.Invoke("Purchase did not work: " + order.FailureReason);
        }

        static void Grant(ICart cart, bool fresh)
        {
            var d = SaveSystem.Data;
            foreach (var item in cart.Items())
            {
                string id = item.Product.definition.id;
                if (id == RemoveAds) { if (!d.adsRemoved) { d.adsRemoved = true; Progress.Notify(); Delivered?.Invoke(); } }
                else if (fresh && Array.IndexOf(CoinPacks, id) >= 0) { Progress.AddCoins(CoinsIn(id)); Delivered?.Invoke(); }
            }
        }

        public static string PriceOf(string id) => products.TryGetValue(id, out var p) ? p.metadata.localizedPriceString : "...";

        public static void Buy(string id, Action<string> problem)
        {
            if (Application.isEditor) { FakeBuy(id); return; }
            if (!products.TryGetValue(id, out var p)) { problem?.Invoke("Store not ready. Check your internet and try again."); Reconnect(); return; }
            pendingProblem = problem;
            store.PurchaseProduct(p);
        }

        public static void Restore(Action<string> message)
        {
            if (store == null) { message?.Invoke("Store not ready"); return; }
            store.RestoreTransactions((ok, err) => message?.Invoke(ok ? "Purchases restored" : "Could not restore: " + err));
        }
#else
        public static void Init() { Status = "purchasing not installed"; }
        public static void Reconnect() { }
        public static string PriceOf(string id) { int i = Array.IndexOf(CoinPacks, id); return i >= 0 ? FakePrices[i] : "$2.99"; }
        public static void Buy(string id, Action<string> problem) { if (Application.isEditor) FakeBuy(id); else problem?.Invoke("Store not ready"); }
        public static void Restore(Action<string> message) { message?.Invoke("Store not ready"); }
#endif

#if WQ_IAP
        static string FakePrice(string id) { int i = Array.IndexOf(CoinPacks, id); return i >= 0 ? FakePrices[i] : "$2.99"; }
#endif

        static void FakeBuy(string id)
        {
            if (id == RemoveAds) { SaveSystem.Data.adsRemoved = true; Progress.Notify(); }
            else Progress.AddCoins(CoinsIn(id));
            Delivered?.Invoke();
        }

        /// <summary>Price label used by the shop. The Editor shows fake prices.</summary>
        public static string Price(string id)
        {
#if WQ_IAP
            return Application.isEditor ? FakePrice(id) : PriceOf(id);
#else
            return PriceOf(id);
#endif
        }
    }
}
