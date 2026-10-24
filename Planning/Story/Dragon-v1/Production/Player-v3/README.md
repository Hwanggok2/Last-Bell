# 루카 Player-v3

NPC 토리·리나·바룬·도토와 외곽선 및 색 면을 맞춘 게임 연결용 아트 묶음. 총괄의 원형 비교 및 6종 최종 시트 검토를 거쳐 **A안 채택·원본 동결** 상태다. 기존 Player-v2 파일은 수정하지 않았다.

- `manifest.json`: actor ID `hero_on`, 표시 이름 `루카`, 32×32 PNG 및 기존 클립 시간·피벗·접촉 프레임.
- `canonical-a-outline.png`: 선택 원형. 갈색 머리·붉은 목도리·청록 튜닉·작은 가방·단일 강철 검 유지.
- `canonical-b-head.png`: 미채택 비교안. 머리 캡1px 축소. 게임에서 사용하지 않는다.
- `clips/`: Run8 / Idle8 / Attack14 / DodgeUp8 / DodgeBack8 / Hit6, 총52개 PNG. 아래 쳐내기(DodgeDown)는 Attack14를 재사용한다.
- `reviews/canonical-comparison.png`: NPC·기존 루카·A/B 비교.
- `reviews/*-final.png`: 실제 원본52프레임 확대 시트6종.
- `validation.json`: 모든 source PNG 해시·alpha·허용 색 변경·보호색·피벗/타이밍 보존 검사.
- `requests/`: PixelLab 원본 및 새 gallery/workbench ID, 프레임별 정확한 수정 좌표와 보존 기록.
- `style-grounding-review.md`: 스타일 차이, 선택 이유, 접지 기준, 검토 한계.
- `license.json`: 이전 제작 묶음의 PixelLab 권리·출처 기록을 이어받고 이번 정밀 편집 출처를 추가했다.

공유8색에서 상의 `#2D735D → #287780`, 목도리 `#8B2F35 → #A23B2D`를 바꿨다. 노출된 머리/가방의 기록된 갈색 외곽만 기존 남색 `#202D38`로 정리했다. 외곽을 바깥으로 팽창시키지 않았고 피부·손의 밝은색·강철·크림 픽셀과 투명 영역은 동일하다.

Run400ms, Attack14/contact7, 공통 클립 피벗과 발 위치는 Player-v2를 유지한다. Run02/06 체공, 위 회피의 공중 자세, 공격 후 회수 자세를 강제로 지면에 붙이지 않는다. 게임의 월드 접지·판정·실시간 전환 검증은 총괄이 별도로 수행한다.
