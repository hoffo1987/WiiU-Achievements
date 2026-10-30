"""Prepare per-game achievement art from the owner's full-size icon PNGs.

Optional developer tool (Python 3 + Pillow + NumPy + SciPy); the app never runs it.
The committed 256 px PNGs under assets/games/ are what build.ps1 embeds.

    python -I tools/prepare-game-art.py <folder with the original PNGs> <game id>

Every supplied icon shares one frame template whose metal follows the rarity.
The script:
  * resizes each icon whose file name matches an achievement title to
    assets/games/<game>/icons/<achievement id>.png;
  * rebuilds the empty frame per rarity (bronze/silver/gold, platinum derived
    from silver) by removing the varying centre from the per-rarity median;
  * extracts the shared sky disc, so code-drawn glyphs sit on the same
    background as the supplied artwork.
"""
import os
import sys
import unicodedata
import xml.etree.ElementTree as ET

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

SIZE = 256
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RULE_FILES = ["achievements.xml", "achievements-story.xml", "achievements-planned.xml"]
# The owner's file names follow the titles; this one differs by a preposition.
ALIASES = {"uno sguardo al mare": "uno sguardo sul mare"}


def key(text):
    text = unicodedata.normalize("NFC", text).strip().lower()
    return ALIASES.get(text, text)


def catalog():
    titles, rarity = {}, {}
    for name in RULE_FILES:
        for node in ET.parse(os.path.join(ROOT, name)).getroot().iter("achievement"):
            titles[key(node.get("title"))] = node.get("id")
    for node in ET.parse(os.path.join(ROOT, "achievement-presentation.xml")).getroot().iter("entry"):
        rarity[node.get("id")] = node.get("rarity")
    return titles, rarity


def load(path):
    return np.asarray(Image.open(path).convert("RGBA")).astype(np.float32)


def shrink(rgba):
    # Premultiplied resampling avoids dark fringes along transparent edges.
    image = Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), "RGBA").convert("RGBa")
    return image.resize((SIZE, SIZE), Image.LANCZOS).convert("RGBA")


def sky_like(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return (g > 175) & (b > 210) & (b >= r + 8)


def main(source, game):
    titles, rarity = catalog()
    target = os.path.join(ROOT, "assets", "games", game)
    os.makedirs(os.path.join(target, "icons"), exist_ok=True)
    groups = {}
    for name in sorted(os.listdir(source)):
        if not name.lower().endswith(".png"):
            continue
        rule = titles.get(key(os.path.splitext(name)[0]))
        if rule is None:
            print("skipped (no matching title):", name)
            continue
        image = load(os.path.join(source, name))
        shrink(image).save(os.path.join(target, "icons", rule + ".png"), optimize=True)
        groups.setdefault(rarity[rule], []).append(image)
        print("icon", rule, rarity[rule], "<-", name)

    size = next(iter(groups.values()))[0].shape[0]
    centre = size // 2
    # The centre varies between icons of one rarity; the frame and its boat do not.
    medians, varies = {}, np.zeros((size, size), bool)
    for tier, images in groups.items():
        stack = np.stack(images)
        medians[tier] = np.median(stack, axis=0)
        if len(images) > 1:
            spread = (stack[..., :3].max(axis=0) - stack[..., :3].min(axis=0)).max(axis=2)
            varies |= spread > 40
    reference = medians["bronze"]
    opaque = reference[..., 3] > 200
    candidate = (varies | sky_like(reference[..., :3])) & opaque
    labels, _ = ndimage.label(candidate)
    # Constant, non-sky islands inside the disc (bubbles) stay part of the frame.
    inner = ndimage.binary_opening(labels == labels[centre, centre], iterations=2)
    soft = np.asarray(Image.fromarray((inner * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.6))).astype(np.float32) / 255

    if "silver" in medians and "platinum" not in medians:
        silver = medians["silver"].copy()
        rgb = silver[..., :3]
        lum = rgb.mean(axis=2, keepdims=True)
        grey = (rgb.max(axis=2) - rgb.min(axis=2) < 48)[..., None] & (lum > 70)
        pearl = np.concatenate([lum * 0.94 + 18, lum * 0.98 + 14, lum * 1.0 + 22], axis=2)
        medians["platinum"] = np.concatenate([np.where(grey, pearl, rgb), silver[..., 3:]], axis=2)
    for tier, median in medians.items():
        frame = median.copy()
        frame[..., 3] *= 1 - soft
        shrink(frame).save(os.path.join(target, "frame-" + tier + ".png"), optimize=True)
        print("frame", tier)

    # Sky: per-row median of the median background (the gradient differs slightly
    # between icons, so it is not "constant"), filled into a disc under the frame.
    background = sky_like(reference[..., :3]) & inner
    rgb = reference[..., :3]
    rows = np.zeros((size, 3), np.float32)
    for y in range(size):
        picked = background[y]
        if picked.sum() > 20:
            rows[y] = np.median(rgb[y, picked], axis=0)
    known = np.where(rows.sum(axis=1) > 0)[0]
    for channel in range(3):
        rows[:, channel] = np.interp(np.arange(size), known, rows[known, channel])
    rows = ndimage.gaussian_filter1d(rows, 18, axis=0)
    distance = np.sqrt(((np.argwhere(inner) - centre) ** 2).sum(axis=1))
    radius = np.percentile(distance, 99.5) + size * 0.02
    sky = np.zeros((size, size, 4), np.float32)
    sky[..., :3] = rows[:, None, :]
    mask = Image.new("L", (size * 4, size * 4), 0)
    ImageDraw.Draw(mask).ellipse([(centre - radius) * 4, (centre - radius) * 4, (centre + radius) * 4, (centre + radius) * 4], fill=255)
    sky[..., 3] = np.asarray(mask.resize((size, size), Image.LANCZOS)).astype(np.float32)
    shrink(sky).save(os.path.join(target, "sky.png"), optimize=True)
    print("sky radius", round(radius / size, 4), "of the icon")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2])
