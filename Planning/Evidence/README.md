# 기획·Unity 검증 자료

기획서에서 사용하는 정지 캡처·접촉 시트·GIF/영상과 실제 Unity QA 캡처·검증 결과를 보관한다. 생성 원본과 선택 모션은 `Planning/Presentation/PixelLab`, Unity 적용 자산은 `Client/Assets/Game/Art/Presentation`에 있다.

`Presentation*` 아래 `attack`, `feedback`, `hit-flash`, `random`, `run`, `transition` 폴더의 PNG 시퀀스는 미리보기 인코딩용 중간 산출물이다. 원본·렌더 스크립트와 완성 미리보기를 저장하고, 중간 시퀀스는 로컬에 보존하되 Git에서 제외한다. 대응 버전의 `Planning/Presentation/render-*.mjs`로 렌더한 뒤 `encode-*.py`로 GIF를 다시 만들 수 있다. 실제 Unity 실행에서 기록한 `Combat-*`, `Unity*` 등의 프레임은 이 제외 규칙에 해당하지 않는다.

테스트 플레이어 저장과 검사 전 사용자 설정 사본은 로컬에만 보존한다. 검사 결과 문서에는 복구 여부와 근거를 기록하며, 과거 결과를 이후 후보의 합격으로 해석하지 않는다.
