# Dragon-v6 candidate05 · 0.9.6 HP 잔상 표적 통과

2026-10-25 KST. 승인된 HP 전용 두 프로세스를 각각 한 번 실행해 **3,036 PASS / 0 FAIL / Console 오류 0**으로 완료했다. 플레이어·일반 적·보스에서 초록 실제 HP의 즉시 감소와 주황 잔상의 유지·선형 감소·종료를 두 해상도에서 확인했다. **전체 acceptance는 NOT_CERTIFIED**이며 기존 변경 범위는 [candidate04 보고서](dragon-v6-candidate04-result-20261025.md)를 채택한다. 전체 캠페인은 반복하지 않았다.

- Version `0.9.6`, data `mvp1.18.0`, candidate `dragon-v6-candidate-05`, 실행 스위치 `-HealthBarOnly`, native mode `v6-hp-only`.
- Runtime SHA256 `405774c10578e9c81c82d335558747e7a91b61039b4b61e85db4245106a2925f`.
- 인계 빌드 UTC `2026-10-24T17:04:02.1572289Z`. 전체 freeze 5,890파일과 QA 소스/의존 8파일 일치 후 실행했다.
- 실행 **02:05:19–02:06:01 KST**, 두 프로세스 exit 0, timeout·자동 재시도 없음.
- [원본 campaign](../Evidence/Dragon-v6/dragon-v6-candidate-05/campaign.json), [읽기 전용 분석](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/analysis.json), [수동 시각 검토](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/visual-review.json).

## 실행 범위와 결과

| 해상도 | PASS | PNG·상태 표본 | synthetic UI 입력 |
|---|---:|---:|---:|
| 1280×720 | 1,512 | 211 | 2 |
| 640×360 | 1,524 | 213 | 2 |
| 합계 | **3,036** | **424** | **4** |

PASS는 반복 프레임 단언을 포함하며 인수 기준 수가 아니다. 연속 PNG 424장의 SHA·수량·필수 표본 대조는 integrity=true, errors=[]였다. 실제 검토는 **원본18장 표본**이다. 1280 보스 원본3장을 전체로 열고, 나머지15장은 크기를 바꾸지 않은5개 sheet로 검토했다. [crop manifest](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/crop-manifest.json)에 영역·해시를 기록했다.

해상도마다 실제 전투 피해 사건 세 번만 관찰했다. 플레이어는 180/360 HP 피해(50%), 일반 적·보스는 10/30 HP 피해(정확히1/3)다. **분모 HP는 명시된 QA fixture**이며 보스 밸런스 수치가 아니다. 일반 공격은 저장 UI의 synthetic 입력을 사용했고, 플레이어 피격은 실제 적 공격 경로를 통과했다. HP 그래픽의 ApplyDamage/SetValue/Sample을 프로브가 직접 호출하지 않았다. 첫 접촉 후 Core 진행을 멈추고 실제 presenter 프레임 시계로 잔상을 관찰해 두 번째 피해를 배제했다.

초록 HP는 즉시 0.5 또는0.666667로 바뀐 뒤 유지됐다. 주황 잔상은1에서 중간0.761~0.765 또는0.841~0.845를 거쳐 실제 HP에 도달했다. 프레임 delta 누적으로 **0.12초 유지+0.55초 선형 보간**과 비교한 최대 오차는 **1.59×10⁻⁷**로 허용1×10⁻⁵ 이내다. 종료가 포착된 프레임은 접촉 후0.676~0.683초였다. 이는 프레임 표본의 종료 시점이며 설정 시간 변경을 뜻하지 않는다.

[읽기 전용 곡선·픽셀 감사](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/health-trail-readonly-audit.json)에서18개 실제 HP 영역의 초록 `#51A441` 픽셀 수는 각 시퀀스 내내 일정했다. 주황 `#EF8F35`는 시작→중간에 줄고 종료 때0이었다. 직접 시각 검토에서도 두 색이 구분되고 기존 HUD를 가리지 않았다. 다른 대상 HP는 초록으로 유지됐다.

## HTML 추천 원본

1280 보스 HP가 가장 크고 변화가 명확하다. 아래 **편집하지 않은 원본3장**을 순서대로 권장한다. 실제 HP는 모두2/3이며 주황 구간만 줄어든다. [추천 manifest](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/html-image-recommendations.json)에 원본 SHA와 값이 있다.

| 순서 | 원본 | 잔상 값 | 권장 설명 |
|---|---|---:|---|
| 시작 | [000151](../Evidence/Dragon-v6/dragon-v6-candidate-05/1280x720/fresh/v3-frame-v6-hp-trail-boss-000151.png) | 100% | 실제 HP는 즉시 감소하고 주황 잔상이 남음 |
| 중간 | [000173](../Evidence/Dragon-v6/dragon-v6-candidate-05/1280x720/fresh/v3-frame-v6-hp-trail-boss-000173.png) | 84.4% | 초록 HP를 유지한 채 주황 잔상이 따라옴 |
| 종료 | [000191](../Evidence/Dragon-v6/dragon-v6-candidate-05/1280x720/fresh/v3-frame-v6-hp-trail-boss-000191.png) | 66.7% | 잔상이 실제 HP에 도달하여 주황 구간이 사라짐 |

## 담당 단위 근거와 보존

총괄의 [HP PlayMode15/15](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/root-healthbar-playmode-15.json)를 재사용했다. 생존 최소셀5건, 단일 피해 임계7건(미만/정확히/초과·정수 올림·0·큰 정수), 보간·작은 연타 비합산·종료시간 유지·큰 후속 피해·회복·새 대상/치명타 재설정3건이다. QA가 다시 실행한 결과가 아니다.

추가로 총괄의 [일시정지 실연결 PlayMode1/1](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/root-healthbar-pause-retest.json)을 확인했다. 실제Stage11 적 접촉 후0.25초 정지 동안 실제 HP와 잔상이 동결되고, 재개0.8초 후 실제 HP 변화 없이 잔상이 수렴했다. job `ff6ba71a47c241909d9cde011ac06999`. [첫 실패 기록](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/root-healthbar-pause-attempt1.json)도 보존했다. 총괄 진단에 따르면 복원 세션의 기본 정지 상태를 해제하지 않아 접촉 전제에서 실패한 테스트 fixture 오류였고, SetPaused(false)를 추가해 수정했다. 제품 변경이나 native 재실행은 없었다. 사망/다음 대상은 이번 native에서 촬영하지 않았고 단위 수준 근거로 한정한다.

candidate04 원본 **1,613파일 변경0**, candidate05 원본 **466파일**의 [해시 manifest](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/raw-manifest.json) 및 QA 동결 소스8개 사본을 보존했다. 게임/Editor PlayerPrefs 두 범위 복원 일치, 보호 sentinel·이전 v5 QA 저장 불변, 사용자 기본 프로필 **NOT_ACCESSED**. [검토 후 무결성](../Evidence/Dragon-v6/dragon-v6-candidate-05/review/post-review-integrity.json)을 참조한다. 자동 결과·이전 후보 원본은 고치지 않았다.

**기존 candidate04 채택 범위와 이번 HP 표적·시각·담당 단위 근거에서 패키징 진행 가능**하다. 새 차단 결함은 확인하지 않았다. 일반0.9.6 ZIP 제작·해시는 총괄 소유이며 이 observed 결과가 일반 ZIP의 직접 실행 검증을 대신하지 않는다. 실입력은 사용자 요청으로 보류한다. 모바일·청음·성능·장시간 누수·전체44/이전60 인수 인증은 이번 범위에 포함되지 않는다.
