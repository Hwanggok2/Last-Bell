# Forest-v1 출처와 사용 조건

이 프로젝트에 대해 사용자가 승인한 PixelLab로 새 원형과 모션을 제작했다. 제삼자 게임 스프라이트나 원화 시트의 잘라낸 조각을 생성 입력으로 쓰지 않았다. 기존 프로젝트의 `story.json`과 `monster-pixel-v2.png`는 종족·장비·방향·밀도 기획 참고다. 게임 원형은 텍스트와 명시한 팔레트에서 별도로 생성했다.

원형은 PixelLab `create_image_pro_flash`, 기본 모션은 `animate_image_pixminimax`다. 검·창 동작은 원형의 실제 관절과 무기 손잡이 위치를 읽어 `animate_with_skeleton_v3`로 8개 포즈를 직접 지정했다. 요청 전문·seed·원본 URL·job ID는 `jobs.json`, `animation-jobs*.json`에 기록한다. PixMiniMax의 원본0은 생성 입력이며 생성 모션 수에 포함하지 않는다. Skeleton v3는 입력을 되돌려 주지 않으므로 0~7 모두 요청한 포즈다.

색 혼입·배경 없는 파편·경계에 닿은 칼끝은 PixelLab `pixelart_workbench`의 guarded patch/map 연산으로 국소 보정했다. 좌표·기대색·대체색·작업 ID·새 gallery ID는 `pixel-corrections*.json`에 남겼다. 수정은 원본을 보존하고 별도 `processed` PNG로 내려받았다. Node Canvas는 픽셀값 읽기·연락표 합성에만 사용했으며 캐릭터 픽셀을 수정하지 않았다. 최종 PNG는 선택한 원본 또는 서비스 보정본의 바이트 복사다. sourceIndices, sourcePath, correctedAssetId, SHA256은 manifest로 추적한다. 런타임 파편·흰색 피격·공격 투사체는 이 원본에 붙이지 않는다.

2026-10-24 확인한 [PixelLab 이용약관](https://www.pixellab.ai/termsofservice) 1.3·3.3은 생성물의 상업/비상업 이용, 수정, 배포를 허용하며 모델 훈련은 서면 허가를 요구한다. 4.1은 Open RAIL-M을 참조한다. [공식 FAQ](https://www.pixellab.ai/docs/faq)도 상업적 이용 가능성을 안내한다. 이는 제공자의 이용 조건 기록이며 독점 저작권 성립이나 모든 제삼자 권리의 자동 보증을 주장하지 않는다.
