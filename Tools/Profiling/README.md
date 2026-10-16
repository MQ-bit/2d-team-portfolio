# Unity Profiler CLI / MCP

저장된 데모 `.raw` / 수동 `.data` 캡처를 Unity로 읽어 CPU 시간·GC 할당·프레임 통계와 측정 구간 유효성을 검사한다. 게임 씬을 실행하는 도구가 아니라 저장 파일을 분석하는 도구다.

## MCP로 씬 실행부터 분석까지

`Run-PerformanceDemo.py`는 열려 있는 Unity의 `performance_demo` 도구를 호출한다. 씬 실행 → Measure 캡처 → 플레이 종료 → 아래 CLI 분석까지 이어진다. Python 표준 라이브러리만 사용하며 OpenAI API 키는 필요 없다.

Unity 프로젝트를 열고 다음을 프로젝트 루트에서 실행한다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Profiling/Start-UnityMcp.ps1
python Tools/Profiling/Run-PerformanceDemo.py discover
python Tools/Profiling/Run-PerformanceDemo.py start --scenario S1_Areas_20 --repetitions 1 --wait
```

서버는 설치된 Unity MCP 패키지와 같은 버전의 `mcpforunityserver`를 `uvx`로 실행한다. Python과 uvx가 필요하고 첫 실행에는 패키지 다운로드가 필요하다. 로컬 `127.0.0.1:8080`에서만 연결하며 서버 텔레메트리를 끈다. 연결 요청은 `Logs/PerformanceMcp/connect.request`, 결과는 `connect.result.json`, 최신 작업 상태는 `job.json`에 저장한다. 이미 실행 중인 서버는 재사용한다.

다른 명령:

```powershell
python Tools/Profiling/Run-PerformanceDemo.py status
python Tools/Profiling/Run-PerformanceDemo.py stop --wait
python Tools/Profiling/Run-PerformanceDemo.py start --scenario all --wait
python Tools/Profiling/Run-PerformanceDemo.py analyze --run-directory "C:\captures\실행ID" --wait
```

- `--scenario`는 시나리오 에셋의 정확한 이름 또는 `all`이다. `--repetitions 0` 또는 생략 시 원래 반복 횟수를 사용한다. 양수는 메모리 사본에만 적용한다.
- `--no-analyze`는 실행과 저장까지만 진행한다. 작업 수락 직후 반환하는 것이 기본이며 `--wait`는 완료까지 상태를 조회한다. 대기 시간 초과가 실행을 중단하지는 않는다.
- `--no-capture --no-analyze`는 raw 기록 없이 기능만 검증한다. 이 모드는 CPU 성능 비교용이 아니며 포커스 이탈을 캡처 실패로 판정하지 않는다. 기본 캡처 모드의 포커스·시간 검증은 유지한다.
- 여러 Unity가 연결되면 `--instance "이름@해시"`로 대상을 지정한다. `discover` 오류에도 연결된 인스턴스 목록이 나온다. 플레이 전환 중 일시적인 연결 해제를 기다린다.
- 수정된 다른 씬이 열려 있으면 저장을 요구하며, 자동 저장하지 않는다. 데모의 AutoStart는 꺼져 있어야 한다. 직접 진입한 플레이 모드만 자동 종료한다. 기존 플레이 모드에서는 유휴 러너가 있어야 한다.
- 플레이 진입 완료 후 실행하고 자동 실행 중 백그라운드 실행을 켠다. 시나리오·캡처·백그라운드 설정은 종료 후 복구한다. 측정 중 새 포커스 상실, 긴 프레임 중단 등은 기존 유효성 검사대로 실패한다.
- 중지는 러너의 정리·JSON 저장을 기다린다. 분석 프로세스가 시작된 뒤에는 완료까지 기다린다. 강제 에디터 종료나 컴파일 중단까지 복구를 보장하지 않는다.
- 결과는 `Logs/ProfilerReports/mcp_<작업ID>/report.md`, `summary.json`에 저장한다. `--wait` 종료 코드는 완료 `0`, 실패/중지 `2`, 연결·도구 오류 `1`이다.

프로젝트 `.codex/config.toml`에 `UnityMCP`의 URL이 `http://127.0.0.1:8080/mcp`로 설정되어 있다. Codex에서 이 서버의 도구 목록을 새로 읽은 세션이면 `execute_custom_tool`에 `tool_name="performance_demo"`, `parameters={"action":"start","scenario":"S1_Areas_20","repetitions":1,"analyze":true}`를 전달할 수 있다. 이어 `parameters={"action":"status"}`로 완료 여부를 확인한다. 현재 세션에서 Unity 도구가 노출되지 않아도 위 Python 명령으로 같은 MCP 도구를 호출할 수 있다.

에디터 자동 실행은 기능 확인·병목 탐색용이다. 성능 기준값은 같은 조건의 화면 있는 Development Build에서 따로 측정한다.

추가된 조합·추적·접촉·복합 전투·반복 전투의 구성과 허용 종료 결과는 [게임 루프 시나리오 사용법](../../Docs/PerformanceDemoGameLoopImplementation.md)에 정리했다. 분석 보고서는 `gameLoops`의 기능 판정과 `CombatCompleted`/`PlayerDefeated`를 표시한다. 승리와 패배를 같은 작업량으로 비교하지 않는다.

## 실행

프로젝트 루트의 PowerShell에서 실행한다. `-CapturePath`에는 실행 결과 폴더, `run.json`, `.raw`, `.data`를 지정할 수 있다. 가능한 한 `run.json`이 있는 실행 폴더를 지정한다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Profiling\Analyze-Profiler.ps1 -CapturePath "C:\Users\sdkmg\AppData\LocalLow\DefaultCompany\PotionJunkie\PerformanceDemo\실행ID"
```

파일 하나만 분석할 수도 있다. 같은 폴더에 `run.json`이 있으면 해당 캡처의 상태·샘플 수와 자동 대조한다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Profiling\Analyze-Profiler.ps1 -CapturePath "C:\captures\S1_Areas_20_repeat01.raw" -OutputDirectory ".\Logs\MyProfilerReport"
```

`Bypass`는 이 PowerShell 프로세스에만 적용하며 시스템 실행 정책을 바꾸지 않는다. 로컬에서 작성·확인한 이 스크립트의 실행 제한을 해제하는 용도다.

추가 인자:

| 인자 | 기본값 | 설명 |
| --- | --- | --- |
| `-UnityPath` | 프로젝트 버전의 Unity Hub 경로 탐색 | `Unity.exe` 절대 경로 |
| `-OutputDirectory` | `Logs/ProfilerReports/<시간_고유ID>` | 비어 있는 출력 폴더 |
| `-MaxFrames` | `100000` | 로드 기록 한도. 2,000~1,000,000 |
| `-TimeoutSeconds` | `600` | 분석 프로세스 제한 시간 |

Unity 6000.3.22f1에서 검증했다. 프로젝트의 `ProjectSettings/ProjectVersion.txt`를 기준으로 Unity를 찾는다. 실행에 해당 Unity Editor 라이선스가 필요하다. 다른 Unity 버전의 캡처는 같은 버전의 분석기로 읽어야 하며, 실행 메타데이터와 버전이 다르면 무효로 표시한다.

## 출력

```text
Logs/ProfilerReports/<분석ID>/
├─ report.md                 전체 캡처 판정과 개별 보고서 링크
├─ summary.json              Codex·자동화에서 읽을 구조화 결과
├─ request.json              사용한 입력 경로·기록 한도
├─ unity.log                 Unity 실행·로드·컴파일 로그
└─ capture_001/
   ├─ report.md              통계와 CPU 상위 구간
   ├─ summary.json           판정·사유·통계·상위 함수/할당 경로
   ├─ frames.csv             프레임별 CPU 시간·GC·측정 마커
   ├─ hotspots.csv           함수별 Self/Inclusive 시간·호출 수
   └─ allocation-sites.csv   GC.Alloc의 상위 샘플 경로와 할당량
```

정상 통계는 `PerfDemo.Measure`가 있고 Setup/Cleanup이 없는 프레임에서만 계산한다. 시작·종료 경계 프레임은 부분 프레임일 수 있어 통계에서 제외하되 전체 마커 수 대조에는 포함한다. 백분위는 정렬한 프레임 시간의 nearest-rank 방식이다. 평균·p50·p95·p99·최대값, 60/30FPS 프레임 예산 초과 횟수, 메인 스레드 GC/frame을 제공한다.

CPU 함수 순위는 Self 시간 기준이다. Inclusive 시간과 Inclusive GC는 상위·하위 함수에 중복 포함되므로 더해서 총비용으로 쓰지 않는다. 메인 스레드 프레임 시간에는 에디터 작업과 대기가 들어간다. Render Thread CPU 시간은 GPU 시간이 아니며, GC 할당은 잔존 메모리나 누수를 뜻하지 않는다. GPU 시간·작업자 전체 스레드 할당·스냅샷 메모리 분석은 이 도구의 범위에 포함하지 않는다.

## 유효성

- `run.json`과 일치하면 `validationLevel: RuntimeAndMarkers`로 실행 상태·Unity 버전·파일 크기·측정 마커를 검증한다. `Completed` 캡처만 유효 후보로 인정한다.
- 메타데이터가 없는 단일 파일은 `MarkersOnly`다. 마커는 확인할 수 있지만 실행 성공이나 전체 기록 보존은 보장할 수 없다는 경고가 붙는다.
- 측정 마커 없음, Setup/Cleanup 포함, 중단·실패, 누락·손상, 기록 한도 도달, 기대 마커 수와 2개를 넘는 차이는 무효다. 시작·종료 시 프레임 플러시에 따른 최대 2샘플 차이는 허용한다.
- 원본 `run.json`, 캡처, 현재 게임 씬은 변경하지 않는다. 검증 결과는 별도 보고서의 `markerVerification`에 기록한다.
- `observedMainThreadTiming`은 로드된 전체 프레임의 진단값이다. 성능 비교에는 **유효 캡처의** `measurementMainThreadTiming`을 사용한다.
- 출력 폴더에 파일이 있으면 덮어쓰지 않는다. 실행 결과 폴더에 캡처 목록이 없으면 오류를 낸다.

종료 코드: `0` = 모든 캡처 통과, `2` = 분석 완료·무효 캡처 있음, `1` = 인자·Unity 실행 등 도구 오류. PowerShell에서는 실행 직후 `$LASTEXITCODE`로 확인한다.

## 동작과 제한

분석은 `Logs/ProfilerCliProject/<Unity버전>`에 생성한 별도 프로젝트에서 실행한다. 같은 버전의 CLI 작업은 동시에 하나만 실행되며 게임 에디터는 열어 둬도 된다. 첫 실행에는 프로젝트 초기화·컴파일 시간이 추가된다. 분석 중에는 게임을 돌려 성능 기준을 측정하지 않는 편이 좋다.

Unity의 로더가 UI 프레임 기록 한도 때문에 파일 일부만 읽는 문제를 피하기 위해, 분리된 분석 프로젝트에서 `ProfilerDriver.SetMaxFrameHistoryLength` 어댑터를 사용한다. 이 내부 API가 없는 Unity 버전에서는 조용히 일부만 읽지 않고 실패한다. 한도에 도달하면 `-MaxFrames`를 올려 재분석한다. 큰 값은 메모리 사용량을 늘린다.

## 검증 기록

Unity 6000.3.22f1에서 다음을 확인했다.

- 실제 정상 `.raw`의 2,597프레임 로드, `run.json`의 기대 2,596 Measure 샘플과 실제 마커 수 일치.
- 정상·중단·커버리지 불일치·손상·누락을 섞은 실행 폴더에서 정상 1개/무효 4개 분류.
- 기존 수동 `.data`를 측정 마커 없음으로 거부, 종료 코드 2.
- 공백이 있는 입력·출력 경로와 `run.json` 직접 입력, 종료 코드 0.
- `-MaxFrames 2000`으로 기록을 제한하면 `HistoryLimitReached`와 `MeasurementCoverageMismatch`를 보고하고 종료 코드 2를 반환.
- 프레임 CSV의 GC 합계와 JSON 합계 일치, 메인 스레드 Self 합계와 프레임 시간 합계의 부동소수점 오차 범위 내 일치.

검증 산출물은 `Logs/ProfilerCliValidation/`에 있으며 버전 관리 대상이 아니다. 검증용 CPU 수치는 가짜 서비스를 사용하는 이전 캡처 fixture의 값으로, 실제 게임 성능 기준이 아니다.
