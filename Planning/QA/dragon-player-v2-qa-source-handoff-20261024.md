# Player-v2·HP QA 소스 동결 인계 요청

2026-10-24 KST. **SOURCE_FROZEN / PREPARED_NOT_RUN.** 총괄이 허용한 QA 소스 수정만 완료하고 아래 해시 기준으로 동결 인계한다. 총괄은 아트 완료 뒤 refresh/apply/관측 빌드를 준비하고 별도로 실행 인계를 줄 예정이다. 실행 금지와 사용자의 OS 실입력 보류 지시는 유지한다.

최종 원형 기준은 **32×32 캔버스·불투명 몸높이22px·8색·recommendedIntegerScale7 → 기준 화면154px**다. 기존18~20px 목표 대신 검사 계획에 반영했다. 이 후속 변경은 문서 기준만 갱신하며 QA 소스나 검사 범위를 추가하지 않는다.

## 준비 범위

- 기존 `DragonThemeSmokeProbe`에 `v2` 모드와 전용 partial을 연결했다. 실제 선택 actor의 Idle/Run/Attack/Up/Back/Down을 읽고, 매 렌더 프레임을 고유 PNG·SHA256·frame/time/clip/source/root/HP index로 연결한다. CPU 캡처 후 구간 종료 시 인코딩하며 누락·실제 간격을 기록한다.
- 공격1회당200ms 피해1회, 실제 Run400ms/두 주기/root X 고정/1920px 이동, 방어 수락/완벽 판정/clip contactFrame hold/후속 회복/HP 보존을 검사하도록 준비했다. 자동 입력은 기존 PointerCommand synthetic event다.
- HP0/tiny/25/50/100 ×3폭 ×2해상도를 준비했다. 용량26/31/50의 기대 값과 실제 Canvas mesh 채움 열 수·규격·중심을 대조하고 PNG를 남긴다. 일반 session HP와 분리된 표시 fixture다.
- 기존 Run 테스트2개에서 `hero_on` 강제 선택과 옛60/17/111 피벗을 제거했다. 관련 공격/회피 메서드에 실제 선택 clip과119/120/160/200/380ms 경계를 반영했다. 기존 HP 메서드는 확정5수준/3용량으로 바꿨다. 대량 신규 테스트는 추가하지 않았다.
- launcher `-V2Only`는1280×720/640×360 두 케이스만 실행하도록 준비했다. 허용 빌드는 `Builds/Dragon-v2-QA`, 출력은 신규 `Planning/Evidence/Dragon-v2/<candidate>`다. 기존 v1 동작과0.9.0 배포 파일은 보존한다. 일반 배포 경로는 `Builds/Dragon-v2`, 버전0.9.1/데이터mvp1.14.0이다.

[실행·증거 설명](../../Tools/QA/Dragon-v2/README.md), [표적 필터](../../Tools/QA/Dragon-v2/target-filters.json), [업데이트한 검사 계획](dragon-player-v2-hp-plan-20261024.md), [준비 소스 해시](dragon-player-v2-qa-source-hashes-20261024.json).

## 검토 수준과 제한

소스와 호출 API를 읽어 대조했고, 대상 테스트 파일의 `git diff --check`에서 공백 오류는 없었다. **QA에서는 컴파일·Unity/MCP·테스트·게임·빌드를 실행하지 않았다.** C#·PowerShell의 실제 실행 성공은 아직 검증하지 않았다. 제품 Runtime·씬·프리팹·아트는 QA에서 수정하지 않았다.

총괄의 후속 Unity 컴파일에서 `DragonThemeSmokeProbe.V2.cs(235,52) CS1501`이 보고됐다. 설치된 Unity6000.3.15f1의 `Editor/Data/Managed/UnityEngine/UnityEngine.UIModule.xml`에 명시된 인자 없는 `CanvasRenderer.GetMesh()`로 해당 probe만 수정했다. 반환값은 renderer의 현재 mesh이므로 읽기만 하고 QA가 파괴하지 않는다. 새 SHA256은 `6bd6400c41921ec70fe249ca0a9f44fc84d1da9ae50045ec02c5b976ce615602`이며 해시 인계를 갱신했다. **수정 후 컴파일은 재확인 대기**, 테스트/게임 실행 금지는 계속 유지한다.

각 실시간 구간의 raw RGB 보관 예산은768MiB다. GPU/Unity 부가 메모리는 별도이며 초과 시 프레임을 버리지 않고 실패시킨다. PNG 인코딩을 구간 밖으로 옮겨도 렌더/ReadPixels 부하는 남는다. 실제 캡처 간격과 빠른 준비 자세의 관측 여부를 확인해야 하며60fps·성능 합격으로 표시하지 않는다. Idle만 전투 시계를 고정하고 visual delta로 두 주기를 촬영한다는 구분도 남긴다.

자동 `result.passed`는 시각 합격이 아니다. 별도 시각 검토 상태는 기본 NOT_RUN이며 복장·가방·검/손 연결·팔다리·접지·픽셀 크기·실제 접촉·HP 간격은 정상속도와 프레임별 검토 후 판정한다. 기존 v1 PASS나 고유 프레임 해시만으로 새 플레이어 품질을 합격시키지 않는다.

다음 작업은 **최종 아트/HP 통합·관측 후보의 경로/해시·전용 Editor/표적 테스트 실행 인계가 도착한 뒤** 진행한다. 현재 동결 요청은 QA 소스 준비 완료 인계이며 실행 허가로 간주하지 않는다.
