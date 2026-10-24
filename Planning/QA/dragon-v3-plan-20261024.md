# Dragon-v3 표적 QA 계획 · 2026-10-24

**상태: 관측 변경 범위 QA 완료 / DQA3-003 CLOSED.** 최신 판정과 후보01~05 실패·재검사 이력은 [최종 결과](dragon-v3-result-20261024.md)를 따른다. 기존 전체 관측에05의 두 해상도 표적 검사(각168 PASS, native exit0)를 더해19/20 실제 콤보 복귀와 자동 Idle 공백을 해소했다. 앱 **0.9.2**, 데이터 **mvp1.15.0**, 관측 폴더 `Builds/Dragon-v3-QA` 기준이다. 저장 EditMode는 [별도 결과](dragon-v3-save-selector-result-20261024.md)의 **5/5 PASS**를 유지했고 재실행하지 않았다. Editor·일반 배포 패키지는 총괄 소유다. OS 실입력·모바일은 보류하며 **사용자 기본 저장 프로필에는 접근하지 않았다.** 아래는 검사 범위·설계 기록이다.

## 범위와 완료 기준

| ID | 변경 범위 | 완료 기준 |
| --- | --- | --- |
| V3-01 | Stage 11~20 신규 적 도트 | 10개 층 각각의 stage → enemy → art → 실제 Actor/clip 연결이 최종 인계표와 일치한다. 옛 적 외형·누락 참조·임시 fallback이 남지 않는다. Idle·실제로 선택되는 Attack·Stun을 전부 확인한다 |
| V3-02 | Stage 15·20 보스 | 숲송이 대장·기블의 확정 체격과 바닥 기준이 모든 동작에서 유지된다. 진입 확대·포효·카메라 복귀가 새 아트에 맞고 이름·HP·전투 영역이 잘리지 않는다. 포효 중 전투 차단과 종료 후 진행이 유지된다 |
| V3-03 | Stage 20 뒤 줄거리 | 옛 집행관·사슬·종지기 대사를 새 숲 이야기로 교체한다. 확정된 화자·대사·이름이 표시되고 다음/건너뛰기·저장 복원·보상·완주 연결에 누락이나 중복이 없다. MVP는 20층까지이며 21층 전투나 50층 구현으로 확대하지 않는다 |
| V3-04 | 캐릭터 표시명 | `on → 루카`, `dam → 다렌`, `rin → 미엘`. 영구 ID와 저장 참조는 그대로이며 `on`만 해금된다. 선택·상세/강화·대사 등 실제 노출 지점의 이름이 일치하고 잠긴 캐릭터가 선택되지 않는다 |
| V3-05 | exe 신규 시작 저장 분리 | Client 확정 저장 계약에 따라 새 영역의 첫 시작은 1층으로 진입하고 기존 10층 체크포인트/진행을 가져오지 않는다. 같은 새 영역의 재실행에서는 저장한 진행과 재화·성장·선택 상태를 보존한다. 매 실행 초기화는 불합격이다 |
| V3-06 | 기존 사용자 세이브 보존 | 사용자 기본 프로필에 접근하지 않고, 격리된 가상 기본 폴더의 구 저장 sentinel과 신규 namespace로 분리·보존 계약을 검증한다. 실제 사용자 파일 해시 전후 감사는 미실시로 명시한다. 기존 파일 삭제·이동·덮어쓰기·초기화·자동 이전을 하지 않는다 |
| V3-07 | 전용 스킬은 기획만 | 캐릭터별 전용 스킬의 기획과 실제 구현 상태를 구분한다. 노출한 설명에는 미구현 상태가 명확하고 사용 가능한 버튼·쿨타임·해금/구매 약속으로 오해시키지 않는다. 새 스킬 실행 명령·피해/버프·저장 상태가 추가되지 않는다. 기존 기본 공격·회피·쳐내기와 자동 유물 효과는 보존한다 |
| V3-08 | 후보·증거·종료 | 최종 후보의 버전/해시, 실제 실행 결과, 화면 증거, 저장 보호 결과를 연결한다. 실패·미실시를 숨기지 않고 사용한 Editor와 설정을 반환한다. 일반 배포 빌드·ZIP 검증 주체와 QA 관측 빌드를 구분한다 |
| V3-09 | 스테이지별 지면과 동작별 접지 | 1~20층의 ground base 정의와 실제 배경/배우 기준이 일치한다. 신규 11~20과 지역 전환에서 플레이어·적·15/20 보스의 발 기준을 대조하고, Idle/Attack/Run/Up/Back/Down/Hit의 의도한 공중·전진 이동을 접지 결함과 구분한다 |
| V3-10 | 플레이어·NPC 스타일과 모션 | reviewer의 비교 기준과 최종 아트를 같은 화면 배율에서 대조한다. 도트 밀도·윤곽·명암·신체 비례·복장 정체성이 조화롭고 각 동작의 준비/무게 이동/접촉/복귀가 자연스럽다. Player-v3 채택 여부와 해당 검증 범위를 명시한다 |
| V3-11 | 차별성·재미 기획 | 총괄의 신규 기획에서 핵심 선택·보상·재도전의 재미와 현재 구현/향후 제안을 구분한다. 기획 추가를 스킬 구현이나 재미 실증 완료로 표시하지 않는다 |
| V3-12 | 신규 적 공격 효과 연결 | 예정된10종 공격 구간에서 고블린6종의 Metal 무기 궤적과 숲송이4종의 Wood 입자/지면 충격을 확인한다. 효과의 위치·방향·발생 시점이 실제 공격 및 공통 지면과 맞고, 동작 종료/다음 fixture에 이전 효과가 남지 않는다 |

새 배경 제작, Stage 21~50, 캐릭터 추가 해금, 전용 스킬 런타임 구현, 밸런스·경제·조작 전면 변경은 이번 범위가 아니다. 기존 Dragon-v2 합격을 신규 적·이름·저장 분리·지면의 합격 근거로 전용하지 않는다. 추가 인계에 따라 플레이어 각 모션의 지면/스타일 정합은 이번 범위에 포함한다. HP 전수 검사는 접점에서 이상이 보이거나 관련 변경이 인계된 경우에만 추가한다.

## 담당과 편집 경계

| 담당 | 소유 범위 | QA가 받을 근거 |
| --- | --- | --- |
| 총괄(root) | 기획·캐릭터 표시명·차별성/재미 문서, 최종 통합/빌드 | 확정 이름·대사·스킬 미구현 표기, 최종 후보/실행권 |
| Client | 새 저장 정책·스테이지 지면/접지 코드 | 저장 경로 계약, ground base 정의·적용 범위·좌표계·허용 offset |
| art | 11~20의 신규 몬스터 10개 연결 | 원형·동작·몸높이·발 기준·pivot·실제 art/clip manifest |
| reviewer | 플레이어와 NPC 스타일 비교, Player-v3 수정 필요 판단 | 비교 화면·판단 이유·채택 기준; 수정이 채택되면 담당과 최종 자산 인계 |
| QA | 이 계획, 완료 소스 인계 후 표적 검증과 결과 | 독립 관측·결함·범위 기록. 다른 담당의 파일을 선행 수정하지 않음 |

## 확정 계약과 남은 인계 사항

최신 총괄 계약은 [v3 작업 계약](../Production/dragon-v3-work-contract.md)과 [v3 데이터 바인딩](../Production/dragon-v3-data-bindings.json)을 따른다. 아래 최초 관찰의 옛 이름·데이터 값은 변경 전 기준이다.

- 저장: `SaveDirectorySelector.Select(persistentDirectory,args,true)`는 `persistent/Playtests/dragon-0.9.2`를 선택한다. `-lastbell-save-dir`가 우선한다. Bootstrap의 활성 조건은 `UNITY_STANDALONE_WIN && LASTBELL_PLAYTEST_092 && !UNITY_EDITOR`이며 관측/일반 빌드 모두 이 define을 넣는 것이 총괄 계약이다. 기본 사용자 프로필을 실제로 열지 않고 가상 기본 경로 selector 검사와 명시적 격리 override의 새 시작/같은 경로 native 재실행을 구분한다.
- 지면: `ParallaxWorld.GroundForStage(stage)`, `GroundPoint(x,stage)`, `GroundPlanY=590`. 기준 카메라의 720p 화면 약469.226px는 투영값이며 원본 논리 좌표와 혼동하지 않는다. 숲 tint는 `(202,211,174)`, 기존 Run의 원본2도트 공중 구간은 의도된 동작이다. Client가 본 기존 발끝468/바닥469 PNG는 Client의 참고 근거이며 신규 후보의 QA 실측이 아니다.
- 아트: 모든 forest art ID는 아래 이름에 `dragon_` 접두사를 붙인다: `goblin_scout`, `goblin_slinger`, `forest_mushroom`, `goblin_shield`, `mushroom_captain`, `goblin_spear`, `spore_mushroom`, `moss_mushroom`, `goblin_guard`, `gib`. 프레임 수·pivot·타이밍은 최종 manifest와 대조한다.
- Player-v3 A안은 총괄의52원본/6시트 직접 검토 후 채택됐다.8색·alpha·pivot·타이밍·복장/손검·기존 동작을 보존한다. 이는 제작 인계 사실이며 QA의 신규 게임 관측 완료는 아니다.
- 10/20 대사는 각각3페이지를 유지한다.20의 화자는 기블→도토→루카, stable boss ID는 `chain_jailer`다.19층 이름은 `기블의 문지기`로 확정됐다.
- 마지막 Unity 검사 때 Client의 `SaveDirectorySelectorTests` **5 cases만** 표적으로 실행권을 인계받을 예정이다. 기존 모션11 cases를 반복하지 않는다. root의 GameData export21 tests와 기획 독립 검토는 root 수행 근거로 구분한다.

- Unity 버전은 [ProjectVersion.txt](../../Client/ProjectSettings/ProjectVersion.txt)의 `6000.3.15f1`이다. 진입점은 저장된 [Mvp 씬](../../Client/Assets/Game/Scenes/Mvp.unity)의 [GameBootstrap](../../Client/Assets/Game/Runtime/Bootstrap/GameBootstrap.cs)이다.
- 현재 [카탈로그](../../GameData/mvp-catalog.json)는 `dataVersion=mvp1.14.0`, `defaultCharacterId=on`, `mvpLastStage=20`, `boundaryStage=21`이다. 현재 표시명은 온/담/린이고 11~20은 옛 적이다. 최종 데이터 버전은 총괄 인계 때 고정하며 0.9.2와 같은 값이라고 추정하지 않는다.
- [Dragon-v1 이야기 원본](../Story/Dragon-v1/story.json)의 숲 편성을 따른다. 기존 기획의 `깁`, `깁의 문지기`, 화자 `온`은 이번 요청의 **기블**, **루카**와 다르다. Stage 20의 기준 이름은 기블이며 관련 표기도 최종 데이터·기획·화면에서 일관되게 반영해야 한다. Stage 19의 최종 표시명은 인계표에서 확정한다.
- 현재 `story_boss_20`에는 매달린 집행관/선택한 종지기의 옛 대사가 남아 있다. 신규 대사는 숲을 빠져나와 도토가 광산 통로를 알리는 기획 방향을 기준으로 총괄이 확정한다. 기존 기획의 두 대사를 현재 런타임 페이지 수에 맞추는 방법을 QA가 임의로 정하지 않는다.
- [GameBootstrap](../../Client/Assets/Game/Runtime/Bootstrap/GameBootstrap.cs)과 [SaveDirectorySelector](../../Client/Assets/Game/Runtime/Bootstrap/SaveDirectorySelector.cs)의 확정 경로 분기를 읽었다. [JsonSaveRepository](../../Client/Assets/Game/Runtime/Bootstrap/JsonSaveRepository.cs)는 `last-bell-save.json`과 `.bak`를 사용한다. 실행 검증과 소스 확인을 구분한다.
- 신규 아트의 최종 art ID·GUID·원본/프레임 해시·크기·pivot·프레임 시간·공격 접촉·포효 방식은 제작 중이다. 미확정 값을 예상 경로나 임의 수치로 채우지 않는다.
- 현재 [BattlePresentation](../../Client/Assets/Game/Runtime/Presentation/BattlePresentation.cs)은 다수 위치에 논리 Y=590을 사용하고, [ParallaxWorld](../../Client/Assets/Game/Runtime/Presentation/ParallaxWorld.cs)는 1280×720 기획 좌표를 world로 변환한다. [CombatGeometry](../../Client/Assets/Game/Runtime/Core/CombatGeometry.cs)의 판정 좌표는 960×540이다. 현재 상수와 서로 다른 좌표계를 새 스테이지 지면의 확정값으로 간주하지 않는다. Client가 정의할 ground base와 변환을 기준으로 대조한다.
- 기존 [Player-v2 manifest](../Story/Dragon-v1/Production/Player-v2/manifest.json)는 원본 프레임별 재중앙화/발 정규화를 하지 않는 계약과 clip pivot override를 가진다. 새 접지 처리가 달리기/회피의 의도한 공중 자세를 지면에 강제로 붙이지 않는지 확인한다. Player-v3의 제작 또는 채택은 아직 확정됐다고 기록하지 않는다.
- 읽은 대상 경로와 상위 경로에서 적용할 `AGENTS.md`는 발견되지 않았다. 공유 작업 폴더의 기존 수정 사항은 보존한다.

다음 내용을 함께 인계받아 검사 대상을 고정한다. 미확정 항목이 남으면 해당 항목을 `PENDING_HANDOFF`로 유지하며 실행·합격을 선언하지 않는다.

| 인계 항목 | 필요한 내용 |
| --- | --- |
| 후보 | 관측 exe 절대 경로, 빌드 시각, 앱/데이터 버전, Runtime DLL·카탈로그·아트 manifest SHA256, 일반 빌드와 관측 빌드의 차이 |
| 데이터 | 11~20 stage/enemy/art/표시명/kind 대응표, 원본→생성 JSON/Unity 참조 경로, 캐릭터 이름과 1명 해금, 변경 허용 필드 |
| 아트 | 신규 10개 연결의 실제 자산, Idle/공격별 clip/Stun 및 15·20 Roar 연결, 재사용/alias 명세, 불투명 몸높이·기준 스케일·pivot·접촉 프레임 |
| 이야기·문구 | 20층 뒤 최종 화자/본문/페이지, Stage 10 등 기존 화자 이름 변경, 19층 관련 명칭, 스킬 기획 및 미구현 안내의 노출 위치 |
| 저장 | 신규 기본 저장 영역과 이전 영역의 정확한 구분, 첫 시작/재실행 계약, CLI override 우선순위, 오류 시 원본 보존, QA가 안전하게 기본 경로를 검증할 방법 |
| 지면 | 1~20 각 stage → 배경/ground profile → base 좌표, 좌표계·카메라 투영, 플레이어/일반 적/보스 기준, 접지 프레임/발 기준점·허용 공중/전진 offset·복귀 기준·오차 허용값 |
| 플레이어/스타일 | reviewer 비교 결과와 수정 필요 여부, 기존 Player-v2 유지 또는 Player-v3 채택, 최종 원형/복장/장비·모션 manifest, 변경 시 승인 타이밍 보존 여부 |
| 재미 기획 | 차별성·핵심 선택·보상/실패/재도전의 설명, 현재 구현과 향후 스킬/콘텐츠 제안의 구분 |
| 실행 도구 | 변경 범위를 검사할 probe/fixture/필터와 결과 형식, 필요한 소스 수정에 대한 별도 인계, 독점 Editor 사용 여부·반환 조건 |

**v1/v2 실행기의 `-V2Only`·`-V2HpOnly`는 이번 범위를 검증하지 않는다.** 후속 허가로 `v3`/`v3-resume`과 [V3 전용 실행기](../../Tools/QA/Dragon-v3/run-campaign.ps1)를 준비했다. 두 해상도 각각 관측+동일 저장 경로 재실행, 총4프로세스가 한 캠페인이다. 최종 아트/후보/실행권 인계 전 `-Run`을 사용하지 않는다.

## Stage 11~20 전수 바인딩 표

아래 stable enemy ID는 계획 작성 시 현재 카탈로그 값이다. 기존 v1의 ID 보존 방식을 기본 비교 기준으로 삼되, 최종 인계가 ID 변경을 요구하면 저장 영향과 대응 계약을 먼저 확인한다. 기획의 enemy ID를 런타임 ID로 그대로 대체해야 한다고 단정하지 않는다. 최종 art ID는 인계 후 별도 증거표에 기록한다.

| Stage | 현재 stable enemy ID / 옛 표시명 | 목표 외형·표시명 | kind | 관측 항목 |
| --- | --- | --- | --- | --- |
| 11 | `sewer_rat` / 하수구 거대쥐 | 숲길 고블린 | normal | Idle·선택 Attack·Stun, 짧은 칼과 손 연결 |
| 12 | `drowned_resident` / 침수된 잔상 | 돌팔매 고블린 | normal | Idle·선택 Attack·Stun, 돌팔매 실루엣과 공격 방향 |
| 13 | `water_wraith` / 물안개 망령 | 숲송이 | normal | Idle·선택 Attack·Stun, 갓/몸통·발밑 기준 |
| 14 | `ash_wisp` / 재의 불씨 | 방패 고블린 | normal | Idle·선택 Attack·Stun, 방패와 손 연결 |
| 15 | `chain_thrall` / 사슬 망령 | 숲송이 대장 | miniboss | Idle·선택 Attack·Stun, 큰 갓·진입 확대·Roar·복귀 |
| 16 | `hook_hound` / 배수로 갈고리개 | 창 고블린 | normal | Idle·선택 Attack·Stun, 긴 창의 손잡이·끝·잘림 |
| 17 | `chain_puppet` / 문패 꼭두각시 | 포자 숲송이 | normal | Idle·선택 Attack·Stun, 포자/갓 변형 식별 |
| 18 | `cracked_blade` / 금 간 검기사 | 이끼 숲송이 | normal | Idle·선택 Attack·Stun, 이끼·몸폭 식별 |
| 19 | `chain_dancer` / 사슬 무희 | 기블의 문지기 | normal | Idle·선택 Attack·Stun, 투구·방패·칼 식별 |
| 20 | `chain_jailer` / 매달린 집행관 | 기블 | boss | Idle·선택 Attack·Stun, 방패·깃털 투구·Roar·복귀·처치 후 이야기 |

각 행은 데이터 연결과 실제 렌더를 모두 확인한다. 파일명이나 art ID가 새 이름이라는 이유만으로 통과시키지 않는다. 신규 art 안의 의도한 clip 재사용은 인계 명세와 대조하고, 누락 때문에 옛 아트나 기본 clip으로 조용히 대체되는 경우와 구분한다.

## 한 차례 실행 묶음의 순서

한 차례는 **동결된 후보에 대한 한 QA 캠페인**을 뜻한다. 저장 재실행 확인에는 종료/재기동이 필요하므로 단일 프로세스라는 뜻은 아니다. 해상도와 저장 재실행에 필요한 프로세스 수를 실행 직전 명세에 기록하고 임의 반복 실행하지 않는다. 결함이 나오면 실패 항목과 바로 인접한 경계만 후속 인계에 따라 재검사한다.

| 순서 | 표적 | 방법과 증거 |
| --- | --- | --- |
| 1 | 인계 대조·저장 보호 | 후보 해시/버전·소스/생성 데이터 일치·신규 격리 경로를 확인한다. 가상 구 저장 sentinel의 전후 manifest와 scoped 설정 백업을 남긴다. 사용자 기본 프로필은 열지 않는다 |
| 2 | 관련 정적/자동 검사 | 10개 바인딩과 누락 clip, stable ID/해금, 새 저장 경로 선택, 첫 시작/복원, 15·20 진입과 20 뒤 이야기/보상 경계만 대상으로 한다 |
| 3 | 1280×720 적 전수 | 11~20 각 Idle 2주기, 실제 선택 공격의 준비→접촉→회복→Idle, Stun 진입→유지→복귀를 연속 기록한다. 15·20 진입/포효도 기록한다 |
| 4 | 640×360 적 전수·문구 | 같은 10개 바인딩과 세 모션 상태, 15·20 포효를 확인한다. 원본 해상도에서 픽셀 경계·장비·긴 이름·HP/대사 영역을 직접 검토한다 |
| 4a | 지면·플레이어 모션·스타일 | 아래 지면 표본 규칙에 따라 Idle/Attack/Run/Up/Back/Down/Hit를 관측한다. 신규 일반 적·두 보스·대화 NPC와 플레이어를 실제 사용 배율에서 비교한다. Player-v3가 채택되면 변경된 clip과 로비 연결도 같은 묶음에서 확인한다 |
| 5 | 진행 경계 | 격리 fixture로 10→11 연결, 14→15→16, 19→20 처치→새 이야기→보상→20층 완주를 확인한다. 전체 1~20 수동 주행으로 확대하지 않는다 |
| 6 | 이름·잠금·스킬 상태 | 로비/선택/강화 등 존재하는 화면, Stage 10·20 화자, 미구현 안내를 확인한다. 루카만 선택 가능하고 다렌/미엘 선택 거부가 진행·재화에 영향을 주지 않는지 본다 |
| 7 | 신규 저장·재실행 | 새 영역 첫 시작의 1층과 지정 중간 진행의 저장/종료/재기동 복원을 확인한다. 이야기 경계의 저장 복원은 표적 fixture/테스트로 함께 확인한다 |
| 8 | 복구·종료 | 각 native exit code·오류·timeout, 격리 legacy fixture 해시, scoped 설정 복원, Editor 비실행 상태/씬 반환을 기록한다. 증거 누락과 보류 항목을 구분해 최종 보고한다 |

### 모션·보스 판정 방법

- stage/enemy/art ID, Actor의 실제 motion/clip/source frame, 렌더 프레임 번호·시간, 게임 시각·상태, 피해/회피/Stun 이벤트를 함께 연결한다. 정지 포즈 강제 지정은 연결 확인용이며 실제 전투 전이 통과 근거로 대신하지 않는다.
- `BattlePresentation.EnemyMotion`은 패턴에 따라 `AttackLow/High/Thrust/Rear` 등을 선택한다. 각 층에서 실제 도달하는 공격 연결을 확인하고 ‘Attack 한 개가 존재함’으로 나머지 누락을 통과시키지 않는다. 원형 재사용 범위는 최종 아트 명세를 따른다. 패턴·피해 수치를 새 그림에 맞추려고 QA가 변경하지 않는다.
- 공격은 준비 자세·무기 궤적·접촉·후속 자세와 실제 판정을 비교한다. 무기 분리, 몸/장비 변경, 추가 팔다리, 바닥 미끄러짐, 프레임별 크기 튐, 옛 적의 피격/공격 잔상을 확인한다. Stun은 게임의 Stun 상태와 취소된 공격·복귀를 함께 보며 단순 피격 flash와 구분한다.
- Stage 15·20은 인계된 **불투명 몸체 크기**와 화면 실측을 비교한다. 투명 캔버스 여백을 몸 크기로 세지 않는다. 특히 기블은 기존 기획상 작은 몸과 큰 방패/구도로 체급을 표현하므로 임의로 거대한 고블린을 합격 기준으로 삼지 않는다.
- 보스 진입의 확대→포효→카메라/배우 복귀를 본다. 포효 프레임 또는 명시적 재사용과 VFX 입 위치가 새 몸체에 맞고, 오디오 호출이 반복 프레임마다 중복되지 않아야 한다. 실제 청취 여부와 이벤트 확인을 별도로 기록한다. 첫 진입/반복 진입의 seen 상태와 전투 차단 경계는 관련 자동 검사로 확인한다.
- 정상속도 직접 재생을 수행했을 때만 체감 모션을 검토했다고 적는다. 접근 제한으로 PNG/타임라인만 검토했다면 그 범위와 미관측 source pose를 적는다. 프레임 파일 수나 해시만으로 자연스러움·60fps·모든 포즈 관측을 선언하지 않는다. 브라우저 제한을 다른 경로로 우회하지 않는다.

### 신규 적 공격 이펙트 · 추가 인계 반영

총괄의 [EnemyAttackEffects.Style](../../Client/Assets/Game/Runtime/Presentation/EnemyAttackEffects.cs) 분기와 [EnemyEffectBurst.Position](../../Client/Assets/Game/Runtime/Presentation/EnemyEffectBurst.cs)의 `GroundPlanY` 참조를 읽었다. 이는 소스 확인이며 실제 렌더 합격은 아직 아니다. **기존 `enemy-<stage>-attack-hit` 구간의 프레임/이벤트 증거에서 함께 검토하며 별도 캠페인을 추가하지 않는다.** QA probe·실행기 소스는 동결 상태를 유지한다.

| 효과 | 대상 stage / art ID | 시각 검토 |
| --- | --- | --- |
| Metal |11 `dragon_goblin_scout`,12 `dragon_goblin_slinger`,14 `dragon_goblin_shield`,16 `dragon_goblin_spear`,19 `dragon_goblin_guard`,20 `dragon_gib` |기존 무기 궤적이 새 무기·동작 방향과 어긋나거나 분리되지 않는지, 접촉 때 실루엣/타격 피드백이 읽히는지 |
| Wood |13 `dragon_forest_mushroom`,15 `dragon_mushroom_captain`,17 `dragon_spore_mushroom`,18 `dragon_moss_mushroom` |갈색 입자·지면 충격의 색/위치가 새 버섯 공격과 맞는지, 금속 궤적이나 이전 종의 효과가 섞이지 않는지 |

1280/640 원본에서 준비→release→impact→회복 순서와 실제 피해 이벤트를 대조한다. 효과 원점은 `CombatGeometry.HomeY` 기준 판정 좌표에서 `ParallaxWorld.GroundPlanY`로 변환되는 현재 계약을 따른다. 스테이지 지면/배우의 발과 효과 투영을 같은 좌표계로 비교하며 590의 상수 치환을 새 지면 높이 변경으로 취급하지 않는다.15·20의 큰 몸/방패/갓과 카메라 복귀 후에도 궤적과 지면 충격이 어긋나지 않는지 본다.

Stage1~10의 기존 효과 분기와 패턴·시간·판정 불변은 최종 소스/데이터 차이로 확인하고, 이번 효과 연결 때문에 이전 전체 캠페인을 반복하지 않는다. 효과 코드에 오디오 분기가 있더라도 실제 청취를 하지 않으면 소리 검증 완료로 쓰지 않는다. guard/gib의 최종 공격 자산과 관측 빌드는 아직 인계 대기다.

### 지면 기준·허용 이동·모션 자연스러움

**정의 확인은 1~20층 전부**, 새 적의 기본 발 위치는 **11~20층 전부**를 대상으로 한다. 같은 ground profile·공통 코드 경로를 쓰는 층에서 플레이어 7모션을 매번 반복하지 않는다. 각 고유 ground profile/코드 경로마다 대표 일반 전투에서 전 모션을 관측하고, 10→11 전환과 Stage 15·20의 상대 체격/카메라 조건을 추가한다. 층별 offset 예외가 있으면 해당 층도 전 모션 표본에 포함한다. 두 해상도에서 같은 좌표 변환과 복귀를 확인한다.

| 상태 | 기준 접지와 허용 이동 | 확인할 경계 |
| --- | --- | --- |
| Idle | 바닥의 지지 발/밑면이 승인된 base에 놓인다. 호흡·몸통 변화는 허용하며 무기 끝이나 투명 캔버스 아래를 발로 취급하지 않는다 | 루프 이음, 각 모션 복귀, stage 변경 후 지속 부유/파묻힘 없음 |
| Attack | ground base를 유지하는 root와 기존 접근→접촉→원위치의 X 이동을 분리한다. 승인된 자세의 발 들기/전경·원본 내부 offset은 허용한다 | 준비→전진→접촉→회수→Idle의 발/무기 연결, 접근량의 이중 적용 없음 |
| Run | root의 지면 기준과 화면 이동/배경 스크롤을 분리한다. 접지 프레임과 비행 프레임이 번갈아 있는 동작을 유지한다 | 발 밀기→체공→다음 접지, Idle→Run→Idle, 이동 중 누적 Y drift·강제 발 붙이기 없음 |
| Up | 지면 기준에 런타임 점프 offset과 승인된 포즈가 더해진다. 체공은 정상이다 | 이륙→정점→하강→착지, 원본 이동과 root 이동의 중복·지면 아래 착지 없음 |
| Back | 기존 수평 후퇴/복귀를 허용한다. 추가 공중 구간은 인계된 원형에 있는 경우만 허용한다 | 적을 보는 방향 유지, 후퇴→지지→복귀, 뜻하지 않은 2차 점프·수직 튐 없음 |
| Down | 현재 아래 입력은 home에서의 쳐내기다. 베기 포즈에 따른 발/몸 변화는 허용하되 새로운 전진·잠수 이동을 만들지 않는다 | 준비→쳐내기 접촉→후속→Idle, root home/base 복귀와 무기 연결 |
| Hit | 승인된 피격 기울기/밀림/발 들기는 허용한다. 실제 Hit clip·flash 처리와 root 이동을 구분한다 | 실제 피해 발생→피격→회복, 영구 떠 있음·파묻힘·크기 변경 없음 |
| 적 Stun·15/20 Roar | 해당 종의 발/밑면 기준과 승인된 웅크림·갓 움직임·포효 자세를 따른다 | Stun 해제, 포효 줌 복귀, 보스의 방패·갓을 발로 오인한 보정 없음 |

측정에는 stage/ground profile/base, 배우 root(world), SpriteOffset/clip pivot/reference height, 승인된 발/지지점, 허용 모션 offset, 카메라, 투영된 화면 위치를 함께 남긴다. **예상 지지점은 base + 인계된 동작 offset을 같은 좌표계로 변환한 값**이며, PNG 행 번호·논리 Y·world Y를 직접 비교하지 않는다. clip 내부 변위와 root 변위의 합이 의도한 움직임을 한 번만 적용하는지 대조한다. 오차 허용값과 예외는 결과를 보기 전에 Client/art 명세로 고정한다.

관찰용 지면 선/기준점 표시는 증거에만 분리하고 최종 게임 화면에 QA 장치를 추가하지 않는다. ‘가장 아래 불투명 픽셀을 매 프레임 바닥에 맞춤’은 검·방패·포자·비행 자세 때문에 잘못된 기준이 될 수 있으므로 합격 계산으로 사용하지 않는다.

각 모션의 자연스러움은 포즈 나열 외에 정상속도에서 무게 이동·접지/체공의 리듬·상체와 장비의 일관성·전이의 튐을 평가한다. 프레임 정지 검토는 손잡이/손 분리·발 위치·프레임 경계를 보완한다. Run 400ms·기존 이동 속도, 공격 접촉 200ms, 회피/쳐내기 판정은 별도 변경 인계가 없는 한 보존한다. Player-v3로 원본이 바뀌면 실제 clip과 이 타이밍의 접점을 다시 검증하며 이전 Player-v2 합격으로 대신하지 않는다.

### 플레이어와 NPC 스타일·복장 정합, 재미 기획

- reviewer가 선정한 플레이어와 신규 고블린/숲송이/기블, 실제 대화 NPC를 같은 화면 배율에서 비교한다. 픽셀 블록 크기·윤곽 두께·명암 단계·채도/밝기·머리/몸 비례·장비 덩어리·배경 대비를 본다. 서로 다른 캐릭터의 키나 팔레트 색 수를 무조건 같게 만들지는 않는다.
- 현재 플레이어 정체성은 갈색 머리·붉은 스카프·초록 튜닉·가방·한손검이다. reviewer/총괄이 확정한 변경이 있으면 새 기준을 기록한다. Idle/Attack/Run/Up/Back/Down/Hit에서 복장 색면·검/가방·머리·비율이 유지되고 과거 후드 캐릭터나 다른 의상의 프레임이 섞이지 않아야 한다. 로비와 전투의 동일 캐릭터 연결도 확인한다.
- Player-v2 유지면 ‘리뷰 후 유지’ 근거를, Player-v3 채택이면 신규 원본·연결·적용/검증 결과를 기록한다. 아트 reviewer의 비교 완료와 QA의 실제 게임 관측 완료를 각각 표시한다. 아직 받지 않은 리뷰를 완료로 쓰지 않는다.
- 차별성/재미 문서는 총괄 소유다. QA는 사용자가 무엇을 읽고 어떤 타이밍/전략 선택을 하는지, 그 선택의 피드백·보상·실패 후 재도전 동기가 설명되는지와 기획/현행 구현 경계가 명확한지 검토한다. 이 문서 검토를 플레이테스트나 ‘재미 검증 완료’로 표현하지 않는다. 전용 스킬은 계속 기획 단계로 남긴다.

### 20층 뒤 줄거리와 보상

- 새 이야기의 화자·본문·페이지 순서·루카 표기를 확정 원본과 비교한다. 매달린 집행관/선택한 종지기·사슬로 시간을 거둔다는 옛 본문이 화면이나 실제 선택 캐릭터 응답 분기에 남지 않아야 한다.
- 처치→퇴장/대화→기존 보상 처리→완주 흐름을 최종 통합 계약과 대조한다. 다음/건너뛰기는 대화 진행만 처리하며 자동 유물 선택이나 보상 중복 지급을 만들지 않는다.
- 대화 중간 저장을 격리 fixture에서 복원해 페이지·읽음 상태·보상 ID가 보존되는지 확인한다. 대화 종료와 완주 결과 재진입에서도 재화·유물·완주 기록을 두 번 지급하지 않는다.
- 도토가 광산 길을 알리는 내용은 다음 지역의 이야기 예고다. MVP 종료 후 Stage 21 전투·존재하지 않는 캐릭터/적을 열지 않으며 새 열쇠·부품 수집 조건을 요구하지 않는다.

### 새 저장 영역과 기존 저장 보존

1. **사용자 기본 프로필에는 접근하지 않는다.** QA 폴더 안에 가상 persistent 루트와 구 저장/백업 sentinel을 만들고 그 파일만 비교한다. 실제 사용자 파일을 복사하거나 fixture로 열지 않는다.
2. selector가 가상 기본 루트의 `Playtests/dragon-0.9.2`를 선택하고 디렉터리 생성·삭제·이전·초기화를 하지 않는지 소스/자체 검사와 후속 EditMode5 cases로 확인한다. CLI override가 더 높은 우선순위를 가지는지 확인한다.
3. **기본 경로 선택 계약과 CLI override의 native 실행을 별개로 검증한다.** 관측 빌드에 playtest define이 있는지 확인하고 두 경로 분기를 기록한다. 사용자 기본 경로의 직접 exe 실행/실제 파일 해시 감사는 `NOT_RUN`/`NOT_ACCESSED`로 남긴다.
4. 실제 관측은 신규 증거 폴더 하위에 명시적 `-lastbell-save-dir`를 전달한다. 경로 정규화·strict child·reparse point 검사를 적용하고 기존 결과/저장을 덮어쓰지 않는다. 일반 배포 exe는 실행하지 않는다.
5. 신규 영역 최초 부팅에서 기본 `on`/루카만 선택 가능하고, 프롤로그·기존 튜토리얼 뒤 첫 전투가 Stage 1인지 확인한다. 기존 영역의 최고층·체크포인트·골드를 이어받지 않아야 한다. 기본 재화/튜토리얼 값은 확정 카탈로그를 따른다.
6. 같은 신규 영역에서 Stage 1을 벗어난 식별 가능한 진행과 재화/성장 상태를 저장한다. 정상 종료 뒤 같은 경로로 재실행해 계약상 복원 가능한 phase·층·run ID·캐릭터 ID·재화·성장·읽음 기록을 비교한다. 기존 복원 규칙에 따른 일시정지/예고 재시작은 기록하되 무조건 바이트 일치로 요구하지 않는다.
7. 첫 실행과 재실행 후 격리 legacy sentinel manifest를 비교한다. 차이가 있으면 실패로 남기고 중단하며 증거를 덮어써 복구하지 않는다. 사용자 파일 보존에 대한 근거는 비접근·명시적 경로 격리·selector 계약이며 실제 사용자 파일 전후 비교를 수행했다고 쓰지 않는다. 설정 복원은 실행기가 소유한 scoped PlayerPrefs에만 적용한다.

## 자동 검사 선정 원칙

현재 존재하는 관련 근거는 [GameSessionTests](../../Client/Assets/Game/Tests/EditMode/Core/GameSessionTests.cs), [JsonSaveRepositoryTests](../../Client/Assets/Game/Tests/EditMode/Persistence/JsonSaveRepositoryTests.cs), [CombatPresentationTests](../../Client/Assets/Game/Tests/PlayMode/CombatPresentationTests.cs)다. 다음은 **선정 후보**이며 신규 v3 필터를 작성하거나 실행한 목록이 아니다.

| 위험 | 기존 재사용 후보 / 필요한 보완 |
| --- | --- |
| 새 저장 첫 시작·복원 | `FreshProfileHasNoSave`, `VersionTwoTransitionAndStorySurviveJsonUtility`; 저장소 테스트만으로 bootstrap의 기본 namespace 분리는 증명되지 않으므로 최종 Client 경로 선택 검사와 exe 관측을 연결 |
| 저장 오류로 기존 파일 유실 | `InvalidSaveIsPreservedWhenBothCopiesFail`, `UnsupportedVersionCannotOverwriteExistingSave`; 격리 fixture만 사용하며 새 저장 정책 변경에 관련될 때만 포함 |
| 이름 변경·해금 | 실제 카탈로그의 on/dam/rin ID·표시명·availableInMvp를 대조하고 잠긴 캐릭터 선택 거부 확인. 이름을 저장 ID로 바꾸지 않았는지 검사 |
| 보스 진입 | `BossIntroBlocksAllCombatAndMarksSeenOnlyAtCompletion`, `BossZoomHoldsThenRestoresBeforeAnyCueOrDamage`; 기존 5·10 중심 표본을 통과한 것만으로 신규 15·20을 통과시키지 않음 |
| 20층 이야기·완주 | `BossRewardIsFixedBeforeCollapseThenStoryResumesBeforeRelicChoice`, `StoryIsOncePerCharacterAndSkipNeverChoosesRelic`, `BossTenRemainsForDialogueThenRewardContinuesAndTwentyClears`; 옛 대사 문자열/층 가정과 최종 인계의 차이를 확인 |
| 신규 적 모션·Stun | `StunCancelsHitsAndBlocksGainForTwoSecondsAfterward`는 판정 근거 후보다. 신규 art 연결과 시각 Stun은 실제 Actor 관측이 추가로 필요 |
| 지면·플레이어 모션 | 최종 Client ground base 계약의 stage 매핑/좌표 변환·허용 offset·동작 종료 복귀를 검사. `ApprovedRunKeepsScaleAndRootAcrossAllFramesBothFacingsAndIdleTransitions` 등 기존 표적은 재사용 후보이며 새로운 지면/Player-v3를 실제 참조하는지 먼저 확인 |

준비 권한 안에서 V3 probe와 Unity 없이 실행하는 자체 fixture/경로 검사를 작성했다. Unity EditMode는 실행 인계 후 Client의 `SaveDirectorySelectorTests`5 cases만 수행한다. 전체 Core suite, 전체 v1/v2 캠페인, 기존 모션11 cases와 HP30상태를 반복하지 않는다. 플레이어 모션은 새 지면/Player-v3의 실제 프레임 관측으로 확인한다. 자체 검사 결과를 Unity 테스트 결과로 부르지 않는다.

## 증거와 최종 판정

후속 산출물의 제안 루트는 `Planning/Evidence/Dragon-v3/<새 candidate-id>/`다. 아직 생성하거나 실행하지 않았다. 기존 Dragon-v1/v2 증거·세이브·빌드·ZIP을 덮어쓰지 않는다.

| 증거 | 필수 내용 |
| --- | --- |
| 후보 manifest | exe·Runtime DLL·데이터·아트 해시, 빌드 시각/버전, 허용 변경, 관측/일반 빌드 구분 |
| 바인딩/모션 표 | 10개 층, 최종 enemy/art/clip ID, 선택 공격과 alias, 실제 참조/프레임 증거, 누락 상태 |
| 화면/연속 프레임 | 1280·640 원본, 이름·잠금·스킬 안내, 15·20 확대/포효, 20 뒤 대화/결과; 캡처 경로·순서·해시·시각 |
| 지면/스타일 | 1~20 ground base 정의와 실제 관측 표본, root/발/허용 offset/카메라 투영 기록, 플레이어 7모션 및 보스 발 기준, reviewer 비교·Player-v3 채택 여부 |
| 자동 검사 | 실제 실행 필터·개별 결과·실패/skip·로그. 준비와 실행 결과를 분리 |
| 저장 보호 | 가상 구/새 경로 계약, legacy fixture 전후 manifest, 최초 시작·중간 저장·동일 영역 재실행의 값 비교, CLI/default 증거 구분, 사용자 기본 프로필 NOT_ACCESSED |
| 실행/반환 | 프로세스별 native exit·timeout·오류, 설정 복구, Editor 사용 시 기존 씬·비Play·dirty 상태 및 권한 반환 |
| 결과 보고서 | 완료 기준별 PASS/FAIL/NOT_RUN, 결함과 재검사, 정상속도/청취/실입력/모바일/일반 배포 검증 범위와 담당 |

기존 사용자 저장 변경, 첫 실행이 기존 진행을 이어받음, 재실행 때 초기화, 잘못된 적/누락 모션, 잠금 해제, 이야기/보상 진행 막힘·중복은 해결 전 완료로 판정하지 않는다. 미실시 검사는 통과와 구분하고 최종 인계 범위가 충족된 경우에만 QA 종료한다.

이 계획은 **작성 완료, 실행 대기**다. 기존 [Dragon-v2 최종 결과](dragon-v2-result-20261024.md)는 종료 상태를 유지하며, 새 작업의 실행권 인계 전 추가 실행은 하지 않는다.
