"""Cut a transparent sprite sheet into single sprites.

Usage: python Tools/slice_sheet.py <sheet> <cols> <rows> <name1,name2,...>
Finds the items by connected alpha blobs (robust to uneven layouts), removes the soft glow around each item,
and writes Assets/Art/Sprites/<name>.png. Items are named in reading order (rows top to bottom, left to right).
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parent.parent
SHEETS = ROOT / "Assets" / "Art" / "Sheets"
OUT = ROOT / "Assets" / "Resources" / "Art"


def find_items(alpha, n):
    mask = alpha > 150
    mask = ndi.binary_closing(mask, iterations=3)
    lab, cnt = ndi.label(mask)
    if cnt == 0:
        raise SystemExit("no items found")
    sizes = ndi.sum(mask, lab, range(1, cnt + 1))
    order = np.argsort(sizes)[::-1]
    keep = [i + 1 for i in order[:n]]
    # attach every other blob (sparkles, small parts) to the nearest kept blob
    objs = ndi.find_objects(lab)
    centers = {i: ndi.center_of_mass(mask, lab, i) for i in range(1, cnt + 1)}
    group = {k: [k] for k in keep}
    for i in range(1, cnt + 1):
        if i in keep:
            continue
        if sizes[i - 1] < 30:
            continue
        best = min(keep, key=lambda k: (centers[k][0] - centers[i][0]) ** 2 + (centers[k][1] - centers[i][1]) ** 2)
        group[best].append(i)
    items = []
    for k, ids in group.items():
        m = np.isin(lab, ids)
        ys, xs = np.where(m)
        items.append((k, m, (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)))
    return items


def reading_order(items, rows):
    if rows == 0:
        return sorted(items, key=lambda it: (round((it[2][1] + it[2][3]) / 20), (it[2][0] + it[2][2]) / 2))
    cy = sorted(items, key=lambda it: (it[2][1] + it[2][3]) / 2)
    per = int(np.ceil(len(items) / rows))
    out = []
    for r in range(rows):
        row = cy[r * per:(r + 1) * per]
        row.sort(key=lambda it: (it[2][0] + it[2][2]) / 2)
        out += row
    return out


def main(sheet, cols, rows, names):
    im = Image.open(SHEETS / f"{sheet}.png").convert("RGBA")
    arr = np.array(im)
    items = reading_order(find_items(arr[:, :, 3], len(names)), rows)
    OUT.mkdir(parents=True, exist_ok=True)
    for (k, m, box), name in zip(items, names):
        grow = ndi.binary_dilation(m, iterations=6)  # keep the soft edge, drop the glow
        x0, y0, x1, y1 = box
        pad = 8
        x0, y0 = max(0, x0 - pad), max(0, y0 - pad)
        x1, y1 = min(arr.shape[1], x1 + pad), min(arr.shape[0], y1 + pad)
        crop = arr[y0:y1, x0:x1].copy()
        g = grow[y0:y1, x0:x1]
        crop[~g, 3] = 0
        # fade very faint pixels
        a = crop[:, :, 3].astype(np.int32)
        crop[:, :, 3] = np.where(a < 20, 0, a).astype(np.uint8)
        Image.fromarray(crop).save(OUT / f"{name}.png")
        print(f"{name}: {x1 - x0}x{y1 - y0}")


if __name__ == "__main__":
    s, c, r, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4].split(",")
    main(s, c, r, n)
