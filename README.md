# Word Quest Daily

A word search game for phones, made with Unity 6 (URP 2D, portrait).

- No timer. Levels go from 6x7 (Beginner) to 11x11 (Expert): 200 levels in 8 chapters.
- 3 stars per level: finish, find the hidden Mystery Word, finish without power-ups.
- Daily Quest (same puzzle for everyone each day), streak with freezes, daily reward once per day (resets at 00:00).
- Word collection with 12 categories, coins, XP, power-ups (Shuffle, Hint, Reveal Letter, Word Finder, Reveal Word).
- Weekly league (simulated rivals), achievements, avatars, board themes, word effects.
- All UI is built in code from generated sprite sheets (`Assets/Resources/Art`). Pills and cards are drawn in code so corners are exact; animations use DOTween.
- Hand tutorials teach swiping (level 1) and one power-up at a time (Hint L1, Shuffle L3, Reveal Letter L7, Word Finder L12, Reveal Word L18); each power-up is locked until its lesson is done.
- Effects Lab (Profile or Settings) previews every word effect and sound and shows which one is in use.
- Google Mobile Ads (AdMob) and Unity IAP are wired (Remove Ads + 3 coin bags); test ads in the Editor / debug builds.

## Project layout
- `Assets/Scripts/Core` - levels, puzzle generator, economy, save data, league
- `Assets/Scripts/Game` - the board (`WordGridView`) and game screen
- `Assets/Scripts/App` - screens and popups
- `Assets/Scripts/UI`, `Audio`, `Ads` - helpers, procedural sound effects, rewarded ads
- `Tools/gen_art.py` - makes sprite sheets with `gpt-image-1.5` (needs `OPENAI_API` in an env file, never committed)
- `Tools/slice_sheet.py` - cuts a sheet into single sprites
- `Docs/ART_COSTS.md` - what the art cost

## Economy (short)
Level coins: Beginner 75, Easy 100, Medium 125, Hard 150, Expert 200, +25 / +50 for the 2nd / 3rd star.
Power-up prices: Shuffle 150, Hint 200, Reveal Letter 250, Word Finder 350, Reveal Word 500. Stock is capped (5/5/3/3/2).
Free power-ups every 5/10/15/20/25 completed levels. Rewarded ad: +100 coins, 4 a day.
