# Player-v2·HP 표적 QA 준비

최신: 후보01의 두 관측130검사씩/native exit0·PlayMode11/11, 후보02 HP전용 두 관측70검사씩/native exit0 및 PNG30상태 검증이 통과했다. **640 HP 혼색 DQA2-001 CLOSED**. [최종 QA 결과](../../../Planning/QA/dragon-v2-result-20261024.md)를 따른다. `v2-hp` / `-V2HpOnly`는 기존 `V2Health()`만 호출했고 최초 모션/PlayMode는 반복하지 않았다. 아래 최초 준비 기록의 NOT_RUN은 역사적 상태다. 완료한 검사를 근거 없이 다시 실행하지 않는다.

2026-10-24: **QA 소스 준비 / 컴파일·Unity·MCP·테스트·게임·빌드 NOT_RUN.** 최종 후보와 실행 인계 전에는 아래 명령을 실행하지 않는다. OS 마우스·키보드 실입력 보류는 유지한다.

관측 후보는 `Builds/Dragon-v2-QA/LastBellMvp.exe`, 일반 배포는 `Builds/Dragon-v2`, 앱 버전은 **0.9.1**, 데이터는 **mvp1.14.0**이다. 기존0.9.0/v1 빌드와 증거를 보존한다. 같은 launcher의 `-V2Only`는 v2 경로만 허용하고 신규 `Planning/Evidence/Dragon-v2/<candidate>` 아래에 두 해상도 케이스만 만든다. 기존 격리 save·PlayerPrefs 복구·소유 process handle·native exit·덮어쓰기 금지 보호를 재사용한다.

```powershell
& C:\Github\Test2\Tools\QA\Dragon-v1\run-campaign.ps1 `
  -Executable C:\Github\Test2\Builds\Dragon-v2-QA\LastBellMvp.exe `
  -CandidateId dragon-v2-candidate-01 -V2Only -Run
```

`v2` 모드는 기본 전체 캠페인·layout·story·cards를 호출하지 않는다. `DragonThemeSmokeProbe.V2.cs`는 기존 probe의 Development/Editor 전용 partial이며 제품 코드나 일반 빌드에 기능을 추가하지 않는다.

- **Idle:** 실제 actor의2주기를 실제 visual delta로 기록한다. 적 공격을 막기 위해 combat session만 고정하므로 `real-frame-idle-static-session`이라고 명시한다.
- **Attack:** 작은 적(stage1)과 boss(stage10), Idle pre-roll→수락→준비→200ms 피해1회→회복→Idle. 실제 선택 actor/clip을 읽는다. 0~120ms 첫 자세·120~200ms 준비·200~500ms 후속 경계는 기존 PlayMode 검사에서 확인한다.
- **Up/Back/Down:** 실제 위험의 접촉 전80ms 이내에 synthetic command를 보낸다. 수락/완벽 방어/HP, 실제 contactFrame 유지와 이후 회복 프레임, Idle/home 복귀를 확인한다. 고정8프레임/hold3 가정을 쓰지 않는다.
- **Run:** 실제 약한 적 처치→1920 논리px/2초 이동→도착. 선택 Run의 `40,40,60,60,40,40,60,60`/400ms, 두 주기 이상·실제 clip frame·root X 고정·960px/s·종료1920px를 확인한다.
- **HP:**1280×720/640×360 각각 player·일반 적·boss ×0/tiny/25/50/100, 총30상태. 기존 저장 UI를 표시 fixture로 표본화하고 실제 session HP를0으로 만들지 않는다. 폭160/184/280·높이36(640에서 절반), 중심 고정, 용량26/31/50과 실제 Canvas mesh의 밝은 채움 열 수를 대조한다. 보이는 도트/간격/충돌은 PNG 직접 검토 대상이다.

각 모션의 모든 렌더 프레임을 EOF에서 수집한다. 전역 번호·해상도·구간·모션을 포함한 PNG와 `frames.jsonl`에 SHA256/frameCount/실제 시각/delta/게임 시각/실제 clip 및 source frame/root/body offset/scale/HP를 대응시킨다. 같은 경로는 CreateNew로 거부한다. `v2-evidence.json`은 선택 clip의 holds/contact/피벗·각 구간의 실제 간격/누락/프레임 수·HP 상태를 기록한다. `timeline.csv`와 `events.csv`도 유지한다.

실시간 구간에서는 CPU 이미지만 모으고 구간 종료 후 PNG를 인코딩한다. 구간당 raw RGB 예산은768MiB이며 초과하면 실패로 남긴다. GPU/Unity 부가 메모리는 이 수치에 포함되지 않는다. 수집 자체의 렌더 부하는 남으므로 **60fps나 성능을 인증하지 않는다**. 실제 간격이 큰 구간이나 준비 포즈가 누락된 경우 검토에서 제한/실패로 기록한다. 정상속도 재생은 index의 realtime 간격을 사용하며 전체 원본을 프레임별로 검토한다. 해시/프레임 수만으로 시각 합격을 내리지 않는다.

[target-filters.json](target-filters.json)은 기존 PlayMode 메서드6개+HP 메서드(5수준)만 지정한다. 원래 Run 테스트의 `hero_on` 직접 선택을 없애고 actual selected actor로 변경했다. 원본 피벗 좌표60/17/111도 선택 clip의 피벗/캔버스를 기준으로 바꿨다. 발 접지의 시각 품질은 별도 검토다. 기존 Editor 설정 helper에는 `-Theme Dragon-v2`를 전달한다. 최종 인계에 지정된 Editor 외에 새 Editor를 시작하지 않는다.

`result.json.passed`는 자동 규칙/수집 결과만 나타낸다. `v2-evidence.json.visualReview`는 자동으로 **NOT_RUN**을 유지한다. 연속 화면에서 복장·가방·한 자루 검과 손 연결·팔다리·발·크기·접촉 및 HP 두 해상도를 실제 검토한 뒤 별도 QA 결과에 판정한다. 최종 일반 빌드 패키징/별도 실행/실입력 상태도 각각 기록한다.

소스 준비를 마치고 [해시와 동결 인계](../../../Planning/QA/dragon-player-v2-qa-source-handoff-20261024.md)를 남겼다. 최종 원형 기준은32×32/몸높이22px/8색/정수7배(720p154px)다. 총괄의 아트 통합·관측 빌드와 실행 인계를 기다리며 실행 허용은 아직 받지 않았다.
