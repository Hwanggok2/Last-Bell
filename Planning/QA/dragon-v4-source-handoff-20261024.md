# Dragon-v4 QA 소스 인계 · 2026-10-24

**최종 실행 완료:** candidate03의1280 대시 표적/1280 resume/640 fresh/resume4프로세스 모두 exit0,1,037 checks PASS, Console0. [종합 QA 결과](dragon-v4-result-20261024.md). 최종 관측 Runtime은 `BA7F4144C9936FE5306231BC298BBB3FEE13FBF91446C8FBC356C94AD537A234`다. 아래는 수정·소스 동결·빌드 대기의 이력이며 추가 실행 지시가 아니다.

**실행 직전 계측 보정 동결:** root 지적에 따라 렌더 프레임 접촉시계 exact/최대전진2px 단언을 제거하고 실제 접촉 전후 framegap 안의 시각오차와 충분한 전진을 검사한다. 정확한 접촉 이벤트시계 검사는 유지한다. 최종 V4 C# SHA256은 `D2D1C25FBFA71EB05F53D40FD97ABEF02033F498B734ED64E5B4FB63D3CCACF7`; 진입·runner·analysis는4차 동결과 동일하다. [5차 동결](../Evidence/Dragon-v4/source-fix-20261024-05/verification.json). 아직 새 native 실행 없음.

**최종 소스 갱신: DQA4-003 Stage11 제품 전진 누락의 표적 계측까지 동결.** 이전900 checks에는 실제 전진거리 조건이 없었으므로 candidate02 Stage11 대시 시각 PASS는 채택하지 않는다. `v4-dash` 모드와 첫/반복 대시의 실제 root전진·접촉위치·복귀·발Y 검사를 추가했다. V4 SHA256 `19EB04E32B402C606B2EEB61E5707D578AA128ACA5D46D22A00ACC0115E630B8`, 진입 파일 `ECFF2C16875B7A2E8C8DE71249C9A7A94E5F8677061B8C87639796A9AE478B0F`. AudioSource guard도 포함한다. Client 제품 수정 통합 후 root 최종 관측빌드 대기다.

최종 `-Resume1280From candidate02` 실행 scope는 **1280 Stage11 표적 →1280 resume →640 fresh →640 resume,4프로세스**다. 첫 표적은 `1280x720/dash` 별도 저장, resume은 candidate02 checksum 검증 저장 복사본을 사용한다. 다른1280 기능·전체75대사는 재실행하지 않는다. [DQA4-003](dragon-v4-DQA4-003-20261024.md), [4차 소스동결](../Evidence/Dragon-v4/source-fix-20261024-04/verification.json). 아래3프로세스 문구는 Stage11 결함 발견 전 인계 이력이다.

**최신 갱신: candidate02 완료900 checks PASS, QA audio tap Console 오류1건만 분리.** `DragonThemeSmokeProbe.V4.cs`는 AudioSource가 정확히1개일 때만 optional tap을 부착하도록 수정했다. 여러 소스이면 `PCM_CAPTURE_UNAVAILABLE / MULTIPLE_AUDIO_SOURCES`, 소스 수, 실제 attack/contact 이벤트와 mute 상태를 기록한다. 제품 AudioSource 배치·동작은 변경하지 않았다. V4 C# SHA256 `B174B11EC7D4A0E82BAFF1C8438794F0A5109EC54C2CDF4DAB095082871ECCBA`. 재컴파일·Unity·재실행은 root 소유다.

새 runner 옵션 `-Resume1280From 'C:/Github/Test2/Planning/Evidence/Dragon-v4/dragon-v4-candidate-02'`은 완료/전체PASS/단일 QA tap 오류, catalog/art 동일성, 저장 checksum을 확인하고 저장·기대값을 새 candidate로 복사한다. **1280 resume →640 fresh →640 resume, 총3프로세스만 실행**한다.1280 기능·대사와 Core는 반복하지 않는다. candidate02의 기존 passed=false/Console 오류/PCM은 원본 그대로 보존하며, 그 PCM으로 실제 Sfx·mute·청음 합격을 판정하지 않는다. [DQA4-002](dragon-v4-DQA4-002-20261024.md).

**최신 갱신: candidate01 QA double 비교 실패 후 probe-only 수정 동결.** 새 모드 `v4-tail`, runner 옵션 `-Continue1280From`. [2차 동결·해시](../Evidence/Dragon-v4/source-fix-20261024-02/verification.json)와 [DQA4-001](dragon-v4-DQA4-001-20261024.md)을 따른다.1280 다렌 복원/방패→미실시 tail,1280 resume,640 fresh/resume만 준비했다. 이미 통과한 루카·on/dam 선택 구간은 반복하지 않는다. 새 컴파일·빌드·실행은 root 인계 대기다. 아래는 최초 작성·컴파일 인계 기록이다.

**SOURCE_FROZEN_READY_FOR_ROOT_REFRESH_APPLY_OBSERVED_BUILD.** 새 V4 probe·runner·analysis 작성과 Player defines 독립 Runtime 컴파일1회 완료. 오류0, 기존 JSON DTO 경고27건. PowerShell3파일과 Python AST 구문 검사 PASS. Unity·Apply·관측 빌드·native QA·Core 재실행은 하지 않았다.

[소스 해시·컴파일 동결 기록](../Evidence/Dragon-v4/source-freeze-20261024-01/verification.json), [도구와 실행 명령](../../Tools/QA/Dragon-v4/README.md), [완료 기준](dragon-v4-acceptance-20261024.md).

| 변경 | 범위 |
| --- | --- |
| `DragonThemeSmokeProbe.V4.cs` + meta | 새 V4 시나리오·상태/이벤트 기록·선택적 오디오 소스 필터 PCM |
| `DragonThemeSmokeProbe.cs` | v4/v4-resume 모드 허용·분기·재시작 저장 검사·선택 인물 아트·타임아웃·V4 종료 정리만 추가 |
| `Tools/QA/Dragon-v4` | 새 실행기·입력 검증·분석·소스 컴파일 도구·README |

V3 본체 SHA256은 `9737f351ac7e0e32492211760629fc1f4b00d86c1f93e7f989db49148a2eb671`로 유지했다. V4가 기존 연속 프레임 수집기를 호출하지만 V3 캠페인은 실행하지 않는다. 새 V4 소스 SHA256은 `025b741e94d3f5f0f0cc8b798595ed1a2f7149074149a8379073ccee89d96150`, entry는 `8b6d8777e6040373e60e205fdcab228e795a6a1f080f6a64678084199b6902aa`다.

관측 준비 범위는 세 캐릭터 실제 선택·출전·스킬 접촉/쿨/파일복원, 다렌 방패 표식 활성·흡수·만료, 예고·기절바, Stage11 실제 정면 대시와 다음 반복 공격, 공유 공격 분기의19/20 회복, 세 인물×5장면의 전체25페이지·소개·초상화·다음/스킵·중간 복원이다. 두 해상도마다 fresh와 같은 경로 native resume, 총4프로세스가 한 batch다. 재시작 checkpoint는1280 다렌·640 미엘의20층 대사 중간이며 쿨·화자·초상화·진행을 함께 대조한다.

Stage11의 승인된 `frontal_dash`/단일 pool 교체는 허용하며 기존 timing·HP·damage·sequence weight는 대조한다.19/20 stage 데이터와 기존 combat 설정은0.9.2 최종 관측 카탈로그와 대조한다. 기절바 본체408×7.2와 새 외곽선을 구분하며 예고 collision 영역 확대를 요구하지 않는다. `???`는 장면별 portrait ID를 따르고 UI는 해석된 `session.Story`와 비교한다.

Core246 assertions PASS는 [Client 결과](../Production/DragonV093ClientValidation/core-tests.txt)를 재사용한다. probe의 마지막1초 쿨/마지막100ms 방패 만료 fixture는 UI 경계 확인이며 전체20초/3초 계약을 새로 입증했다는 의미가 아니다. root의 데이터 검사26 PASS도 재실행하지 않았다.

오디오는 실제 합성 pointer→정상 공격 경로의 accepted attack/contact 이벤트와 mute/source 상태를 기록하고, 가능하면 Sfx에 연결한 QA 필터에서 PCM을 남긴다. 현재 제품 API에는 정확한 `PlayOneShot` voice 수를 직접 읽는 기능이 없다. 따라서 이벤트·clip 참조·PCM의 파형 수치로 직접 청취나 무중첩을 합격 처리하지 않는다. **직접 청취 NOT_RUN, 정확한 중복 voice 검증 NOT_CERTIFIED**를 유지하고 후속 결과에서 관측 가능한 범위만 보고한다.

root가 제공할 항목은 최종 `Builds/Dragon-v4-QA/LastBellMvp.exe`, Runtime DLL SHA256, `Client/Logs/dragon-v4-operation.json`, 실행권이다. 실행기는 이 해시와 generated mvp1.16.0을 확인하고 기존 candidate를 덮어쓰지 않는다. 사용자 기본 저장은 접근하지 않으며 CLI 격리 override만 사용한다. OS 실입력·모바일 보류는 유지한다. **이 문서는 실행 준비 완료 기록이며 제품 QA 합격 기록이 아니다.**
