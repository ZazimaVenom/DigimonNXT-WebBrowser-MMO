#!/usr/bin/env python3
"""Build the native clients' deterministic local assets; Python 3, no dependencies."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

MIME = {
    ".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg",
    ".webp": "image/webp", ".gif": "image/gif", ".svg": "image/svg+xml",
    ".ico": "image/x-icon", ".ogg": "audio/ogg", ".mp3": "audio/mpeg",
    ".wav": "audio/wav", ".woff": "font/woff", ".woff2": "font/woff2",
    ".ttf": "font/ttf", ".css": "text/css", ".js": "application/javascript",
    ".json": "application/json",
}


def revision_for(assets: dict) -> str:
    canonical = "".join(f"{key}\0{row['sha256']}\0{row['size']}\0{row['mime']}\n"
                        for key, row in sorted(assets.items()))
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def eligible_files(web_root: Path):
    """Do not follow symlinks, even when they point back into the web root."""
    for relative in ("assets", "html/static"):
        directory = web_root / relative
        if not directory.is_dir() or directory.is_symlink():
            continue
        for item in directory.rglob("*"):
            if item.is_symlink() or any(p.is_symlink() for p in item.parents if p != web_root):
                continue
            relative_path = item.relative_to(web_root).as_posix()
            if "\\" in relative_path or any(ord(c) < 32 or ord(c) == 127 for c in relative_path):
                continue
            if item.is_file() and item.suffix.lower() in MIME:
                yield item
    favicon = web_root / "favicon.png"
    if favicon.is_file() and not favicon.is_symlink():
        yield favicon


def build(web_root: Path, output: Path, game_version: str):
    web_root = web_root.resolve(strict=True)
    output.mkdir(parents=True, exist_ok=True)
    bundle = output / "GameAssets"
    bundle.mkdir(parents=True, exist_ok=True)
    assets = {}
    for source in sorted(eligible_files(web_root)):
        relative = source.relative_to(web_root).as_posix()
        payload = source.read_bytes()
        row = {"sha256": hashlib.sha256(payload).hexdigest(), "size": len(payload),
               "mime": MIME[source.suffix.lower()]}
        assets[relative] = row
        target = bundle / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        # Write only changed content; this avoids needless rebuilds of mobile resources.
        if not target.is_file() or hashlib.sha256(target.read_bytes()).hexdigest() != row["sha256"]:
            target.write_bytes(payload)
    # Remove stale bundle files so an update never redistributes retired assets.
    for target in sorted(bundle.rglob("*"), reverse=True):
        if target.is_file() and target.relative_to(bundle).as_posix() not in assets:
            target.unlink()
        elif target.is_dir() and not any(target.iterdir()):
            target.rmdir()
    manifest = {"protocol": 1, "game": "Digimon NXT", "game_version": game_version,
                "revision": revision_for(assets), "assets": dict(sorted(assets.items()))}
    serialized = json.dumps(manifest, ensure_ascii=False, separators=(",", ":")) + "\n"
    (output / "asset-manifest.json").write_text(serialized, encoding="utf-8")
    (bundle / "client-assets.json").write_text(serialized, encoding="utf-8")
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--web-root", type=Path, required=True,
                        help="DigimonNXT/public_html from the selected server release")
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--game-version", default="0.5.1")
    args = parser.parse_args()
    manifest = build(args.web_root, args.output, args.game_version)
    print(json.dumps({"files": len(manifest["assets"]),
                      "bytes": sum(x["size"] for x in manifest["assets"].values()),
                      "revision": manifest["revision"], "output": str(args.output.resolve())}))


if __name__ == "__main__":
    main()
