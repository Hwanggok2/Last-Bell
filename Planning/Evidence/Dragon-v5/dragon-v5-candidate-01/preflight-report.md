# Dragon-v5 QA 결과 · candidate01

2026-10-24 KST · **PREFLIGHT_BLOCKED / PRODUCT_QA_NOT_RUN / VISUAL_NOT_RUN**

승인된 전용 runner를1회 호출했으나, 게임을 시작하기 전 scoped PlayerPrefs 백업 단계에서 Windows 레지스트리 접근이 거부되어 중단됐다. **네이티브 게임 프로세스0/4, 제품/프로브 실행 검사0, PNG0장**이다. 나머지 프로세스와 자동 재실행은 시작하지 않았다. 현재 결과는 제품 FAIL도 제품 PASS도 아니며 실행 환경의 권한 문제다.

## 관측 빌드와 실행 입력

| 항목 | 값 |
| --- | --- |
| Candidate |`dragon-v5-candidate-01` |
| 실행 파일 |`Builds/Dragon-v5-QA/LastBellMvp.exe` |
| 버전 / 데이터 / 저장 |0.9.4 / mvp1.17.0 / schema9 |
| Runtime SHA256 |`4F4C66BCF7B6431E6E28DE6080B92527657BFAAA589026449964F0A9BDCE56C9` |
| Freeze |[인계된5881파일 manifest](../Production/dragon-v5-observed-build.json) |
| Core evidence |[Client 인계](../Production/dragon-v094-client-handoff.md): Core217 assertion, data28, Unity EditMode198/198. QA가 재실행하지 않음 |
| Integration evidence |[saved Apply/integration](../Production/dragon-v5-integration.json) |
| Legacy source |v4-candidate03/1280x720/fresh/profiles/boot의 schema8 QA 저장. 실제 사용자 저장 아님 |
| Legacy SHA256 |`517FAA3D7E20E5D4D1329C0EE95FB2F7E9CAB315CC624B079162BA842BA3C059` |

실행한 명령은 [전용 runner](../../Tools/QA/Dragon-v5/run-campaign.ps1)에 인계된 absolute exe/manifest/Core/integration/legacy 경로, 위 Runtime SHA, candidate ID와 `-Run`을 전달한 것이다. Runtime 해시·동결파일·legacy 입력 검증을 통과한 뒤 candidate와 합성 sentinel이 생성됐으며, 첫 registry 읽기에서 종료코드1이 반환됐다.

## DQA5-001 · 실행 환경의 scoped registry 읽기 거부

- 단계: `HKCU/Software/LastBellPrototype/Last Bell MVP`의 `OpenSubKey`를 이용한 읽기 전용 백업 시작.
- 오류: `Exception calling "OpenSubKey" with "1" argument(s): "Requested registry access is not allowed."`
- 분류: **ENVIRONMENT_REGISTRY_ACCESS / OPEN**. Windows/실행 컨텍스트의 접근 거부다. 자동 승인 검토의 거절 메시지는 수신하지 않았다.
- 영향: `playerprefs-before.json`, native process 목록/로그/result, PNG가 생성되기 전 종료. 제품 행동 및 C# probe 단언까지 도달하지 않았다.
- 조치: 실패 기록과 입력 사본을 보존했다. 코드 동결·레지스트리 설정을 바꾸지 않았고, 백업을 생략해 게임을 실행하지 않았다. 같은 candidate를 덮어쓰거나 전체 캠페인을 자동 재시도하지 않았다.
- 후속 조건: 해당 scoped PlayerPrefs 읽기/복원이 가능한 실행 컨텍스트를 확보하고 Root가 재개 범위를 인계해야 한다. 기존 candidate는 보존한다.

보존한 [preflight-failure.json](../Evidence/Dragon-v5/dragon-v5-candidate-01/preflight-failure.json), [preflight-integrity.json](../Evidence/Dragon-v5/dragon-v5-candidate-01/preflight-integrity.json), [원본 sentinel 기록](../Evidence/Dragon-v5/dragon-v5-candidate-01/protected-saves-before.json)을 기준으로 실패 단계를 확인할 수 있다. 동일 폴더의 freeze/core/integration 사본은 이 시점 인계를 보존한다.

## 실제 확인 범위

| 범위 | 판정 |
| --- | --- |
| 관측 Runtime SHA / source freeze / prior-QA envelope |runner 사전 검증 통과. native launch는 아직 아님 |
| scoped PlayerPrefs 백업 |접근 거부로 미완료 |
| PlayerPrefs 변경·복원 |변경 단계 미진입. 복원 PASS로 표시하지 않음 |
| 합성 sentinel |전후 SHA 일치 |
| 실제 사용자 기본 프로필 |**NOT_ACCESSED**. 읽기·복사·해시·변경 없음 |
|1280 fresh / resume |**NOT_RUN** |
|640 fresh / resume |**NOT_RUN** |
| 실제 효과·대화·여관 PNG |**0장 / NOT_RUN**. 대표 캡처를 제공할 실행 증거가 아직 없음 |
| 화면 수동 검수 |**NOT_RUN** |
| 모바일 / OS 실입력 / 직접 청취 |요청대로 보류 |

[프로브 준비 결과](dragon-v5-probe-preparation-20261024.md)의 정적 구문·타입 진단 통과와 [60개 합격 기준](dragon-v5-acceptance-20261024.md)은 실행 완료 증거가 아니다. 이번에는 스킬/보호막/기절 ParticleSystem, 대화 좌우 배치, 여관 거래/편성, guided 입력, 회복, 동료 주기·재시작에 대해 새 native PASS를 선언하지 않는다.
