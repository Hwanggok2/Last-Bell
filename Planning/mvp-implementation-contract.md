# 1–10 Stage Unity MVP 구현 계약

2026-10-21. 편집 원본은 planning-v4 결정에서 시작한다. 실제 구현 범위는 첫10층·로비 강화·유물3택1·저장 복구다. 10층 보상 뒤11층 자동 진입을 검증할 수 있도록11층을 경계 표본으로 둔다. 50종 전체 제작 완료를 이번 MVP로 표시하지 않는다.

## 파일 소유자

- 총괄: Client 프로젝트/Packages/ProjectSettings, Runtime/Bootstrap, Runtime/Art, Editor/아트 임포트, 실제 Unity 실행·빌드·통합 검사, 아트 이미지 생성.
- 전투 담당: Runtime/Core 및 Tests/EditMode/Core. 순수 C# 게임 세션·상태 DTO·전투·튜토리얼·진행·보상/강화/교환. Unity UI 의존 없음.
- UI 담당: Runtime/Presentation, Editor/MvpSceneBuilder.cs. 저장된 Scene/Canvas/Prefab/SpriteRenderer 참조와 View/Presenter, 편집 가능한 조립. 런타임 계층 통째 생성 금지.
- 경제/데이터 담당: GameData 원본 JSON·검증 도구, Runtime/Data/GameCatalog.cs, Data/Generated. 수치·패턴·캐릭터·유물 데이터. 실행 상태 소유 금지.
- 기획 담당: Planning/Production의5구역·50종 roster·재사용/모션 명세와 담당 검토. 코드 직접 수정 없음.

## 공유 방향

namespace LastBell. Built-in 2D + uGUI, Unity6000.3.15f1. 외부 결제/광고/네트워크 없음. 한국어 기본. 가로960×540,지면82%,도트 Point filtering. PNG/프리팹/씬은 실제 자산으로 남긴다.

데이터 담당은 Unity JsonUtility 호환 serializable public fields 클래스들을 먼저 작성한다. 원본 GameData/mvp-catalog.json → 검증 성공 시 Client/Assets/Game/Data/Generated/mvp-catalog.json. 구체 DTO와 API는 담당 초기 작성 후 이 계약에 추가한다. 다른 담당은 DTO를 재정의하지 않는다.

전투 담당은 GameSession이 유일한 규칙 소유자이며 Tick(deltaSeconds), Dodge(direction), Attack(), StartRun(), ReturnToLobby(), ChooseRelic(id), Upgrade(statIndex), ConfirmExchange()/CancelExchange(), EquipRelic(id), UnequipRelic(id)를 제공한다. 입력 시간/판정은세션, 연출은UI. 실제명칭은 초기선언후공유한다. 저장 DTO는 JSON 직렬화 가능한 필드만 쓴다.

UI는 Session의 상태를 읽고 명령을 호출한다. 씬 빌더는 CreateMissing/Validate를 기본으로 하며 기존 수동 편집 자산을 무단 재생성하지 않는다. 초기 제작 중 직접소유한 생성물 수정은 가능하되 사용자 편집보존 경계를 문서화한다.

## 보상 규칙 개정 — 기획 반영, Unity 적용 대기

2026-10-21: 유물은10·20·30·40·50층 보스에서만3개 중1개를 선택한다. MVP의5층은 기본 골드·종 파편1개만 저장한 뒤6층으로 진행하며, 유물 후보/선택권을 만들지 않는다. 10층은 기존 종 파편3개와 유물 선택을 유지한다. 신규Lv1·중복+1레벨·MAX 골드 전환·고정 후보 복구 규칙은 동일하다. 아래 기존 구현 및 실행 기록은 이 개정의 Unity 적용 증거가 아니다.

## 필수 검증

최초 실행→튜토리얼2행동→실시간전투→보상선택→다음층,사망→로비→스탯강화/부족분교환→재출전. 5층1파편·유물없이6진입/10층3파편·유물3개중1선택,동일결과중복방지,10선택후11진입,9사망후Stage1/10후Stage11,앱중단선택복구. 키보드WASD/방향키/Space 및 포인터. 모든 화면 실제 저장된 프리팹/씬에서참조. 에디터컴파일·상태검사·실제플레이화면·Windows실행파일가능여부확인.

## 데이터 API — 초기 구현 공유

`Client/Assets/Game/Runtime/Data/GameCatalog.cs`, namespace `LastBell`. Unity 의존 없이 `[Serializable]` public fields로 작성했다. Bootstrap은 `JsonUtility.FromJson<GameCatalog>(text)` 뒤 `ValidateOrThrow()`를 호출한다. 설정 객체를 실행 중 수정하지 않는다.

- 루트: `combat`, `growth`, `economy`, `relicRules`, `characters`, `enemies`, `stages`, `relics`, `defaultCharacterId`, `mvpLastStage=10`, `boundaryStage=11`.
- 조회: `GetCharacter(id)`, `GetEnemy(id)`, `GetStage(number)`, `TryGetStage(number,out StageDefinition)`, `GetRelic(id)`, `GetUpgradeCost(targetLevel)`, `GetBaseGold(stage)`, `GetFragmentReward(stage)`, `GetMaxRelicGold(stageNumber)`, `GetStunDurationMs(kind)`.
- 캐릭터: `CharacterDefinition.id/displayName/artId/stars/availableInMvp`. stars 순서는 공격/HP/행운/공명. 초기 플레이 가능 캐릭터는 on, dam/rin은 데이터만 지원.
- 스테이지: `StageDefinition.number/enemyId/kind/maxHp/damage/teachDodge/patternSteps`. kind=`normal|miniboss|boss`, teachDodge는 첫1~4층에 `up,back,down,forward`. 안정적 적ID와 변경 가능한artId를 분리한다.
- 패턴: `PatternStep.danger/requiredDodge/telegraphMs/intervalMs/comboStep`. danger=`low_sweep|thrust|high_sweep|outer_arc`, requiredDodge=`up|back|down|forward`. 배열 순서가 타격 순서다. intervalMs는 직전 타격부터 이번 타격까지(배열 반복 경계 포함), 첫 전투 타격만 telegraphMs 뒤 발생. comboStep=0 단발650ms,1 연타 시작900ms,2이상 후속250ms. 모든 타격 간격 최소450ms. 연타가 끝나면 다음 배열 원소의 intervalMs를 사용한다.
- 유물: `RelicDefinition.id/displayName/effect/effectLabel/cap/values`, `EffectAtLevel(level)`; values는 Lv1..5의 총효과. 미보유Lv0은0. 재화 비용은 `economy.upgradeCosts[targetLevel]`(0=0,1..50 구매비용). 시간 단위ms, 확률/퍼센트는0..1, HP 원본 단위HP, 실행 정밀도`combat.hpUnitsPerPoint=1000`.
- 원본 `GameData/mvp-catalog.json` → Python 검증 내보내기 → `Client/Assets/Game/Data/Generated/mvp-catalog.json`. JsonUtility용 배열만 직렬화하고 Dictionary는 검증 후 조회 캐시로 만든다. 실행 상태·저장 DTO는 전투 담당 소유다.
