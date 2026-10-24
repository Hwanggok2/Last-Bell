# Dragon v1 관측 도구

candidate-01 자동 캠페인, candidate-02 대사/카드 표적 검사, candidate-03 카드 배치 표적 검사를 종료했다. 요청된 자동·대표 시각 범위는 PASS이며 한계와 수정 이력은 [최종 QA 결과](../../../Planning/QA/dragon-v1-result-20261024.md)를 따른다. 일반 빌드·ZIP 검증/해시는 [배포 기록](../../../Planning/Production/dragon-v1-release.json)에 있고, 일반 빌드의 별도 실행은 하지 않았다. 이 도구를 근거 없이 다시 실행하지 않는다.

## 진입과 저장 보호

- Runtime/Bootstrap의 `DragonThemeSmokeProbe.cs`는 `UNITY_EDITOR || DEVELOPMENT_BUILD` 조건부다. 일반 실행에는 설치되지 않고 `-dragon-theme-smoke <절대 output>`가 있을 때만 설치한다. 최종 `BuildOptions.None` 빌드에서는 클래스 자체가 제외된다.
- `-lastbell-save-dir <절대경로>`가 필수이며 output의 엄격한 하위 경로여야 한다. 기본 사용자 저장과 겹치는 output,상위/같은 save 경로,경로 junction/symlink,중복 인자,기존 결과 덮어쓰기,다른 smoke 동시 실행은 거부한다.
- 현재 GameBootstrap은 Start에서 저장을 초기화한다. probe의 AfterSceneLoad 콜백이 명시적 QA 실행일 때만 먼저 bootstrap을 비활성화한다. 검증 실패면 초기화하지 않고 standalone exit64. 정상 경로만 명시적으로 Initialize한 뒤 해당 테스트 씬의 bootstrap을 제거하여 별도 fixture 저장을 quit/autosave가 덮지 않게 한다. 제품 소스/저장 프리팹/원본 게임 로직은 변경하지 않는다.
- launcher는 `Builds/Dragon-v1-QA` 후보만 받고 신규 `Planning/Evidence/Dragon-v1/<candidate>`에 기록한다. 사용자 저장을 복사하지 않는다. PlayerPrefs 게임/화면 범위만 백업하고 finally에서 복구·검증한다. 프로세스 timeout은 이 launcher가 만든 player handle만 종료하며 정상 exit로 보고하지 않는다.

## 인계 이후의 실행 예

```powershell
& C:\Github\Test2\Tools\QA\Dragon-v1\run-campaign.ps1 `
  -Executable C:\Github\Test2\Builds\Dragon-v1-QA\LastBellMvp.exe `
  -CandidateId dragon-v1-candidate-01 -Run
```

이는 사용자에게 새 승인을 요구하는 절차가 아니라,준비 코드의 실수 실행을 막는 명시적 실행 인자다. 총괄 후보 인계 전에는 실행하지 않는다.

| case | 범위 | 시간/입력 근거 |
| --- | --- | --- |
|full1280×720|fresh 의뢰 skip→선택/강화→튜토리얼→1~10→3페이지 대사→11→정산/유물→+1 강화/MAX 견적→11 재출전; 대표 UI/모션|경로는6×50ms 논리 tick/렌더 프레임,UI 버튼 raycast+synthetic click와 직접 PointerCommand. OS 입력 아님|
|layout960×540,640×360|1/3/5/6/10 몸체,세 버튼 중심/안전영역,로비/강화/보관함/pause/3페이지/정산 화면|명시된 synthetic save fixture·UI 이벤트·실제 저장 씬 렌더. 전체 전투 반복 없음|
|resume1280×720|full의 격리 최종 저장 사본으로 별도 프로세스 bootstrap 복원·계속|새 output 하위로 save/expectation을 복사. 이전 결과/프로필 덮어쓰기 없음|
|review1280×720,640×360|첫 보스 대사0.35초 유지/flash 소멸,유물 카드 선택 전·선택 후·확정|`-ReviewOnly`로만 두 프로세스를 실행. 전체 경로/모션 갤러리 제외|
|cards1280×720,640×360|보상 상태를 만든 뒤 유물 카드 선택 전·선택 후·확정|`-CardsOnly`로 두 프로세스만 실행. 대사 대기/촬영도 생략|

full에만 실제 프레임 시계를 사용하는 별도 관측을 포함한다: 1/3/5/6/10의 Idle/준비/공격·피격,세 방어 입력의 표시,1→2/4→5/9→10 처치/달리기/배경/등장. 일반 이동1920과10→11의 기존1020을 혼동하지 않는다. fixture에서 약한 적HP를 사용하는 전환은 timeline의 `real-frame-fixture`로 명시한다. 방어 입력3종 모션 표본이 모두 성공 판정 검증을 뜻하지는 않는다. Core/DragonUiContractTests는 별도 기존 검사를 사용한다.

PNG는 LateUpdate 뒤 EndOfFrame에서 같은 WorldCamera와 저장 Canvas를 임시 RenderTexture로 렌더하고 즉시 원상 복구한다. 계측 부하가 있으므로 프레임 시간은 성능 인증으로 쓰지 않는다. 정상속도 영상/실입력과 가속 경로를 섞어 PASS로 표현하지 않는다.

## 산출물과 제한

`result.json`(assertions/errors/환경),`events.csv`(상태 이벤트),`timeline.csv`(시계/모션 프레임/actor 좌표/parallax2레이어 좌표),PNG,`*-layout.json`(실제 screen 하단 원점 text/control bounds),`expected-save.json`,`processes.json`,후보 파일 hash 및 설정 복구 기록을 남긴다. JSON passed와 native exit code를 모두 확인한다. 간헐적 QA-003은 성공1회로 해결 판정하지 않는다.

자동 판정이 시각 품질 전체를 대체하지 않는다. 캡처를 실제 검토하고 미검증 AC를 남긴다. 50층,모바일 실기기/성능,비대칭 Safe Area 주입,전체 유물/저장 오류 조합,OS 마우스·키보드는 이 작은 probe에 복제하지 않는다. OS 실입력은 기존 사용자 보류 지시와 총괄10월24일 재확인에 따라 수행하지 않는다. 실패한 후보를 자동 재시도하지 않는다.

candidate-01에서 `Observe`의 호출별 PNG 번호 재시작 때문에 같은 모션의 초기 촬영 일부가 덮였다. CSV는 온전하지만 이 PNG 파일명 순서를 연속 영상으로 사용하지 않는다. probe를 실행 전체 고유 번호로 수정했고, 이후 새 인계 빌드에 포함한다. 기존 증거를 수정하거나 전체 캠페인을 재실행하지 않는다.

Unity 테스트는 총괄의 후속 인계에 따라 기존 전용8093 Editor에서 실행한다. `editor-prefs.ps1`는 화면 테스트 전후 Last Bell의 설정 범위만 백업/복원한다. 게임 씬을 열어 Play하거나 새 Editor를 실행하지 않는다. 테스트용 복사본은 해시 대조만 했으며 실행하지 않았다.
