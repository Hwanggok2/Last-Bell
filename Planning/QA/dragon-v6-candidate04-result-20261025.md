# Dragon-v6 candidate04 · 기존 변경 범위 통과 / HP 잔상 추가 검증 대기

2026-10-25 KST. 인계받은 네 프로세스를 각각 한 번 실행해 **10,274 PASS / 0 FAIL / Console 오류 0**으로 완료했다. 기존 V6 변경 범위에서 새 차단 결함은 확인하지 않았다. **전체 acceptance는 NOT_CERTIFIED**이며, 이후 요청된 초록 실제 HP·주황 잔상 HP는 이 결과에 포함되지 않는다. 해당 기능은 새 동결 빌드의 표적 QA 후 판단한다.

- Version `0.9.5`, data `mvp1.18.0`, candidate `dragon-v6-candidate-04`.
- Runtime SHA256 `ae0a525b5378b20624d4250122ea84a993fbf3f9897a072ce3997e1b642d9d09`.
- 인계 빌드 UTC `2026-10-24T16:47:45.8240022Z`. 전체 freeze 5,890파일과 QA 소스/의존 8파일 해시 일치 후 실행했다.
- 실행 **01:50:17–01:52:12 KST**, 네 프로세스 모두 exit 0, timeout 없음. `-ContinueAfterCandidate03`으로 1280 경로만 재진행했다. 자동 재시도 없음.
- [원본 campaign](../Evidence/Dragon-v6/dragon-v6-candidate-04/campaign.json), [읽기 전용 분석](../Evidence/Dragon-v6/dragon-v6-candidate-04/review/analysis.json), [시각 검토](../Evidence/Dragon-v6/dragon-v6-candidate-04/review/visual-review.json).

## 실행과 저장 검증

| 프로세스 | PASS | PNG | UI 입력 | 파일 roundtrip |
|---|---:|---:|---:|---:|
| 1280 경로·pending checkpoint | 2,470 | 38 | 93 | 2 |
| 1280 native 재실행 | 260 | 35 | 1 | 1 |
| 640 전체 변경 범위 | 7,280 | 1,119 | 74 | 37 |
| 640 native 재실행 | 264 | 36 | 1 | 1 |
| 합계 | **10,274** | **1,228** | **169** | **41** |

PASS 수는 반복된 프레임 단언을 포함하며 인수 기준 개수가 아니다. PNG는 연속 1,159장과 정적 69장, 상태 표본은 1,228개다. 연속 프레임 SHA·개수와 필수 사건 대조를 포함한 분석 integrity=true, errors=[]였다. 관측 최대 입자는 **231/376**이다.

DTO 비교 **77개 모두 통과**. 세 비교 파일에서 기록된 double 차이 6건은 모두 **1 ULP**이며, 같은 동료 값의 payload/복원 비교 중복을 포함한다. pending 동료/스킬 잔여 시간과 cooldown, 재실행 후 cooldown/enemyHitAtMs의 직렬화 끝자리 차이다. 정확한 경로·전후 값은 분석 파일에 남겼다. 시간 규칙을 바꾸거나 차이를 숨기지 않았고, 허용 범위 밖 차이는 없다.

1280에서는 성장 fixture에서 시작해 실제 전투 사건으로 **스테이지 1→20 연속 처치**, 이야기와 정산 선택·확인을 거쳐 완료했다. 중간 단계 주입으로 대체하지 않았다. 640은 별도 전투 fixture 검사이며 연속 1→20을 다시 실행하지 않았다. native는 1280 접촉 대기와 640 접촉 후의 두 선택된 checkpoint를 다룬다.

## 실제 화면 판정

candidate04의 원본 **85장 표본**을 직접 검토했다. 640 원본 82장을 크기 변경 없이 21개 sheet로 열었고, 1280 원본 3장(완료·native 시작/종료)을 전체로 열었다. [crop manifest](../Evidence/Dragon-v6/dragon-v6-candidate-04/review/crop-manifest.json)에 원본 해시와 영역을 기록했다. 1,228장 전수 검토 주장은 하지 않는다.

- **DQA6-001 수정 확인:** 1280 candidate03와 640 candidate04에서 일반·독립 스킬·동료 합산의 세 숫자가 기절 막대 아래에 분리된다. 640 동시 접촉의 raw 합계 15,760 milli가 cyan `16` 하나로 표시되고, 일반 `10`과 독립 스킬 `13`은 별개다. 타격 순간에는 이펙트가 글자 일부를 가린다. 후기 표본에서는 셋 모두 읽히며 수명 종료 후 기존 세 숫자가 사라진다.
- **DQA6-002 수정 확인:** 두 해상도에서 미엘·리나의 일반 적 번개와 5/10/15/20 대형 적 번개가 굵고 이어진 형태로 보인다.
- **DQA6-003 수정 확인:** 네 보스 이름이 방향 아이콘·HP·정지 버튼과 겹치지 않는다.
- 640에서 여관 선택/회복, 5동료 강화 전후 수치·주기·확률·DPS, L30 상한, HP 만/반/최소 양수, 세 플레이어 스킬 양 입력 순서, 5동료 일반/발동 공격의 흰 피격을 한정 확인했다.
- 기절별은 활성·후기 표본에 나타나고 종료 후 없어졌다. 실제 입자 21개와 일시정지 시간 보존은 프로브 단언으로 확인했다. 별을 눈으로 21개 세었다는 뜻은 아니다.
- 강화 직후 반짝임이 강화 버튼 글자 위를 잠시 지나는 현상과 타격 순간의 숫자 가림은 시각 메모로 남긴다. 모든 순간에 글자가 가려지지 않는다고 판정하지 않는다.

채택 이미지:

- [640 세 숫자·수명](../Evidence/Dragon-v6/dragon-v6-candidate-04/review/640-concurrent.png)
- [640 보스 15/20 번개·HUD](../Evidence/Dragon-v6/dragon-v6-candidate-04/review/640-boss-15-20.png)
- [640 기절 활성·정지·종료](../Evidence/Dragon-v6/dragon-v6-candidate-04/review/640-stun.png)
- [640 여관·회복·상한](../Evidence/Dragon-v6/dragon-v6-candidate-04/review/640-inn-01.png)
- [1280 스테이지20 완료](../Evidence/Dragon-v6/dragon-v6-candidate-04/1280x720/fresh/v6-route-mvp-complete.png)
- [1280 수정 후 번개·보스 HUD](../Evidence/Dragon-v6/dragon-v6-candidate-03/review/1280-boss-20.png)

## 이전 증거 채택과 보존

candidate01의 1280 legacy/여관/HP, candidate02의 세 플레이어·5동료 및 저장/정지 검사를 재사용한다. candidate02의 옛 번개/HUD 결함 화면은 수정 후 근거로 채택하지 않는다. candidate03의 1280 번개/HUD/기절/동시 접촉/처치 경계는 스트리밍·PNG 관측을 채택한다. [candidate03 시각 검토](../Evidence/Dragon-v6/dragon-v6-candidate-03/review/visual-review.json)는 원본 21장 표본이다. 강제 중단된 candidate03에는 최종 프로브 보고서가 없으므로 PASS 수를 만들지 않았다.

중단 원인은 QA6-PROBE-001 직렬화 1 ULP 문자열 비교, QA6-PROBE-002 PNG 인코딩 후 기절 관측 시간, QA6-PROBE-003 보상 선택 뒤 확인 입력 누락이었다. 각 이전 실패/중단 원본은 그대로 유지했다. candidate04 경로에서 선택→렌더→확인→렌더로 정산을 끝냈으며 동일 상태 반복 제한에 걸리지 않았다.

검토 후 candidate01 **146**, candidate02 **1,000**, candidate03 **597** 원본 파일의 변경은 모두 0이다. candidate04 원본 **1,613파일**의 [해시 manifest](../Evidence/Dragon-v6/dragon-v6-candidate-04/review/raw-manifest.json)와 QA 소스 8파일 사본을 보존했다. 게임/Editor PlayerPrefs 두 범위 복원 일치, 보호 sentinel·이전 v5 QA 저장 불변, 사용자 기본 프로필 **NOT_ACCESSED**. [무결성 기록](../Evidence/Dragon-v6/dragon-v6-candidate-04/review/post-review-integrity.json)을 참조한다. 원본 자동 결과의 visualReview=NOT_RUN은 수정하지 않고 별도 수동 검토로 보충했다.

## 패키징 판단과 한계

**기존 V6 변경 범위는 제한된 QA 근거로 패키징 진행 가능**하다. 다만 이번 결과는 observed 빌드의 결과이며 일반 배포 ZIP 검증을 대신하지 않는다. 이후 요청된 **초록 실제 HP·주황 잔상 HP는 미검증**이므로 새 기능을 포함한 최종 패키지 판정은 표적 QA 뒤에 한다.

실제 마우스·키보드 검사는 사용자 요청으로 보류했다. synthetic UI EventSystem/raycast, seed·고HP·미래 3슬롯 fixture와 수동 시간 진행을 사용했다. 모바일·청음·정상 속도 성능·장시간 누수, 전체 44/이전60 인수 기준은 인증하지 않는다. Client/Data/총괄 Unity 검증은 담당자의 인계 근거를 재사용하며 QA 재실행으로 표시하지 않는다. 새 HP 기능의 probe 변경이나 실행은 아직 하지 않았다.
