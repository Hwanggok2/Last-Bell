# Dragon-v6 continuation · QA SOURCE FROZEN / NOT RUN

현재 필요한 저장 비교 수정·팝업 증거·미실행 범위 재개만 준비했다. QA 소스 수정은 중지한다. [새 QA manifest](dragon-v6-continuation-source-manifest-20261025.json)는 제품/빌드 freeze를 대신하지 않는다. 총괄의 새 observed build/RuntimeSHA/전체 freeze/실행 인계 후 새 candidate에서 실행한다.

- 원인 확정: [총괄 Unity 읽기 진단](../Production/DragonV095ClientValidation/roundtrip-readonly-result.json)에서 `run.skillCooldownRemainingMs`만 `19356.571599841118 → 19356.57159984112`, **1 ULP**, recovered=false. QA의 Python 필드 대조도 이 한 항목만 확인했다. Core 변경 없음. candidate01 원본 실패는 그대로 보존한다.
- `V6CompareDto`는 Unity 직렬화의 DTO 모양과 optional-inline 정규화를 기준으로 재귀 비교한다. 정수/불리언/enum/문자열/목록 순서는 엄격하며, **원래 메모리 float/double값은 loaded와 직접 비교해 유한값·최대4ULP만 허용**한다. null string→empty와 inline 표현은 Unity wire shape로 한정한다. before/loaded/restored JSON, 필드 경로·전후·ULP·허용 여부를 저장한다. 복원/native 동료의 remainingMs/damageFraction에도 적용했다. 접촉1회/잔여시간/쿨다운 검사는 유지한다.
- 팝업은 개발용 read-only reflection으로 실제 digits/raw milli/남은 수명/lane을 읽는다. 변경/주입은 없다. 5접촉 fixture에서 일반·독립스킬 각각1개, 동료 합계1개인 **총3숫자** 및 raw milli 합산 후1회 반올림을 단언한다. 1.15초 렌더 창으로 수명 종료도 확인한다. configured color와 expected glyph tint를 구분하며 **실제 청록색·가독성·HUD 비중첩은 PNG 판정**이다. 사각 경계 겹침은 보수적인 경고일 뿐 실제 glyph 실패로 자동 판정하지 않는다.
- 러너에 **`-ContinueAfterCandidate01`** 추가. 기존 필수 인자는 동일하다. 첫1280 과정은 `v6-continue`로 끝낸 legacy/여관/HP를 건너뛰고 루카 스킬/저장 회귀에서 시작한다. 이후 남은1280 사례/연속1→20/native복원과 미실행640 두 과정을 순서대로 수행한다. candidate01의 기록된 raw146파일과 정확한 중단점을 gate로 검사하고 실행 전후 보존을 확인한다. 자동 재시도 없음.
- 기존 QA 소스8개는 [candidate01 보관본](../Evidence/Dragon-v6/dragon-v6-candidate-01/review/frozen-qa-source/)에 원래 SHA 그대로 복사했다. 원래 manifest/실행 결과/PNG는 변경하지 않았다.

정적 검사: **Runtime76 C#9 파일 진단 오류0**, PowerShell2 AST 오류0, Python AST PASS. 검사UTC `2026-10-24T16:24:11.6473351Z`. DLL emit·Unity 실행·새 네이티브 실행은 QA가 하지 않았다. 수정 완료는 실패 재검 PASS가 아니다. DQA6-001과 최대 동시 타격 표시, 큰 보스 벼락 등은 새 캡처를 기다린다.
