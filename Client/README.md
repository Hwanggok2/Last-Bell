# 마지막 종지기 — Unity MVP

Unity **6000.3.15f1**, Built-in 2D + uGUI. 프로젝트 폴더는 `C:/Github/Test2/Client`다.

## 실행

- Windows: `C:/Github/Test2/Builds/Windows/LastBellMvp.exe`를 실행한다. exe 옆의 `LastBellMvp_Data`, `UnityPlayer.dll`, `MonoBleedingEdge`도 함께 있어야 한다.
- 다른 PC로 옮길 때는 `C:/Github/Test2/Builds/LastBellMvp-Windows.zip` 전체를 풀고 실행한다.
- Android 테스트 APK: `C:/Github/Test2/Builds/Android/LastBellMvp.apk`. Android8.0 이상 ARM64용 개발 빌드다. 원스토어에 게시한 빌드는 아니며 실제 Android 기기의 터치·성능 검증은 남아 있다.
- Unity Hub에서 `Client`를 Add한 뒤 `Assets/Game/Scenes/Mvp.unity`를 열고 Play한다.
- 새 저장에서는 Stage 1 회피·공격 튜토리얼이 바로 시작한다. 진행 중 저장을 다시 열면 일시정지 메뉴의 **계속**으로 재개한다.
- 마우스/터치: 왼쪽 네 삼각형으로 회피, 오른쪽 검으로 공격. 키보드: 방향키 또는 WASD, Space 공격, Esc 일시정지.
- 낮은 쓸기에는 ▲, 높은 쓸기에는 ▼, 앞에서 오는 찌르기에는 ◀, 뒤를 감싸는 원호에는 ▶로 대응한다. 일반 회피와 완벽 회피가 적의 종 게이지를 다르게 채운다.

## 연결한 범위

온1명, 첫10층, 일반3종·중간 보스1종·보스1종, 유물10종. 5·10층에서 서로 다른 유물3개 중 하나를 선택한다. 새 유물은 Lv.1, 재획득은 레벨 상승, 최대 레벨은 골드로 바뀐다. 신규 유물 장착은 로비에서 한다.

10층 보상 선택 후 **11층 전투로 바로 이어진다**. 이번11층은 자동 진행과 체크포인트 확인을 위한 경계 표본이다. 사망하면 로비에서 강화·유물 장착 후 보스 체크포인트 다음 층에서 시작한다. 11층을 완료하면 MVP 범위를 알린다. 최종50층 엔딩이 아니다.

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

## 빌드와 검증

- `Last Bell/Build/Windows MVP`: 개발용 Windows 실행 파일.
- `Last Bell/Build/Android Test APK`: ARM64, Android8.0 이상 개발용 APK. 기기 설치·성능 검증과 원스토어 제출 준비는 별도다.
- Test Runner의 EditMode: 전투 경계/연타/진행/보상/교환/저장 테스트.
- 개발 빌드에만 `-lastbell-smoke <출력폴더>`를 주면 실제 UI 명령으로 전체 흐름을 확인하고 Unity 카메라/Canvas를 PNG로 캡처한다. 별도 새 저장 폴더도 반드시 지정한다. 일반 실행에서는 이 자동 조작이 생성되지 않는다.
- 실행 근거와 최종 확인 결과: `C:/Github/Test2/Planning/Production/implementation-status.md`.

## 아트 범위

제공된 레퍼런스 방향으로 생성한 온8개·적15개 대표 자세와 성문 배경1장이다. 네 회피는 측면 점프/숙이기/후퇴/돌진이다. 적5종의 대기·공격·기절은 각 한 자세이며 피격과 이동 복귀는 절차 연출을 조합한다. 완성 프레임 애니메이션이나50종 완성본으로 보지 않는다.

전체 구역·50종 명단과 실제 초안은 `C:/Github/Test2/Planning/Production/index.html`에서 확인한다. 12~50층·NG+·나머지 캐릭터·전체 배경·사운드·출시용 모션 정리는 후속 범위다.
