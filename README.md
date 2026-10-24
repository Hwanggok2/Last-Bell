# Last-Bell

**용 잡으러 갑니다**(가칭) — 가로 화면의 도트 그래픽 타이밍 액션 게임. Unity **6000.3.15f1**, Built-in 2D + uGUI로 제작 중이다. 저장소와 저장 경로의 Last-Bell 식별자는 유지한다.

현재 **v0.9.6 / mvp1.18.0**은 **1~20스테이지 MVP**다. 루카·다렌·미엘의 독립 20초 스킬, 동료 5종의 확률 스킬과 강화, 기존 여관 배치 복원, 도트 HP·피해 숫자와 큰 피해의 주황 잔상을 연결했다. 새 계정은 토리2·바룬12를 만나며, 이전 저장의 리나·도토 해금을 유지한다. **21~50 전투와 모모42 첫 만남은 후속 제작**이다. [현재 적용 기록](Planning/Production/dragon-v6-integration.json)을 기준으로 본다.

<!-- dragon-v6-publication:start -->
일반 실행판: `Builds/Dragon-v6/LastBellMvp.exe` · [변경 기획서](Planning/Story/Dragon-v1/Adventure-v6/index.html)

기존 v6 변경은1280·640 관측 및 재실행을 확인했고, HP 잔상은15개 Unity PlayMode 검사와 두 해상도 실제 피해 화면을 추가 검증했다. 관측판의 표적 QA이며 전체 인수·모바일 실기기 인증은 아니다. 일반판은 패키지 무결성을 확인했다.

[v0.9.6 ZIP](Builds/Dragon-v6-Windows-0.9.6.zip) · [최신 QA](Planning/QA/dragon-v6-candidate05-result-20261025.md) · [배포 기록](Planning/Production/dragon-v6-release.json)
<!-- dragon-v6-publication:end -->

<details><summary>이전 v0.9.4 배포 기록</summary>

<!-- dragon-v5-publication:start -->
일반 실행판: `Builds/Dragon-v5/LastBellMvp.exe` · [v0.9.4 ZIP](Builds/Dragon-v5-Windows-0.9.4.zip)

**확인된 QA 범위:** candidate02의 두 해상도 통합·재시작 4프로세스 및 영향 없는 시각 관측을 채택했습니다. candidate03에서는 바룬의 Stage12 대화만 3캐릭터·2해상도, 30페이지로 재검증해 초상화 오류를 해결 확인했습니다.

통합 기록: Core 217 assertions / 데이터 28 tests / 실제 Unity EditMode 198 passed. 최종 화면 campaign은 별도 2개 프로세스이며 이전 후보·채택 증거를 합산한 숫자가 아니다.

- 전체60개 기준 인증이 아닙니다. 일반 실행판은 패키지 무결성을 확인했으며 별도 전체 플레이 검사는 하지 않았습니다.
- OS 실입력·모바일 실기기·직접 청취·정상 속도 성능 검사는 미실시입니다.
- 기절 입자는 실제 상태에 맞춰 표시·종료되지만 작은 화면에서 별 모양과 순간 식별성은 추가 검토 대상입니다.

[최종 QA](Planning/QA/dragon-v5-result-20261024.md) · [실행 기록](Planning/Evidence/Dragon-v5/dragon-v5-candidate-03/campaign.json) · [배포 기록](Planning/Production/dragon-v5-release.json)

일반 배포판은 패키지 검증이며, 관측용 실행판의 표적 QA와 별도 전체 플레이 검증을 구분한다.
<!-- dragon-v5-publication:end -->

</details>


실제 실행 화면과 최신 기획은 [Dragon v1 제작 현황](Planning/Story/Dragon-v1/index.html#unity-build), 상세 적용·검증 기록은 [Client 안내](Client/README.md)를 기준으로 본다. [새 공격음 듣기](Planning/Story/Dragon-v1/Audio-v2/index.html)에서 실제 연결한 CC0 타격음·터치음을 비교할 수 있다.

## Unity에서 실행

1. 이 저장소를 클론한다.
   ```sh
   git clone https://github.com/Hwanggok2/Last-Bell.git
   ```
2. Unity Hub에서 저장소의 **Client** 폴더를 추가하고 Unity **6000.3.15f1**로 연다.
3. `Assets/Game/Scenes/Mvp.unity`를 열고 Play한다. 새 저장은 프롤로그와 캐릭터 선택·강화를 거쳐 첫 전투 튜토리얼로 이어진다.

마우스·터치는 왼쪽 화살표로 **위·뒤 회피 + 아래 쳐내기**, 오른쪽 검으로 공격한다. 키보드는 위/W, 왼쪽/A, 아래/S와 공격 Space, 스킬 E, 일시정지 Esc다. 전진 회피는 비활성이다. 방향키 위치는 레퍼런스 실측값이며 기존 회피·쳐내기 조작을 유지하며, v0.9.4에서 동료·회복력·여관 거래·저장 이전을 추가했다.

## 저장소 구성

| 경로 | 내용 |
| --- | --- |
| [Client](Client/README.md) | Unity 프로젝트, 저장된 씬·프리팹, 런타임·에디터 코드, 아트 및 테스트 |
| [GameData](GameData/README.md) | 게임 데이터 편집 원본, 내보내기 도구 및 독립 검증 |
| [Planning](Planning/index.html) | 게임 기획, 와이어프레임 및 검토 기록 |
| [Planning/Production](Planning/Production/index.html) | 실제 제작 화면, 캐릭터 초안, 5개 구역·50종 제작 계획 |
| [Skills](Skills) | 프로젝트 구조와 게임 UI·문구 제작 스킬 |

HTML 기획서는 클론 후 브라우저에서 연다. 최신 제작 범위는 [Dragon 적용 기록](Planning/Production/dragon-v6-integration.json), 이전 단계 기록은 [제작 현황 보관본](Planning/Production/implementation-status.md)에 있다. 일부 과거 검토 문서의 절대 경로는 제작 PC 기준이다.

## 데이터와 검증

편집 원본은 `GameData/mvp-catalog.json`이다. 변경 후 저장소 루트에서 실행한다(Python 3.11 이상, 외부 패키지 없음).

```sh
python GameData/export_catalog.py
python GameData/export_catalog.py --check
python -m unittest discover -s GameData/tests -p "test_*.py" -v
```

생성본 `Client/Assets/Game/Data/Generated/mvp-catalog.json`은 원본과 함께 커밋한다. 원본 해시가 바이트 기준이므로 `.gitattributes`가 줄바꿈을 LF로 유지한다. Unity에서는 Test Runner의 EditMode 테스트를 실행한다. 선택적인 독립 C# 검증 방법은 [데이터 안내](GameData/README.md)를 참고한다.

## 빌드와 현재 범위

현재 테마의 부분 저장 자산·검증·Windows 빌드는 `Last Bell/Adventure v6` 메뉴와 `DragonAdventureMigration`을 사용한다. 구 전체 마이그레이션은 이전 테마로 바꿀 수 있다. 최신 일반 실행판은 `Builds/Dragon-v6/LastBellMvp.exe`, 공유 묶음은 `Builds/Dragon-v6-Windows-0.9.6.zip`이다. Android 빌드에는 Unity Hub의 Android Build Support 모듈이 필요하다. 빌드 결과와 Unity 캐시, 개인 설정, 테스트 저장 파일은 Git에서 제외한다.

이번 Windows 실행판은 `persistentDataPath/Playtests/dragon-0.9.2`의 기존 Dragon 진행을 이어간다. 첫 실행은 기존 프롤로그·선택·강화 후 **Stage1 튜토리얼**로 이어지고 이후 실행은 이 새 진행을 복원한다. 기존 사용자 저장을 삭제하거나 매 실행마다 초기화하지 않는다. 명시적인 `-lastbell-save-dir`이 우선한다. Editor 및 해당 define이 없는 빌드는 기존 저장 경로를 유지한다.

현재 플레이어1명, 초원5체형을 사용하는10층과 숲10종을 사용하는10층, 유물10종을 구현했다. 루카 원본52장·숲30클립235샘플을 연결했고 아래 쳐내기는 베기 원본을 재사용한다. 전체50종의 완성 애니메이션은 후속 범위다. 실제 유료 결제·광고·서버 검증은 포함하지 않는다. Android 실기기 검증과 원스토어 제출도 후속 작업이다.

폰트의 라이선스는 [NanumGothic OFL](Client/Assets/Game/Art/Fonts/OFL.txt)을 따른다. 생성 아트의 출처와 적용 범위는 [아트 제작 기록](Planning/Production/generated-asset-notes.md)을 참고한다.
