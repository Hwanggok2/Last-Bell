몬스터 모션 신규 제작 v3

유일한 시각 참고는 Inputs/의 지정된 10개 기본 PNG입니다.
제작 도구: PixelLab animate_image_pixminimax / animate_image / edit_image / edit_image_pixen / pixelart_workbench.
사슬 인형의 기절 루프는 새로 생성한 힘 빠진 자세에서 PixelLab 정확 픽셀 편집으로 머리만 1px 움직이도록 제작했습니다.
갈고리 사냥개의 기절도 새 자세에서 PixelLab 정확 픽셀 편집으로 상체를 낮추고 발을 고정했으며 상체의 1px 왕복으로 구성했습니다.
기존 애니메이션이나 게임 파일을 편집하지 않습니다.

진행 상태
10종 완료. Attack 10개, Stun 10개, Hit 효과 10개, 선택 PNG 프레임 146장.
delivery.json에는 공격·기절·피격 검수가 끝난 몬스터만 순차 등록됩니다.
progress.json과 production-jobs.json에서 완료 수, 작업 ID, 프롬프트, 탈락 사유를 확인할 수 있습니다.
candidate.html과 Sources/는 검수 후보이며 게임 연결 대상이 아닙니다.

최종 사용 파일
- delivery.json: Client 연결 계약. 상대 경로, 고정 피벗, 프레임 유지 시간, 실제 contactFrame.
- preview.html: 외부 설치 없이 열 수 있는 자체 포함 재생기. Attack / Hit / Stun만 선택.
- Actors/{id}/Attack, Stun: 선택된 128×128 투명 프레임 PNG.
- Actors/{id}/*-sheet.png, *-contact.png: 스프라이트 시트와 프레임별 검수 시트.
- overview.png: 완성된 몬스터의 기본 자세, 실제 타격 자세, 기절 자세 비교.

상태 처리
평상시는 Inputs/{id}.png 한 장입니다. Idle/Death 애니메이션은 제공하지 않습니다.
Attack은 한 번 재생하고 제공 기본 자세로 돌아옵니다.
Stun은 힘이 빠진 자세의 선택 프레임 루프입니다.
Hit은 새 그림이 아니라 현재 자세의 알파를 유지하는 whiteFlash 효과입니다.
45ms 완전 흰색 유지 후 75ms 동안 원래 색으로 선형 복귀합니다.
미리보기에서도 Attack 또는 Stun의 현재 자세를 그대로 잡아 흰색 효과를 적용합니다.
contactFrame은 선택된 frames 배열의 0-based index입니다. 타격 시작 시각은 이 프레임 직전까지의 frameDurationsMs 합입니다.
개별 프레임의 불투명 영역으로 재중앙정렬하거나 크기를 다시 맞추지 말고, 128×128 셀과 고정 피벗을 그대로 사용합니다.
프레임별 유지 시간은 각 동작에 맞춰 선택했으므로 일괄 동일 FPS로 압축하지 않습니다.

검수 기록
Review/{id}-selection.json: 선택/제외 프레임, 동작별 시간, 실제 관찰 근거.
Review/technical-checks.json: 크기, 알파 여백, 파일/시간/피벗 계약, 원본 해시 등 기계적 검사.
Review/review-report.json: 전체 완료 수, 실제 검수 범위, 타격 시각, 탈락 기록을 모은 보고서.
기계적 검사 통과가 시각 품질 통과를 뜻하지 않습니다.
시각 검수는 전체 컨택트 시트, 1×와 0.5× 브라우저 재생, 타격 프레임 정지를 구분해 수행합니다.

파일 소유 범위는 이 MonsterMotionFresh-v3 폴더 안으로 한정합니다.
게임 임포트와 상태 연결은 Client 개발 세션에서 담당합니다.

2026-10-22 미리보기 수정
- hook_hound 공격의 뜨는 위치 수정: 중간 8프레임 아래로 8,10,23,21,18,18,18,2px 이동. 지지발 y113, 원본 픽셀·좌우 이동 보존. Review/hook_hound-registration.json 참조.
- 새 10종을 ../route-prologue-tutorial-v4.html의 스테이지1~20과 모션 갤러리에 연결. ?stage=12로 갈고리개 바로 확인.
- 미리보기 갤러리 재생성: node ../MonsterMotion-v1/install.cjs --preview-only
- 로컬 실행: node Tools/serve-preview.cjs → http://127.0.0.1:8875/route-prologue-tutorial-v4.html?stage=12
- Unity/Client 적용 보류: 사용자 미리보기 확인 후 별도로 진행.
