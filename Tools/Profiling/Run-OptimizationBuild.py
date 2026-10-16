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


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=ROOT / "Logs/Optimization/Build/PotionOptimization.exe")
    parser.add_argument("--suite", choices=["core", "gc", "combine", "enemy", "regression", "all"], default="core")
    parser.add_argument("--scenario", action="append")
    parser.add_argument("--repetitions", type=int, default=3)
    parser.add_argument("--frame-rate", type=int, default=120)
    parser.add_argument("--journal", type=Path)
    parser.add_argument("--attempts", type=int, default=1)
    parser.add_argument("--exclude-journal", type=Path)
    args = parser.parse_args()
    exe = args.exe.resolve(strict=True)
    if not 1 <= args.repetitions <= 20 or args.frame_rate < 1 or args.attempts < 1:
        parser.error("Invalid repetitions, frame rate or attempts")
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
               "resolution": [1280, 720], "vSync": 0, "attempts": []}
    save = lambda: path.write_text(json.dumps(journal, ensure_ascii=False, indent=2), encoding="utf-8")
    excluded = set()
    if args.exclude_journal:
        earlier = json.loads(args.exclude_journal.read_text(encoding="utf-8"))
        excluded = {item["scenario"] for item in earlier["attempts"] if item.get("phase") == "Completed"}
    save()
    for name in args.scenario or optimization.scenarios(args.suite):
        if name in excluded:
            continue
        for attempt in range(1, args.attempts + 1):
            identifier = name + "_attempt" + str(attempt)
            result_path = directory / (identifier + ".json")
            log_path = directory / (identifier + ".log")
            arguments = [str(exe), "-batchmode", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720",
                         "-logFile", str(log_path), "--optimization-scenarios", name,
                         "--optimization-repetitions", str(args.repetitions), "--optimization-frame-rate", str(args.frame_rate),
                         "--optimization-result", str(result_path)]
            item = {"id": identifier, "scenario": name, "attempt": attempt, "phase": "Running", "arguments": arguments}
            journal["attempts"].append(item)
            save()
            print("Starting " + identifier, flush=True)
            # Keep helpers hidden; graphics remains enabled in the standalone player.
            process = subprocess.Popen(arguments, cwd=exe.parent, creationflags=subprocess.CREATE_NO_WINDOW,
                                       env={key.upper(): value for key, value in os.environ.items()})
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
    successful = {item["scenario"] for item in journal["attempts"] if item["phase"] == "Completed"}
    unresolved = sorted({item["scenario"] for item in journal["attempts"]} - successful)
    print(json.dumps({"journal": str(path), "completed": len(successful), "unresolved": unresolved}), flush=True)
    return 2 if unresolved else 0


if __name__ == "__main__":
    raise SystemExit(main())
