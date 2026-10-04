"""Builds the Android adaptive icon layers and the Play Store images from the generated art.
Run:  python Tools/make_icons.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets" / "Resources" / "Art"
ICONS = ROOT / "Assets" / "Art" / "Icons"
STORE = ROOT / "Store"
FONT = ROOT / "Assets" / "Resources" / "Fonts" / "LilitaOne-Regular.ttf"
ICONS.mkdir(parents=True, exist_ok=True)
STORE.mkdir(exist_ok=True)

BLUE = (30, 143, 232)

# --- adaptive icon: background = flat sky-blue gradient, foreground = the logo tile on transparent (kept inside the safe zone) ---
bg = Image.new("RGB", (1024, 1024), BLUE)
px = bg.load()
for y in range(1024):
    t = y / 1023
    c = (int(60 - 30 * t), int(170 - 40 * t), int(245 - 20 * t))
    for x in range(1024):
        px[x, y] = c
bg.save(ICONS / "icon_bg.png")
mark = Image.open(ART / "logo_mark.png").convert("RGBA")
mark.thumbnail((640, 640), Image.LANCZOS)
fg = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
fg.alpha_composite(mark, ((1024 - mark.width) // 2, (1024 - mark.height) // 2))
fg.save(ICONS / "icon_fg.png")

# --- Play Store icon 512 (full bleed square from the logo art) ---
Image.open(ICONS / "brand_icon.png").convert("RGB").resize((512, 512), Image.LANCZOS).save(STORE / "play_icon_512.png")
Image.open(ICONS / "brand_icon.png").convert("RGB").resize((1024, 1024), Image.LANCZOS).save(STORE / "app_icon_1024.png")


def outlined(d, xy, text, font, fill, outline, w=5, anchor="mm"):
    x, y = xy
    for dx in range(-w, w + 1):
        for dy in range(-w, w + 1):
            if dx * dx + dy * dy <= w * w:
                d.text((x + dx, y + dy), text, font=font, fill=outline, anchor=anchor)
    d.text((x, y), text, font=font, fill=fill, anchor=anchor)


# --- feature graphic 1024x500: capsule background + logo + title ---
sp = Image.open(ART / "brand_splash.png").convert("RGB")
w, h = sp.size
crop = sp.crop((0, int(h * 0.30), w, int(h * 0.30) + int(w * 500 / 1024))).resize((1024, 500), Image.LANCZOS)
feat = crop.copy()
shade = Image.new("RGBA", (1024, 500), (255, 255, 255, 0))
ImageDraw.Draw(shade).rectangle((0, 0, 1024, 500), fill=(255, 255, 255, 120))
feat = Image.alpha_composite(feat.convert("RGBA"), shade).convert("RGB")
m2 = Image.open(ART / "logo_mark.png").convert("RGBA"); m2.thumbnail((360, 360), Image.LANCZOS)
feat.paste(m2, (70, (500 - m2.height) // 2), m2)
d = ImageDraw.Draw(feat)
f1 = ImageFont.truetype(str(FONT), 112); f2 = ImageFont.truetype(str(FONT), 78)
outlined(d, (640, 200), "WORD QUEST", f1, (30, 143, 232), (255, 255, 255), 6)
outlined(d, (640, 305), "DAILY", f2, (255, 138, 27), (255, 255, 255), 6)
feat.save(STORE / "feature_graphic_1024x500.png")

# --- store splash 1080x1920 ---
sp2 = sp.resize((1080, 1620), Image.LANCZOS).crop((0, 0, 1080, 1620))
canvas = Image.new("RGB", (1080, 1920), (255, 255, 255)); canvas.paste(sp2, (0, 150))
m3 = Image.open(ART / "logo_mark.png").convert("RGBA"); m3.thumbnail((560, 560), Image.LANCZOS)
canvas.paste(m3, ((1080 - m3.width) // 2, 560), m3)
d = ImageDraw.Draw(canvas)
f3 = ImageFont.truetype(str(FONT), 150); f4 = ImageFont.truetype(str(FONT), 100)
outlined(d, (540, 1250), "WORD QUEST", f3, (30, 143, 232), (255, 255, 255), 7)
outlined(d, (540, 1380), "DAILY", f4, (255, 138, 27), (255, 255, 255), 7)
canvas.save(STORE / "splash_1080x1920.png")
print("icons and store images written")
