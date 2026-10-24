# Dragon-v3 DQA3-003 표적 재검사 소스 인계 · 2026-10-24

**실행 후 갱신: candidate05 두 프로세스 각168 PASS / native exit0, DQA3-003 CLOSED.** 자동 Arrival Idle 공백도 해소했다. [최종 결과](dragon-v3-result-20261024.md)와 [실제 이미지 검토](../Evidence/Dragon-v3/dragon-v3-candidate-05/review/direct-visual-review.json)를 따른다. 소스 동결 해시는 유지됐으며 아래 내용은 실행 전 인계 기록이다. 이 명령을 같은 candidate ID로 반복 실행하지 않는다.

**SOURCE_FROZEN_WAITING_FOR_FIXED_OBSERVED_BUILD.** QA 실행 소스 3개만 변경했다. 제품 수정은 Client 담당의 `BattlePresentation.cs`이며 QA는 읽기·컴파일만 했다. Player/Editor defines 소스 컴파일 오류 0, 기존 DTO 경고 27/28건이고 PowerShell 실행기 구문 검사는 PASS다. Unity·native·EditMode·PlayMode는 추가 실행하지 않았다.

candidate03은 4프로세스 모두 native exit 0, 단언 **35 / 16 / 1,245 / 16 PASS**, Console 오류 0으로 완료했다. 설정 복구·격리 구 저장 sentinel 일치도 확인했다. 이전 candidate02의 1280 5,416프레임과 candidate03의 640 5,539프레임은 각각 51회 관측(고유 이름50개)이며 누락·중복·PNG 해시 불일치가 없다. 자동 검사 통과와 별개로 **DQA3-003 및 Idle 관측 공백은 미해결**이다. 기존 자료는 재수집하지 않는다.

후보04는 1280에서81 PASS /1 FAIL, native exit1, Console 오류0으로 중단했다. 강제 Attack 샘플의 첫 캡처가80ms 슬롯을 지나 프레임0을 건너뛰었고1~7만 관측됐다. 실제 모션은 Attack으로 유지돼 LateUpdate 덮어쓰기를 원인으로 보지 않는다.19 자동 Idle2주기는 통과했으나 제품 회복 검사는 실행되지 않았다. [04 실패 이력](../Evidence/Dragon-v3/dragon-v3-candidate-04/failure-triage.json)을 보존한다.

**2차 동결:** 강제 샘플러와 콜백을 제거했다. 다음 캠페인은 **후보05 / `-RecoveryOnly` / 1280·640 각1회, 총2프로세스**다. 사용자 기본 저장과 OS 입력에 접근하지 않고 새 격리 프로필을 사용한다. 제품 수정 빌드 및 실행권 인계 후 아래 명령을 사용한다.

```powershell
& ./Tools/QA/Dragon-v3/run-campaign.ps1 `
  -Executable 'C:/Github/Test2/Builds/Dragon-v3-QA/LastBellMvp.exe' `
  -CandidateId 'dragon-v3-candidate-05' -RecoveryOnly -Run
```

각 해상도에서 아래5개 관측만 수행한다. 원본 자산의 전체 프레임 목록은 기존 소스·manifest 검증을 채택하고, 실제 전투에서 표시된 Attack 프레임은 강제 샘플 없이 별도로 기록한다.

| 대상 | 검사와 분류 |
| --- | --- |
| 19·20 각각 Idle | 유효한 Arrival 전환 fixture에서 combat clock을 멈추고 Actor의 자동 Idle을2주기 기록한다. 전체 기간의 실제 모션이 Idle이고8프레임 모두 출현하는지 검사한다. **전투 중 Idle 발생 빈도 검증은 아니다.** |
| 19·20 각각 실제 콤보 | 첫 공격 준비 직전부터 예약된 연속 공격 전체(2회 이상)와 마지막 타격 뒤 `clip.Duration + 250ms`까지 실제 세션 시간을 흘려 관측한다. 피해 이벤트 시각은 원래 threat의 impact 시각과 대조한다. 회복·다음 준비·다음 실제 접촉의 연속 프레임과 X 이동을 DQA3-003 타임라인에 대조한다. |
| 인접18 실제 공격 | authored timing이 없는 다른 `SampleEnemy` 분기의 실제 공격·피격·지면 연결만 확인한다. 다른 적 전수·플레이어 전수·이동·서사·저장 재시작은 반복하지 않는다. |

좌우 이동의 자연스러움은 단언 개수로 자동 합격시키지 않는다. 실제 콤보의 연속 프레임과 루트 좌표에서 접촉 다음 프레임에 홈으로 복귀하는 급변이 없어졌는지 확인해야 한다. 발 피벗의 지면 Y 오차와 수평 이동 연속성도 분리한다.

| 동결 파일 | SHA256 |
| --- | --- |
| DragonThemeSmokeProbe.cs | `88a6b29b3bfd6a22ec8477aa3d7d0285ec30e6e4206a3afe389341f4640795a2` |
| DragonThemeSmokeProbe.V3.cs | `9737f351ac7e0e32492211760629fc1f4b00d86c1f93e7f989db49148a2eb671` |
| run-campaign.ps1 | `28672c8e396f88920e56337c12675e335171d4eeacf4dd836b0c5e0080f0d652` |

[2차 컴파일·동결 근거](../Evidence/Dragon-v3/recovery-source-fix-20261024-02/verification.json). 제품 `BattlePresentation.cs` 해시는 `e7ad0f8a2e6ad2074c1096c683b05778a5f1117bc9e9c90627b800de0cece9ec`로 유지했다. [DQA3-003 실제 타임라인 CSV](../Evidence/Dragon-v3/dragon-v3-candidate-02/review/DQA3-003-timeline.csv), [결함 보고서](dragon-v3-DQA3-003-20261024.md). Editor·빌드·제품 소스는 총괄/Client 소유이며 QA는 새 빌드 인계 전 실행하지 않는다. 실입력 검사는 사용자 지시에 따라 보류한다.
