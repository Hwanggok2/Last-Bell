# Dragon v1 candidate-01 — 진행 중 QA 기록

2026-10-24. 초기 후보 관찰과 실패 원인을 보존한 기록이다. 기존 실패8건은 표적 재검사에서 모두 PASS(2/2 EditMode,6/6 PlayMode,실패·스킵0)했고 설정 복구/Editor 반환을 마쳤다. 카드 가림은candidate-02, 제목/테두리 겹침은candidate-03에서 해결 확인했다. 최신 종합 결과는 [최종 결과 문서](dragon-v1-result-20261024.md)를 따른다.

- 인계 후보: `Builds/Dragon-v1-QA/LastBellMvp.exe`, version0.9.0/data mvp1.14.0.
- [캠페인](../Evidence/Dragon-v1/candidate-01/campaign.json): full1280, layout960, layout640, 별도 프로세스 resume1280 모두 자체 assert PASS·native exit0·Unity error0. 총315 PNG. 설정 두 경로 복구 일치.
- full은6×50ms 논리 tick/frame인 자동 경로, 모션은 별도 실제 프레임 fixture다. OS 입력·성능 인증 아님.
- [분석](../Evidence/Dragon-v1/candidate-01/analysis.json): 1→2/4→5/9→10 실제 프레임에서 Run0~7,3회 이동의 원경/지면 변위비0.08. 전체 경로는 여러 이동/래핑을 포함하므로 그 구간 전체 변위비는 평가하지 않는다.
- Unity 테스트는 총괄 인계에 따라 **원본 전용8093 Editor의 빈 clean 씬**에서 한 번씩 수행했다. 복사본4650파일은 해시 대조만 했고 별도 Unity를 실행하지 않았다. Editor 종료/프로젝트 전환/게임 씬 Play 없음. 테스트 뒤 빈 clean 씬과 설정 복원을 확인했다.

## 확인된 시각 결함

**DQA-001 / 유물 선택 카드 아이콘·이름 가림 / 수정 필요.**

1280 및640에서 카드의 양피지 버튼 면이 아이콘과 이름 위에 그려진다. 후보 선택/명시 확인/큐 소비 논리는 통과하지만 사용자가 후보를 식별하기 어렵다. root와 UI 담당이 최소 수정 중이다. 자동 PASS를 시각 PASS로 재사용하지 않는다.

- [1280](../Evidence/Dragon-v1/candidate-01/full-1280x720/layout-relic-choice.png)
- [640](../Evidence/Dragon-v1/candidate-01/layout-640x360/layout-relic-choice.png)
- 관련 소스: `DragonUiMigration.StyleButton`과 저장 reward receive 버튼/아이콘 형제 순서. 제품 파일은 QA가 변경하지 않았다.

2026-10-24 **candidate-02 표적 재검사에서 가림 해결 확인**. 1280/640 모두 아이콘과 이름이 보이고, 선택 표시·효과 미리보기·확정·한 번 지급/큐 소비가 통과했다. 두 프로세스는 exit0/error0/33assertions였고, 게임/화면 설정 두 경로가 원래 값으로 복구됐다. [표적 캠페인](../Evidence/Dragon-v1/candidate-02-targeted/campaign.json).

**DQA-002 / 카드 제목이 테두리와 겹침 / 후속 배치 수정 권고.**

가림이 사라진 뒤 이름의 Text 폭이 카드 면보다 넓어 수직 테두리가 글자 일부를 가로지르는 것이 드러났다. “생명의 목걸이”가 가장 뚜렷하며 1280/640 모두 재현된다. 선택과 확정을 막지는 않지만 겹침 없는 문구 기준은 충족하지 못한다. 제목을 카드 테두리 아래로 옮기거나 카드 안에 충분한 여백을 확보하는 최소 배치 수정을 권고했다. 추가 제품 수정은 root 판단과 담당자 작업으로 연결한다.

- [1280 선택 전](../Evidence/Dragon-v1/candidate-02-targeted/review-1280x720/review-relic-choice.png)
- [1280 선택 후](../Evidence/Dragon-v1/candidate-02-targeted/review-1280x720/review-relic-selected.png)
- [640 선택 후](../Evidence/Dragon-v1/candidate-02-targeted/review-640x360/review-relic-selected.png)
- [640 확정 후](../Evidence/Dragon-v1/candidate-02-targeted/review-640x360/review-result.png)

## 보스 대화 첫 캡처의 흰 피격 효과

현재 **제품 결함으로 확정하지 않음**. [layout-story-1](../Evidence/Dragon-v1/candidate-01/full-1280x720/layout-story-1.png)은 흰색이지만 [layout-story-3](../Evidence/Dragon-v1/candidate-01/full-1280x720/layout-story-3.png)은 정상 색이다. 파일 기록 간격 약156ms이며 route에서도 약144ms 뒤3페이지에서 정상 색이다. `ActorView`는45ms full flash+75ms fade이고 `CombatSuspended`는StoryDialogue를 포함하지 않는다. 대화 중 idle/frame 진행도 보인다. root 요청의 **첫 대화에서0.3초 이상 기다린 표본**은 수정 후보 표적 검사의 별도 증거로 수집할 예정.

candidate-02에서 **정상 소멸을 직접 확인**했다. [첫 대사 대기 후](../Evidence/Dragon-v1/candidate-02-targeted/review-1280x720/review-story-after-035s.png), [timeline](../Evidence/Dragon-v1/candidate-02-targeted/review-1280x720/timeline.csv): 같은 StoryDialogue/첫 페이지/전투시계200ms를 유지한 채 실제 시간 약410ms 후 enemyFlash1→0, 플레이어 idle frame1→6. 제품 결함 아님.

## 기존 테스트의 실패 목록과 원인 분리

[EditMode 원본 응답](../Evidence/Dragon-v1/candidate-01/unity-tests/editmode-result.json):186건 종료,실패2건. [PlayMode 원본 응답](../Evidence/Dragon-v1/candidate-01/unity-tests/playmode-result.json):26건 종료,실패6건. MCP는완료progress/실패목록을 반환했으나result payload는null이다. 전체 suite를 PASS로 표시하지 않는다. 새HP4경계는실패목록없음.

| 테스트 | 관찰/분류 |
| --- | --- |
|DuplicateEnemyAndDuplicateDesignAreRejected|stage4 art를 하드코딩 `wild_dog`로 바꿔 중복을 만들려 하지만 stage1은 이제 `dragon_trail_slime`. 실제 중복이 아니어서 throw 없음. fixture를 현재 stage1 art로 바꿔 중복 거부 검사를 보존해야 함|
|StoryIsOncePerCharacterAndSkipNeverChoosesRelic|구 dam 대사 문자열 기대,실제 확정 새3페이지의 온 답변. 문구 기대 갱신 필요; once/skip/queue/checkpoint 검증 보존|
|BossTenRemainsForDialogueThenRewardContinuesAndTwentyClears|구 첫 대사 `주민들을 가두고` 기대,실제 `용은 저 봉우리에 있어.`. 문구 기대 갱신 필요|
|EnemyWaitsInIdleUntilAttackAndPauses|Dragon에 한해 예고 시작부터 attack frame0 준비 자세를 고정하는 명시 분기와 기존 Idle 기대 충돌. 새 준비 자세 계약과 legacy11~20 Idle 검증을 분리해야 함. 조용히 테스트 삭제하면 안 됨|
|HighContactFrameAcceptsDownButtonAndStaysAtHome|완벽 쳐내기/HP100/홈 좌표 assertion까지 진행 후 `AttackUp` 대신 새 `DodgeDown` 모션에서 실패. 확정 조작/새native 모션 기대 갱신 필요|
|LobbyPortraitPlaysIdleAndRefreshDoesNotResetIt|현재 선택 캐릭터는dragon_hero_on인데 시험이hero_on clip을 직접 읽음. 선택 art 기준으로 수정 후 idle연속성/일시정지 검증을 유지해야 함|
|ReviewedEnemyKeepsItsAuthoredHoldsAndContactsOnTheCombatBeat|wild_dog 고정 ID에서 즉시 실패. 현재stage art 기준으로 작성된 hold/contact 검증을 다시 수행해야 함|
|SelectedPreviewPosesMatchBack08AndSlashSequence|구hero_on의 Back08·12프레임slash 기대를 새8프레임native actor에 적용. 현재art/모션별정의로 재작성 필요|

총괄의 후속 승인으로 QA가 위8건의 기대/fixture를 수정했다. 제품 규칙은 변경하지 않았다. 두 이름은 `EnemyCuePoseAndIdleRespectPause`, `SelectedNativeDodgeAndAttackFollowAcceptedTiming`으로 바꿨으며, [정확한 표적 목록](../Evidence/Dragon-v1/candidate-01/unity-tests/target-filters.json)에 새 이름을 기록했다. 대화0.35초 뒤flash0 검사도 기존 보스 흐름 검사에 포함했다. 총괄 refresh 이후8건만 다시 실행한다.

## 관측 도구 한계 발견

`Observe` 호출마다 PNG 번호가0으로 돌아가 동일한 `motion-stage-N`의 두 구간(첫1.2초,공격후0.55초)이 일부 파일을 덮었다. 이는 **QA probe 결함**이며 제품 결함이 아니다. 실제프레임 CSV는연속으로남았고, 이동/세 방어/단일UI캡처는호출구간이겹치지않는다. 다만 `motion-stage-N-000`부터파일명순으로정상속도재생하면시간순서를보장할수없다. 이미쓰인315PNG를전체구간무손실캡처로표현하지않는다. 고유번호수정과첫대사0.35초/유물선택후확정전표본을위한QA전용표적모드추가가필요하며,현재probe소스동결때문에총괄인계전까지수정하지않는다.

## 검토한 화면

대표몸체1/3/5/6/10 모두왼쪽의온을향하고,발은수평지면위에있다. 큰뿔토끼/큰엄니체급이구별되고점샘플링외곽선이유지된다. HP/이름은현재홈위치기준으로하단선반안에남아공격접근시따라튀지않는다. 세방향아이콘의중심/경계는세해상도assert를통과했다.

640선택/강화/MAX/유물보관함과1280의뢰/정산/대사에서주요문구는읽힌다. 선택상단“이야기”의Text rect는화면왼쪽밖으로약9px나가지만실제글자와버튼은안에있으므로자동bounds경고만으로잘림결함으로판정하지않는다. 의뢰의Paragraph2가첫프레임아래에있는것은기존스크롤등장흐름이며18초전체를이번초기캡처로검증하지않았다. 유물선택카드가림은별도의확정결함이다.

## 현재 보류

OS 마우스·키보드는 사용자 지시에 따라 보류. 모바일 성능,비대칭safe area,새11~50자산,모든 유물/오류 저장 조합은 이 캠페인이 증명하지 않는다. 기존QA-003 native 종료 문제는이번4회정상exit만으로 해결 판정하지 않는다.
