"""Analyze multiple journals in one version-matched Unity process.

Reuses ProfilerCaptureAnalyzer and its original per-run metadata/validity rules.
Does not merge or rewrite original run.json files or binary captures.
"""
import argparse
import json
import msvcrt
import os
from pathlib import Path
import re
import subprocess
import time

ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("journals", type=Path, nargs="+")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--timeout", type=int, default=3600)
    args = parser.parse_args()
    version = re.search(r"m_EditorVersion:\s*(\S+)", (ROOT / "ProjectSettings/ProjectVersion.txt").read_text()).group(1)
    unity = Path("C:/Program Files/Unity/Hub/Editor") / version / "Editor/Unity.exe"
    if not unity.exists():
        raise RuntimeError("Matching Unity executable not found: " + str(unity))
    output = (args.output or ROOT / "Logs/Optimization" / ("analysis_" + time.strftime("%Y%m%d_%H%M%S"))).resolve()
    if output.exists():
        raise RuntimeError("Use a new output directory.")
    sources, manifests, jobs = [], [], []
    journals = {path: json.loads(path.read_text(encoding="utf-8")) for path in args.journals}
    for journal in journals.values():
        for item in journal["attempts"]:
            if not item.get("runDirectory") or item.get("phase") not in ("Completed", "Failed", "Stopped"):
                continue
            previous = item.get("analysis") or {}
            if previous.get("summaryPath") and Path(previous["summaryPath"]).exists():
                continue
            directory = Path(item["runDirectory"]).resolve()
            manifest = directory / "run.json"
            metadata = json.loads(manifest.read_text(encoding="utf-8-sig"))
            own_sources = []
            for capture in metadata.get("captures", []):
                source = (directory / capture["fileName"]).resolve()
                if not source.is_relative_to(directory):
                    raise ValueError("Capture escapes its run directory.")
                sources.append(str(source))
                manifests.append(str(manifest))
                own_sources.append(str(source))
            if own_sources:
                jobs.append((item, own_sources))
    if not sources:
        print("No unanalyzed captures.")
        return 0
    project = ROOT / "Logs/ProfilerCliProject" / version
    project.mkdir(parents=True, exist_ok=True)
    with (project / "analysis.lock").open("a+b") as lock:
        lock.seek(0)
        try:
            msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        except OSError as error:
            raise RuntimeError("Another CLI analysis is using the project.") from error
        for name in ["Assets/Editor", "Packages", "ProjectSettings"]:
            (project / name).mkdir(parents=True, exist_ok=True)
        analyzer = (ROOT / "Tools/Profiling/Editor/ProfilerCaptureAnalyzer.cs").read_text(encoding="utf-8-sig")
        (project / "Assets/Editor/ProfilerCaptureAnalyzer.cs").write_text(analyzer, encoding="utf-8")
        (project / "Packages/manifest.json").write_text('{"dependencies":{"com.unity.modules.jsonserialize":"1.0.0"}}', encoding="utf-8")
        (project / "ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: " + version + "\n", encoding="utf-8")
        output.mkdir(parents=True)
        request = output / "request.json"
        request.write_text(json.dumps({"sources": sources, "manifestPaths": manifests,
                                      "outputDirectory": str(output), "maxFrames": 100000,
                                      "optimizationOnly": True}, indent=2), encoding="utf-8")
        print("Analyzing " + str(len(sources)) + " captures: " + str(output), flush=True)
        process = subprocess.Popen([str(unity), "-batchmode", "-nographics", "-projectPath", str(project),
                                    "-executeMethod", "ProfilerCaptureAnalyzer.Run", "-profilerAnalysisRequest", str(request),
                                    "-logFile", str(output / "unity.log")], creationflags=subprocess.CREATE_NO_WINDOW,
                                   env={key.upper(): value for key, value in os.environ.items()})
        deadline = time.monotonic() + args.timeout
        while process.poll() is None:
            if time.monotonic() > deadline:
                process.kill()
                process.wait()
                raise TimeoutError("Analysis timed out; partial capture reports are retained in " + str(output))
            time.sleep(2)
        summary_path = output / "summary.json"
        if not summary_path.exists():
            raise RuntimeError("Analysis exited without summary; see " + str(output / "unity.log"))
        summary = json.loads(summary_path.read_text(encoding="utf-8-sig"))
        reports = {report["source"]: report for report in summary["captures"]}
        for item, own_sources in jobs:
            if item.get("analysis"):
                item.setdefault("analysisAttempts", []).append(item["analysis"])
            valid = sum(reports[source]["valid"] for source in own_sources)
            item["analysis"] = {"phase": "Completed" if valid == len(own_sources) else "Failed",
                                "validCaptures": valid, "invalidCaptures": len(own_sources) - valid,
                                "summaryPath": str(summary_path), "sources": own_sources,
                                "reportPath": str(output / "report.md")}
        for path, journal in journals.items():
            path.write_text(json.dumps(journal, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps({"valid": summary["validCaptures"], "invalid": summary["invalidCaptures"], "summary": str(summary_path)}), flush=True)
    return 0 if all(item["phase"] != "Completed" or item["analysis"]["phase"] == "Completed" for item, _ in jobs) else 2


if __name__ == "__main__":
    raise SystemExit(main())
