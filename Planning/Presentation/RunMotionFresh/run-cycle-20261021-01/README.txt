새 달리기 모션

유일한 시각 참고: ../character-base.png
제작: 내장 image_gen 도구로 새로운 8프레임 스프라이트 시트를 생성.
다른 캐릭터, 기존 달리기 애니메이션, 기획서 또는 검증 기록은 참고하지 않음.
기존 게임이나 기존 모션 파일에 적용하지 않음.

재생
- run-preview.gif: 512×512 확대, 무한 반복. 미리보기 배경은 짙은 회색.
- preview.html: 별도 설치나 인터넷 연결 없이 파일을 열어 재생.
  일시정지, 이전/다음 프레임, 프레임 선택, 재생 속도 변경 지원.

이미지
- frames/run-01.png ~ run-08.png: 128×128, 투명 RGBA PNG.
- run-sheet.png: 1024×128, 왼쪽에서 오른쪽 순서의 투명 시트.
- contact-sheet.png: 8개 자세와 각 프레임의 재생 시간을 한눈에 확인.
- animation.json: 프레임 순서와 재생 시간.

모션
오른쪽으로 달리는 제자리 루프. 양발의 착지/무게 받기/밀어내기/공중 동작.
프레임별 시간: 80, 60, 60, 80, 80, 60, 60, 80 ms.
총 560 ms, 초당 약 14.29 프레임. 발의 접지선은 y=114 부근.
원본 캐릭터의 크림색 후드, 검은 얼굴, 노란 눈, 금색 단검을 유지하도록 제작.

제작 소스
- source/generated-sheet.png: image_gen 원본 결과.
- source/generation-prompt.txt: 실제 사용한 전체 프롬프트.
- source/package.ps1: 생성 시트 분리, 배치 정렬, PNG/GIF/HTML 포장.
- source/set-gif-timing.cjs: GIF 프레임 시간 및 무한 반복 메타데이터.
- source/preview-template.html: 로컬 재생기 템플릿.

확인 범위
프레임별 외형, PNG 크기/투명 여백/고유성, GIF 8프레임과 재생 시간을 확인.
브라우저 도구가 로컬 file:// URL을 차단하여 브라우저 내 직접 실행 검사는 수행하지 못함.
