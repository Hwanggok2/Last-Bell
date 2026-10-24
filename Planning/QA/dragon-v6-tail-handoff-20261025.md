# Dragon-v6 tail · QA SOURCE FROZEN / NOT RUN

[candidate02 결과](dragon-v6-candidate02-result-20261025.md)를 보존하고 프로브 관측 오류와 남은 범위만 보완했다. [tail manifest](dragon-v6-tail-source-manifest-20261025.json)는 QA 소스 동결이며 전체 제품/빌드 freeze가 아니다. 이후 QA 소스 수정과 네이티브 실행은 중지한다.

러너 기존 필수 인자에 **`-ContinueAfterCandidate02`** 사용. candidate01 switch와 동시 사용 금지. candidate02의 정확한 중단점과 raw1000파일을 gate로 보존한다. 정해둔4프로세스/첫 실패중지 정책은 같다.

1280 `v6-tail`: 정상적 미엘·리나 별도0.65초 벼락 표적 → 수정 영향 큰 적5/10/15/20 벼락+이름/방향아이콘 → active/end 기절 → 미실행 동시5접촉(3숫자)/막타/연속1→20 → pending checkpoint/native복원. 이후640 fresh/native는 아직 안 한 전체 예정 범위다. 완료1280 player/companion/legacy/inn/HP를 다시 실행하지 않는다.

관측 보완은 현재 V6 wrapper에만 적용했다. PNG 인코딩 뒤 `TickSession=false`인 채 `Rendered()`를 한 번 지나 긴 unscaled dt를 소진한다. Core 타이머 규칙/시간값은 바꾸지 않는다. 기절21/old-UI없음은 `IsEnemyStunned`인 실제 snapshot에서 단언하고 파일명을 기록하며, 종료0은 별도 ended snapshot으로 확인한다. 소스 당시 보관본은 candidate02/review/frozen-qa-source에 원본 SHA로 남겼다.

최신 시각 계약: HP중심(640,38)560×44 유지, 보스 이름중심(900,84)440×40으로 방향/일시정지 영역과 분리. 벼락은 총괄의 동일중심 Ink/bright 폭 변경만 반영하며4PS/376입자/게임 규칙은 유지된다. 이 변경의 실제 화면 PASS는 아직 없다.

정적 검사 UTC2026-10-24T16:35:14.3084531Z: Runtime76 C#9파일 오류0, PowerShell2 AST 오류0, Python AST PASS. DLL emit/Unity 실행/새 native 실행 없음. 보고서와 수동review만 새로 기록했다. 새 observed build/RuntimeSHA/전체 freeze/실행 소유권 인계 뒤 재개한다.
