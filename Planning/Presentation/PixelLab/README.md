# 플레이어 v5

10종을 새로 생성하고 질주 B 및 깊은 아래 회피 수정본을 선택했다. 기본 검토기는 질주 B를 표시한다. 이전 v4 플레이어는 목록에서 별도 표시한다.

현재 기록: `player-v5-jobs.json`, `player-v5-run-job.json`, `player-v5-run-alt-job.json`. 모든 입력00과 생성 PNG를 보존한다. 채택 순서/피벗/검토 상태는 `edits.json`, 합성은 상위 `index.html#player`에서 확인한다.

# 기존 v4 원본·제작 도구 기록

Unity에 적용하지 않은 원본/검토 자산이다. `review.html`은 원본 프레임 재생기이고 상위 `index.html`은 선별 프레임의 전투 합성이다.

- `jobs.json`: 15회 생성, 생성 프레임288장. 각 작업의 입력 원본1장(총15장)은 별도. 재시도·반려본을 포함하므로 유효 출시 프레임 수가 아니다.
- `inputs/`: 기존 승인 초안에서 고정128×128 투명 참조를 준비한 그림. 포즈별 비율을 바꾸지 않는다.
- `clips/*/frames/`: PixelLab 다운로드 원본. 00은 입력, 이후는 생성 프레임. 일부 장면에는 모델이 요청과 달리 잔상/중복 동작을 넣어 반려 또는 편집했다.
- `edits.json`: 담당 검토를 반영한 상태·메모·선별 재생 순서·단일 접점·얼굴 앵커. 주보드는 이 순서를 사용한다. 원본 검토기는 생성을 숨기지 않고 전체 원본 순서를 재생한다.
- `sheet.png`, `contact.png`, `animation.gif`: 원본 셀 패킹/번호표/전체 원본 재생. `edited-preview.gif`가 있으면 선별 순서 미리보기이다. 프레임 보간으로 장수를 늘리지 않는다.
- `diagonal-slash`: PixelLab **VFX Workbench로 직접 구성**한10프레임 효과. AI 캐릭터 생성과 구분한다. 256×128,40fps,250ms. 크림/황동/암청 외곽. `recipe.json`은 내보낸 프레임 좌표·타이밍 메타데이터이다.

v4 당시 주 후보는 달리기C, 온 단발공격 재작업과4방향 회피, 파수병·망령·불씨의 중복 hold를 줄인 공격, 기사 단발 재작업이다. 수문장 철퇴 구간의 준비 연결은 추가 제작이 필요하다. 적19개 방향별 동작이나50종 전체 모션을 완성했다고 세지 않는다.

`../prepare-pixellab-references.mjs`는 참조 준비, `download-results.py <clip-id...>`는 명시된 생성 결과 다운로드, `pack-clips.py`는 고정 셀 패킹과 `assets.json/js` 생성이다. 패킹은 Pillow가 있는 번들 Python으로 실행한다. `edits.json`과 원본PNG가 편집 원본이며 생성 manifest를 직접 고치지 않는다.

참격 수정: [PixelLab 효과](https://pixellab.ai/create-vfx?effect=faf3549e-3c15-4f10-aca4-5c0e8a54127a). 최종 내보내기ID `c2caf0c5-ed1f-4ee6-943e-6b794107d2cd`, 편집 step `4d4aa149-b07a-4bd9-9283-6b1536ac2619`. 프레임의 고정 배치가 필요한 편집에서는 auto-trim을 사용하지 않는다.

검증: 원본 시퀀스·오프라인 Canvas 합성·제어 핸들러·프레임 크기/경로 확인. 실제 브라우저 연속재생, 모바일 터치, 최종 게임 애니메이션, Unity 검증은 수행하지 않았다. 현재 아트 검토는 `../../Reviews/pixellab-player-v5-art.md`, 이전 기록은 `../../Reviews/pixellab-motion-v4-art.md`에 있다.
