# 2026-10-21 생성 아트 초안

참고: `style-reference.png`. 사용자가 제공한 이미지의 청색 폐허·황금 실루엣·큰 도트 덩어리를 방향으로 삼았다.
생성: 내장 ImageGen. 원본 생성 파일은 사용자 generated_images 디렉터리에 보존했고 Unity Source에는 사본을 둔다.

|프로젝트 원본|구성|상태|
|---|---|---|
|`Client/Assets/Game/Art/Source/on-poses-v1.png`|온 4×2 시트: idle 2, 공격 2, 점프·숙이기·후퇴·돌진 각 1|8개 키 포즈 초안|
|`Client/Assets/Game/Art/Source/gate-enemies-v1.png`|문지기·사슬 망자·재의 종령·성문 파수장·종 기사, 각 idle/attack/stun|5종 × 3개 키 포즈 초안|
|`Client/Assets/Game/Art/Source/gate-background-v1.png`|1~10층 성문 폐허, 낮은 바닥|배경 초안|

## 생성 프롬프트 구성 기록

아래는 요청 구성의 요약이다. 원문을 그대로 인용한 기록은 아니다.

- 플레이어: 첨부 참고의 단순한 남색·금색 후드 영웅 한 명, 오른쪽을 보는 측면, 4열 2행 투명 아틀라스. 같은 인물/장비/체급을 유지하고 idle A/B, 공격 준비/타격, 점프, 숙이기, 뒤로 피하기, 앞으로 파고들기를 구분. 글자·격자선·라벨·배경 금지.
- 몬스터: 왼쪽을 보는 5개 실루엣, 5열 3행 투명 아틀라스. 짧은 칼 성문병, 사슬 후드 망자, 깨진 종령, 넓은 방패와 철퇴 파수장, 후광과 장병기 종 기사. 행은 대기/공격/기절. 청색 내부와 황금 외곽, 제한된 명암, 레퍼런스의 낮은 해상도 느낌.
- 배경: 16:9 측면 폐허 성문·성당 아치·초승달, 청색 원경과 남색 전경, 전투 중앙의 여백, 인물/문자/UI 없음. 바닥 위치를 화면 높이 약 82%로 낮추는 편집을 한 차례 적용.

## 원본 출처 파일

- 플레이어: `exec-58e6833a-3230-4902-9cc8-75315d3e5ac8.png`
- 몬스터: `exec-0d839d89-bb1b-400b-a3df-9150ec16185a.png`
- 배경 최종 초안: `exec-0ba118d0-db6a-48a4-bc5a-3c4c2462f20e.png`
- 공통 원본 폴더: `C:/Users/JW/.codex/generated_images/01a159aa-e5ea-7643-b8dd-d07372e5f713/`

## Unity 적용과 제한

원본 PNG를 다시 그리거나 덮어쓰지 않는다. Importer는 투명 여백에서 셀을 분리하고 발 위치를 pivot으로 지정한다. Point 필터, mipmap 없음, 무압축. 이동·피격·기절 흔들림·사망 전환은 런타임 연출을 조합한다. 이번 결과는 완성 손작업 애니메이션이나 50종 완성본이 아니다. 시트별 픽셀 크기, 공격 실루엣과 연결 프레임은 실제 플레이 검토 후 정리한다.

한글 폰트: Google Fonts의 Nanum Gothic Regular, SIL Open Font License. 원본 및 라이선스는 `Client/Assets/Game/Art/Fonts`에 보존. 출처: https://github.com/google/fonts/tree/main/ofl/nanumgothic
