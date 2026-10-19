"""Guard measurement provenance when resuming and combining journals."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

DIRECTORY = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("optimization_build", DIRECTORY / "Run-OptimizationBuild.py")
build = importlib.util.module_from_spec(spec)
spec.loader.exec_module(build)
summary_spec = importlib.util.spec_from_file_location("optimization_summary", DIRECTORY / "Summarize-Optimization.py")
summary = importlib.util.module_from_spec(summary_spec)
summary_spec.loader.exec_module(summary)


class JournalCompatibilityTests(unittest.TestCase):
    def setUp(self):
        self.conditions = {"managedAssemblySha256": "managed-a", "exeSha256": "exe-a",
                           "environment": "development-player-batch-graphics", "repetitions": 3,
                           "frameRate": 120, "resolution": [1280, 720], "vSync": 0}
        self.earlier = {**self.conditions, "attempts": [
            {"scenario": "done", "phase": "Completed"},
            {"scenario": "failed", "phase": "Failed"},
            {"scenario": "interrupted", "phase": "Running"}]}

    def test_resume_skips_only_completed_scenarios(self):
        self.assertEqual(build.completed_scenarios(self.earlier, self.conditions), {"done"})

    def test_resume_requires_matching_provenance_and_request(self):
        for key in self.conditions:
            for missing in (False, True):
                with self.subTest(key=key, missing=missing):
                    earlier = dict(self.earlier)
                    if missing:
                        earlier.pop(key)
                    else:
                        earlier[key] = "different"
                    with self.assertRaisesRegex(ValueError, key):
                        build.completed_scenarios(earlier, self.conditions)

    def summarize(self, conditions):
        with tempfile.TemporaryDirectory() as directory:
            paths = []
            for index, metadata in enumerate(conditions):
                path = Path(directory) / (str(index) + ".json")
                path.write_text(json.dumps({**metadata, "attempts": []}), encoding="utf-8")
                paths.append(str(path))
            output = Path(directory) / "result.json"
            process = subprocess.run([sys.executable, str(DIRECTORY / "Summarize-Optimization.py"),
                                      *paths, "--output", str(output)], capture_output=True, text=True)
            result = json.loads(output.read_text(encoding="utf-8")) if output.exists() else None
            return process, result

    def test_summary_preserves_matching_provenance(self):
        process, result = self.summarize([self.conditions, self.conditions])
        self.assertEqual(process.returncode, 0, process.stderr)
        self.assertEqual(len(result["journals"]), 2)
        self.assertEqual(result["journals"][0]["managedAssemblySha256"], "managed-a")

    def test_summary_rejects_mixed_builds_settings_and_environments(self):
        for key in ["managedAssemblySha256", "exeSha256", "frameRate", "resolution", "vSync", "environment"]:
            with self.subTest(key=key):
                process, result = self.summarize([self.conditions, {**self.conditions, key: "different"}])
                self.assertNotEqual(process.returncode, 0)
                self.assertIsNone(result)

    def test_summary_rejects_unverified_legacy_builds_when_combining(self):
        legacy = dict(self.conditions)
        legacy.pop("managedAssemblySha256")
        process, result = self.summarize([legacy, legacy])
        self.assertNotEqual(process.returncode, 0)
        self.assertIsNone(result)

    def test_event_frames_exclude_boundaries_and_deduplicate_commits(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)
            (path / "frames.csv").write_text("loadedFrameIndex,selected\n0,False\n1,True\n2,True\n3,True\n")
            (path / "event-frames.csv").write_text(
                "loadedFrameIndex,name,mainThreadMs\n0,PerfDemo.CombineCommit,999\n"
                "1,PerfDemo.CombineCommit,20\n1,PerfDemo.CombineCommit,20\n"
                "2,PerfDemo.CombineCommit,40\n3,Optimization.Combine.Shape,100\n")
            self.assertEqual(summary.combine_event_timing(path),
                             {"frames": 2, "maximumMs": 40, "over16_67Ms": 2, "over33_33Ms": 1})


if __name__ == "__main__":
    unittest.main()
