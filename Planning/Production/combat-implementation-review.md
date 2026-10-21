# 1~10층 MVP 전투 코어 구현 검토

작성일: 2026-10-21 KST  
상태: **코어 구현·C#7.3 독립 컴파일·NUnit31건 통과 / 실제 Unity EditMode·UI 통합 검증은 총괄 진행**

## 소유 범위와 결과

수정한 런타임은 Client/Assets/Game/Runtime/Core의 GameSession.cs, GameSession.State.cs, GameSession.Combat.cs, GameSession.Progression.cs, GameSession.Economy.cs, SessionState.cs다. 테스트는 Tests/EditMode/Core/GameSessionTests.cs와 LastBell.Core.Tests.asmdef이며 Runtime 참조는 LastBell.Runtime이다. 데이터 DTO·카탈로그·UI·Bootstrap·씬·아트는 수정하지 않았다.

외부 SDK·서비스·Unity UI 의존 없이 하나의 GameSession이 시간·전투·상태 전이·보상·로비 경제를 소유한다. 같은 클래스를 전투/진행/경제/저장 상태 파일로 나눴으며 별도 범용 프레임워크나 인터페이스 계층은 추가하지 않았다. GameCatalog의 필드는 읽기 전용 계약으로 사용한다.

## 공개 API와 연결

- 생성: GameSession(GameCatalog catalog, GameSaveData saved = null). 새 저장은 첫1층의 TutorialDodge로 즉시 시작한다.
- 입력: Tick(float), Attack(), Dodge(DodgeDirection), StartRun(), ReturnToLobby(), ChooseRelic(id), Upgrade(statIndex), ConfirmExchange(), CancelExchange(), EquipRelic(id), UnequipRelic(id).
- 로비/정지: SelectCharacter(id), BuyNextSlot(), SetPaused(bool). 제작 중 캐릭터는 availableInMvp=false이면 선택을 거부한다.
- 읽기: Phase, Stage, Cycle, CheckpointStartStage, PlayerHp/PlayerMaxHp, EnemyHp/EnemyMaxHp, Stagger, IsEnemyStunned/IsStaggerImmune, StunSecondsRemaining, EnemyIntent, PlayerStats, RelicChoices, EquippedRelicIds, PendingExchange/CanConfirmExchange, Gold/Fragments/UnlockedSlots, GetStatLevel/GetUpgradeCost/GetRelicLevel.
- 이벤트: Changed는 표시 갱신, EventRaised는 실제 피해 milli-HP·치명 여부·회피 방향·발생 층 등의 연출 정보, SaveRequested는 확정 변경의 저장 요청이다. 입력/보상/경제 변경과 실제 타격을 저장 요청하며 시간만 흐른 프레임은 IsDirty만 갱신한다.
- 저장: ExportSave()는 배열·목록·중첩 DTO까지 분리한 복사본이다. 저장 성공 뒤 MarkSaved(). GameSaveData 등은 JsonUtility 호환 public fields/[Serializable]이다. 파일 IO와 저장 실패 시 게임 정지는 총괄 Bootstrap 책임이다.

기존 MvpPresenter와 GameBootstrap의 호출명을 읽어 일치함을 확인했다. 키 입력을 처리한 뒤 Tick하는 현재 Presenter 순서와 함께 사용할 수 있다. UI는 판단/재화 계산을 다시 하지 않고 결과를 표시한다.

## 실제 구현 규칙

| 영역 | 반영 |
|---|---|
|튜토리얼 |처음 올바른 위 회피만 승인→공격 준비100ms 뒤 실제 명중→실시간 전투. 잘못된 입력/기다림은 보호. 단계 저장·복구 |
|회피 |정답 방향·타격 전160ms 이내 성공, 그중60ms 이내 완벽. 다음 회피260ms, 공격은 회피160ms 뒤 가능 |
|공격 |500ms 시작 간격·100ms 준비. 회피가 준비 중 공격을 취소해도 간격은 환급하지 않음 |
|시계 |프레임마다한 번 타격하는 방식 대신 예약 시각 순으로 처리. 동시 적 타격/플레이어 공격에서는 적 타격과 플레이어 사망을 먼저 처리 |
|기절 |일반20/완벽40/문턱100, 피격-20. 일반1.5/중간1.2/보스1초, 기절 중·이후2초 축적 불가. 패턴 취소 후 온전한 예고로 재개 |
|피해 |실제HP·피해는 long형 milli-HP. 치명/공명 배율을 각각 한 번 적용. 화면용 float와 저장값을 분리 |
|패턴 |카탈로그 patternSteps 순서·650/900/250ms 예고·450ms 이상 간격. 위/뒤/아래/앞4방향과 다음 연타 예고 정보 제공 |
|진행 |일반 승리는HP 유지 후 다음 층 자동 진입.5/10처치 기본 보상과3개 후보 저장, 선택 후 다음 적 자동 진입 |
|유물 |신규Lv1/중복Lv+1/MAX만 구역 골드. 자동 장착 없음. 장착 성장만 다음 적 능력에 적용, HP 증가분1회 추가 |
|10층 |선택 확정 때 체크포인트10·새 최대HP 전회복을 함께 적용한 뒤11층 실시간 전투 시작 |
|사망/귀환 |확정 보상 유지·로비 복귀.9층 사망은1층,10완료 뒤 재출전은11층. 보상 선택 대기는 선택을 마치기 전 귀환 불가 |
|MVP 범위 |11층 경계 적까지 싸울 수 있다. 데이터에 없는 다음 층은 MvpComplete로 종료하고 제작 범위를 명시.50완주/NG+를 가짜로 증가시키지 않음 |
|로비 성장 |4개 독립 레벨·별점·돌파·비용을 카탈로그에서 계산. 출전 중 강화/장착 변경 거부 |
|부족분 교환 |부족골드를100G 단위 올림한 견적. 확인은 파편 차감/골드 지급만 수행, 다시+를 눌러 강화. 취소·부족파편·낡은 견적은 소비 없음 |
|공유 범위 |지갑·유물 레벨·슬롯 권리는 계정 공용. 강화·장착·체크포인트는 캐릭터별 |

동일 보상은 stageRewardApplied·제안 rewardId·lastAppliedRewardId·상태 전이로 한 번만 처리한다. 유물 선택과HP 적용 후 다음 층 상태를 같은 ExportSave 결과로 내보내므로 재접속으로 증가분을 다시 더하지 않는다. 견적은 캐릭터/스탯/레벨/비용/지갑버전/잔액/환율을 다시 대조하며 확인 후 폐기한다.

## 중단 복구

전투 중 복구는 양측HP·기절/면역 남은 시각·난수 상태를 유지하고 일시정지 상태에서 시작한다. 진행 중인 플레이어 공격과회피는 취소하며 공격 간격은 환급하지 않는다. 적은 현재 단계의 온전한 예고부터 다시 보여 준다. 기절 중이라면 기절 종료까지 남은 시간을 유지한 뒤 온전한 예고를 시작한다.

보상 대기 중 복구는 같은 후보ID·제안 당시 레벨·이미 지급한 기본 보상·난수 상태를 그대로 복원한다. 재추첨/중복 지급하지 않는다. 튜토리얼은 성공한 단계를 되돌리지 않는다.

## 실행한 검증

설치된.NET10 Roslyn을 C#7.3 언어 모드로 사용해 Core와 실제 GameCatalog를 컴파일했다. Unity에.NET10 API를 추가하지 않았다. NUnit fixture는 같은 Unity 제공 nunit.framework.dll을 참조한 독립 실행기로 **31건 모두 통과**했다.

주요 검사: 튜토리얼2행동/중단,59/60/61/160/161ms 회피 경계,260ms 재사용,4방향 대응,450ms 연타/250ms 후속 예고,공격 취소와 간격 유지,큰/작은 Tick 동일 결과,동시 치명적 타격 우선순위,기절/면역,치명×공명1회 적용,milli-HP·분리된 저장 복사본,보상3개/중단/중복/MAX,HP 성장1회,10선택→11/9사망→1,미구현 경계 종료,교환/취소/부족/낡은 견적,계정 공용 슬롯·캐릭터별 장착/성장,전이 이벤트의 실제 발생 층이다.

독립 실행기의 결과는 Temp/CoreValidation/results.txt에 남겼다. 임시 DLL/실행기는 Assets 밖 Temp/CoreValidation으로 옮겼으며 Tests/EditMode/Core/.validation는 남기지 않았다. Unity의 중복 플러그인 로드 대상에 포함하지 않는다.

Unity EditMode에서는 같은 fixture의 UNITY_EDITOR 경로가 실제 JsonUtility를 사용한다. 독립 실행기는 System.Text.Json public fields 직렬화를 사용했으므로 **독립31건 통과를 Unity JsonUtility/Editor 실행 완료로 바꾸어 표시하지 않는다.**

## 총괄 통합 확인 범위

코어와 테스트는 실행 가능한 상태로 준비되었다. 실제 Unity6000.3.15f1 EditMode 실행·전체 어셈블리 컴파일·저장 파일 IO·씬/프리팹·포인터 입력·화면 피드백·Windows 빌드는 총괄의 통합 검증 범위다. 현재 코드 확인에서 공개API 연결의 필수 불일치는 발견하지 않았다.

총괄 smoke probe의실제 튜토리얼→1~10→5/10보상→11사망→로비강화/교환/장착→체크포인트 재출전 결과를 별도로 기록해야 한다. 코어 테스트의 약한 적/주입된 경계 저장 fixture를 실제 난도로 무사망 완주한 증거로 사용하지 않는다.
