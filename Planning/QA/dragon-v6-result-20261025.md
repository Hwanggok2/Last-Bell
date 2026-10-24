# Dragon-v6 최신 결과 · candidate05 HP 잔상 표적 통과

최신 결과는 [candidate05 HP 표적 보고서](dragon-v6-candidate05-result-20261025.md)를 따른다. 0.9.6의 두 프로세스 **3,036 PASS / 0 FAIL / Console 오류0**, PNG424장 수집, 원본18장 표본 시각 검토 완료. 플레이어·일반 적·보스의 초록 실제 HP 즉시 감소와 주황 잔상의 선형 감소·종료를1280/640에서 확인했다. 총괄의 HP PlayMode15/15 및 실제 접촉·일시정지 추가1/1 근거를 채택했다. 기존0.9.5 전체 변경 범위는 [candidate04 보고서](dragon-v6-candidate04-result-20261025.md)의10,274 PASS 및 시각 검토를 채택하며 반복하지 않았다. 한정 범위에서 패키징 차단 결함은 없으며 전체 acceptance=NOT_CERTIFIED, 실입력 보류를 유지한다. 아래는 보존한 candidate01 당시 기록과 후속 진단이다. candidate02 중단 경위는 [별도 보고서](dragon-v6-candidate02-result-20261025.md)에 남아 있다.

# Dragon-v6 candidate01 · 중단 / 시각 결함 1건 / 전체 미인증

2026-10-25 KST. 인계된 통합 캠페인을 **1회** 실행했다. 첫 1280×720 프로세스가 루카 스킬 접촉 후 저장 비교에서 중단되어 나머지 3프로세스는 시작하지 않았다. 자동 재시도나 소스 수정은 하지 않았다. 이미 수집된 PNG의 한정 시각 검토를 완료했으며 재개는 총괄의 새 동결/실행 인계를 기다린다.

- Candidate: `dragon-v6-candidate-01`, version0.9.5 / data mvp1.18.0.
- Runtime SHA256: `3b21748c7c16f5014a7341555b69f15675177bff12f630b6df24a16a92d456e1`.
- 전체 freeze 5,890파일 및 Runtime 해시 gate를 통과했다. 실행 시각 **01:09:00–01:09:18 KST**, exit1, timeout 아님.
- **693 PASS / 1 FAIL / Console errors0**, 22 synthetic UI edges, 완료한 파일 roundtrip9개. PNG91장(정적19+연속72), 상태91개. 이 숫자는 중복된 프레임별 단언도 포함하며 전체 기준 통과 수가 아니다.
- [원본 campaign](../Evidence/Dragon-v6/dragon-v6-candidate-01/campaign.json), [원본 result](../Evidence/Dragon-v6/dragon-v6-candidate-01/1280x720/fresh/result.json), [읽기 전용 분석](../Evidence/Dragon-v6/dragon-v6-candidate-01/review/analysis.json).

## QA6-PROBE-001 · 저장 문자열 비교 실패, 원인 미확정

`DragonThemeSmokeProbe.V6.cs:148`의 `V6Roundtrip("player-on-after")`에서 `JsonUtility.ToJson(before)==JsonUtility.ToJson(disk)`가 실패했다. 그 직전에는 일반 타격/독립 스킬 각각1접촉, 스킬 정확히200ms, noncritical 검사가 통과했다. 이전 idle/pending 저장과 여관 저장은 통과했다.

[실패 지점 저장 원본](../Evidence/Dragon-v6/dragon-v6-candidate-01/1280x720/fresh/fixtures/player-on-after/last-bell-save.json)의 payload checksum은 유효하고, JSON으로 읽은 payload 전체는 마지막 렌더 상태의 save와 같다. 당시 clock643.4284001588821ms / cooldown19356.571599841118ms다. `.bak`은 없고 Load 자체 예외도 없다. 그러나 프로브가 **로드 직후 DTO/문자열 차이를 저장하지 않았으므로 정확히 어떤 필드가 달라졌는지는 확인할 수 없다.** 정밀도·정규화·엄격한 문자열 비교 가능성은 진단 가설이며 제품 저장 손실로 확정하지 않는다.

[진단 기록](../Evidence/Dragon-v6/dragon-v6-candidate-01/review/save-comparison-diagnostic.json)을 Client/총괄 진단 입력으로 남겼다. 이 실패를 PASS로 바꾸거나 raw 결과를 수정하지 않았다.

## DQA6-001 · 실제 피해 숫자와 기절 누적 막대 겹침

**P2 / 확인된 시각 결함.** 루카 일반 공격과 독립 스킬을 동시에 사용한 1280 화면에서 위쪽 피해 숫자 `16`이 기절 누적 막대 우측과 겹친다. 숫자 `10`과 `16`은 서로 분리되지만, 위쪽 숫자는 약 x850–885/y162–178의 막대 영역을 침범한다.

근거는 원본 [접촉 프레임000048](../Evidence/Dragon-v6/dragon-v6-candidate-01/1280x720/fresh/v3-frame-v6-skill-on-overlap-000048.png)과 [후기 프레임000063](../Evidence/Dragon-v6/dragon-v6-candidate-01/1280x720/fresh/v3-frame-v6-skill-on-overlap-000063.png)이다. 두 파일을 전체1280×720으로 직접 열었다. 소스상 MvpPresenter의 기본 위치 상한 뒤 BattleEffects가 lane×62와 위쪽 이동을 추가하는 구조가 관찰과 부합한다. 이는 원인 추론이며 PNG 겹침 관측과 구분한다.

총괄이 지적한 **최대5접촉/3lane 재사용** 사례에는 도달하지 못했다. 그 위험을 이번 실행으로 재현했다고 쓰지 않는다. 총괄의 동료 동시 피해 합산/표시 변경은 이후 새 빌드에서 검토할 사항이다.

## 실제 PNG 검토 범위

[visual-review.json](../Evidence/Dragon-v6/dragon-v6-candidate-01/review/visual-review.json)에 실제 열어 본 파일, 원본 SHA, 방법과 판단을 기록했다. 전체 원본10장과 확대/축소하지 않은 crop sheet8장(27 crops)을 열었고, 중복을 제외하면 **원본33장: 정적19장 전체+연속 프레임14장 표본**이다. 나머지 연속 프레임을 전수 검토했다고 주장하지 않는다. [crop manifest](../Evidence/Dragon-v6/dragon-v6-candidate-01/review/crop-manifest.json)에 정확한 영역을 남겼다.

1280에서 이전 성장 배치/회복 전후/우상단 동료 진입, 5동료 설명·강화 전후 확률/주기/DPS, L30 토리 표시, HP 만/반/최소 양수 구분, 루카 베기·흰색 피격·후기 정리를 한정 확인했다. 구매 직후 피드백이 강화 버튼 글자 위를 지나가는 순간도 보였으며 모든 거래 상태의 가독성 통과를 주장하지 않는다.

**미실행/미검토:** 640 전체, 다렌/미엘 스킬, 5동료 전투 발동·흰색 피격, 큰 보스 상공 벼락, 실제 기절별, 최대 동시 접촉, 연속1→20, native2재시작. legacy 부팅 화면에는 pause가 있으므로 보스 HUD 전체 가독성 판정에 사용하지 않았다.

## 보존과 남은 작업

게임/Editor의 지정 PlayerPrefs 두 scope 모두 복원 일치. 보호 sentinel/이전 v5 QA seed 불변, 사용자 기본 프로필은 **NOT_ACCESSED**다. 검토 종료 시 QA 동결 파일/읽기 전용 의존8개와 Runtime이 인계 해시에 일치했고, 기록한 원본146파일의 변경은0이었다. [검토 후 무결성 기록](../Evidence/Dragon-v6/dragon-v6-candidate-01/review/post-review-integrity.json)을 참조한다. 분석기의 전체 integrity=false는 미도달 사례/누락 프로세스를 포함한다. 수집된72연속 프레임 SHA와 개수 대조에는 오류가 없었다.

담당 근거는 Client9547+217 assertions, 데이터30 PASS 및 총괄 Unity202/202 PASS를 재사용한다. QA가 이 검사를 다시 실행한 결과가 아니다. 실제 소스/프로브 수정과 재실행은 새 인계 후 진행한다. **acceptance=NOT_CERTIFIED**, 실입력·모바일 보류, 청음·성능 미검증을 유지한다.

## 후속 진단 · candidate01 결과는 보존

총괄의 [Unity 읽기 진단](../Production/DragonV095ClientValidation/roundtrip-readonly-result.json)으로 QA6-PROBE-001은 쿨다운 double **1 ULP** 차이에 대한 과도한 문자열 비교로 확인됐다. 저장 payload의 다른 필드는 같고 recovered=false다. 이는 총괄이 수행한 후속 진단이며 candidate01 자체가 통과했다는 뜻은 아니다. [재개 인계](dragon-v6-continuation-handoff-20261025.md)에 비교 수정과 새 증거 항목을 기록했다. DQA6-001 수정 결과와 미실행 범위는 아직 새 빌드 검증 전이다.
