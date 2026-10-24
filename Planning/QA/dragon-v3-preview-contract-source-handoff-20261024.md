# Dragon-v3 미리보기 계약 QA 수정 인계 · 2026-10-24

**SOURCE_FROZEN_WAITING_FOR_OBSERVED_REBUILD.** 총괄이 원인 확인 후 QA 수정 보류를 해제하고 요청한 범위만 반영했다. Unity/Editor·게임을 실행하지 않았으며, 총괄의 관측 빌드 재생성과 별도 실행권 인계 후 `dragon-v3-candidate-02`에서 기존 4프로세스 계획을 재개한다. 실제 OS 입력은 사용자의 “실입력 검사는 나중에 진행” 지시에 따라 계속 보류한다.

후보01은 미완성 캐릭터의 목록 패널도 비활성화되어야 한다는 QA 가정 때문에 첫 1280×720 프로세스에서 종료됐다. 실제 계약은 모든 목록 패널에서 정보를 미리볼 수 있고, 다렌·미엘을 미리보는 동안 `StartRun`과 `Session.SelectCharacter`가 출전을 차단하는 것이다. 제품 UI 변경은 필요하지 않다. 당시 21 PASS / 1 FAIL, native exit 1, Console 오류 0이며 나머지 3프로세스와 모션 관측은 미실행이다. [후보01 원인 기록](../Evidence/Dragon-v3/dragon-v3-candidate-01/failure-triage.json)과 모든 후보01 산출물은 보존했다. 실패 직후 화면은 없으므로 원래 이름 표시 원인은 소스 추적 결과다.

변경된 실행 소스는 [DragonThemeSmokeProbe.V3.cs](../../Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.V3.cs)의 `V3Fresh`뿐이다.

- 루카·다렌·미엘의 안정 ID/순서/표시명과 실제 출전 가능 여부를 검사한다.
- 기존 Unity synthetic pointer/raycast helper로 각 목록 패널을 누르고 화면·레이아웃을 캡처한다. OS 마우스·키보드를 보내지 않는다.
- `CharacterName`, `StartRun.interactable`, `StartLabel`을 각각 기록·검사한다. 다렌·미엘은 `제작 중`이며 실제 선택 버튼이 비활성화되어야 한다.
- 각 미리보기에서 실제 선택 ID, 골드와 조각, 로비 상태가 변하지 않는지 확인한다. 직접 `SelectCharacter` 거부 검사도 유지했다.
- 마지막에 루카 패널을 눌러 선택 가능 상태와 지갑 불변을 확인한 뒤 기존 강화·첫 Stage1 튜토리얼 검사로 이어간다.

동결 소스 SHA256: `46854150acef28bf659a3e5877b069ebb3b4f866097e460c493cbf3a15165a8e` (30,313 bytes).

| 검증 | 결과 |
| --- | --- |
| Player defines 소스 컴파일 | PASS, 오류 0 / 기존 DTO CS0649 경고 27 |
| Editor defines 소스 컴파일 | PASS, 오류 0 / 기존 DTO CS0649 경고 28 |
| QA 실행 코드 변경 범위 | 이전 동결 목록 대비 V3 partial 한 파일 |
| 제품 통합 파일 | integration manifest의 15개 해시 일치 |
| ReferenceLobbyView / GameSession.Economy | 수정 전후 해시 일치 |
| 후보01 campaign / failure-triage | 수정 전후 해시 일치 |
| 수정된 UI 흐름의 native 검증 | NOT_RUN, 새 관측 빌드 필요 |
| 기존 EditMode5 / PlayMode11 | 재실행하지 않음 |
| OS 실입력 / 사용자 기본 저장 | DEFERRED / NOT_ACCESSED |

기계 기록과 컴파일 로그·수정 diff는 [verification.json](../Evidence/Dragon-v3/preview-contract-source-fix-20261024-01/verification.json)에 연결되어 있다. 컴파일은 Unity 실행 없이 로컬 C# 컴파일러와 기존 참조 DLL을 사용했고, 산출물은 QA 검증 폴더에만 썼다. 제품 소스·아트·일반 릴리스는 변경하지 않았다. 이번 인계는 소스 준비 완료이며 native 실행이나 시각 합격 선언이 아니다.
