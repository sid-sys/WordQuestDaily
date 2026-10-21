# Word Quest Daily

A word search game for phones, made with Unity 6 (URP 2D, portrait).

- No timer. Levels go from 6x7 (Beginner) to 11x11 (Expert): 200 levels in 8 chapters.
- 3 stars per level: finish, find the hidden Mystery Word, finish without power-ups.
- Daily Quest (same puzzle for everyone each day), streak with freezes, daily reward once per day (resets at 00:00).
- Word collection with 12 categories, coins, XP, one power-up (Reveal Word, bulb button top right).
- Weekly league (simulated rivals), achievements, avatars, board themes, word effects.
- All UI is built in code from generated sprite sheets (`Assets/Resources/Art`). Pills and cards are drawn in code so corners are exact; animations use DOTween.
- A hand tutorial teaches swiping on level 1.
- Google Mobile Ads (AdMob) and Unity IAP: Remove Ads, Remove Ads Plus and 4 coin bundles (ids in `Assets/Scripts/Ads/Iap.cs`).

## Project layout
- `Assets/Scripts/Core` - levels, puzzle generator, economy, save data, league
- `Assets/Scripts/Game` - the board (`WordGridView`) and game screen
- `Assets/Scripts/App` - screens and popups
- `Assets/Scripts/UI`, `Audio`, `Ads` - helpers, procedural sound effects, rewarded ads
- `Tools/gen_art.py` - makes sprite sheets with `gpt-image-1.5` (needs `OPENAI_API` in an env file, never committed)
- `Tools/slice_sheet.py` - cuts a sheet into single sprites
- `Docs/ART_COSTS.md` - what the art cost

## Economy (short)
See `Assets/Scripts/Core/Economy.cs`. Level coins 90/120/160/195/200 (Beginner to Expert), star bonus 40/20, +150 coins every 5 levels, Reveal Word costs 400 (max 3, free every 10 levels), rewarded ad +100 coins x3 a day.

## Working on this project
Read `HANDOFF.md` first: setup on a new machine/account, secrets (never in git), release steps, and the open to-do list.
