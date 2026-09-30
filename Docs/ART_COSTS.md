# Image generation cost

Model: **gpt-image-1.5**, quality **medium**. Prices are from the OpenAI pricing table (GPT Image 1.5, Medium):

| Size | Price per image |
|---|---|
| 1024 x 1024 | $0.034 |
| 1024 x 1536 | $0.050 |
| 1536 x 1024 | $0.050 |

Note: the model has no true 1080p size. The largest sizes are 1536x1024 and 1024x1536, so the art was
made at those sizes (transparent PNG) and cut into single sprites. The backgrounds are scaled to fill the screen.

## What the game needed (one image per sheet)

| Sheet | Size | Items | Cost |
|---|---|---|---|
| buttons | 1536x1024 | 8 pill buttons | $0.050 |
| icons_main | 1536x1024 | 20 icons (coins, stars, chest, ...) | $0.050 |
| icons_power | 1536x1024 | 12 (tab icons, 5 power-ups, ...) | $0.050 |
| panels | 1536x1024 | popup, cards, ribbons, bars | $0.050 |
| ui_round | 1536x1024 | 6 round buttons | $0.050 |
| avatars | 1536x1024 | 12 avatars | $0.050 |
| badges | 1536x1024 | 4 frames + 8 medals | $0.050 |
| categories | 1536x1024 | 12 category icons | $0.050 |
| map_fx | 1536x1024 | level nodes, sparkles, confetti | $0.050 |
| frames | 1024x1024 | 4 board frames | $0.034 |
| bg_meadow / ocean / candy / night | 1024x1536 | 4 backgrounds | 4 x $0.050 = $0.200 |
| brand_mark | 1024x1024 | logo mark | $0.034 |
| brand_icon | 1024x1024 | app icon | $0.034 |
| brand_splash | 1024x1536 | splash | $0.050 |
| brand_feature | 1536x1024 | Play Store banner | $0.050 |
| tutorial | 1024x1024 | pointing hand for the tutorials | $0.034 |
| **Total (19 images)** | | ~112 sprites | **$0.886** |

Packing many items into one sheet gives about 6 sprites per image. Separate images would have cost
about $5.50 for the same sprites (110 x $0.05).

## What was actually spent

`Tools/cost_log.csv` lists every call: **36 images, $1.688**.
The extra $0.80 is 17 duplicate sheets (made in the first run, before the tutorial hand was added): a first background run was believed to have stopped, but it kept
running next to the second one, so every sheet was made twice. `gen_art.py` now skips sheets that already
exist unless `--force` is given, so this cannot happen again.
