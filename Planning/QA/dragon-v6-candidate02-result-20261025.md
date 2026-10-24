# Dragon-v6 candidate02 · 관측 시간 오류로 중단 / 시각 수정 재검 대기

2026-10-25 KST. 수정 관측 빌드 Runtime `c6941292af3dba88f808e97130eab08a9792f5aef074dd84c7931119735d38a1`에서 `-ContinueAfterCandidate01`로 1회 재개했다. **1280 첫 과정만 실행, 나머지 3과정 미시작**이다. 자동 재시도하지 않았다.

실행 01:27:33–01:28:51 KST, exit1/timeout 아님. **5,167 PASS / 1 FAIL / Console0**, UI edges44, 파일 roundtrip29개/DTO 비교58개 PASS. PNG791(정적7+연속784), 상태791, 실제 최대141입자/설정 예산376이다. 반복 프레임 단언 수를 전체 합격 기준 수로 해석하지 않는다. [원본 campaign](../Evidence/Dragon-v6/dragon-v6-candidate-02/campaign.json), [분석](../Evidence/Dragon-v6/dragon-v6-candidate-02/review/analysis.json)을 보존했다.

## 완료 범위와 관측 중단

세 플레이어 정·역순 독립 스킬/접촉/저장·pause·CD 경계, 5동료 각각 기본·발동/예약·저장/모모회복, 큰 적5/10/15/20의 미엘+리나 접촉이 실행됐다. 저장 비교 수정 뒤 중단은 없었다. 이 런의58개 DTO 비교에 허용오차를 사용한 차이는0개였고, candidate01의1ULP 원인 확인은 총괄의 별도 읽기 진단을 근거로 한다.

**QA6-PROBE-002: 기절 관측창 끝 단언의 오류.** `v6-miel-ready` 끝 전투시계403.470896ms 뒤 PNG encoding1.571991초가 발생했고, 다음 `v6-actual-stun` 첫 frame의 dt1.604333초가 전투 시계에 들어가2007.804178ms가 됐다. 정상 타격/기절 시작은603.470896ms, 종료 예정은2103.470896ms였다. 따라서 첫 프레임000744에는 기절중/21입자가 실제 있고,000749부터 기절종료/0입자다. 창 마지막2706.163770ms에서 여전히 기절중/21이어야 한다고 요구한 프로브가 실패했다. **제품 별 누락으로 판정하지 않는다.** [시간 진단](../Evidence/Dragon-v6/dragon-v6-candidate-02/review/stun-timing-diagnostic.json).

기절 pause/종료 단언, **5접촉 합산3숫자**, 막타 경계, 연속1→20,1280 native복원,640 전체는 실행하지 못했다. 5접촉 검사가 통과했다는 주장은 이 증거로 뒷받침되지 않는다.

## 실제 화면 판정

[visual-review.json](../Evidence/Dragon-v6/dragon-v6-candidate-02/review/visual-review.json): 전체 원본6장 및 무배율 crop sheet11장(37개 source crops)을 실제로 열었다. 중복 제외 **원본37/791장**의 표본 검토이며640 증거는 없다.

- DQA6-001: 1280 일반+스킬2숫자 원본과 후기 crop에서 기절 막대 침범이 사라졌다. 기존 재현 조건 수정 확인이며 합산3숫자/640까지 통과했다는 뜻은 아니다.
- **DQA6-002 / P2: 벼락 형태가 작은 점선으로 끊긴다.** 미엘 일반적 원본000139, 리나 발동461/471, 큰 적4종 contact/late에서 확인했다. 접촉/입자는 있고 상공 여유도94–120UI로 fallback 조건48보다 높지만, 굵고 연속된 지그재그로 읽히지 않는다. Root의 두줄 폭 변경은 이후 새 빌드에서 재검한다.640 품질은 미실행이다.
- **DQA6-003 / P2: 보스 이름과 방향 예고 아이콘 중첩.** [10단계 원본000622](../Evidence/Dragon-v6/dragon-v6-candidate-02/1280x720/fresh/v3-frame-v6-boss-lightning-10-000622.png), [20단계 원본000702](../Evidence/Dragon-v6/dragon-v6-candidate-02/1280x720/fresh/v3-frame-v6-boss-lightning-20-000702.png)에서 노란 방향 아이콘이 이름/레벨 글자와 겹친다. Root는 이름을 중심(900,84),440×40으로 옮겼다고 인계했다. 실제 새 캡처 판정은 아직이다.
- 동료5종 기본/발동의 흰색 피격과 청록 숫자, 전용 접촉 모양, 미엘 외 루카·다렌 효과는 표본 범위에서 식별했다. 기절별3개는 active 원본에서 보였지만 시간 오류 때문에 전체 지속 구간은 인증하지 않는다.

## 보존·다음 범위·패키징 제한

지정 게임/Editor PlayerPrefs 모두 복원 일치, v5 QA seed와 candidate01 raw 보존 gate 일치. 사용자 기본 저장 **NOT_ACCESSED**. candidate02 원본1000파일 manifest 및 당시 QA소스8개 보관본을 남겼다. [후처리 무결성](../Evidence/Dragon-v6/dragon-v6-candidate-02/review/post-review-integrity.json).

[tail 인계](dragon-v6-tail-handoff-20261025.md)는 인코딩 dt 소진/active21·ended0 분리와 미실행 범위만 보완한다. 기존 완료1280 player/companion 범위는 재사용하고, 수정 영향인 일반적 미엘/리나와 큰 적4종 벼락·보스 HUD만 표적 재검한다. 합산3숫자/막타/route/native 및640은 최초 수행이다.

현재 근거로 **전체 QA 완료 또는 최종 배포 검증 완료 패키지로 표시할 수 없다**. 내부 관측 빌드/증거 전달은 가능하며 최종 정상 빌드와 패키징은 총괄 소관이다. acceptance=NOT_CERTIFIED, 실입력·모바일 보류, 청음·성능 미검증. 기존 Unity EditMode202/202 및 총괄 팝업 PlayMode1/1 PASS는 담당 근거 재사용이며 QA 재실행 결과가 아니다.
