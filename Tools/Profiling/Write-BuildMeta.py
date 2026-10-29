"""Record which source revision a Development Build came from.

Run right after building and before measuring. The measurement runner refuses a
build without this file so results stay traceable to a commit. Only Assets,
Packages and ProjectSettings count for the dirty check; Logs is ignored by Git.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE_PATHS = ["Assets", "Packages", "ProjectSettings"]


def git(*arguments):
    return subprocess.run(["git", *arguments], cwd=ROOT, capture_output=True, text=True, check=True).stdout


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def build_meta(exe):
    dirty = [line for line in git("status", "--porcelain", "--", *SOURCE_PATHS).splitlines() if line.strip()]
    return {"gitHead": git("rev-parse", "HEAD").strip(), "gitBranch": git("rev-parse", "--abbrev-ref", "HEAD").strip(),
            "gitDirty": bool(dirty), "dirtyFiles": dirty[:50], "dirtyFileCount": len(dirty),
            "writtenUtc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            "exeSha256": sha256(exe),
            "managedAssemblySha256": sha256(exe.parent / (exe.stem + "_Data") / "Managed/Assembly-CSharp.dll")}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, required=True)
    parser.add_argument("--allow-dirty", action="store_true",
                        help="Record a build made from uncommitted sources; the dirty flag stays in the file.")
    args = parser.parse_args()
    exe = args.exe.resolve(strict=True)
    meta = build_meta(exe)
    if meta["gitDirty"] and not args.allow_dirty:
        raise SystemExit("Uncommitted source changes (" + str(meta["dirtyFileCount"]) +
                         " files). Commit them and rebuild, or pass --allow-dirty to record the build as dirty.")
    target = exe.parent / "build-meta.json"
    if target.exists():
        raise SystemExit("build-meta.json already exists; rebuild into a new folder to keep earlier evidence.")
    target.write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"written": str(target), "gitHead": meta["gitHead"], "gitDirty": meta["gitDirty"]}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
