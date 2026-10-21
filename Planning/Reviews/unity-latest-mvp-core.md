# 최신 기획의 1~10층 MVP 진행·저장 적용

2026-10-21. Core/API와 실행 데이터 적용 완료. 이 문서 작성 시점에는 순수 C#·내보내기 검사를 통과했으며, Presenter 결합 후 Unity 자동검사·빌드·실제 화면 검수는 총괄이 별도로 진행한다. 50층 전체 콘텐츠를 구현한 상태가 아니다.

## 진행 계약

- 최초 프로필은 기존처럼 즉시 Stage 1 회피→공격 튜토리얼이다. 이후 로비 출전은 Arrival 700ms 뒤 전투를 연다.
- 일반 처치: 기본 보상 확정 → EnemyDefeat 550ms → Travel 2000ms → Arrival 700ms(정착200+Stage500) → 전투. 전투 시계는 전환 동안 멈춘다.
- 다음 적의 데이터와 HP는 Travel 시작에 준비한다. `StagePrepared`는 이 상태를 알린다. 첫 온전한 예고와 공격·회피 입력이 열릴 때만 `StageStarted`를 발행한다. 공격/회피의 이전 쿨타임 잔여를 새 적에게 넘기지 않는다.
- 5층 중간 보스는 골드·종 파편1개만 지급한다. 유물·대사·체크포인트가 없다.
- 10층 보스는 기본 보상·종 파편3개·고정 유물3후보를 처치 시 저장한다. 분해 뒤 캐릭터 첫 하단 대사3줄 → 유물3택1 → 선택/체크포인트10/HP회복 저장 → 11층으로 이동한다.
- 중간 보스와 보스의 등장 시간은 Arrival 뒤 각각1800/2200ms다. 재조우는1000/1200ms. 화면의 큰 줌·포효·표시용 공격·복귀는 Presenter가 표현하고, Core는 이 기간 피해·입력을 허용하지 않는다.
- 사망은 Death1800ms 뒤 로비를 연다. 분해·암전·등불 표현 역시 이 단일 시계의 진행도를 읽는다.
- Stage11은 기존 MVP 경계 표본이다. 그 이후를 50층 완주/NG+로 위장하지 않는다.

## API와 책임

`SessionPhase` 기존 숫자는 유지하고 `Transition`, `StoryDialogue`를 뒤에 추가했다. `TransitionKind`는 `EnemyDefeat`, `Travel`, `Arrival`, `BossIntro`, `Death`다.

`session.Transition`은 nullable `TransitionView`이며 `Kind`, `FromStage`, `ToStage`, `ElapsedSeconds`, `DurationSeconds`, `IsRepeat`, `Progress`를 제공한다. `session.Story`는 nullable `StoryView`이며 `SceneId`, `Text`, `Speaker`, `LineIndex`, `LineCount`를 제공한다. 대사 입력은 `AdvanceStory()` 또는 `SkipStory()`로만 진행한다. 자동 넘김이나 유물 자동 선택은 없다.

`Tick`와 `TickInputFrame`은 전환 중 Core 연출 시계만 진행한다. 큰 delta의 잔여는 다음 시각 전환까지 이어 쓰지만 새 Combat/Story/Reward/Lobby에는 넘기지 않는다. Presenter는 입력 잠금 구간의 터치를 버리고 새 입력만 전달해야 한다. 완료 프레임을 한 번 표시한 후 정확한 T에서 회피를 받는 기존 `TickInputFrame` 계약은 유지한다.

공격 명중은200ms, 재공격500ms다. 회피 활성160ms·완벽60ms·재사용260ms,650/900/250ms 예고와 최소450ms 연타는 유지한다. `PlayerDamaged` 이벤트도 실제 요구 방향을 전달한다.

## 저장과 마이그레이션

schema2는 전환 종류·출발/도착층·경과시간, 대사 장면/줄/보상ID, 캐릭터별 대사 완료와 보스 등장 완료를 저장한다. 대사 해금은 계정 공유 목록, 자동 표시 완료는 캐릭터별 목록이다. 등장 완료 기억은 캐릭터 안의 `cycle:bossId`이며 끝까지 재생한 시점에만 기록한다.

처치 재화와 고정 후보는 분해보다 먼저 확정한다. 대사 중 복원은 그 줄부터, 보상 선택 중 복원은 같은 후보부터 진행한다. 보상 선택 이전에는 체크포인트가 바뀌지 않는다. 선택 완료ID는 중복 지급을 막으며 저장된 전환을 재생해도 처치 보상이나 후보 추첨을 반복하지 않는다. 일반 전환·등장·사망 중 중단은 저장된 경과에서 이어진다. 전투 저장 복원은 기존처럼 일시정지된 온전한 예고부터 재개한다.

schema1도 읽으며 Core에서 schema2로 올린다. 이미 가진 유물은 유지한다. 업데이트 전에5층에서 확정된 미선택 유물 보상은 그1회만 예외적으로 유지한다(`legacyMidbossReward`). 새 중간 보스 처치에는 이 예외를 만들지 않는다. 기존10층 미선택 보상은 후보를 유지한 채 새 첫 대사부터 이어진다.

## 데이터와 검사

실행 원본은 `GameData/mvp-catalog.json`의 `mvp-1.1.0`이다. `transitions`,10층 `stories`,공격200ms를 추가했다. `Planning/story.json`에서 승인된10층 대사를 옮겼으며 exporter가 기획 문서를 자동 덮어쓰지는 않는다. 생성물은 기존 `Client/Assets/Game/Data/Generated/mvp-catalog.json` 경로를 유지한다.

- 순수 C# Core67/67: 기존 완료 프레임·30/60/144fps·방향·쿨다운·연타·동시 치명 우선과, 새 전환/대사/미니보스 보상/체크포인트/복원/마이그레이션 검사 통과.
- Python exporter19/19: 새 시간·스토리 참조 및 기존 무효 입력 시 생성물 보존 검사 통과.
- C#7.3 DTO·카탈로그 독립 검사 통과.
- 자연1~10 진행·경제·유물·저장 독립 계약808 assertions 통과.
- JsonSaveRepository에 schema1 로드와 schema2 전환/대사 JsonUtility 왕복 검사를 추가했다. Unity에서의 이 검사 결과는 총괄 통합 실행 후 기록한다.

자동 검사 결과는 실제 기기 터치, 음향, 실시간 화면의 연출 품질을 승인한 의미가 아니다. 기존 실행본 smoke의 보상2회 수치는 구버전 중간 보스 유물 규칙의 기록이며, 이번 자연 진행은10층 유물1회를 기준으로 한다.

## 통합 검사 코드 인계

`MvpSmokeProbe`를 새 진행에 맞춰 갱신했다. 200ms 튜토리얼 명중,5층 재화만 지급,10층 하단3줄 실제 UI 진행→유물 선택1회→11층 자동 진입,1800ms 사망·700ms 재출전,성장/장착/저장 재실행을 확인하도록 준비했다. 이동·큰 Stage·중간 보스/보스 등장·대사3줄·등불의 별도 화면 캡처도 남긴다.

PlayMode 검사는10개로 준비했다. 실제 저장된 `PresentationActor.prefab`/`PresentationArtCatalog.asset`과 기존 UI를 읽고 `BattlePresentation`, `PresentationOverlay`, `StageBannerGraphic`, `CombatVfx`, `ParallaxWorld`를 명시적으로 연결한다. 오디오 장치·카메라 픽셀 비교는 검사 범위가 아니다. 기존 완료 게이지/피해 팝업/감소모션 검사에200ms 접촉1회·원위치 복귀,일시정지 동결,중간 보스 무유물,보스 확대 유지/복귀,사망 등불 중 출전 잠금을 추가했다. 이 절은 코드 준비 상태이며, 실제 PlayMode·실행본 smoke 통과 결과는 총괄이 실행한 뒤 별도로 기록한다.
