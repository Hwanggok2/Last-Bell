Last Bell — audio v4 (2026-10-22)

2026-10-22 사용자 승인 후 Unity 저장 씬·프리팹과 Windows 실행 빌드에 적용했다.
최신 검 충돌음은 명중200ms, 낮고 긴 종은 보스 처치에 연결했다. 전투·로비·보스 BGM은 스테레오다.
검증 기록: ../../Evidence/ReviewedMotionAudio-v4/verification.json

미리보기
- http://127.0.0.1:8874/preview.html#audio-review
- http://127.0.0.1:8875/route-prologue-tutorial-v4.html?stage=12
- 소리 켜기, 검 충돌음 듣기, 낮은 종 듣기, 배경음 선택과 전체/음악 음량 조절.
- 기존 베기 A/B/C를 검 충돌음 하나로 교체. 미리듣기와 몬스터 공격은 같은 파일을 사용한다.
- 충돌음은 명중 프레임에 재생한다. Hit 버튼은 별도 명중음.
- 5스테이지 전투: 낮은 예고 종 / 타격 / 회피 / 완벽 대응을 이벤트 시각에 연결.
- 음악은 같은 구간에서 다시 시작하지 않으며 전환 시 700ms 교차 페이드.
- 음소거·전투 일시정지·탭 비활성화에서 효과음을 중단. 큰 종은 1개, 같은 효과음은 최대 3개.
- 화면 밖의 갤러리 공격음은 재생하지 않음. 모션 저속 재생에서도 효과음 음높이는 유지.

음원
- 검 충돌: Ben Jaszczak / Brian Nelson의 Sabre Norse Sword Blade on Blade.wav. 12.4575~13.1075초의 단일 충돌, 650ms. 원래 음높이·EQ·금속 잔향 유지. 별도 베기·바람 소리를 섞지 않는다.
- 금속 명중: Kenney impactMetal_light_002 가공, 149ms.
- 큰 종: Joseph SARDIN & Axeline T. 실제 종 녹음, -7.73반음, 5.717초.
- 전투 예고 종: 같은 낮은 종에서 1.15초 감쇠 버전. 빠른 예고에서 큰 종 전체를 중첩하지 않음.
- 회피·완벽 대응·돌길 발소리: Kenney 원본 가공. 등불: 자체 합성.
- 성문 밖: Dark Theme / JaggedStone.
- 등불 아래: Dark Shrine Loop / qubodup, yd.
- 보스: Epic Boss Battle / Juhani Junkala (SubspaceAudio).
- 성문 안: Dungeon Ambience / yd.

출처·라이선스·변경 내용
- sources.json / Sources/*-source.html: 제작자 원본 페이지, 다운로드 URL, 원본 해시.
- delivery.json: 실제 사용 파일, 편집, 길이, 피크/RMS, 출력 해시.
- CREDITS.txt 및 Kenney 라이선스 사본. 새 검 충돌 CC0 표시는 Sources/weapon-impacts-source.html에 보관. 외부 원본은 모두 CC0 1.0.
- Unity 슬롯 연결 계획: unity-handoff.json (아직 적용하지 않음).

검증
- validation.json: 현재 등록된 12파일 전체 디코딩, 클리핑 없음, 브라우저 확인.
- comparison.json: 현재 파일의 주파수·길이 측정. 공격음은 고음 증가를 합격 조건으로 삼지 않으며, 실제 검날 충돌 원본 구간과 단독 재생 여부를 확인한다.
- 자동 신호 검사와 브라우저 재생 동작을 확인했다. 실제 음색·게임 적합성의 최종 청음은 사용자 검토 대상이다.

다시 만들기
- node download-sources.cjs (다운로드 필요 시)
- 번들 Python + numpy로 build_audio.py 실행. 스크립트의 FFMPEG 경로는 설치된 Vrew ffmpeg.
- measure-comparison.py 실행.
- node ../MonsterMotionFresh-v3/Tools/build-preview.cjs 로 8874용 오디오 사본과 모션 미리보기를 함께 갱신.

서버
- 기존 8874: MonsterMotionFresh-v3 폴더만 서비스.
- 8875: ../MonsterMotionFresh-v3/Tools/serve-preview.cjs (Story + 필요한 게임 이미지 허용 목록).
