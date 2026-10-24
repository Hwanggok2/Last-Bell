# Dragon-v3 QA 소스 동결 인계

2026-10-24 KST. **준비 소스 동결 / Unity·게임·관측 캠페인 NOT_RUN.** 앱0.9.2·데이터mvp1.15.0·`Builds/Dragon-v3-QA` 기준이다. 총괄의 최종 아트/Unity 통합과 관측 빌드·실행권 인계를 기다린다. 이번 준비에서 사용자 기본 저장 프로필에 접근하지 않았다.

## 변경 파일

- [QA 계획](dragon-v3-plan-20261024.md):11~20 적·이름·20 뒤 이야기·저장 분리·ground base·플레이어7모션·스타일·미구현 스킬/재미 기획의 완료 기준.
- [DragonThemeSmokeProbe.V3.cs](../../Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.V3.cs)와 `.meta`: manifest 기반 비교, 신규 적/플레이어/보스 프레임 관측, 지면 좌표, 새 시작/동일 저장 native 복원,20 뒤 이야기 경계.
- [기존 probe](../../Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.cs): v3/v3-resume 허용·라우팅·resume preflight·V3만600초 제한·V3 자원 정리. 기존 v1/v2 소스나 결과는 재실행하지 않았다.
- [전용 실행기](../../Tools/QA/Dragon-v3/run-campaign.ps1), [공통 preflight](../../Tools/QA/Dragon-v3/common.ps1), [사용 안내](../../Tools/QA/Dragon-v3/README.md), [EditMode 필터](../../Tools/QA/Dragon-v3/target-filters.json).
- `Tools/QA/Dragon-v3/` 내부의 소스 컴파일·경로/selector 자체 검사·순수 C# fixture 검사. 제품·Client 테스트·데이터·아트·씬·프리팹은 QA가 변경하지 않았다.

## 준비 검증 결과

| 검사 | 결과 |
| --- | --- |
| C# 전체 Runtime 소스, Windows observed defines | PASS, `DEVELOPMENT_BUILD + UNITY_STANDALONE_WIN + LASTBELL_PLAYTEST_092` |
| C# 전체 Runtime 소스, Editor defines | PASS. Unity 프로세스 실행 없음 |
| selector/경로/PowerShell 자체 검사 |15/15 PASS. 가상 구 primary/backup 보존, override 우선, 재선택 비초기화, traversal/형제prefix/루트 자체 거부 |
| 순수 C# V3 fixture 검사 |55/55 PASS. 숲10층 피격 생존·Stun 진입/해제,15/20 BossIntro,20 이야기/보상/완주/skip 경계 |
| 계획 문서 로컬 링크 |깨진 링크0 |
| 완성 입력 preflight |대기: 검사 시 `dragon_goblin_scout`가 최종 runtime art manifest에 아직 없어서 명시적으로 거부. 아트 통합 전 정상적인 차단이며 게임 실행 없음 |
| Unity EditMode5 / PlayMode / native 관측 |모두 NOT_RUN. 기존 PlayMode11 재실행 계획 없음 |

컴파일의 CS0649는 JsonUtility로 채우는 DTO와 Editor 분기 필드의 경고다. 준비 중 발견한 잘못된 BossIntro fixture의 공격 예약은 제거했다. 기본 HP로 숲 공격을 받으면 사망하므로 정상 HP 성장값 fixture로 바꾸고 실제 피해 후 생존을 자체 검사했다. 전투 데이터는 바꾸지 않았다.

## 실행 인계 때 할 일

1. 최종 forest10종/Player-v3 manifest·원본해시·Unity 연결, 새 지면 GroundTiles, 관측 빌드의 playtest define과 버전/해시를 고정한다. 실행기는 이미지 해시와 보호 데이터 도메인을 검사한 뒤에만 프로세스를 시작한다.
2. 허용된 기존 Editor에서 Client의 `SaveDirectorySelectorTests`5 cases만 실행한다. 사용 전 상태를 기록하고 검사 후 비Play/씬/설정을 반환한다. 현재 Editor 소유권은 총괄에 있다.
3. 신규 candidate로1280/640 각 관측+같은 경로 resume, 총4프로세스를 한 캠페인으로 수행한다. 사용자 기본 프로필은 계속 비접근이며 selector/default 계약과 override native 검증을 구분한다.
4. 신규 적과 플레이어의 실제 프레임·보스 발/얼굴·복장/스타일·이름/대사/스킬 미구현 표기를 별도 검토한다. 모션 자연스러움과 실제 source pose 관측 한계를 적고 정상속도 재생을 수행한 경우에만 그 결과를 주장한다.

최종 실행 결과·시각 합격·일반 배포 빌드/ZIP 검증은 아직 이 문서의 완료 범위가 아니다. OS 실입력·모바일 보류와 v2 QA 종료 상태를 유지한다. 파일 해시와 준비 검증의 기계 기록은 [source verification](../../Tools/QA/Dragon-v3/Validation/source-verification.json)을 따른다.
