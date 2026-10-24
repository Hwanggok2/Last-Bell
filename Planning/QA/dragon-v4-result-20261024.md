# Dragon-v4 · v0.9.3 QA 결과 · 2026-10-24

**승인된 변경 범위 QA 완료 · PASS.** candidate03의4프로세스 모두 native exit0,1,037 checks PASS, FAIL0, Console 오류0이다. 실제 대시 누락과 QA 진단기2건을 수정·재검증하고 종결했다. 실입력·모바일·직접 청음과 정확한 동시 voice 수는 미실시/미확인 범위다.

v0.9.3 / mvp1.16.0 변경 범위만 검사한다. 기존 Core246 assertions PASS는 Client 증거를 재사용했고 재실행하지 않았다. 실입력은 사용자 지시대로 DEFERRED, 직접 청음 NOT_RUN, 실제 사용자 기본 저장 NOT_ACCESSED다. Unity Apply·빌드·제품 수정은 Root/Client 소유다.

| 후보 | 실제 실행 | 판정과 후속 |
| --- | --- | --- |
| candidate01 |1280 fresh 도중65 PASS/1 FAIL,79 연속 PNG |다렌 JSON double exact 비교 QA 오류 DQA4-001. 통과한 루카/선택 구간만 채택 |
| candidate02 |1280 continuation 완료900 PASS/0 FAIL,475 연속 PNG |QA audio tap Console 오류1로 native exit1. 기능/대사 증거는 채택하되 Stage11 실제 전진은 제품 결함 DQA4-003으로 제외 |
| candidate03 |1280 Stage11 표적40 PASS →1280 resume18 PASS →640 fresh961 PASS →640 resume18 PASS |4프로세스 exit0/Console0, 최종699 연속 PNG 검증, 대시 전진·복귀 직접 확인 |

최종 관측 Runtime SHA256은 **`BA7F4144C9936FE5306231BC298BBB3FEE13FBF91446C8FBC356C94AD537A234`**, 실행 파일은 `Builds/Dragon-v4-QA/LastBellMvp.exe`다. [최종 실행기록](../Evidence/Dragon-v4/dragon-v4-candidate-03/campaign.json), [빌드 파일 해시](../Evidence/Dragon-v4/dragon-v4-candidate-03/candidate-manifest.json), [수치 요약](../Evidence/Dragon-v4/dragon-v4-candidate-03/review/final-summary.json). 일반 배포 빌드/패키징은 Root의 별도 결과를 따른다.

candidate01 Runtime은 `1CB02919E0302E33E23C20974E497E4C48324D00295BFD06ACCEA7D833E756BF`, candidate02는 `8EA472DC57C48B7572A9A4B0295710B57D41BE5C75DB238302A5A74C4BBB7066`다. 원본 실패 결과·체크·Console·PNG를 수정하지 않았다.1280 미실시 구간만 이어갔으며 새로운 전체 반복으로 결과를 덮지 않는다.

채택한1280 증거와 최종640 fresh에서 확인한 동작은 다음과 같다. candidate03의1280은 Stage11 표적과 채택 저장의 재시작만 실행했고, 통과한 전체 기능·대사는 반복하지 않았다.

- 세 캐릭터 선택·출전·20초 스킬 준비/사용/중복 거절, 전투 clock 감소, pause/story freeze, 파일 복원과 스테이지 carry. 다렌 방패 표식 활성/복원/한 번 흡수/만료, 미엘 스킬 후 기절바, 마지막1초 쿨 경계를 확인했다. 마지막1초/100ms fixture는 전체 시간 계약의 대체 증거가 아니며 전체 계약은 Client Core 결과와 구분한다.
- 기절바 본체408×7.2, 별도 외곽선, 채움/준비/기절 색상, 예고 stroke4/opacity0.92를 확인했다. 기존 timing/geometry는 승인된 Stage11 패턴 교체를 제외하고 frozen0.9.2 catalog와 대조했다.
- Stage11은 candidate02의 고정 root.x 결함을 제외하고 candidate03의 실제 전진/복귀로 최종 판정했다. 두 해상도 각각 최초·반복 공격 모두 **296 계획px 전진, 접촉시각900/6600ms, 원위치 복귀오차0px, 발-지면Y 오차0px**다.1280은 각13개,640은 각15개 중간 복귀프레임을 기록했다. 대표 원본 PNG를 직접 열어 플레이어 방향 전진·무기 접촉·중간 복귀·원위치를 확인했다. 렌더 시계 exact 일치를 요구하지 않는 계측이며 이번 관측의 최근접 접촉시각 오차는 실제0ms였다. Stage19/20의 실제3연타와 복귀도 양쪽 해상도에서 확인했다.
- 각 해상도의 세 캐릭터×25페이지=75, 총150 대사 페이지를 전부 원본 크기 대화영역으로 읽었다. 화자 좌상단·초상화·소개순서·선택 캐릭터별 응답과 장면별 ??? 정책이 일치했다. 확인한 페이지에서 잘림/버튼 겹침이 없었다. 토리의 빠른 안내와 잔꾀, 리나의 실용적 조언, 바룬의 호탕한 말투, 도토의 구체적인 길 안내가 유지됐다. 다음/스킵/중간 파일복원 상태 검사도 통과했다.
- native 재시작은1280 다렌·640 미엘의Stage20 대사 page4에서 화자 도토·초상화·본문·선택 인물·지갑·run/진행·쿨/방패 상태를 보존했다. 최종640 파일 복원 duration21건과 각 native resume2건의 차이는0이었다. 정수 상태와 같은 인스턴스 freeze는 정확 비교를 유지했다.
- 최종640 오디오는 unmuted/muted 각각 accepted AttackStarted1·EnemyDamaged1 및 mute 전달을 확인했다. AudioSource2개이면 tap을 붙이지 않는 guard가 `PCM_CAPTURE_UNAVAILABLE / MULTIPLE_AUDIO_SOURCES`, samples0으로 기록되고 Console 오류0이었다. candidate02 PCM은 소스 귀속 오류 때문에 채택하지 않는다. 원본 WAV 터치39.297ms/타격113.719ms 파형과 실제 trigger 증거, 장치 출력/청음은 별개다. **직접 청음 NOT_RUN, 정확한 동시 voice 수·청각적 중첩 NOT_CERTIFIED**다.

candidate03 연속699 PNG(1280표적110+640 fresh589)는 해시 불일치0, 누락렌더0, 중복파일0, 누락 상태캡처0, player/enemy 발-지면Y 최대오차0px다. native resume은 각1개 정적 PNG이며 연속프레임0은 미실행 오류가 아니다. candidate02 연속475 PNG도 같은 무결성 항목이0이었다. 이는 수집된 프레임의 무결성·좌표 결과이며 정상속도 성능이나 동영상 자연스러움의 인증은 아니다.

직접 시각 검토는1280의75대사/28정적 화면,640의75대사/39정적 화면, 양쪽 native resume, 각 동작 대표 contact sheet와 지정 대시 원본 PNG다. 모든 연속 PNG를 각각 원본 해상도로 독립 열람했다고 주장하지 않는다. [1280 채택 검토 범위](../Evidence/Dragon-v4/dragon-v4-candidate-02/review/manual-review.json), [최종 직접 검토 범위](../Evidence/Dragon-v4/dragon-v4-candidate-03/review/manual-review.json), [최종 수치 분석](../Evidence/Dragon-v4/dragon-v4-candidate-03/review/analysis.json), [원본 WAV 분석](../Evidence/Dragon-v4/dragon-v4-candidate-02/review/asset-audio-analysis.json).

| 결함 | 소유 | 상태 |
| --- | --- | --- |
| [DQA4-001](dragon-v4-DQA4-001-20261024.md) |QA exact double 비교 |CLOSED: candidate02 파일 복원 허용오차 검사 PASS |
| [DQA4-002](dragon-v4-DQA4-002-20261024.md) |QA optional audio tap |CLOSED:640 guard 기록/무오류/trigger 확인 |
| [DQA4-003](dragon-v4-DQA4-003-20261024.md) |제품 Stage11 대시 이동 누락 |CLOSED:양쪽 해상도 최초·반복296px 전진/복귀0px/발Y0px, 직접 PNG 확인 |

candidate01/02/03 모두 game·Editor의 scoped PlayerPrefs 원복과 합성 구 저장 sentinel 해시가 일치했다. 실제 기본 저장 파일은 열거나 해시하지 않았다.092 namespace/define을 유지하는 제품 계약과 격리 CLI QA 실행은 구분한다. **모바일 실기기·물리 입력·직접 청음은 미실시**다.
