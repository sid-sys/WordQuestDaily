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

# --- adaptive icon: background = the generated icon, foreground = the logo mark on transparent ---
bg = Image.open(ICONS / "brand_icon.png").convert("RGBA").resize((1024, 1024), Image.LANCZOS)
bg.save(ICONS / "icon_bg.png")
mark = Image.open(ART / "logo_mark.png").convert("RGBA")
target = 600
mark.thumbnail((target, target), Image.LANCZOS)
fg = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
fg.alpha_composite(mark, ((1024 - mark.width) // 2, (1024 - mark.height) // 2))
fg.save(ICONS / "icon_fg.png")

# --- Play Store icon 512 ---
bg.convert("RGB").resize((512, 512), Image.LANCZOS).save(STORE / "play_icon_512.png")

# --- feature graphic 1024x500: generated banner + title text ---
feat = Image.open(ICONS / "brand_feature.png").convert("RGB")
w, h = feat.size
crop_h = int(w * 500 / 1024)
top = (h - crop_h) // 2
feat = feat.crop((0, top, w, top + crop_h)).resize((1024, 500), Image.LANCZOS)
d = ImageDraw.Draw(feat)
f1 = ImageFont.truetype(str(FONT), 120)
f2 = ImageFont.truetype(str(FONT), 76)


def outlined(text, font, xy, fill):
    x, y = xy
    for dx in (-5, 0, 5):
        for dy in (-5, 0, 5):
            d.text((x + dx, y + dy), text, font=font, fill=(18, 30, 70), anchor="lm")
    d.text((x, y), text, font=font, fill=fill, anchor="lm")


outlined("WORD QUEST", f1, (40, 190), (255, 255, 255))
outlined("DAILY", f2, (46, 300), (255, 200, 46))
feat.save(STORE / "feature_graphic_1024x500.png")

# --- splash 1080x1920 (portrait) with logo + title ---
sp = Image.open(ART / "brand_splash.png").convert("RGB")
sw, sh = sp.size
scale = max(1080 / sw, 1920 / sh)
sp = sp.resize((int(sw * scale), int(sh * scale)), Image.LANCZOS)
sp = sp.crop(((sp.width - 1080) // 2, (sp.height - 1920) // 2, (sp.width - 1080) // 2 + 1080, (sp.height - 1920) // 2 + 1920))
d = ImageDraw.Draw(sp)
m2 = Image.open(ART / "logo_mark.png").convert("RGBA")
m2.thumbnail((520, 600), Image.LANCZOS)
sp.paste(m2, ((1080 - m2.width) // 2, 560 - m2.height // 2 + 180), m2)
f3 = ImageFont.truetype(str(FONT), 150)
f4 = ImageFont.truetype(str(FONT), 100)
for dx in (-6, 0, 6):
    for dy in (-6, 0, 6):
        d.text((540 + dx, 1180 + dy), "WORD QUEST", font=f3, fill=(18, 30, 70), anchor="mm")
        d.text((540 + dx, 1310 + dy), "DAILY", font=f4, fill=(18, 30, 70), anchor="mm")
d.text((540, 1180), "WORD QUEST", font=f3, fill=(255, 255, 255), anchor="mm")
d.text((540, 1310), "DAILY", font=f4, fill=(255, 200, 46), anchor="mm")
sp.save(STORE / "splash_1080x1920.png")
print("icons and store images written")
