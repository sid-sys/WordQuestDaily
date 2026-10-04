using System;
using System.Collections.Generic;
using UnityEngine;
#if WQ_IAP
using UnityEngine.Purchasing;
#endif

namespace WordQuest
{
    /// <summary>
    /// Real-money purchases (Google Play).
    ///   remove_ads        non-consumable   removes full-screen ads
    ///   remove_ads_plus   non-consumable   removes ads + a small kit (given once)
    ///   bundle_starter / bundle_player / bundle_power / bundle_mega   consumable bundles with FIXED contents (see Bundles in Economy.cs)
    /// Product ids must match Play Console. In the Editor a fake store grants purchases instantly for testing.
    /// </summary>
    public static class Iap
    {
        public const string RemoveAds = "remove_ads";
        public static string[] BundleIds => Array.ConvertAll(Bundles.All, b => b.Id);

        public static string Status = "off";
        public static event Action PricesChanged;
        public static event Action<string> Delivered;      // product id

        static readonly Dictionary<string, string> FakePrices = new Dictionary<string, string>
        {
            { "bundle_starter", "Rs 99" }, { "bundle_player", "Rs 299" }, { "bundle_power", "Rs 599" }, { "bundle_mega", "Rs 999" },
            { "remove_ads", "Rs 199" }, { "remove_ads_plus", "Rs 399" },
        };

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
                var defs = new List<ProductDefinition>
                {
                    new ProductDefinition(RemoveAds, ProductType.NonConsumable),
                    new ProductDefinition(Bundles.AdFreePlus, ProductType.NonConsumable),
                };
                foreach (var id in BundleIds) defs.Add(new ProductDefinition(id, ProductType.Consumable));
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
            foreach (var item in cart.Items()) Deliver(item.Product.definition.id, fresh);
        }

        public static string PriceOf(string id) => products.TryGetValue(id, out var p) ? p.metadata.localizedPriceString : "...";

        public static void Buy(string id, Action<string> problem)
        {
            if (Application.isEditor) { Deliver(id, true); return; }
            if (!products.TryGetValue(id, out var p)) { problem?.Invoke("Store not ready. Check your internet and try again."); Reconnect(); return; }
            pendingProblem = problem;
            store.PurchaseProduct(p);
        }

        public static void Restore(Action<string> message)
        {
            if (store == null) { message?.Invoke("Store not ready"); return; }
            store.RestoreTransactions((ok, err) => message?.Invoke(ok ? "Purchases restored" : "Could not restore: " + err));
        }

        public static string Price(string id) => Application.isEditor ? FakePrices[id] : PriceOf(id);
#else
        public static void Init() { Status = "purchasing not installed"; }
        public static void Reconnect() { }
        public static void Buy(string id, Action<string> problem) { if (Application.isEditor) Deliver(id, true); else problem?.Invoke("Store not ready"); }
        public static void Restore(Action<string> message) { message?.Invoke("Store not ready"); }
        public static string Price(string id) => FakePrices.TryGetValue(id, out var p) ? p : "";
#endif

        /// <summary>Gives what a product contains. `fresh` = a new purchase (consumables are only given for new purchases).</summary>
        static void Deliver(string id, bool fresh)
        {
            var d = SaveSystem.Data;
            if (id == RemoveAds)
            {
                if (!d.adsRemoved) { d.adsRemoved = true; Progress.Notify(); Delivered?.Invoke(id); }
            }
            else if (id == Bundles.AdFreePlus)
            {
                bool first = !d.adsRemoved || !d.adFreeKitClaimed;
                d.adsRemoved = true;
                if (!d.adFreeKitClaimed) { GameApp.I.BundleDelivered(Bundles.AdFreeKit, true); }
                else Progress.Notify();
                if (first) Delivered?.Invoke(id);
            }
            else if (fresh)
            {
                var b = Bundles.Find(id);
                if (b != null) { GameApp.I.BundleDelivered(b, false); Delivered?.Invoke(id); }
            }
        }
    }
}
