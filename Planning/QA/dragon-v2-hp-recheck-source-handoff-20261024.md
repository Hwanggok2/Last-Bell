# DQA2-001 HP 표적 재관측 소스 인계

최종 결과: 총괄의 수정 빌드 인계 뒤 후보02에서 HP전용 두 케이스70검사씩/native exit0, PNG30상태의 혼색0·예상 픽셀 수 일치를 확인했다. **DQA2-001 CLOSED**. [최종 QA 보고서](dragon-v2-result-20261024.md). 아래 NOT_RUN은 최초 소스 인계 시점의 기록이다.

2026-10-24 KST. **SOURCE_FROZEN / PREPARED_NOT_RUN.** 총괄의 별도 지시에 따라 QA 소스3파일만 최소 수정했다. 제품 HP 위치 수정은 Client 담당이며 QA에서 변경하지 않았다. 새 빌드·실행 인계 전에는 실행하지 않는다.

- `DragonThemeSmokeProbe.cs`: 허용 case에 `v2-hp`를 추가하고 기존 partial로 연결한다.
- `DragonThemeSmokeProbe.V2.cs`: `PlayerV2(hpOnly:true)`는 버전/해상도·격리 bootstrap 확인 뒤 **기존 `V2Health()`만 실행**하고15바 상태 수를 확인한다. clip 검사·Idle/Attack/회피/Run 관측을 호출하지 않고 모션 index를 만들지 않는다. 정리/설정/종료 경로는 재사용한다.
- `run-campaign.ps1`: `-V2HpOnly`는 다른 표적 옵션과 동시 사용을 거부하고 v2 허용 빌드/새 증거 경로에서 `v2-hp-1280x720`, `v2-hp-640x360`만 실행한다. 기존 v1/v2 결과를 덮지 않는다.

[소스 SHA256](dragon-v2-hp-recheck-source-hashes-20261024.json). 소스 읽기만으로 경로를 대조했고 컴파일·Unity·테스트·빌드·게임 실행은 추가하지 않았다. 최초 후보01의 자동130×2 PASS/native exit0,11/11 PlayMode PASS와 모션 증거는 계속 유효하다.

인계 후 실행 형식(아직 실행하지 않음):

```powershell
& C:\Github\Test2\Tools\QA\Dragon-v1\run-campaign.ps1 `
  -Executable C:\Github\Test2\Builds\Dragon-v2-QA\LastBellMvp.exe `
  -CandidateId dragon-v2-candidate-02-hp -V2HpOnly -Run
```

재관측은 HP30상태의 자동 mesh/폭/높이/중심 검사와 실제 PNG 색·픽셀 행·tiny/채움 단조 증가·이름/선반 간격을 대조한다. 전체 모션, PlayMode, 캠페인은 재실행하지 않는다. OS 실입력 보류도 유지한다.

Client 전달용: [원본640 PNG](../Evidence/Dragon-v2/dragon-v2-candidate-01/v2-640x360/v2-hp-640x360-stage-1-100.png), [관측 JSON](../Evidence/Dragon-v2/dragon-v2-candidate-01/hp640-raster-observation.json). 플레이어 bounds x210/y84.3870926/80×18(하단 원점). PNG 좌상단 원점(218,261)=RGB126,165,146,(218,262)=114,185,155,(218,263)=75,151,132,(218,271)=37,89,89.
