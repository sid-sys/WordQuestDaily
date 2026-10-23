// Cuts a sprite sheet made in ChatGPT into single PNGs (no Python needed).
//   node Tools/cut_sheet.js <sheet.png> <name1,name2,...> [outDir] [--preview]
// - removes the white / grey checkerboard background (flood fill from the edges, plus enclosed checkerboard holes)
// - finds every item as a connected blob (near blobs are merged), sorts them in reading order (rows, then left to right)
// - use the name "-" for an empty cell in the list so the order still lines up
// - writes <outDir>/<name>.png (default Assets/Resources/Art). With --preview writes into Temp/cut_preview instead.
const fs = require("fs");
const path = require("path");
const { PNG } = require(path.join(__dirname, "node_tmp", "node_modules", "pngjs"));

const args = process.argv.slice(2).filter((a) => !a.startsWith("--"));
const preview = process.argv.includes("--preview");
const holes = process.argv.includes("--holes");   // sheets with a baked-in checkerboard: also clear checkerboard seen through handles / rings
const [sheetPath, namesArg, outArg] = args;
if (!sheetPath || !namesArg) { console.error("usage: node cut_sheet.js sheet.png name1,name2,... [outDir] [--preview]"); process.exit(1); }
const names = namesArg.split(",");
const root = path.resolve(__dirname, "..");
const outDir = preview ? path.join(root, "Temp", "cut_preview") : path.resolve(outArg || path.join(root, "Assets", "Resources", "Art"));
fs.mkdirSync(outDir, { recursive: true });

const png = PNG.sync.read(fs.readFileSync(sheetPath));
const W = png.width, H = png.height, d = png.data;
const idx = (x, y) => (y * W + x) * 4;

// ---- 1. background mask ----
const isBgLike = (i) => {
  const a = d[i + 3];
  if (a < 12) return true;
  const r = d[i], g = d[i + 1], b = d[i + 2];
  return Math.min(r, g, b) >= 185 && Math.max(r, g, b) - Math.min(r, g, b) <= 24;   // white or light grey (checkerboard squares)
};
const bg = new Uint8Array(W * H);
const stack = [];
const push = (x, y) => { const p = y * W + x; if (!bg[p] && isBgLike(p * 4)) { bg[p] = 1; stack.push(p); } };
for (let x = 0; x < W; x++) { push(x, 0); push(x, H - 1); }
for (let y = 0; y < H; y++) { push(0, y); push(W - 1, y); }
while (stack.length) {
  const p = stack.pop(), x = p % W, y = (p / W) | 0;
  if (x > 0) push(x - 1, y); if (x < W - 1) push(x + 1, y); if (y > 0) push(x, y - 1); if (y < H - 1) push(x, y + 1);
}
// enclosed checkerboard holes (handles, rings): bg-like areas not touching the border that show BOTH white and grey squares
let G = 0;
if (holes) {
  const hist = new Map();
  for (let p = 0; p < W * H; p++) if (bg[p]) { const mn = Math.min(d[p*4], d[p*4+1], d[p*4+2]); if (mn >= 150 && mn <= 245) hist.set(mn, (hist.get(mn) || 0) + 1); }
  let best = 0; for (const [k, v] of hist) if (v > best) { best = v; G = k; }
  console.log("checkerboard grey = " + G);
}
if (holes) {
  const seen = new Uint8Array(W * H);
  for (let s = 0; s < W * H; s++) {
    if (bg[s] || seen[s] || !isBgLike(s * 4)) continue;
    const comp = [s]; seen[s] = 1; let white = 0, grey = 0;
    for (let k = 0; k < comp.length; k++) {
      const p = comp[k], x = p % W, y = (p / W) | 0, i = p * 4;
      const mn = Math.min(d[i], d[i + 1], d[i + 2]);
      if (mn >= 246) white++; else if (Math.abs(mn - G) <= 5 && Math.max(d[i], d[i + 1], d[i + 2]) - mn <= 8) grey++;
      const nb = [x > 0 ? p - 1 : -1, x < W - 1 ? p + 1 : -1, y > 0 ? p - W : -1, y < H - 1 ? p + W : -1];
      for (const q of nb) if (q >= 0 && !seen[q] && !bg[q] && isBgLike(q * 4)) { seen[q] = 1; comp.push(q); }
    }
    const n = comp.length, gf = grey / n, wf = white / n;
    if (n < 6000 && gf > 0.35 && wf > 0.35) for (const p of comp) bg[p] = 1;
  }
}
// soften: any remaining pixel next to background that is light gets removed (fringe)
const fg = new Uint8Array(W * H);
for (let p = 0; p < W * H; p++) fg[p] = bg[p] ? 0 : 1;
for (let pass = 0; pass < 2; pass++) {
  const kill = [];
  for (let y = 1; y < H - 1; y++) for (let x = 1; x < W - 1; x++) {
    const p = y * W + x; if (!fg[p]) continue;
    if (fg[p - 1] && fg[p + 1] && fg[p - W] && fg[p + W]) continue;
    const i = p * 4;
    if (Math.min(d[i], d[i + 1], d[i + 2]) >= 205 && Math.max(d[i], d[i + 1], d[i + 2]) - Math.min(d[i], d[i + 1], d[i + 2]) <= 30) kill.push(p);
  }
  for (const p of kill) fg[p] = 0;
}

// ---- 2. blobs (merge near parts with a dilated mask) ----
const mArg = process.argv.find((a) => a.startsWith("--merge="));
const R = mArg ? +mArg.split("=")[1] : 14;   // how far apart parts of one item may be (px); lower it when items nearly touch
const dil = new Uint8Array(W * H);
{
  const tmp = new Uint8Array(W * H);
  for (let y = 0; y < H; y++) { let last = -1e9; for (let x = 0; x < W; x++) { if (fg[y * W + x]) last = x; if (x - last <= R) tmp[y * W + x] = 1; } last = 1e9; for (let x = W - 1; x >= 0; x--) { if (fg[y * W + x]) last = x; if (last - x <= R) tmp[y * W + x] = 1; } }
  for (let x = 0; x < W; x++) { let last = -1e9; for (let y = 0; y < H; y++) { if (tmp[y * W + x]) last = y; if (y - last <= R) dil[y * W + x] = 1; } last = 1e9; for (let y = H - 1; y >= 0; y--) { if (tmp[y * W + x]) last = y; if (last - y <= R) dil[y * W + x] = 1; } }
}
const lab = new Int32Array(W * H).fill(-1);
const blobs = [];
for (let s = 0; s < W * H; s++) {
  if (!dil[s] || lab[s] >= 0) continue;
  const id = blobs.length; const q = [s]; lab[s] = id; let minx = W, miny = H, maxx = 0, maxy = 0, area = 0;
  for (let k = 0; k < q.length; k++) {
    const p = q[k], x = p % W, y = (p / W) | 0;
    if (fg[p]) { area++; if (x < minx) minx = x; if (x > maxx) maxx = x; if (y < miny) miny = y; if (y > maxy) maxy = y; }
    if (x > 0 && dil[p - 1] && lab[p - 1] < 0) { lab[p - 1] = id; q.push(p - 1); }
    if (x < W - 1 && dil[p + 1] && lab[p + 1] < 0) { lab[p + 1] = id; q.push(p + 1); }
    if (y > 0 && dil[p - W] && lab[p - W] < 0) { lab[p - W] = id; q.push(p - W); }
    if (y < H - 1 && dil[p + W] && lab[p + W] < 0) { lab[p + W] = id; q.push(p + W); }
  }
  blobs.push({ id, minx, miny, maxx, maxy, area });
}
let items = blobs.filter((b) => b.area > 900);
console.log(`found ${items.length} items, expected ${names.filter((n) => n !== "-").length}`);
// reading order: group by rows
items.forEach((b) => { b.cy = (b.miny + b.maxy) / 2; b.cx = (b.minx + b.maxx) / 2; });
items.sort((a, b) => a.cy - b.cy);
const rows = []; const tol = Math.max(...items.map((b) => b.maxy - b.miny)) * 0.55;
for (const b of items) { const row = rows.find((r) => Math.abs(r.cy - b.cy) < tol); if (row) { row.items.push(b); row.cy = row.items.reduce((s, i) => s + i.cy, 0) / row.items.length; } else rows.push({ cy: b.cy, items: [b] }); }
rows.sort((a, b) => a.cy - b.cy);
const ordered = rows.flatMap((r) => r.items.sort((a, b) => a.cx - b.cx));
console.log("rows: " + rows.map((r) => r.items.length).join(","));

// ---- 3. write sprites ----
const pad = 6;
ordered.forEach((b, n) => {
  const name = names[n];
  if (!name || name === "-") return;
  const x0 = Math.max(0, b.minx - pad), y0 = Math.max(0, b.miny - pad), x1 = Math.min(W - 1, b.maxx + pad), y1 = Math.min(H - 1, b.maxy + pad);
  const w = x1 - x0 + 1, h = y1 - y0 + 1; const out = new PNG({ width: w, height: h });
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const sp = (y0 + y) * W + (x0 + x), si = sp * 4, oi = (y * w + x) * 4;
    const keep = fg[sp] && lab[sp] === b.id;
    out.data[oi] = d[si]; out.data[oi + 1] = d[si + 1]; out.data[oi + 2] = d[si + 2]; out.data[oi + 3] = keep ? 255 : 0;
  }
  fs.writeFileSync(path.join(outDir, name + ".png"), PNG.sync.write(out));
  console.log(`${n + 1}. ${name}  ${w}x${h}`);
});
