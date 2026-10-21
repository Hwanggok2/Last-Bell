# 달리기 v10 — SimpleGame 검술사 참고

> 속도 개정(2026-10-21): 아래는 최초16fps·500ms 후보의 제작·프레임 검토 기록이다. 이후 사용자 요청으로 현재 재생은24fps·약333.33ms(1.5배), 월드 이동은1920px/2000ms·960px/s(거리·속도2배)로 변경했다. 선택 자세8장과 원본은 같다. 현재 GIF는3주기24프레임/1000ms로 평균24fps를 보존한다. 최신 수치는 [현재 기획](../Presentation/proposal.json)을 따른다.

2026-10-21. 사용자가 지정한 SimpleGame 검술사(BladeMage)의 실제 Move 클립을 기준으로 온의 달리기를 다시 생성했다. Unity 적용 전 기획 시안이다.

## 참고 근거

- [원본 8장](../Presentation/PixelLab/v10/reference/contact.png), [원본 반복 GIF](../Presentation/PixelLab/v10/reference/reference.gif), [클립·컨트롤러·소스 해시](../Presentation/PixelLab/v10/reference/source-manifest.json).
- 실제 클립은 8프레임·16fps·500ms 반복이며, 직렬화된 Move 상태 속도는 1이다. 실행 중 Animator 속도를 측정한 결과는 아니다.
- 검술사는 검을 몸 뒤로 잡고 팔을 작게 움직인다. 다리의 접지·회수, 몸통의 반응과 옷자락으로 달리기를 표현한다. 큰 팔 휘젓기를 필수로 요구했던 이전 기준을 철회했다.
- 원본은 왼쪽, 온은 오른쪽을 본다. 원본 프레임은 뒤집거나 수정하지 않고 비교 자료로 보존했다.

## 생성 방법

1. 온의 기존 Idle 01을 인물 기준으로, 검술사 Run 00을 자세 기준으로 PixelLab에서 새 달리기 시작 자세를 만들었다. 사람 얼굴·긴 팔다리·은색 장검을 복사하지 않도록 명시했다.
2. SimpleGame의 `BladeMageAnimationV2/motion-prompts.json`에 있는 실제 달리기 프롬프트를 온의 체형과 오른쪽 방향에 맞춰 수정했다. 후드·한쪽 눈·짧은 체형·허리 종·금색 단검을 유지하고 검손과 칼날 연결을 강조했다.
3. PixelLab MiniMax로 몸 전체를 다시 생성했다. 이전 v9의 고정 몸통에 팔다리를 그려 붙이는 방식은 사용하지 않았다.

[시작 자세 요청](../Presentation/PixelLab/v10/keypose-request.json) · [애니메이션 요청 전문](../Presentation/PixelLab/v10/run-request.json) · [원형과 새 자세](../Presentation/PixelLab/v10/keypose-comparison.png)

- 시작 자세 작업: `fb1dd7d5-cdbf-4453-93e2-9f15960b346a`
- 애니메이션 작업: `929856d9-e432-453f-bec9-379bbfd9166f`
- 반환 9장 = 입력 00 + 생성 01~08. 반복에는 00~07을 사용한다. 08은 시작과 같은 달리기 위상으로 돌아온 끝 자세이며, 00과 픽셀이 동일한 복제본이라는 뜻은 아니다.
- 128×128 원본 셀, 공통 피벗 (60,114), 8장·16fps·500ms. 프레임마다 크기를 맞추거나 몸통을 회전하지 않는다.
- GIF는 10ms 단위여서 60/70ms를 분배하며 전체 500ms를 보존한다. 기획 Canvas는 62.5ms 간격이다.

## 반영 범위와 검토

[새 반복 GIF](../Presentation/PixelLab/clips/hero-v10-blademage-run/edited-preview.gif) · [원형과 선택 8장](../Presentation/PixelLab/clips/hero-v10-blademage-run/identity-contact.png) · [아트 검토](run-v10-art.md)

기획서의 이동과 공격 진입에서 새 달리기를 사용한다. 플레이어 화면 기준점 x500, 월드 이동 960px/2000ms, 일반 적 전환 3250ms, 기존 베기·복귀·회피·피격 및 보상 규칙은 유지한다. v9는 비교를 위해 보존한다.

정지 프레임의 순서·무기 연결·의상과 오프라인 Canvas 렌더를 확인한다. 브라우저가 `file://` 접근을 거부했으므로 우회하지 않았으며, 실시간 브라우저 반복 재생·Unity 실기기 확인은 완료했다고 주장하지 않는다. 최종 자연스러움은 실제 재생 검토가 남아 있는 시안 상태다.
