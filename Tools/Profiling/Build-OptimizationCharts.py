"""Build the SVG charts used by Docs/OptimizationFinalReport.md from the saved result JSON files.

Every number comes from a Summarize-Optimization.py output (median of the per-capture values).
Charts use a white background and fixed colors so they read the same in any Markdown viewer.
"""
import json
from pathlib import Path
import statistics

ROOT = Path(__file__).resolve().parents[2]
LOGS = ROOT / "Logs/Optimization"
OUT = ROOT / "Docs/images/optimization"
COLORS = ["#8a8f98", "#2f6fdb", "#e0793a", "#2a9d6f"]
TEXT = "#1f2328"
GRID = "#d0d7de"


def groups(name):
    return json.loads((LOGS / name).read_text(encoding="utf-8"))["groups"]


def median(group, key):
    entry = group["metrics"].get(key)
    return statistics.median(entry["values"]) if entry else None


def esc(text):
    return text.replace("&", "&amp;").replace("<", "&lt;")


def grouped_bars(title, subtitle, categories, series, unit, filename, decimals=1):
    """categories: list of labels; series: list of (name, [value per category])."""
    width, height = 760, 420
    left, right, top, bottom = 70, 24, 78, 70
    plot_w, plot_h = width - left - right, height - top - bottom
    maximum = max(v for _, values in series for v in values if v is not None)
    step = 10 ** len(str(int(maximum))) / 10
    for factor in (1, 2, 2.5, 5, 10):
        if maximum <= step * factor:
            top_value = step * factor
            break
    group_w = plot_w / len(categories)
    bar_w = group_w * 0.7 / len(series)
    parts = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" font-family="Segoe UI, Arial, sans-serif">',
             f'<rect width="{width}" height="{height}" fill="#ffffff"/>',
             f'<text x="{left}" y="28" font-size="18" font-weight="600" fill="{TEXT}">{esc(title)}</text>',
             f'<text x="{left}" y="48" font-size="12" fill="#57606a">{esc(subtitle)}</text>']
    for i in range(6):
        value = top_value * i / 5
        y = top + plot_h - plot_h * i / 5
        parts.append(f'<line x1="{left}" y1="{y:.1f}" x2="{width - right}" y2="{y:.1f}" stroke="{GRID}"/>')
        parts.append(f'<text x="{left - 8}" y="{y + 4:.1f}" font-size="11" text-anchor="end" fill="#57606a">{value:g}</text>')
    parts.append(f'<text x="14" y="{top + plot_h / 2}" font-size="11" fill="#57606a" transform="rotate(-90 14 {top + plot_h / 2})" text-anchor="middle">{esc(unit)}</text>')
    for ci, category in enumerate(categories):
        gx = left + ci * group_w + group_w * 0.15
        for si, (_, values) in enumerate(series):
            value = values[ci]
            if value is None:
                continue
            bar_h = plot_h * value / top_value
            x = gx + si * bar_w
            y = top + plot_h - bar_h
            parts.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{bar_w - 3:.1f}" height="{bar_h:.1f}" fill="{COLORS[si % len(COLORS)]}"/>')
            parts.append(f'<text x="{x + (bar_w - 3) / 2:.1f}" y="{y - 5:.1f}" font-size="11" text-anchor="middle" fill="{TEXT}">{value:.{decimals}f}</text>')
        parts.append(f'<text x="{left + ci * group_w + group_w / 2:.1f}" y="{top + plot_h + 20}" font-size="12" text-anchor="middle" fill="{TEXT}">{esc(category)}</text>')
    for si, (name, _) in enumerate(series):
        lx = left + si * 150
        parts.append(f'<rect x="{lx}" y="{height - 26}" width="12" height="12" fill="{COLORS[si % len(COLORS)]}"/>')
        parts.append(f'<text x="{lx + 18}" y="{height - 16}" font-size="12" fill="{TEXT}">{esc(name)}</text>')
    parts.append("</svg>")
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / filename).write_text("\n".join(parts), encoding="utf-8")
    print("wrote", OUT / filename)


def main():
    release = groups("v10rel-W-results.json")
    combine_cats, combine_keys = ["10쌍 동시", "단순 벽"], ["S4_Combine_Burst_10", "S4_Combine_SimpleWall"]
    variants = [("원본", ""), ("마스크 공유", "_SharedMask"), ("마스크 공유 + 행 구간 래스터화", "_FastMask")]
    grouped_bars("조합 이벤트 프레임 최대 시간", "창 있는 Development Build, FPS 무제한, 교차 실행 3라운드 중앙값 (16.67ms = 60FPS 예산)",
                 combine_cats,
                 [(label, [median(release[k + suffix], "combineEvent.maximumMs") for k in combine_keys]) for label, suffix in variants],
                 "ms", "combine-event-frame.svg")
    grouped_bars("마스크 생성 구간 (Mask.SetFootprint)", "한 실행 동안의 합계(10쌍은 장판 20개 착지), 중앙값",
                 ["10쌍 동시", "단순 벽"],
                 [(label, [median(release[k + suffix], "Optimization.Mask.SetFootprint.inclusiveMs") for k in combine_keys]) for label, suffix in variants],
                 "ms / 실행", "mask-cost.svg")

    land_key = "Optimization.Land.Total.inclusiveMs"
    grouped_bars("착지 전체 비용 (Land.Total)", "한 실행 동안의 합계(10쌍은 장판 20개 착지), 중앙값",
                 combine_cats,
                 [(label, [median(release[k + suffix], land_key) for k in combine_keys]) for label, suffix in variants],
                 "ms / 실행", "land-total.svg")

    # Everyday frame cost in real combat scenarios (original path, windowed, uncapped).
    combat_file = LOGS / "v10rel-W-combat-results.json"
    if combat_file.exists():
        combat = groups("v10rel-W-combat-results.json")
        names = ["S8_Combat_Minimal", "S8_Combat_Representative", "S8_Combat_Peak"]
        grouped_bars("실제 전투 시나리오의 평소 프레임 시간", "창 있는 Development Build, FPS 무제한, 교차 3라운드 중앙값 (60FPS 예산 = 16.67ms)",
                     ["최소 전투", "대표 전투", "최대 전투"],
                     [("평균", [median(combat[n], "frame.averageMs") for n in names]),
                      ("p99", [median(combat[n], "frame.p99Ms") for n in names])],
                     "ms", "combat-frame-time.svg", decimals=2)

    # Rendering: frame time does not grow with area count (windowed player, uncapped).
    render = groups("v6-W1-results.json") if (LOGS / "v6-W1-results.json").exists() else None
    if render:
        names = ["S0_Baseline_UI_Off", "S1_Areas_01", "S1_Areas_05", "S1_Areas_10", "S1_Areas_20"]
        labels = ["기준", "장판 1", "장판 5", "장판 10", "장판 20"]
        grouped_bars("장판 개수에 따른 평균 프레임 시간", "창 있는 Development Build, FPS 무제한 (RTX 4060 Laptop, 1280x720)",
                     labels, [("평균 프레임 ms", [median(render[n], "frame.averageMs") for n in names])], "ms", "area-render.svg", decimals=2)

    # Status GC (batch build, CPU markers only).
    gc = groups("v6-A-results.json")
    prefix = "S7_Area_ContactEffects_30_Sustained_"
    grouped_bars("상태이상 경로 할당 (PlayerLoop, 적 30명 지속 접촉)", "Development Build, 교차 5라운드 중앙값. 할당량이며 프레임 시간 변화는 없음",
                 ["PlayerLoop GC (KiB/프레임)"],
                 [("원본", [median(gc[prefix + "Original"], "PlayerLoop.gcBytesPerFrame") / 1024]),
                  ("개선", [median(gc[prefix + "Improved"], "PlayerLoop.gcBytesPerFrame") / 1024])], "KiB / frame", "status-gc.svg")


if __name__ == "__main__":
    main()
