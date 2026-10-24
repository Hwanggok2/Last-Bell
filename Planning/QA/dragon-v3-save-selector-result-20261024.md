# Dragon-v3 저장 EditMode 표적 결과

2026-10-24 KST. **SaveDirectorySelectorTests 5/5 PASS**, 실패0·skip0. 기존 Editor 실행권을 인계받아 지정된 저장 검사만 수행했고 반환했다. native 게임·PlayMode·아트 관측 캠페인은 실행하지 않았다. 사용자 기본 저장 프로필은 계속 비접근이다.

| 항목 | 결과 |
| --- | --- |
| 기존 Editor | `Client@65e60f79bc76ded9`, `C:/Github/Test2/Client`, Unity6000.3.15f1 |
| 모드/범위 | EditMode, `SaveDirectorySelectorTests`4메서드/5 cases만 |
| job ID | `d08fb707b8654142875fa82d936f98d7` |
| 시작/종료 |19:19:35.275 →19:19:39.761 KST |
| 결과 |5 passed /0 failed /0 skipped, test summary duration0.7612274초 |
| 입력 소스 |검사 전후 selector·repository·테스트 소스·generated catalog SHA256 동일 |
| 반환 상태 |Mvp 씬, 비Play·dirty false·root3·테스트 미실행·Console 오류0 |
| 설정 |게임/Editor scoped PlayerPrefs 전후 동일. 복원 쓰기 불필요 |
| 사용자 저장 |실제 기본 프로필에 접근하지 않음. 테스트가 소유하는 임시 GUID 디렉터리의 가상 저장만 사용 |

## 실행한 다섯 검사

| 검사 | 결과 |
| --- | --- |
| `ProductionKeepsExistingNamespaceAndSelectionCreatesNothing` |기존 경로 유지·playtest 별도 namespace 선택·selector의 파일/폴더 비생성 PASS |
| `ExplicitDirectoryHasPriority(False)` |일반 경로 선택에서 명시적 override 우선 PASS |
| `ExplicitDirectoryHasPriority(True)` |playtest 경로 선택에서 명시적 override 우선 PASS |
| `FirstPlaytestStartsStageOneWithoutChangingLegacyPrimaryOrBackup` |가상 구 저장/백업 보존, 새 프로필 프롤로그→첫1층 PASS |
| `ReopeningTheSamePlaytestPreservesItsCheckpointAndWallet` |동일 playtest 경로 재개 시 골드12345·체크포인트11 보존 PASS |

실행 전 컴파일은 종료 상태였지만 새 C# 파일이 아직 Unity에 등록되지 않아 `SaveDirectorySelector`, `DragonV3`, `ReleaseV3Frames` 참조 오류가 남아 있었다. `SaveDirectorySelector.cs`, `DragonThemeSmokeProbe.V3.cs`, `SaveDirectorySelectorTests.cs` 세 파일만 개별 등록하고 scripts 컴파일을 완료했다. 이후 총괄의 추가 지시대로 `scope:all, mode:force` 새로고침을1회 수행했다. 새 Runtime 어셈블리와 오류0을 확인한 뒤 테스트를 시작했다. 이 과정에서 소스 수정·새 Editor 시작·Play 진입·씬 저장은 하지 않았다.

테스트 종료 후 기존 Mvp 씬과 비Play 상태, 실행 중인 테스트 없음, scoped 설정 일치를 확인했다. **Editor 실행권은 총괄에 반환했다.** 기존 모션11 cases와 전체 Core suite를 반복하지 않았다.

실제 exe의 신규 시작·native 재실행, 새 적/Player-v3/지면의 렌더 검증, 일반 배포 빌드·ZIP은 이 결과에 포함하지 않는다. 최종 아트 통합과0.9.2 관측 빌드 완료 후 별도 native 캠페인 인계를 기다린다. OS 실입력·모바일 보류도 유지한다.

근거: [원본 테스트 결과](../Evidence/Dragon-v3/editor-save-selector-20261024-01/editmode-result.json), [반환 Editor 상태](../Evidence/Dragon-v3/editor-save-selector-20261024-01/editor-return.json), [반환 씬](../Evidence/Dragon-v3/editor-save-selector-20261024-01/scene-return.json), [설정 비교](../Evidence/Dragon-v3/editor-save-selector-20261024-01/editor-playerprefs-restored.json), [기계 검증 기록](../Evidence/Dragon-v3/editor-save-selector-20261024-01/verification.json). 준비 시점의 [소스 동결 인계](dragon-v3-source-handoff-20261024.md)는 역사 기록으로 보존한다.
