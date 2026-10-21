# Permission-to-win / StageGame 재사용 규칙 추출

조사일: 2026-10-20 KST. 원본 `C:/Github/StageGame`을 읽기 전용으로 조사했다. 원본 수정, Unity 실행, 테스트 재실행, 빌드, 배포를 하지 않았다. 이 문서는 신규 프로젝트용 스킬의 **근거 자료**이며 아래 권고가 원본에 모두 구현되어 있다는 뜻은 아니다.

## 조사 범위와 우선순위

- 최상위 협업 계약: [AGENTS.md](C:/Github/StageGame/AGENTS.md).
- 최신 통합 결정: [CONTRACT.md](C:/Github/StageGame/Docs/Management/CONTRACT.md). 2026-10-19 기록까지 있으며, 과거 제외/승인 대기 문구보다 날짜가 최신인 변경을 우선한다고 명시한다.
- 데이터 형식: [FORMAT.md](C:/Github/StageGame/GameData/FORMAT.md), [export_data.py](C:/Github/StageGame/GameData/export_data.py).
- 현행 UI: `Client/Assets/StageGame/Prefabs/RankUI/`, `Editor/RankUiBuilder.cs`, `Scripts/UI/RankGameApp.cs`, 반응형 fitter와 독립 UI 검토 문서.
- 읽은 채팅: `클라이언트` — `01a138d9-9d94-71b2-8f03-290654cb2574`, `총괄 · 일정 및 출시` — `01a138d9-7ede-70d0-ba36-e84a05e6b105`. `read_thread` 최근 6턴씩에서 사용자 요청·최종 답변을 확인했다. 반환된 최신 턴은 10월 16일 작업까지이며 원본 계약에는 그 이후 변경이 있다. 전체 대화 이력 완독으로 표현하면 안 된다.

## A. 원본에서 확인한 사실

### 1. 폴더와 책임

| 항목 | 구체 근거 | 재사용할 의미 |
| --- | --- | --- |
| 역할별 변경 범위 | `AGENTS.md`는 Client/ 클라이언트, GameData/·Server/ 데이터/서버, Docs/Design/·Licence/ 기획/출시, Docs/QA/·Tests/ QA, 루트·Overview/·Docs/Management/ 총괄로 나눈다. | 한 파일의 최종 작성자를 정하고 담당 경계를 문서화한다. |
| 런타임과 에디터 분리 | `Scripts/StageGame.Runtime.asmdef`, `Editor/StageGame.Editor.asmdef`; 후자는 Editor 플랫폼만 포함하고 Runtime을 참조한다. | Excel importer와 prefab builder가 플레이어 빌드에 들어가지 않도록 경계를 둔다. |
| 책임별 코드 | `Scripts/Core/{ConnectFourBoard,AiSearch,GameData,StringTable,ProgressStore}.cs`, `Scripts/Platform/{Advertising,AdRemovalPurchases}.cs`, `Scripts/UI/`가 존재한다. | 게임 규칙, 외부 서비스 연결, 표시/입력을 분리할 근거다. 단, 폴더 분리만으로 엄격한 계층 아키텍처가 완성되었다는 증거는 아니다. |
| 계획/증거 분리 | `Overview/`, `Docs/Management/CONTRACT.md`, `Client/Evidence/<작업-날짜>/README.md`, `Docs/QA/*REVIEW*.md`. | 확정 결정과 구현·검증·출시 상태를 따로 기록한다. |

### 2. Excel 원본과 생성 데이터

[FORMAT.md](C:/Github/StageGame/GameData/FORMAT.md:32)의 실제 계약은 다음과 같다.

1. 게임 수치와 제품 문구의 원본은 `GameData/GameData_MVP.xlsx`다. MVP에는 Stages, AdConfig, Strings, Multiplayer 네 시트만 허용한다.
2. 1행은 필드명, 2행 자료형, 3행 추출 범위, 4행 한글 설명, 5행부터 레코드다. 단위·허용 범위·빈값 의미를 설명한다. 잘못된 메타행을 구형 표로 조용히 해석하지 않는다.
3. `ExportType=USE/NONE`은 추출 상태다. 게임 안에서 잠겼는지, 활성화됐는지와 구분한다. ID는 표시명과 행 번호에서 분리하며 삭제된 ID 재사용을 피한다.
4. 현재 exporter는 시트별 고정 스키마를 검증한다. `All/Client/Server/None` 메타가 있다고 범용 서버 필터가 구현된 것은 아니다. `long`, enum, 목록을 공통 지원한다고 일반화할 수 없다.
5. 원본 → 메타/타입/행 필터 → 키·참조·도메인 검증 → UTF-8 JSON → 클라이언트 Resources 인계다. 런타임은 XLSX나 Excel 라이브러리를 읽지 않는다.
6. `.xlsx`의 숫자·문구를 바꾸고 재생성한다. 생성 JSON 직접 편집은 금지다. 정렬과 출력 인코딩을 고정하고 시간값을 삽입하지 않아 같은 원본에서 동일 바이트를 만든다.
7. [export_data.py](C:/Github/StageGame/GameData/export_data.py:391)는 임시 출력과 이전 파일을 준비한 뒤 교체하며 예외 시 복구한다. 복구 실패/중단 시 `.export-incomplete`를 남겨 검사를 막는다. 여러 파일 전체의 파일시스템 트랜잭션·전원 장애 안전성을 보장하지 않는다고 코드가 명시한다.
8. `schemaVersion`은 런타임 형식 버전, `saveVersion`은 진행 저장 버전이다. 둘을 같은 개념으로 취급하지 않는다.
9. Excel은 프리팹 계층이나 시각 배치값의 원본이 아니다. Prefab/Sprite/AudioClip은 별도 에셋 카탈로그에서 의미 ID로 연결한다.

문구는 `Strings.SearchKey / Content_Kor / Content_Eng`가 원본이다. [StringTable.cs](C:/Github/StageGame/Client/Assets/StageGame/Scripts/Core/StringTable.cs:14)는 `Dictionary<string,StringEntry>`로 조회하고 중복·필수 번역·양 언어 포맷 인덱스를 검증한다. `HashSet<int>`로 포맷 인덱스 집합을 비교하며 `{0}` 반복과 언어별 순서 변경은 허용한다. 누락 키를 표시 문구로 조용히 대신하지 않고 예외로 드러낸다.

### 3. 저장된 프리팹과 런타임 소유권

- `AGENTS.md`는 화면과 공통 버튼을 저장된 Unity prefab으로 만들고 직렬화 참조를 사용하며 런타임 UI 트리 조립을 금지한다.
- `Prefabs/RankUI/`에는 `RankAppRoot`, `Lobby`, `Detail`, `Match`, `Result`, `Button`, `CloseButton`, `StageCard`, `Settings`, `Help`, `ActionConfirm`, `TurnToast`, `OnlinePanel`, `AdRemovalOffer`가 실제 저장되어 있다.
- [RankUiBuilder.cs](C:/Github/StageGame/Client/Assets/StageGame/Editor/RankUiBuilder.cs:643)의 `Save`는 기존 prefab을 `LoadPrefabContents`로 열어 수정하고 `SaveAsPrefabAsset`으로 저장한 뒤 `UnloadPrefabContents`로 정리한다. `Instance`는 같은 이름의 기존 자식이 있으면 재사용한다.
- 같은 파일 `Validate`는 직렬화된 ObjectReference 누락, 카드/돌/열 수, 별 이미지 참조, 문자열 키를 검사한다. “빌더를 실행했음”과 “저장 결과 참조가 유효함”을 구분한다.
- 초기 [ProjectBuilder.cs](C:/Github/StageGame/Client/Assets/StageGame/Editor/ProjectBuilder.cs:373)의 `Ensure`는 유효한 기존 prefab을 반환하고, 인식 못하는 파일이 같은 위치에 있으면 보호 오류를 낸다. `AssertTarget`는 실제 Unity 프로젝트 경로를 확인한다.
- [RankGameApp.cs](C:/Github/StageGame/Client/Assets/StageGame/Scripts/UI/RankGameApp.cs:13)는 화면·버튼·텍스트·이미지·음향 참조를 직렬화하고 이벤트와 화면 상태를 연결한다. `Start`가 데이터를 로드하고 진행·언어를 복원한다. `OnApplicationPause/OnDestroy`는 저장, 검색·보상·결과 연출 취소를 처리한다.
- [SafeAreaFitter.cs](C:/Github/StageGame/Client/Assets/StageGame/Scripts/UI/SafeAreaFitter.cs:33)는 `Canvas.willRenderCanvases`에서 OS/호스트 안전영역과 CSS/렌더 픽셀 차이를 반영한다. 비활성화 시 이벤트를 해제한다. [RankMatchFitter.cs](C:/Github/StageGame/Client/Assets/StageGame/Scripts/UI/RankMatchFitter.cs:6)는 기존 RectTransform을 조정한다. 런타임 배치 보정은 런타임 트리 생성과 다르다.

**주의:** `RankUiBuilder.Save`는 기존 prefab의 속성을 실제로 수정한다. 따라서 “기존 사용자 작업을 절대 덮어쓰지 않는 완전한 비파괴 빌더”라고 과장하지 않는다. 신규 스킬에는 소유 경로, 수정 대상, 변경 diff 및 재실행 검증을 요구해야 한다. 초기 `ProjectBuilder`에는 과거 개발 안내·초안 문구가 남아 있어 최신 제품 문구 기준으로 그대로 복사하면 안 된다.

### 4. 자료구조와 적용된 패턴

| 실제 구현 | 이유/효과 | 일반화 한계 |
| --- | --- | --- |
| `ConnectFourBoard.cs`: `int[42] cells`, 열별 `int[7] heights`, 읽기용 `IReadOnlyList<int> WinningCells`, `Clone()` | 고정 격자를 연속 배열로 보관하고 규칙을 렌더러·Unity와 분리한다. AI/되돌리기용 상태 복사 가능. | 7×6, ±1 플레이어, 낙하 규칙은 이 게임 전용이다. |
| `StringTable.cs`: Dictionary와 HashSet | 의미 키 조회, 중복·포맷 인덱스 검증. | 모든 컬렉션을 Dictionary로 통일할 근거는 아니다. |
| `ProgressStore.cs`: 직렬화 List + 런타임 HashSet | `matchId` 중복 결과 반영 방지. 저장 가능한 표현과 빠른 조회 표현을 분리한다. | 처리 ID 목록은 누적된다. 새 프로젝트에는 성장 한도와 보존 정책을 별도 설계해야 한다. |
| `Advertising.cs`: `IAdService`, `NoFillAds`, `EditorTestAds` | 서비스 경계를 interface로 두고 미충전·취소·중복 콜백을 시험할 수 있다. | 실 SDK 연결 완료의 증거는 아니다. |
| `Advertising.cs`: `RewardGate`의 request token, pending/granted | 취소 후 늦은 응답과 중복 완료가 보상을 중복 지급하지 못하게 한다. | 다른 비동기 기능도 수명·소유권 검토 없이 이 클래스를 그대로 쓰면 안 된다. |
| `AdRemovalPurchases.cs`: `IAdRemovalPurchases` | 플랫폼 표시 가격·상품 준비·검증된 권리를 분리한다. 구매 콜백만으로 권리를 부여하지 않는다. | 검증 서버 및 스토어 정책은 플랫폼별로 다시 확인해야 한다. |
| `RankTheme.cs`: ScriptableObject + characterId → Portrait 매핑 | Unity Sprite와 테마 참조를 데이터 수치·문구와 분리한다. | 현재 소수 캐릭터는 배열 순회다. 규모 확인 없이 사전 캐시나 복잡한 registry를 추가할 근거는 없다. |
| `RankGameApp.cs`: enum PendingAction, Coroutine, generation/matchId, lifecycle 취소 | 화면 이동·검색·광고 응답의 이전 실행 흔적을 통제한다. | 완성된 상태 패턴/DI 컨테이너/ECS/MVVM 구현이라고 부를 수 없다. 많은 책임이 App 클래스에 모여 있다. |

### 5. UI 설계·문구와 독립 검토

[멀티 UI layout-spec](C:/Github/StageGame/Docs/Design/multiplayer-ui-20261018/layout-spec.md)은 14개의 상태별 SVG 와이어프레임과 치수 계약을 만든 뒤 저장 prefab에 적용했다. 화면 전체 ScrollRect가 결과·하단 행동을 밀어내는 원인을 먼저 설명하고, 헤더/프로필/차례/가변 보드/결과·상태/고정 행동으로 재배치했다.

- 결과 텍스트와 상대 알림을 다른 슬롯으로 둔다. 리매치·연결 오류가 승패/점수를 지우면 안 된다.
- 와이어프레임의 임시 설명 문구를 제품 코드에 복사하지 않는다. 실제 제품 문자열은 기존 Excel 키를 재사용하거나 원본에 등록한다.
- 긴 영어, 실제 폰트의 preferredHeight, 짧은 화면, 호스트 안전영역, DPR을 확인한다. 줄 수를 늘릴 때 핵심 버튼 위치·입력 크기를 유지한다.
- 버튼 그림 크기와 실제 입력 크기를 분리한다. 이 프로젝트의 기준은 주요 행동 48 CSS px, 닫기 44 CSS px다. 보드의 공간적 선택은 별도 절충을 기록하며 불가능한 표본은 통과로 숨기지 않는다.
- 와이어프레임은 설계 증거다. 실제 Unity 저장 결과와 브라우저 캡처는 별도 검증이며 최종 구현 후 보정이 초안보다 우선한다.
- [로비 독립 검토](C:/Github/StageGame/Docs/QA/LOBBY-REDESIGN-REVIEW-20261018.md)는 구현 담당과 다른 사람이 소스·PNG·실행 결과를 검토했다. 아이콘 대비, 잠금 상태 식별, 다른 플레이어 캐시 오염 등 3개 문제를 요구한 뒤 해결 여부를 다시 확인했다. 검토자는 제품 소스를 직접 고치지 않았다.

두 채팅에서 직접 확인한 사용자 선호:

| 출처 턴 | 직접 요청 내용 | 재사용 원칙 |
| --- | --- | --- |
| 클라이언트 `01a1423f-5b3a-7390-bf09-2a40554510eb` | 잘 안 보이는 하단 텍스트 삭제. 착수 안내는 앱 실행 후 첫 판 첫 수에만 중앙 표시. `?`로 규칙 다시 열기. | 반복적인 상시 설명을 줄이고 필요한 순간에 짧게 안내하며 다시 찾아볼 수 있게 한다. |
| 클라이언트 `01a14226-fc7e-70b3-9868-e74ea7d9dc06` | 규칙은 최초 실행 때 한 번. | 안내 확인 상태와 단순 저장 존재를 구분한다. 최신 계약의 `rulesAcknowledged`가 누락 문제를 보완했다. |
| 클라이언트 `01a14081-0681-7ac1-be19-9796c89ae408` | 중앙 흰 글씨 가독성, 되돌리기/다시하기 배경이 너무 큼. | 가독성과 시각 밀도를 함께 다루며 그림 축소로 입력 영역까지 줄이지 않는다. |
| 총괄 `01a14279-75a1-7371-839f-08bec8306d81` | 상단에 붙은 패널을 다른 씬처럼 중앙으로 이동. | 화면군의 일관된 배치와 안전영역 기준을 지킨다. |
| 총괄 `01a14078-d6bc-7cb1-86e9-0ee5dc391470`, `01a1408b-7c74-7311-95c7-d9ca5baaddc2` | 기능별 한국어 커밋, Pages 워크트리는 빌드 결과물만, public 아래 배치로 생긴 404 수정. | 개발 소스와 배포 산출물 경계를 유지하고 실제 호스팅 진입 경로를 검증한다. |

### 6. 검증·통합 흐름

`AGENTS.md`와 최신 `CONTRACT.md`가 요구하는 흐름은 역할별 범위 합의 → 설계/데이터 계약 → 독립 검토 → 저장 에셋 구현 → 자동 검사 → 에디터/브라우저/기기 증거 → 총괄 통합 기록이다. 검토 코멘트가 해결되었는지 재확인하며, 자동 통과를 사람 플레이 재미·실기기·실광고 검증으로 확대하지 않는다.

수익·유지율·재미 목표를 사실로 쓰지 않고 가정으로 표시한다. 인증정보는 직접 읽지 않고 공식 도구 또는 사용자가 처리한다. 원본 에셋·소스 복사 없이 구조와 원칙을 추출한다.

## B. 신규 스킬에 넣을 권고

아래는 위 근거를 다른 프로젝트에 적용하기 위한 **권고**다.

1. 먼저 신규 게임의 Unity 루트·코드/에셋 루트·Excel 원본·생성 출력·역할 소유권을 계약에 적는다. 기존 폴더명/경로를 무조건 복제하지 않는다.
2. 순수 도메인 모델을 Unity UI에서 분리한다. 입력은 명령, 모델은 검증된 상태, 뷰는 표시를 맡긴다. 수명과 전이가 복잡해질 때 명시적 state machine을 도입하고, 작은 기능에 DI/ECS/MVVM부터 강요하지 않는다.
3. 컬렉션은 사용 이유와 최대 크기로 고른다. 고정 슬롯은 배열, 순서 있는 기록은 List, 의미 ID 조회는 Dictionary, 중복 판정은 HashSet을 우선 검토한다. 가변 개체가 실제 병목일 때만 pool을 도입한다.
4. Excel 숫자/문자열, Unity 에셋 카탈로그, 진행 저장, 플랫폼 권리를 별도 원본으로 둔다. 표시문구·행번호·배치 좌표를 도메인 ID와 혼합하지 않는다.
5. importer는 전체 검증을 통과한 뒤만 생성물을 교체한다. 오류 위치, 기존 정상 결과 보존, 동일 입력 재생성, 중단 복구의 범위를 검증한다.
6. 공통 버튼/닫기/패널과 화면을 prefab으로 저장한다. builder는 소유 경로와 기존 에셋 보호를 갖추고 재실행해 중복 자식·참조 손실·사용자 변경 덮어쓰기가 없는지 diff로 확인한다.
7. 런타임은 모델 갱신·표시 바인딩·입력·상태 전이·기존 RectTransform 조정만 맡는다. UI hierarchy를 화면마다 코드로 새로 만드는 방식을 기본으로 삼지 않는다.
8. 비동기 요청은 요청 ID/세대와 화면·플레이 수명을 연결한다. 재시도/취소/화면 이탈/백그라운드 후 늦은 콜백을 무시하며 보상·결과 저장은 멱등하게 한다.
9. UI는 상태 목록과 핵심 행동부터 결정 → 와이어프레임 → 실제 비율·긴 문구·오류/빈/잠금 상태 → 담당자 독립 검토 → 저장 prefab → 실제 화면 검증 순서로 진행한다.
10. 제품 문구는 사용자의 다음 행동, 현재 상태, 결과, 필요한 서사·법적 정보를 전달할 때만 둔다. 제작 의도, 구현 방식, “이 게임은…” 식 홍보형 설명, SDK 상태, 장식용 상시 안내는 제거한다. 교육이 필요하면 첫 필요 시점에 짧게 제공하고 도움말로 재접근시킨다.
11. “계획 / 제작 / 연결 / 자동 검증 / 실제 환경 검증 / 공개”를 별도로 기록한다. 재미·시장성·매출은 별도 검토와 데이터가 필요한 가설로 유지한다.

## C. 이 프로젝트 전용으로 남길 규칙

신규 스킬의 기본값으로 복제하지 않는다.

- 군대·내무반·계급·19인·30스테이지·7×6 4목, 200–1600 AI 표시 레이팅.
- 1080×1920 저장 기준과 720×1280 논리 배율 1.5, PC 최대480 CSS px, 특정 Toss inset 및 이 게임의 보드 최소 입력 절충.
- `C:/Github/StageGame/Client` 절대 경로, StageGame 네임스페이스, 특정 prefab/Strings 키, `stagegame.progress.v1`.
- 광고 정상 종료 매판, 광고 제거 약3,900원 계획, 무료 서버 운영 방식, 원스토어 무광고 초판→앱인토스 전략 및 별도 PvP 서버.
- 특정 곡·초상·폰트/아트 자산과 라이선스. 일반적인 라이선스 기록 규칙만 이전한다.
- 이 채팅의 “웹 빌드는 별도 사용자 승인”, “배포는 사용자가 담당”, “다른 세션 중단” 지시는 과거 작업의 범위다. 새로운 프로젝트의 모든 빌드·협업에 상시 승인 장벽으로 넣지 않는다.
- `public/` 중간 폴더 금지는 당시 Pages 업로드 경로 계약의 결과다. 모든 호스팅 구조에 대한 보편 규칙이 아니다.

## 추출 결과의 한계

이번 조사는 문서·소스 정적 읽기와 두 채팅의 최근 6턴이다. 원본 테스트 보고서를 읽은 것과 이번에 검증을 실행한 것을 구분한다. 레거시 `Prefabs/UI/`와 초기 builder가 남아 있으므로 최신 `RankUI`·최신 계약을 우선해야 한다. 이 자료는 설계 원칙을 재사용하기 위한 것이며 원본 전체 구조를 최선의 아키텍처로 인증하지 않는다.
