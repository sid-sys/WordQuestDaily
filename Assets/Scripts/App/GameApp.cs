using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace WordQuest
{
    public enum Tab { Home, Levels, Collection, Profile }

    /// <summary>
    /// The whole app. Everything is built from code at start-up, so no scene setup is needed.
    /// This file: bootstrap, canvas layers, tabs, popups, toasts. Screens live in the other partial files.
    /// </summary>
    public partial class GameApp : MonoBehaviour
    {
        public static GameApp I;

        Canvas canvas;
        RectTransform canvasRt, safe, bgLayer, body, tabBar, popupLayer, toastLayer, fxLayer, gameLayer;
        Image bgImage;
        Tab tab = Tab.Home;
        int lastTabIndex = 0;
        public RectTransform FxLayer => fxLayer;
        readonly List<Popup> popups = new List<Popup>();
        GameScreen game;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("WordQuest");
            DontDestroyOnLoad(go);
            go.AddComponent<Fx>();
            go.AddComponent<AudioSource>();
            go.AddComponent<GameApp>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SaveSystem.Load();
            Sfx.Init(GetComponent<AudioSource>());
            BuildCanvas();
        }

        void Start()
        {
            Progress.EnsureMissionsToday();
            SetBackground(SaveSystem.Data.theme);
            ShowSplash(() =>
            {
                ShowTab(Tab.Home);
                var week = League.SettleIfNeeded();
                if (week != null) StartCoroutine(AfterFrames(2, () => ShowLeagueResult(week)));
                else if (Progress.CanClaimDailyReward()) StartCoroutine(AfterFrames(2, ShowDailyReward));
            });
            Progress.Changed += OnProgressChanged;
            Iap.Init();
            Iap.Delivered += () => { RefreshCoinPills(); Toast("Thank you! Purchase complete.", Palette.Green); Sfx.Play(Sfx.Kind.Coin); };
#if WQ_ADMOB
            AdMobService.Init();
#endif
        }

        IEnumerator AfterFrames(int n, Action a) { for (int i = 0; i < n; i++) yield return null; a?.Invoke(); }

        void OnDestroy() { Progress.Changed -= OnProgressChanged; }

        void OnApplicationPause(bool paused) { if (paused) SaveSystem.Save(); }
        void OnApplicationQuit() { SaveSystem.Save(); }

        // Refresh the visible screen when coins etc. change (not while playing)
        void OnProgressChanged() { RefreshCoinPills(); }

        // =============== canvas ===============
        void BuildCanvas()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(transform, false);

            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var sc = cgo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 0f;
            canvasRt = (RectTransform)cgo.transform;

            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Palette.Navy; }

            bgLayer = UI.Stretch(UI.Node(canvasRt, "Background"));
            bgImage = UI.Node(bgLayer, "Painting").gameObject.AddComponent<Image>();
            UI.Stretch(bgImage.rectTransform);
            var fit = bgImage.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1024f / 1536f;
            bgImage.raycastTarget = false;

            safe = UI.Stretch(UI.Node(canvasRt, "SafeArea"));
            ApplySafeArea();
            body = UI.Stretch(UI.Node(safe, "Body"), 0, 190, 0, 0);
            gameLayer = UI.Stretch(UI.Node(safe, "Game"));
            tabBar = UI.Node(safe, "TabBar");
            popupLayer = UI.Stretch(UI.Node(canvasRt, "Popups"));
            toastLayer = UI.Stretch(UI.Node(canvasRt, "Toasts"));
            fxLayer = UI.Stretch(UI.Node(canvasRt, "Fx"));
        }

        void ApplySafeArea()
        {
            var r = Screen.safeArea;
            var min = r.position; var max = r.position + r.size;
            min.x /= Screen.width; min.y /= Screen.height; max.x /= Screen.width; max.y /= Screen.height;
            safe.anchorMin = min; safe.anchorMax = max; safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        // =============== background ===============
        static readonly string[] ThemeKeys = { "meadow", "ocean", "candy", "night" };
        public static string ThemeKey(int i) => ThemeKeys[Mathf.Clamp(i, 0, 3)];
        public void SetBackground(int theme)
        {
            var s = Art.Get("bg_" + ThemeKey(theme));
            bgImage.sprite = s;
            bgImage.color = Color.white;
        }

        // =============== tabs ===============
        static readonly string[] TabNames = { "Home", "Levels", "Collection", "Profile" };
        static readonly string[] TabArt = { "tab_home", "tab_levels", "tab_collection", "tab_profile" };
        RectTransform tabGlow; readonly List<RectTransform> tabIcons = new List<RectTransform>();
        readonly List<Text> tabTexts = new List<Text>();
        readonly List<Image> tabImgs = new List<Image>();
        Image tabBadge;

        public void ShowTab(Tab t)
        {
            int old = (int)tab;
            tab = t;
            bool first = tabBar.childCount == 0;
            if (first) BuildTabBar();
            gameLayer.gameObject.SetActive(false);
            body.gameObject.SetActive(true); tabBar.gameObject.SetActive(true);
            foreach (Transform c in body) Destroy(c.gameObject);
            coinDisplays.Clear();
            SetBackground(SaveSystem.Data.theme);
            switch (t)
            {
                case Tab.Home: BuildHome(body); break;
                case Tab.Levels: BuildLevels(body); break;
                case Tab.Collection: BuildCollection(body); break;
                case Tab.Profile: BuildProfile(body); break;
            }
            SlideTabTo((int)t, !first && old != (int)t);
        }

        void RefreshCurrentTab() { if (game == null) ShowTab(tab); }

        void BuildTabBar()
        {
            UI.Place(tabBar, 0.5f, 0, 0, 100, 1040, 160);
            var bar = UI.Sliced(tabBar, "navbar", 160, "Bar");
            UI.Stretch(bar.rectTransform);
            var glow = UI.Sliced(tabBar, "btn_orange", 132, "Glow");
            tabGlow = glow.rectTransform;
            tabGlow.anchorMin = tabGlow.anchorMax = new Vector2(0, 0.5f);
            tabGlow.sizeDelta = new Vector2(236, 132);
            float w = 1040f / 4f;
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var cell = UI.Node(tabBar, "Tab_" + TabNames[i]);
                UI.Place(cell, 0, 0.5f, w * (i + 0.5f), 0, w, 160);
                var hit = cell.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0);
                UI.Click(hit, () => { if ((int)tab != idx) ShowTab((Tab)idx); });
                cell.GetComponent<ButtonFx>().Down = 0.96f;
                var icon = UI.Icon(cell, TabArt[i], 68, "Icon");
                icon.raycastTarget = false;
                UI.Place(icon.rectTransform, 0.5f, 0.5f, 0, 20, 68, 68);
                var label = UI.Label(cell, TabNames[i].ToUpper(), 23, Palette.Ink, TextAnchor.MiddleCenter, true);
                label.GetComponent<Outline>().effectColor = UI.PillOutline("btn_orange");
                UI.Place(label.rectTransform, 0.5f, 0.5f, 0, -42, 236, 26);
                tabIcons.Add(icon.rectTransform); tabTexts.Add(label); tabImgs.Add(icon);
            }
            tabGlow.SetSiblingIndex(1);
            tabBadge = UI.Icon(tabBar, "spark_glow", 30, "Badge"); tabBadge.gameObject.SetActive(false);
        }

        void SlideTabTo(int idx, bool animate)
        {
            float w = 1040f / 4f, target = w * (idx + 0.5f);
            tabGlow.DOKill();
            if (animate) tabGlow.DOAnchorPosX(target, 0.3f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(tabGlow.gameObject);
            else tabGlow.anchoredPosition = new Vector2(target, 0);
            for (int i = 0; i < tabIcons.Count; i++)
            {
                bool on = i == idx;
                tabImgs[i].color = on ? Color.white : new Color(1, 1, 1, 0.9f);
                tabTexts[i].color = on ? Color.white : Palette.Ink;
                tabTexts[i].GetComponent<Outline>().enabled = on;
            }
            // red dot on Collection or Profile when a reward is waiting
            bool dot = false; int dotTab = 2;
            for (int c = 0; c < WordBank.Count; c++) if (Progress.ClaimableStep(c) >= 0) dot = true;
            if (!dot) { dotTab = 3; dot = Progress.ReadyAchievements() > 0; }
            tabBadge.gameObject.SetActive(dot);
            if (dot) { UI.Place(tabBadge.rectTransform, 0, 0.5f, w * (dotTab + 0.5f) + 40, 48, 30, 30); tabBadge.color = Palette.Red; }
        }

        // =============== shared HUD ===============
        class CoinDisplay { public Text T; public int Shown; public Tween Tw; public RectTransform Pill; }
        readonly List<CoinDisplay> coinDisplays = new List<CoinDisplay>();

        /// <summary>Counts every visible coin counter up or down to the real amount (DOTween).</summary>
        float coinHoldUntil;

        /// <summary>Keeps the coin counters still for a moment, so coins can fly in before the number changes.</summary>
        public void HoldCoins(float seconds)
        {
            coinHoldUntil = Mathf.Max(coinHoldUntil, Time.unscaledTime + seconds);
            DOVirtual.DelayedCall(seconds + 0.05f, RefreshCoinPills).SetUpdate(true);
        }

        /// <summary>Gives coins: they fly from `from` to the coin counter, then the counter counts up.</summary>
        public void RewardCoins(int amount, Vector2? from = null, Action done = null)
        {
            int n = Mathf.Clamp(amount / 70, 5, 14);
            float dur = 0.95f + n * 0.05f;
            HoldCoins(dur);
            Progress.AddCoins(amount);
            Fx.Fly(fxLayer, from ?? Vector2.zero, CoinTarget(), "coin", n, () => Sfx.Play(Sfx.Kind.Coin, 1f + UnityEngine.Random.value * 0.35f, 0.4f), () => done?.Invoke(), 64f, 0.95f, 0.05f);
        }

        void RefreshCoinPills()
        {
            if (Time.unscaledTime < coinHoldUntil) return;
            int to = SaveSystem.Data.coins;
            coinDisplays.RemoveAll(c => c.T == null);
            foreach (var c in coinDisplays)
            {
                if (c.Shown == to) continue;
                int from = c.Shown; bool up = to > from;
                c.Tw?.Kill();
                float dur = Mathf.Clamp(Mathf.Abs(to - from) / 500f, 0.4f, 1.3f);
                var t = c.T;
                c.Tw = DOVirtual.Int(from, to, dur, v => { c.Shown = v; if (t != null) t.text = v.ToString("N0"); }).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(t.gameObject);
                c.Tw.OnComplete(() => { c.Shown = to; });
                t.DOKill();
                t.color = up ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f);
                t.DOColor(Color.white, 1.0f).SetUpdate(true).SetLink(t.gameObject);
                if (c.Pill != null) Fx.Punch(c.Pill, up ? 0.12f : 0.07f, 0.4f);
            }
        }

        public RectTransform CoinPill(Transform parent, bool plus = true)
        {
            var pill = UI.Sliced(parent, "chip", 84, "CoinPill", true);
            var rt = pill.rectTransform; rt.sizeDelta = new Vector2(300, 84);
            var coin = UI.Icon(rt, "coin", 62, "Coin");
            UI.Place(coin.rectTransform, 0, 0.5f, 52, 0, 60, 60);
            var t = UI.Label(rt, SaveSystem.Data.coins.ToString("N0"), 40, new Color32(0xB5, 0x55, 0x0A, 255), TextAnchor.MiddleCenter);
            UI.Stretch(t.rectTransform, 84, 0, plus ? 100 : 28, 0);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 22; t.resizeTextMaxSize = 40;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            coinDisplays.Add(new CoinDisplay { T = t, Shown = SaveSystem.Data.coins, Pill = rt });
            if (plus)
            {
                var p = UI.GlyphButton(rt, "g_plus", Palette.Green, 62, "Plus");
                UI.Place(p.rectTransform, 1, 0.5f, -46, 0, 62, 62);
                UI.Click(p, () => ShowShop());
            }
            UI.Click(pill, () => ShowShop());
            return rt;
        }

        public Vector2 CoinTarget()
        {
            // the visible coin counter that is not inside a popup, in Fx layer coordinates
            foreach (var c in coinDisplays)
                if (c.Pill != null && c.Pill.gameObject.activeInHierarchy && !c.Pill.IsChildOf(popupLayer))
                    return fxLayer.InverseTransformPoint(c.Pill.TransformPoint(new Vector3(-c.Pill.rect.width * 0.3f, 0, 0)));
            return new Vector2(canvasRt.rect.width / 2 - 260, canvasRt.rect.height / 2 - 130);
        }

        public RectTransform TopBar(Transform parent, bool gear = true)
        {
            var d = SaveSystem.Data;
            var bar = UI.Node(parent, "TopBar");
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1);
            bar.anchoredPosition = new Vector2(0, -12); bar.sizeDelta = new Vector2(0, 130);

            // avatar + level
            var av = Avatar(bar, d.avatar, d.ring, 110);
            UI.Place(av, 0, 0.5f, 76, 0, 110, 110);
            UI.Click(av.GetComponent<Image>(), () => ShowTab(Tab.Profile));
            var lvl = UI.Label(bar, "LV " + d.playerLevel, 34, Color.white, TextAnchor.MiddleLeft, true);
            UI.PlaceL(lvl.rectTransform, 0, 0.5f, 150, 26, 200, 44);
            var xp = UI.ProgressBar(bar, 240, 34, Palette.Blue, "Xp");
            UI.Place(xp.Root, 0, 0.5f, 270, -22, 240, 34);
            xp.Set(Progress.XpFraction);
            var xpl = UI.Label(xp.Root, $"{d.xp}/{Economy.XpForLevel(d.playerLevel)}", 22, Palette.Ink, TextAnchor.MiddleCenter);
            UI.Stretch(xpl.rectTransform);

            var cp = CoinPill(bar);
            UI.Place(cp, 1, 0.5f, gear ? -290 : -180, 0, 300, 82);
            if (gear)
            {
                var g = UI.GlyphButton(bar, "g_gear", Palette.Blue, 90, "Gear");
                UI.Place(g.rectTransform, 1, 0.5f, -70, 0, 90, 90);
                UI.Click(g, ShowSettings);
            }
            return bar;
        }

        /// <summary>Round avatar picture with an optional frame ring.</summary>
        public RectTransform Avatar(Transform parent, int avatar, int ring, float size)
        {
            var root = UI.Node(parent, "Avatar");
            root.sizeDelta = new Vector2(size, size);
            var rootImg = root.gameObject.AddComponent<Image>(); rootImg.color = new Color(0, 0, 0, 0);
            var img = UI.Img(root, "av_" + Mathf.Clamp(avatar, 0, 11), "Face", true);
            UI.Stretch(img.rectTransform, size * 0.06f, size * 0.06f, size * 0.06f, size * 0.06f);
            if (ring > 0)
            {
                var r = UI.Img(root, "ring_" + new[] { "bronze", "silver", "gold", "diamond" }[Mathf.Clamp(ring - 1, 0, 3)], "Ring");
                UI.Stretch(r.rectTransform, -size * 0.06f, -size * 0.06f, -size * 0.06f, -size * 0.06f);
            }
            return root;
        }

        // =============== scroll area ===============
        public static ScrollRect Scroll(Transform parent, out RectTransform content)
        {
            var area = UI.Node(parent, "Scroll");
            UI.Stretch(area);
            var img = area.gameObject.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0);
            var sr = area.gameObject.AddComponent<ScrollRect>();
            var vp = UI.Node(area, "Viewport"); UI.Stretch(vp);
            vp.gameObject.AddComponent<RectMask2D>();
            var vimg = vp.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0);
            content = UI.Node(vp, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 2000);
            sr.viewport = vp; sr.content = content; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 40f; sr.decelerationRate = 0.12f;
            return sr;
        }

        /// <summary>Row anchored to the top of scroll content, 1000 wide, advancing the y cursor.</summary>
        public static RectTransform RowAt(RectTransform content, ref float y, float h, float w = 1000f)
        {
            var r = UI.Node(content, "Row");
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1); r.pivot = new Vector2(0.5f, 1);
            r.anchoredPosition = new Vector2(0, -y); r.sizeDelta = new Vector2(w, h);
            y += h;
            return r;
        }

        public static void EndScroll(RectTransform content, float y) { content.sizeDelta = new Vector2(0, y + 60); }

        // =============== toast ===============
        public void Toast(string text, Color? color = null)
        {
            var bubble = UI.Sliced(toastLayer, "btn_navy", 100, "Toast");
            var t = UI.Label(bubble.transform, text, 38, color ?? Color.white, TextAnchor.MiddleCenter, false);
            float w = Mathf.Clamp(t.preferredWidth + 90, 320, 960);
            bubble.rectTransform.sizeDelta = new Vector2(w, 100);
            UI.Stretch(t.rectTransform);
            UI.Place(bubble.rectTransform, 0.5f, 0.5f, 0, -420, w, 100);
            var g = bubble.gameObject.AddComponent<CanvasGroup>();
            Fx.Tween(2.2f, k =>
            {
                if (bubble == null) return;
                bubble.rectTransform.anchoredPosition = new Vector2(0, -420 + 120 * Fx.OutCubic(k * 3));
                g.alpha = k < 0.8f ? 1f : 1f - (k - 0.8f) / 0.2f;
            }, () => { if (bubble != null) Destroy(bubble.gameObject); });
        }
    }
}
