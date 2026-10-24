# Dragon v4 신규 플레이어 원본

다렌(`dam` / `player_dragon_daren`)과 미엘(`rin` / `player_dragon_miel`)의32px 생성 스프라이트와 모션. 루카(`on` / `dragon_hero_on`)는 기존 Player-v3를 유지한다.

- `manifest.json`: 임포터 입력과 고정 피벗·프레임 타이밍.
- `review.html`: 동작별 동시 재생, 속도 조절, 초상화 미리보기.
- `visual-review.md`, `validation.json`: 소스 검수와 PNG 검사.
- `requests/`: PixelLab 요청·job ID·응답·원본 다운로드/해시.
- `{daren,miel}/raw`: 직접 받은 생성 프레임. `clips`: 검사 후 그대로 복사한 제작용 프레임.
- `license.json`: 출처·이용 조건 기록.

루트의 `Tools/import-dragon-playable-v4.py`는 기본 실행 시 검증만 한다. `--apply`가 있을 때에만 두 배우를 공용 아트 명세에 추가하고, 신규 PNG와9개 초상화를 Unity 경로로 복사한다. 다른 배우/텍스처의 구조는 유지한다. 이 임포터 자체는 Unity Editor 임포트나 프리팹 연결을 실행하지 않는다.
