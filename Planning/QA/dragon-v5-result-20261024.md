# Dragon-v5 QA 최종 결과 · candidate02 채택 + candidate03 표적 확인

2026-10-25 KST · **BOUNDED_OBSERVATION_PASS / DQA5-002_CLOSED / FULL_ACCEPTANCE_NOT_CERTIFIED**

이번에 합의한 대표 관측 범위의 차단 결함은 없다. **일반 빌드 패키징은 기록된 한계를 유지하면서 진행 가능**하다. candidate02의 네이티브4프로세스·자동4680단언 통과 증거와 영향 없는 시각 관측을 채택하고, candidate03에서 Stage12 초상화 수정만 두 해상도로 재검증했다. 표적300단언과 대화30페이지 수동 검토를 통과하여 DQA5-002를 닫았다. 전체60개 합격 기준 완료나 일반 release 바이너리 자체의 실행 인증은 아니다.

## 실행별 증거와 판정

| 증거 | Runtime SHA256 | 실제 실행 범위 | 결과 |
| --- | --- | --- | --- |
| candidate02 |`4F4C66BCF7B6431E6E28DE6080B92527657BFAAA589026449964F0A9BDCE56C9` |1280/640 fresh+native resume,4프로세스 |자동 PASS, 당시 시각 DQA5-002 FAIL 보존 |
| candidate03 |`590432e7d0dc586f55c4b9b50a48196648dc20a441f1e6e94b65af0799b3c572` |Stage12 on/dam/rin,1280/640,2프로세스 |자동·표적 시각 PASS |

두 빌드 모두 v0.9.4 / data mvp1.17.0 / schema9이다. 후보별 [02 freeze](../Evidence/Dragon-v5/dragon-v5-candidate-02/freeze.json), [03 freeze](../Evidence/Dragon-v5/dragon-v5-candidate-03/freeze.json)를 보존했다. 원본 campaign/result의 `observationPassed`, `visualReview` 필드는 변경하지 않았다. 수동 판정은 아래 별도 JSON이 담당한다.

| 실행 | 단언 PASS | 연속 PNG | 정적 PNG | FAIL/Console |
| --- | ---: | ---: | ---: | --- |
|02:1280 fresh, native schema8 boot |2245 |1367 |87 |0/0 |
|02:1280 native resume, guided |32 |0 |2 |0/0 |
|02:640 fresh |2317 |1447 |87 |0/0 |
|02:640 native resume, companion/shield |86 |51 |1 |0/0 |
|03:1280 Stage12 only |150 |0 |15 |0/0 |
|03:640 Stage12 only |150 |0 |15 |0/0 |

candidate02는 PNG3042장(연속2865+정적177), candidate03은30장이다. 모든 프로세스가 exit0, timeout 없음. [02 읽기 전용 정합성 분석](../Evidence/Dragon-v5/dragon-v5-candidate-02/review/analysis.json)과 [03 표적 집계/동결 차이](../Evidence/Dragon-v5/dragon-v5-candidate-03/review/analysis.json)를 보존했다. [Client 인계](../Production/dragon-v094-client-handoff.md)의 Core217 assertion·data28·Unity EditMode198/198은 재사용한 결과이며 QA가 재실행하지 않았다.

## candidate02에서 채택하는 범위

- 실제 과거 QA schema8 파일의 이전, 별도 합성 레거시7/15/20 중간 대사 복원과 새 계정 진입.
- 회복 성장 구매, 일반3/보스10·20 처치 회복1회와 저장 복원 뒤 중복 회복 없음.
- 여관5동료의 편성·강화·서비스 확인/취소/구매·예약 환불, 슬롯 표시. 미래 동료는 명시한 규칙 fixture이다.
- guided 자연 진입, 합성 Up/Down·거절·hold 동결·완료1회, native 재시작 시 첫 시퀀스 복귀.
-3캐릭터 스킬 contact와 실제4개 ParticleSystem, pause 동결, 보호막1회 흡수와 마지막180ms 만료, 미엘 준비/실제 기절15입자/종료0 및 이전 UI 별 비활성.
-5동료 각3회 contact의 시간·pause, 실제 배우와 렌더 표본, native companion/shield 재시작 시 남은 시간·windup 보존/현재 보호막 표시/과거 burst 미재생.
- 영향 없는 Stage2/10/20 및 레거시 대화의 캡처·화자 좌우·이름/본문/버튼 관측.

5881개 동결 파일의 해시를 비교한 결과 변경은 catalog2사본과 QA probe/runner3파일뿐이다. catalog JSON 의미 차이는 actual12 및 planned22/32/42의 portraitId4개이고 sourceSha256 메타데이터가 함께 변경됐다. Core/일반 Presentation/프리팹/아트는 동일하다. 이에 따라 위 관측을 재사용한다. **candidate02 전체 캠페인을 수정 바이너리에서 다시 실행했다는 뜻은 아니다.** Stage12의 기존 첫 초상화 FAIL은 재사용하지 않고 candidate03으로 대체한다. 미래22/32/42는 데이터 차이만 확인했다.

## 수동 화면 검토

[02 visual-review.json](../Evidence/Dragon-v5/dragon-v5-candidate-02/review/visual-review.json)은 실제 연44개 검토 파일, 원본157개 경로, 해시, 판단과 한계를 기록한다. 두 해상도의 대화129개를 원래 크기의 하단 crop으로 검토했다. 이름·본문·버튼 잘림/겹침은 보이지 않았고 플레이어 왼쪽/NPC 오른쪽은 일치했다. Stage12 첫줄6개에서 여우 실루엣 오류를 발견했다.

여관 성장/편성 원본을 두 해상도에서 확인하여 회복5번째 행, 서비스 설명, 조작 버튼의 표시를 확인했다. 스킬 원본에서 금색 공격·파란 보호막·민트 준비/기절 효과가 보였다.640에서도 보호막 입자/방패 표시는 보였고, 기절 입자는 섬광 이후에도 남았다. 다만 기절 입자가 작고 옅으므로 **별 모양 선명도와 정상속도 인지성은 미인증**이다.5동료15프레임을 절반 크기의 sheet로 확인하여 배우 구분과 접촉·복귀를 관측했으며 전체 연속 동작 검수는 아니다.640 native resume의 pause 창은 잘리지 않지만 그 한 장만으로 창 뒤 보호막을 시각 인증하지 않았다.

[03 visual-review.json](../Evidence/Dragon-v5/dragon-v5-candidate-03/review/visual-review.json)은 실제12대화30페이지를 native-scale 하단 crop6장으로 모두 검토하고 첫줄 전체 원본2장을 추가 확인한 기록이다. 모든 캐릭터·해상도에서 첫 이름은 `???`, 실제 얼굴은 바룬이며 다음 페이지 이름·본문·선택 버튼도 정상이다. 데이터/해석된 화자/실제 sprite 자동 검사와 함께 [DQA5-002](dragon-v5-DQA5-002-20261024.md)를 CLOSED로 판정했다.

## 보존과 한계

두 후보 모두 game/Editor scoped PlayerPrefs 전후 일치, 합성 sentinel 및 prior-QA schema8 원본 불변을 확인했다. [02 복원](../Evidence/Dragon-v5/dragon-v5-candidate-02/playerprefs-restored.json), [03 복원](../Evidence/Dragon-v5/dragon-v5-candidate-03/playerprefs-restored.json). **실제 사용자 기본 프로필은 NOT_ACCESSED**이며 읽기·복사·해시·변경하지 않았다. 사용자 지시에 따라 OS 마우스/키보드·모바일은 DEFERRED, 직접 청음은 NOT_RUN이다.

candidate01은 registry 백업 읽기에서 막혀 프로세스0/PNG0이었던 실행 환경 문제다. 인계된 권한으로 새 candidate02를 수행하여 해소했으며 [당시 보고서](../Evidence/Dragon-v5/dragon-v5-candidate-01/preflight-report.md)와 원본을 보존했다. 제품 실패로 합산하지 않는다.

긴 동료 타이머는 수동 시계이며 contact 주변만 실제 렌더다. 보호막 전체3초 규칙 등은 Client 증거와 마지막180ms fixture를 구분한다. 모든 후퇴 archetype, typewriter/held/prebuffer 경계, 전체 거래·손상 저장, native 기절 복원, RT 수명/누수, 물리입력·모바일·음성·정상속도 성능 및 전체60기준 완료는 미인증이다. 일반 release 빌드/패키징 확인은 Root 소유이며 QA는 해당 바이너리를 실행하지 않았다. 새 코드·에셋 변경이 발생하면 이번 동결 범위를 벗어나므로 영향 평가가 필요하다.
