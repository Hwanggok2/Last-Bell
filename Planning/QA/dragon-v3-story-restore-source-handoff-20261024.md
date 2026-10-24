# Dragon-v3 대사 복원 렌더 순서 QA 인계 · 2026-10-24

**SOURCE_FROZEN_WAITING_FOR_OBSERVED_REBUILD.** 제품 통합 15개 파일 해시를 유지한 채 QA 소스만 수정했다. Player/Editor defines 소스 컴파일은 모두 오류 0이며 기존 DTO 경고는 27/28건이다. 실행기 PowerShell 구문 검사도 통과했다. Unity·native·EditMode·PlayMode를 추가 실행하지 않았다.

candidate02는 1280×720 첫 프로세스에서 **1,232 PASS / 1 FAIL**, native exit 1, timeout 없음, Console 오류 0으로 중단됐다. 20층 대사의 두 번째 페이지를 파일로 복원한 직후 `StoryNext`가 아직 활성화되기 전에 QA가 클릭했다. `V3Bind → MvpPresenter.Bind → BattlePresentation.Bind`가 `Overlay.Hide`를 호출하고, 다음 `LateUpdate → TickVisuals`에서 저장된 대사를 표시하는 흐름이다. `V3Fresh` 등 기존 준비 코드에는 렌더 대기가 있지만 이 복원 분기에 빠져 있었다. [실패 기록](../Evidence/Dragon-v3/dragon-v3-candidate-02/failure-triage.json)을 남겼다.

복원 후 `Capture("v3-story20-restored-page-1")`로 다음 렌더를 기다린 뒤 실제 화자·본문을 확인하고 기존 synthetic pointer 클릭을 수행하도록 수정했다. 복원 직후와 렌더 후 버튼 active/interactable 값도 기록하므로 새 관측에서 원인을 직접 대조할 수 있다. 임의 시간 지연이나 제품 오버레이 수정은 하지 않았다.

1280에서 이미 수집한 **51회 관측(고유 시나리오 50개)·5,416프레임**은 PNG 해시 불일치 0, 프레임 누락 0, 중복 파일 0이다. 적11 공격은 숲 적 검사와 플레이어 Hit 검사에서 같은 이름으로 두 차례 관측한다. 위 회피의 의도된 상승을 제외한 발 피벗/지면 화면 Y 차이는 플레이어·적 모두 최대 0px다. 이것은 피벗 좌표 검사이며 실제 발 그림의 접지·모션 자연스러움 전체 합격은 별도 시각 검토 대상이다. [분석 기록](../Evidence/Dragon-v3/dragon-v3-candidate-02/review/analysis.json)은 아직 시각 검토 전 상태로 구분한다. 실패 뒤의 대사·정산·재시작과 모든 640 검사는 미실행이다.

후보03 실행기는 `-StoryOnly1280`으로 아래 4프로세스를 수행한다. 후보01·02 파일이나 저장은 덮어쓰지 않는다.

1. 1280×720 `v3-story`: 새 격리 프로필로 20층 대사 파일 복원·완료·스킵·정산·20층 경계만 검사하고, 이어하기용 Stage11 저장을 준비한다. 기존 모션을 재수집하지 않는다.
2. 1280×720 `v3-resume`: 직전 프로세스의 **동일 저장 경로**로 native 재시작한다.
3. 640×360 `v3`: 아직 미실행인 전체 관측을 수행한다.
4. 640×360 `v3-resume`: 해당 640 프로필의 동일 저장 경로로 native 재시작한다.

새 모드는 다른 fresh 모드와 동일하게 비어 있는 격리 저장만 허용한다. 관측 결과에 `coverage=story20-and-resume-preparation-only`와 `full1280MotionRun=false`를 남긴다. candidate02의 정상 수집 범위는 제품 동일성 확인 후 최종 보고서에서 명시적으로 채택하며 후보03 단독으로 1280 전체를 재검사했다고 쓰지 않는다.

| 동결 파일 | SHA256 |
| --- | --- |
| DragonThemeSmokeProbe.cs | `b9cbd8ff412b0aa677cd6f17681f22d9c1d952294c67ae043accc9f7e76c7397` |
| DragonThemeSmokeProbe.V3.cs | `33e3e66e9feb0432cd60e05152301f89855c0427b617b2d82d1965c44d6df24e` |
| run-campaign.ps1 | `ce6efff08cf0e547df08eed65f0b90309c5b204a8d6f5ffa334de9983d786033` |

[동결 근거·컴파일 로그·diff](../Evidence/Dragon-v3/story-restore-source-fix-20261024-01/verification.json). 총괄의 새 관측 빌드와 실행권 인계를 기다린다. OS 실입력은 사용자 지시로 보류하며 사용자 기본 저장은 접근하지 않는다. Editor는 계속 총괄 소유다.
