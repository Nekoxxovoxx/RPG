"""Bake fire-only pixel hitboxes without making the source textures CPU-readable.

Run from any directory with Python, Pillow and numpy. The preview is diagnostic;
source sprites and their import settings are never modified.
"""

import argparse
import json
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
SPRITES = ROOT / "Assets/Graphics/boss/boss/individual sprites/09_demon_fire_breath"
OUTPUT = ROOT / "Assets/Data/Demon fire breath hitboxes.json"
PREVIEW = ROOT / "Logs/CombatValidation/fire-hitboxes.png"


def extract_mask(image, body_mask):
    rgba = np.asarray(image.convert("RGBA"))
    candidate = (rgba[:, :, 3] >= 128) & ~body_mask
    # The original art faces left. Exclude the head and its right-hand torch.
    candidate[:, 127:] = False
    hot = (rgba[:, :, 0] >= 190) & (rgba[:, :, 1] >= 160) & (rgba[:, :, 2] < 80)
    remaining = candidate.copy()
    result = np.zeros(candidate.shape, dtype=bool)
    height, width = candidate.shape
    for row, column in zip(*np.nonzero(candidate)):
        if not remaining[row, column]:
            continue
        queue = deque([(int(column), int(row))])
        remaining[row, column] = False
        component = []
        while queue:
            x, y = queue.popleft()
            component.append((x, y))
            for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if 0 <= nx < width and 0 <= ny < height and remaining[ny, nx]:
                    remaining[ny, nx] = False
                    queue.append((nx, ny))
        # Cold smoke and isolated sparks are visual-only.
        if len(component) >= 8 and sum(bool(hot[y, x]) for x, y in component) >= 3:
            for x, y in component:
                result[y, x] = True
    return result


def pack_rectangles(mask):
    """Merge identical horizontal pixel runs; never bridge a transparent gap."""
    rectangles = []
    active = {}
    for y, row in enumerate(mask):
        padded = np.pad(row.astype(np.int8), 1)
        starts = np.flatnonzero(np.diff(padded) == 1)
        ends = np.flatnonzero(np.diff(padded) == -1)
        next_active = {}
        for start, end in zip(starts, ends):
            key = (int(start), int(end))
            rect = active.pop(key, None)
            if rect is None:
                rect = dict(x=int(start), y=y, width=int(end - start), height=1)
            else:
                rect["height"] += 1
            next_active[key] = rect
        rectangles.extend(active.values())
        active = next_active
    rectangles.extend(active.values())
    return rectangles


def bake(check_only=False):
    paths = sorted(SPRITES.glob("*.png"), key=lambda p: int(p.stem.rsplit("_", 1)[1]))
    assert len(paths) == 21, "Review the extraction settings when replacing this animation."
    images = [Image.open(path).convert("RGBA") for path in paths]
    # These are the same breathing pose without the breath, not a guessed box.
    body = np.maximum(np.asarray(images[5])[:, :, 3], np.asarray(images[20])[:, :, 3])
    body_mask = np.asarray(Image.fromarray(body).filter(ImageFilter.MaxFilter(3))) >= 128
    frames = []
    masks = []
    for index, (path, image) in enumerate(zip(paths, images)):
        mask = extract_mask(image, body_mask) if 6 <= index <= 16 else np.zeros(body.shape, dtype=bool)
        rectangles = pack_rectangles(mask)
        reconstructed = np.zeros_like(mask)
        for rect in rectangles:
            x, y, w, h = (rect[k] for k in ("x", "y", "width", "height"))
            reconstructed[y:y + h, x:x + w] = True
        assert np.array_equal(mask, reconstructed), path.name
        assert not np.any(mask & body_mask), "Body/torch pixels entered the hitbox."
        assert not np.any(mask & (np.asarray(image)[:, :, 3] < 128)), "Transparent pixels entered the hitbox."
        if 7 <= index <= 16:
            assert mask.sum() > 0, f"Missing visible breath: {path.name}"
        frames.append(dict(spriteName=path.stem, width=image.width, height=image.height, rectangles=rectangles))
        masks.append(mask)
        print(f"{path.stem}: {int(mask.sum())} pixels, {len(rectangles)} rectangles")
    payload = json.dumps(dict(frames=frames), separators=(",", ":")) + "\n"
    if check_only:
        assert OUTPUT.read_text(encoding="utf-8") == payload, "Baked hitboxes are stale; rerun the baker."
        print("PASS: all 21 frames match the source sprites and exclude body/transparent pixels.")
        return
    OUTPUT.write_text(payload, encoding="utf-8")
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview = Image.new("RGB", (288 * 4, 184 * 6), (34, 34, 38))
    draw = ImageDraw.Draw(preview)
    for index, (image, mask) in enumerate(zip(images, masks)):
        x, y = (index % 4) * 288, (index // 4) * 184
        preview.paste(image, (x, y + 20), image)
        overlay = Image.new("RGBA", image.size, (0, 220, 255, 0))
        overlay.putalpha(Image.fromarray(mask.astype(np.uint8) * 100))
        preview.paste(overlay, (x, y + 20), overlay)
        draw.text((x + 8, y + 4), f"Frame {index + 1:02d} / cyan = damage", fill="white")
    preview.save(PREVIEW)
    print(f"Wrote {OUTPUT.relative_to(ROOT)} and {PREVIEW.relative_to(ROOT)}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    bake(parser.parse_args().check)
