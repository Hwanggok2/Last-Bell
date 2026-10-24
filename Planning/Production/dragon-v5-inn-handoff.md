# Adventure v5 여관 UI 인계

## 소유 파일과 범위

- `Client/Assets/Game/Runtime/Presentation/ReferenceLobbyView.cs`: 기존 캐릭터 선택 → 강화 동선, 성장/동료/유물 연결, 회복력을 포함한 다섯 스탯과 각 행의 10칸·별점·+ 버튼.
- `Client/Assets/Game/Runtime/Presentation/CompanionInnView.cs`: 저장된 동료 목록·편성·서비스·확인 모달의 표시와 Core 명령 연결.
- `Client/Assets/Game/Runtime/Presentation/FeedbackLayout.cs`: 기존 성장 화면의 다섯 행 및 공통 여관 헤더/탭 레이아웃. 동료 하위와 StoryV5 대사 하위 글꼴은 각각의 전용 레이아웃에 맡김.
- `Client/Assets/Game/Editor/DragonV5InnMigration.cs`: 기존 여관 하위에만 누락 노드를 추가하는 명시적 부분 마이그레이션. 씬/프리팹 로드·저장·빌드 호출 없음.

런타임에는 UI 노드를 생성하지 않는다. 기존 `GameView.prefab`의 ReferenceLobbyView 연결을 읽고, 원래 캐릭터 선택·스탯 견적·강화·교환 경로를 보존한다. MvpView/MvpPresenter/Core/Data/Persistence/원본 프리팹은 이 담당자가 수정하지 않았다.

## 총괄 연결 지점

1. `MvpView.Companions`에 `CompanionInnView` 저장 필드 추가.
2. `Reference.Wire(this)` 다음 `Companions.Wire(this)` 호출. 내부에는 한 번만 이벤트를 등록하는 가드가 있다.
3. 여관 렌더에서 `Reference.Render(Session,Art)` 다음 `Companions.Render(Session,Art)` 호출. `Reference.UpgradeScreen`을 유지하며 `Companions.IsOpen`이 성장/동료 본문만 전환한다.
4. 여관 프레임 갱신에서 `Companions.SamplePortrait(IsPaused?0:unscaledDeltaTime)` 호출. Core 계산은 이 프레임 샘플러에서 호출하지 않는다.
5. 세션 Bind/선택 초기화 때 `Reference.ResetSelection()`이 연결된 동료 화면까지 초기화한다. 직접 호출할 필요가 있다면 `Companions.ResetSelection()`도 제공한다.
6. `DragonV5InnMigration.Apply(view)`를 총괄의 저장 마이그레이션 안에서 호출. `view.Companions`까지 연결하며, 이후 총괄이 해당 프리팹과 씬을 저장한다.

`Open()`, `Close()`, `IsOpen`, `Layout(width,height)`도 공개된다. 동료 화면의 Stage 버튼은 자체 저장된 `Depart`이며 기존 Core.StartRun을 호출한다. 동료 본문이 열린 동안 기존 공통 View.StartRun은 숨기고, 성장으로 돌아오면 복원한다. 이는 오른쪽 패널과 공통 출전 버튼의 형제 그리기 순서 충돌을 방지한다.

## 에셋 선행 조건

초상 경로: `Assets/Game/Art/Dialogue/Dragon-v5/{tori,barun,doto,rina,momo}.png`, 24px 원본과 Point 필터.

Idle ID: `companion_tori`, `companion_barun`, `companion_doto`, `companion_rina`, `companion_momo`. `ArtCatalog`에 등록된 해당 ActorSprites의 Idle 클립을 재생한다. 목록/슬롯 초상은 48px, 상세 초상은 144px 기준이며 정지 초상은 Idle가 없을 때의 표시 대체물이다. Idle 제작 완료를 대체 초상 표시만으로 주장하지 않는다.

마이그레이션은 5개 초상이 모두 임포트되고 Point 필터인지 먼저 확인한 뒤 UI를 수정한다. 누락 시 예외를 내며 임의 대체 얼굴을 만들지 않는다. 모모는 미발견·Stage42로 표시하고 서비스 구매는 Core 해금으로 잠근다.

## 화면과 입력

1280×720 Safe Area 좌상단 기준. 목록 (24,208,376,488), 상세 (424,208,832,488). 작은 편성 슬롯 3개는 왼쪽 목록 상단에 있으며, 목록은 세 행을 보이는 세로 스크롤이다. 행·주 행동·모달 버튼은 96px 높이로 640×360에서 48px가 된다. 목록의 나머지 두 NPC도 스크롤로 선택할 수 있다.

성장 목록은 동일한 왼쪽 패널에 96px 행 5개로 배치한다. 각 행에 스탯명/Lv, 별점, 10칸, +를 저장한다. +는 해당 스탯을 한 단계 강화하며, 골드가 부족하면 기존 교환 확인을 연다. 큰 강화 버튼 및 +1/최대 견적도 기존 경로를 유지한다. 회복 표시 문구는 실제 `CombatStatsData.recovery`를 사용한다.

저장 GameView를 직접 확인한 결과 Purchase/Exchange는 전체 영역으로 Stretch된 ReferenceUpgrade의 직계 자식이고, OneLevel/Maximum만 UpgradeDetail의 자식이다. 따라서 Purchase/Exchange의 x456/y572 등은 Safe Area 기준, OneLevel/Maximum은 상세 패널 기준으로 분리한다. +는 스탯 선택 버튼 안에 중첩하지 않고 StatMenu의 형제 버튼으로 저장한다. 스탯 선택 (x16,w232)과 + (x264,w96) 사이에는 16px 간격이 있어 포인터 영역이 겹치지 않는다. 10칸 이미지는 raycast=false이고 선택 영역 안에 들어간다. 마이그레이션 검증도 이 부모 계약을 확인한다.

여관 우상단에 골드/보석, 헤더에 현재 모험가, 우하단 출전 버튼에 `Stage N`을 표시한다. 성장/동료/유물 탭이 연결되며, 유물은 기존 저장 인벤토리와 뒤로가기를 그대로 사용한다.

## 상태·거래 계약

- UI는 `Companions`, `QuoteCompanionSlot`, `QuoteService`, `ServiceEffect`, `ServiceDuration`와 성장 견적을 읽는다. 해금·가격·피해·DPS 상한·별점 성장·환불을 별도 계산하지 않는다.
- 동료 강화는 현재 피해와 `NextDamageMilli`, 가격을 표시한다. 임시 레벨 한도에는 다음 보스 조건을 원본 카탈로그에서 읽어 표시한다.
- 미발견은 같은 얼굴의 실루엣과 만남 Stage로 표시하며 강화/출전/서비스를 잠근다. 기존 저장에서 해금된 도토·리나는 Stage22/32를 다시 기다리지 않는다.
- 전체 슬롯이 찼으면 교대할 슬롯 선택 → 실제 교대 확인의 두 단계. 동일 동료 중복과 최종 슬롯 유효성은 Core가 검사한다.
- 서비스 행을 누르면 전용 저장 모달이 열린다. 효과·기간·단계·보스 조건·현재 재화·정확한 견적을 표시하고 원래 견적을 Core에 제출한다.
- 무기는 `CanCraftWithoutGold`와 `WeaponLockReason`으로 보스 잠금과 골드 부족을 구분한다. 보스 조건을 만족하지 못하면 골드 교환을 권하지 않는다. 예약 취소액은 `ReservedServicePaidGold`의 실제 지불액이다.
- 토리의 지역 안내는 `VisitedRegionStarts`로 받은 방문 지역만 Stage 구간과 해당 지역 보스 이름으로 표시한다. 미방문 지역을 방문한 것으로 계산하거나 다른 캐릭터 체크포인트를 해금하지 않는다.
- 골드 부족은 `RequestPurchaseExchange(cost)`로 기존 교환 모달을 연다. 전용 모달은 먼저 닫으며, 교환은 골드만 지급한다. 이후 해당 강화/제작/예약 버튼을 다시 눌러야 한다. 교환 취소는 구매하지 않는다.
- `Upgrade`/`PurchaseService`가 성공한 반환값일 때만 해당 버튼 위치에 `SkillEffects.PlaySupport(Upgrade/Blessing, target)`을 호출한다. 견적 열기·실패·거절·교환에는 구매 효과를 재생하지 않는다.

## 적용 및 검증 상태

현재 전달은 소스와 부분 마이그레이션 도구 작성 단계다. 이 담당자는 Unity Apply, 빌드, 테스트, QA, 커밋, 푸시를 실행하지 않았다. 실제 저장 프리팹 적용·런타임 렌더 확인·Core 통합 검증은 총괄과 완료 후 전용 QA가 담당한다.

최종 통합 시 확인할 항목: 새 계정의 잠긴 목록/기존 네 동료, 640×360의 다섯 스탯과 목록 스크롤, 편성 교체 거절, 골드 부족 교환 취소, 제작 보스 잠금, 예약 취소 환불, 성공 시만 파티클, 성장↔동료↔유물 왕복과 캐릭터 변경 후 재화/Stage/예약 일치.
