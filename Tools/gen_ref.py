"""Generate sprite sheets in the style of reference screenshots (gpt-image-1.5, LOW quality, image edit endpoint).

Usage: python Tools/gen_ref.py <sheet> [<sheet> ...] [--force]
Reference pictures are given with --ref in the sheet table below (paths on this PC). The API key is read like in gen_art.py.
"""
import base64, csv, sys, time
from pathlib import Path
from openai import OpenAI

sys.path.insert(0, str(Path(__file__).resolve().parent))
from gen_art import read_key, OUT, LOG, MODEL, PRICES  # noqa: E402

IMG = Path(r"C:\Users\sidha\AppData\Local\Temp\claude\D--UNITY-GAMES-SID-WordQuestDaily\ad95bbdc-3ce3-4280-9573-cc377aa87fca\images")
REF_SHOP, REF_HOME, REF_PLAY, REF_ADS = IMG / "34.jpg", IMG / "33.jpg", IMG / "35.jpg", IMG / "36.jpg"

STYLE = ("Match the art style of the reference screenshot exactly: glossy chunky 3D-jelly casual mobile puzzle game art, thick soft shapes, "
         "rich saturated colors, a bright top highlight, a darker underside, a thin dark outline, cute and clean. "
         "Do NOT copy any item from the reference, draw the new items described. Absolutely no text, no letters, no numbers, no watermark.")
GRID = ("Lay the items out on one clean grid on a fully transparent background with wide empty gaps so nothing touches or overlaps. "
        "Each item is complete, large and centered in its own cell, no cast shadows, no outer glow, no cell borders or labels.")

SHEETS = {
 "ui_icons_v3": ("1536x1024", True, [REF_SHOP],
   f"{STYLE} A sprite sheet of 12 icons in a grid of 4 columns and 3 rows, reading order: 1 gold coin with an embossed star in the middle, "
   "2 small neat stack of three gold star coins, 3 pile of gold star coins, 4 big heap of gold star coins with a few loose coins, "
   "5 red gift box with a gold ribbon bow, 6 wooden treasure chest with gold trim, 7 gold padlock, 8 round green badge with a white check mark, "
   f"9 gold trophy cup, 10 gold crown, 11 gold five-point star, 12 pale grey five-point star. {GRID}"),
 "tabs_v3": ("1536x1024", True, [REF_HOME],
   f"{STYLE} A sprite sheet of 6 big navigation icons in a grid of 3 columns and 2 rows, reading order: 1 stack of two rounded cards with a purple star "
   "(collection), 2 orange shopping basket full of colorful jelly gems (shop), 3 cute purple jelly house with a smiling face and a white snowy roof (home), "
   "4 gold trophy cup (league), 5 purple shield with a gold star-shaped emblem (profile), 6 red heart. "
   f"{GRID}"),
 "powers_v3": ("1536x1024", True, [REF_PLAY],
   f"{STYLE} A sprite sheet of 6 power-up icons in a grid of 3 columns and 2 rows, each a single cute glossy jelly object (no background tile), "
   "reading order: 1 green jelly with two crossing arrows (shuffle), 2 magnifying glass with a blue lens and a gold handle (hint), "
   "3 purple letter tile with a sparkle (reveal letter), 4 round blue radar target with a green ping (word finder), "
   "5 pink magic wand with golden stars (reveal word), 6 round purple orb with a white question mark shape (mystery). "
   f"{GRID}"),
 "misc_v3": ("1536x1024", True, [REF_SHOP],
   f"{STYLE} A sprite sheet of 12 icons in a grid of 4 columns and 3 rows, reading order: 1 calendar page with a red top, 2 golden bell, "
   "3 light-blue snowflake crystal, 4 speaker with sound waves, 5 speaker with a red x, 6 open book with a blue cover, 7 rolled scroll with a red ribbon, "
   "8 blue shield with a white lightning bolt, 9 orange flame, 10 purple gift box with a gold question mark, 11 blue shield with a gold star, "
   f"12 cute pink piggy bank with a coin slot. {GRID}"),
 "levelcard_v3": ("1536x1024", True, [REF_HOME],
   f"{STYLE} A sprite sheet of 3 empty level cards in a grid of 3 columns and 1 row, each a rounded-square card (about as tall as wide) with a thick colored frame, "
   "a muted empty picture area in the upper part and a flat label strip at the bottom: 1 green frame with a bright green label strip, "
   "2 green frame with a bright glowing green label strip and tiny sparkles around, 3 grey-blue locked card with a dark grey label strip. "
   f"No picture inside, no number. {GRID}"),
 "mascot_v3": ("1024x1536", True, [REF_ADS],
   f"{STYLE} A sprite sheet with 2 illustrations stacked vertically, each centered in its half: top a cute round blue jelly mascot character with big happy eyes "
   "holding a big red circle-with-slash symbol (no ads) shield; bottom a red circle badge with a thick white diagonal slash and a white bold rounded television "
   f"shape inside (an ad-block badge, without any text). {GRID}"),
 "bg_blue_v3": ("1024x1536", False, [REF_HOME],
   "Vertical mobile game background in the exact style of the reference screenshot's background: a bright blue gradient (lighter cyan blue at the top and "
   "center, deeper blue at the bottom) covered with a faint tone-on-tone pattern of large rounded five-point stars in slightly lighter blue. "
   "Soft, low contrast, calm. No text, no characters, no UI."),
 "logo_v3": ("1536x1024", False, [REF_ADS],
   "Three different game logo designs for a word search game called WORD QUEST DAILY, side by side in one row on a plain white background with wide gaps, "
   "in the glossy chunky 3D-jelly style of the reference screenshot: (left) the words WORD QUEST in fat bubbly white letters with a thick dark-blue outline and "
   "a small rainbow capsule underneath and a gold star, (middle) a square app icon: blue jelly tile with a big orange capsule across it with the letters W O R D in white, "
   "(right) a round gold badge with a colorful capsule and a star. Clean, bold, readable small."),
 "splash_v3": ("1024x1536", False, [REF_HOME],
   "Vertical game splash background in the exact style of the reference screenshot: bright blue gradient with a faint star pattern, big soft white sparkles, "
   "and a few colorful glossy 3D-jelly rounded capsules (yellow, purple, green, pink, red) crossing near the corners, leaving a big calm empty glow area in the "
   "middle for a logo. No text, no letters, no numbers."),
}

SMALL = ("IMPORTANT: draw every item SMALL, fully inside its own cell with a wide empty margin on all four sides (each item uses only about half of the "
         "cell width and height), so no item is ever cut off or touches an edge. ")
G32 = ("Grid of 3 columns and 2 rows on a fully transparent background, wide empty gaps, no cast shadows, no outer glow, no labels.")
SHEETS.update({
 "misc_a_v3": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 6 icons, reading order: 1 calendar page with a red top, 2 golden bell, 3 light-blue snowflake crystal, "
   f"4 speaker with sound waves, 5 speaker with a red x, 6 open book with a blue cover. {G32}"),
 "misc_b_v3": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 6 icons, reading order: 1 rolled scroll with a red ribbon, 2 blue shield with a white lightning bolt, "
   f"3 orange flame, 4 purple gift box with a gold question mark, 5 blue shield with a gold star, 6 cute pink piggy bank with a coin slot. {G32}"),
 "ui_b_v3": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 6 icons, reading order: 1 gold trophy cup, 2 gold crown, 3 pile of gold star coins, "
   f"4 big heap of gold star coins with a few loose coins, 5 round purple orb with a white question mark, 6 gold five-point star. {G32}"),
 "glyphs_c_v3": ("1536x1024", True, [REF_SHOP], "A sprite sheet of 6 chunky UI symbols in pure white with a soft light-grey underside, rounded ends, thick strokes, no outline, no background. "
   f"{SMALL}Reading order: 1 gear, 2 house, 3 left arrow, 4 X close mark, 5 plus sign, 6 check mark. No text. {G32}"),
})

ANI = ("Each portrait is a cute round character face with big shiny eyes inside a colored round badge with a thin white rim, clearly different animals with unmistakable features. ")
TILE = ("Each item is a simple, instantly recognizable symbol on a glossy rounded-square tile with a thin white rim. ")
MED = ("Each item is a round gold achievement medal with a colored ribbon below and ONE simple symbol in the middle. ")
SHEETS.update({
 "avatars_a_v4": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 6 avatars. {ANI}Reading order: 1 orange fox with pointed ears, 2 black and white panda, 3 pink cat with whiskers, 4 dark brown owl with two feather ear tufts and a small orange beak (NOT green), 5 bright green frog with wide eyes on top of its head, 6 golden lion with a mane. {G32}"),
 "avatars_b_v4": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 6 avatars. {ANI}Reading order: 1 white bunny with long ears, 2 blue and white penguin with an orange beak, 3 grey koala with big round ears, 4 green baby dragon with small horns, 5 silver robot with a screen face, 6 wizard with a blue pointed hat and white beard. {G32}"),
 "cats_a_v4": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 6 category icons. {TILE}Reading order: 1 paw print on a yellow tile, 2 hamburger on an orange tile, 3 earth globe on a blue tile, 4 soccer ball on a green tile, 5 rocket on a purple tile, 6 leafy tree on a mint tile. {G32}"),
 "cats_b_v4": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 6 category icons. {TILE}Reading order: 1 film clapperboard on a dark purple tile, 2 computer chip on a teal tile, 3 music note on a magenta tile, 4 red heart on a pink tile, 5 blue fish over waves on a sky-blue tile, 6 stone castle on a gold tile. {G32}"),
 "medals_a_v4": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 4 medals in a grid of 2 columns and 2 rows. {MED}Reading order: 1 magnifying glass, 2 flame, 3 lightning bolt, 4 compass. Wide gaps, transparent background, no labels."),
 "medals_b_v4": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 4 medals in a grid of 2 columns and 2 rows. {MED}Reading order: 1 calendar with a check mark, 2 crown, 3 open book, 4 star. Wide gaps, transparent background, no labels."),
 "core_a_v4": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 6 icons, reading order: 1 gold coin with an embossed star, 2 small stack of three gold star coins, 3 red gift box with gold ribbon, 4 wooden treasure chest with gold trim, 5 gold padlock, 6 round green badge with a white check mark. {G32}"),
 "core_b_v4": ("1536x1024", True, [REF_SHOP], f"{STYLE} {SMALL}A sprite sheet of 6 icons, reading order: 1 gold five-point star, 2 pale grey five-point star, 3 small blue four-point sparkle star, 4 round glowing pink confetti dot, 5 round yellow confetti dot, 6 small white fluffy cloud. {G32}"),
})


def main(names):
    force = "--force" in names
    names = [n for n in names if n != "--force"]
    client = OpenAI(api_key=read_key())
    for name in names:
        size, transparent, refs, prompt = SHEETS[name]
        if (OUT / f"{name}.png").exists() and not force:
            print(f"{name}: exists, skipped"); continue
        t = time.time()
        files = [open(r, "rb") for r in refs]
        kw = dict(model=MODEL, image=files, prompt=prompt, size=size, quality="low", n=1, output_format="png")
        if transparent: kw["background"] = "transparent"
        res = client.images.edit(**kw)
        for f in files: f.close()
        (OUT / f"{name}.png").write_bytes(base64.b64decode(res.data[0].b64_json))
        new = not LOG.exists()
        with open(LOG, "a", newline="") as f:
            w = csv.writer(f)
            if new: w.writerow(["time", "sheet", "size", "quality", "usd"])
            w.writerow([time.strftime("%Y-%m-%d %H:%M:%S"), name, size, "low", PRICES["low"][size]])
        print(f"{name}: saved ({time.time() - t:.0f}s, ${PRICES['low'][size]})")


if __name__ == "__main__":
    main(sys.argv[1:])
