# 온 달리기 v9 · 전경 질주 프롬프트와 검수 기준

2026-10-21 · 기획 담당과 아트 담당의 공동 방향 · 생성 실행은 총괄 담당. 프롬프트 합의·반려 이력·최종 작성 후보를 함께 기록한다. 최종 후보는 `hero-v9-sprint-authored`이며, 제작 완료와 동작 품질 검토를 구분한다. Unity 적용 완료 기록이 아니다.

## 이번에 바꿀 동작

**몸을 앞으로 숙이고 양팔과 다리로 지면을 밀어내는 질주를 만든다.** 이동은 기존 960px/2000ms, 한 주기는 480ms를 유지한다. 다시 빨리 재생해서 질주처럼 보이게 만들지 않는다. 자세와 운동 경로로 사용자가 요구한 힘을 보여 준다.

직접 확인한 원본은 [Idle01](../Presentation/PixelLab/clips/hero-v5-idle/frames/01.png), [초기 영웅 원형](../Presentation/PixelLab/inputs/hero.png), [v8 전체 프레임](../Presentation/PixelLab/clips/hero-v8-run-consistent/contact.png)이다. 원본은 128×128 캔버스, 실제 인물 높이 약84px다. 색과 옷이 남아 있는 것만으로 충분하지 않다. v8 정지 시퀀스에서는 어깨가 골반보다 충분히 앞서지 않고, 검손이 몸 앞의 좁은 범위에 머문다. 이 관찰은 정지 프레임에 대한 것이며 실제 시간 재생 측정으로 표현하지 않는다.

이전 [v7 아트 검토](run-v7-art.md)의 원형 변화·검 소실·약한 전경·긴 체공 반려 이유를 이어받는다. 그 문서의 240ms/960px·1초는 과거 값이다. v9의 기준은 **480ms/960px·2초**다. 이번 문서는 외부 모션 연구를 새로 수행했다는 기록이 아니다.

## 공동 확정한 제작 순서

1. 실제 Idle01을 인물 디자인 기준으로 넣고, **이미 허리를 숙여 달리고 있는 키포즈 한 장**을 먼저 만든다.
2. 기획·아트 담당이 그 한 장에서 몸통 전경, 원형 비율, 두 팔과 검의 연결을 확인한다. 직립 자세나 다른 캐릭터가 나오면 애니메이션 생성 전에 키포즈를 수정한다.
3. 통과한 전경 키포즈를 첫 프레임으로 사용하는 **12장, 총480ms** 루프를 만든다. 원래 Idle을 첫 장이나 끝 장에 섞지 않는다.
4. 12장 전체와 마지막→첫 장 연결을 확인한 뒤, 480ms 반복 및 960px/2000ms 배경 이동과 합성해 검수한다. 정지 시트만으로 질주 체감·접지 통과를 선언하지 않는다.

16장은 12장의 연결 품질 때문에 필요할 때만 쓰는 대안이다. 12장은 장당40ms, 16장은 장당30ms로 **같은480ms**를 유지한다. 프레임 수가 많다는 이유로 선택하지 않는다. 최초 생성에는 공동 검토한12장안을 사용한다.

아래 생성 실험에서는 시작/끝 이미지를 같은 B로 보냈으므로 출력의 첫·마지막 그림이 중복인지 확인한다. API 요청 장수, 실제 받은 장수, 최종 재생에 쓰는 고유 장수를 각각 기록한다. 중복 끝 그림을 제외해야 한다면 최종 장수와 프레임별 시간을 다시 기록해 총480ms를 유지한다. 고유11장을12장 완성이라고 부르거나 끝에서 두 번 멈추는 루프를 그대로 쓰지 않는다. 최종 Workbench 후보는 이 실험 결과를 재사용하지 않고 `p0~p11`의12개 자세를 직접 작성했다.

## 원형과 동작 계약

| 항목 | 유지하거나 바꿀 기준 | 반려되는 모습 |
|---|---|---|
| 인물 | 큰 각진 크림 후드, 검은 얼굴 안 세로 황금 눈, 짧은 암청 몸통·팔다리, 크림 망토/옷자락, 허리의 작은 황금 종, 각진 부츠 | 긴 다리·큰 새 몸통·둥근 눈·다른 후드·추가 갑옷 |
| 검 | 같은 손의 두껍고 짧은 황금 검. 길이·폭·손잡이·손 연결 유지 | 프레임마다 짧아지거나 녹는 검, 사라지는 무기, 공중에 떠 있는 손잡이 |
| 몸통 전경 | 수직에서 약30~35°. 어깨가 골반보다 확실히 앞서며 루프 전체에서 유지 | 전체 스프라이트를 회전, 머리만 내밀기, 직립 몸통에 다른 발만 붙이기 |
| 머리·시선 | 얼굴은 동쪽을 보고 몸통보다 덜 기울어짐. 짧은 목과 낮은 머리 위치 | 목이 길어짐, 얼굴이 땅을 향함, 큰 후드가 매 장 다른 물체가 됨 |
| 팔 | 양쪽 상완·팔꿈치·손이 함께 움직이며 반대 위상으로 펌핑. 팔꿈치를 굽혀 손이 몸 앞과 뒤를 실제로 왕복 | 손목만 까딱임, 두 팔을 뒤에 고정, 한 팔이 정지, 양팔이 같은 방향으로 움직임 |
| 검을 든 팔 | 검손을 포함한 팔 전체가 낮고 짧은 전후 원호를 그린다. 달리기 자세로 읽힘 | 머리 위 큰 휘두름, 공격 참격, 검이 몸이나 얼굴을 관통 |
| 다리 | 앞 무릎 회수→접지→몸 아래 지지→뒤로 밀기→반대쪽 교대. 양쪽 위상은 반 주기 차이 | 발 패치만 좌우 이동, 역관절, 두 발 동시 착지, 두 발이 겹쳐 한 다리처럼 움직임 |
| 상하 변화 | 128px 원본에서 머리·골반의 작은1~2px 변화가 출발 목표. 낮은 질주 실루엣 유지 | 위로 통통 뛰는 반복, 머리가 크게 출렁임, 체공이 먼저 읽힘 |
| 화면·경계 | 동쪽 측면, 공통 기준점, 투명 배경. 검·부츠까지 캔버스 안의 투명 여백 확보 | 좌우 반전, 카메라 회전, 프레임별 자동 확대, 가장자리 잘림 |

아트 담당이 제안한 골반 약(60,95)과 어깨 약(74~77,70)는 **128px 시트의 검토 보조점**이다. 모델에 모든 좌표를 강제하지 않는다. 같은 그림으로 읽히는 비율과 실제 어깨–골반 기울기를 먼저 본다. 발 접지선은 원본의 y≈114를 출발점으로 삼되, 공중 다리까지 같은 선에 고정하지 않는다.

원형 보존은 원본 픽셀을 그대로 잘라 붙이라는 요구가 아니다. 후드·의상·장비의 디자인은 유지하고, 숙인 몸통의 옷 주름과 어깨·팔·허벅지의 연결은 새 자세에 맞춰 그린다.

## 1단계 · 전경 키포즈 생성용 공동 프롬프트

입력: `hero-v5-idle/frames/01.png`. 원본 이미지 URL은 총괄이 실제 API 요청 기록에 보존한다. 아래는 기획·아트 합의문이며, 실행 시 사용한 문장·시드·모델·작업ID는 실행 기록을 기준으로 삼는다.

```text
Use the reference as the canonical character design and edit it into ONE strong sprint key pose, side view facing screen RIGHT. Keep its oversized angular cream hood, black face opening and vertical amber eye, very short dark-navy torso and limbs, cream cape/tunic tails, golden waist bell, blocky boots, and thick short golden sword in the same hand. Preserve the recognizable proportions, clothing shapes, palette, and chunky pixel scale while redrawing the articulated pose.

The body drives forward from the hips: the torso leans about 30–35 degrees forward from vertical, with the shoulders clearly ahead of the pelvis. Keep the head low, looking forward and less tilted than the torso, with a short neck. This must read as determined low sprinting. One leg pushes backward against the ground while the opposite thigh and bent knee recover forward. Both bent elbows and upper arms participate: the sword-side arm is forward while its same-side leg pushes back, and the other arm is behind the body. Carry the sword low in a compact running arc, retaining its original blade length, thickness and attachment to the glove; no attack slash.

Redraw the torso, hip folds, shoulders and limbs as a coherent athletic pose. Do not rotate the entire standing sprite, keep a standing torso while swapping feet, lengthen the legs, change the hood or costume, freeze both arms behind the body, or invent armor. Preserve the source ground baseline and fit every pixel inside the 128×128 frame with a clear transparent border. Transparent background; one isolated pose; no dust, speed lines, motion blur, text, or scenery.
```

실행 기록: Pro Flash 키포즈 작업 `5087bbc1-7d08-49c2-aeac-b6b966a3561c`, seed1091, palette correction 사용. 실제 실행문에는 접지선 y≈114가 보조값으로 추가됐다. 총괄이 생성한 [키포즈 A](../Presentation/PixelLab/v9/sprint-keypose-a.png)를 기획 담당이 직접 열었다. v8보다 어깨·후드가 낮게 전진하고 반대 다리가 뒤로 밀려 **자세 방향에는 동의**했다. 그러나 이후 아트 검토에서 등 아래의 세 번째 손처럼 보이는 금색 덩어리와 찌그러진 허리종이 발견되어 **A를 애니메이션 입력으로는 반려했다.** 한 장의 자세 동의가 원형·해부 구조 통과를 대신하지 않는 사례다.

총괄은 이 두 곳만 수정한 [키포즈 B](../Presentation/PixelLab/v9/sprint-keypose-b.png), 작업 `65cfe1d2-1900-41f7-b83a-299151b3a9d5`, seed1092를 만들었다. 총괄을 통해 B의 아트 확인 완료를 전달받았다. B를 입력/끝 이미지로 한 MiniMax12장 작업 `c67e54a8-d19e-4769-a56c-e4e888b662d4`, seed1093이 접수됐다. 이 단락은 키포즈와 접수 이력이며, 이어진 전체 루프의 반려와 최종 직접 작성 후보는 아래에 따로 기록한다.

## 2단계 · 12장 질주 애니메이션 프롬프트

입력은 **1단계에서 통과한 전경 키포즈**다. Idle01을 다시 첫 프레임으로 넣지 않는다. 현재 방향·캐릭터 크기·골반 위치를 유지하고 한 주기 내내 기울기를 유지한다.

아래 긴 문장은 **설계 의도를 보존하는 공동 합의문**이다. 실제 MiniMax 호출에서는 `description` 최대1000자, `subject_description` 최대300자의 검증 제한이 확인됐다. 총괄은 동작 설명907자·인물 설명269자로 압축해 접수했다. 실제 사용한 문장·길이·시드·모델·입력은 [실행 요청 기록](../Presentation/PixelLab/v9/generation-requests.json)이 근거다. 긴 합의문 전체가 그대로 API에 전달됐다고 표시하지 않는다. API 한도는 이 도구 호출에서 확인한 값이며 다른 PixelLab 기능에 일괄 적용하지 않는다.

```text
Animate this approved forward-leaning key pose into one seamless 12-frame sprint cycle, side view facing screen RIGHT, running in place. This exact character keeps the same oversized angular cream hood, vertical amber eye in the dark face opening, short dark-navy body and limbs, cream cape/tunic tails, golden waist bell, blocky boots, and thick short golden sword attached to the same glove throughout.

Maintain a low 30–35 degree forward torso lean for the entire cycle. The shoulders remain ahead of the hips; the head looks forward and stays lower with only a tiny one-to-two-pixel vertical bob. Show strong driving effort through the pose, not hopping or rapid shaking. Keep the body compact without lengthening the neck, torso or legs.

Both upper arms swing from the shoulders, elbows bent, in opposite phases and opposite their same-side legs. Each hand travels clearly forward and backward past the body during the stride. The sword-carrying whole arm also pumps: carry the rigid sword low in a compact forward/backward arc, with continuous hand attachment and unchanged blade length and thickness. This is running, with no attack windup, slash, sword trail or face-crossing weapon swing. Do not freeze either arm behind the body or animate only the wrists.

Alternate the legs with a clear forward knee recovery, foot contact, support under the hip, backward push and recovery. Each foot contacts once per full loop; the second half exchanges the leg and arm roles of the first half. Keep the support foot moving backward consistently while grounded, with only a brief flight phase. Avoid simultaneous two-foot landings, backward knees, scissoring through one another, floating steps or bouncing.

Begin directly from the supplied sprint pose and remain in the run throughout. No idle/start/stop poses, no held standing frame, no duplicate endpoint pause. Frame 11 must flow into frame 0 as naturally as neighboring frames. Keep a fixed camera, fixed character scale, stable root and source ground baseline within the 128×128 canvas. Preserve the original palette, pixel clusters and clear transparent border. Transparent background, no particles, blur, speed lines, labels or scene. The final playback is one 480ms cycle; preserve expressive poses instead of increasing playback speed.
```

## 첫 연속 모션의 반려와 다음 실험

MiniMax A, 작업 `c67e54a8-d19e-4769-a56c-e4e888b662d4`, seed1093은 **반려**했다. 기획 담당도 [A 전체 시트](../Presentation/PixelLab/clips/hero-v9-sprint-a/contact.png)를 직접 확인했다.

- 03~04에서 칼날이 짧아지고05~08에서는 손 앞의 작은 금색 덩어리 정도로 줄어 원래의 두꺼운 검으로 읽히지 않는다. 몸 뒤로 자연스럽게 가려진 장면이 아니다.
- 검손이 대체로 몸 앞에 머물러, 요구한 앞뒤 왕복과 반대 팔 위상이 성립하지 않는다. 다리가 바뀌는 것만으로 전력질주를 충족하지 않는다.
- 허리의 종은 둥근 고리·장갑 같은 형태로 변한다. 이동 프레임마다 장식의 정체성이 바뀐다.

저장 폴더는 `00` 입력과 `01~12` 생성 출력의13장이다. 00/12는 비슷한 끝 자세지만 파일 바이트 해시는 달랐다. 픽셀 비교 없이 동일 이미지라고 확정하지 않는다. 이 후보는 형태 문제만으로 이미 반려됐으므로 끝 프레임을 제거하는 것으로 품질 문제를 해결했다고 보지 않는다.

총괄은 같은 키포즈 B를 입력/끝으로 사용하는 표준 `animate_image`12장, 작업 `24e1fe78-a558-4441-94c3-426ed2bc27dc`, seed1094를 다음 실험으로 접수했다. 이 B 루프도03~07 검 축소와08~09 재확대, 공격처럼 보이는 무기 변형으로 반려됐다. 반대 위상 키포즈 C, 작업 `465041bc-6b5c-409f-829a-279fa382ee00`, seed1095도 검을 든 손 변경·약한 팔다리 위상 교환·약150도 칼날 반전으로 반려됐다. 두 후속 결과의 판정은 총괄·아트 검토와 실행 JSON을 근거로 하며, 기획 담당이 직접 본 A 시트와 구분한다.

### 검토 이력: 두 극점을 고정한 반주기 제작안

시작과 끝에 같은 그림만 넣으면 한 주기 중간에서 검손이 몸 뒤로 가야 한다는 조건이 약하게 반영될 수 있다. 이에 **팔·발 위상이 반대인 키포즈 C**를 먼저 그려 두 극점을 고정하는 안을 검토했다. C 편집도 반려되어 이 반주기 보간은 최종 방식으로 채택하지 않았다. 아래는 검토 이력이며 제작 완료 기록이 아니다.

| 기준점 | 필요한 실제 자세 | 유지 사항 |
|---|---|---|
| 키포즈 B | 검팔 앞·같은 쪽 다리 뒤로 밀기, 반대팔 뒤·반대 무릎 앞으로 회수 | 현재 통과한 전경과 원형 |
| 키포즈 C | **같은 검팔**을 골반 뒤로 보내고 반대팔은 앞으로. 검팔과 같은 쪽 다리가 앞으로 접지할 위상 | 동쪽 시선·같은 손의 검·몸통 전경·후드·허리종·몸 크기 |
| 전반240ms | B→C 사이 접지·뒤로 밀기·무릎 회수를 보간 | 팔의 왕복 경로와 검 연결 |
| 후반240ms | C→B로 반대쪽 다리의 같은 과정을 보간 | 마지막→처음의 끊김 없음 |

C는 B를 좌우 반전한 그림이 아니다. 카메라·얼굴 방향·검을 든 손을 바꾸지 않고, 팔과 다리의 주기 위상만 바꾼다. 원래 프레임을 역순 재생하는 방법도 아니다. 발은 지지 중 뒤로 밀고 회수 중 앞으로 돌아와야 하므로 두 반주기를 각각 확인한다.

각 반주기의 목표는240ms다. 예를 들어 한 구간이 시작점+중간5장+끝점의7개 노드로 반환되면 두 구간을 합친 뒤 접합점 C와 루프 끝 B를 중복 표시하지 않아12개의 재생 자세가 된다. 실제 API가 반환한 구조를 먼저 확인하고 이 예시에 억지로 맞추지 않는다. 최종 고유 장수·프레임 시간·총480ms를 기록한다. 두 극점이 맞아도 중간 프레임에서 검이 줄거나 팔이 녹으면 통과하지 않는다.

## 3단계 · 검수에서 볼 순서

| 순서 | 실제로 확인할 것 | 통과 기록의 범위 |
|---|---|---|
| 키포즈 | Idle01과 나란히 같은 배율로 비교. 후드/옷/종/검/부츠 원형, 어깨–골반 전경, 앞뒤 팔·접힌 무릎 | 한 장의 디자인·자세 판단 |
| 전체12장 | 00~11 원본을 모두 보고 양팔 왕복, 양쪽 다리 접지와 회수, 검 경로, 직립 프레임 유무 확인 | 정지 시퀀스·이웃 프레임 관계 |
| 루프 | 480ms로 최소 여러 주기 재생. 11→00 끊김·머리 출렁임·통통 뛰기·팔 정지 여부 확인 | 실제 시간 재생 체감 |
| 이동 합성 | 주인공 화면 위치 고정, 바닥960px/2000ms, 새 적1860→900에서 확인 | 이 배경 속도에서의 접지·이동감 |
| 전환 | 분해 후 달리기 진입,2초 이동 후 정착·Stage 표시, 공격/회피로 넘어갈 때 자세 복귀 확인 | 기획 시안 상태 연결. Unity 검증과 구분 |

발 미끄러짐의 비교 단위도 속도와 함께 고정한다. 한480ms 주기 동안 바닥은230.4px 이동한다. 12장이면 장당40ms에19.2 화면px, 16장이면 장당30ms에14.4 화면px다. 접지발이 원본 좌표에서 뒤로 가야 하는 양은 이 값을 실제 표시 배율로 나눈다. 예를 들어 배율1.52075가 유지되는 화면이면12장 기준 약12.6 원본px/장이다. **표시 배율은 실제 보드 값을 확인한 뒤 사용한다.** 모델의 손발 좌표를 수식에 맞췄다는 이유만으로 실루엣이나 접지를 통과시키지 않는다.

위의30~35°·1~2px·480ms는 제작 목표이며 모델이 정확히 출력한다고 보장하는 값이 아니다. 검수에서 손과 검이 잠깐 몸 뒤에 가려지는 것은 이웃 프레임의 연속된 경로로 설명될 때만 허용한다. 검을 없앴다가 다른 위치에 새로 나타내는 것은 반려한다.

접지·팔 펌핑·원형이 불합격인 결과를 빠른 FPS, 프레임별 확대, 스프라이트 전체 회전, 장식 먼지로 숨기지 않는다. 같은 결함을 그대로 둔 채 후보명만 바꾸지 않는다. 최종 적용 후보를 정할 때 실제 파일ID·프레임 수·루프 시간·검토 범위를 함께 기록한다.

## 제작 방식 개정 · 전경 원형과 직접 설계한 관절 모션

총괄·아트 담당의 후속 결과를 전달받았다. MiniMax A에 이어 표준 모델 B와 반대 위상 키포즈 C 편집도 검·관절 형태가 목표를 지키지 못해 반려됐다. 기획 담당이 직접 본 시퀀스는 위 A이며, B/C의 구체 결과 판정은 총괄·아트 담당의 기록을 근거로 한다. 같은 전체 루프 생성을 계속 반복하는 대신 **검토를 통과한 키포즈 B의 인물 디자인을 고정하고, Workbench에서12개 자세의 관절 경로를 직접 작성하는 방식**으로 바꾼다.

이 변경은 자세·장비 조건을 완화하는 것이 아니다. 전체 동작을 한 번에 생성하면 제어되지 않았던 손과 검의 연결, 양팔 반대 위상, 무릎 방향을 개별적으로 작성하고 검수하는 제작 절차다. 앞의 긴 프롬프트는 유지할 동작 목표와 실패 원인 기록으로 남긴다. 두 극점 보간안도 검토 이력으로 보존하되, 현재 선택한 다음 제작 방식은 아래와 같다.

| 구성 | 새 제작 방식 | 확인해야 할 실패 |
|---|---|---|
| 몸통·후드 | 실제로 앞으로 숙여 그린 키포즈 B의 후드·크림 의상·얼굴·허리종을 원형으로 보존 | 직립 Idle을 통째로 회전한 몸으로 되돌아감, 목 늘어남, 원형을 프레임마다 확대 |
| 기존 팔다리 제거 | 새 팔·다리를 얹기 전에 B에 남은 원래 팔·손·발의 겹치는 흔적을 정리하고 몸의 가려진 부분을 복원 | 세 번째 손·두 겹 부츠·금색 잔상·비어 있는 어깨 |
| 양팔 | 상완과 하완의 길이·관절을 분리하고 손 목표점을 따라 팔꿈치가 접히는 경로를 직접 지정 | 손만 왕복하고 상완은 정지, 팔꿈치가 프레임 중 반대로 꺾임, 두 팔 동위상 |
| 검팔 | 같은 원본 손·손잡이·칼날을 한 덩어리로 연결하고 팔 전체의 목표점을 따라 이동 | 손에서 떨어지는 검, 프레임마다 검 길이 변경, 손목 회전만으로 펌핑을 흉내 냄 |
| 두 다리 | 허벅지와 정강이 길이·무릎 방향을 지정해 앞 무릎 회수와 뒤로 미는 지지 동작을 작성 | 무릎 반전, 발 패치만 평행 이동, 발이 허벅지를 통과 |
| 작은 반응 | 몸통 상하 약1px, 옷자락 약2px 반응을 출발값으로 사용 | 후드·허리종이 별도 물체처럼 흔들림, 전체 몸이 통통 튐 |
| 겹침 | 먼 팔/다리→몸통→가까운 팔/다리의 관계를 일관되게 유지. 허리종과 손을 구분 | 종이 주먹으로 보임, 가까운 검이 이유 없이 몸 뒤로 사라짐 |

관절 경로를 계산하는 방식 자체가 질주 품질을 보장하지 않는다. 같은 길이의 뼈가 연결됐어도 짧은 팔다리 캐릭터에서 포즈가 읽히지 않으면 반려한다. 어깨–팔꿈치–손과 골반–무릎–발의 꺾임 방향을 프레임 간 연속되게 두고, 한쪽에서 다른 쪽으로 갑자기 접히는 해를 선택하지 않는다. 손·부츠의 크기를 관절 목표점에 맞추기 위해 늘이거나 줄이지 않는다.

이전 v8과의 차이는 **출발 몸통 그림과 움직이는 부위**다. v8은 정지 Idle 부분을 약12도 기울이고 손·발 패치를 움직였지만, 이번에는 애초에 전경을 그린 B를 몸통 기준으로 삼고 상완/하완·허벅지/정강이를 함께 움직인다. 그래도 최종 그림이 다시 인형처럼 보이면 방식 이름과 무관하게 통과하지 않는다.

한 장씩 보기,12장 전체 보기,480ms 반복 보기,640px 화면에서 보기,960px/2000ms 배경과 함께 보기를 같은 반려 기준으로 실시한다. 몸통은 공통 기준점과 배율을 유지하며 개별 프레임의 그림 크기에 맞춰 다시 정규화하지 않는다. 실제 결과를 보기 전에는 팔 왕복·전경 유지·접지 통과로 기록하지 않는다.

## 최종 작성 후보 · hero-v9-sprint-authored

최종 Workbench 작업은 `a37e3f5e-26e9-40b5-a1ef-115fa3741489`다. [작성 레시피](../Presentation/PixelLab/v9/authored-run-recipe.json), [납품 레시피](../Presentation/PixelLab/clips/hero-v9-sprint-authored/recipe.json), [전체12장 시트](../Presentation/PixelLab/clips/hero-v9-sprint-authored/contact.png), [480ms GIF](../Presentation/PixelLab/clips/hero-v9-sprint-authored/animation.gif)를 함께 남긴다. 이 후보는 생성 A/B 루프를 고친 결과가 아니라, 통과한 키포즈 B의 후드·몸통·허리종과 같은 손·검의 그림을 기준으로 관절 자세를 직접 작성한 결과다.

| 항목 | 실제 기록 |
|---|---|
| 출력 | 128×128, 동쪽 측면, `frames/00.png~11.png`의12장 |
| 재생 | `p0~p11`, 각40ms, 총480ms. 바닥 이동960px/2000ms 유지 |
| 몸과 장비 | B의 후드·의상·허리종 그림을 보존. 같은 손·손잡이·검을 연결한 원본 조각 사용. 프레임별 크기 정규화 없음 |
| 움직임 | 양팔의 상완·하완, 두 다리의12개 위상을 작성. 몸통 상하 변화는 약1px |
| 이전 작성본 수정 | `75cf1cc9-ad20-46b0-a8c0-50929c57b623`에서 팔과 같은 쪽 다리가 같은 방향으로 움직이는 문제가 확인됨. `near_arm`·`far_arm`·`sword`·`glove`를 다리 주기 대비6프레임 이동해 반대 위상으로 수정 |
| 후드 마스크 수정 | 후드 조각에 남아 있던 기존 먼 팔의 크림색 부분을 제외해 고정된 옛 팔이 새 팔 위에 겹치는 문제를 수정 |
| 레시피 대조 | 작성본과 납품본의 자세·마스크·프레임 시간 데이터는 일치. 납품본에는 Workbench의 버전·백엔드·원본 레이어 경로 등8개 메타데이터 항목이 추가됨. 두 파일이 바이트 단위로 동일하다는 뜻은 아님 |

기획 담당은 최종 시트를 직접 열어12장에 검과 허리종이 남아 있고 검팔의 위치가 앞뒤로 바뀌는 것을 확인했다. 이전 A의03~08에서 보였던 칼날 축소·소실과는 구분된다. 이 관찰은 정지 시퀀스에 한정한다. 총괄·아트 담당은 오프라인 시트와480ms GIF로 연속감을 검토 중이며, 이 문서에서는 실시간 브라우저 재생·배경 합성·Unity 검증이 끝났다고 기록하지 않는다. 최종 채택 여부는 위 반려 기준을 그대로 적용한다.

## 담당 간 합의와 남은 일

- 기획 담당: 강한 전경과 양팔의 실제 펌핑을 요구한다. 480ms/2초 이동 유지에 동의한다.
- 아트 담당 피드백 반영: 머리는 몸보다 덜 기울이고 전방을 본다. 상완 자체가 반대 위상으로 움직이며, 허벅지 회수·뒤로 미는 지지 동작을 새로 그린다. 전체 회전·직립 몸통+발 패치 방식은 제외한다. 프롬프트의 좌표 과잉 지시를 줄이고 디자인 정체성을 기준으로 삼는다.
- 총괄 담당: 합의문을 바탕으로 키포즈를 생성한 뒤 결과를 공유한다. 실제 실행 프롬프트·모델·작업ID·시드·원본/결과를 보존한다.
- 현재 상태: 키포즈 A는 해부·종 형태 문제로 반려, 키포즈 B의 국소 수정은 아트 확인 완료. 전체 루프 A/B와 반대 위상 C 편집은 반려됐다. B 원형을 사용한 Workbench 관절12자세 작성과 수정본 출력까지 끝났으며, 최종 후보 ID는 `hero-v9-sprint-authored`다. 실제 시도·출력·반려 상태는 위 실행 JSON에서 관리한다.
- 아직 남은 것: 원형·팔/다리 경로·검 연결에 대한 최종 아트 판정,480ms 연속 재생·배경 합성 검토, 최종 후보의 기획서 반영. 정지 시트·오프라인 GIF 검토를 실시간 브라우저 검증 완료로 표현하지 않는다. Unity 파일은 이 작업의 범위 밖이다.
