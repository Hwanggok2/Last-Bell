<!-- dragon-v6-publication:start -->
# 현재 적용: 독립 스킬·동료 성장·도트 HP v0.9.6 · 2026-10-25

실행 범위1~20, 데이터mvp1.18.0. 기존 저장 schema9·Playtests/dragon-0.9.2를 유지한다.

- 기존 여관의 좌측 스탯 선택→우측 견적→하단 강화 구성을 복원하고, 동료 진입은 우측 상단에 둔다.
- 플레이어 스킬은 공격·회피 중에도 입력한다. 입력부터20초 쿨타임, 독립200ms 후 접촉1회이며 기존 동작을 덮지 않는다.
- 동료5종 전용 스킬은 해당 차례 기본 공격을 대체한다. 레벨마다 발동률+1%p, 기본 주기의1% 감소. 현재→다음 확률·주기·기대DPS를 표시한다.
- 동료도 적에게 흰색 피격·도트 파편을 발생시킨다. 피해 숫자는5×7 비트맵, 일반6px/치명8px 셀과 외곽선,0.85초 우상단 이동.80ms 동료 타격은 합산해 한 숫자로 표시한다.
- 모든 HP바는 초록 실제 HP를 즉시 표시한다. 단일 실제 피해가 피격자 최대HP의1/3 이상이면 주황 잔상이120ms 유지 후550ms 선형 보간으로 감소한다. 회복·새 대상에서 초기화하며 일시정지 중 정지한다.
- 5종 동료 기능은 구현됐지만 새 계정의 현 구간에서는 토리·바룬만 만난다.21~50 및 모모42 첫 만남은 후속 제작이다.

기존 v6 관측·재실행 QA 완료. HP 추가 범위는15개 PlayMode 검사와1280/640 두 프로세스·원본18장 시각 검토를 완료했다. 전체 인수·OS 실입력·모바일·직접 청음·정상 속도 성능 검증은 별도다.

일반 실행판 `Builds/Dragon-v6/LastBellMvp.exe` · [v0.9.6 ZIP](../Builds/Dragon-v6-Windows-0.9.6.zip) · [HP QA](../Planning/QA/dragon-v6-candidate05-result-20261025.md) · [기존 변경 QA](../Planning/QA/dragon-v6-candidate04-result-20261025.md). 일반판은 패키지 검증이며 관측판과 동일 프로덕션 소스를 사용한다.

[최신 기획](../Planning/Story/Dragon-v1/Adventure-v6/index.html) · [적용 기록](../Planning/Production/dragon-v6-integration.json) · [변경 내역](../Planning/Production/dragon-v6-release-notes.md)
<!-- dragon-v6-publication:end -->

---

<!-- dragon-v5-publication:start -->
# 이전 적용: 동료·회복력·도트 이펙트 v0.9.4 · 2026-10-24

현재 플레이는1~20스테이지다. 저장 씬·프리팹 적용 및 Windows 패키지를 연결했으며,21~50은 제작 완료 범위가 아니다.

- 플레이어·NPC 발화에 따라24px 초상의 좌우와 이름표 위치가 바뀐다. 기존9종에 모모를 더해10종이다.
- 동료5종의 Idle·Attack·Run 15클립98프레임과 여관 편성·강화·서비스를 연결했다. 새 계정은 토리2·바룬12, 이전 저장은 기존 해금 최대4명을 지원한다. 모모42는 미래 구간 잠금이다.
- 회복력을 다섯 번째 강화 스탯으로 추가했다. 적 처치마다 한 번 정량 회복하며,10·20 보스 뒤 추가 완전 회복은 제거했다.
- Stage5 첫2연타는 위 회피→아래 쳐내기의 새 입력을 확인한다. 몬스터 후퇴·준비·전진과 예고는 실제 적중 시각에 맞춘다.
- 스킬·동료 발사체·강화·축복·회복에 실제 ParticleSystem을 연결했다. 효과는 Core의 판정·타이머를 대신하지 않는다.
- 기존 `Playtests/dragon-0.9.2` 저장 namespace를 유지한다. 버전 변경으로 기존 진행을 초기화하지 않으며, 명시적 CLI save-dir이 우선한다. 새 저장만 최초 흐름에서 Stage1로 시작한다.

**확인된 QA 범위:** candidate02의 두 해상도 통합·재시작 4프로세스 및 영향 없는 시각 관측을 채택했습니다. candidate03에서는 바룬의 Stage12 대화만 3캐릭터·2해상도, 30페이지로 재검증해 초상화 오류를 해결 확인했습니다.

통합 기록: Core 217 assertions / 데이터 28 tests / 실제 Unity EditMode 198 passed. 최종 화면 campaign은 별도 2개 프로세스이며 이전 후보·채택 증거를 합산한 숫자가 아니다.

- 전체60개 기준 인증이 아닙니다. 일반 실행판은 패키지 무결성을 확인했으며 별도 전체 플레이 검사는 하지 않았습니다.
- OS 실입력·모바일 실기기·직접 청취·정상 속도 성능 검사는 미실시입니다.
- 기절 입자는 실제 상태에 맞춰 표시·종료되지만 작은 화면에서 별 모양과 순간 식별성은 추가 검토 대상입니다.

일반 실행판은 `Builds/Dragon-v5/LastBellMvp.exe`, 공유 묶음은 [v0.9.4 ZIP](../Builds/Dragon-v5-Windows-0.9.4.zip). 일반판은 패키지 검증이고, 관측판의 실제 표적 QA를 일반판 전체 플레이 통과로 확대하지 않는다.

[최종 QA](../Planning/QA/dragon-v5-result-20261024.md) · [실행 기록](../Planning/Evidence/Dragon-v5/dragon-v5-candidate-03/campaign.json) · [배포 기록](../Planning/Production/dragon-v5-release.json) · [여관 인계](../Planning/Production/dragon-v5-inn-handoff.md) · [Core 인계](../Planning/Production/dragon-v094-client-handoff.md)
<!-- dragon-v5-publication:end -->

---

# 이전 v0.9.3 제작 기록

저장 프리팹과 MVP 씬에 적용했다. 현재 실행 범위는 1~20스테이지다. [통합 기록](../Planning/Production/dragon-v4-integration.json)에서 최종 QA·배포 상태를 확인한다.

- 루카(on), 다렌(dam), 미엘(rin) 모두 선택 가능. 각각 바람 베기·철벽 반격·별빛 봉인을 하단 중앙 버튼 또는 E로 사용한다. 쿨타임은 전투 시간 20초이며 이동·대화·일시정지 동안 멈춘다. 스킬 접수 뒤 취소하면 쿨타임만 유지하고 접촉 효과는 발생하지 않는다.
- 다렌·미엘의 새 32px 원본104장, 7클립씩132프레임 참조를 연결했다. 루카의 기존 모션을 기준으로 의상·무기를 유지하는 별도 캐릭터다.
- 예고는 노란색과 짙은 외곽선을 적중 직전까지 유지한다. 기절바 본체340×6→408×7.2, 하늘색 누적→노란색 준비→민트색 기절. Stage11 정면 돌진은 뒤 회피로 대응한다.
- 2/7/15 만남과 10/20 보스 뒤 대사25페이지, 선택 주인공별 응답, 초상화9종을 연결했다. 이름은 대화창 본문 좌상단이며 소개 전에는 ???로 표시한다. 각 캐릭터별 최초 1회와 대화 중 복원을 지원한다.
- 공격 입력은 39ms 도트 터치음, 실제 적중은 114ms 도트 타격음. SubspaceAudio의 CC0 음원을 고정 게인만 조정했으며 배포 라이선스를 포함한다. [청취 자료](../Planning/Story/Dragon-v1/Audio-v2/index.html).
- Windows 저장 namespace와 define은 기존 `Playtests/dragon-0.9.2` / `LASTBELL_PLAYTEST_092`를 유지한다. 버전 변경으로 기존 Dragon 진행을 초기화하지 않는다. 명시적 CLI save-dir이 우선하고, 최초 진행은 Stage1 튜토리얼로 시작한다.

Core 변경246 assertions와 데이터26검사는 통과했다. native QA와 실행판 정보는 [현재 제작 현황](../Planning/Story/Dragon-v1/index.html#unity-build)에 기록한다. OS 실입력·모바일 실기기 및 직접 청취 검증은 별도 범위다. [Client API 인계](../Planning/Production/dragon-v093-client-handoff.md), [새 모션](../Planning/Story/Dragon-v1/Production/Player-v4/review.html), [대화 미리보기](../Planning/Story/Dragon-v1/dialogue-v4.html).

일반 실행판은 `Builds/Dragon-v4/LastBellMvp.exe`, 공유 묶음은 [v0.9.3 ZIP](../Builds/Dragon-v4-Windows-0.9.3.zip)이다. [최종 QA](../Planning/QA/dragon-v4-result-20261024.md)와 [배포 기록](../Planning/Production/dragon-v4-release.json)에 검증 범위와 한계를 기록했다.

---

# 이전 v0.9.2 제작 기록

1~20스테이지의 새 판타지 테마를 연결했다. 이번 변경은11~20의 고블린·숲송이10종, NPC와 맞춘 루카의 외형, 지면 기준 통일, 이름·대사와 별도 Stage1 시작 기록이다. **1280×720·640×360 관측 및 공격 복귀 표적 QA 완료.**

- 루카의 원본52장과7클립66참조를 보정했다. 얼굴·검·손·발 위치, 달리기400ms, 베기·위/뒤 회피·아래 쳐내기 타이밍을 유지하며 머리/가방 외곽과 튜닉/스카프 색을 통일했다.
- 새 숲10종은 Idle·Attack·Stun30클립235샘플이다. 문지기 베기와 기블의 당김→찌르기는 직접 관절을 지정해 보강했다. 기블은6샘플/4고유 자세이며 접촉 뒤300ms는 원형 대기 자세를 유지한다. 잘린 창·방패가 변형된 시안은 제외했다.
- 지면은 계획Y590의 공통 기준이다. 원본 발 피벗과 실제 바닥을 연결하며 달리기/회피의 의도한 체공은 유지한다. 11~20의 바닥 색조만 구분했고 별도 숲 배경 원화는 아직 없다. 몬스터4종은 기존Wood, 무장 고블린6종은Metal 효과를 사용한다.
- 이름은 루카(on)·다렌(dam)·미엘(rin), 보스 기블이다. stable ID·진행도·수치·해금은 유지한다. 현재 루카만 선택 가능하며 전용 스킬과 차별화 요소는 기획서에만 추가했다.
- Windows 일반/관측 빌드는 `LASTBELL_PLAYTEST_092`를 해당 빌드에만 사용한다. 새 저장 위치는 `persistentDataPath/Playtests/dragon-0.9.2`. 첫 진행은 프롤로그→선택·강화→Stage1 튜토리얼이며 이후에는 이 진행을 이어간다. 기존 저장은 보존하고 명시적인 save-dir override는 우선한다. Editor는 기존 경로를 사용한다.

[기획·현재 적용](../Planning/Story/Dragon-v1/index.html#unity-build) · [1~20 모션 뷰어](../Planning/Story/Dragon-v1/Production/motion-v3-review.html) · [50스테이지 지면](../Planning/Story/Dragon-v1/index.html#stage-ground) · [전용 스킬 기획](../Planning/Story/Dragon-v1/index.html#character-skills) · [적용 기록](../Planning/Production/dragon-v3-integration.json).

실행판 경로는 `Builds/Dragon-v3/LastBellMvp.exe`, 공유 ZIP은 `Builds/Dragon-v3-Windows-0.9.2.zip`이다. ZIP의 전체 파일 해시·무결성·라이선스와 관측 코드 제외를 확인했다. [최종 QA](../Planning/QA/dragon-v3-result-20261024.md), [배포 기록](../Planning/Production/dragon-v3-release.json)을 참고한다. 저장 관련 EditMode5/5는 통과했다. 21~50 플레이, 추가 캐릭터·전용 스킬 구현, 전용 숲 배경은 이번 완료 범위가 아니다. OS 실입력·모바일 실기기 검사는 이전 보류를 유지한다.

공격 뒤 순간 복귀(DQA3-003)는 실제 공격→직전 회복→다음 예고 준비의 표시 순서로 수정했다. 타격 시각과 밸런스를 유지한 채 두 해상도의 해당 구간만 재검사했다. [공격음 비교 후보](../Planning/Story/Dragon-v1/Audio-v1/index.html)는 기획 자료이며 현재 게임 음원은 유지한다.

---

# 이전 적용: 모험가 모션·체력바 도트 개선 v0.9.1 · 2026-10-24

32×32 원본·8색의 모험가로 플레이어를 교체했다. 갈색 머리, 붉은 스카프, 초록 튜닉, 여행 가방과 한 자루 검을 모든 모션에서 유지한다. 기존 승인 달리기·베기의 자세를 새 골격에 맞춰 다시 제작했으며, 아래의 v0.9.0 플레이어 프레임 수는 이전 기록이다.

- 원본 6종 52장: Idle 8, Run 8, Attack 14, DodgeUp 8, DodgeBack 8, Hit 6. 아래 쳐내기는 Attack 14장을 전용 타이밍으로 재사용한다. 활성 7클립·총 66프레임 참조다.
- 달리기 400ms와 공격 접촉 200ms, 위·뒤 회피 + 아래 쳐내기, 기존 이동·판정·성장·저장 규칙을 유지했다. 플레이어만 교체하며 나머지 38개 아트 항목을 보존했다.
- 체력바는 계단형 테두리와 굵은 칸으로 변경했다. 720p 높이 36px·격자 4px, 360p 격자 2px이며 기존 폭을 유지한다. 두 체력바의 실제 화면 경계를 정수 픽셀에 맞춰 혼색 행을 제거했다.

[현재 기획·실제 화면](../Planning/Story/Dragon-v1/index.html#unity-build), [이전·새 모션 비교](../Planning/Story/Dragon-v1/Production/player-v2-review.html), [적용 기록](../Planning/Production/dragon-v2-integration.json), [최종 QA](../Planning/QA/dragon-v2-result-20261024.md).

일반 실행판은 `Builds/Dragon-v2/LastBellMvp.exe`, 공유 묶음은 `Builds/Dragon-v2-Windows-0.9.1.zip`이다. ZIP 전체를 풀어 실행한다. QA 관측 코드가 제외된 일반 빌드와 ZIP 166파일의 무결성·라이선스를 확인했다. [빌드·해시 기록](../Planning/Production/dragon-v2-release.json).

관측 빌드의 1280×720·640×360 모션/판정 검사, 표적 PlayMode 11/11, 체력바 30상태 재검사를 통과했다. 아트와 실제 프레임 기록을 검토했으며 정상속도 영상 직접 재생이나 60fps 인증은 아니다. 일반 배포판의 별도 플레이, OS 실입력 및 모바일 실기기 검사는 포함하지 않는다.

---

# 이전 적용: 용 잡으러 갑니다 · 초원 도트 MVP v0.9.0 · 2026-10-24

현재 제작 범위는 1~10스테이지의 밝은 판타지 테마다. 기존 11~20스테이지와 진행도는 유지한다. 전체 50스테이지 기획을 구현한 버전은 아니다.

- 사용자가 확정한 **위·뒤 회피 + 아래 쳐내기**, 공격 Space/검 버튼을 유지한다. 세 화살표는 레퍼런스의 실제 게임 영역 기준 위치에 배치했다. 방향키 배경은 없다.
- 검사 온의 9모션 70프레임, 슬라임·뿔토끼·멧돼지·큰뿔토끼·큰엄니의 15모션 120프레임을 native 픽셀 아트로 연결했다. 10개 층의 적은 이 5체형을 공유한다. 달리기는 8프레임 400ms 주기다.
- 승인 초원 이미지의 원경과 지면을 별도 레이어로 이동하며, 도트 UI 아이콘 45개와 HP·패널·피해 숫자를 적용했다. 로비는 양피지 UI이며 전용 여관 아트와 별도 NPC 배우는 아직 없다.
- 프롤로그·10층 보스 후 토리 대사를 새 주제로 교체했다. 기존 전투·성장·저장 schema8, 5층 보석0/10층 보석4, 도전 종료 시 유물 선택·보유 유물 자동 적용 규칙은 보존한다.
- 회사/제품 식별자는 저장 경로 호환을 위해 유지한다. 데이터는 `mvp1.14.0`이다. `LastBell.Editor.DragonThemeMigration`이 저장 자산 갱신·검증·빌드를 담당한다. 구 전체 마이그레이션을 다시 실행하면 이전 테마로 바뀔 수 있다.

제작판 기획·실행 캡처: [Dragon v1](../Planning/Story/Dragon-v1/index.html#unity-build). [적용 내역](../Planning/Production/dragon-v1-integration.json), [Client 인계](../Planning/Production/dragon-v1-client-handoff.md), [QA 기록](../Planning/QA/dragon-v1-candidate-01-review.md).

일반 Windows 빌드는 `Builds/Dragon-v1/LastBellMvp.exe`다. 공유할 때는 `Builds/Dragon-v1-Windows-0.9.0.zip` 전체를 풀어 사용한다. 일반 빌드에는 Dragon QA 관측 코드가 없고, ZIP 165파일의 무결성과 라이선스 포함을 확인했다. [파일 해시·빌드 기록](../Planning/Production/dragon-v1-release.json).

[최종 QA](../Planning/QA/dragon-v1-result-20261024.md): 관측 빌드에서 시작·1~10 전투·11 진입·성장·유물 정산·별도 프로세스 저장 복원이 통과했다. 기존 테스트 8건의 구 아트/문구 기대를 갱신하고 해당 8건 재실행도 통과했다. 유물 카드 가림과 긴 이름 겹침은 수정 후 1280·640 화면에서 다시 확인했다. 일반 빌드 자체를 별도로 플레이한 결과는 아니다. OS 마우스·키보드 실입력은 사용자의 이전 보류 지시를 유지하며 모바일 실기기 검사는 포함하지 않는다.

---

# 이전 적용: 중복 없는 스테이지 적 · 밝은 칩튠 · 회피음 v8.6 · 2026-10-24

Windows: `Builds/Windows/LastBellMvp.exe` (0.8.6). 공유 시 `Builds/LastBellMvp-Windows.zip` 전체를 압축 해제한다.

- 1~20층에 적 20종을 한 번씩 배치한다. 4 여우, 5 멧돼지, 6 오소리, 7 스라소니, 8 산양, 9 큰뿔 사슴을 추가한다. 14~19층에는 기존 불씨/사슬 망자/갈고리 사냥개/순찰 인형/검사/사슬 무희를 복원하며 기존 공격·기절·크기를 보존하고 Idle을 추가한다.
- 일반·완벽 회피와 아래 방향 쳐내기 모두 입력 수락 시 바람 소리를 1회 재생한다. 거절된 입력·일시정지에는 재생하지 않으며, 성공 판정에서 소리를 중복 재생하지 않는다. 긴 종 효과음 연결과 전용 재생 코드를 제거한다.
- BGM 4개를 Juhani Junkala / SubspaceAudio의 `4 Chiptunes (Adventure)`로 교체한다. 로비 Stage Select, 성 밖 Stage 1, 주거구 Stage 2, 보스 Boss Fight. 공격음은 Kenney RPG Audio의 짧은 베기+충격 편집본이다. 신규 음악/공격/회피음은 CC0이며 원본 페이지, 배포본, 작성자 고지, SHA-256, 편집 내역과 사용 이력을 함께 보관한다.
- 로비·조작키 배치, 캐릭터 선택 Idle, 기존 몬스터 PNG/공격, 보스, HP·피해·강화·유물·체크포인트·저장 규칙은 보존한다.

미리보기: `Planning/Reference/TimingHero-20261023/feedback-v86.html`. 검증: `Planning/Evidence/Feedback-v86/verification.json`. 아트: `Planning/Story/Monster-v86/`. 음원: `Planning/Story/Audio-v86/`. 배포 라이선스: `Client/Assets/StreamingAssets/AudioLicenses/` 및 `ArtLicenses/PixelLab-v86.txt`.

---

# 이전 적용: 캐릭터 선택 Idle · 몬스터 Idle와 도트 조정 v8.5 · 2026-10-23

Windows: `Builds/Windows/LastBellMvp.exe` (0.8.5). 공유 시 `Builds/LastBellMvp-Windows.zip` 전체를 압축 해제한다.

- 캐릭터 선택의 정지 이미지를 기존 승인 Idle 클립 재생으로 변경한다. 현재 미리 보는 캐릭터의 아트를 사용하고, 일반 화면 갱신은 재생 시간을 초기화하지 않는다. 일시정지 중에는 멈춘다. 승인 아트가 없는 제작 중 캐릭터에는 다른 캐릭터 이미지를 대신 표시하지 않는다.
- 성 밖/주거구 6종은 변환한 영웅 이미지 대신 원본 `hero-v5-idle/01.png`를 직접 참고해 더 굵은 도트, 큰 색면, 짧고 단단한 비율로 제작한다. 각 종의 Idle·공격 1개·기절 루프를 같은 기본형으로 맞춘다. 피격은 기존 현재 포즈 위 흰색 플래시다.
- 10/20층 보스는 원래 디자인과 공격·기절을 보존하고 Idle만 추가한다. 원본 PNG의 불필요한 전체 이동은 프레임별 피벗으로 등록하며 원본을 늘이거나 다시 그리지 않는다.
- 공격 예고 초반에는 Idle을 재생하고, 실제 공격 준비 시점부터 기존 판정 시각에 맞춰 공격 모션으로 넘어간다. 전투 판정, 성장·유물·보상·저장 및 UI 배치는 변경하지 않는다.

모션 미리보기: `Planning/Reference/TimingHero-20261023/feedback-v85.html`. 검증: `Planning/Evidence/Feedback-v85/verification.json`. 소스/검수: `Planning/Story/Monster-v85/`. 라이선스: `Client/Assets/StreamingAssets/ArtLicenses/PixelLab-v85.txt`.

---

# 이전 적용: 도트 통일 · 명중 기절 · 자동 유물 v8.4 · 2026-10-23

Windows: `Builds/Windows/LastBellMvp.exe` (0.8.4). 공유할 때는 `Builds/LastBellMvp-Windows.zip` 전체를 압축 해제한다. 아래 이전 기록보다 이 절이 우선한다.

- 몬스터 6종을 주인공 기준 native64 도트·공통 팔레트·각진 외곽선으로 재제작했다. 모션용 투명 여백은 픽셀 리샘플링 없이 추가했다. 공격 1종, 별도 기절 루프, 기본 정지 포즈, 피격 흰색 플래시. 문지기·간수·주인공은 그대로 보존한다.
- 기절 누적 100은 **준비** 상태다. 피격해도 완성 상태는 보존되며, 다음 공격이 살아 있는 적에게 명중해야 기절한다. 충전만으로는 연속 공격을 취소하지 않는다. 발동 타격은 일반 피해, 이후 타격은 기존 기절 배율/속도/연장 규칙이다. 처치·동시 사망에는 추가 기절을 발동하지 않는다.
- 유물은 보유 종류 전체가 자동 적용된다. 캐릭터별 수동 장착은 폐기하며, 출전 시 소유/레벨을 전투 능력치로 고정한다. 기본 보관 3종, 최대 10종. 중복 획득은 Lv.5까지 강화되며 칸을 쓰지 않는다.
- 새 종류가 보관 한도를 넘을 때 확장 비용을 명시한 버튼으로 확장과 획득을 함께 처리한다. 확장 비용은 종 파편 4/8/12/18/26/36/48개다. 다른 후보를 고르거나 **대신 N 골드**를 명시적으로 선택할 수 있다. 자동 전환·자동 지출은 없다. 보관함 화면에서도 미리 확장할 수 있다.
- 저장 schema8: 이전 구매 횟수만큼 기본 3칸에 추가하며, 이미 가진 유물이 더 많으면 모두 들어갈 만큼 보존한다. 기존 `unlockedSlots`와 캐릭터 `equippedRelicIds`는 구 저장 호환 입력이며 새 규칙의 소유/효과 진실은 `relics`, 용량은 `storageCapacity`다. 진행 중인 구 전투는 능력치 스냅샷을 유지하고 다음 출전부터 자동 적용한다.
- 프롤로그/10·20층 대사는 종을 울리고 등불을 밝혀 주민의 시간을 되돌린다는 기존 줄기를 가다듬었다. 주민 생존 설정에 맞춰 `drowned_resident`의 표시 이름을 **침수된 잔상**으로 수정했다. 스테이지 1~20, 체크포인트 11, 전투 타이밍/HP/공격력/일반 보상은 유지한다.

설명·게임 캡처·모션 미리보기: `Planning/Reference/TimingHero-20261023/feedback-v84.html`. 검증 기록: `Planning/Evidence/Feedback-v84/verification.json`. 아트 작업/검수: `Planning/Story/Monster-v84/`, 라이선스: `Client/Assets/StreamingAssets/ArtLicenses/PixelLab-v84.txt`.

---

# 이전 적용: 골드 HUD · 전장별 몬스터 v8.3 · 2026-10-23

Windows 실행: `Builds/Windows/LastBellMvp.exe` (0.8.3). 배포 시 `Builds/LastBellMvp-Windows.zip` 전체를 풀어 실행한다. 아래 버전 기록보다 이 절이 우선한다.

- 모든 현재 게임 문구는 **골드**다. 플레이어 타격 콤보는 제거했으며 기존 저장의 `hitCombo`는 읽을 때 0으로 비운다. 적 연속 공격과 완벽 회피 연속 성공 규칙은 유지한다.
- 실제 처치 지급액을 중앙 상단 `+22` 형태로 1.1초 표시한다. 보상 이벤트만 사용하며 UI에서 재지급하지 않는다. 일시정지 시 표시 시간도 멈춘다.
- HP는 금속 프레임의 126×16(플레이어) / 144×16(적) 바다. 옥색·적갈색으로 구분하며 보스도 같은 하단 위치를 쓴다. 배치는 `FeedbackLayout`, 저장 구성은 `ReferenceV81Migration`이 소유한다.
- 1~9: 들개·회색 늑대·불곰. 10: 기존 성문 문지기. 11~19: 하수구 거대쥐·익사한 주민·물안개 망령. 20: 기존 사슬 간수장. 기존 아트는 삭제하지 않았다. 새 적의 공격 예고 풀을 종에 맞췄으며 HP·피해·보상·강화비·판정 시간은 유지했다.
- PixelLab 원본 162장과 작업·검수 기록은 `Planning/Story/Monster-v83`, 저장 임포트 계약은 Presentation `manifest.json`이다. 기본 포즈는 정지, 공격 1종·기절 1종, 피격은 기존 흰색 플래시. 새 사망 클립은 없다. 상업 이용 근거는 `StreamingAssets/ArtLicenses/PixelLab-v83.txt`에 기록했다.
- 종별 발톱·나무·물결 효과, 접근/복귀, 체형에 맞는 검격·피해 숫자·파편 위치를 적용했다. 회피 성공 시 피격 충격음과 흔들림이 같이 나오던 부분을 분리했다.

분석·미리보기: `Planning/Reference/TimingHero-20261023/feedback-v83.html`. 초반 HP 40/60 비교, 공격 입력 80ms 버퍼, 완벽 반격 1.4배, 짧은 정지 효과는 **미적용 실험안**이다. 최종 검증: `Planning/Evidence/Feedback-v83/verification.json`.

---

# 이전 UI: 레퍼런스 실측 v8.2 · 2026-10-23

선택/강화/전투/유물/결과 화면을 영상의 실제 게임 영역(1200×540 프레임에서 x44..1128)과 대조해 배치했다. 아래 v8.1의 **24px 일괄 적용은 폐기**했다. 네오둥근모 Pro를 유지하고 1084×540 기준 제목44(가로0.75), 본문24, 뒤로가기20, 보조18을 사용한다. 네오 글꼴의 실제 잉크 높이를 제목35px/본문20px에 맞춘 값이다. 960×540에서는 39/21/18/16으로 비례 조절한다. 위치는 `FeedbackLayout` 하나가 소유하며 `ReferenceV81Migration.Apply`가 기존 씬/프리팹에 저장한다.

두 패널 구조, 가운데 제목, 목록의 한 줄 별점, 수치→강화 후 수치, 현상금 가격/소지량의 위치와 밑줄 버튼을 반영했다. 실제 이름/값과 저장·밸런스는 유지한다. `현상금`은 기존 Gold의 화면 표기이며 `평가 n / 20`은 네 별점의 합계다. 전투의 상시 Stage/HP 숫자/플레이어 이름은 숨기고 적 Lv.·이름과 하단 HP를 표시한다. 게임 고유 종은 상단80px로 옮겨 참조 게이지(y124)와 겹치지 않게 했다.

실제 게임과 참조 영상 비교: `Planning/Evidence/Reference-v82/index.html`. 좌표/문구 명세: 같은 폴더 `layout-spec.json`, 각 촬영 화면의 `*-text.json`. Windows 실행 파일: `Builds/Windows/LastBellMvp.exe` (0.8.2).

---

# 현재 적용: 레퍼런스 v8.1 · 2026-10-23

Windows 실행: `C:/Github/Test2/Builds/Windows/LastBellMvp.exe`. exe 옆의 Data 폴더·UnityPlayer.dll·MonoBleedingEdge를 함께 보관한다. 아래 이전 버전의 규칙보다 이 절이 우선한다.

- SimpleGame에서 확인한 **NeoDunggeunmo Pro**를 전체 UI에 적용했다. 기존 프로젝트에는 정적 TMP SDF만 있어 같은 글꼴의 공식 v1.020 원본 TTF를 사용했다. 960×540 기준 글자 크기는 모두 **24**, 화면 전체 비율에 따라 함께 확대·축소된다. 자동 글자 축소를 사용하지 않는다. `StreamingAssets/FontLicenses`에 원문 OFL·출처·해시가 포함된다.
- 성문 배경을 어둡게 정리하고 황동 테두리의 패널을 사용한다. 캐릭터 선택 → 능력치 강화 → 전투 시작 화면을 분리했다. 담/린은 정보만 볼 수 있으며 출전할 수 없다. 1Lv./MAX는 견적이고 강화 버튼을 눌러 구매한다. MAX도 돌파 구간의 실제 순차 비용으로 계산한다.
- 회피는 **↑/W, ←/A, ↓/S** 3개다. ↓는 공격 버튼이 아니라 방어용 검 쳐내기다. →/D와 그 방향을 요구하는 패턴은 제거했다. 방향키 중심은 영상의 실제 게임 영역(x=44..1128, y=0..540)에서 측정한 좌표를 Safe Area 비율로 적용한다. 공격은 Space 또는 오른쪽 검 버튼이다.
- 예고는 0/300/600ms에 각100ms 강조 후 **900ms**에 접촉한다. 모든 방향을 먼저 예고하고 연속 타격은 **750ms** 간격이다. 파동은 매 타격750ms 전에 시작한다. 회피 유효240ms·회복120ms·완벽90ms·반격 기회1200ms. 적의 쉬는 시간은 마지막 타격→다음 예고 기준 일반4800/중간보스3600/보스2700ms다.
- HP는 양쪽 캐릭터 아래의 고정 위치에 둔다. 보스도 같은 하단 HP를 사용하며 `Lv.스테이지 이름`을 표시한다. 종은 사용한 칸을 흐리게 표시해 공격 횟수를 유지한다. 기존 몬스터 모션·피격 흰색 반짝임·선정 효과음은 보존했다.
- 파편은 보스10/20 처치 때 각4개, 중간보스는0개다. 보스 처치 시 유물 후보를 저장하고 다음 전장으로 이동한다. 사망/도전 종료/20클리어 → 도전 요약 → 스테이지 클리어 안내 → 유물 선택/확정 → 최종 결과(다시하기/로비) 순서다. 보상 지급은 한 번만 일어나며 효과는 다음 도전부터 적용된다. 보스 전회복은 처치 시점에 적용하고 동시 사망을 되살리지 않는다.
- 프롤로그는 18초 자동 진행과 건너뛰기만 있다. 완료 후 캐릭터 선택으로 들어간다. 처음 스테이지5의 연속 공격 안내 패널과10클리어 후11재시작은 유지한다. 첫20스테이지 HP/공격력은 v7 값을 유지했다.

저장 schema7은 기존 재화·성장·체크포인트·이미 약속된 유물 후보를 유지한다. `ReferenceV81Migration.Apply`는 해당 프리팹과 씬을 부분 갱신한다. 검증 증거: `Planning/Evidence/Reference-v81/index.html`. 자동 검증은 `-lastbell-v81-smoke <출력폴더>`와 별도 `-lastbell-save-dir`을 함께 사용한다. 웹 공개 배포는 이번 작업 범위에 포함하지 않는다.

---

# 추가 적용: 전투 음향·라이선스 v7.2 · 2026-10-23

공격은 사용자가 선정한 SonoFxAudio의 Sword_Fire01.wav (CC BY 4.0), 일반 회피는 qubodup의 Swosh / Whoosh / Air Cut (CC0), 피격은 Philipp Grzemba / Sheyvan의 Impact: Smack Body (CC0)를 편집해 사용한다. 공개 HQ MP3 미리듣기를 PCM WAV로 변환했으며 원본 무손실 파일을 받았다고 표기하지 않는다. 공격 명중·일반 회피 성공·실제 피격 시점에 각각 재생한다. 적 무기/충돌음은 별도 EnemySwing/EnemyImpact 슬롯으로 기존 소리를 유지해 회피 성공 시 몸 피격음이 재생되지 않는다. 완벽 회피는 기존 종소리다.

출처 화면은 로비 상단 ‘음원 출처’에서 열며 원본·CC BY 링크와 편집 사실을 표시한다. 원본 페이지 증빙·미리듣기 파일·재현 스크립트는 `Planning/Story/Audio-v72`, 게임 적용 파일과 SHA-256 manifest는 `Assets/Game/Audio/Combat-v72`, 배포 라이선스 묶음은 `Assets/StreamingAssets/AudioLicenses`가 원본이다. 빌드는 이 폴더를 Windows 데이터 폴더와 WebGL StreamingAssets에 포함한다. 웹 진입 페이지에도 출처 링크가 있다.

`AudioV72Migration.Apply`가 저장 프리팹/씬의 오디오 참조와 크레딧 UI를 부분 적용한다. `MvpBuild.Prepare`는 음원 SHA-256과 크레딧·라이선스 원문 존재를 검사한다. 오디오를 바꾸면 manifest·편집 내역·출처 UI·라이선스 묶음을 함께 갱신한다. 과거 `Reviewed-v4` 메타데이터는 이전 편집본 기록이며 현재 전체 라이선스는 v7.2 묶음을 기준으로 한다.

---

# 추가 적용: 성문 연결과 11스테이지 체크포인트 · 2026-10-23

10스테이지 가디언 처치 시 해당 캐릭터의 시작 지점을 11스테이지로 저장한다. 이후 사망 결과의 다시하기와 로비 출전 모두 11부터 시작한다. 10 클리어 전에는 1부터 시작하며, 20스테이지 완주 뒤에도 시작 지점은 11이다. 기존 저장의 캐릭터별 `highestStage >= 10` 또는 기존 체크포인트로 해금을 복원한다. 보상 선택은 건너뛰지 않고 재화·강화·유물은 유지한다.

성문은 10과 11에서 같은 원본 스프라이트·크기·문틀을 사용한다. 별도 열린 그림으로 교체하지 않고 철창만 위로 올린다. 10→11 구간은 1020px/1062ms로 이동 속도를 유지하며 화면 우측에서 좌측으로 연속 이동한다. 11→12에서도 성문이 화면 밖으로 나갈 때까지 유지한다. 부분 에셋 적용은 `LastBell.Editor.GateCheckpointMigration.Apply`, 검증은 `GateCheckpointMigration.Validate`. 현재 공격 효과음은 사용자가 새 음원을 고른 뒤 교체한다.

---

# 현재 적용: Feedback v7 · 2026-10-23

20스테이지 구성에서 강화·별점·적 HP/공격·보상을 재조정했다. 중앙 유물 3후보는 미리 선택 후 공용 ‘선택’ 버튼으로 확정한다. 모바일 조작 버튼을 Safe Area 안쪽으로 옮기고 프롤로그에 기존 배경·양피지 두루마리·좌상단 등불을 연결했다. 4문단 18초이며 읽기/건너뛰기를 지원한다.

원본 데이터는 `GameData/mvp-catalog.json`, 기획은 `Planning/Presentation/feedback-plan-v7.html`이다. 저장 프리팹 적용은 `LastBell.Editor.FeedbackV7Migration.Apply`, 빌드 전 검증은 `FeedbackV7Migration.Validate`. 검증용 실행 옵션 `-lastbell-v7-smoke <출력폴더>`는 별도 `-lastbell-save-dir`과 함께 사용한다. 일반 실행에 자동 조작을 설치하지 않는다. 과거 버전 절의 수치보다 이 버전이 우선한다.

---

# Last Bell 클라이언트

2026-10-22 mvp1.8.2: 적의 타격당 공격력을 매 스테이지 증가하도록 조정.1층8·5층13·10층20·15층27·20층35이며,보스 다음 층도 내려가지 않는다.

최신 공개 배포: [20스테이지 플레이테스트](https://last-bell-playtest.nugunagame1.chatgpt.site). 최신 검증 기록: [공격력 증가](../Planning/Evidence/Progressive-damage-v182/verification.json). 아래 이전 배포 기록보다 이 빌드를 우선한다.

2026-10-22 추가 수정(mvp1.8.1):5층 성문 수문장 중간 보스·9층 검문쇠 거인 일반 적으로 교환.1~10층 일반 적 단타·5층1~2연타·10층2연타,3연타는15층부터.10층 처치 직전 프레임을 대사와 이후 소멸까지 유지한다. 완벽 방어 종소리 음량은0.55에서0.38로 낮췄다.

2026-10-22 v6 로컬 통합: 15종을 20스테이지에 배치했다. 공격 횟수와 방향은 예고 전에 확정하며 저장 후에도 미해결 순서를 유지한다. 기절 중 공격 속도×1.2, 명중당 기절+50ms(최대300ms), 보스 기절 저항과 연속 완벽 방어 보너스, 상단 적 이름·보스 HP·무피격 명중 콤보를 연결했다.

골드는 1골드당1개(최대32768개 가중 도트)를 몸통에서 우측 숫자로 직선 이동한다.840ms 방출+360ms 비행으로 최대1.2초, 짧은 획득음은 최소75ms 간격으로 묶는다. 지갑 지급과 시각 표시를 분리하며 저장 schema6은 기존 재화·성장을 보존한다.10층은 보스가 남아 대사→소멸→보상·전회복→11층,20층 보상 후 클리어다. 낮은 종은 완벽 회피·완벽 쳐내기만 사용한다.

적용 메뉴: `Last Bell/Feedback v6/Apply` (기존 씬/프리팹의 해당 참조만 부분 변경). 검증: `Last Bell/Feedback v6/Validate`. 새 실행 검증은 별도 저장 폴더와 `-lastbell-feedback-v6-smoke <capture-directory>`를 사용한다. 최신 증거는 [Feedback-v6-runtime](../Planning/Evidence/Feedback-v6-runtime/)에 기록한다. 로컬 Windows/WebGL 대상이며 공개 웹 사이트와 Android는 별도 배포 이력이다.

아래는 이전 통합 기록이다. 현재 범위·종소리·스테이지 종료 규칙은 위 v6 설명을 따른다.


Unity **6000.3.15f1**, Built-in 2D + uGUI. 프로젝트 폴더는 `C:/Github/Test2/Client`다.

2026-10-22 모션·음향 통합: 검수한 새 몬스터10종을 저장 카탈로그에 추가하고 1~10층에 빗장 초병·종추 사냥개·금 간 검기사·검문쇠 거인·새 종의 기사를 배치했다. 나머지5종은 확장용으로 등록하고 실제 실행 검증에서 확인했다. 공격은 원본 프레임 유지 시간을 보존해 전투 접촉 시점에 맞춘다. 검 충돌음은 명중200ms, 낮고 긴 종은 보스 처치 때 재생한다. 전투·로비·보스 BGM과 회피·발소리·등불 음향도 교체했다. [적용·검증 기록](../Planning/Evidence/ReviewedMotionAudio-v4/verification.json). Windows와 로컬 WebGL이 이번 통합 대상이며, 아래 공개 플레이테스트 링크와 Android APK는 이전 버전이다.

소스 재연결: `node Planning/Story/MonsterMotionFresh-v3/install-runtime.cjs` → `python GameData/export_catalog.py` → Unity 메뉴 `Last Bell/Reviewed Motion and Audio/Apply`. 이미지156개와 음원12개의 검수 해시를 확인한 후 저장 씬·프리팹에 연결한다.

최신 적용: 승인 달리기8자세·40/60ms 유지·400ms 주기·1,920px/2초. 기존 v5.2의 회피 방향 기호·문구, 정수 전투 스탯, 사망 결과·다시하기·로비를 적용했다. [검증 기록](../Planning/Evidence/Feedback-v52/verification.json). Android APK는 이번 수정으로 재빌드하지 않았다.


최신 전투 v5.9.1: 기본/빠른 패턴 모두0·240·480ms 점멸 뒤720ms에 중앙 파동 접촉·타격. 연속 공격은 전체 방향 예고 후480ms 간격으로 진행한다. 네 방향 회피 중 아래 방향은 제자리 검 쳐내기 모션으로 상단을 방어한다. [실제 게임 검증](../Planning/Evidence/Combat-v591/index.html), [수정 보드](../Planning/Presentation/feedback-plan-v591.html). Android APK는 이전 버전이다.

## 실행

- 웹 플레이테스트: [https://last-bell-playtest.nugunagame1.chatgpt.site](https://last-bell-playtest.nugunagame1.chatgpt.site). 1~10스테이지, 모든 새 도전은1스테이지부터.

- Windows: `C:/Github/Test2/Builds/Windows/LastBellMvp.exe`를 실행한다. exe 옆의 `LastBellMvp_Data`, `UnityPlayer.dll`, `MonoBleedingEdge`도 함께 있어야 한다.
- 다른 PC로 옮길 때는 `C:/Github/Test2/Builds/LastBellMvp-Windows.zip` 전체를 풀고 실행한다.
- Android 테스트 APK: `C:/Github/Test2/Builds/Android/LastBellMvp.apk`. Android8.0 이상 ARM64용 개발 빌드다. 원스토어에 게시한 빌드는 아니며 실제 Android 기기의 터치·성능 검증은 남아 있다.
- Unity Hub에서 `Client`를 Add한 뒤 `Assets/Game/Scenes/Mvp.unity`를 열고 Play한다.
- 새 저장에서는 Stage 1 회피·공격 튜토리얼이 바로 시작한다. 진행 중 저장을 다시 열면 일시정지 메뉴의 **계속**으로 재개한다.
- 마우스/터치: 왼쪽 네 삼각형으로 회피(아래는 쳐내기), 오른쪽 검으로 공격. 키보드: 방향키 또는 WASD 회피(↓/S는 쳐내기), Space 공격·반격, Esc 일시정지.
- 낮은 쓸기에는 ▲, 높은 쓸기에는 ▼/S(검 쳐내기), 앞에서 오는 찌르기에는 ◀, 뒤를 감싸는 원호에는 ▶로 대응한다. 일반 회피와 완벽 회피가 적의 종 게이지를 다르게 채운다.

2026-10-21 최신 기획의 저장 모션·전환·Stage·보스 등장·서사·음향을 씬과 프리팹에 연결했다. 공격은 접근→베기→귀환이며 명중200ms, 재사용500ms다. 회피160ms·완벽 회피60ms·재사용260ms와 예고100% 표시 프레임의 회피 입력 처리는 유지한다. 막타는 월드만120ms 흔들리고 피해 숫자는 우측 위로650ms 이동한다.

최신 통합본의 전용 QA는 [완료 기준](../Planning/QA/acceptance-20261021.md)과 [검사 계획](../Planning/QA/qa-plan-20261021.md)에 따른다. 이전 [전투 피드백 검사](../Planning/Reviews/telegraph-end-timing.md)는 이전 빌드의 증거이며 최신 통합본 통과를 뜻하지 않는다. Windows 후보와 검사 결과는 [최신 QA 보고서](../Planning/QA/result-20261021.md)를 확인한다. 기존 Android APK는 이번 통합으로 다시 빌드하지 않았다.

## 연결한 범위

온1명, 첫10층, 일반3종·중간 보스1종·보스1종, 유물10종. 5층은 골드와 종 파편1개만 지급한다. 10층은 종 파편3개와 첫 처치 대사3개 뒤 서로 다른 유물3개 중 하나를 선택한다. 새 유물은 Lv.1, 재획득은 레벨 상승, 최대 레벨은 골드로 바뀐다. 신규 유물 장착은 로비에서 한다.

프로토타입은 **10층 보상 선택 후 클리어 결과 화면에서 종료**한다. 획득 골드·종 파편과 다시하기/로비 버튼을 표시한다. 사망 시에도1800ms 연출 뒤 결과 화면에 머문다. **클리어·사망 후 다시하기와 로비 출전은 모두1층부터**이며 재화·강화·유물은 유지한다. 예전11층 저장은 영구 진행을 보존하고 로비로 돌린다. 1~10층 진행 중 브라우저를 다시 열면 일시정지 상태로 이어갈 수 있다.

강화는 네 스탯의 +로 수행한다. 종10개가 레벨 묶음을 표시하고 11/21/31/41에서 돌파한다. 골드가 모자라면 필요한 파편 교환 견적을 표시한다. 교환을 확인한 뒤 **+를 다시 눌러야 강화**한다. 중간 보스는 종 파편1개, 보스는3개를 지급한다.

## 저장과 데이터

- 웹 빌드: Unity 메뉴 `Last Bell/Build/Web Prototype`, 출력 `Builds/WebGL`. 브라우저별 PlayerPrefs 저장에 동일한 체크섬·백업 봉투를 사용한다. 공유 계정이나 서버는 사용하지 않는다. 브라우저 데이터를 지우면 해당 브라우저의 진행 기록도 지워진다.
- 웹 배포 전용 체크아웃은 `Web/LastBellPrototype`이며 Unity 프로젝트 전체를 게시하지 않는다. 공개 사이트에는 WebGL 실행 산출물만 포함한다.
- 일반 실행 저장: `%USERPROFILE%/AppData/LocalLow/LastBellPrototype/Last Bell MVP/last-bell-save.json`. 이전 저장은 `.bak`에 둔다. 체크섬·임시 파일 교체와 백업 복구를 사용한다. 손상된 저장을 자동 초기화하지 않는다.
- 테스트 저장은 `-lastbell-save-dir <절대경로>`로 분리한다. 자동 검증은 실제 사용자 저장을 사용하지 않는다.
- 편집 원본: `C:/Github/Test2/GameData/mvp-catalog.json`. 프로젝트 루트에서 `python GameData/export_catalog.py`로 검증 후 생성한다. `--check`는 수정 없이 일치 여부만 확인한다.
- 생성본: `Assets/Game/Data/Generated/mvp-catalog.json`. 이 파일을 직접 수정하지 않는다.
- 로컬 MVP 지갑이며 실제 유료 구매·광고·서버 검증은 포함하지 않는다.

## 제작 구조

|경로|책임|
|---|---|
|Runtime/Core|순수 C# 전투·성장·보상·진행 상태, 저장 DTO|
|Runtime/Data|설정 DTO·ID 조회·런타임 검증|
|Runtime/Presentation|포인터/키보드 명령 전달, 상태 표시, 도트 숫자·파편·흔들림|
|Runtime/Bootstrap|카탈로그 로드·저장·오류 재시도|
|Runtime/Art|스프라이트 카탈로그|
|Editor|명시적인 임포트·Create Missing·Validate·빌드 명령|
|Prefabs, Scenes|저장된 GameView/Actor 프리팹과 Mvp 씬|
|Art/Source|생성 원본 사본. Unity metadata로만 스프라이트 분리|

UI 계층은 에디터에서 저장하며 Play 때 다시 만들지 않는다. `Last Bell/Build/Prepare MVP`는 없는 에셋만 만들고 검증한다. 기존 프리팹을 자동 덮어쓰지 않는다. 생성 이후 배치 변경은 저장된 에셋에서 편집한다.

명시적 `Last Bell/Presentation/Apply Approved MVP` 메뉴는 선택 아트·신규 VFX/배경/음향을 저장 씬에 연결한다. Play 중이나 저장하지 않은 Mvp 씬이 있으면 실행하지 않는다. `Validate Saved MVP`는 카탈로그·UI·실제 씬 참조를 검사한다. 사용 중인 MCP는 [LastBell 전용 8093 서버](../Tools/MCP/README.md)다.

## 빌드와 검증

- `Last Bell/Build/Windows MVP`: 개발용 Windows 실행 파일.
- `Last Bell/Build/Android Test APK`: ARM64, Android8.0 이상 개발용 APK. 기기 설치·성능 검증과 원스토어 제출 준비는 별도다.
- Test Runner의 EditMode: 전투 경계/연타/진행/보상/교환/저장 테스트.
- 개발 빌드에만 `-lastbell-smoke <출력폴더>`를 주면 실제 UI 명령으로 전체 흐름을 확인하고 Unity 카메라/Canvas를 PNG로 캡처한다. 별도 새 저장 폴더도 반드시 지정한다. 일반 실행에서는 이 자동 조작이 생성되지 않는다.
- 최신 적용 범위: [통합 기록](../Planning/Production/unity-presentation-integration-20261021.md). 실제 검사 결과·미해결 항목: [QA 보고서](../Planning/QA/result-20261021.md).

## 아트 범위

선택 원본281 PNG를 보존한6종/37모션 카탈로그를 사용한다. 온의 달리기는 사용자가 승인한 `run-dash-20261021-02`8자세/400ms이며, 상하 베기·4방향 회피·Idle·피격·귀환과 적 공격·피격을 연결했다. 일반3종은 모두 높이2, 중간보스3.2, 보스4로 적용한다. [아트 매니페스트와 제한](Assets/Game/Art/Presentation/README.md)에 실제 선택과 재사용을 기록했다.

기존 몬스터 5종의 외형·공격과 단일 Idle·Stun 자세를 유지한다. 피격은 현재 자세를 유지한 채 45ms 흰색 + 75ms 색 복귀만 적용하고, 적 사망 클립은 사용하지 않는다. 신규 10종은 기획 미리보기 후보로만 보관한다. 포효는 기존 공격1회+얼굴 원형 파동이며 별도 신규 포효 스프라이트는 아니다. 배경은5개 깊이 레이어의 저장 타일로 이동한다. 음향은 Kenney CC0 효과4개와 직접 합성8개를 사용하며 [출처·해시](Assets/Game/Audio/Presentation/audio-provenance.json)와 라이선스 사본을 포함한다.

전체 구역·50종 명단과 실제 초안은 `C:/Github/Test2/Planning/Production/index.html`에서 확인한다. 12~50층·NG+·나머지 캐릭터·전체 구역 배경·출시용 모션 정리는 후속 범위다.

승인 달리기: [실제 게임 재생](../Planning/Evidence/ApprovedRun-20261021/run-preview.html) · [104 EditMode/14 PlayMode 및 실제 실행 검증](../Planning/Evidence/ApprovedRun-20261021/verification.json). 전투 v5.5도 게임에 적용했다. [실제 게임 재생](../Planning/Evidence/Combat-v55/index.html)과 [검증 기록](../Planning/Evidence/Combat-v55/verification.json)을 확인한다.


## 전투 v5.5

금빛 위험 사각형을3회 점멸하며 모든 연타 예고 뒤 순서대로 타격한다. 화면 중앙 원형 파동과500ms 완벽 반격 기회, 적 ParticleSystem·무기 리본·잔상·충격 파문을 적용했다. `Last Bell/Combat/Apply v5.5`는 해당 UI와 저장된 CombatV55 프리팹/씬 참조만 명시적으로 마이그레이션한다. 기본 빌드 준비에도 CombatV55 에셋 검증이 포함된다.

개발 빌드의 `-lastbell-combat-v55-smoke <출력폴더>`는 별도 저장 폴더와 함께 사용한다. 실제 카메라 캡처, 예고·타격 시각, 반격, 화면 중앙/반경, 입자 상한, 정지/연출 줄이기를 검증한다. 일반 실행은 자동 조작을 설치하지 않는다. 원본 시간표는 `GameData/mvp-catalog.json`, 공유 전투 공간은 `Runtime/Core/CombatGeometry.cs`다.

## Route v4 적용 · 2026-10-22

첫 저장은 28초 자동 스크롤 프롤로그로 시작한다. 일시정지·문단 읽기·건너뛰기와 로비 다시보기를 제공한다. 최초 5스테이지는 중앙 패널에서 확인을 누르면 온전한 첫 연속 공격 예고로 이어진다. 저장 schema5의 prologueSeen/comboHintSeen으로 반복 여부를 관리하며 구버전 재화·강화·보상은 보존한다.

1~9의 반복 배경에서 성문을 제거하고, 9→10 이동 시 지면에 고정된 단일 성문이 들어오도록 했다. 10스테이지 가디언 처치 뒤 성문이 열리며 보상 후 프로토타입이 끝난다. 재시작은 항상 1스테이지다. 인게임 아트 카탈로그는 온과 기존 몬스터 5종으로 복구했으며 1~10 원래 적 배치를 유지한다. 새 디자인은 별도 후보 갤러리에만 표시한다.

명시적 저장 에셋 갱신: `LastBell.Editor.RouteV4Migration.Apply`. 검증용 Windows 빌드: `LastBell.Editor.RouteV4Migration.ApplyAndBuild`. 모션/생성 기록은 `Planning/Story/MonsterMotion-v1`, 미리보기는 `Planning/Story/route-prologue-tutorial-v4.html`, 실제 실행 증거는 `Planning/Evidence/Route-v4-runtime`을 참고한다. 공개 웹 배포는 이번 작업에 포함하지 않는다.
