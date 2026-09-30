using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace WordQuest.EditorTools
{
    /// <summary>Turns the WQ_ADMOB / WQ_IAP switches on when the AdMob plugin / Unity IAP are installed (and off if they are removed).</summary>
    [InitializeOnLoad]
    public static class AdMobDefine
    {
        static AdMobDefine() { EditorApplication.delayCall += Apply; }

        static bool HasType(string name) => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType(name, false) != null);

        static void Apply()
        {
            bool ads = HasType("GoogleMobileAds.Api.MobileAds");
            bool iap = HasType("UnityEngine.Purchasing.StoreController");
            foreach (var t in new[] { NamedBuildTarget.Android, NamedBuildTarget.Standalone })
            {
                var list = PlayerSettings.GetScriptingDefineSymbols(t).Split(';').Where(s => s.Length > 0).ToList();
                bool changed = false;
                changed |= Toggle(list, "WQ_ADMOB", ads);
                changed |= Toggle(list, "WQ_IAP", iap);
                if (changed) PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", list));
            }
        }

        static bool Toggle(System.Collections.Generic.List<string> list, string sym, bool on)
        {
            if (on && !list.Contains(sym)) { list.Add(sym); return true; }
            if (!on && list.Contains(sym)) { list.Remove(sym); return true; }
            return false;
        }
    }
}
