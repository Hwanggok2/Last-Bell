# Dragon-v3 QA 결과 · 2026-10-24

**최종 판정: 관측 빌드의 변경 범위 QA 완료, DQA3-003 CLOSED.** candidate02의1280 관측과 candidate03의640 관측·대사·native 재시작을 채택하고, 제품 회복 수정은 candidate05의 두 해상도 실제 콤보로 확인했다.19/20 자동 Idle 관측 공백도 해소했다. OS 실입력은 사용자의 “실입력 검사는 나중에 진행” 지시에 따라 보류하며, 정상속도 영상 재생·모바일·일반 배포 패키지 검증까지 완료했다는 판정은 아니다.

## 후보와 실행 근거

앱0.9.2 / 데이터mvp1.15.0 / Windows D3D12 관측 빌드이며 실제 실행 경로는 `Builds/Dragon-v3-QA/LastBellMvp.exe`다. 후보별 빌드 전체 해시는 각 `candidate-manifest.json`에 보존했다. candidate02→03은 QA 소스만 바뀌었고 제품 통합15개 파일 해시는 같았다. [03 실행 당시 통합 명세](../Evidence/Dragon-v3/dragon-v3-candidate-03/review/integration-at-launch.json)는 수정 전 소스 인계의 해시와 일치한다. 일반 배포 빌드·ZIP은 총괄 소유이며 이 관측 결과만으로 패키지 검증을 주장하지 않는다.

candidate05 Runtime SHA256은 `ce7474e2d8605a370ac062a8da9a060e3eedf47280eeb657b5fd1d891defa68f`다.03 이후 제품15개 중 `BattlePresentation.cs`만 승인된 회복 수정 해시 `e7ad0f8a2e6ad2074c1096c683b05778a5f1117bc9e9c90627b800de0cece9ec`로 바뀌었고 나머지14개는 일치한다. [제품 동일성 대조](../Evidence/Dragon-v3/dragon-v3-candidate-05/review/product-identity.json). QA 강제 샘플 제거 뒤 소스3개 동결 해시도 유지했다.

| 후보 / 프로세스 | 단언 PASS / FAIL | native exit | 결과 |
| --- | --- | --- | --- |
| 01 · 1280 전체 시작 | 21 / 1 | 1 | QA가 미리보기 버튼을 출전 잠금으로 잘못 검사. 조기 중단, 이후3프로세스 미실행 |
| 02 · 1280 전체 관측 | 1,232 / 1 | 1 | 모션 수집 완료 뒤 대사 파일 복원 직후 렌더 대기 누락으로 QA 중단. 이후3프로세스 미실행 |
| 03 · 1280 대사·저장 준비 | 35 / 0 | 0 | PASS |
| 03 · 1280 동일 경로 재시작 | 16 / 0 | 0 | PASS |
| 03 · 640 전체 관측 | 1,245 / 0 | 0 | PASS, 자동 단언 범위 |
| 03 · 640 동일 경로 재시작 | 16 / 0 | 0 | PASS |
| 04 · 1280 표적 재검사 시작 | 81 / 1 | 1 | QA 강제 샘플의 첫80ms 슬롯 누락. 실제 회복 검사와640은 미실행 |
| 05 · 1280 회복·자동 Idle·18 회귀 | 168 / 0 | 0 | PASS, 실제 콤보3타·마지막 복귀 포함 |
| 05 · 640 회복·자동 Idle·18 회귀 | 168 / 0 | 0 | PASS, 실제 콤보3타·마지막 복귀 포함 |

candidate03의4프로세스와05의2프로세스는 Console 오류0·timeout 없음이다.05는20:34:10~20:36:02 KST에 실행·종료했고 launcher exit0, scoped 게임·Editor PlayerPrefs 원복 일치 및 합성 구 저장 sentinel 일치를 확인했다. [03 native 검증](../Evidence/Dragon-v3/dragon-v3-candidate-03/review/native-verification.json), [05 native·프레임 분석](../Evidence/Dragon-v3/dragon-v3-candidate-05/review/analysis.json). 후보01·02·04 실패 이력과 원본 결과는 삭제하거나 성공으로 고치지 않았다. QA는 Editor·EditMode·PlayMode를 추가 실행하지 않았다.

## 완료 기준별 현재 판정

| 기준 | 판정 | 근거와 범위 |
| --- | --- | --- |
| V3-01 신규 적10종 | PASS · 관측 범위 | 11~20 stage/enemy/art·명칭과 필수 클립 참조 일치. Idle/Attack/Stun을 확인했고19/20은05에서 전용 자동 Idle2주기와 실제 Attack 후반까지 보완 |
| V3-02 보스15·20 | PASS · 관측 범위 | 체격·장비·포효·전투 차단·카메라 복귀 확인. 기블 실제 콤보 복귀 DQA3-003을05에서 재검사·종결 |
| V3-03 20층 이야기·완주 | PASS | 기블→도토→루카3페이지, 파일 중간 복원, 다음/스킵, 명시적 정산·유물1회, MvpComplete와20층 경계.21층 진입·재보상 없음 |
| V3-04 이름·선택 잠금 | PASS | 루카/다렌/미엘 표시, stable ID 보존. 다렌·미엘 정보 미리보기 허용, 실제 선택 버튼 비활성 및 Session 선택 거부, 지갑 불변 |
| V3-05 신규 시작·재시작 | PASS · 경로 범위 한정 | 새 격리 override의 Prologue→선택→강화→Stage1 튜토리얼, 같은 경로 native 재시작의 stage/phase/run ID·지갑·성장·선택·유물 보존. 기본 namespace는 별도 selector/EditMode 근거 |
| V3-06 사용자 저장 보호 | PASS · 계약 범위 | 실제 사용자 기본 저장 NOT_ACCESSED. 합성 구 저장/backup sentinel 전후 일치. 게임·Editor scoped PlayerPrefs 원복·대조 일치 |
| V3-07 스킬은 기획만 | PASS · 소스/문구 범위 | 전용 스킬 자료의 미구현·비임포트 표기, 루카만 선택 가능. 제안 스킬 ID의 Runtime/생성 데이터 참조 없음, 보호 데이터 도메인 동일 |
| V3-08 후보·증거·종료 | PASS · QA 관측 범위 | 01~05 결과·실패·해시 보존.05 두 프로세스 정상 종료·설정 원복 확인. Editor·일반 배포 패키지 검증은 총괄 소유 |
| V3-09 접지 | PASS · 피벗·관측 범위 | 1~20 ground 정의, 실제 새 적·플레이어·지역 전환의 발 피벗 Y 일치. 의도된 Up/Run 체공 분리.05에서19/20 Idle과 수평 복귀도 확인 |
| V3-10 플레이어 스타일·동작 | PASS · 관측 프레임 범위 | 짙은 외곽, 청록 의상·붉은 스카프·등짐·한손검 유지. Idle/Attack/Run/Up/Back/Down/Hit와 복귀를 실제 캡처로 확인. 정상속도 영상 재생·사용성 실증은 미실시 |
| V3-11 재미·차별성 기획 | PASS · 문서 범위 | 현재 기본 전투와 후속3칸 충전·전용 스킬·30초 예시를 구분한다. 재미 실증·스킬 구현으로 간주하지 않음 |
| V3-12 공격 효과 | PASS · 관측 범위 | 무장 고블린6종의 Metal, 숲송이4종의 Wood 분기와 실제 공격 전후 궤적·입자·지면 충격 확인. 새 VFX 자산이나 규칙 구현을 주장하지 않음 |

## 프레임과 접지 측정

| 관측 | 실제 PNG / 관측 횟수 | 누락 / 중복 / 해시 불일치 | 최대 지상 발 피벗 Y 오차 |
| --- | --- | --- | --- |
| candidate02 · 1280×720 | 5,416 / 51 | 0 / 0 / 0 | 플레이어0px, 적0px |
| candidate03 · 640×360 | 5,539 / 51 | 0 / 0 / 0 | 플레이어0px, 적0px |
| candidate05 · 1280×720 표적 | 607 / 5 | 0 / 0 / 0 | 플레이어0px, 적0px |
| candidate05 · 640×360 표적 | 680 / 5 | 0 / 0 / 0 | 플레이어0px, 적0px |

51회는 고유 시나리오50개다. 적11 공격이 적 검사와 플레이어 Hit 검사에서 각각 기록되므로 이름만 합쳐 관측을50회로 세지 않는다. [1280 분석](../Evidence/Dragon-v3/dragon-v3-candidate-02/review/analysis.json), [640 분석](../Evidence/Dragon-v3/dragon-v3-candidate-03/review/analysis.json).

오차는 매 프레임의 `WorldCamera.WorldToScreenPoint(FeetWorldPosition).y`와 해당 stage 지면 투영 Y 차이다. 발 피벗 좌표이며, 이미지의 가장 아래 불투명 픽셀이나 무기 끝을 자동으로 발로 간주한 값이 아니다. 위 회피는 의도된 루트 상승이므로 지상 오차 집계에서 제외했고 최대 상승은1280에서143.1033px,640에서71.5100px다. Run 원본02/06의2소스픽셀 체공도 유지된다. 지면 계획 Y590과 기본 카메라의 화면 Y≈469.23(720p)을 구분한다. 보스 확대 때 화면 Y가 바뀌므로 고정469줄로 비교하지 않았다.

기존 전체 관측의 각 연속 구간 최대 기록 간격은1280 약59.14ms,640 약41.75ms이며,05 표적은 각각52.04/38.80ms다. 화면 readback과 저장 비용이 있는 관측이며60fps 성능 인증이 아니다. PNG 인코딩은 구간 뒤에 수행했다. 연속 기록과 대표 프레임·전후 이미지·원본 프레임 순서를 검토했으며 정상속도 영상 재생이나 오디오 청취를 완료했다고 쓰지 않는다.

## 결함과 관측 공백

- **DQA3-001, CLOSED:** 미완성 캐릭터 목록도 비활성화돼야 한다는 QA 가정을 수정했다. 모든 패널의 미리보기와 실제 출전 잠금을 나눠 확인했고03의640과02의1280에서 통과했다.
- **DQA3-002, CLOSED:** 대사 복원 직후 `Bind`가 오버레이를 숨긴 상태에서 클릭한 QA 순서 오류다. 렌더 대기 후 실제 화자/본문을 확인하도록 수정했다.03은 `after Bind: active=False → after Rendered: active=True`를 기록했고 대사·정산·재시작을 모두 통과했다.
- **DQA3-003, CLOSED:** 19/20 공격 접촉 다음 렌더의 즉시 홈 복귀를05의 두 해상도·각3타에서 재검사했다. 공격 후반 표시와 점진적 복귀가 유지되고 다음 준비 자세는 복귀 후 나타난다. [종결 보고서와 수정 전후 근거](dragon-v3-DQA3-003-20261024.md).
- **19/20 Idle 공백, CLOSED · Arrival fixture 범위:** 기존 `enemy-*-idle` 구간은 static combat clock의 첫 cue에 따른 준비 자세였다.05의 별도 유효한 Arrival에서는 실제 모션이 계속 Idle이며, 각2.04~2.06초에8프레임이 모두 관측됐다.1280의19/20은98/117장,640은124/124장을 기록했다. 전투 중 Idle 발생 빈도를 시험한 결과로 확대하지 않는다.

Client는 실제 공격→직전 회복→이른 예고 준비 순으로 제품 표시 우선순위를 수정했다. 후보04의 첫 강제 캡처는 프레임1부터 시작해0만 빠졌고 실제 모션은 Attack이었다. LateUpdate 덮어쓰기를 원인으로 보지 않으며04에서는 제품 회복 검사에 도달하지 못했다. [04 실패 분석](../Evidence/Dragon-v3/dragon-v3-candidate-04/failure-triage.json).05에서는 강제 샘플러를 제거하고 실제 combat에서 문지기 Attack0~7, 기블0~5를 모두 기록했다. 인접18의 비 authored 공격 분기도 두 해상도에서 통과했다.

| 해상도 / 적 | 수정 전 접촉 다음 프레임 복귀 폭 | 수정 후 첫 복귀 폭,3타 범위 | 원위치가 처음 기록된 시각, 접촉 이후 |
| --- | --- | --- | --- |
|1280 / 문지기 |296px |13.42~20.12px |282.51~290.69ms |
|1280 / 기블 |211px |14.04~19.54px |292.68~296.85ms |
|640 / 문지기 |148px |1.93~3.67px |283.66~291.14ms |
|640 / 기블 |105.5px |1.18~3.92px |281.96~293.77ms |

위 X 폭은 카메라 흔들림을 제외한 루트 이동이다. 프레임 간격이 달라 첫 복귀 폭의 크기만으로 해상도별 품질을 비교하지 않는다.12회 모두 다음 프레임이 홈 위치가 아니고 복귀는 단조로우며, 기존280ms smooth 복귀 곡선과 관측 좌표의 최대 잔차는0.00045px 미만이다.2700·3450·4200ms 접촉 시각도 유지됐다. 비교 기준은 `combatMs==impact`의 실제 접촉 자세이며 다음 update의 피해 이벤트 프레임과 구분한다. [05 좌표 타임라인](../Evidence/Dragon-v3/dragon-v3-candidate-05/review/recovery-timeline.csv), [직접 이미지 검토 기록](../Evidence/Dragon-v3/dragon-v3-candidate-05/review/direct-visual-review.json).

## 대표 실제 캡처

| 확인 대상 | 파일 |
| --- | --- |
| 숲길 고블린·플레이어 접지 | [1280 Stage11](../Evidence/Dragon-v3/dragon-v3-candidate-02/1280x720/fresh/v3-stage-11-idle.png) |
| 숲송이 대장 체격 | [1280 Stage15](../Evidence/Dragon-v3/dragon-v3-candidate-02/1280x720/fresh/v3-stage-15-idle.png) |
| 기블 일반 전투 구도 | [640 Stage20](../Evidence/Dragon-v3/dragon-v3-candidate-03/640x360/fresh/v3-stage-20-idle.png) — 당시 실제 모션은 공격 준비 자세 |
| 미엘 미리보기·출전 잠금 | [640 선택 화면](../Evidence/Dragon-v3/dragon-v3-candidate-03/640x360/fresh/v3-character-preview-rin.png) |
| 복원된 도토 대사 | [1280 복원 후](../Evidence/Dragon-v3/dragon-v3-candidate-03/1280x720/fresh/v3-story20-restored-page-1.png) |
| 루카의 마지막 대사 | [640 마지막 페이지](../Evidence/Dragon-v3/dragon-v3-candidate-03/640x360/fresh/v3-story20-page-2.png) |
|20층 완주·경계 | [640 승리 화면](../Evidence/Dragon-v3/dragon-v3-candidate-03/640x360/fresh/v3-twenty-complete.png) |
|문지기3타 복귀 수정 | [640 연속 프레임 시트](../Evidence/Dragon-v3/dragon-v3-candidate-05/review/640x360-enemy-19-recovery.png) |
|기블3타 복귀 수정 | [1280 연속 프레임 시트](../Evidence/Dragon-v3/dragon-v3-candidate-05/review/1280x720-enemy-20-recovery.png) |

보스 확대는 얼굴 중심의 기존 클로즈업이므로 하체가 화면 밖으로 나가는 구간과 일반 전투 전신 구도를 구분한다. 확대 중 화면 밖의 발을 직접 보았다고 주장하지 않고 좌표 연결을 대조했다. [총괄 정지 프레임 리뷰](../Production/dragon-v3-root-visual-review.md)도 범위를 구분한 별도 근거다.

## 보존한 이전 검사와 명시적 한계

최종 아트 사전검사는 [source preflight](../Evidence/Dragon-v3/source-preflight-20261024-01/verification.json)의1,650개 확인,301개 신규 프레임 참조를 따른다. 기존38개 actor의 과거 전체 JSON을 QA가 독립 보관하지 않아 역사적 byte 동일성을 별도 증명했다고 쓰지 않는다. 관련 저장 selector는 [EditMode5/5 결과](dragon-v3-save-selector-result-20261024.md)를 채택했고 재실행하지 않았다. 준비 단계 Core55·경로15 검사도 역사적 소스 근거이며 native 단언과 합산하지 않는다.

전용 스킬은 기획만, 플레이 가능한 구간은20층이다. 지면 명세50행 중 실행 표시20행, 신규 전용 장식 완료0행이며 전용 숲 배경은 후속이다. 현재는 기존 초원 배경과 숲 바닥 색조를 사용한다. [기획 범위 검토](../Evidence/Dragon-v3/dragon-v3-candidate-02/review/planning-review.json). 기본 namespace 직접 exe 실행, 실제 사용자 저장 해시 감사, OS 마우스·키보드, 모바일 실기기, 모든 랜덤 패턴의 실제 플레이, 정상속도 영상 청취·재생, 일반 배포 ZIP 검증은 이 QA의 완료 주장에 포함하지 않는다.
