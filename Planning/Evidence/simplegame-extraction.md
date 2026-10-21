# SimpleGame 재사용 규칙 추출

조사일: 2026-10-20. 원본 `C:\Github\SimpleGame`은 읽기만 했으며 수정하지 않았다. 이 문서는 신규 프로젝트 스킬의 근거 자료다. 아래 **확인 사실**, **재사용 권고**, **프로젝트 고유값**을 구별해서 적용한다.

## 1. 직접 확인한 사용자 선호

채팅은 `read_thread`로 읽었으며, 이전 답변의 성공 주장보다 사용자 발언과 현재 파일을 우선했다.

| 채팅·턴 근거 | 직접 확인한 요청 | 신규 스킬에 반영할 원칙 |
|---|---|---|
| `클라이언트 개발` · `019ff125-4947-7d70-aa5a-f145c2c3c989` · 턴 `01a1530f-0131-7673-bbc6-53274ff0f89a` | “캐릭터당 레벨 제한 없다 같은 건 대체 왜 텍스트로 달아두는 거야? 쓸데 없는 텍스트들 싹 다 없애” | 개발자가 요청한 구현 조건을 게임 안내 문구로 옮기지 않는다. 플레이어의 행동·판단·복구에 필요한 문구만 남긴다. |
| 같은 채팅 · 턴 `01a1531c-a678-77e0-b23f-0881332928c3` | 출석 패널에서 불필요한 UI를 없애고 7일 보상과 오늘 받기만 명확하게 표시 | 한 화면의 주행동을 먼저 정의하고, 보상·비용·현재 상태를 그림과 짧은 텍스트로 보여 준다. |
| 같은 채팅 · 턴 `01a1536c-5e9f-7e82-9922-3dcf72710f32` | 탑의 중복 시간 텍스트 제거, 별점 오른쪽 확대·능력치 아래 배치, 캐릭터 장점을 짧게 설명 | 정보의 우선순위·위치를 구체적으로 설계하며 긴 설명으로 배치 실패를 보완하지 않는다. |
| 같은 채팅 · 턴 `01a14f8c-7289-7f13-8f92-fc7e092c9422` | 담당 세션끼리 논의하고 새 재미·소비처·과금의 우선순위와 이유를 먼저 보고 | 구현 목록을 전략으로 포장하지 않는다. 대안·반론·선정 이유를 기록한다. |
| `전투 HUD 및 결과 화면 개선` · `019fe631-5c4a-70f0-b27d-25a5c22a5125` · 턴 `019fe6eb-cff3-7e92-9279-b6784af96ee7` | 수정해 둔 슬라이더를 임의로 새로 만들지 말고 복사해 추가 | 기존 수동 디자인을 기준 자산으로 삼아 확장한다. 누락 참조 수리가 리디자인 권한은 아니다. |
| 같은 채팅 · 턴 `019fe690-0bbd-76a1-bea0-2beaa5be8ad8` | 한국어 옆 영어 열, 기능별 GameData 파일 분리, 조이스틱 원형 복구 | 문자열과 수치를 책임별 원본으로 관리하고 사용자 시각 자산을 보존한다. |
| 같은 채팅 · 턴 `019fe686-e4bd-77f1-a2e4-c35cb92ce84d` | 로비·전투 설정을 같은 프리팹으로, 문맥에 따른 항목 활성화 | 실제 구조가 같은 화면은 공용 프리팹을 사용하고 문맥별 표시만 바꾼다. |

이전 채팅의 특정 구현 승인·가격·수치까지 새 프로젝트에 승계하지 않는다. 이번 요청에 명시된 담당자 검토 권한을 사용하며, 예전 문서에 등장한 모든 “승인”을 신규 확인 단계로 확대하지 않는다.

## 2. 프로젝트 구조와 의존성

**확인 사실**

- `Planning/ScriptStructure.md`는 Runtime/Editor/Tests 분리를 설명한다. 실제로 `Assets/Game/Runtime/SimpleGame.Runtime.asmdef`, `Assets/Game/Editor/SimpleGame.Editor.asmdef`, EditMode·PlayMode 테스트 asmdef가 존재한다.
- Runtime은 `Combat`, `Core`, `Data`, `Enemies`, `Entities`, `Lobby`, `Player`, `Presentation`, `UI`, `World` 책임별 폴더다.
- 최신 실제 트리에는 `Assets/Game/Platform/Common`, `Local`, `AppsInToss`, `OneStore` 어셈블리도 있다. `PlatformContracts.cs:6` 이후에 인증·저장·광고·결제·가시성 인터페이스가 정의된다.
- `PrototypeGameSession.cs`는 씬 시스템과 `GameDataManifest` 직렬화 참조, 런 상태를 소유한다. 초기화 완료 전 전투 Update를 진행하지 않으며 저장 초기화 실패를 UI에 전달한다.
- `PrototypeHUDPresenter.cs:95` 이후 버튼은 세션 명령에, 세션 이벤트는 View 표시 함수에 연결된다. `OnDestroy`에서 이벤트를 해제한다. 빈번한 요약 갱신은 0.1초 간격이다.

**재사용 권고**

```text
GameData 원본 ── Editor 파싱·검증 ── Generated SO
                                        ↓
수동 Catalog SO ──────────────────── GameDataManifest
                                        ↓
Scene/Prefab 참조 ── Session/Coordinator ── 도메인 규칙·Actor 모듈
                              ↕
                         Presenter ── View

플랫폼 SDK ── 플랫폼별 Adapter ── 공통 인터페이스 ── 게임 코드
```

- 작은 게임에서 출발할 때도 편집기 코드·런타임·플랫폼 SDK 경계는 분리한다. 기능이 없는 폴더·범용 프레임워크를 미리 늘리지 않는다.
- 세션/Coordinator는 참조 연결·초기화·중단·종료를 조율하고, 피해 계산·이동·저장·광고를 모두 구현하는 거대 클래스로 만들지 않는다.
- View는 표시와 입력 전달을 맡는다. 쿨다운을 독자적으로 감소시키거나 보상·전투 판정을 수행하지 않는다.
- 광고 보상, 구매 완료, 저장 소유권은 플랫폼 경계에서 결과를 검증한 뒤 게임 명령으로 전달한다.

## 3. 데이터의 단일 원본과 변환

**확인 사실**

`GameData/GameData_StructureGuide.md`가 가장 명확한 데이터 소유권 문서다.

| 종류 | 소유 위치 | 근거 |
|---|---|---|
| 수치·반복 정의·비용·확률 | 도메인별 Excel | `GameData_Balance/Enemy/Stage/Player/Skill/Lobby/Map/Character/Economy/String/Endgame.xlsx` 존재 |
| 표시 문자열·다국어 | 문자열 Excel | `SearchKey`/한국어/영어/포맷 인자 개수 규약 |
| Prefab·Sprite·AudioClip 연결 | 수동 Catalog SO | Unity Object 참조는 Excel의 계층/경로 문자열로 대체하지 않는 소유권 규칙 |
| Scene·Prefab의 계층·위치·색·연출 곡선 | Unity 자산 | `RectTransform` 등 수동 편집값 보존 |
| 런타임 조회 결과 | `Assets/Game/Data/Generated/*.asset` | Import 결과이므로 원본처럼 직접 편집하지 않음 |
| 연결 진입점 | `Assets/Game/Data/GameDataManifest.asset` | 생성 테이블과 수동 카탈로그의 직렬화 참조 |

- `GameDataExcelImporter.cs:202`의 Parse 경로는 강타입 모델을 구성하고 `:343`에서 참조를 검증한다.
- `:2786`의 `ImportFromPath`는 모델 파싱 후 Manifest를 확인한다. `ImportModel`은 맵·Unity 참조·이미지 해석을 먼저 검증하고 Undo 그룹을 등록한 뒤 적용한다. `:2866` 부근 catch는 Undo로 되돌리고 다시 오류를 던진다.
- `OpenXmlWorkbookReader.cs`는 Editor 전용 `.xlsx` reader다. Player 런타임은 workbook 파일을 직접 파싱하지 않는다.
- `GameStringTable.cs:50`은 직렬화 `List<GameStringEntry>`를 저장하며 `:124`의 lookup 생성으로 `Dictionary<string, GameStringEntry>` 조회 캐시를 만든다.
- 구조 가이드는 안정적인 의미 ID를 사용하고, 표시 이름·행 번호를 외래키로 쓰지 않으며 삭제 ID·enum 숫자의 재사용을 금한다.
- 4행 메타 포맷은 필드명/자료형/추출범위/한글 설명, 5행부터 데이터다. Import 대상에는 병합 셀·수식을 넣지 않고 계산 보고서를 별도 시트로 둔다.

**재사용 권고**

1. 새 테이블마다 소유 원본, 안정 ID/복합키, 단위, 허용 범위, 참조 대상을 정한다.
2. 파싱 → 타입·범위·중복·외래키·로컬라이징 placeholder 검증 → 전체 적용 순서를 지킨다. 실패 시 기존 정상 생성물과 수동 자산을 보존한다.
3. 원본과 생성물을 둘 다 수정하지 않는다. 수치는 원본 수정 후 재생성하고 결과 diff를 확인한다.
4. 계산 가능한 누적값은 중복 저장하지 않는다. 런타임용 파생값이 필요하면 importer가 계산한다.
5. 저장 스키마·ID·enum 변경은 명시적인 이전 경로와 구버전 저장 검증을 동반한다.

**주의: 문서의 목표와 구현을 구별**

- 구조 가이드의 `ExportType=USE/NONE`, `Client/Server/None` 열 필터 등에는 **향후 목표이며 지원 전 추가하지 말라**는 설명이 있다. 그 기능이 모든 현재 importer에서 구현됐다고 쓰지 않는다.
- `GameData_System.xlsx`는 목표 파일로 적혀 있지만 실제 운영 파일 목록에 없다. 빈 파일을 만들어 “구조 완료”로 간주하지 않는다.
- 이미지 데이터에 기존 파일명 해석 호환 경로가 있지만 신규 구조의 일반 원칙은 안정 ID→수동 Catalog 해석이다.

## 4. 프리팹 생성·수정과 수동 에셋 보존

**확인 사실**

- `AGENTS.md`는 기존 `.prefab/.unity/.asset/animation/material/UI`를 사용자 저작물로 취급한다.
- 2026-08-05 ControlSettingsPanel 덮어쓰기 사고를 기록하고, 새 View가 없다는 이유로 생성기로 기존 UI를 재생성하지 말라고 명시한다.
- `PrototypeSceneBuilder.cs:1901`의 `EnsureControlSettingsPanelPrefab`은 설정 완료면 기존 자산 반환, 자산이 있으나 참조가 미완료면 `UpgradeUiPrefabInPlace`, 자산이 정말 없을 때만 이전/생성 경로를 사용한다.
- `UpgradeUiPrefabInPlace`는 `:857`에 존재한다. 기존 프리팹을 로드해 필요한 컴포넌트·참조를 연결하는 용도다.
- `AGENTS.md`는 스킬 시각 오프셋·콤보 전방 거리·프리뷰 적 거리/방향 역시 수동 조정값으로 보존하도록 명시한다.

**재사용 권고: 에디터 도구 계약**

1. `CreateMissing`: 경로가 없을 때만 생성한다.
2. `MigrateExisting`: `PrefabUtility.LoadPrefabContents`로 원본을 새로 로드하고 명시된 필드·참조만 변경한다. `try/finally`로 Unload한다.
3. `Validate`: 필수 자식 경로가 없으면 정확한 경로를 보고한다. “검증 실패”가 자동 재생성의 근거가 되지 않는다.
4. 참조 수리 전후 YAML diff와 대표 시각 속성, 실제 화면을 비교한다. 계층·위치·색·문구가 광범위하게 변하면 중단하고 변경 범위를 축소한다.
5. 프리뷰 복제본이나 오래된 컴포넌트 스냅샷을 소스 프리팹 전체에 저장하지 않는다.
6. 새 UI 요소는 같은 화면의 수동 조정 요소를 복제하거나 공용 컴포넌트를 사용한다.

정적 화면은 Editor에서 자산화하고 런타임에는 표시·데이터만 바꾸는 것이 기본이다. 다만 실제 `PrototypeHUDPresenter.cs:68`에는 누락된 `OffscreenEnemyIndicators`를 런타임 생성하는 경로가 있으므로 **현재 프로젝트에서 런타임 UI 생성이 전혀 없다고 주장하면 틀린다**. 신규 프로젝트에서는 동적 목록·일시 효과·검증된 fallback 등 생성 이유를 명시하고, 고정 레이아웃을 런타임 코드로 다시 짜지 않는 방향을 권고한다.

## 5. 자료구조와 디자인 패턴

| 확인한 구조·경로 | 실제 이유 | 재사용 범위 |
|---|---|---|
| `ComponentPrefabPool.cs:40`, `:288`: Prefab/Instance Dictionary, 비활성 Stack, 중복 반납 방지 HashSet, 관리 집합 | 생성·반납 추적, 상한 초과 폐기, 장면 정리 | 반복 생성 빈도가 높고 수명이 짧은 적·투사체·팝업·VFX에 측정 후 적용 |
| `EnemyWorldService.cs:12`: 2m 셀 Dictionary, 적→점유셀 Dictionary, 버킷 List pool, 후보 List/HashSet | 근접 분리 대상의 전체 쌍 검색을 줄임 | 다수 개체 근접 질의가 병목일 때. 소수 적 타이밍 게임에 선도입하지 않음 |
| `FillEnemiesInRadius(..., List<EnemyBase> results)` | 호출자가 결과 버퍼 재사용 | 매 틱 반복 범위 검색. 현재 다른 Collect 함수에는 새 List 생성도 남아 있음 |
| `FilthProjectile.cs:18`, `:309`: 적→SpawnGeneration Dictionary | 같은 pooled 인스턴스의 새 생명과 재진입을 구분 | 한 번만 적중·발동하는 효과에서 객체 주소만으로 이력 추적하지 않기 |
| `EnemyActor` + 공격/이동/체력 모듈, `EnemyStateMachine.cs` | 데이터만 다른 빈 파생 클래스를 줄이고 행동을 기능별로 구성 | 얕은 상속·컴포지션. 클래스 이름만으로 완성된 GoF State/Strategy 구현이라고 과장하지 않음 |
| Session 이벤트 + `PrototypeHUDPresenter` + View | 게임 규칙과 UI 바인딩 분리 | 일반 C# event/명령으로 충분한 범위부터 시작 |
| 여러 `*.partial.cs` 책임 파일 | 기존 MonoBehaviour·직렬화 정체성 유지하며 파일 분할 | 변경 충돌/가독성 개선. CPU·메모리 최적화라고 표현하지 않음 |

**스킬 규칙으로 요약:** List는 순회, Dictionary는 ID·키 조회, HashSet은 중복 방지·멤버십, Queue/Stack은 소비 순서에 실제 요구가 있을 때 쓴다. 자료구조·패턴은 기능 요구와 측정 결과에서 선택하며, 새 게임에 모든 패턴을 복사하지 않는다. 생명주기·동작이 다른 Player/Enemy 체력 계층을 이름이 비슷하다는 이유로 범용화하지 않는다.

## 6. UI 배치·문구·검증 워크플로우

**확인 사실**

- `Planning/UIExperience_20260920.md`는 상태를 “HTML 시안 검토 단계 / Unity 미적용”으로 표시하고, 세로·가로 정보 계층 확인 → 검토된 화면 순차 적용을 명시한다. 시안이 원본 UI 전체 재생성 근거가 되지 않는다고 적혀 있다.
- 같은 문서는 UI가 전투 상태의 읽기 전용 스냅샷을 사용하며 자체 타이머/규칙을 추가하지 않도록 한다. 실제 상태와 미리보기 가정을 구분한다.
- `FixedAspectLayout.cs`는 기준 크기·Safe Area·HUD 확장 여부를 직렬화하고 화면 크기/Safe Area 변경 시 재계산한다. 중첩 레이아웃에서 Safe Area를 중복 적용하지 않는 옵션이 있다.
- `ModalPanelSafeArea.cs`는 전체 화면 backdrop을 유지하며 내용 영역만 안전 영역 아래로 배치한다.
- `UiRatioRegressionCapture.cs`, `UiRatioRegressionComparison.cs`, `ModalPanelSafeAreaTests.cs`, `BattleHudEdgeLayoutPlayModeTests.cs`가 존재한다.

**재사용 권고**

1. 화면마다 주행동 1개, 필수 정보, 상태(빈 상태/잠김/진행/성공/실패)를 먼저 정한다.
2. 텍스트·박스 중심 와이어프레임에서 위치·크기·우선순위를 결정하고 실제 게임 에셋을 얹은 시안을 검토한다. 모바일 기준 화면과 지원할 다른 비율을 함께 본다.
3. 담당자가 플레이어 행동·터치 접근·가독성·문구 필요성을 검토하고 결정 기록을 남긴 뒤 프리팹에 반영한다.
4. “요청사항을 구현했다”, “제한이 없다”, “서버와 연동된다” 같은 구현 설명은 게임에 넣지 않는다. 행동/비용/보상/조건/오류 복구 문구는 필요한 위치에 남긴다.
5. 색만으로 선택·잠김·위험을 구분하지 않는다. 숫자·아이콘·상태 표시와 결합한다.
6. UI 로직 테스트와 별도로 실제 캡처를 확인한다. 텍스트 잘림, 한글/영어, 버튼 중첩, 터치 영역, 긴 숫자, Safe Area, 모달, 입력 차단을 본다.
7. 시안 완료, Unity 반영, 실행 검증, 실제 플랫폼 연결을 서로 다른 완료 상태로 기록한다.

## 7. 그대로 복사하지 않을 프로젝트 고유값과 문서 한계

- 1080×1920 기준 크기, HUD 0.1초 갱신, 공간 셀 2m, 팝업 16개 prewarm/64개 상한, 특정 투사체 16개 비활성 상한은 SimpleGame의 선택값이다. 신규 게임은 화면·기기·동시 개체 수에서 재결정한다.
- 스킬·직업·룬·도전의 탑·아우라·8/10분 전투 및 원플레이/앱인토스 서비스 계약은 프로젝트 고유 기능이다.
- `ArchitectureDesignDocument.md`의 2026-08-02 설계에는 빈 Enemy 파생 클래스 예시와 “SDK 미설치” 등 현재 코드보다 오래된 내용이 있다. 현 구현 근거는 실제 파일을 우선한다.
- `ScriptStructure.md`의 벤치마크는 Unity Editor Mono 미시 측정값이다. 실제 모바일 프레임 성능이나 새 게임의 개선율로 인용하지 않는다.
- 이 조사에서는 소스·문서·채팅 근거를 확인했으며 Unity 실행, 테스트 재실행, prefab 이미지 렌더링은 하지 않았다. 과거 테스트 통과 수치를 이번 조사 검증 결과로 주장하지 않는다.

## 8. 새 스킬용 최소 체크리스트

- [ ] 원본/생성물/수동 자산/저장 데이터 소유권 표
- [ ] Runtime/Editor/Platform 경계와 씬 초기화·종료 흐름
- [ ] ID·참조·수치·문자열 전체 검증 후 Import
- [ ] CreateMissing/MigrateExisting/Validate 구분 및 수동 자산 diff 보존
- [ ] 패턴·자료구조 선택의 실제 이유와 pool reset/cleanup 계약
- [ ] 와이어프레임·시안·담당자 검토·배치 근거
- [ ] 최소 게임 문구, 한국어/영어 표시 및 Safe Area 확인
- [ ] 해당 변경에 필요한 검증과 실제 화면 확인
- [ ] 초안/구현/검증/플랫폼 실연동 상태 구별
