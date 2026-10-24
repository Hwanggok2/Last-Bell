# Dragon-v2 최종 QA 결과 · 후보01 + HP 표적 후보02

2026-10-24 KST. **요청된 변경 범위 PASS / DQA2-001 CLOSED.** 후보01의 모션·판정·표적 PlayMode11/11 결과와 후보02의 HP30상태 재관측을 합산해 QA를 종료한다. 자동 결과와 PNG/프레임 기록 시각 검토를 구분하며, 정상속도 영상 직접 재생·체감 부드러움/60fps·OS 실입력은 검증 범위에 포함하지 않는다. 최종 HP 수정이 포함된 일반 배포 빌드 갱신/패키징은 총괄의 후속 단계다.

## 실행과 자동 결과

두 후보 모두 `Builds/Dragon-v2-QA/LastBellMvp.exe`, 앱0.9.1/데이터mvp1.14.0이다. 최초 [후보01 manifest](../Evidence/Dragon-v2/dragon-v2-candidate-01/candidate-manifest.json)의 Runtime DLL SHA256은 `7c151285e5431b3de798fb2efd802acb3c81797f0ec723cf5835e4f770079b4a`다. HP 수정 반영 [후보02 manifest](../Evidence/Dragon-v2/dragon-v2-candidate-02-hp/candidate-manifest.json)의 Runtime DLL SHA256은 `41fe0fbffacaa8798db29bbea9353559459af12ea58e9acdc210b7c577236a2b`다. 각 후보는 총괄의 컴파일/통합/관측 빌드 완료 인계 후 실행했다.

| 범위 | 결과 | 증거 |
| --- | --- | --- |
| v2 1280×720 |130검사 PASS, 오류0, native exit0, timeout 없음|[result](../Evidence/Dragon-v2/dragon-v2-candidate-01/v2-1280x720/result.json)|
| v2 640×360 |130검사 PASS, 오류0, native exit0, timeout 없음|[result](../Evidence/Dragon-v2/dragon-v2-candidate-01/v2-640x360/result.json)|
| v2-hp 1280×720, 후보02 |70검사 PASS, 오류0, native exit0, timeout 없음|[result](../Evidence/Dragon-v2/dragon-v2-candidate-02-hp/v2-hp-1280x720/result.json)|
| v2-hp 640×360, 후보02 |70검사 PASS, 오류0, native exit0, timeout 없음|[result](../Evidence/Dragon-v2/dragon-v2-candidate-02-hp/v2-hp-640x360/result.json)|
| 표적 PlayMode7메서드 |매개변수 HP5표본 포함11/11 PASS, 실패0/skip0|[테스트 결과](../Evidence/Dragon-v2/dragon-v2-candidate-01/unity-tests/playmode-result.json)|
| 격리/설정 |신규 후보별 하위 save, 두 프로세스 및 Editor 설정 복구 일치|[프로세스](../Evidence/Dragon-v2/dragon-v2-candidate-01/processes.json), [관측 설정 복구](../Evidence/Dragon-v2/dragon-v2-candidate-01/playerprefs-restored.json), [Editor 설정 복구](../Evidence/Dragon-v2/dragon-v2-candidate-01/unity-tests/editor-playerprefs-restored.json)|
| HP 표적 재관측 격리/설정 |별도 신규 후보, 두 프로세스 설정 복구 일치|[프로세스](../Evidence/Dragon-v2/dragon-v2-candidate-02-hp/processes.json), [설정 복구](../Evidence/Dragon-v2/dragon-v2-candidate-02-hp/playerprefs-restored.json)|
| Editor 반환 |Client@65e60f79bc76ded9/8093, Mvp 비Play, dirty=false, 테스트 종료|[Editor](../Evidence/Dragon-v2/dragon-v2-candidate-01/unity-tests/editor-return.json), [scene](../Evidence/Dragon-v2/dragon-v2-candidate-01/unity-tests/scene-return.json)|

후보01 관측은1280 18:19:39~18:20:55,640 18:20:55~18:21:35 KST였다. 후보02 HP 전용 관측은1280 18:35:01~18:35:10,640 18:35:10~18:35:17 KST였다. 전체 캠페인이나 추가 Core suite는 실행하지 않았다. 기존 활성 Editor의 Test Runner가 빈 테스트 씬을 만든 뒤 Mvp를 복구하는 경로를 사용했고 새 Editor를 시작하지 않았다. 후보02에서는 Unity/MCP를 조작하지 않고 standalone HP 관측만 수행했으며 편집 권한을 반환했다. 실제 마우스·키보드 입력은 계속 보류다.

실제 선택 `dragon_hero_on`의400ms Run/1920px 이동/root X 고정, 공격119/120/160/200/380ms 표시 및200ms 피해1회, 위·뒤 회피/아래 쳐내기와 HP 보존은 통과했다. HP26/31/50용량의0/tiny/25/50/100 값과 실제 Canvas mesh 채움 열 수도 통과했다.

## 실제 PNG와 프레임 기록 검토

모션 PNG는1280에서661개,640에서673개, 총1334개다. index 경로 중복0, 파일별 SHA256 전부 일치, 각 구간 내부 렌더 frameCount 누락0이다. HP는 해상도별10PNG에15바 상태, 합계20PNG/30상태다. [독립 분석](../Evidence/Dragon-v2/dragon-v2-candidate-01/evidence-analysis.json)에 캡처 간격·관측 clip frame·HP 실제 래스터 수치를 남겼다.

원본 화면과 각 구간의 **시간순 서로 다른 렌더 자세/전환 경계**를 접촉표로 직접 검토했다. [검토 PNG 폴더](../Evidence/Dragon-v2/dragon-v2-candidate-01/visual-review/)에는 두 해상도 각각 Idle, Attack(stage1/10), Up/Back/Down, 처치→Run→도착의7개 표와 HP15상태 표가 있다. 표의 선택 JSON은 원본 파일/frame/time에 대응한다.640 접촉표의 작은 crop만 nearest2배로 표시했고 원본640 전체 화면도 따로 확인했다.

- 갈색 머리·붉은 스카프·초록 옷·가방·한 자루 검의 정체성, 손/손잡이 연결, 팔다리 수에 새 결함을 찾지 못했다. Run에서 상체 기울기·발 교차·접지/비행 자세가 구별되며 Idle 전환에서 scale/root 흔들림이 없었다.
- 베기는 준비→베기→후속→Idle로 이어지고 작은 적/큰 보스의 접촉 이펙트와 피해 사건이 대응한다. 큰 적과 아래 쳐내기에서는 적 몸체가 일부 손/검을 가리는 프레임이 있어 그 부분의 가시성 한계는 남는다.
- Up의 상승/착지, Back의 뒤 이동/복귀, Down의 홈 위치 쳐내기가 구별된다. 실제 성공/HP 기록과 함께 확인했다.
-32×32/몸높이22px/8색/720p7배154px의 통합 기준을 사용했다.1280의 굵은 픽셀과640의 축소된 실루엣은 확인했다.

**정상속도 영상을 직접 재생해 본 검토는 아니다.** 실제 delta로 진행한 렌더 프레임 PNG와 기록시각을 대조했다. 캡처 평균률은1280 구간별55.70~59.02/s,640 57.96~59.39/s였고 최대 관측 간격은각각38.51/37.98ms다. 이는 체감 부드러움이나60fps 성능 보증이 아니다. 공격 준비/회피 진입의 일부 원본 clip frame은 실제 렌더 표본에 나타나지 않았으며 렌더 frameCount 누락과 구분한다. 영상 재생·미관측 중간 자세를 합격으로 대체하지 않는다. `file://` 접근 차단을 우회하거나 다른 브라우저/localhost로 재생하지 않았다.

## DQA2-001 ·640 HP 가로 경계 혼색 해결

**CLOSED / 후보02의 HP30상태에서 해결 확인.** 최초 후보01에서는0/tiny/25/50/100 채움 열 수가 맞았지만640 하이라이트와 위/아래 가로 경계에 혼색 행이 있어 단색2px 격자와 달랐다.1280은 기대 래스터와 일치했다. 아래 원본 관측 내역은 수정 전 이력으로 보존한다. 자동 mesh 검사 PASS만으로 이 시각 항목을 닫지 않았다.

[640 원본](../Evidence/Dragon-v2/dragon-v2-candidate-01/v2-640x360/v2-hp-640x360-stage-1-100.png), [비교 확대](../Evidence/Dragon-v2/dragon-v2-candidate-01/visual-review/hp-raster-edge-comparison.png), [좌표/색 JSON](../Evidence/Dragon-v2/dragon-v2-candidate-01/hp640-raster-observation.json).

| 항목 | 값 |
| --- | --- |
| 플레이어 bounds, 화면 하단 원점 |x210,y84.3870925903,width80,height18|
| 일반 적 / boss bounds |x404 /380, 같은 y84.3870925903, width92 /140,height18|
| PNG 상단 edge |257.6129074097px|
| PNG 좌상단 기준(218,261) |RGB126,165,146: 회색/하이라이트 사이 혼색|
| (218,262) |RGB114,185,155: 원래 하이라이트|
| (218,263) |RGB75,151,132: 하이라이트/채움 사이 혼색|
| (218,271) |RGB37,89,89: 채움/테두리 사이 혼색|

수정 전640의 각 양수 표본은 예상 채움 열 수를 유지하지만 exact highlight 픽셀은 기대4×filled 대신2×filled이고 exact fill은16×filled 대신14×filled였다. 혼색 경계 행을 합격 수치로 흡수하지 않았다. 원본 PNG는 수정하지 않는다.

Client 수정은 두 HP Rect의 실제 화면 좌하단 경계를 정수 픽셀로 반올림하는 평행 이동이다. 후보02에서1280 y168.774185→169(+0.225815px),640 y84.387093→84(-0.387093px)를 확인했다. x·폭·높이·용량은 그대로다. 이 경계 정렬 후 같은 capture 경로의 혼색이 제거됐다.

[후보02 독립 래스터 검증](../Evidence/Dragon-v2/dragon-v2-candidate-02-hp/hp-raster-verification.json): **30/30 PASS, 혼색 픽셀0, 모든 bounds 정수, 크기 유지, 이동 절댓값0.5px 이내**.1280 하이라이트/채움은 각 기대16×filled/64×filled,640은4×filled/16×filled에 정확히 일치한다. tiny는1열,0은 활성색0,25/50/100은 용량26/31/50에 맞는7/8/13·13/16/25·26/31/50열이다.

[1280 HP15상태](../Evidence/Dragon-v2/dragon-v2-candidate-02-hp/visual-review/1280x720-hp-15-states.png), [640 HP15상태](../Evidence/Dragon-v2/dragon-v2-candidate-02-hp/visual-review/640x360-hp-15-states.png), [수정 후640 전체 화면](../Evidence/Dragon-v2/dragon-v2-candidate-02-hp/v2-hp-640x360/v2-hp-640x360-stage-1-100.png)을 직접 확인했다. 테두리/채움 경계가 명확하며 이름·선반·입력 영역에 새 충돌을 찾지 못했다.

## 종료 범위와 인계

`v2-hp` / `-V2HpOnly`는 실제로 기존 `V2Health()`만 호출했고 모션 frame/segment/index가 생성되지 않은 것도 확인했다. 후보02에는 HP20PNG/30상태만 추가됐다. 후보01 모션과11테스트는 반복하지 않았다. QA 소스는 동결 유지하며 추가 실행은 없다. [통합 검증 요약](../Evidence/Dragon-v2/verification.json).

최종 HP 수정이 포함된 일반 배포 빌드 갱신과 패키징은 총괄에게 인계한다. QA는 일반 빌드 패키지/해시/별도 실행을 아직 검증하지 않았다. 기존 v1 0.9.0 배포와 모든 후보 증거는 보존한다.
