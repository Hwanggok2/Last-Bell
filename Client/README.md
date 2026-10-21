# 마지막 종지기 — Unity MVP

Unity **6000.3.15f1**, Built-in 2D + uGUI. 프로젝트 폴더는 `C:/Github/Test2/Client`다.

최신 적용: 승인 달리기8자세·40/60ms 유지·400ms 주기·1,920px/2초. 기존 v5.2의 회피 방향 기호·문구, 정수 전투 스탯, 사망 결과·다시하기·로비를 적용했다. [검증 기록](../Planning/Evidence/Feedback-v52/verification.json). Android APK는 이번 수정으로 재빌드하지 않았다.


최신 전투 v5.9: 기본0·180·360ms 점멸 뒤540ms에 중앙 파동 접촉/타격. 빠른 패턴은160ms 박자/480ms 타격이며 연속 공격은 모든 방향 예고 후360/320ms 간격으로 실행한다. 화면 밖 원형 파동과 남은 공격 수의 종, 공격 버튼으로 상단 쳐내기, 선택한 후퇴/접근 모션을 적용했다. [실제 게임 재생과 검증](../Planning/Evidence/Combat-v59/index.html), [상세 명세](../Planning/Presentation/design-spec-v59.md). Android APK는 이전 버전이다.

## 실행

- Windows: `C:/Github/Test2/Builds/Windows/LastBellMvp.exe`를 실행한다. exe 옆의 `LastBellMvp_Data`, `UnityPlayer.dll`, `MonoBleedingEdge`도 함께 있어야 한다.
- 다른 PC로 옮길 때는 `C:/Github/Test2/Builds/LastBellMvp-Windows.zip` 전체를 풀고 실행한다.
- Android 테스트 APK: `C:/Github/Test2/Builds/Android/LastBellMvp.apk`. Android8.0 이상 ARM64용 개발 빌드다. 원스토어에 게시한 빌드는 아니며 실제 Android 기기의 터치·성능 검증은 남아 있다.
- Unity Hub에서 `Client`를 Add한 뒤 `Assets/Game/Scenes/Mvp.unity`를 열고 Play한다.
- 새 저장에서는 Stage 1 회피·공격 튜토리얼이 바로 시작한다. 진행 중 저장을 다시 열면 일시정지 메뉴의 **계속**으로 재개한다.
- 마우스/터치: 왼쪽 세 삼각형으로 회피, 오른쪽 검으로 공격. 키보드: 위/왼쪽/오른쪽 방향키 또는 WAD, Space 공격·상단 쳐내기, Esc 일시정지.
- 낮은 쓸기에는 ▲, 높은 쓸기에는 공격/Space, 앞에서 오는 찌르기에는 ◀, 뒤를 감싸는 원호에는 ▶로 대응한다. 일반 회피와 완벽 회피가 적의 종 게이지를 다르게 채운다.

2026-10-21 최신 기획의 저장 모션·전환·Stage·보스 등장·서사·음향을 씬과 프리팹에 연결했다. 공격은 접근→베기→귀환이며 명중200ms, 재사용500ms다. 회피160ms·완벽 회피60ms·재사용260ms와 예고100% 표시 프레임의 회피 입력 처리는 유지한다. 막타는 월드만120ms 흔들리고 피해 숫자는 우측 위로650ms 이동한다.

최신 통합본의 전용 QA는 [완료 기준](../Planning/QA/acceptance-20261021.md)과 [검사 계획](../Planning/QA/qa-plan-20261021.md)에 따른다. 이전 [전투 피드백 검사](../Planning/Reviews/telegraph-end-timing.md)는 이전 빌드의 증거이며 최신 통합본 통과를 뜻하지 않는다. Windows 후보와 검사 결과는 [최신 QA 보고서](../Planning/QA/result-20261021.md)를 확인한다. 기존 Android APK는 이번 통합으로 다시 빌드하지 않았다.

## 연결한 범위

온1명, 첫10층, 일반3종·중간 보스1종·보스1종, 유물10종. 5층은 골드와 종 파편1개만 지급한다. 10층은 종 파편3개와 첫 처치 대사3개 뒤 서로 다른 유물3개 중 하나를 선택한다. 새 유물은 Lv.1, 재획득은 레벨 상승, 최대 레벨은 골드로 바뀐다. 신규 유물 장착은 로비에서 한다.

10층 보상 선택 후 **2초 이동·700ms 조우/Stage 표시를 거쳐 11층 전투로 이어진다**. 이번11층은 자동 진행과 체크포인트 확인을 위한 경계 표본이다. 사망하면1800ms 연출 뒤 쓰러진 스테이지·이번 도전 획득 골드·종 파편을 보여 주는 결과 화면에 머문다. 다시하기는 보스 체크포인트 다음 층에서 시작하고, 로비 버튼으로 강화·유물 장착 화면에 돌아간다. 11층을 완료하면 MVP 범위를 알린다. 최종50층 엔딩이 아니다.

강화는 네 스탯의 +로 수행한다. 종10개가 레벨 묶음을 표시하고 11/21/31/41에서 돌파한다. 골드가 모자라면 필요한 파편 교환 견적을 표시한다. 교환을 확인한 뒤 **+를 다시 눌러야 강화**한다. 중간 보스는 종 파편1개, 보스는3개를 지급한다.

## 저장과 데이터

- 일반 실행 저장: `%USERPROFILE%/AppData/LocalLow/LastBellPrototype/Last Bell MVP/last-bell-save.json`. 이전 저장은 `.bak`에 둔다. 체크섬·임시 파일 교체와 백업 복구를 사용한다. 손상된 저장을 자동 초기화하지 않는다.
- 테스트 저장은 `-lastbell-save-dir <절대경로>`로 분리한다. 자동 검증은 실제 사용자 저장을 사용하지 않는다.
- 편집 원본: `C:/Github/Test2/GameData/mvp-catalog.json`. 프로젝트 루트에서 `python GameData/export_catalog.py`로 검증 후 생성한다. `--check`는 수정 없이 일치 여부만 확인한다.
- 생성본: `Assets/Game/Data/Generated/mvp-catalog.json`. 이 파일을 직접 수정하지 않는다.
- 로컬 MVP 지갑이며 실제 유료 구매·광고·서버 검증은 포함하지 않는다.

## 제작 구조

|경로|책임|
|---|---|
|Runtime/Core|순수 C# 전투·성장·보상·진행 상태, 저장 DTO|
|Runtime/Data|설정 DTO·ID 조회·런타임 검증|
|Runtime/Presentation|포인터/키보드 명령 전달, 상태 표시, 도트 숫자·파편·흔들림|
|Runtime/Bootstrap|카탈로그 로드·저장·오류 재시도|
|Runtime/Art|스프라이트 카탈로그|
|Editor|명시적인 임포트·Create Missing·Validate·빌드 명령|
|Prefabs, Scenes|저장된 GameView/Actor 프리팹과 Mvp 씬|
|Art/Source|생성 원본 사본. Unity metadata로만 스프라이트 분리|

UI 계층은 에디터에서 저장하며 Play 때 다시 만들지 않는다. `Last Bell/Build/Prepare MVP`는 없는 에셋만 만들고 검증한다. 기존 프리팹을 자동 덮어쓰지 않는다. 생성 이후 배치 변경은 저장된 에셋에서 편집한다.

명시적 `Last Bell/Presentation/Apply Approved MVP` 메뉴는 선택 아트·신규 VFX/배경/음향을 저장 씬에 연결한다. Play 중이나 저장하지 않은 Mvp 씬이 있으면 실행하지 않는다. `Validate Saved MVP`는 카탈로그·UI·실제 씬 참조를 검사한다. 사용 중인 MCP는 [LastBell 전용 8093 서버](../Tools/MCP/README.md)다.

## 빌드와 검증

- `Last Bell/Build/Windows MVP`: 개발용 Windows 실행 파일.
- `Last Bell/Build/Android Test APK`: ARM64, Android8.0 이상 개발용 APK. 기기 설치·성능 검증과 원스토어 제출 준비는 별도다.
- Test Runner의 EditMode: 전투 경계/연타/진행/보상/교환/저장 테스트.
- 개발 빌드에만 `-lastbell-smoke <출력폴더>`를 주면 실제 UI 명령으로 전체 흐름을 확인하고 Unity 카메라/Canvas를 PNG로 캡처한다. 별도 새 저장 폴더도 반드시 지정한다. 일반 실행에서는 이 자동 조작이 생성되지 않는다.
- 최신 적용 범위: [통합 기록](../Planning/Production/unity-presentation-integration-20261021.md). 실제 검사 결과·미해결 항목: [QA 보고서](../Planning/QA/result-20261021.md).

## 아트 범위

선택 원본281 PNG를 보존한6종/37모션 카탈로그를 사용한다. 온의 달리기는 사용자가 승인한 `run-dash-20261021-02`8자세/400ms이며, 상하 베기·4방향 회피·Idle·피격·귀환과 적 공격·피격을 연결했다. 일반3종은 모두 높이2, 중간보스3.2, 보스4로 적용한다. [아트 매니페스트와 제한](Assets/Game/Art/Presentation/README.md)에 실제 선택과 재사용을 기록했다.

몬스터 Idle·Stun은 아직 단일 포즈 fallback이다. 포효는 기존 공격1회+얼굴 원형 파동이며 별도 신규 포효 스프라이트는 아니다. 배경은5개 깊이 레이어의 저장 타일로 이동한다. 음향은 Kenney CC0 효과4개와 직접 합성8개를 사용하며 [출처·해시](Assets/Game/Audio/Presentation/audio-provenance.json)와 라이선스 사본을 포함한다.

전체 구역·50종 명단과 실제 초안은 `C:/Github/Test2/Planning/Production/index.html`에서 확인한다. 12~50층·NG+·나머지 캐릭터·전체 구역 배경·출시용 모션 정리는 후속 범위다.

승인 달리기: [실제 게임 재생](../Planning/Evidence/ApprovedRun-20261021/run-preview.html) · [104 EditMode/14 PlayMode 및 실제 실행 검증](../Planning/Evidence/ApprovedRun-20261021/verification.json). 전투 v5.5도 게임에 적용했다. [실제 게임 재생](../Planning/Evidence/Combat-v55/index.html)과 [검증 기록](../Planning/Evidence/Combat-v55/verification.json)을 확인한다.


## 전투 v5.5

금빛 위험 사각형을3회 점멸하며 모든 연타 예고 뒤 순서대로 타격한다. 화면 중앙 원형 파동과500ms 완벽 반격 기회, 적 ParticleSystem·무기 리본·잔상·충격 파문을 적용했다. `Last Bell/Combat/Apply v5.5`는 해당 UI와 저장된 CombatV55 프리팹/씬 참조만 명시적으로 마이그레이션한다. 기본 빌드 준비에도 CombatV55 에셋 검증이 포함된다.

개발 빌드의 `-lastbell-combat-v55-smoke <출력폴더>`는 별도 저장 폴더와 함께 사용한다. 실제 카메라 캡처, 예고·타격 시각, 반격, 화면 중앙/반경, 입자 상한, 정지/연출 줄이기를 검증한다. 일반 실행은 자동 조작을 설치하지 않는다. 원본 시간표는 `GameData/mvp-catalog.json`, 공유 전투 공간은 `Runtime/Core/CombatGeometry.cs`다.
