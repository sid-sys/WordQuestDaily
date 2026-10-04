"""Generate sprite sheets with gpt-image-1.5 (medium quality).

Usage:  python Tools/gen_art.py <sheet> [<sheet> ...] [--force]      (or: all). Existing sheets are skipped.
The API key is read from D:\\UNITY GAMES\\.env.local (OPENAI_API=...). It is never printed or committed.
Every call is logged to Tools/cost_log.csv so the total cost is known.
"""
import base64, csv, os, sys, time
from pathlib import Path
from openai import OpenAI

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Art" / "Sheets"
LOG = ROOT / "Tools" / "cost_log.csv"
MODEL = "gpt-image-1.5"
# USD per image (from the OpenAI pricing table, GPT Image 1.5)
PRICES = {"medium": {"1024x1024": 0.034, "1024x1536": 0.05, "1536x1024": 0.05},
          "low": {"1024x1024": 0.009, "1024x1536": 0.013, "1536x1024": 0.013}}

STYLE = ("Casual mobile puzzle game art: bright, glossy, chunky and friendly. Soft rounded shapes, "
         "a clean deep-navy outline, a soft top highlight and gentle bottom shade, rich saturated colors, "
         "smooth painted vector look. Absolutely no text, no letters, no numbers, no watermark.")
GRID = ("Lay the items out on one clean grid on a fully transparent background. Leave wide empty gaps "
        "between items so that none touch, overlap or get cut off. Every item is complete, centered in "
        "its own cell, same scale rules, no cast shadows, no outer glow or halo around items, no cell borders or labels.")

SHEETS = {
 "icons_main": ("1536x1024", True,
  f"{STYLE} A sprite sheet of 20 game icons in a grid of 5 columns and 4 rows, in this reading order: "
  "1 shiny gold coin, 2 small stack of gold coins, 3 small sack of gold coins, 4 gold star, 5 empty grey star, "
  "6 orange flame (streak), 7 wrapped gift box with bow, 8 wooden treasure chest, 9 padlock, 10 green check mark badge, "
  "11 gold trophy cup, 12 gold crown, 13 open book, 14 rolled scroll, 15 blue lightning-bolt XP badge, "
  "16 bell, 17 speaker with sound waves, 18 speaker muted, 19 glowing purple mystery orb with a question mark shape, "
  f"20 calendar page. {GRID}"),
 "icons_power": ("1536x1024", True,
  f"{STYLE} A sprite sheet of 12 icons in a grid of 4 columns and 3 rows, reading order: "
  "1 house (home), 2 winding road map pin (levels), 3 stack of books (collection), 4 smiling person bust (profile), "
  "5 shuffle icon: two crossing arrows on a green orb, 6 hint icon: magnifying glass on a yellow orb, "
  "7 reveal letter icon: a letter tile with sparkle on a purple orb, 8 word finder icon: a radar/compass on a blue orb, "
  "9 reveal word icon: magic wand with stars on a pink orb, 10 mystery box with question mark, "
  f"11 shield league emblem, 12 fire streak freeze snowflake. {GRID}"),
 "buttons": ("1536x1024", True,
  f"{STYLE} A sprite sheet of 8 wide rounded pill-shaped empty buttons (no icon, no text), each about 3.2 times wider than tall, "
  "arranged in 2 columns and 4 rows, reading order: 1 grass green, 2 sunny yellow, 3 sky blue, 4 coral red, "
  "5 purple, 6 light grey, 7 white with grey rim, 8 dark navy. Each is glossy with a raised top highlight and a darker bottom edge. "
  f"{GRID}"),
 "panels": ("1536x1024", True,
  f"{STYLE} A sprite sheet of 8 empty UI elements, no text: 1 large rounded popup panel in cream with a thick teal rim "
  "(portrait), 2 medium rounded light card with a soft blue-grey rim (landscape), 3 pink ribbon banner with folded ends (wide), "
  "4 green ribbon banner with folded ends (wide), 5 small grey rounded word chip pill (wide), 6 empty progress bar track "
  "(very wide, dark), 7 green progress bar fill (very wide, glossy, same shape), 8 dark navy rounded bottom navigation bar (very wide). "
  f"{GRID}"),
 "ui_round": ("1536x1024", True,
  f"{STYLE} A sprite sheet of 6 round glossy buttons, reading order in 3 columns and 2 rows: 1 blue with a white left arrow, "
  "2 red with a white X, 3 green with a white plus, 4 grey with a white gear, 5 yellow with a white lightbulb, "
  f"6 purple with a white list icon. Icons are simple symbols only, not letters. {GRID}"),
 "avatars": ("1536x1024", True,
  f"{STYLE} A sprite sheet of 12 cute round avatar portraits in a grid of 4 columns and 3 rows, each a character face inside a "
  "colored circle badge: 1 fox, 2 panda, 3 cat, 4 owl, 5 frog, 6 lion, 7 bunny, 8 penguin, 9 koala, 10 baby dragon, 11 robot, "
  f"12 wizard. {GRID}"),
 "badges": ("1536x1024", True,
  f"{STYLE} A sprite sheet of 12 items in 4 columns and 3 rows: 1-4 empty round avatar frames (ring only, transparent middle) in bronze, "
  "silver, gold and blue diamond; 5-12 achievement medals: 5 magnifier hunter medal, 6 flame streak medal, 7 lightning speed medal, "
  f"8 compass explorer medal, 9 calendar perfect-week medal, 10 crown master medal, 11 book collector medal, 12 star champion medal. {GRID}"),
 "categories": ("1536x1024", True,
  f"{STYLE} A sprite sheet of 12 category icons in 4 columns and 3 rows, each a simple symbol on a rounded square tile: "
  "1 paw print (animals), 2 burger (food), 3 globe (places), 4 soccer ball (sports), 5 rocket (space), 6 leaf tree (nature), "
  f"7 film clapper (movies), 8 circuit chip (technology), 9 music note (music), 10 heart and body (body), 11 wave and fish (ocean), "
  f"12 castle (history). {GRID}"),
 "map_fx": ("1536x1024", True,
  f"{STYLE} A sprite sheet of 12 items in 4 columns and 3 rows: 1 round green level node with a white check mark, "
  "2 big glowing gold current-level node with a star, 3 grey round locked node with a padlock, 4 round chest node in purple, "
  "5 four-point white sparkle star, 6 soft round white glow dot, 7 white sparkle ring, 8 pink confetti rectangle, "
  f"9 yellow confetti circle, 10 blue confetti strip, 11 gold coin seen from the side spinning, 12 small white cloud. {GRID}"),
 "frames": ("1024x1024", True,
  f"{STYLE} A sprite sheet of 4 square board frames in a 2 by 2 grid. Each frame is a decorative empty picture-frame border with "
  "a fully transparent middle, thick, with ornate corners: top-left a wooden frame with green leaves and flowers, "
  "top-right a rope and coral seashell frame, bottom-left a pink candy frame with lollipops and sprinkles, "
  f"bottom-right a silver-blue frame with moon and stars. {GRID}"),
 "bg_meadow": ("1024x1536", False,
  "Vertical soft painterly mobile game background: a sunny green meadow with rolling hills, a few trees, fluffy clouds and a bright "
  "sky. Very low contrast, gently blurred and pastel so a puzzle grid on top stays readable. Calm and empty in the middle. "
  "No text, no characters."),
 "bg_ocean": ("1024x1536", False,
  "Vertical soft painterly mobile game background: a bright turquoise underwater ocean scene with light rays, tiny bubbles, faint "
  "coral and sand far below. Very low contrast, gently blurred and pastel so a puzzle grid on top stays readable. Calm and "
  "empty in the middle. No text, no characters."),
 "bg_candy": ("1024x1536", False,
  "Vertical soft painterly mobile game background: a pastel candy land with pink and mint clouds, lollipops and gumdrop hills far "
  "away. Very low contrast, gently blurred and pastel so a puzzle grid on top stays readable. Calm and empty in the middle. "
  "No text, no characters."),
 "bg_night": ("1024x1536", False,
  "Vertical soft painterly mobile game background: a deep blue night sky with a large soft moon, tiny stars and dark hills. "
  "Low contrast, gently blurred, dark but not black, so a puzzle grid on top stays readable. Calm and empty in the middle. "
  "No text, no characters."),
 "tutorial": ("1024x1024", True,
  f"{STYLE} A sprite sheet with 2 items side by side, each centered in its own half: 1 a friendly cartoon pointing hand in a white glove "
  "with the index finger extended pointing up and to the left (the fingertip is the top-left tip of the picture), 2 the same hand slightly "
  f"tilted and pressed down, index finger bent a little. {GRID}"),
 "brand_mark": ("1024x1024", True,
  f"{STYLE} A game logo mark, no text: a rounded grid of colorful letter-less tiles with a bright glowing swipe line in a rainbow "
  "capsule shape drawn diagonally across three tiles, small sparkles, and a small gold crown or star above. Centered, big, "
  "on a fully transparent background."),
 "brand_icon": ("1024x1024", False,
  f"{STYLE} A square mobile app icon (full-bleed, no rounded corners, no text): a bright teal to blue gradient background with a "
  "cluster of glossy white letter tiles (blank tiles, no letters) and one glowing rainbow capsule swipe line crossing them "
  "diagonally with sparkles. Centered, bold, simple, readable at small size, with safe margin."),
 "brand_splash": ("1024x1536", False,
  "Vertical game splash screen background: a joyful bright teal-to-blue sky gradient with soft clouds, floating blank glossy letter "
  "tiles and sparkles around the edges, and a soft empty glow area in the upper middle for a logo. Glossy chunky casual mobile "
  "game art. No text, no letters, no numbers."),
 "brand_feature": ("1536x1024", False,
  "Wide game banner: bright teal-to-blue sky with clouds; on the right a grid of glossy blank letter tiles with a glowing "
  "rainbow capsule swipe line crossing them and sparkles; the left half is calm and empty for a title. Glossy chunky casual mobile "
  "game art. No text, no letters, no numbers."),
}



STYLE2 = ("Bright candy-style casual mobile game UI kit art, like a modern puzzle-game GUI pack: smooth glossy vector shapes, "
          "saturated juicy colors, a soft top highlight and a darker underside, a thick cream-white outer outline around each item "
          "(not a dark outline), clean and cute. Absolutely no text, no letters, no numbers, no watermark.")
GRID2 = ("Lay the items out on one clean grid on a fully transparent background with wide empty gaps so nothing touches or overlaps. "
         "Each item is complete and centered in its own cell, no cast shadows, no outer glow, no cell borders or labels.")
CAPS = "yellow #F9BB13, purple #924EF3, green #41C038, blue #178ADC, magenta #E052D6 and red #F14D53"

SHEETS.update({
 "icons_main_v2": ("1536x1024", True,
  f"{STYLE2} A sprite sheet of 20 game icons in a grid of 5 columns and 4 rows, reading order: "
  "1 shiny gold coin, 2 small stack of gold coins, 3 small sack of gold coins, 4 gold star, 5 empty pale grey star, 6 orange flame, "
  "7 wrapped gift box with bow, 8 wooden treasure chest, 9 padlock, 10 green check mark badge, 11 gold trophy cup, 12 gold crown, "
  "13 open book, 14 rolled scroll, 15 blue lightning-bolt badge, 16 bell, 17 speaker with sound waves, 18 speaker muted with an x, "
  f"19 glowing purple mystery orb with a question mark shape, 20 calendar page. {GRID2}"),
 "icons_power_v2": ("1536x1024", True,
  f"{STYLE2} A sprite sheet of 12 icons in a grid of 4 columns and 3 rows, reading order: 1 cute house (home), "
  "2 winding road with a red map pin (levels), 3 stack of colorful books (collection), 4 smiling person bust (profile), "
  "5 shuffle icon: two crossing arrows on a green round badge, 6 hint icon: magnifying glass on a yellow round badge, "
  "7 reveal letter icon: a letter tile with sparkle on a purple round badge, 8 word finder icon: radar target on a blue round badge, "
  "9 reveal word icon: magic wand with stars on a pink round badge, 10 mystery box with a question mark shape, "
  f"11 shield league emblem with a star, 12 blue snowflake freeze crystal. {GRID2}"),
 "glyphs_v2": ("1536x1024", True,
  "A sprite sheet of 12 simple chunky UI symbols in pure white with a very soft light-grey underside shade, rounded ends, thick strokes, "
  "no outline, no background, on a fully transparent background, grid of 4 columns and 3 rows, reading order: 1 left arrow, 2 X close mark, "
  "3 plus sign, 4 gear, 5 light bulb, 6 list (three bars with dots), 7 pause (two bars), 8 house, 9 share (three connected dots), "
  f"10 check mark, 11 speaker with waves, 12 bell. No text, no letters, no numbers. {GRID2}"),
 "ribbons_v2": ("1536x1024", True,
  f"{STYLE2} A sprite sheet of 8 empty ribbon banners with folded back ends and a gentle curve, each very wide (about 6 times wider than tall), "
  "no text, arranged in 2 columns and 4 rows, reading order: 1 orange, 2 purple, 3 pink, 4 green, 5 red, 6 blue, 7 yellow, 8 teal. "
  f"{GRID2}"),
 "avatars_v2": ("1536x1024", True,
  f"{STYLE2} A sprite sheet of 12 cute round avatar portraits in a grid of 4 columns and 3 rows, each a character face inside a colored "
  "circle badge: 1 fox, 2 panda, 3 cat, 4 owl, 5 frog, 6 lion, 7 bunny, 8 penguin, 9 koala, 10 baby dragon, 11 robot, 12 wizard. "
  f"{GRID2}"),
 "medals_v2": ("1536x1024", True,
  f"{STYLE2} A sprite sheet of 8 achievement medals in a grid of 4 columns and 2 rows, each a round medal with a ribbon: "
  "1 magnifier, 2 flame, 3 lightning bolt, 4 compass, 5 calendar with check marks, 6 crown, 7 book, 8 star. "
  f"{GRID2}"),
 "categories_v2": ("1536x1024", True,
  f"{STYLE2} A sprite sheet of 12 category icons in a grid of 4 columns and 3 rows, each a simple symbol on a glossy rounded-square tile: "
  "1 paw print, 2 burger, 3 globe, 4 soccer ball, 5 rocket, 6 leafy tree, 7 film clapper, 8 microchip, 9 music note, 10 heart, "
  f"11 wave and fish, 12 castle. {GRID2}"),
 "map_fx_v2": ("1536x1024", True,
  f"{STYLE2} A sprite sheet of 12 items in a grid of 4 columns and 3 rows: 1 round green level node with a white check mark, "
  "2 big glowing gold round node with a star, 3 grey round locked node with a padlock, 4 round chest node in purple, "
  "5 four-point white sparkle star, 6 soft round white glow dot, 7 white sparkle ring, 8 pink confetti rectangle, "
  "9 yellow confetti circle, 10 blue confetti strip, 11 small white fluffy cloud, "
  f"12 a cartoon pointing hand in a white glove, index finger pointing up and to the right. {GRID2}"),
 "bg_sky_v2": ("1024x1536", False,
  "Vertical soft mobile game background: a bright, cheerful sky-blue gradient (light cyan at the top to a slightly deeper blue at the bottom) "
  "with a few big soft white clouds near the top and bottom edges, tiny white sparkles, very low contrast and calm in the middle so a puzzle "
  "board on top stays readable. Flat vector candy style. No text, no characters."),
 "logo_v2": ("1536x1024", False,
  "Three different square mobile app icon designs for a word search game, side by side in one row on a plain white background with wide gaps, "
  "each a rounded square icon with a glossy candy style: (left) a bright blue tile with a grid of white letter tiles and one big diagonal "
  "orange capsule across it spelling the letters W O R D in white, (middle) a white tile with a grid of small grey letters and several colorful "
  "rounded diagonal capsule lines (yellow, green, purple, blue, magenta, red) crossing it, (right) a sky blue tile with a big white capsule "
  "diagonal reading W O R D in blue letters on a grid of letters. Clean, bold, readable at small size."),
 "splash_v2": ("1024x1536", False,
  "Vertical game splash background in a word search puzzle style: a white background covered with a neat grid of large light-grey capital "
  "letters, and many long rounded capsule strokes crossing it at diagonal, horizontal and vertical angles, colored " + CAPS + ". "
  "Some capsules contain white letters. A large calm empty area in the middle for a logo. Flat glossy candy vector style. "
  "No other text."),
})



SIMPLE = ("Each item must be a simple, instantly recognizable emoji-style symbol, drawn clearly and symmetrical where natural, large in its cell. ")
GRID3 = ("Lay the items out in a grid of 3 columns and 2 rows on a fully transparent background with wide gaps, each item big, complete and "
         "centered in its own cell, no cast shadows, no outer glow, no labels.")

SHEETS.update({
 "icons_a_v2": ("1536x1024", True, f"{STYLE2} {SIMPLE}A sprite sheet of 6 icons in reading order: 1 shiny round gold coin with a star embossed, "
  "2 small neat stack of three gold coins, 3 brown cloth sack tied at the top filled with gold coins, 4 gold five-point star, "
  f"5 pale grey five-point star, 6 orange and yellow flame. {GRID3}"),
 "icons_b_v2": ("1536x1024", True, f"{STYLE2} {SIMPLE}A sprite sheet of 6 icons in reading order: 1 red gift box with a yellow ribbon bow, "
  "2 wooden treasure chest with gold trim, closed, 3 gold padlock, 4 round green badge with a white check mark, 5 gold trophy cup with two handles, "
  f"6 gold crown with three points. {GRID3}"),
 "icons_c_v2": ("1536x1024", True, f"{STYLE2} {SIMPLE}A sprite sheet of 6 icons in reading order: 1 open book with white pages and a blue cover, "
  "2 rolled parchment scroll with a red ribbon, 3 blue shield badge with a white lightning bolt, 4 golden bell, "
  f"5 speaker with sound waves, 6 speaker with a red x. {GRID3}"),
 "icons_d_v2": ("1536x1024", True, f"{STYLE2} {SIMPLE}A sprite sheet of 6 icons in reading order: 1 round purple orb with a white question mark, "
  "2 calendar page with a red top, 3 closed purple gift box with a gold question mark, 4 blue shield with a gold star, "
  f"5 light-blue snowflake, 6 red heart. {GRID3}"),
 "avatars_a_v2": ("1536x1024", True, f"{STYLE2} A sprite sheet of 6 flat, cute animal head portraits, each inside a colored round badge, big friendly eyes, "
  "clearly different animals with unmistakable features: 1 orange fox with pointed ears, 2 black and white panda with round black ears, "
  f"3 pink cat with whiskers, 4 brown owl with big round eyes, 5 green frog with wide eyes, 6 golden lion with a mane. {GRID3}"),
 "avatars_b_v2": ("1536x1024", True, f"{STYLE2} A sprite sheet of 6 flat, cute character portraits, each inside a colored round badge, big friendly eyes, "
  "unmistakable features: 1 white bunny with long ears, 2 blue and white penguin with an orange beak, 3 grey koala with big round ears, "
  f"4 green baby dragon with small horns, 5 silver robot with a screen face and antenna, 6 wizard with a blue pointed hat and a white beard. {GRID3}"),
 "cats_a_v2": ("1536x1024", True, f"{STYLE2} {SIMPLE}A sprite sheet of 6 category icons, each a simple symbol on a glossy rounded-square tile: "
  "1 paw print on a yellow tile, 2 hamburger on an orange tile, 3 earth globe with green continents on a blue tile, "
  f"4 black and white soccer ball on a green tile, 5 rocket on a purple tile, 6 green leafy tree on a mint tile. {GRID3}"),
 "cats_b_v2": ("1536x1024", True, f"{STYLE2} {SIMPLE}A sprite sheet of 6 category icons, each a simple symbol on a glossy rounded-square tile: "
  "1 film clapperboard on a dark purple tile, 2 computer chip on a teal tile, 3 pink music note on a magenta tile, "
  f"4 red heart on a pink tile, 5 blue fish over waves on a sky-blue tile, 6 stone castle with towers on a gold tile. {GRID3}"),
 "medals_a_v2": ("1536x1024", True, f"{STYLE2} {SIMPLE}A sprite sheet of 4 round gold achievement medals with colored ribbons in a grid of 2 columns "
  "and 2 rows, each with one simple symbol in the middle: 1 magnifying glass, 2 flame, 3 lightning bolt, 4 compass. "
  "Wide gaps, each medal big, complete, centered in its own cell, transparent background, no labels."),
 "medals_b_v2": ("1536x1024", True, f"{STYLE2} {SIMPLE}A sprite sheet of 4 round gold achievement medals with colored ribbons in a grid of 2 columns "
  "and 2 rows, each with one simple symbol in the middle: 1 calendar with a check mark, 2 crown, 3 open book, 4 star. "
  "Wide gaps, each medal big, complete, centered in its own cell, transparent background, no labels."),
})


def read_key():
    for line in Path(r"D:\UNITY GAMES\.env.local").read_text(encoding="utf-8").splitlines():
        if line.startswith("OPENAI_API"):
            return line.split("=", 1)[1].strip().strip('"').strip("'")
    raise SystemExit("OPENAI_API not found")


def main(names):
    force = "--force" in names
    names = [n for n in names if n != "--force"]
    client = OpenAI(api_key=read_key())
    OUT.mkdir(parents=True, exist_ok=True)
    if names == ["all"]:
        names = list(SHEETS)
    for name in names:
        size, transparent, prompt = SHEETS[name]
        if (OUT / f"{name}.png").exists() and not force:
            print(f"{name}: already exists, skipped (use --force to pay for it again)")
            continue
        t = time.time()
        quality = "low" if name.endswith("_v2") else "medium"
        kw = dict(model=MODEL, prompt=prompt, size=size, quality=quality, n=1, output_format="png")
        if transparent:
            kw["background"] = "transparent"
        res = client.images.generate(**kw)
        (OUT / f"{name}.png").write_bytes(base64.b64decode(res.data[0].b64_json))
        new = not LOG.exists()
        with open(LOG, "a", newline="") as f:
            w = csv.writer(f)
            if new:
                w.writerow(["time", "sheet", "size", "quality", "usd"])
            w.writerow([time.strftime("%Y-%m-%d %H:%M:%S"), name, size, quality, PRICES[quality][size]])
        print(f"{name}: saved ({time.time() - t:.0f}s, ${PRICES[quality][size]})")


if __name__ == "__main__":
    main(sys.argv[1:])
