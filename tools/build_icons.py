import argparse
import base64
import json
import os
import struct
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_SVG_DIR = os.path.join(HERE, "devicon-src")
DEFAULT_OUT = os.path.join(os.path.dirname(HERE), "assets_pack")


def load_manifest():
    with open(os.path.join(HERE, "icons.json"), encoding="utf-8") as f:
        return json.load(f)


def ensure_devicon(manifest):
    cfg = manifest["devicon"]
    svg_dir = DEFAULT_SVG_DIR
    git_dir = os.path.join(svg_dir, "devicon")
    if os.path.exists(os.path.join(git_dir, ".git")):
        return git_dir
    os.makedirs(svg_dir, exist_ok=True)
    subprocess.run(
        ["git", "clone", "--depth", "1", "--branch", cfg["ref"], cfg["url"], git_dir],
        check=True,
    )
    return git_dir


def find_svg(devicon_dir, slug, variant):
    candidates = [
        f"icons/{slug}/{slug}-{variant}.svg",
        f"icons/{slug}/{slug}-plain.svg",
        f"icons/{slug}/{slug}-original.svg",
        f"icons/{slug}/{slug}-line.svg",
    ]
    for rel in candidates:
        path = os.path.join(devicon_dir, rel)
        if os.path.exists(path):
            return path
    return None


def render(svg_text, size):
    import resvg_py

    data = resvg_py.svg_to_bytes(svg_string=svg_text, width=size, height=size)
    if isinstance(data, (bytes, bytearray)):
        return bytes(data)
    if isinstance(data, str):
        try:
            return base64.b64decode(data)
        except Exception:
            raise RuntimeError("unexpected return type from resvg_py: str (not base64 png)")
    raise RuntimeError(f"unexpected return type from resvg_py: {type(data)}")


def png_size(data):
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", data[16:24])


def main():
    parser = argparse.ArgumentParser(description="Build the Discord asset pack as 1024x1024 PNGs")
    parser.add_argument("--out", default=DEFAULT_OUT)
    parser.add_argument("--size", type=int, default=None)
    args = parser.parse_args()

    manifest = load_manifest()
    size = args.size or manifest.get("size", 1024)
    devicon_dir = ensure_devicon(manifest) if any(i.get("variant") == "original" and i.get("slug") for i in manifest["icons"]) else None

    custom_dir = os.path.join(HERE, "svg")
    os.makedirs(args.out, exist_ok=True)

    rendered, skipped = 0, []
    for icon in manifest["icons"]:
        key = icon["key"]
        if icon.get("variant") == "custom":
            src = os.path.join(custom_dir, f"{key}.svg")
        else:
            src = find_svg(devicon_dir, icon["slug"], icon["variant"]) if devicon_dir else None

        if not src or not os.path.exists(src):
            skipped.append((key, icon.get("slug", key)))
            print(f"SKIP  {key}: no SVG found")
            continue

        with open(src, encoding="utf-8") as f:
            svg_text = f.read()

        png = render(svg_text, size)
        w, h = png_size(png) if png[:8] == b"\x89PNG\r\n\x1a\n" else (None, None)
        if w != size or h != size:
            print(f"WARN {key}: rendered {w}x{h} (expected {size})")
        with open(os.path.join(args.out, f"{key}.png"), "wb") as f:
            f.write(png)
        rendered += 1
        print(f"OK    {key}.png ({len(png)} bytes, {w}x{h})")

    print()
    print(f"Rendered {rendered} icons to {args.out}")
    if skipped:
        print("Missing icons (map to a generic fallback or provide custom SVG in tools/svg/):")
        for key, slug in skipped:
            print(f"  - {key} (slug: {slug})")


if __name__ == "__main__":
    try:
        main()
    except ImportError:
        print("Missing resvg_py. Run:  pip install resvg-py")
        sys.exit(1)
