# Last-Bell

**마지막 종지기** — 가로 화면의 도트 그래픽 타이밍 액션 게임. Unity **6000.3.15f1**, Built-in 2D + uGUI로 제작 중이다.

현재 MVP는 **1~10스테이지**의 튜토리얼 → 전투 → 유물 선택 → 사망 → 로비 강화·장착 → 재도전 흐름을 연결한다. 10층 보상 이후에는 자동 진행과 체크포인트 검증용 11층이 이어진다. 12~50층, 엔딩과 NG+는 기획 단계다.

## Unity에서 실행

1. 이 저장소를 클론한다.
   ```sh
   git clone https://github.com/Hwanggok2/Last-Bell.git
   ```
2. Unity Hub에서 저장소의 **Client** 폴더를 추가하고 Unity **6000.3.15f1**로 연다.
3. `Assets/Game/Scenes/Mvp.unity`를 열고 Play한다. 새 저장에서는 첫 스테이지 튜토리얼이 바로 시작한다.

마우스·터치는 왼쪽 삼각형으로 네 방향 회피, 오른쪽 검으로 공격한다. 키보드는 방향키 또는 WASD로 회피, Space로 공격, Esc로 일시정지한다.

## 저장소 구성

| 경로 | 내용 |
| --- | --- |
| [Client](Client/README.md) | Unity 프로젝트, 저장된 씬·프리팹, 런타임·에디터 코드, 아트 및 테스트 |
| [GameData](GameData/README.md) | 게임 데이터 편집 원본, 내보내기 도구 및 독립 검증 |
| [Planning](Planning/index.html) | 게임 기획, 와이어프레임 및 검토 기록 |
| [Planning/Production](Planning/Production/index.html) | 실제 제작 화면, 캐릭터 초안, 5개 구역·50종 제작 계획 |
| [Skills](Skills) | 프로젝트 구조와 게임 UI·문구 제작 스킬 |

HTML 기획서는 클론 후 브라우저에서 연다. 현재 제작 범위와 검증 결과는 [제작 현황](Planning/Production/implementation-status.md)에 기록한다. 일부 과거 검토 문서의 절대 경로는 제작 PC 기준이다.

## 데이터와 검증

편집 원본은 `GameData/mvp-catalog.json`이다. 변경 후 저장소 루트에서 실행한다(Python 3.11 이상, 외부 패키지 없음).

```sh
python GameData/export_catalog.py
python GameData/export_catalog.py --check
python -m unittest discover -s GameData/tests -p "test_*.py" -v
```

생성본 `Client/Assets/Game/Data/Generated/mvp-catalog.json`은 원본과 함께 커밋한다. 원본 해시가 바이트 기준이므로 `.gitattributes`가 줄바꿈을 LF로 유지한다. Unity에서는 Test Runner의 EditMode 테스트를 실행한다. 선택적인 독립 C# 검증 방법은 [데이터 안내](GameData/README.md)를 참고한다.

## 빌드와 현재 범위

Unity 메뉴 `Last Bell/Build/Windows MVP` 또는 `Last Bell/Build/Android Test APK`로 개발 빌드를 만든다. Android 빌드에는 Unity Hub의 Android Build Support 모듈이 필요하다. 빌드 결과와 Unity 캐시, 개인 설정, 테스트 저장 파일은 Git에서 제외한다.

현재 플레이어 1명, 일반 적 3종·중간 보스 1종·보스 1종, 유물 10종을 구현했다. 아트는 대표 자세를 사용하는 초안이며 전체 50종의 완성 애니메이션은 후속 범위다. 실제 유료 결제·광고·서버 검증은 포함하지 않는다. Android 실기기 검증과 원스토어 제출도 후속 작업이다.

폰트의 라이선스는 [NanumGothic OFL](Client/Assets/Game/Art/Fonts/OFL.txt)을 따른다. 생성 아트의 출처와 적용 범위는 [아트 제작 기록](Planning/Production/generated-asset-notes.md)을 참고한다.
