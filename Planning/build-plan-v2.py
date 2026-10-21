"""Compose the reviewed planning page from the v2 decision record."""
import json
import re
from html import escape
from pathlib import Path

ROOT = Path(__file__).parent
d = json.loads((ROOT / "decisions.json").read_text(encoding="utf-8"))
assert d["version"] == "planning-v2"
c, g, e = d["combat"], d["growth"], d["economy"]

def table(headers, rows):
    return '<div class="table-wrap"><table><thead><tr>' + ''.join(f'<th>{h}</th>' for h in headers) + '</tr></thead><tbody>' + ''.join('<tr>' + ''.join(f'<td>{v}</td>' for v in row) + '</tr>' for row in rows) + '</tbody></table></div>'

def section(id, number, title, body):
    return f'<section id="{id}"><div class="section-top"><span class="section-num">{number:02}</span><h2>{title}</h2></div>{body}</section>'

old = (ROOT / "index-v1.html").read_text(encoding="utf-8")
revenue = re.search(r'<section id="revenue">.*?</section>', old, re.S).group()
revenue = revenue.replace('<span class="section-num">09</span>', '<span class="section-num">10</span>')
revenue = revenue.replace('<h2>과금과 30일 매출 가정</h2>', '<h2>과금과 30일 수익 민감도</h2><p>성장·유물 추가가 매출 상승을 입증하지는 않는다. 기존 입력을 유지한 산술 참고이며 v2의 예측값이 아니다. 새벽석 현금 판매 수익은 0으로 계산한다.</p>')
revenue = revenue.replace('결과에서 장식 조각 +5 / 하루 최대 3회', e["adDisplay"])
revenue = revenue.replace('기본 보상과 즉시 재도전은 유지. 전투력과 무관', '기본 보상과 즉시 재도전 유지. 성장 속도에 영향을 주므로 무광고 진행을 별도 검증')
revenue = revenue.replace('수집 완료 뒤 광고 참여가 줄어드는 민감도도 확인한다.', '성장·수집 완료 뒤 광고 참여가 줄어드는 민감도도 확인한다.')
revenue = revenue.replace('수집 완료 전·후의 활동일과 광고 실노출', '성장·수집 완료 전·후의 활동일과 광고 실노출')
revenue = revenue.replace('Reviews/market.md', 'Reviews/market-v2.md')
revenue = re.sub(r'  <div class="callout"><b>외형 재화의 초기 밸런스:.*?</div>\s*<p class="mini">광고 없이 10번 완주하면.*?</p>', '<p class="mini">두 능력치 성장과 유물 슬롯은 무료 전투 보상으로 진행할 수 있다. 광고는 새벽석을 추가 지급해 성장 속도에 영향을 준다. 성장·슬롯의 유한한 소비처가 소진된 이용자에게는 재화 광고를 숨기므로 이후 노출을 계속 가정하지 않는다.</p>', revenue, flags=re.S)
architecture = re.search(r'<section id="architecture">.*?</section>', old, re.S).group()
architecture = architecture.replace('<span class="section-num">10</span>', '<span class="section-num">11</span>')
architecture = architecture.replace('</section>', '<p><b>v2 데이터 추가:</b> Stage의 종류·체크포인트·드랍, Dodge의 방향·판정 시각, Stagger의 상태·면역, StatUpgrade의 레벨·돌파·비용, Relic의 효과·중복·장착 조건, Currency의 획득·교환을 원본 데이터로 관리한다. 저장에는 성장 레벨·유물 보유/장착·슬롯 수·최초 보상 마스크·처리 거래 ID를 분리한다. 전투 중 성장·장착은 바꾸지 않고 출발 시 스냅샷을 만든다.</p></section>')

head = '''<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>마지막 종지기 · 가로 액션 기획 v2</title><link rel="stylesheet" href="style.css"></head><body>
<header class="wrap" id="top"><div class="hero-top"><div><p class="eyebrow">GAME DESIGN v2 / 2026.10.21</p>
<h1>마지막 종지기</h1><p class="pitch">방향을 읽고, 연속해서 피하고,<br>종을 울려 적의 자세를 무너뜨린다.</p>
<div class="tags"><span class="tag">가로 화면</span><span class="tag">위 · 아래 · 뒤 회피</span><span class="tag">성장 + 유물</span><span class="tag">원스토어 선출시</span></div></div>
<div class="status"><b>사용자 피드백 반영 · 기획 v2</b><span id="review-status">__REVIEW_STATUS__</span><p class="mini" style="margin:12px 0 0">이 페이지는 기획과 배치 시안이다. Unity 게임 구현·실기기 조작 검증은 아직 진행하지 않았다.</p></div></div>
<img class="hero-art" src="Visuals/landscape-battle-v2.svg" alt="가로 전투 시안. 낮은 지면, 중앙 캐릭터, 왼쪽 세 방향 회피와 오른쪽 공격 버튼">
<p class="caption">가로 전투 배치 시안 · 지면은 화면 높이 약 82%. 새 도트 형태를 SVG로 그린 배치 설명이며 실제 게임 캡처나 최종 스프라이트가 아니다.</p></header>
<nav class="nav" aria-label="기획서 목차"><div class="wrap"><a href="#decision">수정 요약</a><a href="#ui">화면</a><a href="#combat">회피·기절</a><a href="#growth">육성</a><a href="#stages">스테이지</a><a href="#relics">유물·재화</a><a href="#story">서사</a><a href="#resources">리소스</a><a href="#mvp">검증 전략</a><a href="#revenue">수익</a><a href="#review">검토</a></div></nav><main class="wrap">'''

decision = section("decision", 1, "핵심을 생략했던 이전 안을 바꾼다", table(
    ["항목", "v1", "v2 기준"],
    [
        ["화면", "세로형", "가로형, 지면을 화면 높이 약 82%로 낮춤"],
        ["회피", "한 버튼·700ms 재사용", f"위/아래/뒤 3방향, 입력 시작 간 최소 {c['dodgeReuseMs']}ms"],
        ["숙련 보상", "완벽회피의 울림 1회 충전", "일반·완벽 회피로 기절 누적 → 행동불능 → 공격 기회"],
        ["육성", "영구 전투력 성장 없음", "골드로 능력치 성장, Lv.10·20·30 돌파"],
        ["진행", "12전 반복 런", "30스테이지 캠페인, 5·15·25 중간 보스 / 10·20·30 보스"],
        ["전리품", "외형 수집", "보스 처치 유물, 장착 조합, 새벽석으로 슬롯·골드 교환"],
    ]
) + '''<div class="callout"><b>한 버튼도 타이밍 게임을 만들 수는 있다.</b> 다만 판정창만 좁히면 선택의 재미보다 입력 지연·반복 피로가 먼저 커질 수 있다. 이번 주력안은 방향과 연타 리듬을 함께 읽는 3방향 회피다. 1버튼은 같은 적을 사용하는 짧은 비교 시험에만 남겨 실제 재도전 의욕을 비교한다.</div>
<p class="mini">원작은 커뮤니티 문서에서 위·아래·뒤 회피를, 플레이 리뷰에서 타이밍에 따른 반격 게이지 보너스·5/10단위 보스·성장·유물을 확인했다. 원작의 정확한 회피 재사용 시간과 10레벨 배율은 확인하지 못했다. <b>기존 0.7초는 원작을 측정한 값이 아니라 v1 제안이었다.</b> 아래 수치도 신작의 검증 전 초기값이다. <a href="Evidence/reference-research-v2.json">확인한 사실과 한계</a></p>''')

ui = section("ui", 2, "낮은 지면, 가운데 전장, 양옆의 손", '''<p>주인공과 적은 중앙에 두고 발이 닿는 선을 낮춘다. 왼쪽 엄지는 위·뒤·아래 회피, 오른쪽 엄지는 공격을 담당한다. HUD는 상단에 두며 기절 게이지는 적 HP 아래 하나만 표시한다. 기절 중에는 같은 위치가 남은 시간으로 바뀐다.</p>
<p><a class="button-link" href="Visuals/wireframes-v2.html">가로 화면·상태·비교안 보기</a> <a class="button-link secondary" href="Reviews/ui-v2.md">배치 명세</a></p>
<figure class="wide-figure"><img src="Visuals/wireframes-v2.svg" alt="가로형 로비, 전투, 결과, 성장, 유물, 재화 교환 화면"><figcaption>960×540 / 1200×540 / 640×360과 안전 영역을 검토한다. 문서상의 논리 픽셀이 실제 기기의 dp와 같다는 뜻은 아니다.</figcaption></figure>
<p class="mini">화면 안의 문구는 행동·필요 상태·가격·회복 안내로 제한한다. “전략적인 성장 경험” 같은 기획 설명은 게임에 넣지 않는다. 구체 hit area·anchor·가림 위험은 UI 담당 명세를 따른다.</p>''')

combat_rows = [
    ["방향", "위=뛰기 / 아래=숙이기 / 뒤=물러나기", "자유 이동이 아닌 짧은 회피 후 제자리 복귀. 방향 오류는 타이밍이 맞아도 피격"],
    ["일반 회피", c["normalWindowDisplay"], f"무피해, 기절 누적 +{c['normalStagger']}"],
    ["완벽 회피", c["perfectWindowDisplay"], f"무피해, 기절 누적 +{c['perfectStagger']}"],
    ["다음 회피", f"유효 {c['dodgeActiveMs']}ms + 회복 {c['dodgeRecoveryMs']}ms = {c['dodgeReuseMs']}ms", "새 입력이 필요. 길게 누르기·회복 중 연타로 판정창 연장 불가"],
    ["연속타 간격", c["comboGapDisplay"], "입력 가능 시각과 새 방향을 볼 시간을 함께 확보"],
    ["기절", f"{c['staggerThreshold']} 누적에서 발동", c["stunRuleDisplay"]],
    ["공격", f"기본 피해 {c['attackDamage']}, 간격 {c['attackIntervalMs']}ms", c["attackRuleDisplay"]],
]
combat = section("combat", 3, "두 회피 등급을 기절과 공격 기회로 연결한다", table(["규칙", "초기값", "플레이 의미"], combat_rows) + f'''
<div class="loop"><div class="step"><span class="number">READ</span><b>방향·타격을 읽기</b><span>자세와 짧은 전조로 안전한 회피를 구분한다.</span></div><div class="step"><span class="number">DODGE</span><b>일반 / 완벽</b><span>둘 다 피할 수 있다. 완벽회피가 기절을 더 빨리 만든다.</span></div><div class="step"><span class="number">BREAK</span><b>{c['staggerThreshold']}에서 기절</b><span>적의 공격을 끊고 잠깐의 공격 기회를 연다.</span></div><div class="step"><span class="number">ATTACK</span><b>빈틈에 공격</b><span>기절 밖에서도 패턴의 빈틈을 읽어 공격한다.</span></div></div>
<p style="margin-top:22px">{c['timingExampleDisplay']}</p><p>{c['antiLockDisplay']}</p>
<p class="mini">기절 게이지는 해당 적 전투만의 상태다. 이전의 울림 1회·마지막 타격 뒤 800ms 강화창은 폐기한다. 성장·유물이 회피 방향을 대신 맞히거나 판정창·재사용 시간을 바꾸지는 않는다. 일시정지는 같은 전투 시계를 멈추고, 중단 복귀 시 안전한 예고부터 다시 시작한다.</p>
<p><a href="Reviews/combat-v2.md">입력 우선순위·동시 판정·기절 전이·연타 검산</a></p>''')

growth = section("growth", 4, "매 레벨은 조금, 열 레벨마다 크게", f'''<p>{g['summaryDisplay']}</p>''' + table(["레벨", "공격력", "최대 체력", "성장 의미"], g["displayRows"]) + f'''
<p class="formula">{g['formulaDisplay']}</p>
<p>{g['purchaseDisplay']}</p><div class="callout">{g['balanceDisplay']}</div>''')

stage_rows = []
for zone in range(3):
    cells = []
    for s in range(zone*10+1, zone*10+11):
        kind = "보스" if s % 10 == 0 else "중간" if s % 10 == 5 else "일반"
        css = "boss" if s % 10 == 0 else "mid" if s % 10 == 5 else ""
        cells.append(f'<div class="stage-cell {css}"><b>{s}</b><small>{kind}</small></div>')
    stage_rows.append(f'<div class="panel"><h3>{["성문","사슬 회랑","종루"][zone]} · {zone+1}구역</h3><div class="stage-track">{"".join(cells)}</div></div>')
stages = section("stages", 5, "다섯 전투마다 보상, 열 번째에 종을 깨운다", '<div class="stage-zones">' + ''.join(stage_rows) + '</div>' + f'''
<p style="margin-top:22px">{d['content']['progressionDisplay']}</p>
{table(["구간", "적 체력·피해 기준", "성장·플레이 가정", "시간 해석"], d["content"]["balanceRows"])}
<p class="mini">{d['content']['timeLimitDisplay']} v1의 216초·12전 계산은 새 캠페인에 적용하지 않는다. 적 종류와 스테이지 수는 다르며 30개의 독립 캐릭터를 제작하지 않는다.</p>''')

relics = section("relics", 6, "보스 유물을 얻고, 장착 조합을 고른다", f'''
<p>{d['relics']['dropDisplay']}</p>
{table(["유물", "효과 초안", "제약"], d["relics"]["displayRows"])}
<div class="grid2"><div class="panel"><h3>보관과 장착을 구분</h3><p>{d['relics']['inventoryDisplay']}</p></div><div class="panel"><h3>새벽석 → 선택</h3><p>{e['gemSpendDisplay']}</p></div></div>
{table(["획득·사용", "기준", "조건"], e["displayRows"])}
<div class="callout">{e['freeRouteDisplay']}</div><p>{e['currencyPolicyDisplay']}</p>
<p class="mini">새벽석은 다이아에 해당하는 가칭 인게임 재화다. 초기에는 현금 직판을 보류한다. 향후 판매하면 성장 속도와 장착 성능에 영향을 주는 상품이므로 가격·무과금 진행·환불·기록 비교를 다시 검토해야 한다.</p>
<p><a href="Reviews/market-v2.md">획득량·강화비·슬롯 확장·교환·광고 경제 검산</a></p>''')

story = section("story", 7, "도입은 짧게, 성장은 종탑을 오르는 이유로", '''<div class="grid3">
<div class="story-line"><small>0–3초</small><p>“밤이 끝나지 않았다.”</p></div><div class="story-line"><small>3–6초</small><p>“새벽을 깨우던 종도<br>잠들었다.”</p></div><div class="story-line"><small>6–9초</small><p>“남은 불씨 하나.<br>종을 깨워라.”</p></div></div>
<p>유물은 이전 종지기들이 남긴 도구다. 10·20·30 보스 뒤 종을 깨우고 해당 구역에 빛이 돌아온다. 긴 대사 대신 종소리·배경 변화·짧은 한 줄로 진행을 보여준다. 장비·체력 성장은 다음 구역을 버티는 준비이며 방향과 타이밍을 배우는 역할을 대신하지 않는다.</p>
<p class="mini">9초 오프닝은 최초 1회·건너뛰기 가능. 30스테이지 첫 완주 기록 확정 뒤 6초 엔딩, 이후 설정에서 무료 재생. 재도전 시 자동 서사 없음. 기존 배경·캐릭터를 재사용한다.</p>''')

resources = section("resources", 8, "가로·성장·유물에 맞춰 리소스를 다시 센다", table(["영역", "첫 검증", "30스테이지 후보"], d["resources"]["displayRows"]) + f'''
<p>{d['resources']['estimateDisplay']}</p><p class="mini">주인공 16×24px 원본을 출발점으로, 일반24~32px·보스48~64px와 4색 기반 팔레트를 유지한다. 방향별 회피·기절 자세를 식별할 수 있어야 한다. UI 한글은 도트 장식보다 가독성을 우선한다.</p>''')

mvp = section("mvp", 9, "성장 수치로 조작 문제를 가리지 않는다", f'''
<div class="grid3"><div class="panel"><h3>1 · 회피 비교</h3><p>동일한 적·피해·예고로 1버튼과 3방향을 비교한다. 순서를 바꿔 시험하고 재도전 의욕·방향 오류·회복 중 입력 실패를 기록한다.</p></div>
<div class="panel"><h3>2 · 10스테이지</h3><p>5중간보스·10보스, 기절, Lv.10 성장, 유물과 슬롯·골드 교환을 연결한다. 무료 진행 비용과 반복 파밍 시간을 관찰한다.</p></div>
<div class="panel"><h3>3 · 30스테이지</h3><p>방향 회피가 이해되고 성장 없이도 실패 이유를 납득한 뒤 확장한다. 20·30 돌파와 모든 유물 조합, 원스토어 결제·광고·복구를 검증한다.</p></div></div>
<p style="margin-top:22px">{d['validation']['display']}</p>
<p>시장 방향은 직접 조작과 성장을 함께 원하는 이용자로 넓어진다. 동시에 방향 입력 피로, 반복 파밍, 유물 조합 밸런스, 더 큰 제작비가 새 위험이다. 글로벌 장르 트렌드만으로 원스토어 수요나 매출을 확정하지 않는다. <a href="https://sensortower.com/blog/h1-2026-digital-gaming-market-index">시장 자료</a></p>
<p class="mini">원스토어 판매자·등급·SDK·결제/복원·광고·개인정보·검수 준비는 유지한다. 베타게임존의 무료 조건·결제 테스트 제한은 별도 확인한다. 기존 검증 링크는 <a href="Reviews/market.md">v1 시장 근거</a>에 보존했다.</p>''')

review = section("review", 12, "담당별 판단과 변경 이력", table(["담당", "이번 검토", "문서"], [
    ["기획", "방향 회피·진행·체크포인트·스토리·시험 범위", '<a href="Reviews/concept-v2.md">기획 v2</a>'],
    ["전투", "회피 시계·기절·성장·30전 수치·유물·공수", '<a href="Reviews/combat-v2.md">전투 v2</a>'],
    ["UI", "가로·지면·손 가림·3방향·성장·유물·교환", '<a href="Reviews/ui-v2.md">UI v2</a>'],
    ["시장·경제", "무료 획득·강화·슬롯·교환·광고·수익 한계", '<a href="Reviews/market-v2.md">경제 v2</a>'],
]) + '''<p>__REVIEW_STATUS__ 기획 검토는 재미·상업성·실기기 검증 완료를 뜻하지 않는다. <a href="decisions.json">현재 결정 기록</a> · <a href="index-v1.html">이전 기획 v1</a></p>
<p class="mini"><a href="Evidence/document-render-check.json">문서 렌더 검사</a> · <a href="Evidence/planning-verification.json">경제·수치·파일 검사</a>. 실행 시점과 버전을 확인할 수 있다. 실제 게임의 입력 지연이나 클리어 시간을 측정한 자료는 아니다.</p>
<ul class="sources"><li><a href="https://timing-hero.fandom.com/wiki/General_Gameplay">Timing Hero 커뮤니티 조작 문서</a> — 방향 회피, 검색 색인 확인.</li><li><a href="https://crashinggamenight.com/blog/mobile-game-monday-timing-hero">2021년 Timing Hero 플레이 리뷰</a> — 반격 보너스·5/10보스·성장·유물, 과거 플레이 기록.</li><li><a href="https://enhawiki.kr/wiki/%EC%9A%A9%EC%82%AC%EB%8A%94_%ED%83%80%EC%9D%B4%EB%B0%8D">리브레 위키 미러</a> — 저스트 회피·유물, 검색 색인. 공식 수치 명세로 취급하지 않음.</li></ul>''')

page = head + ''.join([decision, ui, combat, growth, stages, relics, story, resources, mvp, revenue, architecture, review]) + '''</main><footer class="wrap">기준일 2026-10-21 · planning-v2 · 총괄: 게임 제작 스킬과 기획 수립<br>기획·배치 시안이며 게임 빌드·플레이테스트·출시 완료가 아니다. <a href="#top">처음으로</a></footer><script src="plan.js"></script></body></html>'''
page = page.replace("__REVIEW_STATUS__", escape(d["reviewDisplay"]))
assert "__REVIEW_STATUS__" not in page
(ROOT / "index.html").write_text(page, encoding="utf-8")
print("Built Planning/index.html from planning-v2 decisions")
