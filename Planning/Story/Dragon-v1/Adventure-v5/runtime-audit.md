# Adventure v5 런타임 근거 점검

점검 기준: 2026-10-24 작업 트리, 편집 원본 `GameData/mvp-catalog.json`의 `mvp1.16.0`. **소스와 데이터를 읽어 확인한 내용**이며 Unity를 실행하거나 실입력·영상으로 관찰한 결과가 아니다. 이번 작업은 이 문서만 작성했다. Unity, GameData, 프리팹, 빌드와 테스트 산출물을 변경하지 않았다.

## 1. 현재 구현: Stage 10 뒤 HP가 가득 차는 이유

**보스 처치 순간에 HP를 최대치로 직접 대입하는 코드가 있다.** 다음 스테이지의 스폰이나 대사가 HP를 채우는 것이 아니다.

| 상황 | 현재 동작 | 소스 근거 |
|---|---|---|
| 새 출전·재도전 | 새로운 RunStateData를 만들고 계산된 최대 HP로 시작 | [GameSession.Progression.cs:15](../../../../Client/Assets/Game/Runtime/Core/GameSession.Progression.cs#L15), 같은 파일 25–27행 |
| 일반 스테이지 준비 | 적 HP·기절·패턴·공격 대기 상태를 초기화한다. 플레이어 HP는 대입하지 않는다 | [GameSession.Progression.cs:45](../../../../Client/Assets/Game/Runtime/Core/GameSession.Progression.cs#L45), 63–80행 |
| 일반 적·중간 보스 처치 | 처치 보상을 지급하고 이동/대화로 이어간다. HP 회복 분기에 들어가지 않는다 | [GameSession.Progression.cs:114](../../../../Client/Assets/Game/Runtime/Core/GameSession.Progression.cs#L114), 133–139행 |
| Stage 10·20 보스 처치 | `stage.kind == "boss"`이고 플레이어가 살아 있으면 `playerHpMilli = combatStats.maxHpMilli` | **[GameSession.Progression.cs:133](../../../../Client/Assets/Game/Runtime/Core/GameSession.Progression.cs#L133), 특히 136행** |
| 피격 | 방어 실패 시 피해를 계산하고 다렌의 활성 보호막을 먼저 차감한 뒤 남은 피해를 플레이어 HP에서 뺀다 | [GameSession.Combat.cs:320](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L320) 부근 `AbsorbSkillShield`→`playerHpMilli -= damage` |
| 사망 | 플레이어 HP를 0으로 만들고 사망 전환으로 진입 | [GameSession.Progression.cs:101](../../../../Client/Assets/Game/Runtime/Core/GameSession.Progression.cs#L101) |

따라서 같은 도전의 일반 흐름은 `Stage 1 남은 HP → 2 → … → 9 → 10 전투`로 이어지고, **10을 처치한 순간 완전 회복 → 11 진입 → 12…**가 된다. 5·15 중간 보스는 완전 회복 대상이 아니다. 20에서도 같은 보스 회복 코드를 실행하지만 현재 콘텐츠는 20 뒤 완주로 끝난다. 21~50을 실행 중인 것으로 해석하면 안 된다.

세션의 플레이어 HP 대입/증감 경로를 함께 확인했을 때 일반 전투 중 시간에 따른 자연 회복이나 일반 적 처치 회복은 없다. 다렌의 스킬은 HP 회복이 아니라 다음 피해를 흡수하는 별도 `skillShieldMilli`다([GameSession.Skills.cs:39](../../../../Client/Assets/Game/Runtime/Core/GameSession.Skills.cs#L39)). 저장 마이그레이션의 정수 HP 반올림/최댓값 제한([GameSession.State.cs:228](../../../../Client/Assets/Game/Runtime/Core/GameSession.State.cs#L228))은 기존 형식 변환이며 스테이지 클리어 보너스가 아니다.

### 새 설계에서 정해야 할 것 — 아직 적용하지 않음

- 보스가 구간의 휴식처라면 완전 회복은 유지하되, 보스 패배 뒤 회복 빛·HP 채움으로 원인을 보여 준다. 대사나 다음 스폰 중 갑자기 HP가 바뀌어 보이지 않게 한다.
- 구간을 넘어 생존 자원을 계속 관리하는 방향이라면 보스의 자동 완전 회복을 제거하거나 명시적인 회복량/휴식 보상으로 바꾼다. **현재 HP 규칙을 이미 이렇게 바꿨다고 기획서에 쓰면 안 된다.**
- 일반 스테이지의 남은 HP 유지, 새 출전의 최대 HP 시작, 사망 시 0, 동시 사망 시 보스 처치 회복으로 부활하지 않는 원칙은 별도 항목으로 남긴다.

## 2. 현재 구현: 최초 연속 공격과 실제 시간 순서

### 최초 스테이지

최초 연속 공격 가능 스테이지는 **Stage 5 큰뿔토끼**다. 안정적 적 ID는 `wild_boar`이지만 현재 표시명/아트는 큰뿔토끼다([mvp-catalog.json:396](../../../../GameData/mvp-catalog.json#L396)). ID의 옛 이름을 몬스터 종류로 설명하지 않는다.

| 스테이지 | 1타/2타/3타 가중치 | 현재 실제 의미 |
|---|---|---|
| 1~4 | 100/0/0 | 단타만 선택 |
| 5 | 30/70/0 | 보통 단타30%, 2연타70%. **최초 연속 공격 안내 확인 직후만 2연타 강제** |
| 6~9 | 100/0/0 | 다시 단타만 선택 |
| 10 | 0/100/0 | 항상 2연타 |
| 11 | 60/40/0 | 정면 대시만 사용하며 단타 또는 같은 대시2연타 |
| 15 | 0/50/50 | 현재 데이터에서 처음으로 3연타가 가능 |

데이터 근거: [mvp-catalog.json:423](../../../../GameData/mvp-catalog.json#L423)부터 1~4행, [527](../../../../GameData/mvp-catalog.json#L527)부터 Stage5, [664](../../../../GameData/mvp-catalog.json#L664)부터 Stage10, [697](../../../../GameData/mvp-catalog.json#L697)부터 Stage11. 횟수 선택은 [GameSession.Combat.cs:78](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L78) `ChooseSequence()`다.

### Stage 5 안내 직후의 실제 방향

1. Stage5 첫 도착의 중간 보스 등장 연출을 마치면 `CombatReady()`가 `ComboHint`로 진입한다([GameSession.Transitions.cs:62](../../../../Client/Assets/Game/Runtime/Core/GameSession.Transitions.cs#L62), 71–74행; 등장 연출 이후 호출은 119–122행).
2. 안내의 확인 버튼을 누르면 `comboHintSeen=true`, `forceTwoHitSequence=true`를 설정하고 전투를 시작한다([GameSession.Onboarding.cs:51](../../../../Client/Assets/Game/Runtime/Core/GameSession.Onboarding.cs#L51)).
3. 2타가 강제되지만, **두 방향을 `patternSteps[0]→[1]` 그대로 실행하는 것은 아니다.** 실제 순서는 `randomPatterns`에서 고르고 첫 공격만 `teachDodge`와 `patternSteps[0].danger`로 덮어쓴다([GameSession.Combat.cs:80](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L80), 82–90행).
4. 따라서 첫 타는 `low_sweep` → **위 회피** 고정이다. 두 번째는 `low_sweep` → **위 회피** 또는 `high_sweep` → **아래 쳐내기**다. 데이터 표에 있는 low→high 두 행만 보고 “첫 콤보는 반드시 위→아래”라고 설명하면 틀린다.

### 시간표

T=0은 `ConfirmComboHint()`로 전투가 재개되어 첫 예고를 예약한 시점이다. 적이 쓰러지거나 기절하여 큐가 취소되지 않고, 일시정지도 하지 않은 경우의 **전투 시계** 기준이다.

| 단계 | 첫 타 | 두 번째 타 |
|---|---:|---:|
| 방향 예고 구간 `CueAtMs~CueEndMs` | 0~900ms | 900~1800ms |
| 방출 `ReleaseAtMs` | 1680ms | 2430ms |
| 실제 판정 `ImpactAtMs` | **1800ms** | **2550ms** |
| 이전 타격과 간격 | — | **750ms** |

예약 근거는 [GameSession.Combat.cs:94](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L94) `ScheduleSequence()` 101–115행이다. 각 방향 예고를 900ms씩 모두 보여 준 뒤 첫 타를 판정하고, 나머지를 750ms 간격으로 판정한다. `fast=false`로 실행되므로 여기서 실제로 사용하는 값은 `singleTelegraphMs=900`, `comboDefaultGapMs=750`, `attackFlightMs=120`이다([mvp-catalog.json:22](../../../../GameData/mvp-catalog.json#L22), 33–36행). 첫 콤보가 끝난 뒤에는 마지막 실제 적중 시각으로부터 Stage5 첫 패턴의 `intervalMs=3600`만큼 쉬고 다음 예고를 예약한다([GameSession.Combat.cs:123](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L123)). 이 3600ms는 두 타 사이 간격이 아니다.

현재 회피 활성240ms + 회복120ms이므로 같은 회피를 다시 받는 최소 간격은360ms다. 콤보 적중 간격750ms와 충돌하지 않는다. 완벽 회피 판정 창은90ms다([mvp-catalog.json:11](../../../../GameData/mvp-catalog.json#L11), [GameSession.Combat.cs:179](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L179), 299–305행).

### 현재 학습 방해 요인

아래는 소스로 확인되는 위험이며 “모든 사용자가 실제로 실패했다”는 관측 결과가 아니다.

- **안내는 행동 성공형 튜토리얼이 아니다.** 현재는 “적들은 가끔 연속 공격을 합니다. 방패 표식을 살피세요!”라는 모달을 읽고 확인하면 종료한다([MvpPresenter.cs:176](../../../../Client/Assets/Game/Runtime/Presentation/MvpPresenter.cs#L176)). 두 번 대응하는 것을 확인하지 않는다.
- `comboHintSeen`은 캐릭터 진행이 아닌 `GameSaveData`의 공통 필드다([SessionState.cs:15](../../../../Client/Assets/Game/Runtime/Core/SessionState.cs#L15)). 최초 확인 시 바로 true가 되어, 실제 두 공격을 보기 전에 적을 쓰러뜨리거나 도전을 끝내도 다른 캐릭터에게 안내가 다시 나오지 않는다.
- 두 번째 방향이 무작위라 “첫 타 위, 다음 타 아래”라는 고정 안내를 붙일 수 없다. 실제 예약된 두 위협을 읽어서 안내해야 한다.
- Stage1의 회피 튜토리얼은 올바른 위 버튼을 누르면 즉시 통과하며, 적중 시각에 맞췄는지는 검사하지 않는다([GameSession.Combat.cs:160](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L160), 164–177행). 따라서 처음부터 정확한 타이밍을 익혔다고 전제할 수 없다.
- **첫 예고가 끝나는900ms와 첫 실제 적중1800ms가 다르다.** 콤보에서 `CueEndMs`를 회피할 순간으로 안내하면 이른 회피가 된다. 현재 월드 예고는 이 사이에도 외곽선을 유지하고 실제 판정120ms 전을 밝게 표시한다([EnemyTelegraphGraphic.cs:26](../../../../Client/Assets/Game/Runtime/Presentation/EnemyTelegraphGraphic.cs#L26), 32–42행). “순서를 보여 주는 예고”와 “지금 피해야 하는 표식”을 분리해야 한다.
- 확인 직후는 일반 Combat 상태여서 일반 공격과 스킬이 가능하다([GameSession.Skills.cs:11](../../../../Client/Assets/Game/Runtime/Core/GameSession.Skills.cs#L11)). 강화·스킬로 첫 연속 공격 전에 적이 죽거나, 기절로 남은 큐가 지워질 수 있다([GameSession.Combat.cs:359](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L359)). 이것이 가능한 상태라는 뜻이며 특정 계정에서 실제 몇 타에 죽는지는 측정하지 않았다.

### 새 연속 공격 학습안 — 제안, 미적용

처음 한 번은 두 방향과 두 적중 시점을 고정한 연습을 따로 예약한다. `순서 보기 → 첫 판정 직전 잠깐 정지 + 필요한 방향 하나 → 성공 → 두 번째 판정 직전 필요한 방향 하나 → 성공 → 일반 전투`로 끝낸다. 텍스트는 “▲로 피해!” / “▼로 쳐내!”처럼 그 순간 필요한 행동만 표시한다. 완료 플래그는 안내 확인 시가 아니라 두 대응을 마쳤을 때 저장한다. 학습 중 공격·스킬로 몬스터가 먼저 사라지는 경우와 중단 후 복원을 명시적으로 처리한다.

이는 현재 모달을 동작 튜토리얼로 바꾸는 **새 상태·저장 계약**이다. 일반 전투의 회피 창을 임의로 늘리는 구현과 구분한다. 원래의750ms 간격과900ms 예고는 학습 종료 후 유지할 수 있으며, 학습만 느리게 할 경우 별도 연습 시간표로 기록한다.

## 3. 현재 구현: 후퇴 뒤 공격 리듬의 동기화 지점

### 기존 이동·판정의 책임

- Core가 `CueAtMs/CueEndMs/ReleaseAtMs/ImpactAtMs`와 실제 피해·회피를 결정한다. Unity 물리 접촉이나 애니메이션 이벤트가 판정의 원인이 아니다([CombatGeometry.cs:49](../../../../Client/Assets/Game/Runtime/Core/CombatGeometry.cs#L49), [GameSession.Combat.cs:225](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L225)).
- 입력은 Update에서 받고 LateUpdate에서 세션을 한 번 진행한다([MvpPresenter.cs:135](../../../../Client/Assets/Game/Runtime/Presentation/MvpPresenter.cs#L135)). 실제 판정 T를 한 프레임 보여 준 뒤 다음 입력 묶음을 동일 T에서 받는 장치가 이미 있다([GameSession.Combat.cs:260](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L260)). 후퇴 연출 때문에 이 장치를 우회하면 “차는 순간 눌렀는데 맞는” 문제가 재발할 수 있다.
- 매 프레임 기본 적 위치는 PlanX900으로 놓고 `SampleEnemy()`가 공격 위치를 덮어쓴다([BattlePresentation.cs:129](../../../../Client/Assets/Game/Runtime/Presentation/BattlePresentation.cs#L129)). 여기에 별도 Tween/Update가 위치를 쓰면 위치 경쟁이 생긴다.
- 현재 접근은 **적중 전160ms에 앞으로 접근 → 적중 후280ms에 원래 위치로 복귀**다([BattlePresentation.cs:300](../../../../Client/Assets/Game/Runtime/Presentation/BattlePresentation.cs#L300)). 복귀는 이전 전진을 되돌리는 것이며, 공격 전에 기본 위치보다 더 뒤로 물러나는 동작은 없다.
- authored clip은 `elapsed = (ClockMs-ImpactAtMs)/1000 + clip.ContactTime`으로 샘플링하므로 접촉 프레임을 실제 판정 T에 맞춘다([BattlePresentation.cs:264](../../../../Client/Assets/Game/Runtime/Presentation/BattlePresentation.cs#L264)). 우선순위는 **현재 실제 공격 → 직전 공격의 복귀 → 미래 예고의 준비 자세**다(265–275행). 이전에 수정한 복귀 순간이동 문제를 재발시키지 않으려면 이 순서를 유지해야 한다.

### 새 리듬 제안 — 판정 시각은 유지

기본형: **뒤로 체중 이동 → 읽을 수 있는 준비 자세 → 빠른 전진·접촉 → 확실한 복귀**. 아래 수치는 새 시안 제작용 출발값이며 적용·플레이 검증된 값이 아니다.

| 상대 시간 | 2연타 첫 공격 예시 | 화면 동작 | 판정 변경 |
|---|---:|---|---|
| 첫 CueAt~+250ms | 0~250ms | 적 기본X900에서 최대+48 Plan px 후퇴, 발 접지·몸 방향 유지 | 없음 |
| 후퇴 끝~Impact−160ms | 250~1640ms | 몸을 낮춘 준비 자세. 순서 예고와 실행 임박 표식을 구분 | 없음 |
| Impact−160ms~Impact | 1640~1800ms | 후퇴 지점에서 접촉 지점으로 빠르게 전진, ContactTime 프레임 도달 | 기존1800ms |
| Impact~+280ms | 1800~2080ms | 직전 공격 복귀를 완주 | 없음 |
| 다음 타 임박 | 2390~2550ms | 다음 타 전진. 콤보마다 큰 후퇴를 반복하지 않고 짧은 재정비로 연결 | 기존2550ms |

단타는 첫900ms 안에 같은 구조를 배치한다(후퇴0~250, 준비250~740, 전진740~900, 복귀900~1180ms). **48은1280 기획 좌표의 이동값**이며960 판정 좌표에 그대로 대입하지 않는다. 바닥의 Y와 공격 판정 영역은 유지한다. 실제 화면 경계·큰 보스 체급을 보고 후퇴 폭을 줄일 수 있다.

### 구현 전에 확정할 계약

1. 위치와 모션을 `SampleEnemy()` 한 소유자에서 전투 시계로 계산한다. 일시정지·대화·등장·여행 중에는 새 공격 위상을 진행하지 않는다. 스킬의 전투 시간과 다른 임의 타이머를 만들지 않는다.
2. 한 콤보에서 먼저 알려 주는 다음 방향의 CueAt가 이미 진행 중인 공격/복귀를 끊지 않게 한다. 첫 후퇴는 시퀀스 시작에 한 번, 후속 타는 이전 복귀 뒤 가능한 구간에서만 짧게 준비한다.
3. 기존 clip.ContactTime은 실제 ImpactAt에 고정한다. 후퇴 프레임을 단순히 Attack 앞에 붙여 접촉 시각까지 늦추지 않는다. 후퇴는 별도 포즈/클립 위상 또는 기존 준비 구간으로 제작한다.
4. 피격·기절·사망·다음 스테이지·저장 복구에서 후퇴/돌진 잔상이 남지 않도록 현재 위협과 상태를 기준으로 다시 샘플링한다. 큐가 취소되면 공격은 나가지 않는다. 이전 위치를 계속 더하는 누적 이동은 쓰지 않는다.
5. **뒤로 물러나 있는 적을 플레이어가 때릴 때도 시각적으로 닿아야 한다.** 현재 플레이어 공격은 위치 충돌 검사가 아니라 예정된200ms 타격에서 피해를 준다([GameSession.Combat.cs:335](../../../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs#L335)). 베기 끝점은 실제 적 몸 위치를 읽지만 플레이어의 접근 경로는 고정이다([BattlePresentation.cs:188](../../../../Client/Assets/Game/Runtime/Presentation/BattlePresentation.cs#L188), 193–205행). 후퇴를 이유로 임의 무적/빗맞음 규칙을 추가하지 말고, 적의 실제 몸 위치까지 베기와 접근이 자연스럽게 이어지도록 시안에서 확인한다.
6. 후퇴 모션은 발이 번갈아 지면을 디디는 충분한 원본 포즈로 만든다. 몸통을 두 위치로 튕기는 2프레임이나 뒤집힌 달리기만으로 완료 처리하지 않는다. 옷·무기·실루엣의 일관성을 유지한다.

### 추후 구현 완료 기준 제안

- 2연타 판정은1800/2550ms,750ms간격을 유지하고 화면의 접촉 포즈·VFX도 그 시각과 일치한다.
- 첫 예고가 끝난900ms를 회피할 순간처럼 오인하게 하는 실행 표식이 없다.
- 후속 예고가 직전 복귀를 끊지 않으며, 후퇴·복귀·피격 중 발 위치가 바닥에서 불필요하게 튀지 않는다.
- 이동/대화/일시정지/재개/저장복구에서 공격이 중복 실행되거나 쿨타임이 더 흐르지 않는다.
- 일반 적 처치 후 HP 유지와 보스 회복 정책이 새 기획의 명시된 규칙과 일치한다.

이 기준은 다음 구현을 위한 검증 계획이다. 이번 문서 작성으로 통과했다고 주장하지 않는다.

## 4. 통합 담당의 새 튜토리얼 계약 검토

다음은 위 현재 구현과 구분되는 **통합 제안에 대한 검토**다. Stage5 첫 연습은 위 회피→아래 쳐내기 두 입력으로 고정하고, 실제 두 판정 간격750ms를 보존하는 안을 권장한다.

1. 시작 문구는 “두 번 온다!” 한 번. 예약된 방향 목록은 ▲→▼로 보여 준다.
2. 첫 실제 판정 T1 직전에 전투 시계를 멈추고 위 버튼을 강조한다. “위로 피해!”라는 짧은 지시 후 사용자의 새로운 위 버튼 press-edge를 확인한다. 같은 T에서 회피 동작과 해당 판정을 한 번만 해소한다. 잘못 누른 버튼이나 이미 누르고 있던 버튼으로 자동 진행하지 않는다.
3. 시계를 다시 진행하고 T2=T1+750ms에서 두 번째 입력을 기다린다. “한 번 더! 아래로 쳐내!”를 보여 주고 실제 아래 버튼의 새 입력으로 쳐내기를 처리한다.
4. **정지한 T에서 누르면 기존90ms 창 기준으로 항상 완벽 판정이 되므로**, 가이드 단계의 결과 등급은 일반 회피/일반 쳐내기로 명시한다. 사용자가 입력한 것은 실제로 확인하되, 학습 정지로 만들어진 완벽 타이밍을 보상하지 않는다.
5. 최초 두 입력은 무피해 가드로 보호하고 일반 공격·스킬·향후 동료 자동 공격을 잠근다. 처치·기절로 순서가 사라지거나, 안내 도중 스킬이 먼저 나가지 않게 한다. 학습 대기 중 회피 모션과 보호막·스킬·동료의 시간은 실제 벽시계로 따로 흐르지 않는다.
6. 두 정답 입력 완료 시 학습 플래그를 저장한다. 원한다면 **그 뒤 고정 Up→Down 정상 속도 큐를 딱 한 번** 실행한 뒤 일반 패턴으로 돌아간다. 이는 이미 끝난 가이드를 강제 반복하는 단계가 아니라 실전 첫 큐다. 실패하더라도 가이드를 다시 강제하지 않으며 다음 정상 패턴으로 이어진다. “정상 속도1회”를 생략하고 바로 랜덤 패턴으로 돌아갈지도 문서에서 한 가지로 고정해야 한다.

### 재사용·입력 유지 확인

현재 위/아래 대응은 공통240ms 활성+120ms회복=360ms 재사용 제한을 사용한다. T1에 입력했다면 T2까지750ms이므로 재사용 여유390ms가 있다. **현재 데이터에는0.7초의 회피 재사용이 없다.** 정지 해제 후 새 창에서 `dodgeStartMs`를 다시 기록하고 현재 Core 유효성을 확인한다. 과거 입력을 재사용하거나 손가락을 계속 누른 상태를 다음 정답으로 인정하면 안 된다.

### 900ms 예고 간격의 UI 계약

기존 시간을 유지하는 안: T1=1800ms, T2=2550ms. 900ms는 첫 방향의 소개 종료 시점이며 회피 지시의 기준이 아니다. **▲▼ 순서 목록**과 **현재 공격의 실제 판정 진행 표시**를 분리한다. 크게 강조하는 현재 공격의 게이지·섬광은 반드시 ImpactAt에서 완료한다. 첫 진행은0→1800ms, 두 번째는 첫 판정 해소 후1800→2550ms의750ms로 보여 줄 수 있다. 900ms에서 예고 펄스가 끝났다는 이유로 “지금 피해”를 띄우지 않는다.

반대로 CueEnd와 Impact를 항상 같게 만들면 첫 적중900ms·둘째1650ms처럼 Core의 예약 구조 자체를 바꾸게 된다. 이는 기존 판정시각 유지안이 아니므로 새로운 전투 시간 계약과 별도 검증이 필요하다. 이번 후퇴 리듬·가이드 제안에는 **기존판정유지 + UI의 방향소개/실제판정 역할분리**를 우선 권장한다.


## 최종 통합 합의 · 후퇴 박자

위3절의 CueAt부터 후퇴하는 초기 제안은 최종안에서 수정했다. 큰 첫 후퇴는 Impact−500~−250ms, 준비−250~−160ms, 전진−160~0ms로 단타와 첫 연타에 동일하게 적용한다. 후속타는 Impact−370~−250ms에120ms 짧게 후퇴하고 같은90ms 준비·160ms 전진을 따른다. Stage5의 예는1타1300~1550후퇴/1800타격,2타2180~2300후퇴/2550타격이다. 이전 복귀2080ms를 먼저 마친다. 실제 방향 자세는 최근 소개한 큐가 아닌 가장 가까운 미처리 Impact에 연결한다. 이 변경은 담당자와 재검토한 기획이며 현재 구현 시각·판정·모션이 바뀐 것은 아니다.
