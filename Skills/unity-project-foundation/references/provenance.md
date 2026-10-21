# 추출 근거 — 2026-10-20

아래 경로는 조사한 원본의 위치다. 스킬 실행 시 원본 저장소가 없어도 본문 규칙을 사용할 수 있다. 원본 파일을 복사하거나 원본 에셋을 변경하라는 지시가 아니다.

| 원본 | 확인한 사실 | 재사용 범위 |
|---|---|---|
| Permission-to-win · `C:/Github/StageGame` | `Client`, `Server`, `GameData`, 설계·관리·QA 문서가 분리됨. `Client/Assets/StageGame`에 runtime/editor/tests assembly 구분 | 신규 프로젝트의 책임별 구조 예시 |
| StageGame `Docs/Design/lobby-redesign-20261018/design-spec.md` | 에디터 빌더가 실제 프리팹과 serialized reference를 저장. 런타임 UI 트리 생성 없음 | 편집 가능한 화면 저장 및 참조 연결 |
| StageGame `GameData/FORMAT.md`, `Docs/Management/CONTRACT.md` | Excel 원본과 검증된 JSON을 통한 계약, Core/UI/Platform 책임 분리 | 단일 원본·검증·플랫폼 경계 |
| StageGame의 광고·보상 구현 | 광고 인터페이스, 요청 토큰으로 중복 콜백 차단, HashSet으로 처리 ID 중복 방지 | 외부 콜백과 영구 보상의 멱등성 |
| SimpleGame · `C:/Github/SimpleGame/AGENTS.md` | 사용자가 수정한 프리팹·씬·에셋 보존 | 에디터 생성기가 수동 작업을 덮어쓰지 않는 원칙 |
| SimpleGame 데이터 파이프라인 | Excel→Editor 검증→Generated SO→Manifest | 전체 검증 후 산출물 교체, 설정과 상태 분리 |
| SimpleGame `Planning/ScriptStructure.md` 및 현재 코드 | Session/Presenter, 제한 Pool, Spatial Hash 사용 | 패턴의 존재가 모든 새 게임에 도입할 근거는 아님 |
| SimpleGame `Planning/UIExperience_20260920.md` | HUD가 실제 세션·스포너·정령 상태를 읽음. 별도 UI 게임 타이머 배제 | 상태의 단일 소유자 |

조회한 대화:

- StageGame `클라이언트` · `01a138d9-9d94-71b2-8f03-290654cb2574`
- StageGame `총괄 · 일정 및 출시` · `01a138d9-7ede-70d0-ba36-e84a05e6b105`
- SimpleGame `클라이언트 개발` · `019ff125-4947-7d70-aa5a-f145c2c3c989`

과거 문서와 현재 코드의 날짜를 비교한다. 설계 시안은 구현 사실로, 빌드 성공은 기기 검증으로 취급하지 않는다.

일반화하지 않은 사항: 게임 제목, 특정 폰트 강제, 전투/레이팅 수치, 모든 프로젝트의 서버 의무화, 모든 객체 풀링, 과거 작업의 건별 빌드 승인 조건, 사용자가 직접 배포했던 개별 작업의 제한.
