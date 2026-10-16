"""Reproduce original/improved demo runs; preserve every attempt in a journal.

Capture and analysis are separate so analysis does not compete with game captures.
Uses the existing Unity MCP bridge and PerformanceDemo capture/validation pipeline.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("performance_demo", Path(__file__).with_name("Run-PerformanceDemo.py"))
demo = importlib.util.module_from_spec(spec)
spec.loader.exec_module(demo)


def scenarios(suite):
    def pair(name, variant):
        return [name, name + "_" + variant]
    gc = []
    for name in ["S7_Area_ContactEffects_01", "S7_Area_ContactEffects_10", "S7_Area_ContactEffects_30", "S7_Combine_ContactEffects_10"]:
        gc += [name] + [name + "_" + variant for variant in ["StatusQuery", "Timeline", "Improved"]]
    sustained = ["S7_Area_ContactEffects_30_Sustained_" + variant for variant in ["Original", "StatusQuery", "Timeline", "Improved"]]
    combine = [name for path in sorted((ROOT / "Assets/PerformanceDemo").glob("S4_Combine_*.asset"))
               for name in pair(path.stem, "SharedMask")]
    enemy = [name for path in sorted((ROOT / "Assets/PerformanceDemo").glob("S6_Chase_*.asset"))
             for name in pair(path.stem, "EnemySearch")]
    regression = [name for path in sorted((ROOT / "Assets/PerformanceDemo").glob("S8_Combat_*.asset"))
                  for name in pair(path.stem, "Improved")]
    regression += pair("S9_Combat_Lifecycle", "Improved")
    core = sustained + pair("S7_Area_ContactEffects_30", "Improved") + pair("S7_Combine_ContactEffects_10", "Improved")
    for name in ["S4_Combine_Burst_10", "S4_Combine_SimpleWall", "S4_Combine_ComplexCorner"]:
        core += pair(name, "SharedMask")
    for name in ["S6_Chase_Moving_60", "S6_Chase_Static_60"]:
        core += pair(name, "EnemySearch")
    core += pair("S8_Combat_Minimal", "Improved")
    return {"core": core, "gc": gc + sustained, "combine": combine, "enemy": enemy,
            "regression": regression, "all": gc + sustained + combine + enemy + regression}[suite]


def save(path, journal):
    path.write_text(json.dumps(journal, ensure_ascii=False, indent=2), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["prepare", "validate", "capture", "functional", "analyze"])
    parser.add_argument("--suite", choices=["core", "gc", "combine", "enemy", "regression", "all"], default="core")
    parser.add_argument("--repetitions", type=int, default=3)
    parser.add_argument("--journal", type=Path)
    parser.add_argument("--scenario", action="append", help="Override suite with exact asset names; repeatable")
    parser.add_argument("--attempts", type=int, default=1, help="Maximum attempts per scenario; failures are retained")
    parser.add_argument("--exclude-journal", type=Path, help="Skip scenarios already completed in a previous journal")
    args = parser.parse_args()
    if args.repetitions < 1 or args.attempts < 1:
        parser.error("repetitions and attempts must be positive")
    client = demo.UnityMcpClient()
    try:
        if args.action in ("prepare", "validate"):
            code = "OptimizationScenarioBuilder.Build(); return \"Prepared\";" if args.action == "prepare" else "return OptimizationValidation.Run();"
            print(json.dumps(client.tool("execute_code", {"action": "execute", "code": code}), ensure_ascii=False), flush=True)
            return
        if args.action == "analyze":
            if not args.journal:
                parser.error("analyze requires --journal")
            journal = json.loads(args.journal.read_text(encoding="utf-8"))
            for item in journal["attempts"]:
                if item.get("phase") not in ("Completed", "Failed", "Stopped") or not item.get("runDirectory"):
                    continue
                previous = item.get("analysis")
                if previous and previous.get("summaryPath") and Path(previous["summaryPath"]).exists():
                    continue
                if previous:
                    item.setdefault("analysisAttempts", []).append(previous)
                try:
                    job = client.performance({"action": "analyze", "run_directory": item["runDirectory"]})
                    item["analysis"] = wait(client, job)
                except (RuntimeError, OSError) as error:
                    item["analysisError"] = str(error)
                    save(args.journal, journal)
                    # A request can be accepted before a connection drops. Do not
                    # start a different job until its state has been reconciled.
                    raise
                save(args.journal, journal)
            return
        path = args.journal or ROOT / "Logs/Optimization" / (args.action + "_" + args.suite + "_" + time.strftime("%Y%m%d_%H%M%S") + ".json")
        path.parent.mkdir(parents=True, exist_ok=True)
        if path.exists():
            raise RuntimeError("Use a new journal path; previous evidence is never overwritten.")
        journal = {"action": args.action, "suite": args.suite, "repetitions": args.repetitions, "attempts": []}
        save(path, journal)
        print("Journal: " + str(path), flush=True)
        excluded = set()
        if args.exclude_journal:
            earlier = json.loads(args.exclude_journal.read_text(encoding="utf-8"))
            excluded = {item["scenario"] for item in earlier["attempts"] if item.get("phase") == "Completed"}
        for name in args.scenario or scenarios(args.suite):
            if name in excluded:
                continue
            for attempt in range(1, args.attempts + 1):
                print("Starting " + name + " attempt=" + str(attempt), flush=True)
                job = client.performance({"action": "start", "scenario": name, "repetitions": args.repetitions,
                                          "capture_profiler": args.action == "capture", "analyze": False})
                # Save the accepted job before polling, including ambiguous interruption states.
                item = {"scenario": name, "attempt": attempt, "accepted": job}
                journal["attempts"].append(item)
                save(path, journal)
                item.update(wait(client, job))
                save(path, journal)
                if item.get("phase") == "Completed":
                    break
        failed = [item["scenario"] for item in journal["attempts"] if item.get("phase") != "Completed"]
        completed = {item["scenario"] for item in journal["attempts"] if item.get("phase") == "Completed"}
        unresolved = sorted(set(failed) - completed)
        print(json.dumps({"journal": str(path), "attempts": len(journal["attempts"]), "failedAttempts": failed,
                          "unresolved": unresolved}), flush=True)
        return 2 if unresolved else 0
    finally:
        client.close()


def wait(client, job):
    deadline = time.monotonic() + 3600
    previous = None
    while job.get("phase") not in ("Completed", "Failed", "Stopped"):
        if time.monotonic() > deadline:
            raise TimeoutError("Job still running; use the existing CLI status/stop commands. No start request was retried.")
        state = (job.get("phase"), job.get("runnerPhase"))
        if state != previous:
            print(json.dumps({"id": job.get("id"), "phase": state[0], "runner": state[1]}), flush=True)
            previous = state
        time.sleep(2)
        job = client.performance({"action": "status"})
    print(json.dumps(job, ensure_ascii=False), flush=True)
    return job


if __name__ == "__main__":
    raise SystemExit(main())
