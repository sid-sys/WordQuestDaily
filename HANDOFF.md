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
- **No coins any more. The Hint is the only currency** (it finds a whole word; magnifying glass icon `pu_hint`, stored in `PlayerData.powers[4]`, helper `PowerUps.HintIndex`). All numbers are in `Assets/Scripts/Core/Economy.cs`.
  - New player: 4 hints. First clear of a level: 1 hint, 2 on every 5th level, 3 on every 10th, +1 for 3 stars. Replays: 0. Daily Quest: 2 (+1 for 3 stars). Perfect Day: 2. Level-up: 0.
  - Rewarded ad: +2 hints, 5 a day (shop row, "out of hints" popup, "x2 HINTS (AD)" on the result popup). Ad unit placement is still named `AdPlacement.Coins`.
  - Daily reward: only day 3 (1 hint) and day 7 (3 hints); other days give nothing and are skipped silently (`Progress.CanClaimDailyReward` moves the cycle on). Collection steps 1/1/2/3, achievements 2-5, streaks 1/2/5 hints, league week 5/3/2/1 hints (ranks 1/2/3/top 10).
  - IAP (same product ids): Starter 10 hints Rs99, Player 35 Rs299, Power 80 Rs599, Mega 150 Rs999 + Rainbow effect, Ad-Free Plus 10 hints Rs399. **Update the Play Console names/descriptions.**
  - Old saves: coins (up to 20 hints) and the old power-ups are turned into hints in `SaveData.Fix`. Legacy field `coins` is kept only for this.
  - UI: coin pill is now a hint pill (`HintPill`, `RewardHints`, `HoldHints`, `RefreshHintPills`), game HUD has pause (left), level pill (middle), Hint button with count (right). Out-of-hints popup offers ad or shop. Shop has no coin purchases.
- Game screen keeps the blue star background (white was tried and reverted at the owner request).
- League: all rivals are simulated (no server). Debug/editor builds show a red BOT tag (`Row.IsBot`).
- Shop bundle card: price button is its own button, the name strip stops before it.
- **New icons are in** (2026-10-23): the 3 ChatGPT sheets (`Assets/Art/Sheets/s1_core.png`, `s2_ui.png`, `s3_avatars.png`) were cut with `Tools/cut_sheet.js` into `Assets/Resources/Art` (66 sprites replaced: core icons, tabs, categories, avatars, medals, mascot, hand, sounds). Re-cut commands: `node Tools/cut_sheet.js Assets/Art/Sheets/s2_ui.png <names> Assets/Resources/Art --holes` (sheet 3 also `--merge=3`; `-` skips a cell; needs `cd Tools/node_tmp && npm install pngjs`). `spark_glow` was kept from the old art. Still old: white `g_*` symbols, ribbons, logo, splash, bg. Check every screen on a phone for odd icons.

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
