"""Run the two implementations from the same Development Build, with graphics.

Batch mode avoids desktop-focus transitions. No -nographics flag is used.
PlayerLoop includes frame-cap waits; compare function metrics as well as frames.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("optimization", Path(__file__).with_name("Run-Optimization.py"))
optimization = importlib.util.module_from_spec(spec)
spec.loader.exec_module(optimization)


def completed_scenarios(earlier, current):
    # A completed scenario at a different frame cap or repetition count does not
    # satisfy this capture request. Missing provenance also cannot prove a match.
    for key in ["managedAssemblySha256", "exeSha256", "environment", "repetitions",
                "frameRate", "resolution", "vSync"]:
        if key not in earlier or earlier[key] != current[key]:
            raise ValueError("Cannot resume measurements with different or missing " + key)
    return {item["scenario"] for item in earlier["attempts"] if item.get("phase") == "Completed"}


def run_scenario(args, journal, directory, save, exe, name, round_number):
    for attempt in range(1, args.attempts + 1):
        identifier = name + ("_round" + str(round_number) if args.rounds > 1 else "") + "_attempt" + str(attempt)
        result_path = directory / (identifier + ".json")
        log_path = directory / (identifier + ".log")
        arguments = [str(exe), "-batchmode", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720",
                     "-logFile", str(log_path), "--optimization-scenarios", name,
                     "--optimization-repetitions", str(args.repetitions), "--optimization-frame-rate", str(args.frame_rate),
                     "--optimization-result", str(result_path),
                     "--optimization-seed-offset", str(round_number - 1)]
        if args.screenshots:
            arguments.append("--optimization-screenshots")
        item = {"id": identifier, "scenario": name, "attempt": attempt, "round": round_number, "phase": "Running", "arguments": arguments}
        journal["attempts"].append(item)
        save()
        print("Starting " + identifier, flush=True)
        # Keep helpers hidden; graphics remains enabled in the standalone player.
        process = subprocess.Popen(arguments, cwd=exe.parent, creationflags=subprocess.CREATE_NO_WINDOW,
                                   env={key.upper(): value for key, value in os.environ.items()})
        item["processId"] = process.pid
        save()
        deadline = time.monotonic() + 1800
        while process.poll() is None:
            if time.monotonic() > deadline:
                process.kill()
                process.wait()
                item["failure"] = "Player exceeded 1800 seconds; partial data is diagnostic only."
                break
            time.sleep(2)
        item["exitCode"] = process.returncode
        if result_path.exists():
            result = json.loads(result_path.read_text(encoding="utf-8-sig"))
            item.update({"phase": result["status"], "runStatus": result["status"],
                         "failure": result.get("failure"), "runDirectory": result.get("runDirectory")})
            manifest = Path(item["runDirectory"]) / "run.json" if item.get("runDirectory") else None
            if manifest is not None and manifest.exists():
                metadata = json.loads(manifest.read_text(encoding="utf-8-sig"))
                (directory / (identifier + "_run.json")).write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
        else:
            item["phase"] = "Failed"
            item.setdefault("failure", "Player exited without a result manifest; inspect " + str(log_path))
        if process.returncode != 0:
            item["phase"] = "Failed"
        save()
        print(json.dumps({key: item.get(key) for key in ["scenario", "phase", "failure", "runDirectory"]}, ensure_ascii=False), flush=True)
        if item["phase"] == "Completed":
            break


def load_build_meta(exe, journal, allow_missing):
    # Provenance: tie every measurement to the commit the binary was built from.
    path = exe.parent / "build-meta.json"
    if not path.exists():
        if allow_missing:
            return None
        raise RuntimeError("No build-meta.json next to the executable. Run Write-BuildMeta.py right after building, "
                           "or pass --allow-missing-build-meta to measure an unverified build.")
    meta = json.loads(path.read_text(encoding="utf-8-sig"))
    for key in ["exeSha256", "managedAssemblySha256"]:
        if meta.get(key) != journal[key]:
            raise RuntimeError("build-meta.json " + key + " does not match this executable; it was written for another build.")
    return meta


def round_order(names, round_number):
    # Alternate the order every round so neither version always runs first
    # (thermal state and caches would otherwise favour one side).
    return list(names) if round_number % 2 == 1 else list(reversed(names))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=ROOT / "Logs/Optimization/Build/PotionOptimization.exe")
    parser.add_argument("--suite", choices=["core", "gc", "combine", "enemy", "regression", "all"], default="core")
    parser.add_argument("--scenario", action="append")
    parser.add_argument("--repetitions", type=int, default=3)
    parser.add_argument("--frame-rate", type=int, default=120, help="Target FPS; -1 removes the cap to expose CPU headroom")
    parser.add_argument("--rounds", type=int, default=1,
                        help="Repeat the whole list this many times in alternating order. Use with --repetitions 1 "
                             "to interleave original and improved runs; each round uses seed offset round-1")
    parser.add_argument("--screenshots", action="store_true", help="Save one PNG per scenario right after the measured interval")
    parser.add_argument("--allow-missing-build-meta", action="store_true")
    parser.add_argument("--journal", type=Path)
    parser.add_argument("--attempts", type=int, default=1)
    parser.add_argument("--exclude-journal", type=Path)
    args = parser.parse_args()
    exe = args.exe.resolve(strict=True)
    if not 1 <= args.repetitions <= 20 or args.frame_rate == 0 or args.frame_rate < -1 or args.attempts < 1:
        parser.error("Invalid repetitions, frame rate or attempts")
    if not 1 <= args.rounds <= 50:
        parser.error("rounds must be between 1 and 50")
    if args.rounds > 1 and args.exclude_journal:
        parser.error("--exclude-journal cannot be combined with --rounds")
    path = args.journal or ROOT / "Logs/Optimization" / ("player_" + args.suite + "_" + time.strftime("%Y%m%d_%H%M%S") + ".json")
    path = path.resolve()
    if path.exists():
        raise RuntimeError("Use a new journal path; existing evidence is preserved.")
    directory = path.parent / (path.stem + "_runs")
    directory.mkdir(parents=True, exist_ok=True)
    journal = {"environment": "development-player-batch-graphics", "executable": str(exe),
               "exeSha256": hashlib.sha256(exe.read_bytes()).hexdigest(),
               "managedAssemblySha256": hashlib.sha256((exe.parent / (exe.stem + "_Data") / "Managed/Assembly-CSharp.dll").read_bytes()).hexdigest(),
               "suite": args.suite, "repetitions": args.repetitions, "frameRate": args.frame_rate,
               "resolution": [1280, 720], "vSync": 0, "rounds": args.rounds, "screenshots": args.screenshots,
               "attempts": []}
    journal["buildMeta"] = load_build_meta(exe, journal, args.allow_missing_build_meta)
    if journal["buildMeta"] and journal["buildMeta"].get("gitDirty"):
        print("WARNING: build was made from uncommitted sources (" + str(journal["buildMeta"].get("dirtyFileCount")) + " files).", flush=True)
    save = lambda: path.write_text(json.dumps(journal, ensure_ascii=False, indent=2), encoding="utf-8")
    excluded = set()
    if args.exclude_journal:
        earlier = json.loads(args.exclude_journal.read_text(encoding="utf-8"))
        excluded = completed_scenarios(earlier, journal)
    save()
    names = list(args.scenario or optimization.scenarios(args.suite))
    for round_number in range(1, args.rounds + 1):
        for name in round_order(names, round_number):
            if name in excluded:
                continue
            run_scenario(args, journal, directory, save, exe, name, round_number)
    key = lambda item: (item["scenario"], item.get("round", 1))
    successful = {key(item) for item in journal["attempts"] if item["phase"] == "Completed"}
    unresolved = sorted("%s (round %d)" % pair for pair in {key(item) for item in journal["attempts"]} - successful)
    print(json.dumps({"journal": str(path), "completed": len(successful), "unresolved": unresolved}), flush=True)
    return 2 if unresolved else 0


if __name__ == "__main__":
    raise SystemExit(main())
