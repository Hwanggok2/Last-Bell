# v0.9.3 총괄 화면 검토

2026-10-24. 네이티브 Unity PNG를 직접 열어 검토했다. 브라우저 렌더나 모바일 실입력 검증은 아니다.

- 1280 전투: 노란 공격 예고의 외곽과 어두운 경계가 배경에서 구분된다. 기절 누적50의 하늘색 채움이 빈 영역과 구분되고, 하단 중앙 스킬은 공격·회피 입력 영역과 겹치지 않는다.
- 다렌: 선택 외형과 전투 외형이 일치한다. 스킬 적중 후 방패 표식, 쿨타임20초, 저장 복원 뒤 같은 상태를 확인했다.
- 미엘: 모험가 선택창에서 이름·스킬·외형을 대조했고, 640 화면에서도 중앙 스킬명과 사용 상태를 읽을 수 있다.
- 대화: 토리의 첫 소개, 바룬의 긴 문장, 주인공 전환을 확인했다. 이름은 본문 좌상단, 초상화는 왼쪽이며 확인한 이미지에서 본문과 다음 버튼이 겹치지 않는다. QA 담당은 세 주인공의 전체 대화 페이지를 별도로 검토한다.
- 최초 Stage11 연속 이미지에서 몸체가 제자리에 남는 문제를 발견했다. 균일 fps 클립이 접근 분기에서 제외된 원인을 Client가 수정했다. 수정 뒤 1280 접촉/복귀 원본과 측정값을 대조했다. 몸체가900→604로 전진해 무기가 플레이어에게 닿고 원위치로 돌아온다. 두 해상도의 최초·반복 공격은 모두296 계획px 전진, 복귀 오차0, 발 지면 오차0이다.
- 대화 와이어프레임과 명세가 Canvas 논리 좌표를 화면 픽셀로 잘못 표현한 점을 수정했다. 1280에서 초상화192px, 640에서는96px이며 실제 캡처와 맞췄다.

주요 원본:

- [기절50·예고·중앙 스킬](../Evidence/Dragon-v4/dragon-v4-candidate-02/1280x720/fresh/v4-stagger-50.png)
- [다렌의 보호막 복원](../Evidence/Dragon-v4/dragon-v4-candidate-01/1280x720/fresh/v4-skill-dam-ready-contact-paused-file-restored.png)
- [토리 소개](../Evidence/Dragon-v4/dragon-v4-candidate-02/1280x720/fresh/v4-story-dam-2-page-1.png)
- [바룬의 두 줄 대사](../Evidence/Dragon-v4/dragon-v4-candidate-02/1280x720/fresh/v4-story-dam-15-page-3.png)
- [수정된 돌진 접촉](../Evidence/Dragon-v4/dragon-v4-candidate-03/1280x720/dash/v3-frame-v4-enemy-11-combo-000017.png)
- [돌진 뒤 원위치](../Evidence/Dragon-v4/dragon-v4-candidate-03/1280x720/dash/v3-frame-v4-enemy-11-combo-000053.png)
- [640 미엘 전투 UI](../Evidence/Dragon-v4/dragon-v4-candidate-03/640x360/fresh/v4-skill-rin-ready.png)
- [640 토리 대화](../Evidence/Dragon-v4/dragon-v4-candidate-03/640x360/fresh/v4-story-on-2-page-1.png)

원본 후보의 실패 기록은 보존했다. 이전 후보에서는 통과한 해당 화면·기능만 채택하며, 수정 전 Stage11 이동과 오류가 발생한 오디오 녹음은 합격 근거로 사용하지 않는다. 최종 판정 범위는 [QA 결과](../QA/dragon-v4-result-20261024.md)를 따른다. 효과음 직접 청취·정확한 동시 음성 수·모바일 사용성은 이 검토로 인증하지 않는다.
