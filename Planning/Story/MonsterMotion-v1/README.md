# 몬스터 3상태 재제작 · v3

현재 선택 항목은 Attack·Hit·Stun 세 개다. Idle 애니메이션은 없으며 평상시는 기본 PNG 한 장이다. 공격은 몬스터당 하나만 둔다. 기존5종 원본은 보존한다.

2026-10-22 사용자 승인으로 검수 완료한 새10종을 Unity에 등록하고 현재1~10층 배치에 적용했다. 확장용5종은 실제 게임 렌더 검증까지 완료했으며 현재 플레이 범위는10층까지다. 최신 재연결은 `../MonsterMotionFresh-v3/install-runtime.cjs`와 `Last Bell/Reviewed Motion and Audio/Apply`를 사용한다. 기존 `install.cjs`는 과거 원본5종 복원용이므로 최신 인게임 카탈로그 갱신에 사용하지 않는다. [실행 검증](../../Evidence/ReviewedMotionAudio-v4/verification.json).

사용자 요청에 따라 애니메이션 세션(01a15e4d-1903-7852-bfc8-cf02d747c965)에 기본 PNG 10장만 전달했다. 새 제작 폴더는 `../MonsterMotionFresh-v3/`이며 이전 모션·생성 프롬프트는 전달하지 않았다. 새 결과의 파일은 `delivery.json`에 기록된다. 신규 Attack·Stun은 검수 전까지 기존 모션 대신 기본 이미지와 제작 중 상태를 표시한다. Hit는 45ms 흰색 유지 + 75ms 원색 복귀다.

기존 340ms 균등 압축을 없애고 새 결과의 frameDurationsMs를 읽는다. 반복 재생 시 공격 사이에 400ms 기본 자세를 보여준다. `review-fresh.cjs`는 프레임별 유지 시간이 적힌 검수 시트와 경계 잘림·연결 진단을 만들며 이미지를 수정하지 않는다. `fresh-review.json`에 담당 검수 기록과 delivery actor 해시가 일치해야 메인 후보 갤러리에 연결한다. 생성 담당자의 완료 표시만으로 자동 채택하지 않는다.

아래는 v2 복원 기록이다. v2 신규 후보 모션은 현재 갤러리에서 제외했다.

# 몬스터 원본 유지 · 모션 v2

기존 5종(성문 파수병, 사슬 망령, 재의 불씨, 성문 수문장, 종의 기사)을 원래 스테이지 배치로 사용한다. 외형, Idle·Attack·Stun의 선택 프레임, FPS, 명중 프레임, 크기와 피벗은 기존 값 그대로다. 원본 PNG 125개를 해시로 대조한다.

피격은 현재 동작을 중단하지 않고 셰이더로 45ms 흰색 유지 + 75ms 원색 복귀를 표시한다. 공격 예고 및 타격 시계는 바꾸지 않는다. 적 Hit·Death 클립은 카탈로그와 갤러리에서 제외했다. 갤러리의 피격 항목은 별도 몸 동작이 아닌 동일한 흰색 효과다. 처치되면 마지막 자세를 고정하고 기존 550ms 소멸과 보상 전환을 유지한다. 플레이어의 피격·사망과 승인된 달리기는 유지한다.

신규 10종은 후보 갤러리로만 보관한다. 기존 종의 기사와 후보 가디언은 서로 다른 ID로 구분한다. 종추 사냥개의 하단 공격만 PixelLab으로 재생성했으며, 잘못 생성된 링 효과 3프레임도 PixelLab 편집으로 제거했다. 신규 후보를 기존 몬스터 대신 자동 적용하지 않는다.

- `original-actors.json`: 복원 기준 원본 클립·텍스처 해시·스테이지 배치.
- `jobs.json`, `generation-plan.json`: 최초 신규 후보 제작 기록. Hit·Death 포함 과거 결과는 기록 보존용이며 현재 사용하지 않는다.
- `revisions.json`, `Revisions/`: 사냥개 재제작 및 PixelLab 효과 제거 기록·결과.
- `motions.json`, `motions-data.js`: 기존 5종 + 신규 후보 10종 미리보기 데이터.
- `install.cjs`: 원본 5종 카탈로그 복원 및 후보 갤러리 갱신. 원본 이미지 수정 없음.
- `validate.cjs`, `validation.json`: 원본 보존, 효과 타이밍, 파일 참조, 스테이지 배치 검사.

미리보기: `../route-prologue-tutorial-v4.html#monster-motions`. 기존 5종이 먼저 나오고 후보는 별도 그룹이다. 피격 확인 버튼은 재생 중인 동작에 흰색 효과만 겹친다. 사망 선택 항목은 없다.

Unity 저장 에셋 반영: **Last Bell → Art → Presentation → Migrate Existing**. 기존 카탈로그 GUID를 유지한다. 로컬 실행 증거는 `../../Evidence/Monster-originals-runtime/`에 있다. HTML은 정적 참조·모델 및 DOM 대역 검사로 검증했으며 브라우저 렌더 검증은 수행하지 않았다. 공개 웹 빌드는 갱신하지 않았다.
