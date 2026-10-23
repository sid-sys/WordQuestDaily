# HANDOFF - Word Quest Daily

Written 2026-10-21. Read this first if you pick the project up on another machine or account.

## What this is
Word-search mobile game. Unity 6000.6.0f1, URP 2D, portrait, Android only. Package `com.coreinteractive.wordquestdaily`, developer "Tetra Developers". All UI is built in C# at runtime (no scenes full of prefabs). See `README.md` for the layout.

## Where things stand
- **Live / uploaded on Google Play: version 7** (versionCode 7, version name 1.1.0). It has the new UI, bundle shop, Remove Ads popup, notifications, Daily Quest fixed at 8x8. The owner tested a real purchase on it: IAP works.
- **Newer code on `main` is NOT released.** Commit `00046a0` (2026-10-07) changed the game screen:
  - only ONE power-up is left: Reveal Word (bulb button top right, 400 coins, max 3, free every 10 levels)
  - pause button moved top left
  - board and word list are light grey; found words turn black (no strike-through); unfound words grey
  - swipe colors are now red, green, blue, yellow, violet (all effects use the same five)
  - the bottom power-up bar is gone, the board uses the space
  - old stock of the 4 removed power-ups is converted to coins on load (half the old price) in `SaveData.Fix`
  - daily reward + bundles now give Reveal Word instead of Shuffle/Hint/Letter (bundle contents: 1/2/4/6 Reveal Words, Ad-Free Plus 2)
- This change was checked only in the Unity editor (portrait screenshots, no layout problems except a known false alarm on the SKIP button). **It was never run on a phone and no new .aab was built.**

## Latest changes (2026-10-23, not yet released, not tested on a phone)
- Game screen background is plain white (`SetWhiteBackground`); board and word list panel color `Palette.PanelGrey` #E9ECF1.
- League: all rivals are simulated (no server). In debug/editor builds each simulated row shows a red BOT tag (`Row.IsBot`, `Debug.isDebugBuild`); release builds show nothing.
- Economy rebalanced for Reveal Word only: cost 300 coins, max 5, free every 10 levels. Bundles: Starter 300c + 2 RW (Rs99), Player 900c + 6 RW (Rs299), Power 2000c + 14 RW (Rs599), Mega 4000c + 25 RW + Rainbow effect (Rs999), Ad-Free Plus 600c + 3 RW (Rs399). Daily reward: 100c, 150c, 1 RW, 200c, 250c, 1 RW, 400c + 1 RW.
- Reveal Word icon is the magnifying glass (`pu_hint`), not the wand (`PowerUps.Art[4]`); game button, shop, bundles and daily reward all use it.
- Shop bundle card: price button is its own button, the name strip stops before it.
- `Docs/ICON_PROMPTS.md`: 3 big ChatGPT sheet prompts (24 + 23 + 20 icons) to regenerate the icons; owner has only 2-3 image generations per day. After the PNGs arrive: remove background, cut, rename in the listed order, force reimport. Python is no longer installed/working on this PC, so `Tools/*.py` cannot run until it is reinstalled.
- Play Console: bundle descriptions must match the new contents above.

## To do (in order)
1. Test on a phone (debug build): swipe colors, Reveal Word (use, buy at 0 left), pause, Shop, daily reward day 2/4/6/7, bundles still deliver, result popup, level 1 swipe tutorial, upgrade from an existing save (old power-ups -> coins).
2. Build release **version 8** (`BuildTool.Build(true, 8, "1.2.0")`, see Build below), check it is signed with the upload keystore, upload to Internal testing, test, then Production.
3. **Play Console text**: bundle names/descriptions (bundle_starter, bundle_player, bundle_power, bundle_mega, remove_ads_plus) may still mention Shuffle/Hint/Letter/Finder. Update to "Reveal Word".
4. **Store screenshots are old** (`Store/screenshots/play_*.png` show the removed bottom power bar). Retake 3-4 gameplay screenshots at 70%+ words found with the new UI and upload them.
5. Clean-up (nice to have): dead code for the removed power-ups (`PowerUp` enum entries, `PowerUps.Names/Info/Art` for Shuffle/Hint/Letter/Finder, `Progress.cs` references, unused `Tutorial` kind 2), `EffectsLab.cs` has no UI entry and could be deleted, notification text says "free power-up" (it now means Reveal Word).
6. Not checked: some old art may remain on map nodes and a few screens after the art swap; real rewarded/interstitial ads on the release build; push notifications after the v8 build (they worked in the v7 debug test, delivered about 50 s after scheduling; one earlier test only arrived after data was turned on).
7. Players on the old v5 build start again from level 1 (new save key). Restore Purchases button is in the Shop.
8. Optional: an iOS build was never started.

## Things that are NOT in git (get them from the owner)
- **Upload keystore + password**: folder `Keystore/` (git-ignored). Never commit it, never paste it in chat. Without it you cannot sign updates (Play App Signing can reset it, but only through Play Console support).
- **OpenAI key** for art generation: `OPENAI_API` in `D:\UNITY GAMES\.env.local` (outside the repo). Only needed for `Tools/gen_art.py` / `gen_ref.py`.
- Builds (`Builds/*.aab`, `*.apk`) are git-ignored.
- The promo video `brag-output/` is git-ignored (made with Hyperframes; `brag-output/composition/index.html` is the source, `brag.mp4` is the result). Copy it by hand if needed.

## Setup on a new machine
1. Install Unity Hub + Unity **6000.6.0f1** with Android Build Support (JDK, SDK, NDK).
2. `git clone https://github.com/sid-sys/WordQuestDaily.git`, open in Unity. First import takes a while. Packages: DOTween 1.3.030 (in `Assets/Plugins/Demigiant`), Unity IAP 5.4.3, Google Mobile Ads 11.5.0 + UMP, `com.unity.mobile.notifications` 2.5.0.
3. Scripting defines `WQ_IAP` and `WQ_ADMOB` are switched on automatically by `Assets/Editor/AdMobDefine.cs` when the packages are installed. If purchases or ads do nothing, check these defines.
4. minSdk 26, IL2CPP, ARM64 + ARMv7.

## Build (release)
In the Unity editor, run `WordQuest.EditorTools.BuildTool.Build(bundle: true, versionCode: N, version: "x.y.z")` from `Assets/Editor/BuildTool.cs` (menu or via the Unity CLI/MCP `eval`). It reads the keystore path and password from `Keystore/KEYSTORE-INFO.txt` (git-ignored, keystore file name `wordquest-upload.keystore`, alias `wordquest`). Output: `Builds/WordQuestDaily-vN.aab`. A build takes several minutes; the eval call may time out at ~590 s while the build keeps running - wait for the file.
Debug build for the phone: `Builds/WordQuestDaily-dev.apk` (has a 25-second test notification and test ads). Install with adb (`PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`). A Play-signed install cannot be updated by a locally signed one (INSTALL_FAILED_UPDATE_INCOMPATIBLE): uninstall first.

## Play Console / IDs
- One-time products (all active): `remove_ads`, `remove_ads_plus`, `bundle_starter`, `bundle_player`, `bundle_power`, `bundle_mega` (code: `Assets/Scripts/Ads/Iap.cs`, contents: `Economy.cs` -> `Bundles`). `coins_small/medium/large` were deleted from the console.
- Target prices: Rs 99 / 299 / 599 / 999 (bundles), Rs 199 (remove ads), Rs 399 (ads + kit). The shop shows the store's real price.
- AdMob ids: `Assets/Scripts/Ads/AdIds.cs` (debug builds use Google test ids). `app-ads.txt` is on https://sid-sys.github.io (repo `sid-sys/sid-sys.github.io`).
- Privacy policy: https://sid-sys.github.io/wordquestdaily-privacy/ (repo `sid-sys/wordquestdaily-privacy`), already mentions notifications.
- Developer email: tetraheaddevelopers@gmail.com.

## How we test (Unity editor, no real touch)
Play mode stalls when the editor is not focused, so the tests step frames by hand: `EditorApplication.isPaused = true`, then `TestHarness.Advance/Settle` (`Assets/Editor/TestHarness.cs`). Helpers: `Boot`, `Swipe`, `PlayThrough(level)`, `LintAll`, `LintGame` (uses `Assets/Scripts/Debug/UiLint.cs` to find overlaps and elements cut at the edge). Use the Unity CLI: `unity command eval "<C#>"`, `unity command capture_game_view --width 1080 --height 2340 --save_path <inside project>`, `editor_play`, `editor_stop`, `recompile`. Do not edit scripts while in play mode (domain reload breaks it). To see portrait, set the Game view to a custom 1080x2340 size.

## Art
Sprites live in `Assets/Resources/Art` (sliced from gpt-image-1.5 sheets, low quality, see `Docs/ART_COSTS.md` and `Tools/cost_log.csv`). Pills, cards, rings and tiles are drawn in code (`Assets/Scripts/UI/Shapes.cs`) so corners are exact. If you regenerate a sprite, force a reimport (`AssetDatabase.ImportAsset("Assets/Resources/Art", ImportRecursive | ForceUpdate)`) or Unity shows the old texture. Check new sprites for pixels on the cell edge (this caused "cut-off icon" bugs before).

## Design rules the owner asked for (keep them)
- Game screen: word list on top (grey card, found words black), swiped word in a pill between list and board, full-width light-grey board, pure black large letters, five swipe colors, pause top left, Reveal Word top right, no bottom bar, no frame/board-theme choices, no Effects Lab button.
- Level coins come from FixedReward x TargetWinRate; no unlimited coin purchases; Daily Quest is always the same size (8x8).
- No timer in levels.
