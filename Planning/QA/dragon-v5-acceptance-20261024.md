# Dragon-v5 · v0.9.4 완료 기준과 검증 설계

2026-10-25 KST · **CRITERIA_REFERENCE / CANDIDATE02_ADOPTED / CANDIDATE03_TARGET_PASS / FULL_ACCEPTANCE_NOT_CERTIFIED**

이 문서는 승인된 Adventure-v5 적용의 합격 기준과 최초 QA 설계다. 아래 관측 열의 NOT_RUN/예정 표기는 설계 당시 상태이며 실제 진행·결함·미검증 범위는 [최신 결과](dragon-v5-result-20261024.md)를 따른다. 인계 뒤 candidate02 native4프로세스의 대표 관측과 별도 수동 화면 검토를 완료했다. Core 테스트 재실행, Unity/Editor 조작, Apply, 빌드, 제품 Core/일반 Presentation/카탈로그 수정, 커밋·푸시는 QA가 수행하지 않는다. 총괄의 Unity/MCP 인스턴스도 재연결하거나 교체하지 않는다.

기준은 [plan.json](../Story/Dragon-v1/Adventure-v5/plan.json), [companions-plan.json](../Story/Dragon-v1/Adventure-v5/companions-plan.json)과 최신 총괄 인계다. 계획 JSON의 planning-only 문구는 작성 당시 상태이며 이번 전체 적용 승인을 취소하는 뜻으로 해석하지 않는다. 읽은 시점의 SHA256은 각각 `3342A04CE33A428BE6C5EE29C8835598BB5B688032707F8121903FF09634C7FB`, `D8628E4ACAF62C153B8CE8D811D39D171D232C975C4358F3576BC5E81FA77262`다. 실행 전 최종 명세·카탈로그·아트 manifest 해시를 다시 인계받아 검사 기대값을 고정한다.

## 범위와 판정 원칙

| 범위 | 합격으로 인정할 증거 |
| --- | --- |
| 현재 실제 게임 | v0.9.4의1–20 진행, 세 플레이어 on/dam/rin, 새 계정 토리2·바룬12 첫 만남, 여관·회복·튜토리얼·후퇴·지원 공격·저장 연결 |
| 기존 v4 저장 | 완료 대사에 따라 최대 토리/리나/바룬/도토4명을 소급 해금하고 서비스 제공. 진행 중 레거시 대사·HP·스킬 쿨·보상 보존 |
| NPC5종 전체 규칙 | tori/barun/doto/rina/momo의 데이터·공격·서비스·성장·슬롯 규칙을 Core 계약과 명시된 격리 fixture로 확인. momo·3슬롯·고단계 무기는 미래 조건 fixture임을 표시 |
| 미래 실제 진행 |21–50 지역·보스·새 만남·50 이후 회차를 이번 실제 플레이 완료로 표시하지 않음. 현재1–20 경계를 넘어 이동시키는 QA 진행을 만들지 않음 |
| 시각/위치 |1280×720과640×360의 실제 PNG·연속 프레임·root/발/화면 bounds. 이벤트나 checks PASS만으로 이동·가독성을 합격시키지 않음 |
| 입력/오디오 | 합성 pointer/raycast→제품 UI 명령과 명령 거절 관측. 사용자 지시대로 OS 실입력·모바일 DEFERRED. 직접 청음 NOT_RUN, 정확한 voice 중첩 NOT_CERTIFIED는 별도 확인 전 유지 |

Core 규칙·저장 마이그레이션 개발은 기존 Client, UI/Editor/연출은 Root, 아트는 별도 담당이다. QA는 각 담당의 동일 동결 소스 테스트·검증 결과를 재사용하고, 새 동작의 실제 연결·화면·재시작을 검증한다. [v4 결과](dragon-v4-result-20261024.md)는 재사용 가능한 기반과 회귀 기준이며 v5의 합격 증거가 아니다.

## D · 대화와 초상화

| ID | PASS 기준 | 관측 |
| --- | --- | --- |
| D01 | **플레이어 발화 초상은 왼쪽 위, NPC 발화 초상은 오른쪽 위.** 기존 JSON의 모든 화자 오른쪽 고정 배치는 최신 요청으로 대체한다. 현재 화자의 이름·초상·정렬이 같은 렌더에서 함께 갱신 | 세 플레이어 각각의 발화와 NPC 왕복 전환 PNG, 활성 초상ID·부모/anchor·화면bounds |
| D02 | 아래 인계된1280×720 좌표·이름36/본문32와 일치. 화자 전환 시 이름표도 지정 위치로 이동하며 본문·버튼·초상을 가리지 않음 | **좌표 계약 수신 / 저장 asset·렌더 NOT_RUN**. 실제 bounds/anchor·본문 여백·Safe Area는 최종 저장 프리팹/씬과 대조 |
| D03 | 초상 뒤 판/장식 프레임 없음, 투명 cutout·Point 표시. 새 초상 재제작 범위는 **미엘만**이며 NPC리나 `rina`와 플레이어미엘 `rin`을 혼동하지 않음 | 미엘 asset/GUID/ID/알파/필터/실제 이미지 확인. 다른 초상은 인계된 기존 자산 재사용; 계획의24px 항목을 전체 재제작 지시로 확대하지 않음. 최종 원본 해상도·표시 배율은 아트 인계로 확정 |
| D04 | 긴 한국어 본문은 단어 단위 줄바꿈, 최대3줄 뒤 페이지 분할. 본문·이름·가격 생략 없음. 긴 이름표가 확장되어도 본문/초상/버튼을 가리지 않음 |1280/640 실제 모든 새·수정 페이지, 세 플레이어 해석 문구. 작은 화면 본문16/행22 등은 최종 UI 인계값과 대조; 글자 축소로 overflow를 숨기지 않음 |
| D05 |640에서 활성 조작의 최소 짧은 변48px, hit area 겹침 없음, 다음/스킵이 Safe Area 안. 초상 자체가 버튼을 가로채지 않음 | 실제 ScreenBounds 및 EventSystem raycast. 기기 터치 사용성 검증과 구분 |
| D06 | 타이핑 중 새 press는 현재 페이지 공개, 다음 새 press가 진행. held/prebuffer·중복 callback으로 두 페이지 넘김/중복 해금 없음. 스킵은 한 번 종료 | 입력 edge/lineIndex/sceneId/해금 원장 전후. 파일 Bind 뒤 실제 렌더를 기다리고 검사 |
| D07 |???→소개→이름, 현재 화자에 맞는 초상. 토리 화면 밖 호출에만 허용된 silhouette; 무명 NPC에 여우 silhouette 일괄 사용 금지 | 원본 scene 계약의 역할/ID→Core가 해석한 Story→렌더의3단계 대조. Core/UI가 같은 잘못된 값이라도 서로 일치만으로 합격하지 않음 |

인계된 대화 배치는 Safe Area 왼쪽 위 원점의 계획 좌표다. `x,y,w,h`와 위치만 받은 `x,y`를 구분한다.

| 요소 |1280×720 확정 계약 |640×360, inset 없는 비례 축소 참고값 |
| --- | --- | --- |
| NPC 초상 |(1056,384,144,144) |(528,192,72,72) |
| 플레이어 초상 |(48,384,144,144) |(24,192,72,72) |
| 대화 패널 |(24,488,1232,208) |(12,244,616,104) |
| NPC 발화 이름표 위치 |(48,440) |(24,220) |
| 플레이어 발화 이름표 위치 |(216,440) |(108,220) |
| 이름 / 본문 글꼴 크기 |36 /32 |18 /16 |

640 값은 최종 Canvas 배율과 Safe Area 계약에 따라 대조할 참고값이며 모든 기기의 물리 px 보장은 아니다. 이름표의 폭·높이·anchor, 본문 여백, 페이지 매핑과 inset별 재배치는 아직 최종 인계가 필요하다. 최신 이름표 위치를 기존 JSON의 동일 위치·크기 가정으로 덮어쓰지 않는다.

새 장면 페이지 수·플레이어 응답·보스 재등장 문구는 최종 scene manifest를 따른다. 기본 첫 만남4페이지와 재회1페이지 계약은 확인하되, 최종3줄 분할로 늘어난 렌더 페이지를 무조건4로 실패시키지 않는다. 의미상 대사 line과 표시 page의 매핑을 인계받는다. 현재 캐릭터별 새/수정 페이지를 모두 검토하고, 레거시7/15/20 장면도 저장된 sceneId/lineIndex부터 끝까지 읽을 수 있어야 한다.

## R · 다섯 번째 스탯과 처치 회복

| ID | PASS 기준 | 경계/대표 예 |
| --- | --- | --- |
| R01 | 기존4스탯 순서를 보존하고 recovery를 index4에 추가. 캐릭터별Lv0–50, 별점 on3/dam4/rin2, 비용은 목표Lv의 기존 upgradeCosts×0.5 올림 | Lv0/1/10/11/20/21/40/41/50,50초과·자금부족·중복 구매 거절. UI의현재/다음값·10칸·별점·가격과 실제 결과 일치 |
| R02 |`q=starScale[stars-1]`, `b=max(0,floor((Lv-1)/10))`, `R=round((5+2*q*Lv)*(1.7+0.05*(stars-1))^b)`; 양수.5 올림 | 기본Lv0 모두5. Lv10 on25/dam30/rin20, Lv11 on49/dam60/rin38. 비용Lv1=5/Lv10=38/Lv11=113. 수식/상세 경계는 Client Core 결과 재사용 |
| R03 | 생존 상태의 처치에서만 `HP=min(maxHP,HP+출전고정R+차추가량)`을 runId:cycle:stage 보상 경계에 정확히1회 적용 | 일반/중간보스/10·20보스, 플레이어/동료 막타, HP상한, 동시에 사망한 경우 우선순위. 처치 전후 hpMilli·원장·해금/이동 이벤트 기록 |
| R04 | 보스 완전 회복 제거. 여관에서 새 원정을 시작할 때만 출전 maxHP로 시작 |9→10→11 및19→20 완료 경계. 예: max340/HP120/R25의10처치는145이며340이 아님. 현재20 이후21 실제 플레이를 요구하지 않음 |
| R05 | 회복은 피격·시간경과·회피·도착·체크포인트 저장·대사 다음/스킵·유물선택·앱복귀·사망에서 추가 발생하지 않음 | 처치 확정 직전/직후 저장→파일복원/재시작, 대사/유물 경계 반복 입력. migration에서 기존 완료 보스 heal을 되돌리거나 재지급하지 않음 |
| R06 | 출전 시 회복·차 결과 고정, 원정 중 UI/영구레벨 변경으로 소급 증감 없음. HP/회복/무기는 캐릭터별 | 다른 캐릭터 전환과 출전 스냅샷, 미래 cycle 처리 규칙은 Core fixture로만 구분 |

## E · 후퇴·준비·전진·복귀의 실제 시각

첫 공격의 기준은 권위 있는 combat clock의 해결되지 않은 `ImpactAtMs=I`다. **후퇴 I−500..I−250(250ms) →준비 I−250..I−160(90ms) →전진 I−160..I(160ms) →접촉 I →복귀 I..I+280ms**. 후속타는 **I−370..I−250(120ms),12계획px 후퇴** 뒤 같은 준비/전진/복귀를 따른다. Cue가 다음 방향을 먼저 알려도 pose/루트는 현재 해결할 타격에 묶인다.

| ID | PASS 기준 | 필수 증거 |
| --- | --- | --- |
| E01 | 현재1–20의 모든 실제 적/body archetype에 읽히는 후퇴. 첫 후퇴 small48/large32/giantBoss24계획px, 안전 오른쪽 끝−16px 여유로 cap | final archetype→stage 매핑과 실제 homeX/retreatPeakX/가용 여백/capped거리. 여백0이면 spawn framing 조정 후 가려짐 없이 관측. 단순 animation명으로 판정하지 않음 |
| E02 | homeX 상대 이동, 발/그림자 기준Y 유지, sprite/무기/보스가 Safe Area 밖으로 잘리지 않음.4–6개 구별되는 windup pose와 발/무기 연속성 | 대표 archetype 연속 원본 PNG/clipFrame/sourceFrame/root/body/feet/bounds.2프레임 튕김·run 역재생·scale 찌그러짐을 최종 windup으로 채택하지 않음 |
| E03 | 실제 전진·타격 pose·hit VFX와 이벤트가 I에 정렬하고 원위치까지 복귀. Stage11 기존296계획px forward contact 등 변경하지 않기로 한 reach는 인계 기준과 대조 | 처음·반복과 다단히트의 실제 X 극값·접촉전후·중간복귀·끝오차. 이벤트 PASS만으로 대시 PASS 처리 금지 |
| E04 | 현재 공격 > 이전 복귀 > 다음 windup 우선순위. 후속 cue가 복귀를 끊지 않으며750ms 간격에 추가 gameplay 지연 없음 |2·3연타의 전체 큐/impact/해결 순서, 각각의 후퇴/hold/전진/복귀 구간. 기존 danger bounds·dodge windows·피해값 불변 대조 |
| E05 | 일반 피격은 flash/reaction만, 적 큐·연출시계를 재설정하지 않음. Core의stun/death 취소 시에만 중단하고 잔여 대시·중복피해 없음 | 후퇴/전진/복귀 중 일반타·stun·death의 인접 경계. 별도 tween과 일반 Presentation의 이중 위치쓰기 없음은 소스 인계+실제 좌표로 확인 |
| E06 | 플레이어 slash/효과 endpoint가 후퇴 중인 적을 따라가며 후퇴가 무적을 만들지 않음. 음소거·흔들림 감소에서도 후퇴·형태·타이밍 단서 유지 | 후퇴 구간 실제 플레이어 hit와 적 hpMilli 감소, target/contact 위치, reduced-shake/mute 대표 화면 |
| E07 | 방향 순서 큐는 CueAt/CueEnd에 순서를 보이되 완료 flash 없음. 활성 hit의 timing fill만 I에 완료, 다음 hit는 이전 해결 후750ms. 마지막120ms 고대비 | 큐ID/활성threatID/fill/색/alpha와 PNG. 미래 방향 공개 시점과 현재 실행 타격 표시를 분리 |

연속 캡처는 첫 I−550ms 이전부터 마지막 I+330ms 이후까지 포함하고, 다음 반복은 별도 짧은 구간으로 분리한다. 긴 readback이 시계를 오염시키지 않게 기존 buffered collector를 사용하며 forced pose/Attack 샘플은 실제 전투의 증거로 사용하지 않는다. 적 종류별 정확 홈/스케일/아트/안전cap은 Root 인계값으로 대조한다.

정확한 이벤트시계와 렌더 샘플시계는 별개다. phase 경계 전후 프레임이 존재하고 가장 가까운 샘플 오차가 그 관측 framegap 안인지 기록한다. 렌더가 I와 정확히 같아야 한다거나 항상 최대전진차2px여야 한다는 단언은 쓰지 않는다. root 이동을 계획px로 환산해 카메라 흔들림을 제외하고 화면 발−지면Y도 별도 기록한다. 회복 끝 root오차≤1계획px, 고정 ground pivot 오차≤0.5화면px를 기본 수치 기준으로 쓰되 최종 아트의 의도된 공중/body-offset 계약과 구분한다. framegap이 짧은 phase를 통째로 놓치면 PASS가 아니라 관측 불충분으로 남긴다. 전투 프레임 수집은 정상속도60fps 성능 인증이 아니다.

## P · 실제 Unity 픽셀 파티클과 전투 상태

구현 인계 대상은 [SkillPixelEffects.cs](../../Client/Assets/Game/Runtime/Presentation/SkillPixelEffects.cs)와 [DragonV5SkillEffectsMigration.cs](../../Client/Assets/Game/Editor/DragonV5SkillEffectsMigration.cs)다. 아래 이름·매핑은 현재 소스에서 확인한 계약이며 최종 동결 소스와 저장 asset으로 다시 대조한다. 소스 존재나 픽셀처럼 보이는 PNG만으로 실제 ParticleSystem 적용을 PASS 처리하지 않는다.

| 저장된 실제 ParticleSystem | 컴포넌트 참조 | 관측 대상 |
| --- | --- | --- |
| SkillContact |AttackParticles |`skill_luka_gale_slash` 금색 참격, `skill_daren_iron_counter` 청색 접촉, `skill_miel_star_seal` 민트 접촉, 실제 보호막 흡수 파편 |
| ActualShield |ShieldParticles |플레이어의 실제 보호막 잔량·남은 시간에 대응하는 윤곽 |
| ActualStagger |SealParticles |미엘의 실제 stagger 누적·기절 준비. 실제 기절 중에는 모든 캐릭터에서 별3개·15입자로 대체하고 기존 UI 별은 중복 표시하지 않음 |
| ServiceAndRecovery |SupportParticles |수락된 강화·축복/예약·회복에 대응하는 금색/청색/민트 효과. 최종 제품 호출 연결을 인계받음 |

| ID | PASS 기준 | 필수 증거 |
| --- | --- | --- |
| P01 |전용 prefab에 서로 다른 실제 ParticleSystem4개와 renderer/material/Point texture, 전용 camera·RawImage가 저장되어 있고 실제 scene의 BattlePresentation.SkillEffects 참조와 연결. 실행 중 생성한 대체 hierarchy로 저장 누락을 숨기지 않음 |최종 prefab/scene GUID·해시·serialized 참조, 실행 인스턴스의 타입/개수/대응 경로, owner의 Apply·재로드 검증. migration 반복 시 중복 노드/참조 손실/기존 자산 덮어쓰기 없음은 owner 증거 재사용; QA가 Apply하지 않음 |
| P02 |실제 ParticleSystem 출력이 투명 저해상도 RenderTexture→RawImage로 합성. RT와 원본 texture는 Point, mipmap/MSAA/HDR 없음, 직교 camera와 투명 clear. 흐린 필터·검정 사각형·다른 월드 오브젝트 혼입 없음 |실제 SafeRoot 화면 bounds와 ScreenPixelsPerTexel, RT 크기/format/filter/AA/mips, camera.targetTexture와 Surface.texture 동일 참조, 확대 원본 PNG. 현재 기본2이면 inset 없는 전체화면1280/640에서 RT640×360/320×180 예상; 최종 저장값·실제 bounds로 계산 |
| P03 |세 캐릭터 스킬의 실제 수락·피해 이벤트/skillId/대상과 맞는 공격 효과가 한 번 발생. 루카 참격 방향은 기존 accepted slash variant와 일치하고 후퇴 중 적 endpoint를 따름. 거절·쿨 중 클릭·held/중복 callback·동일 frame 재렌더가 새 효과를 만들지 않음 |명령→권위 이벤트→입자 수/위치/색·PNG 연결, 일반 공격과 스킬 구별. 효과가 피해·쿨·stagger·보상을 추가 변경하지 않는 전후 상태 |
| P04 |pause·대화·이동·튜토리얼 hold 등 combat clock 정지 구간에서 전투 타이머와 전투 효과의 시간 기반 진행이 멈춤. 복귀 시 실제 남은 시간부터 이어지고 catchup/재발동 없음 |정지 전/중/후 combatClock·CombatSuspended·phase·shield/stun 잔여시간과 입자 위치/수명/수 비교. 여관의 수락된 UI 효과는 별도 UI clock임을 구분. 소스의 처치 직후 짧은 cosmetic tail은 최종 owner 계약(현재0.24초)과 대조하고 전투 clock/피해/보상 증가가 없어야 함 |
| P05 |보호막은 실제 잔량>0·남은시간>0·생존 상태에서만 전투 중 표시.3초는 combat 시간이며 pause로 소모되지 않음. 기존 Core의1회 보호 계약에 따라 피격1회에서 min(피해,보호량) 흡수 후 전체 소멸;3초 만료/사망에서도 제거 |발동·3000ms 전후·흡수 전후의 shieldMilli/remainingMs/absorbedMilli/HP·이벤트·PNG. 흡수 파편과 자연 만료 효과를 구분하고 같은 제거에 이중 burst 없음. 프로브의 마지막180ms 만료 fixture는 전체3초 실시간 관측과 구분 |
| P06 |미엘의 stagger0/누적/threshold100 준비/실제 stunned/종료·면역 상태를 구분. 준비 상태를 기절로 표시하거나 별도 slow/debuff 시간을 발명하지 않음. 실제 기절 진입 시 준비 seal을 별3개·15입자로 대체하며 모든 캐릭터에 적용. 기존 UI 별은 숨기고 적 stunned pose·게이지는 권위 상태/잔여시간과 일치 |stagger/threshold/IsStunReady/IsEnemyStunned/IsStaggerImmune·stunRemaining, SealParticles15개·View.StunStars 비활성·actor의 동시 PNG. 기절 종료 시 별0개. 적 공격 중단/재개는 Core 계약대로이며 파티클이 기절을 연장하지 않음 |
| P07 |강화·축복/예약·회복 효과는 실제 성공 이벤트/거래1건에1회. 취소·돈 부족·잠금·반복 Render·같은 저장 재로드로 발생하지 않음. 회복 효과는 실제 회복 결과와 맞고 별도의 heal을 만들지 않음 |거래ID/수락 결과·재화/예약/HP 전후와 SupportKind/위치·입자. 성공한 서비스·처치 회복의 제품 호출 연결 확인; QA가 PlaySupport를 직접 호출한 그림만으로 연결 PASS 처리 금지 |
| P08 |stage/enemy instance/run/캐릭터 교체·사망·여관 복귀에서 이전 대상의 접촉 큐·참격·seal이 새 대상으로 넘어가지 않음. 유지되는 보호막/효과는 권위 상태와 최종 전이 계약에 따름 |전환 직전/직후 원장·대상·4개 입자수·Surface.enabled. 이전 효과 잔상·중복 camera/RT 없음. Disable/재활성·화면 크기 변경 시 RT 해제/재연결과 위치 정렬은 최종 저장 hierarchy를 사용 |
| P09 |1280/640에서 효과가 적의 방향 순서 큐·활성 timing fill·마지막120ms 고대비, 플레이어/적 HP·회피/공격/스킬 상태를 가리지 않음. RawImage.raycastTarget=false, pause/modal 아래에 합성되어 조작을 가로채지 않음 |대표 후퇴·다단히트 예고에 스킬/보호막/미엘 seal·지원 효과가 겹치는 실제 연속 PNG, bounds·sibling/sorting·raycast. reduced-shake에서도 구분 가능한 형태 유지; 화면 전체 flash/크기 증가로 기존 단서 소실 없음 |
| P10 |native 재시작에서 완료된 공격·흡수·강화/예약·회복 burst를 재생하지 않음. 보호막·기절 같은 권위 상태의 보존/초기화는 Client의 저장 계약과 일치하고 해당 현재 상태만 렌더 |종료 전 expected state와 새 process의 저장 로드·최초 렌더·후속 경계. 다른 run/적에 과거 효과가 붙지 않고 HP·쿨·보상·타이머가 추가 변경되지 않음. 파일 Bind 검사와 native 재시작 증거를 구분 |

입자 생성 여부는4개 실제 ParticleSystem의 particleCount/GetParticles 결과와 실제 렌더를 함께 관측한다. 현재 구현은 SetParticles와 Simulate(0)로 위치를 갱신하므로 `ParticleSystem.time`이 증가해야 한다는 단언은 쓰지 않는다. 같은 combat clock에서의 시간 기반 위치/수명·색·수 변화와 actor 이동에 따른 좌표 정렬을 구분한다. source/saved asset/runtime/render를 따로 기록하며 실제4개가 있다는 사실을 GPU 성능·정상60fps·오디오 또는 OS 실입력 검증으로 확대하지 않는다.

## T · Stage5 두 번의 실제 정답 입력

| ID | PASS 기준 |
| --- | --- |
| T01 | Stage5 첫 미니보스 등장/포효 뒤 `combo_practice_v2`가 계정1회 진입. 이전 읽기용 comboHintSeen만으로 완료 취급하지 않음 |
| T02 | low_sweep/Up의1800ms에서 combat hold, **새 Up press edge1개**로 정상회피1회. 이후 combat750ms 진행해 high_sweep/Down-parry의2550ms에서 hold, **별도의 새 Down press edge1개**로 정상쳐내기1회 |
| T03 | 오답·누르고 있던 입력·prebuffer·같은 edge 재사용은 진행0. 공격·스킬·동료 공격 잠금. hold 중 전투/스킬/동료 타이머 동결. HP는 진입값 보존하며 무해한 guided hit에 보충 heal도 없음 |
| T04 | guided practice가 보스피해·기절·회복·재화·perfect 보상을 만들지 않음. 두 정답 후 completed1회 저장, recovery 후 일반 입력 복귀 |
| T05 | 같은 Up→Down을 실제 타이밍으로1회 제시. 실패는 정상피해이며 guided 재강제 없음; 이후 일반 random. 기존 회피 active240+recovery120=360ms 및 일반 쿨 규칙 불변 |
| T06 | skip은 completed와 별도 skipped 저장, 이후 자동 재진입 억제. 중단 저장은 HP·남은 스킬쿨을 보존하고 첫 guided hit부터 재개, 보상/완료 중복 없음 |
| T07 | 도움말 재연습은 별도 무보상 상태. 종료 시 실제 run의HP/쿨/진행/동료 남은시간을 정확 복원; 캐릭터 교체만으로 자동 재강제 없음 |

입력 기록에 edge sequence ID, down/up, command, accepted/reason, hold 단계/clock, 전후 HP/쿨/동료타이머/보상을 남긴다. delegate 호출만으로 T02를 통과시키지 않고 합성 pointer의 새 press/release 경로와 실제 제품 핸들러를 사용한다. 이는 OS 입력 또는 실기기 터치 검증이 아니다.

## C · 첫 만남·여관·성장·편성

| ID | PASS 기준 |
| --- | --- |
| C01 | 새 계정 첫 만남은2토리/12바룬. 적 처치 보상·회복 확정 후 다음 이동 전에 대화. 기존7리나/15바룬/20도토 첫 소개가 새 계정에서 중복되지 않음.10/20 보스 대사는 이미 만난 인물 재등장 계약과 맞음 |
| C02 | 완료/스킵에 계정NPC Lv1+서비스 해금1회. 다른 캐릭터·재방문·중복 callback에서 레벨/재화 재지급 없음. 재회1페이지는 최종 seen 정책대로 한 번만 |
| C03 | 새 첫토리는 빈1번 슬롯 자동합류,Stage3부터 지원. 처리한2층 보상 재계산 없음. 이후 첫만남은 빈자리일 때 함께가기/여관에서만나 선택, 가득 차면 여관에서만나만 표시 |
| C04 | 합류는 그 NPC의현재레벨/피해/주기 스냅샷과 캐릭터 편성에만 추가하고 DPS cap 재계산. 기존HP/회복/스킬쿨/다른성장 보존. 첫만남 외 원정 중 편성·강화 거절 |
| C05 | 여관 성장/동료/유물 분리, 왼쪽 슬롯+scroll목록3행/오른쪽 상세·공격·서비스·현재/다음·가격·강화.640에서도 행/슬롯/서비스/강화/출전 bounds와 raycast 분리. 마지막 목록 항목까지 접근 |
| C06 | NPC미발견/해금미편성/편성/레벨한도/최대/자금부족/예약/미래콘텐츠 상태와 행동이 일치. 골드/보석은 우상단, 출발 Stage N은 실제 캐릭터 checkpoint와 일치 |
| C07 | 동료Lv1–30 계정공유, 한도는보스0/10/20→10/20/30. 목표n 비용=max(20,ceil(upgradeCosts[n]/2)); Lv1해금무료.11/21돌파·한도·부족금·중복 클릭·캐릭터 변경·저장복원 확인 |
| C08 | 슬롯계정공유/편성ID·순서캐릭터별, 중복동료 금지.2슬롯=보스10+바룬+보석4,3슬롯=보스30+바룬+보석8. 현재 새 계정1–20에서3슬롯은 잠금.3슬롯 규칙은 미래조건 fixture로만 확인 |
| C09 | 가득 찬 편성 교체는 선택 슬롯을 명확히 지정. 동료 제거해도 서비스 유지. 유물 capacity/legacy unlockedSlots를 동료 슬롯으로 재사용하지 않음 |

NPC는 플레이어 선택 목록에 들어가지 않으며 기블·브라크는 영입 대상이 아니다. 새 계정1–20에서 잠긴 미래NPC를 조건 없이 실제 영입시켜 통과시키지 않는다. 모든5종 data/규칙과 미래 서비스는 `fixtureScope=future-rule-only`로 구분한다.

## A · 지원 자동 공격

| NPC | 주기ms | 기본 피해계수(A 대비) | 식별 |
| --- | --- | --- | --- |
| tori |6000 |0.24 |작고 밝은 화살1발 |
| barun |10000 |0.40 |대검1회, 플레이어 가리지 않음 |
| doto |8000 |0.32 |작업망치1회, 기절/경직 없음 |
| rina |7000 |0.28 |작은 민트 바람방울1개 |
| momo |6000 |0.24 |씨앗1회, 독/DoT/치유 투사체 없음 |

`A`는 플레이어 공격레벨·별점·돌파만 반영한 출전 기본공격. `G(L)=(1+0.03*(L−1))*1.25^floor((L−1)/10)`, `Q=min(1,0.24/sum(0.04*G(Li)))`; 피해는 `A*계수*G*Q`를milli 단위 반올림한다. 유물·제작·치명·기절·스킬 배수가 포함되지 않아야 한다. 예: A100 토리Lv1=24/6초, Lv11=39/6초.3명Lv30 토리/바룬/도토는48/80/64, 합24 DPS.12%라는 수치는 평타 이론2A/s 대비이며 실제 플레이 피해비율 측정 완료로 쓰지 않는다.

| ID | PASS 기준과 캡처 |
| --- | --- |
| A01 | 생존 Combat에서만 흐르며 숨은 최근 입력 조건/수동 동료 버튼 없음. 플레이어 무입력 구간에서 실제 지원 contact 최소3회/동료를 이벤트로 관측하여 주기 누적오차 확인 |
| A02 | 최초 contact=주기+슬롯index×400ms, 이후는실제 contact→다음 contact가정상주기.180ms예비가매주기추가되지않음. 최초/두 번째/세 번째 예정·실제contact 및windup clock 기록 |
| A03 | 대화/전환/보스등장/메뉴/pause/tutorial 동안 남은시간동결, 스테이지사이유지, 복귀밀린타격몰아치기없음. 현재 적Combat 시작350ms 전에 contact 금지; 필요한 만큼 늦춘 contact부터 다음주기계산 |
| A04 |180ms전 예비, 예정 시각 살아있는 현재 enemy instance에정확히1회. 적교체/플레이어사망 때 이전 적 예약·완료 공격 재생 없음. 최종 Client 계약상 같은 적의 저장복귀는 남은시간·진행중windup을 보존하며 새 적에만350ms grace 적용. 예정공격ID/대상ID/발동·취소·resolve 원장 기록 |
| A05 | 치명·stagger증가/100발동/시간연장/배수, 스킬자동사용/충전/쿨단축, perfect/콤보, 유물/추가골드 추첨, 동료HP/대신피격/부활을 만들지 않음 |
| A06 | 동료막타도 일반처치와 같은 보상·회복·이동1회. 이미죽은플레이어라면공격·회복취소. 일반지원에shake/대형흰flash 없음; 막타의공통처치피드백만1회 |
| A07 | 플레이어뒤 작은보조실루엣, 적위험방향/HP/회피를가리지않음. 실제5종지원아트완료여부는아트인계와일치; 미래규칙fixture 관측을미래지역플레이로부르지않음 |

주기 전체를 PNG로 저장하지 않는다. 전투 clock 이벤트를 지속 기록하고 실제windup/contact/return 주변만 짧게 연속 캡처한다. 관측용 정당한 highHP fixture와 무입력 구간을 표시하며, 일반 캠페인 난이도·장기경제 밸런스 합격으로 확대하지 않는다.

## S · NPC 서비스와 거래 경계

| 서비스 | PASS 기준 |
| --- | --- |
| 토리 길안내 | 영구무료·미편성도유효.클리어골드+3%p를행운과가산,기존150%상한·최종1회내림.교환/유물판매/다른보상제외.첫해금다음스테이지부터,이미준2층보상변경없음 |
| 바룬 짐마차 |C08의조건/4·8보석가격/계정슬롯만변경,별도스탯보너스없음. 구매전확인/취소/부족보석/재입력에정확1회차감 |
| 도토 무기 |on/dam/rin 각전용무기·캐릭터별rank0–3.8400G/+4%,보스30/125100G/+7%,보스40/10337000G/+10%.단계보너스는누적4+7+10이아님.유물상한20%와가산최대30%;동료A제외.이전20도토완료저장은22도달요구없이rank1가능 |
| 리나 축복 |다음출전maxHP+5%p,유물HP상한25%와합최대30%.가격ceil(출발stage normalGold×0.10),stage1=2G/11=18G.현재원정즉시heal/중도구매없음.기존7완료저장소급서비스가능 |
| 모모 약초차 |다음출전회복R의15%반올림추가량고정,동일가격식.예on R496+74=570.추가곱셈/초당heal/신규회복유물없음.현재자연해금불가상태와미래조건규칙fixture를구분 |

서비스는 계정NPC해금이 필요하지만 출전편성과 무관하다. 서비스행→상세modal→효과/기간/조건/현재·다음/정확가격→확인·취소를 캡처한다. 토리는활성표시만,구매버튼없음. 동료Lv강화와다른명령/재화변경으로검증한다.

축복/차는 각각캐릭터·다음출발stage·지불액을묶은예약1개,재구매중첩/연장없음. 출전전취소는정확전액환불1회,출전후사망/포기는소모. 출발stage변경시미소비예약환불후재견적이며다른캐릭터예약을소모하지않음. 부족골드는기존보석→골드정확견적/별도확인만제공하고자동교환/즉시보석강화가격추가없음. 이중클릭·열기닫기·저장복원으로구매/차감/환불을중복하지않는다.

## M · schema8 이전과 저장 보호

목표 저장 schema/dataVersion/namespace는 **최종 Client 인계 대기**다. 계획 JSON의schemaVersion1을게임저장버전으로사용하거나임의로schema9로단정하지않는다. schema8 입력·변환결과·재로드결과와원본checksum을보존한다.

| ID | 격리 입력 fixture | PASS 기준 |
| --- | --- | --- |
| M01 |v4 세 캐릭터4스탯/지갑/유물/편성없음 |기존순서·레벨·ID보존+recoveryLv0.동료슬롯1/편성빈칸/무기0/예약없음.캐릭터checkpoint와해금NPC별개 |
| M02 |여러캐릭터 seenStoryIds 합집합 |encounter2 또는boss10→tori,encounter7→rina,encounter15→barun,boss20→doto.최대4명Lv1+서비스1회,무료.단순unlockedStoryIds만으로해금금지 |
| M03 |최고클리어보완·같은NPC의진행중레거시scene |새합류단계이상최고클리어의보완해금은허용하되그NPC의진행중대화는완료/스킵까지대기.관련조건의조합fixture를Client검사로확인 |
| M04 |진행중7/15/20 각중간page·스킬쿨·HP |레거시정의/sceneId/lineIndex/현재stage유지,완료/스킵시한번해금후원래진행으로복귀.12/22/32순간이동/되돌림/추가보스보상/추가회복없음 |
| M05 |기존원정/보스처치후/유물선택/대사경계 |HP·clock·보상·선택·쿨·전이보존.과거완전heal은되돌리거나반복하지않고다음미처리처치부터R5. migration자체의불시자동편성없음;레거시첫만남완료의빈자리함께가기만허용 |
| M06 |v5 tutorial hold/동료예비중/서비스예약·처치원장 |재시작후완료타격·보상·회복·해금·차감/환불중복없음,타이머catchup없음.연속2회로드가같은상태 |
| M07 |잘못된/불완전한변환입력 |변환검증실패시정상원본보존.계획된저장fallback 계약대로처리,fixture 밖파일변경없음.이검사는실제사용자저장에실행하지않음 |

실제 사용자 기본 프로필은 **NOT_ACCESSED**: 읽기·복사·해시·실행전후비교 대상으로 삼지 않는다. 이전QA evidence의schema8 저장 또는새합성fixture만새 candidate 하위에복사한다. CLI override가출력폴더의strict child인지,기본persistentDataPath와겹치지않는지,link/reparsepoint·기존결과덮어쓰기가없는지검사한다. 원본candidate는불변. 현재승인된범위의game/Editor PlayerPrefs만백업·finally복원,합성구저장sentinel은별도전후해시검사한다. 승인받지않은Unity/기타레지스트리설정은수정하지않는다.

## 재사용과 추가 캡처 설계

| 기반 | 재사용할 내용 | v5에서 교체/추가할 내용 |
| --- | --- | --- |
| [V4 probe](../../Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.V4.cs) |Pointer/raycast,실제Rendered 대기,파일복원delta기록,root/발 측정 패턴 |별도V5 partial/모드로버전·5스탯·NPC·서비스·tutorial·migration상태계약추가. V4의고정25페이지/2·7·15·20만남/우측고정초상가정재사용금지 |
| [V3 collector](../../Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.V3.cs) |buffered실제렌더프레임/PNG SHA/frame gap·clip/root/feet 메타데이터 |V3전수campaign 실행없음. V5목적라벨·phase별연속구간과적/동료/입력/거래/스킬효과 이벤트 연결,4개 ParticleSystem·RT/RawImage 관측 |
| [V4 runner](../../Tools/QA/Dragon-v4/run-campaign.ps1) |후보경로·해시·CLI격리·원본보존·native exit/timeout·scopedprefs복원·실패후중지 |별도Tools/QA/Dragon-v5 준비 시에만새관측버전/실행계획/legacyseed/resume조건명시. 기존v4-candidate 재실행금지 |
| [V4 analysis](../../Tools/QA/Dragon-v4/analyze.py) |hash/누락/중복/ground·contactsheet/JSON/CSV분석 |retreat/hold/advance/contact/return의계획px·시각표,UIbounds,튜토리얼edge표,companion예정/실제contact표,회복/거래/이전원장diff |

추가 evidence안은 `v5-state.jsonl`, `v5-events.jsonl`, `v5-evidence.json`, `v5-migration.json`, `v5-resume-expected.json`, `frames.jsonl`과원본PNG다. 기존공용events/timeline/result 파일을유지할수있다. 이름은설계안이며제품API필드가존재한다고가정하지않는다. Client가인계하는권위상태/이벤트에매핑하며진단용reflection이나범용프레임워크를새로만들지않는다.

각기록에는build/data/schema/fixtureOrigin,runId/stage/enemyInstance/phase,combatClock/realtime/frame,입력edge·명령수락,HP/회복원장,selectedCharacter,스킬쿨,튜토리얼step/hold/completed/skipped,동료ID/slot/level/예정contact/남은시간/대상/resolve,계정해금/편성/슬롯/무기/예약/재화전후,화자역할/scene/line/displayPage/portrait/bounds를필요시포함한다. actual과expected를함께보존하고파생지표를원본보다우선하지않는다.

P 관측 구간에는 source prefab/scene GUID·hash,4개 system 경로/instance/particleCount·대표 입자 position/size/color/lifetime,RT 크기/filter/AA/mips·Surface texture/bounds/raycast/sorting,skillId/이벤트시각/slash variant/대상,shield 잔량·남은ms·흡수량,stagger/ready/stunned/immune·남은시간,SupportKind/성공거래·회복 원장을 추가한다. 효과 clock의 직접 진단 API가 없으면 공개 권위 clock과 입자 표본으로 전후를 비교하고 관측 한계를 명시한다. private 필드를 읽는 범용 reflection이나 제품 동작을 바꾸는 QA 강제효과는 추가하지 않는다.

v4에서확인한교훈을유지한다. JSON/file복원시간만1e−6ms 절대허용오차와delta를사용하며원장/재화/milli/같은인스턴스freeze는정확비교한다. 다중AudioSource에optional OnAudioFilterRead tap을붙이지않는다.해시/clip바인딩/event/PCM/직접청음을별개상태로둔다.기계PASS와직접시각PASS를분리하고검토한원본/대표sheet범위를명시한다.

## 완성 후 한 번의 통합 batch

소스동결·관측빌드와실행권을Root가인계한뒤한번의후보campaign으로묶는다. 기본안은아래4프로세스이며최종저장진입점계약에따라순서/초기seed를동결때확정한다.4개로native이전경계를확인할수없는경우에만별도legacy-startup process 필요를인계문서에명시하고,작성단계에서추가실행하지않는다.

| 순서 | 관측 범위 | native 경계 |
| --- | --- | --- |
|1 ·1280 통합 |초기boot에격리schema8 실제QA 저장을사용해레거시20대화이전을확인하는안.그뒤명시한새계정/기존4명/미래규칙fixture로D/R/E/P/T/C/A/S/M 변경범위와대표9→10→11·19→20 흐름.기존7/15는repository마이그레이션+실제UI파일복원 |native startup검사와이후새계정fixture를구분.튜토리얼중단hold를식별가능한checkpoint로남김 |
|2 ·1280 재시작 |같은격리저장의guided중단→첫단계재개,HP/쿨/진행보존,보상/완료중복없음 |부팅직후렌더와권위상태대조 |
|3 ·640 통합 |같은변경UI·실제후퇴/접촉·스킬파티클/지원·두정답·대화/서비스/회복/이전경계의작은화면관측.순수Core전수중복없음 |동료예정contact가남은원정및HP/쿨/보상원장을저장. P10의상태를함께검사할seed는동결때확정 |
|4 ·640 재시작 |같은 적의 동료남은시간·windup 보존,완료타격재생없음,정확1회contact와P10의과거burst재생없음·현재보호막. 새 적350ms grace와동료막타회복의나머지조합은Client증거/미관측범위를별도명시 |파일로드만으로native재시작통과처리하지않음 |

각해상도에서새/수정대사전체표시페이지·좌우화자전환·여관핵심상태를정지PNG로,각바디유형의후퇴와대표combo/반복·지원VFX·guided입력을짧은연속구간으로남긴다. 긴타이머는상태/event로확인하고경계만렌더한다. Core의수식/조건조합증거를그대로반복하는UI테스트는추가하지않는다.

P01–P10은 이 batch의 기존 스킬·거래·회복·pause/대화·전이·재시작 구간에 묶는다. 세 스킬 contact,보호막 발동/흡수/3초 만료,미엘 누적/준비/기절/종료,성공/거절 지원효과와 적 예고 중첩을 두 해상도에서 짧게 캡처한다. 보호막/기절 저장 계약과 현재4프로세스의 재시작 seed가 양립하는지는 동결 때 확정한다. 둘을 native로 모두 검증하지 못하면 미관측 경계를 명시하고 필요한 표적 process만 계획하며, 파티클 추가를 이유로 별도 전체campaign을 반복하지 않는다.

실패하면그시점candidate/Console/입력/저장/PNG를보존하고나머지프로세스를중지한다. 먼저제품/QA단언/관측불충분을분류한뒤수정된기능과직접인접한경계만새candidate로재검증한다. 동결코드·아트·카탈로그·원본해시로영향이없는통과증거의채택범위를명시하며전체1–20/대사/Core를무조건반복하지않는다. 문서갱신마다QA를실행하지않는다.

## 실행 전 최종 handshake와 완료 보고

| 담당 인계 | 필요한 확정값 |
| --- | --- |
| Root/빌드 |v0.9.4관측exe·Runtime SHA·dataVersion·build operation·define/QA모드·실행권·동결파일목록 |
| Root/UI |D02의1280초상/패널/이름표위치·이름36/본문32는수신완료.저장prefab/scene적용증거·최종anchor/이름표크기/본문여백·640/Safe Area규칙,typer페이지매핑,여관modal/scroll/타깃bounds는대기 |
| Root/연출 |body archetype→stage,windup clip/contact pose,home/거리cap/framing,activeTimingFill와방향큐,hit/stun/death우선순위·각actor의표시ground계약 |
| 스킬효과 담당/Root |SkillPixelEffects·migration 최종 소스해시,4개 저장ParticleSystem/prefab/scene 참조와Apply·재로드·중복없음 검증,RT/RawImage설정,성공한서비스/회복 호출연결,combat freeze·처치tail·전이/재시작 표시정책.미엘 실제기절은 기존actor/별/게이지와의 계약까지 포함 |
| Client |최종schema/namespace·실제마이그레이션진입점,회복·튜토리얼·동료·거래의권위API/events와거절사유,종료/재시작중pending contact정책,동일동결소스Core결과 |
| Story/아트 |신규·수정·레거시scene IDs/페이지,역할→이름/초상매핑,미엘만재제작자산·기존초상재사용manifest,5NPC의실제지원아트/미래규칙표시범위 |

미확정항목은PENDING_HANDOFF로남기며제품코드에추측값을넣지않는다. **대화 좌표 계약과 Root의14:38:50Z 저장 Apply 완료·오류없음 인계를 수신했다. 동료 Run 아트·최종 관측 빌드·QA 렌더 검증은 대기 중이다. 파티클 소스 확인도 QA 실행 PASS가 아니다.** 이대기는별도의사용자승인요청이아니라Root/담당자가제공할구현계약이다.

최종보고는각ID별PASS/FAIL/NOT_RUN/PENDING_HANDOFF,정확한소스·빌드·저장범위,Core재사용근거,기계검사와직접시각검토범위,결함/표적재검증과미실시항목,프로필NOT_ACCESSED/scopedprefs복원/sentinel결과를분리한다. **관찰 probe 소스 준비는 완료했으나 Unity 컴파일·QA실행·제품합격은 아직 아니다.** 실제 준비 범위와 자동화하지 않은 기준은 [프로브 준비 인계](dragon-v5-probe-preparation-20261024.md)와 [도구 README](../../Tools/QA/Dragon-v5/README.md)에 기록한다.
