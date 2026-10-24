# Dragon-v6 · 개발용 프로브 및1회 통합 캠페인 계획

2026-10-25 KST · **DESIGN_REFERENCE / IMPLEMENTATION_PREPARED / NO_EXECUTION**

[변경 범위44개 기준](dragon-v6-acceptance-20261025.md)의 관측을 위한 계획이다. 최초 계획 뒤 총괄이 구현을 승인하여 V6 partial/runner/analyzer를 준비했다. 최신 범위/정적검사는 [구현 인계](dragon-v6-probe-preparation-20261025.md)를 따른다. 아래 표는 설계 참조이며 모든 항목을 자동화했다는 뜻이 아니다. 공용 entry에 V6 분기만 추가하고 V5 partial/runner/보고서/증거는 보존했다. QA 실행은 구현·저장 Apply·담당 검사·최종 동결/빌드와 실행 소유권을 총괄이 인계한 뒤 한 번만 수행한다.

## 소유권과 재사용

- QA: 향후 `Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.V6.cs`와 meta, entry의 v6 허용/dispatch/정리만 최소 추가, `Tools/QA/Dragon-v6/`, `Planning/QA`와 새 evidence 폴더.
- Client/총괄: Core/Data/저장 정책·일반 UI/Presentation·효과·Editor·씬/프리팹·Apply·빌드. QA가 누락된 제품 동작을 fixture나 프로브에서 구현하지 않는다.
- 재사용 후보: 기존 strict-child 경로/링크 거절/해시 유틸, saved Button/PointerCommand raycast, buffered frame collector, 공개 Core 상태와 실제 ParticleSystem 관측. V5 전체 캠페인을 호출하거나 V5 증거 파일을 새 결과로 다시 쓰지 않는다.
- 새 도구 예정: `common.ps1`(v6 동결/이전 QA seed gate), `run-campaign.ps1`(1회 순차 실행/첫 실패 중지/prefs 복원), `analyze.py`(무결성/횟수/참조·중복 분석). 정해진 실패만 새 candidate의 좁은 target으로 검증한다.

## 실행 전에 받아야 하는 계약

| 계약 | 프로브에 필요한 최소 정보 | 미수신 시 처리 |
| --- | --- | --- |
|독립 스킬 |일반/스킬 별도 pending ID·target ID·deadline, 시작/접촉/취소 이벤트, CD 시작점, 재입력 거절, 동시 적/플레이어 사망 우선순위 |기존 pendingSkillAttack를 추측해서 읽지 않고 해당 구현/판정 대기 |
|동료 발동 |5종 안정 skill ID, 실제/다음 확률·period 공개 값, units/rounding/cap, 예약 시점·sequence·발동 결과·RNG state/draw index |원하는 결과를 Reflection/비공개 필드 조작으로 강제하지 않음 |
|성장/DPS |레벨별 산식/한도, 확률 가중 cap, 강화 단조성의 지표, 현재 run snapshot 갱신 시점 |제안 수치를 테스트에 하드코딩하지 않음 |
|저장 |최종schema·9이전, pending 타격·RNG·잔여 시간/일시정지의 유지/취소 계약, 손상/누락 기본값, 실제 새 이벤트 이름 |보존되어야 할 것과 의도적 취소를 명시한 expectation 고정 |
|UI/효과 |v5 이전 성장 기준, saved 버튼/HP/팝업/effect 참조, glyph 목표 크기, 효과시계·수명·풀 기준·동시 타격 표시 정책 |이미지 비교 기준 또는 읽을 참조가 없으면 CONTRACT_PENDING |

Core [최종 인계](../Production/dragon-v095-client-handoff.md), [여관 배치](../Production/dragon-v6-inn-handoff.md), [효과 인계](../Production/dragon-v6-effects-handoff.md)를 반영했다. 아래 세부 계획의 최신 구현 범위와 차이는 구현 인계를 따른다. 기존 GameSession.Skills의 CanUseSkill→CanBeginAttack 및 pendingSkillAttack 결합은 v5 구조이며 v6 API로 가정하지 않는다.

확정된 관측 연결은 `UseSkill/CanUseSkill/SkillContactPending/SkillContactRemainingMs`, `PlayerSkillStarted/PlayerSkillContact`, 동료 기존 `CompanionAttackStarted/CompanionDamaged`의 `SkillId` 분기, 모모 `PlayerRecovered`의 실제회복량이다. CompanionView의 SkillChancePercent/NextSkillChancePercent/PeriodMs/NextPeriodMs/ExpectedDpsMilli/NextExpectedDpsMilli, RunCompanionView의 PendingSkillId를 사용한다. RNG 필드 이름과 상세 DTO는 최종 구현을 읽어 연결한다. 새 스킬을 기존 EnemyDamaged 이벤트로만 세면 누락되므로 V6 collector에서 별도 수집한다.

접촉순서는 적impact/death→일반타격→독립스킬→동료이고, player독립200ms/contact·승인부터20000msCD를 검사한다. 기존 일반 pending 타격 복원은 Client 정책대로 판단하며 모두 이어진다고 가정하지 않는다. 새 player독립예약은1회 재개, 동료예약은적전환에도선택유지→grace뒤1회접촉이 기대값이다.

## 캠페인 구성안

**통합 캠페인1회 안에 순차4프로세스**를 기본으로 계획한다. 계약상 네이티브 복원 추가점이 필수라면 freeze 이전에 총괄과 프로세스 수를 확정하며, 실행 중 임의로 캠페인을 늘리지 않는다. 일반 저장/복원 검사는 같은 프로세스의 실제 repository 파일 roundtrip을 사용하고, 아래2개만 프로세스를 실제 종료/재시작하는 검사다.

| 순서/예정 모드 | 크기 | 범위 | 종료 시점 |
| --- | --- | --- | --- |
|1 `v6-legacy` |1280×720 |v5 schema9 QA seed boot/이전, 여관 UI·거래, 세 플레이어 독립 스킬과5동료 기본/확률 스킬, 대표 경계·단위별 저장, 새 세션1→20 순차 진행 |checkpoint A: 일반+플레이어 스킬+동료 확정 결과가 접촉 전 겹친 상태 |
|2 `v6-resume` |1280×720 |A native restore·pause·재개·각 허용 접촉1회/RNG 연속성·현재 상태 표시 |권위 상태/효과 정리 확인 후 종료 |
|3 `v6` |640×360 |좁은 화면 UI/HP/큰 팝업/세 독립 스킬/5동료·동시 타격과 경계 시각 관측, 단위별 저장 대표 재확인 |checkpoint B: 미엘 또는 남은 중요 스킬 접촉 후, CD/동료 다음 RNG·지속 효과가 남은 상태 |
|4 `v6-resume` |640×360 |B native restore가 지난 contact/번개/팝업을 재생하지 않고 남은 상태를 계약대로 복원 |다음 유효 지원 공격과 effect 정리 확인 후 종료 |

A의 플레이어는 Client 복원 계약과 위험을 보고 확정한다. B는 A와 상보적인 상태를 고른다. 세 플레이어와5동료 **모두** 실제 저장/로드 단위표에 포함하지만 모두 native 재시작했다고 쓰지 않는다. 두 해상도에서 반드시 별도1→20 전체 경로가 필요한지는 freeze 전 확정한다. 기본안은1280에서1회 연속 경로,640에서 동일 변경 상태의 대표 관측이며 양쪽 모두 전체진행이라고 주장하지 않는다.

## 프로브 단계와 성공 조건

| 계획 ID | 단계 | 관측/실패 기준 | 합격 기준 |
| --- | --- | --- | --- |
|Q00 |입력 사전 검사 |Runtime/전체freeze/catalog 버전·QA source SHA·이전schema9 seed envelope·새 폴더·scoped prefs 백업 일치. 불일치면 launch0 |R05,R08 |
|Q01 |여관 복원 |실제 버튼으로 성장↔동료·선택·회복 구매·강화 현재→다음·부족/취소·최대 상태. 원본2해상도 캡처 |U01–U08 |
|Q02 |HP/숫자 |권위 상태로 만HP/부분/경계/저HP/0과 피해/회복/방패, 작은·긴·동시 숫자 발생. 값 조작은 명시한 별도 fixture로만 |V01–V04 |
|Q03 |독립 플레이어 스킬 |3ID×공격→스킬/역순/접촉 전·같은 마감·후. 충분한HP 대상에서는각1타, 낮은HP에서는정해진 순서 뒤 죽은 대상 타격 억제 |S01–S06,V07 |
|Q04 |상태 거절/경계 |pause/연습/대사/죽음/전환 거절,동시 막타·회피 유지, CD직전/경계/직후,효과·구독 정리 |S07–S10,R06 |
|Q05 |동료 기본/전용 |5ID×미발동/발동을 각각 반드시 포함하도록 재현 가능한seed fixture로 고정.각 contact 전/접촉/후와3주기 trace,흰색피격·팝업·전용효과·회복/기절 대표사례 |C01,C02,C05–C10,V05,V06 |
|Q06 |강화·cap |5종L1→L2,현재한도/최대 및cap영향편성. UI/저장/출전 snapshot 일치. 전 레벨 수학 경계는 동일freeze Client 결과로 연결 |C03,C04,C08,U06,U07 |
|Q07 |저장 단위표 |3플레이어×idle/pending/접촉후,5동료×미발동/발동 예약/접촉후의실제QA repository roundtrip;지갑/회복/편성·RNG·deadlines 비교 |R03–R05,C06 |
|Q08 |1→20 연속 경로 |새세션1부터죽지않는범위의명시된성장준비,실제처치·이동·대사/보상·마지막MVP. 매단계진행·보상ledger. DTO stage 점프 금지 |R01,R02,S05,S10 |
|Q09 |최대 겹침·작은 화면 |플레이어공격+스킬+복수동료/위험예고/흰색피격/큰숫자,섬광후기프레임포함. 가독성은후속수동검토 |V04–V08,R07 |
|Q10 |native2복원 |저장 기대값·원본checkpoint SHA 보존,복사한별도save로새프로세스. pause에서재개·RNG/각유효접촉1회·oldburst0 |R04,R06 |
|Q11 |정합성/수동 판정 |raw result와Console,파일/해시/단언/이벤트/프레임 대조. 실제연PNG만review JSON에 기재 |R08,시각 기준 전체 |

Q08의 성장 준비는 합성 재화/높은HP 등을 쓸 경우 필드·이유를 명시하며 정상 난이도/밸런스 검증이라고 부르지 않는다. 전투 타이머를 수동 진행한 구간도 반드시 표기한다. 자연 진행 계약을 우회하는 stage/완료 플래그 주입은 Q08에 쓰지 않는다. 실패 시 무한 재시도·죽음 보정·결과 강제 변경 없이 멈춘다.

Q05의 seed 선택은 공개 생성/저장 경계와 Client가 제공한 검증 가능한 fixture를 사용한다. 정해진 seed가 스킬 발동/미발동을 낳는지 실제 event에서 확인한다. 기대 seed를 못 만들면 그 사례는 미검증으로 남기며 판정 직전에 Core 확률을100%로 바꾸지 않는다. 확률분포를 짧은 플레이 빈도로 검증하지 않는다.

## 수집할 근거

예정 `v6-state.jsonl`은 scenario/fixture/clockMode, run/stage/target·attack/skill/companion sequence, 일반·스킬 각각 시작/접촉 deadline과CD, RNG state/draw index/예약결과, HP/회복/보상·wallet/레벨/주기, pause/phase를 남긴다. 공개 API에 없는 항목은 최종 serialized QA DTO에서 얻을 수 있는지 확인한 뒤 이름을 확정한다.

렌더 상태에는 actor clip/time/root/feet, HPbar bounds/value/cell count, 실제 팝업 숫자·bounds·활성 수, enemy flash상태, 각 실제 ParticleSystem의instance/입자수·위치/크기/색/수명, RT크기/Point/AA/바인딩과저장참조를 기록한다. **GameSession 이벤트 또는 파티클 존재만으로 흰색피격/벼락/선명성 PASS를 만들지 않는다.** 시작·접촉·섬광후·정리 프레임을 짧은 실제 렌더 창으로 수집하고 전체 긴 타이머는 불필요하게 매프레임 캡처하지 않는다.

예정 출력은 새 `Planning/Evidence/Dragon-v6/<candidate>/` 아래 resolution/fresh|resume, 원본 result·state·input·event/frames·PNG·save 기대값, source/build manifest, prefs 복원, 보호 파일 무결성이다. 추가 analysis는 읽기 전용이며 manual-review JSON은 실제 열어 본 원본/crop/재생 구간·판정·한계를 별도로 적는다.

## 진행 순서와 중단 규칙

1. 총괄/Client/UI/효과 계약 인계 → 수치·API·기준 이미지·저장 기대값 확정. 미정 값을 목록으로 남긴다.
2. 별도 요청된 구현 단계에서만 V6 partial/runner/analyzer 작성. C# 구문/타입·PowerShell/Python 구문·경로/manifest 정적 검증, DLL emit/Unity/native 실행 없음.
3. QA 소스 해시/변경 파일 인계 → 총괄 Apply/담당 검사/Observed build/전체source freeze. QA는 동결 후 코드를 수정하지 않는다.
4. 실행 소유권·새 candidate 인계 → scoped prefs를 백업/복원하는 순차 캠페인1회. 첫 실패 뒤 나머지 프로세스는 실행하지 않고 원본을 보존한다.
5. 분석·수동 화면 검토 → 확정된 실제 제품 결함과 프로브/환경 오류를 구분. 제품 수정 후 해당 조건과 직접 영향 범위만 새 candidate에서 표적 재검한다. 환경/프로브 오류는 원인과 미실행 범위를 총괄에 보고하고 재개 인계를 받는다.
6. 최종 보고서는 수행 범위·재사용한 담당 결과·미검증·보류·원본 SHA를 함께 제시한다. OS 실입력은 사용자 지시에 따라 계속 보류한다.

총괄 승인 뒤 현재 필수 사례로 V6 소스를 작성·동결했다. 최신 구현/제한은 위 구현 인계와 manifest를 따른다. 빌드 경로는 `Builds/Dragon-v6-QA`(관측)/`Builds/Dragon-v6`(일반), 운영log는dragon-v6-operation, 버전0.9.5/data1.18.0이다. 캠페인 시작은 최종 source freeze와 별도 실행 인계 이후다.
