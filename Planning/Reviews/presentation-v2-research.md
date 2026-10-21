# 패럴랙스·오디오 조사 검토 v2

2026-10-21 KST · 조사 담당 · 기획 전용. `unity-project-foundation`의 기존 구조·상태 소유·검증 경계에 맞춰 검토했다. Unity·GameData·음원 파일은 변경하거나 다운로드하지 않았다.

## 1. 현재 프로젝트와 배경 권고

실제 프로젝트는 Unity6000.3.15f1, Built-in2D다. `MvpSceneBuilder.cs:43`의 카메라는 Orthographic, size3.1이고, `MvpPresenter.ApplyArt`는 합성 배경 PNG 한 장을 표시한다. 현재 `Packages/manifest.json`에는 Cinemachine·Pixel Perfect·URP 패키지가 없다. 기존 UI는 Screen Space Overlay라 월드 카메라 확대와 분리할 수 있다.

사용자가 기억한 깊이별 이동은 **패럴랙스**다. Perspective 카메라에서는 서로 다른 깊이의 평면이 가로 이동 시 다르게 투영되지만, Orthographic은 원근을 제거한다. 따라서 현재 카메라에서 배경의 z만 달리 놓아도 원하는 속도 차이는 생기지 않는다. 이 결론은 Unity의 투영 설명을 현재 프로젝트의 가로 이동에 적용한 판단이다. [Unity6.3 카메라 개요](https://docs.unity.com/en-us/engine/6000.3/manual/cameras/overview)

| 방식 | 얻는 효과 | 이번 범위의 판단 |
|---|---|---|
| 직교 카메라 +2D 레이어별 이동량 | 멀수록 천천히 흐르는 배경. 인물 크기·판정 표시를 일정하게 유지 | MVP 권고. 필요한 깊이감을 작은 변경으로 만들 수 있음 |
| Perspective+서로 다른 z의 평면 | 실제 투영에 따른 자연스러운 원근·이동차 | 향후2.5D 후보. 스프라이트 크기·카메라 거리·화면좌표·픽셀 배율을 다시 검증해야 함 |
| 직교+z값만 분리 | 앞뒤 겹침·정렬 용도 | 이동차가 자동 발생한다고 기획하지 않음 |

## 2. 레이어와 이동 계약

다음 값은 공식 문서의 수치가 아니라 **이 게임을 위한 시작 제안값**이다. 전진거리D를 화면/월드 중 한 단위로 일관되게 관리하고, 고정 카메라 기준 레이어 위치는 `x = initialX - D × ratio`로 계산한다. 카메라까지 움직인다면 월드좌표 보정과 이 수식을 동시에 적용해 두 번 움직이지 않게 한다.

| 레이어 | 예시 내용 | D에 대한 화면 이동 비율 | D=100px 예시 |
|---|---|---:|---:|
| 하늘 | 달·하늘, 반복하지 않는 랜드마크 |0.02|왼쪽2px|
| 원경 | 먼 성벽·첨탑 |0.08|왼쪽8px|
| 중경 | 중경 아치·성문 |0.22|왼쪽22px|
| 근경 | 가까운 기둥 |0.48|왼쪽48px|
| 바닥 | 발밑 돌길·전경 가장자리 |1.00|왼쪽100px|
| UI | HP·예고·버튼 |0|화면 고정, 월드 추적 요소만 정상 재투영|

최종 통합안 `Planning/Presentation/proposal.json`과 대조해 **하늘을 원경에서 분리한 5겹 구성**에 동의한다. 전진 한 구간은 650ms에 기준 거리 160px이며, 하늘/원경/중경/근경/바닥의 이동량은 각각 3.2/12.8/35.2/76.8/160px다. 이는 1280×720 기준 기획 좌표다. 실제 표시 위치를 픽셀 격자에 맞추더라도 누적 전진 거리 자체를 프레임마다 정수로 버리지 않아야 느린 레이어가 완전히 멈추는 오류를 피할 수 있다. 스테이지마다 D를 초기화하지 않고 누적하고, 반복 레이어는 이음매가 없는 지점에서만 감싼다.

전진 구간에서만 D를 늘리고 전투에 정착하면 멈춘다. 카메라 줌은 등장 연출 소유자가, 수평 패럴랙스는 이동 연출 소유자가 처리한다. 보스 포효를 위해 줌인하는 동안 원경이 계속 흘러 전진 중처럼 보이지 않게 한다. 모션 감소에서는 횡스크롤을 제거하고 전환 길이·입력 복귀 시점은 유지한다.

레이어의 앞뒤는 Sorting Layer와 Order in Layer로 명시한다. z거리와 같은 정렬값의 우연한 순서에 의존하지 않는다. 여러 파츠를 쓰는 보스는 Sorting Group이 검토 대상이다. [Unity2D 정렬](https://docs.unity3d.com/kr/6000.0/Manual/2d-renderer-sorting.html), [Sorting Group](https://docs.unity3d.com/kr/6000.0/Manual/sprite/sorting-group/use-sorting-groups.html)

현재 합성 배경 한 장을 줄별로 잘라 움직이는 것은 완성 패럴랙스가 아니다. 하늘/원경/중경/근경/바닥의 레이어 5장을 새로 준비하고, 하늘 외 레이어는 투명 배경으로 분리한다. 가려졌던 영역을 복원하며 양 끝 반복 이음매와 보스 확대용 여백을 확보한다. 달이 여러 개 반복되지 않게 하늘 랜드마크는 반복 바닥과 별도 취급한다. 프리팹에 각 레이어·비율·반복폭을 저장하고 Play 때 UI나 전체 배경 계층을 재생성하지 않는다.

## 3. 크게 줌인할 때의 도트 처리

Orthographic 카메라는 z로 앞당기는 대신 `orthographicSize`를 줄여 줌인한다. 기본값3.1에서 목표화면배율m이면 후보size는 `3.1/m`이다. 크기 확대와 카메라 배율을 곱한 결과로 실제 보스 프레이밍을 검수해야 한다. [Camera.orthographicSize](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/camera/orthographicsize)

연속 줌이 모든 프레임에서 정수 픽셀 배율을 유지한다고 약속하지 않는다. Point 필터만으로 모든 해상도의 Pixel Perfect가 보장되지는 않는다. 권고는 전투 기본 구도와 포효 유지 구도에서 픽셀 배치를 안정시키고, 짧은 줌 이동 중의 배율 변화는 시각 연출로 받아들이는 것이다. 첫 공격 전 기본 구도·픽셀 격자로 완전히 복귀한다. 저해상도 RenderTexture+Point 확대나 Pixel Perfect 도입은 구현 검토 항목이며 아직 설치된 기능이 아니다. [Unity Pixel Perfect 참조](https://docs.unity3d.com/kr/6000.0/Manual/urp/2d-pixelperfect-ref.html)

Unity의 Cinemachine/Pixel Perfect 안내도 카메라 블렌딩 중에는 일시적으로 픽셀 퍼펙트가 아닐 수 있다고 설명한다. 이 문서는 현재 프로젝트를 URP/Cinemachine으로 옮기자는 제안이 아니라, 큰 줌과 정수배율의 절충을 확인하는 참고다. [Unity 호환성 안내](https://docs.unity3d.com/kr/6000.0/Manual/urp/pixel-cinemachine.html)

## 4. 오디오 선정과 현재 상태

**상업 게임에 사용할 수 있는 라이선스가 확인된 후보를 선정했다. 현재는 다운로드·청음·편집·루프 검수·Unity 도입 전이다.** 구체 파일명이 검증되지 않은 팩의 소리를 이미 찾았다고 표시하지 않는다. 최종 적용 때는 선택한 파일의 원본명·해시·동봉 라이선스·변형 내용을 기록한다. 아래 용도는 제작자의 설명과 게임의 목표 분위기에 근거한 기획 판단이며 청음평이 아니다.

| 자료 | 용도 후보 | 라이선스·확인 근거 | 남은 일 |
|---|---|---|---|
| [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio) | 무기 휘두름·발걸음의 재료 | 공식 페이지50파일, CC0 | 개별 무기/발 파일 청음·짧게 편집 |
| [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds) | 금속 타격·돌가시·무기 착지 | 공식 페이지130파일, CC0 | 금속/돌재질 파일과3변형 선정 |
| [Darkling — Kevin MacLeod](https://www.incompetech.com/music/royalty-free/index.html?isrc=USUAN1700050) | 성문 일반 전투 BGM | 공식 곡 메타데이터 CC BY4.0, ISRC USUAN1700050 | 반복 이음·예고음 마스킹 청음 |
| [Black Vortex — Kevin MacLeod](https://remote.incompetech.com/music/royalty-free/index.html?Search=Search&isrc=USUAN1300037) | 중간 보스·보스 BGM | 공식 곡 메타데이터 CC BY4.0, ISRC USUAN1300037 | 전투 시작에 쓸 구간·루프 선정 |
| [Eternal Hope — Kevin MacLeod](https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1100238&lt=) | 등불 로비 BGM | 공식 곡 메타데이터 CC BY4.0, ISRC USUAN1100238 | 로비에 맞는 얇은 구간 선정 |

Incompetech 곡 상세는 JavaScript로 로드된다. 이 조사에서는 검색에 수록된 제작자 공식 곡별 메타데이터의 제목·ISRC·CC BY4.0 표시와 별도의 공식 라이선스 안내를 대조했다. 페이지 정적 본문만 열면 Loading track이 나타날 수 있다. 내려받은 원본 파일이나 라이선스 사본을 확보했다는 뜻은 아니다.

Kenney는 공식 지원 페이지에서 CC0 에셋의 상업 이용과 의무 크레딧 없음이 확인된다. CC0는 복제·수정·배포·상업 이용을 허용한다. 편집한SFX도 원본과 제작내역을 보존하고 선택적 감사 표기 `SFX source: Kenney (CC0)`를 넣을 수 있다. 로고를 크레딧 장식으로 가져오지 않는다. [Kenney 이용 안내](https://kenney.nl/support), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/)

CC BY4.0 음원은 상업 이용·수정·재배포가 가능하지만 저작자·출처·라이선스 링크를 표시하고 편집 여부를 밝혀야 한다. 추가 제한을 걸지 않는 조건도 따른다. 게임 설정의 크레딧과 배포용 오디오 라이선스 문서에 읽기 쉽게 둔다. `저작권 없음`이라고 쓰지 않는다. Incompetech의 무료 방식도 크레딧을 요구한다. [CC BY4.0](https://creativecommons.org/licenses/by/4.0/), [Incompetech 라이선스](https://incompetech.com/music/royalty-free/licenses/), [공식 크레딧 안내](https://en.incompetech.com/music/royalty-free/faq.html)

권장 크레딧 형식은 다음과 같다(실제로 채택한 곡에만 사용).

> “Darkling”, “Black Vortex”, “Eternal Hope” — Kevin MacLeod (incompetech.com). CC BY4.0: https://creativecommons.org/licenses/by/4.0/ . Edited for looping, fades and game mix.

곡별 공식 출처 링크를 각 제목에 연결한다. 파일을 실제로 자르지 않았다면 편집 문구도 실제 작업에 맞게 고친다. 원본/편집본을 저장소에 넣을 때에도 같은 출처와 라이선스 정보를 동봉한다. 외부 유튜브 음원을 추출하거나 다른 게임의 공격음·포효를 가져오는 방식을 계획에 포함하지 않는다.

## 5. MVP 오디오 큐시트

| 이벤트 | 소리 제안·재료 | 재생 계약 |
|---|---|---|
| 플레이어 공격 시작 | RPG Audio 무기 휘두름 후보 | 칼이 실제로 움직이기 시작할 때 얇은 스윽 1회. 실제 명중과 별도 |
| 일반 명중 | Impact Sounds 금속/돌 후보3변형 | 세션의 실제 명중 결과1회. 빗나감에는 명중음 없음 |
| 치명타 | 일반명중+직접합성 짧은 종 고음 | 치명 이벤트1회. 큰 화면흔들림 없이도 구분 |
| 하단 가시 준비/타격 | 직접합성 낮은 균열음+Impact 돌 후보 | 예고 시작에 낮게, 타격 시 돌이 솟는 강세1회 |
| 후방 무기비 | RPG 무기 바람 후보+Impact 금속 후보 | 장식상 여러 발이어도 한 공격이면 메인타격1회, 나머지는 낮은 착지 잔향 |
| 중간 보스 포효 | 직접녹음/합성한 숨·금속 저음+CC0 금속 레이어 | 줌인 완료 후 포효 정점1회. BGM을 낮춰 분리 |
| 보스 포효 | 위 재료에 낮은 종·긴 잔향을 별도제작 | 확대 유지 동안1회. 음량만 키우지 않음. 첫예고 전에 잔향 낮춤 |
| 달리기 | RPG Audio 발걸음 후보2~3변형 | 접지 프레임에만. 화면전환 끝나면 즉시 정지 |
| 등불 | 직접녹음/합성 점화·짧은 불씨 | 점등 순간1회. 로비BGM0.5초 페이드인 |
| 일반 전투 | Darkling 후보 | 스테이지마다 처음부터 재시작하지 않고 같은구역에선 이어 재생 |
| 보스 전투 | Black Vortex 후보 | 포효 구간은 낮게 두고 줌아웃·정착 후 전투믹스로 전환 |
| 로비 | Eternal Hope 후보 | 사망 암전에서 이전BGM 페이드아웃, 등불에서 새곡 도입 |

보스 포효·등불의 직접 녹음/합성은 **새 제작 과제**다. Kenney 팩에 해당 완성 소리가 들어 있다고 주장하지 않는다. 녹음자는 권리를 제공할 수 있는 제작 참여자로 정하고 출처를 기록한다. 음악 박자에 회피 판정을 맞추지 않으며, BGM은 기존 전투시계와 독립적인 배경이다.

믹스 시작값은 BGM이 주요 예고/SFX를 덮지 않게 -6dB 정도 순간감쇠하는 안이며 실측 목표가 아니다. Master/Music/SFX를 분리하고 무음·모션감소에서도 시각 예고는 같게 유지한다. 같은 무기비5발을 다섯 개의 최대음량으로 겹치지 않으며, 공격취소·기절·사망·일시정지에서 예약SFX를 취소한다. 복귀 시 포효를 중복 시작하지 않는다.

## 6. 후속 구현 검수

- 레이어 5장의 이동 비율과 반복 이음매, 650ms/160px 전진, 카메라 줌 중 패럴랙스 정지, 모션 감소를 가로 1280px/640px 화면에서 확인한다.
- 대형보스+줌을 합산한 결과에서 얼굴·무기·발·입력영역의 의도된 구도를 확인한다. 전투복귀시 격자·HUD 위치가 정확히 돌아와야 한다.
- 실제음원 파일별 라이선스/해시/편집내역을 `audio-sources-v2.json`의 상태와 맞춘다. 현재 downloaded/auditioned/usedInUnity는 모두false다.
- BGM 루프·페이드, 포효 뒤 첫공격예고, 무기비 동시성, 빗나감/치명/일반명중의 소리 구분을 청음한다.

담당 결론: **최종 통합안의 직교 2D 패럴랙스 5레이어(0.02/0.08/0.22/0.48/1.0), 650ms/160px 이동과 권리조건이 명시된 오디오 후보 5개를 기획 출발점으로 수용한다. 실제 오디오·배경 적용과 청음 검수는 완료되지 않았다.**
