#!/usr/bin/env python3
"""
Convert a TileSet .tres that was generated for 16px grid to 32px grid by
mapping tile keys (x:y/...) -> (x//2:y//2/...).

Usage:
  python convert_tres_16_to_32.py <input.tres>

The script will create a backup <input.tres>.bak.TIMESTAMP and overwrite the
original file with the converted version. It prints a short summary.
"""
import re
import sys
import shutil
from collections import defaultdict, OrderedDict
from datetime import datetime
import os


def load_text(path):
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def write_text(path, text):
    with open(path, "w", encoding="utf-8") as f:
        f.write(text)


def convert_tres(text):
    # Find the TileSetAtlasSource subresource block (assume first one)
    sub_re = re.compile(r"^\[sub_resource type=\"TileSetAtlasSource\".*$", re.M)
    m = sub_re.search(text)
    if not m:
        raise RuntimeError("No TileSetAtlasSource sub_resource found")
    start = m.start()
    # Find next [resource] marker after start
    resource_re = re.compile(r"^\[resource\]", re.M)
    m2 = resource_re.search(text, pos=start)
    if not m2:
        raise RuntimeError("No [resource] section found after TileSetAtlasSource")
    end = m2.start()

    header = text[:start]
    block = text[start:end]
    tail = text[end:]

    # Parse block lines
    lines = block.splitlines()

    # We'll capture the texture line and any non-key lines we want preserved
    preserved = []
    tiles = defaultdict(dict)  # (tx,ty) -> {suffix: value}
    key_line_re = re.compile(r"^(\d+):(\d+)(/[^ ]+) = (.*)$")

    original_keys = 0
    for line in lines:
        if line.strip().startswith("texture_region_size"):
            # we will override to 32x32 later; skip
            continue
        mkey = key_line_re.match(line)
        if mkey:
            x = int(mkey.group(1))
            y = int(mkey.group(2))
            suffix = mkey.group(3)
            value = mkey.group(4)
            original_keys += 1
            tx = x // 2
            ty = y // 2
            tiles[(tx, ty)][suffix] = value
        else:
            preserved.append(line)

    # Rebuild subresource block: take preserved header lines up to first key appearance
    # Ensure we include texture line and set texture_region_size = Vector2i(32, 32)
    new_block_lines = []
    # preserved likely contains the [sub_resource...] header and texture line etc.
    for pl in preserved:
        new_block_lines.append(pl)
        # If the line is the texture line, after it insert texture_region_size
        if pl.strip().startswith("texture ="):
            new_block_lines.append("texture_region_size = Vector2i(32, 32)")

    # Now output tile entries sorted by y then x for stable output
    keys_sorted = sorted(tiles.keys(), key=lambda t: (t[1], t[0]))
    new_key_count = 0
    max_tx = 0
    max_ty = 0
    for (tx, ty) in keys_sorted:
        entries = tiles[(tx, ty)]
        max_tx = max(max_tx, tx)
        max_ty = max(max_ty, ty)
        # Determine numeric alternative keys
        alt_indices = []
        for suff in entries.keys():
            if suff.startswith("/") and len(suff) > 1 and suff[1:].isdigit():
                alt_indices.append(int(suff[1:]))
        if alt_indices:
            next_id = max(alt_indices) + 1
            new_block_lines.append(f"{tx}:{ty}/next_alternative_id = {next_id}")
            new_key_count += 1
        # Write alternatives in order
        for idx in sorted(alt_indices):
            suff = f"/{idx}"
            val = entries.get(suff)
            if val is not None:
                new_block_lines.append(f"{tx}:{ty}{suff} = {val}")
                new_key_count += 1
        # Also write any other suffixes (like custom suffixes) except next_alternative_id
        for suff, val in entries.items():
            if suff == "/next_alternative_id":
                # keep user-provided if present (unlikely after our grouping)
                new_block_lines.append(f"{tx}:{ty}{suff} = {val}")
                new_key_count += 1

    # Reassemble
    new_text = header + "\n" + "\n".join(new_block_lines) + "\n" + tail
    return new_text, original_keys, new_key_count, max_tx, max_ty


def main():
    if len(sys.argv) < 2:
        print("Usage: convert_tres_16_to_32.py <path/to/TownTiles.tres>")
        sys.exit(2)
    path = sys.argv[1]
    if not os.path.isabs(path):
        path = os.path.abspath(path)
    text = load_text(path)
    out_text, orig_keys, new_keys, max_tx, max_ty = convert_tres(text)
    bak_path = path + ".bak." + datetime.now().strftime("%Y%m%d%H%M%S")
    shutil.copy2(path, bak_path)
    write_text(path, out_text)
    print(f"Backup written to: {bak_path}")
    print(f"Original matched key lines: {orig_keys}")
    print(f"New emitted key lines: {new_keys}")
    print(f"New max tile coords: maxX={max_tx}, maxY={max_ty}")


if __name__ == '__main__':
    main()
