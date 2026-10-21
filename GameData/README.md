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
- 스테이지는1..10과11 경계 표본이다.50층/NG+ 적 콘텐츠 완성을 뜻하지 않는다. 스테이지별 기본 골드51개(0은 센티널)와 교환/MAX5개 구간은 준비하지만 미구현 층을 자동 생성하지 않는다.
- `transitions`는 전투 시계와 분리된 Core 연출 시간이다. 처치550ms→이동2000ms→도착700ms(정착200+Stage500), 보스 등장·사망은 해당 설정을 사용한다. `stories`는 `Planning/story.json`의10층 확정 대사를 옮긴 실행 원본으로, 자동 덮어쓰지 않는다. 읽기 진행·캐릭터별 최초 표시 여부는 저장 상태가 소유한다.
- 현재 카탈로그 `mvp-1.3.0`은 공격 명중200ms/재공격500ms다. 카탈로그 schema2는 스테이지별 골드·구간별 교환율을 기록한다. 저장 schema3는 골드·가격·보상·견적을 Int64로 보존한다. schema1/2에서 기존 레벨·잔액·확정 보상을 보존하고 오래된 교환 견적만 폐기한다. 전환 경과·대사·보상 후보는 유지한다. schema1의 기존 소유 유물과 업데이트 전 확정된 미선택 중간 보스 보상1회는 유지하며, 이후 새 처치는 보스만 유물을 지급한다.

## 순서 있는 패턴

`patternSteps`는 순서 있는 공격 시퀀스다. `comboStep=0`은 단일,1은 연타 시작,2이상은 같은 연타의 후속이다. 기존 보스3연타까지 유지한다. v5.9의 `telegraphMs`는 기본540/빠름480, 한 박자는180/160ms다. 각 박자의 전반에 밝게 점멸한다. **모든 영역의 세 번째 밝음이 끝난 다음** 마지막 반 박자90/80ms 동안 첫 무기가 날아와 다음 박자에 타격한다. `attackFlightMs=90`, `fastAttackFlightMs=80`. 같은 연타는 같은 속도를 사용한다.

두 타격의 기본 시각은1080/1440ms, 빠른 시각은960/1280ms다. 보스3타격은1440/1760/2080ms다. 연타 간격은 두 박자인 `comboDefaultGapMs=360` / `comboMinGapMs=320`으로 정하고, 후속 영역을 다시 예고하지 않는다. `intervalMs`는 이전 시퀀스 마지막 타격과 다음 시퀀스 첫 타격 사이의 최소 간격이며, 전체 예고를 담을 시간이 부족하면 전체 예고 길이를 우선한다. 후속 원소의 `intervalMs`도 같은360/320으로 기록한다. 내보내기는3박자 예고·반 박자 비행·두 박자 연타의 관계를 검증한다.

`perfectCounterMs=500`, 일반160/완벽60ms, 회피 재사용260ms, 공격 접촉200/재사용500ms다. 확정된 완벽 회피 후 유효한 공격만 즉시 복귀 반격이 된다. 쿨타임이나 추가 피해를 부여하지 않는다.

위험/대응은 `low_sweep→up`, `thrust→back`, `high_sweep→parry`, `outer_arc→forward`다. 기존 필드 이름 `requiredDodge`/`teachDodge`를 유지하면서 상단 응답은 `parry`로 기록한다. 상단은 공격 버튼으로 쳐내며 숙이기 입력은 없다. 방어 재사용260ms를 공유하고 일반 공격500ms 쿨타임과 독립적이다. 첫1~4층은 각 대응을 첫 타격으로 소개한다. 최초 회피→공격 튜토리얼의 완료 상태는 GameSession이 소유한다.
