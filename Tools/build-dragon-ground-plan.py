"""Document each floor's contact contract from the current shared ground constant."""
from pathlib import Path
import hashlib
import html
import json
import re

ROOT = Path(__file__).resolve().parents[1]
PLAN = ROOT / 'Planning/Story/Dragon-v1'
code = ROOT / 'Client/Assets/Game/Runtime/Presentation/ParallaxWorld.cs'
source = code.read_text(encoding='utf-8-sig')
plan_y = float(re.search(r'GroundPlanY\s*=\s*([\d.]+)', source)[1])
story = json.loads((PLAN / 'story.json').read_text(encoding='utf-8'))
screen_y = 360 - ((360 - plan_y) * (6.2 / 720) + 1.04) / (6.2 / 720)
specs = [
    ('바람들판', '다져진 황토길과 짧은 풀 가장자리', '#D5A15B', ['마을 밖 흙길','풀잎 둔덕 옆 흙길','돌울타리 길','자갈 섞인 흙길','넓은 토끼 공터','낮은 숲 가장자리','물웅덩이 옆 마른 길','돌무더기 옆 흙길','멧돼지 오솔길','큰엄니 앞 넓은 공터']),
    ('솔바람숲', '단단한 숲 흙길과 이끼 가장자리', '#A69F62', ['통행료 표지판 앞 흙길','작은 돌이 흩어진 흙길','버섯 군락 옆 마른 길','나무다리 앞 흙길','숲송이 공터','야영지로 가는 흙길','포자 군락 옆 흙길','이끼 군락 옆 흙길','울타리 앞 흙길','기블의 통행료 공터']),
    ('햇돌광산', '평평하게 다진 회갈색 돌바닥', '#A09B89', ['갱도 입구 돌길','목재 지지대 앞 돌길','낮은 수정 군락 돌길','수레길 옆 돌바닥','수정 경비 공터','빛구멍 아래 돌바닥','광석 더미 옆 돌길','옛 작업대 앞 돌길','경비문 앞 돌바닥','돌문의 넓은 경비 바닥']),
    ('구름고개', '밝은 회색 바위길과 짧은 고산 풀', '#A4AAA4', ['광산 출구 바위길','바람 부는 능선길','낮은 구름 아래 바위길','평평한 고개길','오거 앞 넓은 바위','풍화된 바위길','작은 풀밭 옆 바위길','날개 자국이 있는 바위','와이번 둥지 앞 바위길','와이번의 넓은 바위 공터']),
    ('붉은바위둥지', '크림빛 암반과 붉은 지층 단면', '#BE8F79', ['따뜻한 바위길','황금 풀 옆 암반','얕은 김 옆 마른 암반','큰 발자국 옆 암반','불깃 와이번 바위턱','붉은 지층 앞 암반','그을린 표지 옆 암반','용 비늘이 떨어진 암반','둥지 앞 넓은 암반','브라크의 평평한 결전 암반']),
]
rows, regions = [], []
for i, (name, surface, color, motifs) in enumerate(specs):
    regions.append(dict(name=name, firstStage=i*10+1, lastStage=i*10+10, surface=surface,
                        planGroundY=plan_y, swatch=color,
                        artStatus='기존 초원 바닥 적용' if i==0 else '기존 바닥의 숲 색조 적용; 전용 숲 배경은 후속' if i==1 else '기획 · 전용 지형 아트 미구현'))
    for j, motif in enumerate(motifs):
        stage=i*10+j+1
        archetype=story['stages'][stage-1]['archetype']
        hover=72 if archetype=='bat' else 48 if archetype=='wyvern' else 0
        rows.append(dict(stage=stage, enemy=story['stages'][stage-1]['name'], zone=name, surface=surface,
                         groundDetail=motif, planGroundY=plan_y, activeFootSpan=[140,1140],
                         idleMode='planned-hover' if hover else 'grounded', plannedHoverPixels720=hover,
                         collisionShape='flat; no slopes, ledges or ground hazards',
                         implemented=stage<=20, detailArtImplemented=False,
                         detailNote='소재/장식 계획. 개별 스테이지 전용 바닥 그림 완성 의미 아님'))
result=dict(version=1, date='2026-10-24', coordinateSystem='1280x720 plan, top-left; world camera applies vertical framing',
            groundApi='ParallaxWorld.GroundForStage / GroundPoint', planGroundY=plan_y,
            worldGroundY=(360-plan_y)*(6.2/720), nominalScreenY720=screen_y,
            rasterNote='기본 전투 카메라(-1.04), orthographicSize3.1 기준이며 보스 줌·흔들림은 제외. 실제 렌더에서 바닥/발의 경계와 대조하며 계획좌표590을 화면590으로 오해하지 않는다.',
            rules=['풀 끝과 장식 높이는 접지면이 아니다. 단단한 바닥의 윗경계가 접지 기준이다.',
                   '배경/바닥/양쪽 액터는 같은 GroundPoint 기준을 사용한다.',
                   'Idle·준비·회복은 지지발이 기준선에 닿는다. Run·도약의 의도적 공중 프레임은 별도 기록한다.',
                   '후속 공중형은 지지발 대신 지면+부유높이를 사용한다. 박쥐72px·와이번48px는720p 초기계획이며 착지공격에서는0으로복귀한다. 아직런타임미구현이다.',
                   '프레임마다 bbox 하단을 기준선으로 끌어내리지 않는다. 원본 클립 피벗과 의도한 높이 변화를 유지한다.',
                   '적 크기는 기준720p에서 정수 픽셀 배율로 정한다. 다른 해상도에서는 화면 비율로 축소된다. 보스도 발 피벗을 기준으로 위쪽으로 커진다.',
                   '이동 중 플레이어 기본 X는 고정한다. 지면은1배, 원경은0.08배로 흐른다.',
                   '전투 영역에는 실제 경사·발판·낭떠러지·지형 피해를 추가하지 않는다.'],
            regions=regions, stages=rows, sourceSha256=hashlib.sha256(code.read_bytes()).hexdigest())
(PLAN/'stage-ground-v1.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
e=html.escape
region_rows=''.join(f'<tr><td>{r["firstStage"]}–{r["lastStage"]}</td><td><span style="display:inline-block;width:18px;height:18px;background:{r["swatch"]};border:2px solid var(--ink)"></span> {e(r["name"])}</td><td>{e(r["surface"])}</td><td>{e(r["artStatus"])}</td></tr>' for r in regions)
stage_rows=''.join(f'<tr><td>Stage {r["stage"]}</td><td>{e(r["enemy"])}</td><td>{e(r["groundDetail"])}</td><td>{plan_y:g} · 평면'+(f'<br>부유 {r["plannedHoverPixels720"]}px 계획' if r['plannedHoverPixels720'] else '')+f'</td><td>{"실행 구간" if r["implemented"] else "후속 계획"}</td></tr>' for r in rows)
page=f'''<section id="stage-ground"><p class="eyebrow">GROUND · MOTION CONTACT</p>
<h2>지역은 달라도,<br>발을 딛는 선은 분명하게.</h2>
<p class="lead">풀 끝에 떠 있거나 바닥에 파묻히지 않도록, 지면과 양쪽 캐릭터가 하나의 접지 기준을 사용합니다. 보스도 발을 기준으로 위쪽으로 커집니다.</p>
<figure class="wide"><svg viewBox="0 0 1280 590" role="img" aria-label="플레이어와 몬스터의 공통 접지선과 점프 궤적 설계도" style="width:100%;background:#e4ecda;shape-rendering:crispEdges">
<rect x="0" y="430" width="1280" height="160" fill="#c59b64"/><path d="M0 430H1280" stroke="#273d45" stroke-width="8"/>
<path d="M0 445H1280M0 470H1280" stroke="#b17e4d" stroke-width="8"/>
<g fill="#273d45"><path d="M300 235h75v65h-75zM315 300h50v80h-50zM305 380h24v50h-24zM350 380h24v50h-24z"/><path d="M850 230h80v60h-80zM835 290h110v85h-110zM845 375h28v55h-28zM909 375h28v55h-28z"/></g>
<path d="M340 425V180M890 425V180" stroke="#d56243" stroke-width="3" stroke-dasharray="9 9"/><path d="M410 370Q530 140 650 370" fill="none" stroke="#315e4d" stroke-width="6" stroke-dasharray="12 12"/>
<g font-family="Malgun Gothic,sans-serif" fill="#273d45"><text x="52" y="68" font-size="32">접지 설계도 · 실제 게임 캡처 아님</text><text x="265" y="170" font-size="26">플레이어 발 피벗</text><text x="815" y="170" font-size="26">적 발 피벗</text><text x="480" y="250" font-size="24">도약은 의도한 높이 유지</text><text x="52" y="530" font-size="27">공통 계획 Y = {plan_y:g} · 현재 720p 화면 Y ≈ {screen_y:.2f}</text></g></svg>
<figcaption>그림은 접지 원리를 설명합니다. 화면Y는 기본 전투 카메라 기준이며 보스 줌·흔들림은 제외합니다. 잔디 끝·검끝·망토 끝을 발 위치로 오인하지 않습니다. 후속 박쥐·와이번은 명시된 부유 높이를 사용합니다.</figcaption></figure>
<div class="scroll"><table><thead><tr><th>스테이지</th><th>지역</th><th>바닥 정의</th><th>아트 적용 범위</th></tr></thead><tbody>{region_rows}</tbody></table></div>
<div class="grid3" style="margin-top:24px"><article class="card"><h3>정지·공격·회복</h3><p>클립의 발 피벗과 공통 지면을 맞춥니다. 지지발이 버티고, 손과 무기가 연결된 채 준비→접촉→복귀가 읽혀야 합니다.</p></article><article class="card"><h3>달리기·회피</h3><p>접지와 공중 자세를 구분합니다. 프레임마다 발을 바닥으로 끌어내리지 않습니다. 플레이어 X 고정, 지면·원경 이동으로 전진감을 줍니다.</p></article><article class="card"><h3>보스·효과</h3><p>보스 크기와 얼굴 포효 기준을 따로 관리합니다. 바닥에서 나오는 가시도 같은 접지선에서 시작하며, 그림 장식이 판정을 바꾸지 않습니다.</p></article></div>
<details style="margin-top:24px"><summary>Stage 1–50 바닥·접지 명세 전체 보기</summary><p class="small">1–20은 현재 실행 범위,21–50은 후속 계획입니다. 개별 바닥 장식은 제작 지시이며50장의 별도 배경이 완성됐다는 뜻이 아닙니다.</p><div class="scroll"><table><thead><tr><th>스테이지</th><th>적</th><th>바닥 장식 계획</th><th>접지 기준</th><th>진행</th></tr></thead><tbody>{stage_rows}</tbody></table></div></details>
<p><a href="stage-ground-v1.json">50스테이지 접지 수치·제작 범위</a> · <a href="Production/Player-v3/style-grounding-review.md">플레이어 화풍·모션 검토</a></p></section>'''
(PLAN/'stage-ground-section.html').write_text(page,encoding='utf-8')
print(f'Documented50 floors: planY={plan_y}, screenY720={screen_y:.3f}; actual content20 stages.')
