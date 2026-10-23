# Icon prompts for ChatGPT (image 2, medium quality)

Use one prompt per chat message. Each sheet has 6 items (3 columns x 2 rows), so every item is big and sharp.
Paste the **STYLE** block first in every prompt (it is already included in each prompt below).

## About transparent background
ChatGPT often draws a white or checkerboard background even when you ask for transparent. Do this:
1. Send the prompt as written (it asks for a transparent background).
2. If the result has a white / grey / checkerboard background, send this follow-up: **"Redo the same sheet on a flat pure magenta #FF00FF background, same layout, no magenta inside any item."**
3. Save the PNG into `Assets/Art/Sheets/` with the sheet name (for example `sheet_01_coins.png`) and tell Claude. Claude then removes the magenta or white background and cuts the items (the current `Tools/slice_sheet.py` expects real transparency, so a background-removal step has to be added; Python must be reinstalled on this PC first).

## STYLE (same in every prompt)
```
Glossy chunky candy-style casual mobile puzzle game icons, like a modern word-puzzle game UI kit: thick soft rounded shapes, rich saturated colors, a bright top highlight, a darker underside, a thin dark navy outline, cute and clean. Absolutely no text, no letters, no numbers, no watermark.
```

## LAYOUT (same in every prompt)
```
Lay the 6 items out in a grid of 3 columns and 2 rows on one square image with a fully transparent background (PNG with alpha). Draw every item SMALL: each item uses only about 60% of its cell width and height, centered, with a wide empty margin on all four sides so nothing is ever cut off or touches an edge or another item. No cast shadows, no outer glow, no cell borders, no labels.
```

---

## Sheet 1 - coins (file names in order)
`coin, coin_stack, coin_bag, pack_pile, pack_heap, chest`
```
[STYLE] A sprite sheet of 6 icons in reading order: 1 shiny gold coin with an embossed star in the middle, 2 small neat stack of three gold star coins, 3 brown cloth sack tied at the top and filled with gold coins, 4 pile of gold star coins, 5 big heap of gold star coins with a few loose coins, 6 wooden treasure chest with gold trim, closed. [LAYOUT]
```

## Sheet 2 - rewards
`gift, lock, check, star, star_empty, crown`
```
[STYLE] A sprite sheet of 6 icons in reading order: 1 red gift box with a gold ribbon bow, 2 gold padlock, 3 round green badge with a white check mark, 4 gold five-point star, 5 pale grey five-point star, 6 gold crown with three points. [LAYOUT]
```

## Sheet 3 - prizes
`trophy, pu_hint, mystery, scroll, book, noads_badge`
```
[STYLE] A sprite sheet of 6 icons in reading order: 1 gold trophy cup with two handles, 2 magnifying glass with a blue glass lens and a gold handle (Reveal Word / hint power-up), 3 round purple orb with a white question mark shape, 4 rolled parchment scroll with a red ribbon, 5 open book with white pages and a blue cover, 6 round red badge with a thick white diagonal slash across a white television shape (no ads badge, no text). [LAYOUT]
```

## Sheet 4 - status
`calendar, bell, freeze, flame, xp, league`
```
[STYLE] A sprite sheet of 6 icons in reading order: 1 calendar page with a red top, 2 golden bell, 3 light-blue snowflake crystal, 4 orange and yellow flame, 5 blue shield with a white lightning bolt, 6 purple shield with a gold star emblem. [LAYOUT]
```

## Sheet 5 - sound and hands
`sound_on, sound_off, hand_point, spark_star, spark_glow, mascot`
```
[STYLE] A sprite sheet of 6 items in reading order: 1 speaker with sound waves, 2 speaker with a red x, 3 cartoon pointing hand in a white glove, index finger pointing up and to the right, 4 small blue four-point sparkle star, 5 soft round white glow dot, 6 cute round blue jelly mascot character with big happy eyes holding a red circle-with-slash shield (no text). [LAYOUT]
```

## Sheet 6 - confetti (tiny, simple)
`conf_pink, conf_yellow, conf_blue`  (use 6 cells, only the first 3 are needed)
```
[STYLE] A sprite sheet of 6 tiny particles in reading order: 1 round glowing pink dot, 2 round yellow dot, 3 round blue dot, 4 pink confetti strip, 5 yellow confetti strip, 6 blue confetti strip. [LAYOUT]
```

## Sheet 7 - tab icons
`tab_collection, tab_shop, tab_home, tab_league, tab_profile`
```
[STYLE] A sprite sheet of 5 big navigation icons in reading order (leave the 6th cell empty): 1 stack of two rounded cards with a purple star (collection), 2 orange shopping basket full of colorful jelly gems (shop), 3 cute purple jelly house with a smiling face and a white snowy roof (home), 4 gold trophy cup on a small base (league), 5 purple shield with a gold star-shaped emblem (profile). [LAYOUT]
```

## Sheet 8 and 9 - white UI symbols
Sheet 8: `g_back, g_close, g_plus, g_gear, g_bulb, g_list`   Sheet 9: `g_pause, g_home, g_share, g_check, g_sound, g_bell`
```
A sprite sheet of 6 chunky UI symbols in pure white with a very soft light-grey underside shade, rounded ends, thick strokes, no outline, no background, reading order: [Sheet 8: 1 left arrow, 2 X close mark, 3 plus sign, 4 gear, 5 light bulb, 6 list of three bars with dots] [Sheet 9: 1 pause (two bars), 2 house, 3 share (three connected dots), 4 check mark, 5 speaker with sound waves, 6 bell]. No text. [LAYOUT]
```
(Tip: tell ChatGPT to put the white symbols on a flat dark-blue background if it cannot make transparency; the slicer needs a flat color to key out.)

## Sheets 10 and 11 - categories (rounded-square tiles)
Sheet 10: `cat_0 .. cat_5`   Sheet 11: `cat_6 .. cat_11`
```
[STYLE] A sprite sheet of 6 category icons, each a simple symbol on a glossy rounded-square tile, reading order: [Sheet 10: 1 paw print on a yellow tile, 2 hamburger on an orange tile, 3 earth globe on a blue tile, 4 soccer ball on a green tile, 5 rocket on a purple tile, 6 leafy tree on a mint tile] [Sheet 11: 1 film clapperboard on a dark purple tile, 2 computer chip on a teal tile, 3 music note on a magenta tile, 4 red heart on a pink tile, 5 blue fish over waves on a sky-blue tile, 6 stone castle with towers on a gold tile]. [LAYOUT]
```

## Sheets 12 and 13 - avatars (round badges)
Sheet 12: `av_0 .. av_5`   Sheet 13: `av_6 .. av_11`
```
[STYLE] A sprite sheet of 6 flat cute character head portraits, each inside a colored round badge, big friendly eyes, clearly different with unmistakable features, reading order: [Sheet 12: 1 orange fox with pointed ears, 2 black and white panda, 3 pink cat with whiskers, 4 dark brown owl with two feather ear tufts and a small orange beak, 5 bright green frog with wide eyes on top of its head, 6 golden lion with a mane] [Sheet 13: 1 white bunny with long ears, 2 blue and white penguin with an orange beak, 3 grey koala with big round ears, 4 green baby dragon with small horns, 5 silver robot with a screen face and antenna, 6 wizard with a blue pointed hat and white beard]. [LAYOUT]
```

## Sheets 14 and 15 - medals
Sheet 14: `medal_hunter, medal_streak, medal_speed, medal_explorer`   Sheet 15: `medal_week, medal_champion, medal_collector, medal_master`
```
[STYLE] A sprite sheet of 4 round gold achievement medals with colored ribbons in a grid of 2 columns and 2 rows, each with one simple symbol in the middle, reading order: [Sheet 14: 1 magnifying glass, 2 flame, 3 lightning bolt, 4 compass] [Sheet 15: 1 calendar with a check mark, 2 crown, 3 open book, 4 star]. Draw every medal SMALL (about 60% of its cell) with a wide empty margin, fully transparent background, no cast shadows, no labels.
```

## Sheet 16 - ribbons (wide banners)
`ribbon_orange, ribbon_purple, ribbon_pink, ribbon_green, ribbon_red, ribbon_blue, ribbon_yellow, ribbon_teal`
```
[STYLE] A sprite sheet of 8 empty ribbon banners with folded back ends and a gentle curve, each very wide (about 6 times wider than tall), no text, arranged in 2 columns and 4 rows, reading order: 1 orange, 2 purple, 3 pink, 4 green, 5 red, 6 blue, 7 yellow, 8 teal. Wide empty gaps, each banner complete and centered in its own cell, fully transparent background, no cast shadows, no labels.
```

---

## Single images (one prompt each, not sheets)
**logo_wordmark** (transparent background)
```
A game logo, no extra text: the words "WORD QUEST" in fat bubbly white letters with a thick dark-blue outline, two lines (WORD above QUEST), and under them a rainbow-colored rounded banner with the word "DAILY" in white on it, and a glossy gold star on its right end. Glossy chunky candy mobile game style, centered, fully transparent background.
```
**brand_splash** (1024x1536, no transparency needed)
```
Vertical game splash screen background: bright blue gradient with a faint tone-on-tone pattern of large rounded five-point stars, big soft white sparkles, and five glossy rounded capsules (red, green, blue, yellow, violet) flying in from the corners, leaving a big calm glow area in the middle for a logo. No text, no letters, no numbers.
```
**bg_meadow** (1024x1536)
```
Vertical mobile game background: bright blue gradient (lighter cyan at the top and center, deeper blue at the bottom) covered with a faint tone-on-tone pattern of large rounded five-point stars in slightly lighter blue. Soft, low contrast, calm. No text, no characters, no UI.
```

## Not needed any more
Shuffle, Reveal Letter and Word Finder icons (`pu_shuffle, pu_letter, pu_finder`) and the wand `pu_word` were removed. Reveal Word now uses the magnifying glass `pu_hint`. Also unused in code: `node_*`, `hand_press`, `spark_ring`, `heart`, `mystery_box`, `piggy`, `cloud`, `logo_mark`.
