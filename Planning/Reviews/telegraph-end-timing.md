# 공격 예고 종료와 회피 입력 경계 검토

2026-10-21 · 사용자가 현상을 본 화면은 **Unity 실행 화면**으로 확인했다. 변경 전의 읽기 전용 진단을 바탕으로 Core와 Presenter에 완료 프레임 처리를 연결했다. Unity6000.3.15f1 검증 복사본에서 PlayMode5/5와 EditMode58/58 자동검사가 통과했다. `-batchmode -nographics` 검사이므로 사람이 직접 화면을 보고 터치한 검수와 구분한다. Windows 빌드와 새 실행 파일의 전체smoke도 통과했고 GPU 렌더의 치명타 팝업 한 시점을 확인했다. 아트·달리기·전환·보스 서사 기획 전체의 적용 완료를 뜻하지 않는다. GameData는 변경하지 않았다.

## 변경 전 원인과 변경 후 범위

변경 전부터 Core의 판정식은 **타격 시각 T에 도착한 올바른 회피 입력을 완벽 회피로 허용**한다. 다만 `Tick`이 먼저 T까지 진행되면 즉시 피해를 확정하고 다음 공격을 예약하므로, 그 뒤 `Dodge`를 호출해도 이전 피해를 취소하지 않는다. “정확히 T에 입력한 사건”과 “T를 처리한 뒤 다음 입력 콜백이 도착한 사건”을 구분해야 한다.

변경 전 정상 전투의 `EnemyIntent.Progress == 1`은 입력을 기다리는 독립 상태가 아니었다. 당시 표시 갱신은 `Tick` 다음이므로 보통 T 직전 프레임의 거의 찬 예고 다음에 타격 결과 또는 다음 예고가 나온다. 튜토리얼만 Progress=1에서 시계를 멈추고 위 회피를 기다린다. 따라서 판정식이 T를 포함해도 실제 화면에서 완료 프레임을 보고 입력할 기회는 보장되지 않았다. 이번 변경은 정상 전투에도 완료 프레임을 정확히 한 번 제공하고 다음 입력을 T에서 평가한다.

입력 순서가 잘못됐다는 증거는 확인되지 않았다. 키보드는 `MvpPresenter.Update`에서 입력 → `Session.Tick` 순서이고, 로컬 uGUI의 EventSystem 실행 순서도 Presenter보다 앞이다. 완료 프레임이 보장되지 않는 코드 경로를 수정한 것이며, 수정 전 실기기 로그 없이 “터치가 항상 한 프레임 늦다”는 원인으로 확대하지 않는다.

## 변경 전 소스 조사 근거

| 근거 | 확인 내용 |
| --- | --- |
| [GameSession.Combat.cs](../../Client/Assets/Game/Runtime/Core/GameSession.Combat.cs)의 `ScheduleCurrentCue`/`ScheduleNextHit` | 예고 시작과 타격 시각을 세션의 `clockMs`로 예약한다. 후속 공격은 `intervalMs` 뒤에 오며 예고 시작은 그 타격에서 `telegraphMs`를 뺀 시각이다. |
| 같은 파일의 `GetEnemyIntent` | Progress는 `(clockMs−telegraphStartMs)/telegraphMs`를 0~1로 제한한다. 튜토리얼은 Progress=1 고정이다. |
| 같은 파일의 `BeginDodge` | 회피 입력은 `run.clockMs`를 시작 시각으로 저장한다. 입력의 실제 기기 타임스탬프를 별도로 전달받지 않는다. 이전 회피부터 160+100=260ms가 지나야 새 회피를 받는다. |
| 같은 파일의 `AdvanceTime` 기존 즉시 처리 경로 | `Tick`은 목표 시각까지 모든 마감 사건을 처리한다. T가 목표에 포함되면 적 타격을 곧바로 해결한다. 같은 시각의 적 타격/플레이어 예약 공격에서는 적 피해·사망이 우선이다. |
| 같은 파일의 `ResolveEnemyHit` | 필요한 방향과 일치하고 `0 ≤ T−D ≤ 160ms`면 성공, 그중 `T−D ≤ 60ms`면 완벽이다. 판정 후 다음 타격을 예약한다. |
| [GameSession.cs](../../Client/Assets/Game/Runtime/Core/GameSession.cs)의 `TimeEpsilonMs`/`Tick`/`Dodge` | 시간 오차 상수는 0.0001ms이며 `Tick`과 `Dodge`는 별도 공개 호출이다. 이 상수는 사용자용 늦은 입력 유예가 아니다. |
| [MvpPresenter.cs](../../Client/Assets/Game/Runtime/Presentation/MvpPresenter.cs)의 변경 전 `Update`/`RenderBattle` | 실행 순서100. 터치는 `Pressed`에 연결하고, 키 입력을 처리한 다음 Tick과 표시를 실행한다. 회피 버튼의 활성 표시는 현재 회피 쿨타임을 반영하지 않는다. |
| [PointerCommand.cs](../../Client/Assets/Game/Runtime/Presentation/PointerCommand.cs)의 `OnPointerDown` | `IPointerDownHandler`에서 즉시 명령을 전달한다. 손을 떼는 onClick을 기다리는 구조가 아니다. |
| [DangerGraphic.cs](../../Client/Assets/Game/Runtime/Presentation/DangerGraphic.cs)의 `OnPopulateMesh` | 전체 궤적을 먼저 그리고, 밝은 진행 부분과 사각 끝점을 겹친다. 거의 찬 예고가 화면에서 완전히 찬 것으로 보이는지는 실제 렌더로 확인해야 한다. |
| 로컬 `Client/Library/PackageCache/com.unity.ugui@23e8d17bfaf9/Runtime/UGUI/EventSystem/EventSystem.cs.meta` | `executionOrder: -1000`. EventSystem.cs의 Update는 입력 모듈 Process를 호출한다. 로컬 StandaloneInputModule은 터치/마우스 press에서 `pointerDownHandler`를 호출한다. 설치된 소스 기준이며 다른 빌드의 순서까지 보장하는 실기기 측정은 아니다. |

`unity-project-foundation`의 세션 단일 시계·판정 소유 원칙을 따른다. Presenter의 별도 애니메이션 타이머로 피해를 재판정하지 않는다.

## 650 / 900 / 250ms 예고와 방향

확인한 실행 데이터는 [mvp-catalog.json](../../Client/Assets/Game/Data/Generated/mvp-catalog.json)이다. 일반 단발 예고650ms, 연속 공격 첫 예고900ms, 후속 강조250ms이며 보스 연타 간격은 최소450ms다. 예고가 길거나 짧아도 회피의160ms 유효 구간·60ms 완벽 구간은 같다.

| 위험 | 필요한 회피 | Unity 방향 | 기획 보드 방향 |
| --- | --- | --- | --- |
| 하단 가시/low_sweep | 위 | Up | up |
| 상단 칼날/high_sweep | 아래 | Down | down |
| 정면 찌르기/thrust | 뒤 | Back | back |
| 후방 화살/outer_arc | 앞 | Forward | front |

위 표의 위험 위치는 눌러야 할 방향이 아니다. 시각적으로 큰 가시·여러 화살도 이 판정에서는 해당 패턴의 한 타격이며 방향을 맞춰야 한다.

입력 시각을 D, 타격 시각을 T, 슬라이더 값을 `offset = D−T`로 정의한다. 다음 표는 전투 중이며 일시정지·회피 재사용 제한이 없다는 전제의 경계다. 사용자에게 보이는 기획값에서는 0.0001ms 부동소수점 보정은 표시하지 않는다.

| 입력 offset | 올바른 방향의 결과 |
| --- | --- |
| −220ms부터 −160ms 미만 | 너무 이른 입력, 실패 |
| **−160ms 이상, −60ms 미만** | 일반 회피 |
| **−60ms 이상, 0ms 이하** | 완벽 회피 |
| 0ms 초과부터 +100ms | 타격 이후 입력, 실패 |
| 어느 구간이든 잘못된 방향 | 실패 |

특히 **−60ms는 완벽 회피**이며, “일반 −160~−60 / 완벽 −60~0”처럼 경계를 양쪽에 중복 표기하지 않는다. 정확한0ms는 해당 마감의 입력을 먼저 수집·평가한 경우의 성공 계약이다. 이미 처리된 피해에 대한 사후 복구를 뜻하지 않는다.

| 예고 길이 | 일반 회피 시작 Progress | 완벽 회피 시작 Progress | T |
| --- | --- | --- | --- |
|650ms|약75.38%|약90.77%|100%|
|900ms|약82.22%|약93.33%|100%|
|250ms|36%|76%|100%|

이 비율은 기획 검토용 계산이다. 게임에 퍼센트 설명을 추가하자는 제안이 아니다.

## 변경 전 실행한 읽기 전용 경계 진단

변경 전 PowerShell `Add-Type -Path`로 당시 `GameCatalog.cs`와 Core 6파일을 메모리에서 컴파일했다. 생성 카탈로그를 읽고 튜토리얼을 완료한1층 세션을 구성해 실제 `GameSession` 메서드를 호출했다. 이 읽기 전용 진단 당시7개 소스의 전후 SHA-256이 같음을 확인했다. Unity, 화면 입력, EventSystem 재생, 브라우저 타이밍 테스트는 실행하지 않았다.

| 진단 | 결과 |
| --- | --- |
| T−161ms에 Up 입력 후 T까지 Tick | 입력 명령 자체는 수락하지만 피해, HP100→92·기절 누적0 |
| T−160ms / T−61ms에 Up | 일반 회피, HP100·기절 누적20 |
| T−60ms / T−1ms에 Up | 완벽 회피, HP100·기절 누적40 |
| 1~4층에서 각 올바른 방향으로 T−60ms | Up/Back/Down/Forward 모두 완벽 회피 |
| 공개 API로 `Tick(.65f)` 후 `Dodge(Up)` | Dodge 반환값은 true지만 그 전에 HP92가 확정됨. 이전 타격은 복구하지 않음 |
| 아직 해결하지 않은 T 경계를 진단용으로 만든 뒤 `Dodge(Up)`→`Tick(0)` | HP100·누적40, 정확한 T의 판정식은 완벽을 허용 |
| clock450ms에서 입력 후200ms Tick | 입력은450ms로 기록되므로 T650에서200ms 경과, 실패 |

정확한 T 진단은 private `state.run.clockMs`를 아직 해결되지 않은 `enemyHitAtMs`로 맞춘 **진단 전용 경계 주입**을 사용했다. 당시 공개 API가 외부 입력을 그 경계에 끼워 넣을 수 있음을 입증한 것은 아니다. 이 진단 단계에서는 원본 소스나 파일을 바꾸지 않았으며, 이후 별도 수정에서 `TickInputFrame`을 추가했다.

마지막200ms Tick 사례는 프레임이 오래 멈췄을 때 입력을 이전 세션 시각으로 기록하는 한계를 보여준다. 사용자의 실제 증상이 프레임 지연 때문에 발생했다는 증거는 아니다. 평상시에도 입력 시각은 기기 사건 시각이 아니라 마지막 세션 시각으로 근사된다.

기존 [GameSessionTests.cs](../../Client/Assets/Game/Tests/EditMode/Core/GameSessionTests.cs)에는59/60/61/160/161ms, 잘못된 방향, 네 방향,450ms 연타/250ms 강조,260ms 재사용, 동시 치명 적 타격의 우선순위 검사가 있다. 정확한0ms, 실제 터치 수집 시각, 끝 프레임의 표시와 입력을 함께 검증하는 테스트는 확인되지 않았다. 이는 변경 전 조사 결과다. 이후 아래 수정에서 정확한T와 완료 프레임 검사를 추가하고 전체52개를 실행했다.

## 기획 미리보기와 실제 전투를 구분

- [Presentation/app.js](../Presentation/app.js)의 기존 `refreshWarning`은 체크박스의 성공 여부를 `warningFrame`에 전달한다. [effects-v10.js](../Presentation/effects-v10.js)의 기존 시안은 입력을 받은 시각으로 성공을 결정하지 않았다. 추가한 offset 검토는 계약 계산을 보여주는 기능이며 아래 Unity 실행 경로 수정과 별도다.
- [Visuals/wireframes-v4.html](../Visuals/wireframes-v4.html)은1400ms 간격으로 위험 상태를 순환한다. 회피 명령은260ms의 모션만 켜며 타이밍·HP 판정을 연결하지 않는다. 이 화면에서 보이는 실패를 실제 Core의 판정 결과로 설명하지 않는다.
- 새 기획 보드는 −220~+100ms 입력 offset, 실제 입력 방향, 일반/완벽/실패 결과를 함께 검토한다. 처음부터 끝까지 같은 T를 사용하고, 슬라이더 스크럽이나 재생 프레임 수가 결과를 바꾸지 않아야 한다. 이전 성공 체크박스와 새 계산이 서로 다른 결과를 만들지 않게 단일 결과를 사용한다.

## 변경 후 완료 프레임 처리

`GameSession.TickInputFrame(deltaSeconds)`를 실제 표시 프레임용 API로 추가했다. 입력 수집이 끝난 뒤 Presenter의 `LateUpdate`에서 프레임당 한 번 호출하고, 그 결과로 View를 갱신한다. 기존 `Tick`은 시뮬레이션의 즉시 마감 처리 API로 보존한다. 두 API를 같은 실전 프레임에 중복 호출하지 않는다.

| 순서 | Core와 Presenter의 동작 |
| --- | --- |
|1. T 직전 입력|PointerDown과 키 입력을 먼저 수집한다. 회피 재사용·방향 조건은 기존대로다.|
|2. 첫 T 도달|Core 시계를T에 두고 적 타격과 같은 시각의 플레이어 타격을 미확정으로 보존한다. `IsEnemyImpactPending=true`, Progress=1, HP는 아직 바뀌지 않는다.|
|3. 완료 화면 표시|Presenter가 Progress=1을 그린다. 이 화면 다음의 입력까지 별도 Tick으로 먼저 확정하지 않는다.|
|4. 다음 프레임 입력|올바른 회피를 누르면 세션 시각D=T로 기록한다. T−60≤D≤T의 기존 완벽 회피 조건에 들어간다.|
|5. 다음 LateUpdate|대기 중인 적 타격을 판정한 뒤 살아 있는 플레이어의 동시 예약 공격을 처리한다. 입력이 없거나 방향·재사용 조건을 만족하지 않으면 피해를 받는다.|

이 변경은 **논리 시각T를 잠시 유지해 한 표시 프레임의 입력 기회를 보장**한다. 기기 프레임률에 따라 그 프레임의 실제 시간 길이는 다르며 고정+30/+60ms의 늦은 입력 유예를 추가한 것은 아니다. 기존160ms·60ms 판정창은 바꾸지 않는다. 이미 확정한 피해·사망을 뒤늦은 입력으로 복구하지 않는다. 기기 입력 타임스탬프 큐는 이번 최소 변경에 도입하지 않았다.

T에 도달하고 남은 delta는 보존해 다음 진행에 사용한다. 큰 delta에서도 다음 타격을 몰아서 확정하지 않고 새 타격마다 완료 프레임을 제공한다. 후속 타격의 논리 간격450ms와 예고250ms를 유지한다. 입력 후 끝난 회피의 유효 구간을 완료 프레임 대기로 늘리거나260ms 재사용 제한을 우회하지 않는다.

대기 중 일시정지했다가 돌아오면 완료 화면을 한 번 다시 표시한 후 입력 기회를 제공한다. 이 대기 상태는 저장하지 않는다. 저장 복원은 기존 정책대로 일시정지·새 온전한 예고에서 재개한다. 새 Run이나 다른 타격 시각으로 넘어가면 이전 대기와 잔여 시간을 폐기한다. Presenter 이외의 타이머로 피해를 다시 판정하지 않는다.

## 변경 후 코드 검사 결과와 통합 상태

Core 수정 파일은 `GameSession.cs`, `GameSession.Combat.cs`, `GameSessionTests.cs`다. 실제 테스트 소스를.NET10/NUnit으로 컴파일해 **52개 통과·0개 실패**를 확인했다. 이.NET 검사는 이후 아래 Unity 자동검사와 별도로 실행했다. 실기기 측정으로 바꾸어 표현하지 않는다.

추가한 검사는 Progress1에서HP 미확정·T 회피 성공,30/60/144fps의 완료 입력 기회,−161/−160/−61/−60ms 경계,네 방향 오류,미입력,260ms 재사용,3초 delta의 잔여 시간,450/250ms 보스 연타,동시 치명 적 타격 우선,일시정지 복귀 재표시,저장 복원,새Run·새Stage의 대기 초기화를 포함한다. 기존 규칙 검사도 함께 통과했다.

Presenter는 입력 수집 후 `LateUpdate`의 `TickInputFrame`과 Render를 연결했다. **Unity6000.3.15f1에서 PlayMode5/5, EditMode58/58 자동검사가 통과**했다. 원래 열려 있던 Unity를 유지하고 `Temp/CombatFeedbackValidation/Client` 검증 복사본과 격리한 저장 경로를 사용했다. `-batchmode -nographics`에서 View 상태와 PointerDown 호출을 자동 검사했으며 사람의 실제 터치, 화면의 시각 품질, 실기기 체감을 검수했다는 뜻은 아니다.

| PlayMode 검사 | 결과 |
| --- | --- |
|`FullVisibleGaugeAcceptsPointerDownBeforeImpact`|Progress1의 완료 상태에서 PointerDown을 받은 뒤 타격을 처리하는 연결 통과|
|`FullVisibleGaugeWithoutInputStillDealsDamage`|완료 상태에서 입력이 없으면 다음 프레임 피해 처리 통과|
|`KillingPopupAndShakeSurviveStageChangeThenExpire`|일반 적StageStarted 뒤 막타 숫자·흔들림 보존과 종료 통과|
|`BossKillingBlowFinishesBeforeRewardScreen`|보스 막타 피드백을 보여준 뒤 보상 화면 전환 통과|
|`ReducedMotionKeepsPopupStationaryWithoutWorldShake`|감소 모션에서 숫자 위치 고정·월드 흔들림 없음 통과|

EditMode58개는 Core52개와 Persistence6개다. 결과 파일의 passed/failed와 개별 test-case를 대조했다: [PlayMode 결과 XML](../Evidence/UnityCombatFeedback-20261021/playmode.xml), [EditMode 결과 XML](../Evidence/UnityCombatFeedback-20261021/editmode.xml). 두 파일 모두 failed=0이다. Windows 빌드도 성공했다. 빌드 보고 크기는166,062,892bytes, 오류0이다. [통합 검증 기록](../Evidence/UnityCombatFeedback-20261021/verification.json)에 검증 복사본과 현재Runtime 소스 일치, Runtime.dll·Windows ZIP 해시도 남겼다. Android APK는 이번에 다시 빌드하지 않았다. 새 실행 파일의 전체smoke는 [smoke-result.json](../Evidence/UnityCombatFeedback-20261021/smoke-result.json)에 `passed:true`로 기록됐다.

같은 수정에서 막타120ms(치명타80ms와 겹치면 막타1회 우선), 피해 숫자650ms·마지막250ms 소멸을 연결한다. 흔들림은 기획1280×720의 막타최대5px·치명타최대3px와 Unity 월드 진폭을 구분한다. Unity는 기존X0.035/Y0.02 월드 진폭에 막타5/3배를 적용한다. 카메라 orthographic size3.1·960×540에서는 막타가 약X5px/Y3px이며, 기획1280의5px와 정확히 같은 스케일은 아니다. Unity의960기준 UI 좌표에서는 우측+72·위+54,1280×720 기획 Canvas 좌표에서는+96·−72에 해당한다. 일반 적 처치 직후StageStarted가 와도 효과를 지우지 않고, 보스 막타는 보상 화면 전650ms 동안 보이도록 연결한다. 이 연결의 상태·좌표·보상 전환은 위 PlayMode 자동검사로 확인했다. 새 실행 파일의 [GPU 치명타 팝업 캡처](../Evidence/UnityCombatFeedback-20261021/16-critical-hit.png)는 총괄이 한 시점의 시각 결과를 확인했다. 한 장의 이미지로 시간에 따른 흔들림이나 실제 터치 검수를 완료했다고 표시하지 않는다.

새 실행 파일의smoke 결과는 튜토리얼true, 보상2회, 완벽 회피76회·일반 회피1회·치명타11회·피격3회, 체크포인트11, 공격 강화29, 장착 유물1개다. **보상2회는 기존1~10층MVP의 중간 보스·보스 보상 규칙을 보존한 회귀값**이다. 이번 수정에서 중간 보스 유물 규칙을 바꾸지 않았으며, 기획의10단위 보스만 유물 지급 규칙을 적용했다는 증거가 아니다.

## 자동검사 이후 실제 조작·시각 확인 항목

| 범위 | 확인할 결과 |
| --- | --- |
| 각 방향·650/900/250ms | offset −161/−160/−61/−60/−1/0/+1ms에서 표와 같은 결과.0ms는 입력과 타격이 모두 대기 중인 동일 시각 처리로 시험 |
| 입력 장치 | 키보드·마우스·터치의 PointerDown 시각, 수락된 D, T, 표시 Progress를 같은 로그로 대조 |
| 프레임 속도 |30/60/120fps 및 긴 프레임에서 입력 사건 시각에 따른 결과가 달라지지 않는지 확인. 일반 시각 재생 FPS와 구분 |
| 재사용·방향 | 같은 T라도260ms 재사용 제한 또는 잘못된 방향이면 성공시키지 않음. UI 활성과 명령 반환 true만으로 회피 성공을 판단하지 않음 |
| 연속 공격 |450ms 타격 간격과250ms 다음 예고 유지. 이전 입력이 다음 공격에 자동 이월되지 않음 |
| 일시정지·보상·복원 | 잠금 중 입력 폐기, 정상 첫 예고 제공, 이미 확정한 피해·보상 사건 재처리 금지 |
| 표시 | T 직전 밝은 끝점·T의 타격·판정 피드백이 같은 위험을 가리키며, 성공 시 피해 숫자·백색 피격을 동시에 만들지 않음 |

현재 완료한 것은 변경 전 소스 조사, 완료 프레임 Core·Presenter 구현, .NET Core52개 검사, Unity PlayMode5/5·EditMode58/58 자동검사, Windows 빌드·새 실행 파일smoke, GPU 치명타 팝업 한 시점 확인과 문서 갱신이다. 시간적 흔들림·사람의 터치·실기기 체감 검수는 별도로 남는다. 기존 아트·달리기·분해·전진·보스 연출 기획 전체의 Unity 적용 완료는 아니다.
