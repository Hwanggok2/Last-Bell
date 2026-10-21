# Unity 1–10 MVP UI 구현 검토

2026-10-21 KST. 상태: **필수 UI 구현 완료. 보유 유물 필터를 포함한 전달용 Windows·Android 빌드와 Windows 실행 스모크 통과. 10:14 KST 갱신960×540 캡처4장을 독립 확인해 이전 캡처 한계2건의 해소 및 공중 회피·보유 유물만 표시를 확인했다. 추가 소스 변경 없음.**

이 문서의 정식 경로는 `C:\Github\Test2\Planning\Production\ui-implementation-review.md`다. 기존 `C:\Github\Test2\Production\ui-implementation-review.md`에서 이동했다.

## 소유 파일

- `Client/Assets/Game/Runtime/Presentation/*.cs`
- `Client/Assets/Game/Editor/MvpSceneBuilder.cs`
- 이 검토 문서. Core/Data/Art/Bootstrap 코드는 조회만 했다.

## 총괄 조립 API

- `MvpSceneBuilder.CreateMissing()` → 저장된 `Assets/Game/Prefabs/GameView.prefab`, `Actor.prefab`, `Assets/Game/Scenes/Mvp.unity`를 없을 때만 만든다. 기존 파일을 재생성하지 않는다.
- `MvpSceneBuilder.Validate()` → 필수 화면/배열·72×72 기준 회피 hit area·VFX 풀 참조 검사.
- `MvpSceneBuilder.AttachBootstrap(root,presenter)` → 별도 소유 `GameBootstrap`이 있으면 `CatalogJson`과 `Presenter`를 저장 참조로 연결한다.
- `MvpPresenter.Bind(GameSession)` → Changed/EventRaised 구독 및 상태 표시. `MvpPresenter.Update`가 입력 이후 `Session.Tick(unscaledDeltaTime)`을 **한 번만 호출**한다. Bootstrap은 생성/저장만 담당한다.
- `ShowBlockingMessage(title,body,Action retry)` / `ClearBlockingMessage()` → 기존 모달 재사용. 오류 중 입력과 Tick 중단, 재시도만 표시.
- ArtCatalog 경로 `Assets/Game/Art/MvpArtCatalog.asset`. Actor는 idle의 bounds 높이에서 기준 스케일을 한 번 정하고 BottomCenter pivot을 유지한다. 개별 포즈마다 높이를 다시 맞추지 않는다.

## 구현된 표시/입력

- 4방향 화살표만, 투명72×72 hit area(640×360에서48px). 포인터 down·WASD/방향키·Space, Escape 일시정지.
- Canvas overlay HUD와 독립 WorldRoot. 치명타는 WorldRoot만80ms 흔들며 HP/키/예고는 고정한다. 감소 옵션은 로컬 선호로 보존한다.
- 삼각 정답 대신 낮은 쓸기/상단/찌르기/바깥 궤적. 코어 EnemyIntent의 현재/다음 신호를 표시한다.
- 발밑 HP, 중간/보스 상단 HP, 종 게이지와 별. 공격명·기절 글자·별도 전투 판정 없음.
- 최초2행동 튜토리얼, 로비4스탯/별5개/종10개/+·Stage N·우측 지갑. 데이터 availableInMvp에 따라 캐릭터 선택 가능 여부 반영.
- 부족 골드 모달은 코어 견적과 CanConfirmExchange를 표시한다. 확인·취소·강화/중복 처리 계산은 코어 소유다.
- 유물 좌1/3 슬롯/스크롤 보유 목록, 우2/3 상세와 장착·해제·슬롯 구매. 3카드 보상은 신규/Lv전후/효과전후/MAX/장착 여부를 표시하고 코어 ChooseRelic만 호출한다.
- 픽셀 숫자8개와 사각 파편32개를 prefab에 미리 저장한다. 런타임에 UI 계층을 생성하지 않는다. 동작은 세션 이벤트로 시작하며 UI 타이머가 판정하지 않는다.

## 남은 통합 검증

최신 시각 migration, EditMode, 전달용 Windows·Android 빌드, Windows 실행 및960×540 캡처 검토를 완료했다. 프로세스 재시작 후 저장 복구, 640×360·와이드 화면 및 실제 터치 기기 검토는 이 문서의 확인 범위에 포함되지 않는다. 기존 치명타 숫자·일시정지 캡처의 한계는 아래 전달용 검수에서 해소됐다. 유물0개 빈 상태는 소스 분기만 확인했으며 해당 화면 캡처를 독립 확인했다고 주장하지 않는다. 총괄이640×360 캡처와 보드 검증을 진행하며, 추가 소스 변경은 없다.

## 최초 에셋 생성 뒤 입력 크기 마이그레이션

`GameView.prefab` 생성 시각이 마지막 최소 입력 크기 조정보다6초 빨랐다. YAML을 조회해 Pause56/Upgrade64/보상 받기 상대높이 등 이전 크기가 저장된 것을 확인했다. `LastBell.Editor.MvpSceneBuilder.MigrateExisting()` 명시 실행으로 갱신 완료했다. 기존 씬/프리팹을 삭제하지 않으며, 초기 생성값과 일치하는 버튼의 RectTransform만72 reference px로 갱신한다. 목록 높이·토글의 투명 입력면·보스HP 우측 여백도 필요한 초기값만 변경한다. 사용자 수정 크기는 건너뛰고 GUID·스타일·참조는 유지한다. 반복 호출은 동일 결과다.

`prepare-2.log`의 LAST_BELL_PREPARED와 정상 종료를 확인했다. GameView.prefab,Actor.prefab,Mvp.unity가 실제 자산으로 생성됐다.

## 현재 실행 증거와 한계

총괄의 `Planning/Evidence/Unity/editmode-results.xml`은36개 테스트 통과/실패0, `windows-build.log`는 Windows 빌드 성공/오류0이다. `player-render-smoke.log`와 `Smoke/smoke-result.json`에서 튜토리얼,보스 보상2회,완벽 회피76회,치명타11회,피격3회,체크포인트11,장착1개 등 자동 실행 흐름 통과를 확인했다. 이 카운트는 검증용 입력의 결과이며 실사용 플레이 품질 지표가 아니다.

최신 `Planning/Evidence/Unity/editmode-final.xml`은2026-10-21 10:06 KST 기준37개 테스트 통과/실패0/건너뜀0이다. 이는 최신 코드의 EditMode 검증이며 Windows 실행본·터치 실기기·시각 검토 또는 프로세스 재시작 후 저장 복구 완료를 의미하지 않는다.

초기 캡처는 검은 화면이었지만, `player-smoke-3.log` 실행에서 RenderTexture로 정상 캡처됐다. 이때 기본 둥근 스프라이트가 넓은 패널과 HP에 늘어나 흐려지는 결함을 확인했고, 시각 migration 후 아래 최종 캡처에서 해소됨을 확인했다.

## 캡처/전조 보완

총괄의 RenderTexture 캡처를 위해 ScreenPoint 변환은 Canvas가 ScreenSpaceOverlay이면null, 다른모드이면canvas.worldCamera를 사용한다. DangerGraphic은 color.a를 모든 메시 정점에 반영해 다음 예고의0.4alpha가 적용된다. 경과는 밝기뿐 아니라 궤적을 따라 차오르는 주선과 이동하는 사각 선단으로 표현한다. 판정시간은 변경하지 않고 EnemyIntent.Progress만 읽는다. 최신 컴파일·실행 및 캡처까지 완료했다.

## 정상 캡처 후 시각 migration

`MvpSceneBuilder.MigrateExisting()`를 다시 실행하면 `Assets/Game/Art/UI/WhitePixel.png`를 없을 때만 생성한다. 실제 1×1 흰색 PNG이며 Point/Clamp/무압축/FullRect Sprite로 저장한다. GameView의 `UI/Skin/UISprite.psd`를 사용하는 Image만 이 스프라이트로 바꾸므로 기존 색·크기·Filled HP 설정·사용자가 지정한 다른 아트는 유지된다. 초기 위치(0,-2.65,0)와 기본 스프라이트가 그대로인 Ground만 비활성화한다. 새 씬에는 중복 Ground를 생성하지 않는다. 기존 씬/프리팹/GUID를 삭제하거나 재생성하지 않는다.

`Planning/Evidence/Unity/ui-visual-migration.log`에서 `LAST_BELL_UI_INPUT_MIGRATION_COMPLETE; LAST_BELL_UI_VISUAL_MIGRATION_COMPLETE`와 종료 코드0을 확인했다. 저장 자산도 조회해 GameView의 Image99개가 WhitePixel 스프라이트를 참조하고, Ground 비활성 및 Enemy X=1.45가 씬에 저장됐음을 확인했다. WhitePixel의 import 설정은 Point/Clamp/무압축/FullRect이며 자산 GUID는 `3bdef3b04836cee44836eca8cae93a6e`다. 최종 빌드와 새 캡처에도 반영됐다.

근접 거리: 총괄이 확정한 Enemy X=1.45를 적용했다. 새 씬은 1.45에서 생성하고 migration은 기존 X=2.25인 경우만 변경한다. 플레이어의 공격 전진은 0.35, 적은 플레이어 방향(-X)으로 0.16이다. 공격 시작 후 처음100ms는 첫 준비 프레임, 이후는 타격 프레임으로 고정하므로 전역 idle 시간 때문에 타격 프레임부터 시작하지 않는다. 전진도100ms에 최대에 도달한 뒤 복귀한다. 전투 판정과 데미지 시점은 코어를 그대로 사용한다.

## 최종 Windows 실행과 독립 시각 검토

`Planning/Evidence/Unity/windows-final.log`에서 `LAST_BELL_BUILD: Succeeded; 166059804 bytes; 0 errors`를 확인했다. `player-final.log`와 `Smoke/smoke-result.json`은 튜토리얼, 보상2회, Stage11 자동 진입, 사망 후 체크포인트 로비, 강화, 장착, 교환, Stage11 재진입을 통과했다. 결과는 perfect76/normal1/critical11/playerHits3/checkpoint11/attackLevel29/equippedRelics1이다. Stage11 재진입은 같은 프로세스 안의 검증이므로 재시작 저장 복구와 구분한다.

2026-10-21 10:07:57~10:08:05 KST에 갱신된 아래960×540 PNG를 직접 열어 검토했다. 파일은 모두 `Planning/Evidence/Unity/Smoke/` 아래에 있다.

| 캡처 | 확인 결과 |
| --- | --- |
| `03-stage-1.png`, `06-boss.png` | 단색 직사각형 HP·종 게이지·버튼이 선명하며 하단 중복 Ground가 없다. 일반 적 발밑HP와 보스 상단HP가 구분되고, 화살표·공격 입력 영역과 캐릭터가 겹치지 않는다. |
| `09-lobby.png` | 캐릭터, 별·종·레벨·가격·강화+, 우측 지갑과 Stage11 버튼이 잘리지 않는다. |
| `12-relic-equipped.png` | 좌측 목록과 우측 상세의1/3:2/3 배치, 장착1/1·장착 중·해제 상태 및 슬롯 버튼이 읽힌다. |
| `13-gold-exchange.png` | 파편1개/골드100, 지갑 전후 값, 교환 뒤 강화+ 안내, 교환·취소 버튼이 모두 보인다. |
| `07-boss-reward.png` | 3카드의 신규/Lv변경/효과 전후와 받기 버튼, 자동 다음 전투 안내가 잘리지 않는다. |
| `dodge-back.png`, `dodge-down.png`, `dodge-forward.png` | 뒤 이동·몸 낮춤·앞 이동 포즈가 구분된다. 좌/우 위험 궤적이 캐릭터 주변에 표시된다. |
| `dodge-up.png`, `enemy-stunned.png` | 첫 up 캡처는 점프 정점이 아니지만 enemy-stunned 캡처에서 공중 회피가 보인다. 적 머리 위 별3개 및 종 게이지가 표시된다. |
| `16-critical-hit.png` | 큰 숫자와 주변 파편이 표시되지만 이전 숫자와 겹쳐 정확한 치명타 숫자 가독성 통과 근거로 쓰지 않는다. |
| `15-checkpoint-resume.png` | 일시정지 모달·옵션·버튼이 표시되지만 캐릭터가 모달 앞으로 그려지는 캡처 정렬 문제가 있다. |

확인한 정적 화면에서 새로 확정한 필수 UI 코드 결함은 없다. 다만 두 캡처는 다음 조건 때문에 제한적으로 해석한다.

1. `MvpSmokeProbe`는 화면 프레임마다 `Session.Tick(.01f)`를6회 호출하며 코어 시간을 빠르게 진행한다. `BattleEffects`의 숫자는 실제 시간으로450ms 유지된다. 일반 공격 간격500ms와 달리 스모크에서는 이전 숫자가 남은 동안 후속 타격이 발생하므로 `16-critical-hit.png`/`enemy-stunned.png`의 숫자 겹침은 가속 검증의 영향으로 추정된다. 정상 속도의 치명타 단독 캡처로 추가 확인할 수 있다. 현재 증거만으로 일반 플레이 결함이라고 단정하지 않는다.
2. 캡처는 원래 ScreenSpaceOverlay Canvas를 임시 ScreenSpaceCamera로 바꾸지만 정렬 순서를 높이지 않는다. Actor Body의 sortingOrder=2가 Canvas 기본0보다 앞서므로 캐릭터가 일시정지 모달 위로 보이는 것으로 추정된다. 원래 Overlay 모드의 실제 실행 화면 또는 캡처용 Canvas sortingOrder를 액터보다 높인 캡처로 판별해야 한다. 이 문제를 해결하려고 저장 UI/씬의 계층을 변경하지 않았다.

정지 이미지로 공격 준비100ms·기절 지속·흔들림80ms의 시간 정확도 또는 터치 감각까지 검증했다고 주장하지 않는다. 코드 추가 변경은 하지 않았다.

## 마지막 요청: 보유 유물 목록과 빈 상태

최종 시각 검토 이후 총괄 요청에 따라 `MvpPresenter.RenderRelics()`만 수정했다. 목록은 보유 레벨>0인 유물만 채우고, 선택 항목이 현재 목록에 없으면 첫 보유 유물을 선택한다. 보유0개이면 선택을 비우고 기존 상세 라벨에 `유물이 없습니다` / `5·10층에서 획득`을 표시하며 장착 버튼을 비활성화한다. 목록의 스크롤 높이는 실제 표시되는 마지막 행에 맞춘다. 슬롯 표시·가격·구매 조건 및 코어 명령은 유지했다. 새 UI 계층이나 프리팹 migration은 필요 없다.

이 변경은 소스 분기(0개/보유 있음/선택 유효하지 않음)를 확인했다. 이후 전달용 재컴파일·실행과 보유1개 화면까지 아래에서 확인했다. 앞선37 테스트가 이 마지막 변경 이후 재실행됐다고 주장하지 않는다. Root는 캡처 전용 sortingOrder/이전 effect 정리/Up pending/settle 시간을 별도로 보정했으며 실제 게임 효과 코드는 변경하지 않았다.

## 전달용 최종 캡처 재검수 — 10:14 KST

`Planning/Evidence/Unity/windows-delivery.log`의 `LAST_BELL_BUILD: Succeeded; 166060531 bytes; 0 errors`, `android-delivery.log`의 `LAST_BELL_ANDROID_BUILD: Succeeded; 432932320 bytes; 0 errors`를 직접 확인했다. `player-delivery.log`에는 전체 UI 흐름과 `LAST_BELL_SMOKE_PASSED`가 기록돼 있으며 결과는 tutorial=true/bossRewards2/perfect76/normal1/critical11/playerHits3/checkpoint11/attackLevel29/equippedRelics1이다. Android 빌드 성공은 실기기 실행 검증과 구분한다.

10:14:00~10:14:12 KST에 갱신된 아래4장을 실제 이미지로 다시 열었다.

| 최종 캡처 | 독립 확인 |
| --- | --- |
| `Smoke/16-critical-hit.png` | 큰 치명타 숫자 **15**와 양옆 파편이 중첩 없이 선명하다. 이전 숫자 겹침 캡처 한계는 해소됐다. |
| `Smoke/dodge-up.png` | 플레이어 발이 바닥에서 떨어진 실제 공중 회피 포즈가 보인다. 지상 idle 캡처 문제는 해소됐다. |
| `Smoke/15-checkpoint-resume.png` | 모달이 캐릭터보다 앞에 표시된다. `계속`·`귀환` 버튼과 흔들림 옵션이 가려지지 않는다. 이전 카메라 캡처 정렬 한계는 해소됐다. |
| `Smoke/12-relic-equipped.png` | 왼쪽에는 보유한 `꺼진 심지 Lv.2` 한 항목만 표시된다. 오른쪽의 장착 상태·현재/다음 효과·해제 버튼 및 슬롯 구매 버튼이 유지된다. 미보유 목록은 없다. |

위 캡처 보정은 진단 경로에만 적용됐다. 실제 BattleEffects나 UI 동작의 추가 변경 없이 전달용 UI 화면을 확인했다. 이 검토에서 추가로 보고할 필수 UI 결함은 없다.
