# 최신 규칙: mvp1.13.0 / feedback-v8.6

1~20 스테이지에 서로 다른 적 ID와 artId를 하나씩 배치한다. 21은 미공개 경계 표본이므로 이 중복 검사에서 제외한다. 성 밖 9종 야생동물 + 10층 문지기, 주거구 9종 + 20층 간수. 기존 적 ID는 삭제하지 않아 예전 기록과 아트를 보존한다.

몬스터 배치 외의 HP·피해·타이밍·강화·보상·유물·스토리·저장 계약은 v8.4/v8.5와 동일하다. 프레젠테이션 v8.6은 밝은 칩튠, 짧은 공격음, 입력 순간 회피음, 종 오디오 제거를 적용한다. 신규 6종과 복원 6종 Idle이 추가된다.

단일 편집 원본은 `mvp-catalog.json`이며 내보내기와 Unity 양쪽에서 적 ID/아트 ID 중복을 검증한다. 생성 파일 직접 수정 금지.

---

# 최신 규칙: mvp1.12.0 / feedback-v8.4

표현 전용 v8.5(캐릭터 선택 Idle, 몬스터 그래픽/Idle)는 게임 데이터와 저장 규칙을 변경하지 않는다. 런타임 빌드 버전은 0.8.5이며 카탈로그 버전은 위와 같다.

보스 파편0/4, 세 방향 회피,900ms 예고·750ms 연속 타격, 유물 정수 퍼센트를 반영했다. 강화 비용·별점 성장·1~20스테이지 HP/피해는 이전 균형을 유지한다. 생성본 직접 수정 금지.

유물은 전체 보유 효과가 자동 적용되며 `relicRules.initialCapacity=3`, `maxOwned=10`이 서로 다른 종류의 보관 한도를 정의한다. `economy.storageExpansionCosts=[4,8,12,18,26,36,48]`은 3→4부터 9→10까지 종 파편 비용이다. 기존 `maxEquipped`, `slotCosts`, `slotUnlockStages` 계약은 폐기했다. 중복 Lv.업은 새 칸을 쓰지 않는다. 저장 schema8은 구 구매 횟수와 모든 소유 유물을 보존한다.

기절은 수치상 임계값 도달 후 공격 명중 시 발동한다. 6종 아트·프롤로그·10/20층 대사는 갱신했으며 기존 성장/전투 타이밍/HP/피해는 유지한다.

현재 규칙 검사:

```
python -m unittest discover -s GameData/tests -p test_export_catalog.py
python GameData/export_catalog.py --check
dotnet run --project GameData/tests/CatalogSmoke.csproj
dotnet run --project GameData/tests/ReferenceV81/ReferenceV81.csproj
```

`SessionContractReview`는 이전 즉시 보상 규칙의 검토 기록이며, 현재 종료 후 정산 규칙은 `ReferenceV81`과 Unity EditMode/PlayMode 테스트를 기준으로 확인한다.

---

# 최신 기준: v7 / mvp1.9.0 / 2026-10-23

현재 플레이 범위는 1–20층이며 21층은 경계 표본이다. 아래 과거 버전 기록과 충돌하면 이 절과 `mvp-catalog.json`이 우선한다. 전체 설계·별점별 표·근거는 `Planning/Presentation/feedback-plan-v7.html`, 50층 확장 계산 표는 `Planning/Balance/balance-v7.json`이다.

- 목표 Lv.1–10 가격은 기존 `ceil(10 × 1.25^(L−1))`를 유지한다. 이후 목표 레벨 구간마다 직전 정수 가격에 +30/35/40/45% 후 올림. Lv.11/21/31/41은 직전 비용 ×3/3.5/4/4.5만 적용한다. 11=225G, 20=2,399G, 21=8,397G, 50=1,318,008,419G. 4개 능력치 총비용은 16,715,773,056G로 Int64가 필요하다.
- 1–5성 공격/HP 1구간 매회 증가: 2/3/4/5/6 및 12/18/24/30/36. 기본값+선형 증가에 별점별 돌파 배율을 적용하고 최종 정수 반올림한다.
- 행운은 `luckCritPerTenLevels`의 정수 %p를 10레벨에 분산하고 돌파마다 별점만큼 추가한다. 골드는 `luckGoldPerLevel`에 따라 매회 오르므로 치명타가 오르지 않는 단계도 실제 이득이 있다. 공명은 `resonancePerLevel × (L + 10×돌파횟수)` %p. 이전 실수형 luck/resonance 증가 계수는 제거했다.
- 적 기본 HP는 `round(120 × 10^구역 × 1.13^구역내위치)`에 일반/중간/보스 1/1.5/2.4. 공격 구역 시작값 8/180/900/4000/16000, 첫 구역 매층 ×1.18, 이후 ×1.12, 등급 1/1.1/1.25 후 최종 반올림. 20층 HP8652/피해624. 21층 이후는 확장 기획으로 실제 플레이를 열지 않는다.
- 일반 골드는 같은 목표 레벨 비용의 구역별 200/80/60/50/40% 올림, 중간×3/보스×5. 기존 확정 보상의 지급액과 저장 스냅샷은 보존한다. 새 도전에서 새 성장식을 적용한다.
- 프롤로그는 4문단·18000ms, 자동 스크롤/문단 읽기/건너뛰기를 유지한다.

---

# MVP 데이터 편집과 내보내기

단일 편집 원본은 `mvp-catalog.json`이다. 최초 값은 planning-v4에서 옮겼으며, 이후 이 JSON을 직접 편집한다. 내보내기는 기획 문서에서 값을 다시 덮어쓰지 않는다. Excel 변환은 이번 MVP에 포함하지 않는다.

프로젝트 루트에서 실행한다(Python 3.11 이상, 외부 패키지 없음).

```powershell
python GameData/export_catalog.py
python GameData/export_catalog.py --check
python GameData/export_catalog.py --validate-only
python -m unittest discover -s GameData/tests -p 'test_*.py' -v
dotnet run --project GameData/tests/CatalogSmoke.csproj --no-launch-profile
```

`--check`는 원본 검증과 생성물 일치 여부만 확인하며 파일을 쓰지 않는다. 검증 실패 시 기존 생성물을 보존하고 JSON 경로가 포함된 오류를 반환한다. 성공 시 같은 디렉터리의 임시 파일을 원자적으로 교체한다. 생성 결과가 같으면 파일과 수정 시각을 유지한다. C# 독립 검사는 설치된 .NET 10 SDK를 사용한다. Unity 빌드에는 .NET 10 의존성이 없으며 카탈로그 코드는 C#7.3 문법으로도 컴파일한다.

생성물은 `Client/Assets/Game/Data/Generated/mvp-catalog.json`이다. 수동 편집하지 않는다. 루트의 `schemaVersion`, `dataVersion`, `sourcePlanningVersion`, `sourceSha256`, `generatedNotice`가 버전·출처·원본 해시를 기록한다. 해시는 원본 UTF-8 바이트 기준이다. JSON 최상위·중첩 필드의 오타, 중복 키/ID, 잘못된 참조·범위·패턴은 내보내기 전에 차단한다.

Unity에서는 TextAsset을 읽어 `JsonUtility.FromJson<GameCatalog>(text)` 후 `ValidateOrThrow()`를 호출한다. 모든 직렬화 필드는 public이며 배열·기본형·serializable class만 사용한다. Dictionary는 로드 후 비직렬화 조회 캐시로 만든다. DTO/API 전체는 `Planning/mvp-implementation-contract.md`의 데이터 절과 `GameCatalog.cs`에 있다.

## 단위와 편집 경계

- ID는 영문 lower_snake_case로 유지한다. 표시 이름과 `artId`는 별도다. 아트를 바꿔도 적ID·저장된 유물ID를 바꾸지 않는다.
- 캐릭터 별 배열 순서는 공격/HP/행운/공명이며 강화 레벨이 아니다. 온만 `availableInMvp=true`, 담/린은 지원 데이터다.
- 시간은 정수ms, 비율은0..1, 원본 HP는HP 단위다. 실행 상태는 `hpUnitsPerPoint=1000`인 milli-HP로 저장한다. 실제HP·쿨타임·소유 유물 레벨은 이 파일에 저장하지 않는다.
- `upgradeCosts[0]=0`, `[1]..[50]`은 도달 레벨의 구매 비용이다. Lv.11/21/31/41은 큰 돌파다. 가격은 `ceil(10×1.25^(L−1)×2^floor((L−1)/10))`을 한 번만 올림한다. 매회 약25%, 돌파 약2.5배다. 스테이지S 기본 골드는 같은 도달 레벨 가격의2배이며 중간×3/보스×5를 적용한다.
- 유물 `values[0]..[4]`는 Lv.1..5의 **총효과**다. 레벨별 값을 누적 합산하지 않는다. 10단위 `kind=boss`에서만 전체10종 중 서로 다른3후보를 뽑으며 MAX도 포함한다. 중간 보스는 골드·종 파편만 지급한다.
- 스테이지는1..20과21 경계 표본이다.50층/NG+ 적 콘텐츠 완성을 뜻하지 않는다. 스테이지별 기본 골드51개(0은 센티널)와 교환/MAX5개 구간은 준비하지만 미구현 층을 자동 생성하지 않는다.
- `transitions`는 전투 시계와 분리된 Core 연출 시간이다. 처치550ms→이동2000ms→도착700ms(정착200+Stage500), 보스 등장·사망은 해당 설정을 사용한다. 현재 `stories`의 단일 실행 원본은 이 카탈로그이며, 2·7·15 만남과 10·20 보스 뒤 대사를 담는다. `kind`, `portraitId`, `player` 화자와 캐릭터별 `responses`를 검증한다. 기획 대화 미리보기는 이 데이터에서 생성하고, 읽기 진행·캐릭터별 최초 표시 여부는 저장 상태가 소유한다.
- `mvp1.16.0`의 `skills`는 선택 가능한 on/dam/rin 각각 한 개의 전용 기술이다. `cooldownMs=20000`, 실제 접촉 시 피해 배율·보호막·기절 누적을 적용한다. Core가 수락·취소·전투 시간·저장 복원을 소유하며 UI는 남은 시간만 표시한다. 11스테이지의 `frontal_dash` 단일 패턴 풀은 해당 스테이지만 허용하고, 뒤 회피와 기존 공격 시각·수치를 유지한다.
- 현재 카탈로그 `mvp-1.5.0`은 공격 명중200ms/재공격500ms다. 카탈로그 schema2는 스테이지별 골드·구간별 교환율을 기록한다. 저장 schema3는 골드·가격·보상·견적을 Int64로 보존한다. schema1/2에서 기존 레벨·잔액·확정 보상을 보존하고 오래된 교환 견적만 폐기한다. 전환 경과·대사·보상 후보는 유지한다. schema1의 기존 소유 유물과 업데이트 전 확정된 미선택 중간 보스 보상1회는 유지하며, 이후 새 처치는 보스만 유물을 지급한다.

## 패턴 호환 데이터와 예고 시간

`patternSteps`는 이전 저장/카탈로그 호환용 시퀀스다. 최신 실행은 `randomPatterns`와 `sequenceWeights`로 묶음 전체를 먼저 뽑아 예고와 판정에 같은 순서를 사용한다. `comboStep=0`은 단일,1은 연타 시작,2이상은 같은 연타의 후속이다. 기존 보스3연타까지 유지한다. v5.9.1의 `telegraphMs`는 기본/빠름 모두720, 한 박자는240ms다. 밝음/흐림 각120ms씩0·240·480ms에 점멸한다. **모든 영역의 세 번째 밝음이 끝난 다음** 마지막 반 박자120ms 동안 첫 무기가 날아와 다음 박자에 타격한다. `attackFlightMs=120`, `fastAttackFlightMs=120`.

2연타는1440/1920ms, 보스3연타는2160/2640/3120ms다. 연타 간격은 두 박자인 `comboDefaultGapMs=480` / `comboMinGapMs=480`이며 후속 영역을 다시 예고하지 않는다. `intervalMs`는 이전 시퀀스 마지막 타격과 다음 시퀀스 첫 타격 사이의 최소 간격이며 전체 예고 시간이 우선한다. 후속 원소의 `intervalMs`도480으로 기록한다. 내보내기는3박자 예고·반 박자 비행·두 박자 연타 관계를 검증한다.

`perfectCounterMs=500`, 일반160/완벽60ms, 회피 재사용260ms, 공격 접촉200/재사용500ms다. 확정된 완벽 회피 후 유효한 공격만 즉시 복귀 반격이 된다. 쿨타임이나 추가 피해를 부여하지 않는다.

위험/대응은 `low_sweep→up`, `thrust→back`, `high_sweep→parry`, `outer_arc→forward`다. 기존 필드 이름 `requiredDodge`/`teachDodge`를 유지하면서 상단 응답은 `parry`로 기록한다. 상단은 아래 회피 버튼·↓/S로 쳐낸다. 기존 네 방향 회피 중 아래 방향의 모션이 제자리 검 쳐내기이며 공격 버튼과 분리된다. 방어 재사용260ms를 공유하고 일반 공격500ms 쿨타임과 독립적이다. 최초 튜토리얼의 첫 타격은 학습 방향으로 고정하고, 이후 일반 전투는 허용 풀에서 무작위로 고른다. 최초 회피→공격 튜토리얼의 완료 상태는 GameSession이 소유한다.

## 프로토타입 종료 범위

최신 실제 플레이는 `mvpLastStage=20`에서 종료한다.10층은 대사·소멸·보상 뒤11층으로 진행하며20층 보상 뒤 클리어 결과를 표시한다. `boundaryStage=21`은 검증용이다. schema5 이하의 옛10층 클리어/11층 경계 진행은 재화·강화·유물을 보존한 로비로 이전하며 새 도전은1층에서 시작한다.

## v4 동선·온보딩 데이터

`mvp-1.7.0`은 `onboarding`에 프롤로그 제목·8문단·28000ms와 5스테이지 안내 문구를 보관한다. 1~10 적과 공격 순서를 승인된 route-onboarding-v4에 맞췄으며 HP·피해·성장 비용·보상 공식은 유지한다. 구형 적 ID는 기존 데이터 호환을 위해 남겨 두었다. 11 경계 표본은 여전히 플레이에 진입하지 않는다. 저장 schema5는 첫 읽기/안내 확인 여부를 저장한다. 기존 schema4는 진행을 끊지 않도록 프롤로그를 읽은 상태로 승격하며, 로비에서 다시 볼 수 있다.

## 2026-10-22 v6 통합

mvp1.8.0은 15종·20개 플레이 층, 스테이지별 randomPatterns/sequenceWeights, 기절 속도·저항·연속 보너스·연장 수치를 원본으로 관리한다. 저장 schema6의 randomSequence/sequenceCursor/patternRandomState는 보상 RNG와 분리되며 복원 시 미해결 공격만 온전한 예고로 재개한다.10층 완료 구형 저장은 재화를 보존하고 로비로 이전한다. 최신 규칙은 이전 버전 기록보다 우선한다.
