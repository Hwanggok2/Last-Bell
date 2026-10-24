# Dragon v1 QA 결과 — 2026-10-24

**요청된 자동 검증과 대표 시각 검토 범위는 PASS다. 확인된 유물 카드 가림·제목 겹침은 수정 후1280/640 표적 검사에서 모두 해결됐다. 일반 Windows 빌드와 ZIP 패키지 검증도 완료됐다.** OS 마우스·키보드 실입력, 모바일 검증, 일반 빌드의 별도 실행은 수행하지 않았다. 실행 검증 결과는 아래 명시한 관측 빌드·자동 입력·대표 화면 범위에 한정한다.

## 후보와 수행 범위

대상은 `0.9.0` / `mvp1.14.0`의 Stage1~10 Dragon 테마다. 기존20층, schema8, 안정된 저장 ID, 판정·경제·성장·유물 정산 규칙을 유지한다. 조작은 사용자 명시 선택인 **위·뒤 회피, 아래 쳐내기, Space 공격**이다.

- [candidate-01 전체 자동 캠페인](../Evidence/Dragon-v1/candidate-01/campaign.json): 1280×720의 시작→튜토리얼→1~10→3페이지 대사→11→정산/유물→강화→11 재출전, 960×540·640×360 UI 표본, 별도 프로세스 저장 복원. 네 실행 모두 자체 assertion PASS, Unity error0, native exit0.
- [candidate-02 표적 캠페인](../Evidence/Dragon-v1/candidate-02-targeted/campaign.json): 유물 카드 가림 수정 후1280/640의 선택 전·후·확정 및 첫 보스 대사0.35초 대기. 두 실행 모두33 assertions PASS, Unity error0, native exit0.
- [candidate-03 카드 표적 검사](../Evidence/Dragon-v1/candidate-03-cards/campaign.json): 제목 배치 수정 후1280/640의 선택 전·후·확정만 재검사. 두 실행 모두32 assertions PASS, Unity error0, native exit0. 기존 테스트·대사·모션·전체 캠페인은 반복하지 않았다.
- Unity 테스트는 인계받은 기존 전용8093 Editor의 빈 clean 씬에서 한 번씩 수행했다. 테스트용 프로젝트 복사본4650파일은 해시 대조만 했고 실행하지 않았다. 별도 Editor/프로젝트 전환/실제 게임 씬 Play는 하지 않았다.
- 실패 수정 후에는 해당 테스트와 해당 화면만 다시 검사했다. 전체 캠페인·전체 테스트를 반복하지 않았다.

## 자동 검증 결과

| 항목 | 결과와 근거 |
| --- | --- |
| Core·저장 | [EditMode186건 실행](../Evidence/Dragon-v1/candidate-01/unity-tests/editmode-result.json). 구 아트 ID/대사 기대값2건 실패를 수정한 뒤 [2/2 PASS, 실패·스킵0](../Evidence/Dragon-v1/candidate-02-targeted/unity-tests/editmode-result.json) |
| Presenter·새 HP 경계 | [PlayMode26건 실행](../Evidence/Dragon-v1/candidate-01/unity-tests/playmode-result.json). 구 아트/모션/문구 기대값6건을 갱신한 뒤 [6/6 PASS, 실패·스킵0](../Evidence/Dragon-v1/candidate-02-targeted/unity-tests/playmode-result.json). 최초 실행의 새 HP4경계는 실패 목록에 없음 |
| 진행·보상 | 1~10 방문, 10보스3페이지 토리→토리→온, 11 체크포인트, 보스 보석4/중간0, 정산 큐1회 소비, 명시적 유물1개 확정 통과 |
| 성장·유물 | 실제 +1 견적과 골드 차감/레벨 증가 일치, MAX 견적 표시, 보유 유물 자동 적용, 강화 후11 재출전 통과. 유물 전체 조합을 새 probe에 복제하지 않음 |
| 저장 재실행 | 격리 저장의 stage/phase/골드/보석/공격 레벨/유물 수가 별도 프로세스 bootstrap에서 보존되고 pause→resume 통과 |
| 입력 배치 | 세 해상도에서 실측 정규화 중심 오차1.01px 이내, 세 hitbox가 안전영역 안, Forward 비활성 확인. UI raycast와 synthetic pointer event 근거이며 실제 OS 입력 근거 아님 |
| 모션·배경 | 실제 프레임 CSV에서 대표5몸체의 준비/공격, 온의 Idle/Hit/Attack/방어 모션, 세 전환의 Run0~7 기록. 1→2/4→5/9→10에서 원경/지면 변위비 모두0.08. [분석 JSON](../Evidence/Dragon-v1/candidate-01/analysis.json) |
| 보스 대사 flash | 첫 페이지와 전투시계200ms를 유지한 실제 시간 약410ms 동안 enemyFlash1→0. [대기 후 캡처](../Evidence/Dragon-v1/candidate-02-targeted/review-1280x720/review-story-after-035s.png), [프레임 기록](../Evidence/Dragon-v1/candidate-02-targeted/review-1280x720/timeline.csv). 대화 중 흰색 고정 결함 아님 |

초기 MCP 전체 테스트 응답은 완료 progress와 실패 목록을 반환했으나 `result` payload가 null이었다. 따라서 초기 suite 전체를 한 번에 PASS라고 고쳐 쓰지 않는다. 최초 실패8건의 근거와 변경 전 상태는 보존했고, 표적 재실행은 정식 summary의 passed/failed/skipped 값까지 확인했다. [정확한 재검사 목록](../Evidence/Dragon-v1/candidate-01/unity-tests/target-filters.json).

## 시각 검토와 수정 이력

| 항목 | 결과 |
| --- | --- |
| 대표 몸체·배치 | 1슬라임/3뿔토끼/5큰뿔토끼/6산멧돼지/10큰엄니가 구별되고 온을 향함. 발이 지면에 놓이며 HP/이름은 하단 홈 위치 기준으로 유지. 도트 외곽선과 큰 보스 체급 확인 |
| UI·문구 | 640 선택/강화/MAX/보관함/일시정지,1280 의뢰/정산/대사의 주요 문구를 직접 확인. 자동 bounds 경고인 선택 화면 “이야기” Text rect는 글자 자체가 화면 안에 있어 잘림 결함으로 판정하지 않음 |
| DQA-001 카드 가림 | 초기 불투명 버튼 면이 아이콘·이름을 덮음. UI 담당이 Receive를 먼저 그리도록 순서를 수정하고 저장 참조 검증 추가. candidate-02에서1280/640 모두 해결 확인 |
| DQA-002 제목·테두리 겹침 | 가림 수정 후 긴 이름이 좁은 카드 테두리와 겹치는 것을 확인. 카드 높이·제목 폭/줄바꿈·설명 위치를 최소 수정하고 candidate-03에서1280/640 모두 해결 확인 |
| 초기 보스 흰색 | 치사 직후의 정상120ms 피격 효과. 같은 첫 페이지를0.35초 이상 유지한 별도 표본에서 소멸 확인. 제품 수정하지 않음 |
| 기존 테스트8건 | 현재 art ID/대사/새native 모션 기준으로 fixture와 기대값만 갱신. legacy 적의 Idle·일시정지, 접촉 프레임, 완벽 쳐내기 HP/홈 좌표, skip/큐/체크포인트 검증을 유지. 제품 규칙 변경 없음 |
| QA PNG 번호 | 동일 모션의 두 Observe 구간이 번호를0부터 다시 써 초기 PNG 일부를 덮는 probe 오류 발견. CSV는 연속 기록됨. 실행 전체 고유 번호로 수정했으나 변경 후 전체 모션 촬영은 반복하지 않음. 초기 PNG 파일명 순서를 무손실 연속 영상으로 사용하지 않음 |

초기 상세 관찰과 실패 원인은 [candidate-01 기록](dragon-v1-candidate-01-review.md)에 보존한다.

## 저장·설정·Editor 반환

플레이어의 모든 실행은 출력 디렉터리 하위의 명시적 `-lastbell-save-dir`만 사용했다. 기본 사용자 저장을 읽거나 복사하지 않았다. 결과 폴더를 새로 만들고 기존 증거를 덮어쓰지 않았다. QA 도구는 `UNITY_EDITOR || DEVELOPMENT_BUILD` 조건부이며 명시적 CLI가 있을 때만 설치된다. 일반 전달 빌드에는 제외된다.

- [candidate-01 설정 복구](../Evidence/Dragon-v1/candidate-01/playerprefs-restored.json), [candidate-02 설정 복구](../Evidence/Dragon-v1/candidate-02-targeted/playerprefs-restored.json): 게임/Editor의 LastBell·화면/모니터 값 두 경로 일치.
- [candidate-03 설정 복구](../Evidence/Dragon-v1/candidate-03-cards/playerprefs-restored.json): 마지막 두 카드 실행 후에도 동일하게 복구됐다. 이 실행은 standalone만 사용하여 반환한 Editor를 다시 사용하지 않았다.
- [표적 테스트 설정 복구](../Evidence/Dragon-v1/candidate-02-targeted/unity-tests/editor-playerprefs-restored.json), [Editor 반환](../Evidence/Dragon-v1/candidate-02-targeted/unity-tests/editor-return.json): 빈 clean 씬, isDirty=false. 테스트 후 Editor/MCP를 총괄에 반환했다.
- 이번 native exit0 결과로 과거 간헐적 QA-003 종료 문제의 근본 해결을 선언하지 않는다. 엔진 업그레이드는 수행하지 않았다.

## 미검증·명시 제외

- **OS 마우스·키보드 실입력:** 사용자의 “실입력 검사는 나중에 진행” 지시로 보류. 직접 이벤트 검사를 실입력 PASS로 바꾸지 않는다.
- **모바일 실기기·성능·노치/비대칭 Safe Area·와이드 전 해상도:** 이번 대표3해상도 검증으로 보증하지 않는다. 계측 캡처의 frame delta도 성능 인증에 사용하지 않는다.
- **모든 유물·저장 오류 조합 및 모든 legacy 상태:** 기존 Core/저장 테스트 범위와 대표 별도 프로세스 resume만 근거로 삼는다.
- **전 구간 무손실 모션 영상·청음:** 초기 모션 PNG 덮어쓰기 한계를 명시한다. CSV/남은 프레임/기존 Presenter 음향 검사를 영상·청음 인증으로 확대하지 않는다.
- **18초 의뢰 전체 자동 스크롤·수동읽기 전 흐름:** 첫 캡처와 skip을 확인했으며 전 재생을 증명하지 않는다.
- **새11~50 지역·동료/직업·공개 배포:** 이번 제작/QA 범위 밖. 기존11~20은 유지되며11 경계를 검증했다.
- **일반 최종 빌드의 별도 실행:** BuildOptions.None 빌드와 ZIP 패키지 검증은 완료했지만 별도 실행은 하지 않았다. 관측 Development 빌드의 실행 결과를 일반 빌드 실행 결과로 표기하지 않는다.

## 일반 Windows 전달물

총괄이 **2026-10-24 16:23:55 KST** (`07:23:55Z`)에 `BuildOptions.None` 빌드를 완료했다. [전체165파일 해시·배포 기록](../Production/dragon-v1-release.json)을 확인했으며, QA가 실제 ZIP과 Runtime DLL의 SHA256을 다시 계산해 기록과 일치함을 확인했다.

| 항목 | 결과 |
| --- | --- |
| 실행 파일 | [Builds/Dragon-v1/LastBellMvp.exe](../../Builds/Dragon-v1/LastBellMvp.exe) |
| 전달 ZIP | [Dragon-v1-Windows-0.9.0.zip](../../Builds/Dragon-v1-Windows-0.9.0.zip), 46,152,662bytes |
| ZIP 무결성 | 총괄의 CRC·엔트리수165·파일 크기 대조 PASS |
| ZIP SHA256 | `1f382ca4717789af67861de1e7a5254f12aaacf9f1276a6f9037c75f49b6e742` |
| LastBell.Runtime.dll SHA256 | `f4d4dfbd5f90619ae4dfee86249f3139e787d77d820af0114657c8ebfeca038b` |
| QA 관측기 | 총괄이 일반 DLL에 `DragonThemeSmokeProbe` 심볼 미포함 확인, 배포 manifest의 `dragonQaProbeExcluded=true` |
| 아트 라이선스 | `LastBellMvp_Data/StreamingAssets/ArtLicenses/PixelLab-Dragon-v1.txt` 포함, manifest에 경로·크기·해시 기록 |
| 일반 빌드 실행 | **NOT_RUN**. 실행·resume·exit 근거는 Development 관측 후보8회로 한정 |

## 대표 실행 캡처

- [큰엄니와 HP 배치](../Evidence/Dragon-v1/candidate-01/full-1280x720/body-stage-10.png)
- [초원 달리기](../Evidence/Dragon-v1/candidate-01/full-1280x720/travel-from-4-020.png)
- [640 강화 화면](../Evidence/Dragon-v1/candidate-01/layout-640x360/layout-upgrade.png)
- [첫 보스 대사 대기 후](../Evidence/Dragon-v1/candidate-02-targeted/review-1280x720/review-story-after-035s.png)
- [최종1280 유물 선택 전](../Evidence/Dragon-v1/candidate-03-cards/cards-1280x720/review-relic-choice.png)
- [최종640 유물 선택 후](../Evidence/Dragon-v1/candidate-03-cards/cards-640x360/review-relic-selected.png)
- [최종640 유물 확정 후](../Evidence/Dragon-v1/candidate-03-cards/cards-640x360/review-result.png)

## 마지막 카드 배치 표적 결과

**PASS.** 총괄이 인계한2026-10-24T07:21:52Z 관측 빌드에 대해 `cards` 모드1280/640을 각각1회 수행했다. 세 후보의 아이콘과 두 줄 이름이 카드 면 안에 있고 테두리와 겹치지 않는다. 선택 전 안내, 선택 후 효과3줄, 선택 체크, 활성화된 확정 버튼이 서로 겹치지 않는다. 실제 UI raycast 기반 선택·확정이 수락됐고 유물1개/큐0/DeathResult 전환을 확인했다. 각각5PNG·32 assertions·오류0·exit0이며 설정 복구 두 경로가 일치한다.

관측 빌드에서 발견된 제품 시각 결함은 모두 닫았고, 일반 최종 빌드·ZIP 검증 및 해시 기록까지 완료했다. [기계 판독 요약](../Evidence/Dragon-v1/verification.json).
