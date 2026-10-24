# DQA5-002 표적 검사 준비

2026-10-25 KST. QA 소스 동결 완료. [소스 해시](dragon-v5-story12-probe-manifest-20261024.json). Runtime74개 소스의 Roslyn 구문·타입 오류0, PowerShell AST 오류0. DLL emit/Unity 실행/제품 코드 변경 없음.

기존 `Tools/QA/Dragon-v5/run-campaign.ps1`에 `-Story12Only`를 추가하면 `v5-story12`로1280×720/640×360 각각1프로세스만 실행한다. 프로세스당 on/dam/rin의 Stage12 전체 대화5페이지를 캡처한다. 첫줄 데이터 portraitId=barun, resolved speaker=???, resolved portrait=barun, 실제 저장 sprite=Barun이며 unknown fox와 다름을 검사한다. 모든 페이지의 실제 sprite와 resolved portrait, 이름/좌우, 종료/바룬 해금도 확인한다. 전투 시작은 약한 적 fixture, 클릭은 합성 UI raycast이다.

새 observed executable, Runtime SHA256, 전체 sourceFrozen manifest, 기존 Core/integration 인계와 prior-QA schema8 경로를 기존 필수 인수에 전달한다. 새 candidate 이름을 사용한다. Runner의 기존 보호 검사를 재사용하므로 이전 QA schema8 파일은 checksum만 읽고, 표적 프로세스에는 주입하지 않는다. 원본 프로브 소스는 candidate02/review/original-qa-source에 보존했다.

아직 표적 native 실행하지 않았다. 새 빌드·소스 동결·실행 소유권 인계 뒤 `-Run -Story12Only`를 사용한다. 전체 캠페인/native resume를 재실행하지 않는다. 기존 candidate02의 campaign/result/observationPassed는 수정하지 않는다. 새 표적 결과와 수동 시각 판정을 별도로 기록하며, candidate02의 영향 없는 관측 증거를 명시적으로 재사용한다. 전체60개 합격 기준 PASS, 실입력/음성/성능 검증은 선언하지 않는다.
