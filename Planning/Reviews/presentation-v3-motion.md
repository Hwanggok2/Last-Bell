# v3 모션 목록·제작 범위 검토

2026-10-21 · 모션 목록 담당 · Unity·GameData 미수정. `unity-project-foundation`과 `game-ui-wireframe-copy`의 상태 소유·실제 자산 구분 규칙을 적용했다.

## 확인 결과

**현재 실제 그림은 온 8포즈와 적 5종×3포즈, 총 23포즈다.** 원본 PNG 2장을 직접 열고, `.meta`의 이름/파일ID, 저장 `MvpArtCatalog.asset`, 임포터 배정, ActorView와 GameData를 대조했다. 모션 카탈로그는 제작 목록이며 새 프레임·완성 애니메이션을 납품했다는 뜻이 아니다.

실행 아트는 온·성문 파수병·사슬 망령·재의 불씨·성문 수문장·종의 기사 6명이다. 담·린은 데이터에 있지만 `availableInMvp=false`이고 해당 그림이 없다. 적 50종은 `world-art-roster.json`의 확장 기획 명단이다. 현재 MVP는 같은 적 5종을 반복 배치하며 확장 명단의 `targetStage`와 다르다. 예를 들어 MVP 3층은 재의 불씨, 확장 3층 계획은 빗장 사냥개다.

## 기존 상태와 빠진 프레임

| 대상 | 현재 확보 | 새로 필요한 부분 |
|---|---|---|
| 온 | idle 2장, attack 2장, 앞/뒤/위/아래 회피 각1장 | run, 전용 hit/death, 기존 포즈 사이 연결 프레임 |
| 적 5종 각각 | idle/공통 attack/stun 각1장 | 방향별 windup/attack, recover, 전용 hit/death, idle/stun 연결 |
| 중간 보스·보스 | 위 적 공통 포즈와 동일 | 줌 유지 중 roar 3포즈와 포효하는 동안 공격동작1회 |
| 담·린 | 데이터만 있음 | 플레이 상태9종과 모든 실제 그림 |
| 나머지 적45종 | 확장 기획·원형만 있음 | 개별 그림과 아래50종별 상태 매핑 전체 |

`ActorView`는 현재 회피260ms, 공격280ms, 피격180ms를 표현하고, idle에 절차 움직임·hit에 색 변화·stun에 회전을 더한다. 전용 run/death/방향별windup/roar 연결은 없다. 저장 Actor.prefab의 `HurtFrames`도 비어 있다. 필드 선언만으로 전용 피격 그림이 연결됐다고 보지 않는다. 온의 `stun=on_05`는 숙임 그림 중복이다. 게임에는 플레이어 기절 규칙을 추가하지 않는다. `.anim`과 `.controller` 자산은 이 경로에서 각각 0/0개다.

## 모형 시간과 전투 판정

| 상태 | 제안 장수·보드 길이 | 소유·검수 |
|---|---|---|
| 플레이어 idle/run | 4장/500ms, 6장/600ms 루프 | run의 발 접지0·3프레임. 절차 다리 모형은 새 도트 제작이 아님 |
| 플레이어 attack | 6장/280ms | 준비100ms→접점·검흔80ms→회수100ms. 공격입력 간격500ms와 구분 |
| 4회피 | 앞/뒤/아래4장, 위5장; 각260ms | 실제 회피160ms·완벽60ms·회복100ms는 기존 세션 소유 |
| 피격/사망 | 2장/180ms, 6장/550ms | 피격은 기존 공격시계를 초기화하지 않음. 사망은 소실 후 대기복귀 금지 |
| 적 방향별 windup | 방향마다2장; 기준650ms | 단발650/연타첫900/후속250ms에 동기화. 방향별 새 준비 자세 필요 |
| 적 방향별 attack/recover | 발동1장/80ms, 회수2장/160ms | 합계240ms 표현. 공통 회수도 사용 방향별 경로 검수 |
| 적 stun | 3장; 일반1500/중간1200/보스1000ms | 375ms 루프를 상태 유지 동안 재생. 종료는 세션이 결정 |
| 중간/보스 roar | 각3장; 최초750/1000ms | 재조우350/450ms. 줌 유지 중 재생 후 줌아웃·정착 |

모든 장수는 제작 출발값이며 신규 고유 그림 수나 확정 공수가 아니다. 방향별 회수 경로가 공통2장으로 읽히지 않으면 해당 방향을 추가 제작한다. 이전 v2의 최소 우선 예산과 달리 이 목록은 피격·사망 등 필수 상태 전체를 추적한다.

## MVP 6명 개별 모션 범위

### 온 · 종지기 · `hero_on`

| 상태ID | 목표 장수 | 길이ms | 현재 전용 포즈 | 상태 |
|---|---:|---:|---|---|
|`idle`|4|500|on_00, on_01|키 포즈 초안·연결 필요|
|`run`|6|600|없음|신규 프레임 필요|
|`attack`|6|280|on_02, on_03|키 포즈 초안·연결 필요|
|`dodge_forward`|4|260|on_07|키 포즈 초안·연결 필요|
|`dodge_back`|4|260|on_06|키 포즈 초안·연결 필요|
|`dodge_up`|5|260|on_04|키 포즈 초안·연결 필요|
|`dodge_down`|4|260|on_05|키 포즈 초안·연결 필요|
|`hit`|2|180|없음|신규 프레임 필요|
|`death`|6|550|없음|신규 프레임 필요|

### 성문 파수병 · `gate_sentinel`

실행 층: 1, 4, 8 / 필요한 방향: low_sweep, high_sweep, thrust, outer_arc.

| 상태ID | 목표 장수 | 길이ms | 현재 전용 포즈 | 상태 |
|---|---:|---:|---|---|
|`idle`|4|500|gate_00|키 포즈 초안·연결 필요|
|`windup_low`|2|650|없음|신규 프레임 필요|
|`attack_low`|1|80|없음|신규 프레임 필요|
|`windup_high`|2|650|없음|신규 프레임 필요|
|`attack_high`|1|80|없음|신규 프레임 필요|
|`windup_front`|2|650|없음|신규 프레임 필요|
|`attack_front`|1|80|없음|신규 프레임 필요|
|`windup_rear`|2|650|없음|신규 프레임 필요|
|`attack_rear`|1|80|없음|신규 프레임 필요|
|`recover`|2|160|없음|신규 프레임 필요|
|`hit`|2|180|없음|신규 프레임 필요|
|`stun`|3|1500|gate_10|키 포즈 초안·연결 필요|
|`death`|6|550|없음|신규 프레임 필요|

### 사슬 망령 · `chain_thrall`

실행 층: 2, 6, 9 / 필요한 방향: low_sweep, high_sweep, thrust, outer_arc.

| 상태ID | 목표 장수 | 길이ms | 현재 전용 포즈 | 상태 |
|---|---:|---:|---|---|
|`idle`|4|500|gate_01|키 포즈 초안·연결 필요|
|`windup_low`|2|650|없음|신규 프레임 필요|
|`attack_low`|1|80|없음|신규 프레임 필요|
|`windup_high`|2|650|없음|신규 프레임 필요|
|`attack_high`|1|80|없음|신규 프레임 필요|
|`windup_front`|2|650|없음|신규 프레임 필요|
|`attack_front`|1|80|없음|신규 프레임 필요|
|`windup_rear`|2|650|없음|신규 프레임 필요|
|`attack_rear`|1|80|없음|신규 프레임 필요|
|`recover`|2|160|없음|신규 프레임 필요|
|`hit`|2|180|없음|신규 프레임 필요|
|`stun`|3|1500|gate_11|키 포즈 초안·연결 필요|
|`death`|6|550|없음|신규 프레임 필요|

### 재의 불씨 · `ash_wisp`

실행 층: 3, 7 / 필요한 방향: low_sweep, high_sweep, thrust.

| 상태ID | 목표 장수 | 길이ms | 현재 전용 포즈 | 상태 |
|---|---:|---:|---|---|
|`idle`|4|500|gate_02|키 포즈 초안·연결 필요|
|`windup_low`|2|650|없음|신규 프레임 필요|
|`attack_low`|1|80|없음|신규 프레임 필요|
|`windup_high`|2|650|없음|신규 프레임 필요|
|`attack_high`|1|80|없음|신규 프레임 필요|
|`windup_front`|2|650|없음|신규 프레임 필요|
|`attack_front`|1|80|없음|신규 프레임 필요|
|`recover`|2|160|없음|신규 프레임 필요|
|`hit`|2|180|없음|신규 프레임 필요|
|`stun`|3|1500|gate_12|키 포즈 초안·연결 필요|
|`death`|6|550|없음|신규 프레임 필요|

### 성문 수문장 · `gate_warden`

실행 층: 5 / 필요한 방향: low_sweep, high_sweep, thrust, outer_arc.

| 상태ID | 목표 장수 | 길이ms | 현재 전용 포즈 | 상태 |
|---|---:|---:|---|---|
|`idle`|4|500|gate_03|키 포즈 초안·연결 필요|
|`windup_low`|2|650|없음|신규 프레임 필요|
|`attack_low`|1|80|없음|신규 프레임 필요|
|`windup_high`|2|650|없음|신규 프레임 필요|
|`attack_high`|1|80|없음|신규 프레임 필요|
|`windup_front`|2|650|없음|신규 프레임 필요|
|`attack_front`|1|80|없음|신규 프레임 필요|
|`windup_rear`|2|650|없음|신규 프레임 필요|
|`attack_rear`|1|80|없음|신규 프레임 필요|
|`recover`|2|160|없음|신규 프레임 필요|
|`hit`|2|180|없음|신규 프레임 필요|
|`stun`|3|1200|gate_13|키 포즈 초안·연결 필요|
|`death`|6|550|없음|신규 프레임 필요|
|`roar`|3|750|없음|신규 프레임 필요|

### 종의 기사 · `bell_knight`

실행 층: 10 / 필요한 방향: low_sweep, high_sweep, thrust, outer_arc.

| 상태ID | 목표 장수 | 길이ms | 현재 전용 포즈 | 상태 |
|---|---:|---:|---|---|
|`idle`|4|500|gate_04|키 포즈 초안·연결 필요|
|`windup_low`|2|650|없음|신규 프레임 필요|
|`attack_low`|1|80|없음|신규 프레임 필요|
|`windup_high`|2|650|없음|신규 프레임 필요|
|`attack_high`|1|80|없음|신규 프레임 필요|
|`windup_front`|2|650|없음|신규 프레임 필요|
|`attack_front`|1|80|없음|신규 프레임 필요|
|`windup_rear`|2|650|없음|신규 프레임 필요|
|`attack_rear`|1|80|없음|신규 프레임 필요|
|`recover`|2|160|없음|신규 프레임 필요|
|`hit`|2|180|없음|신규 프레임 필요|
|`stun`|3|1000|gate_14|키 포즈 초안·연결 필요|
|`death`|6|550|없음|신규 프레임 필요|
|`roar`|3|1000|없음|신규 프레임 필요|

적의 실제 필요 방향은 **4+4+3+4+4=19개**다. 재의 불씨에는 MVP에서 쓰지 않는 후방 공격을 임의로 추가하지 않는다. 기존 공통 공격5장을 방향별19개 완성 모션으로 계산하지 않는다. 현재 그림에서 파수병은 도끼, 불씨는 부유 마도형, 기사는 장병기로 읽히므로 확장 원형의 무기/체형 문구를 그대로 덮어씌우기 전에 개별 확인이 필요하다.

## 보스 표본의 완료 조건

각 보스는 `줌인 → 큰 줌 유지 중 포효3파동 + 공격동작1회 → 줌아웃 → 200ms 정착 → 전투 첫 예고`를 보여준다. 포효하는 동안 함께 나오는 공격은 위협적인 몸짓이며 **피해0·투사체0·공격 예고0**이다. 실제 공격 이벤트를 호출하거나 첫 실전 공격을 강제로 한 번 추가하지 않는다. 포효 유지 구간에서 준비0~18% → 공격18~62% → 회수62~100%로 한 사이클만 재생한다. 재조우에서도 같은 비율로 정확히 한 번이다. 선택된 보스가 idle만 확대되거나 가슴빛만 깜박이는 것은 완성 포효·공격 모션이 아니다. 기획 보드에서는 기존 키 포즈와 보조 움직임으로 타이밍을 확인하고, 미제작 표기를 게임 밖에 둔다. 반복 재생은 검토 조작이며 실제 전투의 공격 횟수를 만들지 않는다.

## 확장 50종의 개별 매핑

아래 모든 행은 개별 제작·검수가 남았다. 모든 종에 idle/recover/hit/stun/death가 필요하고 각 방향에 windup/attack 쌍을 연결한다. 5·15·25·35·45층 중간 보스와 10·20·30·40·50층 보스는 roar와 포효 중 장식 공격동작1회(피해·투사체·예고0) 검수가 추가된다. 현재 MVP에 이미 쓰이는 종은 실제 사용 방향을 확장 기획 방향과 합집합으로 유지한다.

| 확장층 | 종ID·이름 | 원형 | 방향 매핑 | 현재 그림 |
|---:|---|---|---|---|
|1|`gate_sentinel` 성문 파수병|armored_biped|low, high, front, rear|3포즈 초안|
|2|`gate_runner` 성문 길잡이|light_biped|low, high|미제작|
|3|`gate_hound` 빗장 사냥개|beast_quadruped|low, front|미제작|
|4|`ash_wisp` 재의 불씨|wisp_hover|low, high, front|3포즈 초안|
|5|`gate_warden` 성문 수문장|bell_hulk|low, high, front, rear|3포즈 초안|
|6|`gate_crossbowman` 틈새 사수|light_biped|high, front|미제작|
|7|`gate_mason` 폐문 석공|armored_biped|low, rear|미제작|
|8|`gate_lantern` 빈 등불지기|robed_hover|high, front|미제작|
|9|`gate_executioner` 빗장 집행자|chain_biped|high, rear|미제작|
|10|`bell_knight` 종의 기사|boss_knight|low, high, front, rear|3포즈 초안|
|11|`chain_thrall` 사슬 망령|chain_biped|low, high, front, rear|3포즈 초안|
|12|`chain_hound` 추 달린 짐승|beast_quadruped|low, front|미제작|
|13|`chain_reeler` 권양 파수병|armored_biped|front, rear|미제작|
|14|`chain_crawler` 고리 기어충|clock_construct|low, high|미제작|
|15|`chain_gaoler` 철창 간수|bell_hulk|low, front, rear|미제작|
|16|`chain_hooker` 고리 도약자|light_biped|low, high, rear|미제작|
|17|`chain_echo` 매듭 잔향|wisp_hover|front, rear|미제작|
|18|`chain_cantor` 속박 성가수|robed_hover|low, high|미제작|
|19|`chain_pendulum` 쌍추 운반자|armored_biped|low, rear|미제작|
|20|`chain_jailer` 매달린 집행관|boss_colossus|low, high, front, rear|미제작|
|21|`clock_sentry` 시침 파수병|armored_biped|high, front|미제작|
|22|`clock_scarab` 태엽 갑충|clock_construct|low, high|미제작|
|23|`clock_mote` 초침 불씨|wisp_hover|front, rear|미제작|
|24|`clock_scribe` 누락의 기록자|robed_hover|high, front|미제작|
|25|`clock_escapement` 멈춘 탈진기|bell_hulk|low, high, front, rear|미제작|
|26|`clock_duelist` 분침 결투자|light_biped|low, high, front|미제작|
|27|`clock_hound` 진자 사냥개|beast_quadruped|low, rear|미제작|
|28|`clock_winder` 태엽 감개꾼|chain_biped|high, front|미제작|
|29|`clock_metronome` 박자 추적기|clock_construct|high, rear|미제작|
|30|`clock_regent` 정지한 시왕|boss_colossus|low, high, front, rear|미제작|
|31|`ash_soldier` 그을린 보초|armored_biped|low, front|미제작|
|32|`ash_stalker` 탄화 추적견|beast_quadruped|high, rear|미제작|
|33|`ash_cinder` 쪼개진 잿불|wisp_hover|low, front|미제작|
|34|`ash_tollkeeper` 재받이 사제|robed_hover|high, rear|미제작|
|35|`ash_furnace` 빈 화로지기|bell_hulk|low, high, front, rear|미제작|
|36|`ash_climber` 첨탑 갈퀴꾼|light_biped|low, high, rear|미제작|
|37|`ash_chainbearer` 향로 운반자|chain_biped|low, rear|미제작|
|38|`ash_mill` 재갈이 수레|clock_construct|low, front|미제작|
|39|`ash_bladeguard` 부서진 날개병|armored_biped|high, front, rear|미제작|
|40|`ash_paladin` 잿빛 성기사|boss_knight|low, high, front, rear|미제작|
|41|`dawn_sentinel` 새벽 문지기|armored_biped|low, high, front|미제작|
|42|`dawn_moth` 빛먹는 나방|wisp_hover|low, rear|미제작|
|43|`dawn_chorister` 무음 합창자|robed_hover|high, front|미제작|
|44|`dawn_hound` 초승 뿔짐승|beast_quadruped|low, rear|미제작|
|45|`dawn_bellbearer` 쌍종 운반자|bell_hulk|low, high, front, rear|미제작|
|46|`dawn_duelist` 여명 결투자|light_biped|low, high, front|미제작|
|47|`dawn_chainweaver` 햇실 직조자|chain_biped|high, front, rear|미제작|
|48|`dawn_orrery` 천구 회전기|clock_construct|low, high, rear|미제작|
|49|`dawn_cantor` 마지막 선창자|robed_hover|low, front, rear|미제작|
|50|`dawn_last_bell` 새벽의 빈 종|boss_colossus|low, high, front, rear|미제작|

담(`hero_dam`)·린(`hero_rin`)도 온과 같은 idle/run/attack/4회피/hit/death 상태9종을 각각 매핑했다. 잠금 캐릭터의 이름만 존재하는 것을 그림 제작 완료로 표시하지 않는다.

원형10개의 발·관절 기준점, 타이밍 틀, 공용 파편은 공유할 수 있다. 그래도 각 종의 실루엣·무기 고정점·방향별 준비/타격/회수·기절 종료·사망 소실을 따로 확인한다. JSON의 `expansionMonsters` 50행에 `requiredStateIds`, `directionMappings`, `reviewChecklist`를 두어 공유 원형 뒤로 누락된 종이 숨지 않게 했다.

## 파일 정합성 검증

- 저장 아트6개 ID, 원본 메타 이름23개, 각 상태의 전용/자리표시 이름을 실제 파일과 대조했다.
- MVP6명·확장 플레이어2명·확장 적50종·MVP 방향19개를 확인했다. 확장층1~50과 가족ID 참조가 모두 유효하다.
- 모션 ID는 인물별 중복이 없고, 모든 방향 매핑의 준비/공격/회수 상태가 필수 목록에 존재한다.
- 확장 전체의 방향 매핑은 133개다. 이 숫자는 완성 클립 수나 독립 공격 규칙 수가 아니다.
- 원본 근거10개 파일의 SHA-256을 카탈로그에 기록했다. 데이터 변경 시 방향 매핑을 다시 검사한다.
- 이번 검증은 로컬 파일·목록 정합성이다. 새 애니메이션 렌더·Unity 실행·실기기 검수 완료를 뜻하지 않는다.

산출물: [motion-catalog-v3.json](../Presentation/motion-catalog-v3.json). 담당 수정은 이 문서와 카탈로그 두 파일에 한정한다.
