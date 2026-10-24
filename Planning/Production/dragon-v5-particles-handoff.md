# Adventure v5 — 실제 도트 ParticleSystem 인계

2026-10-24. 담당: `character_skill_plan`. **새 런타임·에디터 소스를 작성했고 소스 계약을 검토했다. 담당자는 Unity Apply, 컴파일, 실행, 빌드, 플레이 QA를 실행하지 않았다.** 저장 자산 생성과 실제 화면 확인은 총괄의 통합 적용 단계다.

## 파일과 적용 경계

- `Client/Assets/Game/Runtime/Presentation/SkillPixelEffects.cs`
- `Client/Assets/Game/Editor/DragonV5SkillEffectsMigration.cs`
- `Client/Assets/Game/Editor/DragonV5PartyMigration.cs`
- 이 인계 문서

기존 Presenter·BattlePresentation·BattleEffects·CombatVfx·전투 판정은 이 담당자가 수정하지 않았다. 총괄이 `BattlePresentation.SkillEffects` 저장 참조와 아래 호출을 연결한다. 배경·UI·캐릭터 프리팹 전체를 다시 생성하지 않는다.

`DragonV5SkillEffectsMigration.Configure(presenter)`는 다음 **누락 전용 자산만** 생성하고, 현재 로드된 `View.SafeRoot` 아래에 프리팹 인스턴스를 연결한다.

```text
Assets/Game/Art/Effects/Dragon-v5/
  square-point.png             1×1 흰색 RGBA · Point · mipmap 없음 · 무압축
  PixelParticles.mat           기존 Built-in Sprites/Default unlit
  SkillPixelEffects.prefab
    RawImage + SkillPixelEffects
    ParticleOnlyCamera
    SkillContact               ParticleSystem · 최대96
    ActualShield               ParticleSystem · 최대32
    ActualStagger              ParticleSystem · 최대24
    ServiceAndRecovery         ParticleSystem · 최대64
```

기존 자산이 있으면 재생성하지 않는다. 저장 필드·Point 텍스처·머티리얼이 빠져 있으면 오류로 반환한다. 자동으로 다른 효과나 화면을 대체하지 않는다. `Configure` 후 저장된 씬·프리팹의 저장은 호출자인 총괄 migration이 맡는다. 새 여관 패널을 만든 **뒤에** Configure를 호출하면 효과 RawImage가 전투·로비 위, 기존 모달·일시정지 아래에 배치된다.

### 동료 배우 저장 migration

`DragonV5PartyMigration.Configure(presenter)`는 먼저 `p.Art`의 `companion_tori/barun/doto/rina/momo`5종을 확인한다. 각 Idle4프레임 이상, Attack6프레임 이상, 유효 Sprite 참조, Attack 접촉180ms·전체600ms가 필요하다. 누락 상태에서는 기존 배우 프리팹을 다시 만들지 않고 중단한다.

검증 후 기존 `WorldRoot` 아래에 누락된 `CompanionParty`만 만든다. `PresentationArtImporter.PrefabPath`의 기존 `PresentationActor.prefab`을3개의 자리로 인스턴스화한다. 새 자리의 저장 위치는 Stage1 지면의 계획x380·288·196, 기본 ID는 토리·바룬·도토, 우향이다. 배우는 처음 비활성이고 런타임의 실제 편성에 따라 표시한다. `DefaultCatalog=p.Art`, 동일 SortingLayer에서 플레이어·적보다 sortingOrder1 낮게 두고 `BattlePresentation.Companions`를 저장한다.

이미 존재하는 자리와 배우 계층을 지우거나 새 프리팹으로 갈아 끼우지 않는다. 기존 Party 루트의 이동·회전·크기가 원점 규칙과 다르면 작성자 변형을 덮어쓰지 않고 오류를 반환한다. `Validate(p)`는 저장된3자리·참조·방향·SortingLayer와5동료 모션 계약을 확인한다. 씬 저장·Unity 실행은 하지 않는다.

총괄 적용 순서는 **동료 원화/클립 Import → Party Configure → 새 여관/UI Configure → SkillEffects Configure → 전체 씬 저장**이다. 원본 Attack600ms를 보존하려면 총괄 소유 `CompanionBattleView`의 샘플링도 `.60초/접촉.18초/복귀.42초`를 사용한다. 이전 `.48초/복귀.30초` 구현과의 차이를 총괄에 전달했다.

## 런타임 API

```csharp
// BattlePresentation.Bind의 owner/session 연결 후
SkillEffects.Initialize(owner);

// 기존 BattlePresentation.OnEvent 처리 마지막.
// 해당 AttackStarted에서 이미 선택한 0..5 방향을 공유한다.
SkillEffects.OnEvent(e, SlashVariant);

// TickVisuals에서 카메라와 ActorView 위치/모션 샘플링을 마친 뒤
SkillEffects.Render(session);
```

기존 참격 방향 선택은0~2만 생성하는 코드였고 실제 상·하 분기는0~2/3~5였다. 총괄에 이 불일치를 전달했으며, 현재 총괄 변경에서0~5 선택을 확인했다. 파티클은 전달된 값만 사용하며 전투 RNG나 새 랜덤 방향을 만들지 않는다.

회복·여관의 확인된 성공에만 다음을 한 번 호출한다.

```csharp
// PlayerRecovered 이벤트이며 실제 RecoveryMilli > 0일 때
SkillEffects.PlaySupportWorld(
    SkillPixelEffects.SupportKind.Recovery,
    PlayerActor.CanonicalBodyPoint(.5f));

// 강화/제작 명령이 성공한 뒤. target은 선택한 카드/캐릭터 미리보기 등.
SkillEffects.PlaySupport(SkillPixelEffects.SupportKind.Upgrade, target);

// 축복 예약 성공 뒤: Blessing / 약초차 예약 성공 뒤: Recovery
SkillEffects.PlaySupport(SkillPixelEffects.SupportKind.Blessing, target);
```

UI 재렌더마다 호출하거나 버튼을 눌렀다는 이유만으로 호출하지 않는다. 실패·취소·이미 예약됨에는 성공 효과를 재생하지 않는다. 영구 서비스인 토리의 골드 보너스는 상시 반복 파티클을 발생시키지 않는다.

동료는 `CompanionAttackStarted`/`CompanionDamaged`라는 별도 이벤트로 처리한다. 플레이어 `EnemyDamaged` 스킬 효과를 재사용하지 않는다. 시작점은 총괄 소유의 `BattlePresentation.Companions.FindActor(id)`가 반환한 배우의 실제 몸 중심이다. 배우가 없으면 임의 위치에서 발사하지 않는다. `Companions.Sample(session)` 뒤 `SkillEffects.Render(session)` 순서를 유지한다.

## 효과와 실제 상태의 대응

| 효과 | 발동·유지 근거 | 그래픽 | 종료 |
|---|---|---|---|
| 루카 바람 베기 | `EnemyDamaged`, 실제 피해>0, `skill_luka_gale_slash` | 플레이어 기본 위치에서 적까지 선택된 사선의 골드·크림·황토 네모 입자, 접촉 방사 | 0.14~0.22초 |
| 다렌 철벽 반격 접촉 | 확인된 `skill_daren_iron_counter` 피해 | 파랑·크림 접촉 조각 | 0.20초 |
| 다렌 보호막 | `SkillShieldMilli > 0` 및 `SkillShieldRemainingMs > 0` | 플레이어 외곽의 성긴 파랑·남색·크림20점. 몸과 위험 방향을 비워 둠 | 실제 흡수·0HP·남은 시간0이면 제거. 자체3초 타이머 없음 |
| 보호막 흡수 | `SkillShieldAbsorbed`, `AbsorbedMilli > 0` | 짧은 파란 파편 | 0.22초, 자연 종료와 중복 발동하지 않음 |
| 보호막 시간 종료 | 실제 활성→비활성 전이 | 작은 파란 조각4개 | 0.18초 |
| 미엘 별빛 봉인 접촉 | 확인된 `skill_miel_star_seal` 피해 | 민트·크림 접촉 입자 | 0.22초 |
| 미엘 기절 누적·준비 | 현재 캐릭터 미엘, 실제 `Stagger`와 `IsStunReady`, 적 생존 | 얼굴 위8개 민트·초록 점의 실제 누적 표시.100 준비이면 크림4점 추가 | 기절 진입·면역·게이지0·적 사망·장면 이탈 |
| 실제 기절 디버프 · 모든 캐릭터 | `IsEnemyStunned`인 실제 기간과 적 생존 | 머리 주위를 도는 작은 민트·크림 도트별3개. 별마다5점, 총15입자 | 실제 기절 끝·적 사망·장면 이탈 즉시 제거 |
| 강화·무기 제작 | 성공한 명령당1회 호출 | 골드·크림·황토 위로 떠오르는 입자 | 0.32~0.40초 UI 시간 |
| 축복 | 예약 성공당1회 호출 | 파랑·크림·남색 입자 | 동일 |
| 실제 회복·약초차 예약 | 실제 회복 이벤트 또는 예약 성공당1회 | 민트·크림·초록 입자 | 동일 |
| 토리 지원 사격 | `CompanionAttackStarted`, ID tori | 골드·황토·크림 네모5개의 작은 화살 | 실제 동료 예비시간 끝에 현재 적 몸 위치 도착 |
| 리나 지원 마법 | 위 이벤트, ID rina | 보라·진보라·크림 네모4개의 바람 방울. 미엘의 민트 봉인과 구분 | 동일 |
| 모모 씨앗 | 위 이벤트, ID momo | 테라코타·갈색·크림 네모4개 | 동일 |
| 동료 접촉 | `CompanionDamaged`, 실제 피해>0 | 각 동료 팔레트의 작은 파편6개. 바룬·도토는 발사체 없이 이 효과만 | 0.16초 |

미엘에게 없는 슬로우·지속 피해·속박을 표현하지 않는다. 기존 흰 피격 플래시는 그대로 둔다. 새 효과에서 피해·기절·힐·스킬20초 쿨타임을 계산하거나 변경하지 않는다. 보호막의 마지막600ms는 점 크기만 작아지며 게임의 보호 성능은 바뀌지 않는다.

실제 기절별은 어떤 캐릭터든 표시하며 SealCapacity24 안에서15개 입자만 사용한다. 기절 누적·준비 표식과 동시에 그리지 않는다. 기절 후 면역만 남은 동안에는 기절별이 없다. 궤도는 Core 전투 시계를 읽어 일시정지·가이드 대기에 같이 정지한다. 총괄은 SkillEffects가 연결된 경우 기존 `View.StunStars`를 숨겨 중복을 없앤다.

동료 발사체는 최대3개만 추적하며 기존 `SkillContact` ParticleSystem의96개 입자 상한을 공유한다. 수명은 카탈로그 `adventure.companionWindupMs`를 읽고 현재 값은180ms다. 날아가는 동안 현재 적 몸 중심을 다시 읽어 이동 중인 적을 향한다. 접촉은 Core의 별도 확정 이벤트가 있어야만 나온다. 사망·스테이지 교체에는 남은 발사체를 지우고, 일시정지·가이드 입력 대기는 전투 시계와 함께 멈춘다. 치명타·기절·스킬 추가 판정을 일으키지 않는다.

## ScreenSpaceOverlay에서 실제로 보이는 방식

현재 캐릭터는 월드 SpriteRenderer지만 로비는 ScreenSpaceOverlay다. 월드 ParticleSystem만 추가하면 불투명한 로비 뒤에 가려진다. 별도 라이브러리나 기존 Canvas 변환 없이 **실제 ParticleSystem을 전용 카메라의 투명 RenderTexture에 그리고 RawImage로 합성**한다.

카메라는 현재 월드와 떨어진 `(16384,16384,-10)`에서 기존 Ignore Raycast 레이어2의 입자만 렌더한다. 기존 월드 카메라·TagManager·SortingLayer는 바꾸지 않는다. 입자는 해당 격리 위치에 world 좌표로 놓이고, 카메라 위치는 Canvas 재배치 뒤에도 다시 고정한다. 기존 프로젝트는 Built-in 렌더 파이프라인(`m_CustomRenderPipeline: 0`)이며 `Camera.Render()`를 사용한다.

`ActorView`의 실제 world 좌표 → 현재 월드 카메라 screen 좌표 → 효과 RectTransform의 local 좌표 → RT texel 좌표로 변환한다. 로비 카드도 자신의 Canvas 카메라로 screen 좌표를 구한다. 따라서 보스 줌·카메라 위치·플레이어 달리기·대상 카드 위치를 고정 화면 좌표로 가정하지 않는다.

RT는 보통 **화면2픽셀당1텍셀**, ARGB32, Point, AA1, mipmap 없음이다. 화면 영역 크기가 변하거나 RT가 손실된 경우에만 다시 만들며, OnDisable/Destroy에서 해제한다. 입자는 회전0의 불투명 정사각형이며 크기는 정수1~3텍셀이다. RT의 홀수 폭·높이에서도 네모의 가장자리가 텍셀 경계에 맞도록 중심을 보정한다. 일반640/1280 가로 화면에서는2·4·6화면픽셀 네모가 된다. 홀수 폭 Safe Area에서의 최종 합성 정렬은 실제 화면 확인 대상이다.

Soft-particle·블룸·그라데이션 발광·화면 전체 섬광을 쓰지 않는다. 알파를 부드럽게 줄이는 대신 각 네모가 정해진 수명에 사라져 투명 RT의 이중 알파 번짐도 줄인다. 네 종류의 고정 입자 버퍼 최대216개이며 매 프레임 입자 GameObject를 생성하지 않는다. seed는73 고정이고 게임 난수 상태에 접근하지 않는다.

입자 위치는 원래의 위치+속도×나이에서 매번 계산하고 `ParticleSystem.SetParticles`로 넣는다. 이어 `Simulate(0)`으로 수동 상태를 갱신한다. 매번 반올림한 위치를 다음 시뮬레이션의 출발점으로 쓰지 않아 느린 입자가 같은 픽셀에 멈추는 누적 오차를 피한다. 실제 ParticleSystemRenderer가 입자를 그리며 UI 메시로 가짜 입자를 생성하지 않는다.

## 시계·입력·가독성

접촉 입자의 시간은 Core 전투 시계 변화량이다. 일시정지·가이드 입력 대기에서는 진행하지 않는다. 확정 막타의 남은 입자만 최대0.24초의 짧은 전환 연출 시간으로 사라진다. 대화나 보스 등장 동안 새 공격 효과를 발동시키지 않는다. 보호막과 봉인 유지 표시도 전투 상태에서만 보이고, 다음 전투에서는 실제 남은 상태를 다시 읽는다.

강화·축복·회복 입자는 UI 시간으로 짧게 소멸한다. 실제 회복이 처치 전환에서 발생해도 멈춘 전투 시계 때문에 화면에 영구히 남지 않는다. RawImage는 raycast를 받지 않아 방향키·공격·스킬·여관 입력을 가로채지 않는다. 화면 흔들림 감소에서는 접촉 입자 수를 줄인다.

## 통합 적용 뒤 한 번 확인할 항목

`DragonV5SkillEffectsMigration.Validate(presenter)`는 저장 참조·카메라·raycast 설정을 확인한다. 실제 렌더 검증을 대신하지 않는다. 완료 묶음의 QA에서 아래를 함께 확인한다.

1. 컴파일 및 씬 저장 후, 다시 열어도4개의 ParticleSystem·카메라·머티리얼·RawImage·BattlePresentation 참조가 남는지.
2. 실제 RT 투명 배경에 검은 사각형·분홍 머티리얼·알파 테두리·상하 반전이 없는지. Pause/Modal이 효과 위에 있는지.
3. 640×360/1280×720에서 골드·파랑·민트 네모가 실제로 보이고 크기가 뭉개지지 않는지. 해상도 변경 이후 위치와 RT가 정상인지.
4. 루카 상·하 참격 방향이 기존 참격과 같은지. 미엘이 면역 중일 때 존재하지 않는 누적·슬로우 표식을 새로 만들지 않는지. 토리·리나·모모의 발사체가 각 배우에서 출발해180ms 예비시간 끝에 적에게 닿는지, 바룬·도토는 확정 접촉 파편만 나오는지.
5. 다렌 보호막이 실제3초 남은 상태와 맞고, 흡수·시간 종료·사망·복귀에서 잘못 남거나 두 번 깨지지 않는지.
6. 적 공격 예고와 HP를 가리지 않는지. 실제 회복·강화 성공에만1회 파티클이 나오는지. 일시정지와 가이드 대기에서 입자가 움직이지 않는지.
7. 로비 카드의 강화 파티클이 패널 뒤로 가려지지 않는지. 취소·재렌더로 반복되지 않는지. 사용 종료·씬 재진입에서 RT 누수가 없는지.

본 담당자는 기존 소스의 이벤트 필드·사선 계약·실제 보호막/기절 속성·Built-in 파이프라인을 읽었고 변경 소스의 참조와 공백을 검토했다. Unity Apply·Play 모드·컴파일·자동 테스트는 총괄의 단일 검증 권한에 따라 실행하지 않았다.

사용 API 근거: [Unity ParticleSystem.SetParticles](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystem.SetParticles.html), [Camera.Render](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.Render.html), [Render Texture Inspector](https://docs.unity3d.com/6000.0/Documentation/Manual/class-RenderTexture.html). 현행 로컬 근거는 `BattlePresentation.cs`, `GameSession.Combat.cs`, `GameSession.Skills.cs`, `EnemyEffectBurst.cs`, `CombatV55Migration.cs`, `GraphicsSettings.asset`이다.
