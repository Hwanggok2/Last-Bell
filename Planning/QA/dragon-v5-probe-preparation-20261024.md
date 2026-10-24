# Dragon-v5 관찰 프로브 준비 인계

2026-10-24 · **PROBE_PREPARED / STATIC_DIAGNOSTICS_PASS / QA_NOT_RUN**

QA 소유 코드·도구 동결 시각은 **2026-10-24 14:49:03 UTC / 23:49:03 KST**다. 정확한 파일별 SHA256은 [프로브 소스 manifest](dragon-v5-probe-source-manifest-20261024.json)에 있다. 이 동결은 QA 코드7개 파일에 한정하며 최종 제품 소스·아트·관측 빌드 전체의 동결 선언이 아니다. Root의 최종 Refresh→저장 Apply→EditMode→ObservedBuild와 실행권 인계를 기다린다.

## 변경 파일

| 파일 | 내용 |
| --- | --- |
| [DragonThemeSmokeProbe.V5.cs](../../Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.V5.cs) |Development 전용 v5 관찰 프로브. 공개 Core/UI/ParticleSystem 참조 사용 |
| [V5 meta](../../Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.V5.cs.meta) |신규 소스의 고정 GUID |
| [DragonThemeSmokeProbe.cs](../../Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.cs) |v5/v5-legacy/v5-resume 허용·preflight·dispatch·timeout·cleanup 추가. 기존v4 분기 유지 |
| [run-campaign.ps1](../../Tools/QA/Dragon-v5/run-campaign.ps1) |1280/640 각각fresh+native resume의4프로세스. 아직 실행하지 않음 |
| [common.ps1](../../Tools/QA/Dragon-v5/common.ps1) |최종 동결 소스·Runtime SHA·버전·schema8 QA 원본 checksum과 경로 검증 |
| [analyze.py](../../Tools/QA/Dragon-v5/analyze.py) |캡처/상태/입력/프레임 수·해시 정합성의 읽기 전용 분석. 시각 PASS 생성하지 않음 |
| [README.md](../../Tools/QA/Dragon-v5/README.md) |실행 입력,4프로세스 범위,fixture·수동시계·실기 한계 |

[합격 기준 문서](dragon-v5-acceptance-20261024.md)는 최신 인계를 반영했다. 제품 Core/Presentation/Editor/카탈로그·프리팹·씬은 QA가 수정하지 않았다. 커밋·푸시도 하지 않았다.

## 준비한 관찰 범위

- native schema8 시작과 실제 과거 QA 파일 이전, 별도 합성7/15/20 레거시 중간 대사 파일 복원, 새 계정 prologue.
- 여관5스탯 중 회복 구매,5동료 목록·강화·서비스 확인/취소/구매·예약 환불·편성. 미래 동료는 명시된 rule/UI fixture다.
- Stage4 처치→5 도착·포효 뒤 guided 진입, 오답/비활성 공격/스킬 거절, hold 동결, 서로 다른 Up/Down 합성 press로 완료1회.1280 native restart는 첫 guided 시퀀스로 복귀.
- 일반3·보스10/20의 실제 처치 회복1회, 저장 복원 후 재회복 없음.
-3캐릭터 스킬 contact와 실제 입자, pause 동결, 보호막 흡수와 마지막180ms 자연 만료, 미엘 준비→실제 기절15입자→종료0. 기존 View.StunStars 중복 비활성.
- 동료5종의 무플레이어입력 contact3회, combat clock 기준 주기·pause 동결, 각 contact의 연속 렌더와 실제 저장 배우.
- 현재2/12/10/20 대화의3캐릭터별 표시 페이지·초상 좌우, legacy 대사 종료까지 캡처.
-640 native restart는 같은 적의 남은 동료 시간·windup을 보존하고 현재 보호막을 렌더하며 과거 burst를 재생하지 않는다. **350ms grace는 새 적에 적용**하는 최종 Client 계약으로 수정했다.

`v5-state.jsonl`은 실제4개 ParticleSystem의 입자수/좌표/크기/색/수명, Point RT와 RawImage, 스킬·보호막·stagger·기절, 동료·위험 큐/impact 시각, 배우 root/발, 대화 bounds/font를 기록한다. `v5-input.jsonl`은 합성 UI 입력 전후 clock/event 수를, `v5-evidence.json`은 권위 이벤트와 미검증 항목을 기록한다. 기존 buffered collector의 프레임 파일명은 `v3-frame-*`지만 v3/v4 캠페인을 실행하는 것은 아니다.

## 실행 전 확인 결과와 한계

2026-10-24 준비 시점에 다음 정적 검사만 수행했다.

| 검사 | 결과 |
| --- | --- |
| 수정 entry+V5 partial C#9 구문 |오류0 |
|현재 Runtime74개 소스의 Roslyn 구문·타입 진단 |오류0. DEVELOPMENT_BUILD/UNITY_STANDALONE_WIN/LASTBELL_PLAYTEST_092 조건과 로컬 Unity 참조 사용. **DLL emit 없음, Unity 시작 없음, Core/QA 테스트 실행 없음** |
| PowerShell2개 스크립트 AST |구문 오류0 |
| Python 분석기 AST |구문 오류0. 실제 candidate 분석 실행 없음 |
| 공백·로컬 문서 링크/ID |별도 정적 점검. 기존 v4 분기를 제거하지 않음 |

Client의 Core189개 assertion·Python28개 테스트 및 Core/Data/Persistence 컴파일 통과는 [담당자 인계](../Production/dragon-v094-client-handoff.md)로 수신했다. QA가 재실행한 결과로 쓰지 않는다. Root의14:38:50Z 저장 Apply 완료·오류없음과 동료 Run5종40프레임/총98프레임 재import 완료도 인계 내용이며 QA 렌더 검증과 구분한다.

자동 결과는 대표 변경 범위의 관측 PASS일 뿐60개 합격 기준 전체의 PASS가 아니다. 모든 후퇴 archetype, 정확한 대화 overflow/타이핑·held/prebuffer, 전체 거래/마이그레이션 경계, native 기절 복원, RT 수명/누수, 음성·물리입력·모바일·정상속도 성능은 여전히 별도 검증이다. PNG 가독성/가림/팔레트는 수동 검토 전 `NOT_RUN`이다. 보호막의 전체3초 규칙은 Client 증거를 재사용하고, 프로브는 마지막180ms 만료 fixture임을 명시한다. 긴 동료 타이머 사이에는50ms 수동 Core tick을 쓰고 contact 주변만 실제 렌더로 수집한다.

## 최종 빌드 이후 인계할 실행 입력

`Builds/Dragon-v5-QA` 안의 exe, Runtime SHA256, 최종 동결 manifest, Core evidence, saved Apply/integration evidence, `Planning/Evidence/Dragon-v4` 안의 schema8 QA 저장을 전달하면 runner가 검증한다. 최종 동결 manifest 형식은 README에 있다. 이 문서의 **probe 전용 manifest에는 Runtime SHA와 전체 제품 파일이 없으므로 그대로 캠페인 입력으로 사용할 수 없다**. 최종 manifest에는 QA 소스 해시와 generated catalog를 포함해야 한다.

출력은 `Planning/Evidence/Dragon-v5/<새 candidate>/`이며 아직 캠페인 결과를 생성하지 않았다. 기본 사용자 프로필은 **NOT_ACCESSED**. 실행 도구는 prior-QA source만 복사·해시하고, strict-child 저장 경로·링크 거절·기존 evidence 보존·game/Editor scoped prefs 복원·합성 sentinel 검사를 포함한다. `-Run`을 전달하지 않으면 시작하지 않는다. 첫 실패 뒤 나머지 프로세스는 중지하며 자동 재실행은 없다.
