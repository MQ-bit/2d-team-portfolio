"""Summarize only runtime-and-marker validated captures, retaining each repetition."""
import argparse
import csv
import json
from pathlib import Path
import statistics

MARKERS = ["PlayerLoop", "PotionStatusVisual.LateUpdate", "Optimization.Status.HasStatus",
           "Optimization.Timeline.Synchronize", "PerfDemo.CombineCommit", "Optimization.Combine.Shape",
           "Optimization.Combine.WallQueryAndClip", "Optimization.Combine.Collider", "Optimization.Combine.Visual",
           "Optimization.Mask.SetFootprint", "Optimization.Mask.RasterizeAndUpload", "Optimization.Enemy.FindPlayer",
           "RangedEnemyController.FixedUpdate"]


def combine_event_timing(directory):
    directory = Path(directory)
    if not (directory / "event-frames.csv").exists():
        return None
    with (directory / "frames.csv").open(encoding="utf-8-sig", newline="") as stream:
        selected = {row["loadedFrameIndex"] for row in csv.DictReader(stream) if row["selected"] == "True"}
    # Each frame is counted once even when multiple combinations commit in it.
    frames = {}
    with (directory / "event-frames.csv").open(encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            if row["name"] == "PerfDemo.CombineCommit" and row["loadedFrameIndex"] in selected:
                frames[row["loadedFrameIndex"]] = float(row["mainThreadMs"])
    if not frames:
        return None
    return {"frames": len(frames), "maximumMs": max(frames.values()),
            "over16_67Ms": sum(value > 16.67 for value in frames.values()),
            "over33_33Ms": sum(value > 33.33 for value in frames.values())}


def capture_row(capture, job_id, diagnostic_only):
    row = {key: capture.get(key) for key in ["scenarioName", "repetition", "valid", "selectedFrames", "issues", "warnings"]}
    row["job"] = job_id
    row["diagnosticOnly"] = diagnostic_only
    row["reportDirectory"] = capture["reportDirectory"]
    row["source"] = capture["source"]
    row["validationLevel"] = capture["validationLevel"]
    row["timing"] = capture.get("measurementMainThreadTiming")
    row["gameLoop"] = capture.get("gameLoop")
    row["combineEventTiming"] = combine_event_timing(capture["reportDirectory"])
    row["markers"] = {}
    frames = capture["selectedFrames"]
    with (Path(capture["reportDirectory"]) / "hotspots.csv").open(encoding="utf-8-sig", newline="") as stream:
        for sample in csv.DictReader(stream):
            name = sample["name"]
            for method in ["PotionStatusVisual.LateUpdate", "RangedEnemyController.FixedUpdate"]:
                if method + "()" in name:
                    name = method
            if sample["thread"] != "Main Thread" or name not in MARKERS:
                continue
            calls = int(sample["calls"])
            row["markers"][name] = {
                "calls": calls, "inclusiveMs": float(sample["inclusiveMs"]),
                "selfMs": float(sample["selfMs"]), "maxSingleMs": float(sample["maxSingleMs"]),
                "gcBytes": int(sample["inclusiveGcBytes"]),
                "gcBytesPerCall": int(sample["inclusiveGcBytes"]) / calls if calls else None,
                "callsPerFrame": calls / frames if frames else None,
                "gcBytesPerFrame": int(sample["inclusiveGcBytes"]) / frames if frames else None,
                "inclusiveMsPerFrame": float(sample["inclusiveMs"]) / frames if frames else None,
                "inclusiveUsPerCall": float(sample["inclusiveMs"]) * 1000 / calls if calls else None,
            }
    return row


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("journals", type=Path, nargs="+")
    parser.add_argument("--output", type=Path, default=Path("Logs/Optimization/results.json"))
    args = parser.parse_args()
    rows, pending, seen = [], [], set()
    environments = set()
    provenance = []
    player_conditions = None
    for path in args.journals:
        journal = json.loads(path.read_text(encoding="utf-8"))
        environments.add(journal.get("environment", "editor"))
        if len(environments) > 1:
            raise ValueError("Summarize Editor and Development Build environments separately.")
        if journal.get("environment", "").startswith("development-player"):
            conditions = {key: journal.get(key) for key in ["managedAssemblySha256", "exeSha256",
                          "frameRate", "resolution", "vSync"]}
            if player_conditions is not None and (conditions != player_conditions or
                                                   not conditions["managedAssemblySha256"]):
                raise ValueError("Summarize different or unverified player builds/settings separately.")
            player_conditions = conditions
        provenance.append({"path": str(path), **{key: value for key, value in journal.items() if key != "attempts"}})
        for attempt in journal["attempts"]:
            analysis = attempt.get("analysis") or {}
            summary = analysis.get("summaryPath") or attempt.get("summaryPath")
            if not summary or not Path(summary).exists():
                pending.append({"scenario": attempt["scenario"], "phase": attempt.get("phase"), "analysis": analysis.get("phase")})
                continue
            for capture in json.loads(Path(summary).read_text(encoding="utf-8-sig"))["captures"]:
                if analysis.get("sources") and capture["source"] not in analysis["sources"]:
                    continue
                if capture["source"] in seen:
                    continue
                seen.add(capture["source"])
                rows.append(capture_row(capture, attempt.get("id"), attempt.get("phase") != "Completed"))
    groups = {}
    for row in rows:
        if not row["valid"] or row["diagnosticOnly"] or row["validationLevel"] != "RuntimeAndMarkers":
            continue
        group = groups.setdefault(row["scenarioName"], {"validCaptures": 0, "repetitions": [], "metrics": {}})
        group["validCaptures"] += 1
        group["repetitions"].append({"job": row["job"], "repetition": row["repetition"]})
        metrics = group["metrics"]
        for name, value in row["timing"].items():
            metrics.setdefault("frame." + name, []).append(value)
        for name, value in (row["combineEventTiming"] or {}).items():
            metrics.setdefault("combineEvent." + name, []).append(value)
        for marker, values in row["markers"].items():
            for name, value in values.items():
                if value is not None:
                    metrics.setdefault(marker + "." + name, []).append(value)
    for group in groups.values():
        group["metrics"] = {name: {"values": values, "median": statistics.median(values), "min": min(values), "max": max(values)}
                            for name, values in group["metrics"].items()}
    result = {"environment": next(iter(environments)), "journals": provenance, "notes": "Medians describe per-capture measurements, not pooled frames. Inclusive scopes overlap. GC is allocation, not retained memory. Missing markers are unavailable, not zero.",
              "validCaptures": sum(row["valid"] and not row["diagnosticOnly"] and row["validationLevel"] == "RuntimeAndMarkers" for row in rows),
              "invalidCaptures": sum(not row["valid"] for row in rows), "pending": pending, "groups": groups, "captures": rows}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    lines = ["# 최적화 측정 결과", "", result["notes"], "",
             "각 값은 실행별 값의 중앙값이다. 원본·개선 간 동일 환경과 기능 결과를 함께 확인한다.", "",
             "| 시나리오 | 유효 캡처 | PlayerLoop GC KiB/f | 상태 표시 GC KiB/f | 조합 최대 호출 ms | 탐색 µs/call | 전체 p95 ms |",
             "|---|---:|---:|---:|---:|---:|---:|"]
    for name, group in groups.items():
        def metric(key, scale=1):
            values = group["metrics"].get(key)
            return "미측정" if values is None else f"{values['median'] * scale:.3f}"
        lines.append("| " + " | ".join([name, str(group["validCaptures"]), metric("PlayerLoop.gcBytesPerFrame", 1/1024),
                     metric("PotionStatusVisual.LateUpdate.gcBytesPerFrame", 1/1024), metric("PerfDemo.CombineCommit.maxSingleMs"),
                     metric("Optimization.Enemy.FindPlayer.inclusiveUsPerCall"), metric("frame.p95Ms")]) + " |")
    lines += ["", "세부 실행별 수치·변동·기능 결과·근거 경로는 같은 이름의 JSON에 보존한다.",
              "분석 미완료 시도: " + str(len(pending)), "무효 캡처: " + str(result["invalidCaptures"])]
    args.output.with_suffix(".md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(args.output), "groups": len(groups), "valid": result["validCaptures"], "invalid": result["invalidCaptures"], "pending": len(pending)}))


if __name__ == "__main__":
    main()
