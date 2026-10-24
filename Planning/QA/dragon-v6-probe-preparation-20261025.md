# Dragon-v6 probe · SOURCE FROZEN / EXECUTION NOT RUN

2026-10-25 KST. 현재 필수 사례로 구현을 고정했다. [소스 manifest](dragon-v6-probe-source-manifest-20261025.json)는 QA 소스 동결이며 제품/빌드 전체 동결이 아니다. [실행 설명](../../Tools/QA/Dragon-v6/README.md)의 인자와 총괄의 최종 빌드·실행 소유권 인계가 필요하다. Unity/MCP/Apply/빌드 및 V6 게임 실행은 하지 않았다.

## 인계 파일과 검사

- `DragonThemeSmokeProbe.V6.cs`와 Unity가 생성한 meta. 공용 entry에는 V6 모드/preflight/dispatch/timeout/cleanup만 추가했다. V5 partial/러너/보고서/증거는 수정하지 않았다.
- `Tools/QA/Dragon-v6/`의 common, runner, analyzer, README. 경로·해시 유틸은 기존 `Tools/QA/Dragon-v3/common.ps1`을 재사용한다. **총괄의 최종 freeze.files에도 이 V3 common을 포함해야 한다.**
- C#9/개발빌드 심볼로 Runtime 76개 소스의 Roslyn 구문·타입 진단 오류 **0**. 2026-10-24T15:58:29Z 검사이며 DLL emit·Unity 실행은 없었다. PowerShell 두 파일 AST, Python AST, 문서 링크 및 최종 SHA 검사는 manifest에 기록한다.
- [Client 최종 인계](../Production/dragon-v095-client-handoff.md)의 9547+217 assertions, 데이터 30 PASS와 [총괄 Unity 검사](../Production/dragon-v6-unity-editmode.json)를 참조한다. QA가 해당 검사들을 재실행한 결과가 아니다.

## 고정된 캠페인

한 캠페인에서 순차 4프로세스만 실행한다. 1280 fresh는 이전 QA schema9 seed로 부팅하고, 아래 변경 사례와 연속 1→20을 관측한 뒤 Daren 독립 스킬/Tori 발동 예약을 저장한다. 1280 resume은 그 별도 복사본을 복원한다. 640 fresh는 같은 변경 사례 뒤 Miel/Tori 접촉 후 상태를 저장하며, 640 resume은 지난 접촉이 재생되지 않는지 관측한다. 첫 실패에서 중지하며 자동 재시도는 없다. 각 프로세스 제한은 프로브 1800초/러너 1830초다.

| 구현 범위 | 관측 방식과 제한 |
| --- | --- |
| 여관 | 복원 플래그/숨김 요소, 우상단 동료 진입, 회복 1회 구매, 5종 강화 전후 텍스트·확률·주기·예상 DPS·실제 저장, L30 cap. 모든 거래/부족/취소 조합을 구현한 것은 아님 |
| HP/효과 | HP 100%/50%/1 milli/처치, 5종 기본·발동 각각 1접촉, 흰색 피격·큰 숫자·최대 겹침 PNG, 실제 기절 21입자와 정리. HP cell/glyph mesh나 가독성 자동 PASS 없음 |
| 플레이어 스킬 | 3종 공격→스킬/역순, pending pause, 접촉 1회씩, 파일 roundtrip, 마지막 100ms CD 경계 fixture, 같은 적 막타 중복 억제. 20초 전 구간·회피/상태별 모든 경계는 Client 근거 참조 |
| 동료 | 실제 Core가 선택한 deterministic seed로 미발동/발동 예약, pause/RNG/예약/잔여시간 저장, 모모 실제 회복. 각 종 3주기 또는 확률분포 관측 주장은 하지 않음 |
| 큰 적 벼락 | 5/10/15/20단계 Miel+Rina 접촉 창과 후기 캡처, 실제 적 중심/상공 여유 기록. headroom<48이면 수동 확인 경고이며 접촉 이벤트만으로 시각 PASS 금지 |
| 진행/복원 | 1280에서 stage1→20 연속 실제 처치·대사·보상·guided/MVP, native 복원 2지점. 640 전체 경로·모든 캐릭터 native 복원 주장은 하지 않음 |

고HP·near-stun·짧은 최초 동료 타이머·L30·미래 3슬롯은 명시한 DTO fixture다. 3슬롯의 highestClearedStage30은 규칙 fixture일 뿐 21–50 플레이가 아니다. 연속 경로는 attack30/HP50/recovery20 성장 준비를 사용하고 이후 stage를 주입하지 않는다. 이미 본 프롤로그/기초 튜토리얼에서 출발하므로 새 계정 전체 onboarding 또는 정상 난이도 검사가 아니다. 긴 공백/경로 시계는 수동 진행하고, 접촉 창은 실제 렌더 프레임을 수집한다.

최종 계약은 4PS/376입자(256+32+24+64), 기절별 21입자, 보스 HP 중심(640,38)/이름(640,84)이다. 벼락 상공 표현·동료 흰색 피격·640 가독성·HP 픽셀·숫자 겹침은 원본 PNG를 열어 별도 판정한다. 상공 벼락 요청이 충족되지 않으면 fallback을 허용한 코드나 contact PASS와 무관하게 실제 시각 결함으로 남긴다.

## 남은 단계

총괄이 observed build/전체 freeze/실행 소유권을 인계하면 지정된 새 candidate에서 한 번 실행하고 원본을 보존한다. 분석기는 읽기 전용이다. 실제 검토 파일과 시각 판단은 별도 review JSON에 기록한다. 제품 결함은 직접 영향 범위만 좁게 재검하고, 프로브/환경 오류는 미실행 범위와 원인을 보고한 뒤 재개 인계를 받는다.

`acceptance=NOT_CERTIFIED`, 실입력·모바일 보류, 청음·성능 미검증을 유지한다. 사용자 기본 프로필은 읽기/복사/해시 모두 하지 않는다. 준비 완료는 실행 PASS 또는 44개/기존 60개 전체 인증이 아니다.
