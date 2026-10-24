# Dragon-v3 QA 준비 소스

2026-10-24 KST. **소스 준비/동결, Unity·게임 관측 NOT_RUN.** 최종 아트 통합·관측 후보·실행권 인계 전 게임을 시작하지 않는다. [QA 계획](../../../Planning/QA/dragon-v3-plan-20261024.md)과 [동결 인계](../../../Planning/QA/dragon-v3-source-handoff-20261024.md)를 따른다.

관측은 `Builds/Dragon-v3-QA`, 앱0.9.2/데이터mvp1.15.0이며 `LASTBELL_PLAYTEST_092` define이 필요하다. 일반 배포 빌드는 실행하지 않는다. OS 실입력과 모바일은 보류한다. **사용자 기본 저장 프로필을 읽거나 쓰지 않는다.**

`DragonThemeSmokeProbe.V3.cs`는 Development/Editor 조건부 partial이다. 기존 probe의 opt-in·격리 경로·중복 결과 거부·저장 씬 Bootstrap을 사용한다. `v3`와 `v3-resume`만 추가했고 기존 v1/v2 캠페인을 호출하지 않는다. V3 관측의 timeout은600초, native 실행기의 own-process timeout은630초다.

## 실행 전 인계 조건

- 최종 forest10종과 Player-v3가 Unity manifest/저장 자산에 연결돼 있어야 한다. `Assert-V3Inputs`는 10층 바인딩, mvp1.15.0, 보호 도메인 불변, 실제 이미지 SHA256을 먼저 확인한다. 미완성 아트는 프로세스 시작 전에 거부한다.
- runtime clip의 프레임 수·선택 source frame·hold·fps·contact·pivot·크기는 복사한 최종 manifest와 대조한다. 옛 아트의 고정 프레임 수/몸높이로 검사하지 않는다. 패턴별 공격 alias는 새 art의 명시적 clip 또는 동일 art의 Attack 재사용으로 기록한다.
- 저장 API는 `SaveDirectorySelector.Select(persistent,args,true)`의 `Playtests/dragon-0.9.2`, override 우선 계약이다. 기본 namespace는 가상 경로와 소스/표적 EditMode로 평가한다. 실제 사용자 기본 경로로 exe를 실행하지 않는다.
- [target-filters.json](target-filters.json)은 Client 작성 `SaveDirectorySelectorTests`4메서드/5 cases만 지정한다. 인계 후 기존 지정 Editor에서 실행하며 이전 PlayMode11 cases는 반복하지 않는다.

## 준비된 캠페인

다음 명령은 **현재 실행하지 않는다.** 완성 후보와 실행권을 인계받은 후에만 사용한다.

```powershell
& C:\Github\Test2\Tools\QA\Dragon-v3\run-campaign.ps1 `
  -Executable C:\Github\Test2\Builds\Dragon-v3-QA\LastBellMvp.exe `
  -CandidateId dragon-v3-candidate-01 -Run
```

1280×720의 fresh 관측→동일 경로 native resume, 640×360의 fresh→resume, 총4프로세스가 한 캠페인이다. 실패하면 남은 프로세스는 시작하지 않는다. `fresh/profiles/boot`에만 저장하고 재실행은 이 경로를 그대로 연다. 재실행의 결과 폴더는 부모라서 이전 결과·PNG를 덮어쓰지 않는다. 신규 candidate 경로만 허용하고 기존 결과 자동 재실행은 거부한다.

- 첫 부팅: 프롤로그→로비/선택→강화→Stage1 튜토리얼, 루카/다렌/미엘 이름과1명 해금.
- 적10종: 각2주기 Idle, 첫 실제 공격/플레이어 Hit, 실제 Stun 진입·해제. 다른 도달 가능 공격의 clip 연결도 정적으로 대조한다. 모든 랜덤 패턴 순서를 실전 관측했다고 주장하지 않는다.
- 플레이어: 초원/숲 대표에서 Idle·Attack·Up/Back/Down·Hit·Run, 보스15/20 상대 Attack.7모션의 source frame·root·body offset/scale·발 pivot/화면 투영을 기록한다. 공중/전진 offset과 실제 보이는 발의 자연스러움은 별도 시각 검토다.
- 보스15/20: 실제 세션 BossIntro의 전투 차단→Roar→복귀.10→11,14→15,15→16 전환과20 처치 뒤3페이지·중간 저장 복원·다음/건너뛰기·명시적 보상·완주를 확인한다.
- 관측 fixture는 정상 HP 성장 최대값을 사용해 숲 적의 실제 피해 후 Hit/복귀를 볼 수 있게 한다. 공격 성장·전투 데이터·피해량은 바꾸지 않는다. 새 사용자 첫 진행과 밸런스 성능 검사는 이 fixture로 대체하지 않는다.
- 새 진행을 식별 가능한11층 fixture로 저장하고 같은 디렉터리의 다음 native 프로세스에서 phase/run ID/층/선택/재화/성장/읽음/유물을 비교한다. 이것과 selector 검사, 사용자 기본 프로필의 비접근 상태를 각각 기록한다.

각 연속 구간은 실제 렌더 프레임을 EOF에서 CPU 버퍼에 모으고 구간 종료 후 PNG를 인코딩한다. 구간당 raw768MiB 상한이며 조용히 프레임을 버리지 않는다. `frames.jsonl`에 양 배우의 clip·source frame·화면/지면 투영·카메라·HP·게임/실제 시간·SHA256, `v3-evidence.json`에 지면1~20·clip/alias·구간 수집 결과를 남긴다. 캡처 부하가 있으므로60fps/성능 인증은 하지 않는다.

`result.json.passed`는 자동 규칙과 수집 결과다. `visualReview`와 `naturalMotionReview`는 자동으로 `NOT_RUN`을 유지한다. 실제 원본 PNG/시퀀스를 검토한 후 별도 결과 보고서에서 판정한다. 정상속도 직접 재생을 하지 않았다면 체감 자연스러움 검증 완료로 쓰지 않는다.

## 저장·설정 보호와 소스 검증

실행기는 사용자 기본 프로필을 열지 않으며 QA 폴더의 synthetic legacy sentinel만 전후 비교한다. 이를 실제 사용자 파일 해시 감사로 표현하지 않는다. scoped `LastBell.*`/창 설정 PlayerPrefs는 백업·복원하고 원래 native exit code를 보존한다. 중단 시 자신이 시작한 Process handle만 종료한다.

소스 준비 검사는 Unity를 실행하지 않는다.

```powershell
& C:\Github\Test2\Tools\QA\Dragon-v3\self-test.ps1
& C:\Github\Test2\Tools\QA\Dragon-v3\compile-source.ps1 -PlayerDefines
& C:\Github\Test2\Tools\QA\Dragon-v3\compile-source.ps1
dotnet run --project C:\Github\Test2\Tools\QA\Dragon-v3\SourceChecks\SourceChecks.csproj -- C:\Github\Test2\Client\Assets\Game\Data\Generated\mvp-catalog.json
```

`self-test`는 selector/경로 보호/PowerShell 구문15검사다. `SourceChecks`는 신규 fixture의 생존·Stun·보스 전환·20 이야기/보상55검사다. C# 컴파일 산출물은 이 폴더의 `Validation`에만 쓰며 Unity Library/씬/프리팹을 갱신하지 않는다. JSON 반사 로딩 DTO의 CS0649 경고는 컴파일 로그에 남긴다. 준비 검사의 통과와 Unity/관측 빌드의 실행 검증은 별개다.
