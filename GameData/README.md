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
- `upgradeCosts[0]=0`, `[1]..[50]`은 도달 레벨의 구매 비용이다. Lv.11/21/31/41은 큰 돌파다.
- 유물 `values[0]..[4]`는 Lv.1..5의 **총효과**다. 레벨별 값을 누적 합산하지 않는다. 전체10종에서 서로 다른3후보를 뽑으며 MAX도 포함한다. 드랍층 필드는 없다.
- 스테이지는1..10과11 경계 표본이다.50층/NG+ 적 콘텐츠 완성을 뜻하지 않는다. 구역별 경제5개 값은 유지하지만 미구현 층을 자동 생성하지 않는다.

## 순서 있는 패턴

`patternSteps` 배열은 반복되는 타격 순서다. `intervalMs`는 **직전 타격에서 이번 타격까지**의 시간이며 배열 마지막→첫 원소에도 적용한다. 전투 최초 타격만 첫 원소의 `telegraphMs` 뒤에 시작한다.

`comboStep=0`은 단발(650ms),1은 연타 시작 전체 예고(900ms),2이상은 그 연타의 후속 타격(250ms 재강조)이다. 후속 간격은 최소450ms이며 예고와 양립해야 한다. 연타 시작은 반드시2가 뒤따라야 하고 후속 번호는 연속이어야 한다. 후속마다650ms를 별도로 기다리지 않는다.

위험/대응은 `low_sweep→up`, `thrust→back`, `high_sweep→down`, `outer_arc→forward`다. 첫1~4층은 각 방향을 첫 타격으로 소개하며 `teachDodge`도 같은 방향이다. 최초 회피→공격 튜토리얼과 이후 방향 학습의 완료 상태는 GameSession이 소유한다.
