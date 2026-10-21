"""Build the current visual planning document. Gameplay is not implemented here."""
import json
from html import escape
from pathlib import Path

ROOT = Path(__file__).parent
d = json.loads((ROOT / "decisions.json").read_text(encoding="utf-8"))
assert d["version"] == "planning-v3"

def table(headers, rows):
    return '<div class="table-wrap"><table><thead><tr>' + "".join(f"<th>{escape(str(v))}</th>" for v in headers) + "</tr></thead><tbody>" + "".join("<tr>" + "".join(f"<td>{escape(str(v))}</td>" for v in row) + "</tr>" for row in rows) + "</tbody></table></div>"

def section(id, number, title, body):
    return f'<section id="{id}"><div class="section-top"><span class="section-num">{number:02}</span><h2>{title}</h2></div>{body}</section>'

def paragraphs(lines):
    return "".join(f"<p>{escape(s)}</p>" for s in lines)

def figure(file, caption):
    return f'<figure class="wide-figure"><img src="Visuals/{file}" alt="{escape(caption)}"><figcaption>{escape(caption)}</figcaption></figure>'

g, c, e, p = d["growth"], d["combat"], d["economy"], d["progression"]
status = escape(d["reviewDisplay"])
head = f'''<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>마지막 종지기 · 기획 v3</title><link rel="stylesheet" href="style.css"></head><body>
<header class="wrap" id="top"><div class="hero-top"><div><p class="eyebrow">GAME DESIGN v3 / 2026.10.21</p>
<h1>마지막 종지기</h1><p class="pitch">위험을 보고 피한다.<br>열 개의 종을 채워 다음 힘을 깨운다.</p>
<div class="tags"><span class="tag">4방향 회피</span><span class="tag">문자 없는 전투 신호</span><span class="tag">캐릭터별 성장</span><span class="tag">50층 · 다음 회차</span></div></div>
<div class="status"><b>사용자 피드백 반영 · 기획 v3</b><span id="review-status">{status}</span>
<p class="mini" style="margin:12px 0 0">기획과 상호작용 시안이다. Unity 게임·실기기 조작·결제 구현 완료를 뜻하지 않는다.</p></div></div>
<img class="hero-art" src="Visuals/landscape-battle-v3.svg" alt="삼각형 방향키, 인물 주변 공격 예고, 발밑 체력과 긴 보스 체력">
<p class="caption">전투 배치 시안 · 지면 약82% · 최종 도트 자산이나 게임 캡처가 아니다.</p></header>
<nav class="nav" aria-label="기획서 목차"><div class="wrap"><a href="#decision">변경 요약</a><a href="#combat">전투 신호</a><a href="#flow">첫 시작</a><a href="#lobby">캐릭터 로비</a><a href="#growth">별점·종 강화</a><a href="#campaign">50층·체크포인트</a><a href="#economy">재화·유물</a><a href="#production">제작·검증</a><a href="#revenue">수익 가정</a><a href="#review">검토 기록</a></div></nav><main class="wrap">'''

decision = section("decision", 1, "전투는 형태로, 성장은 종으로", table(["항목", "v3 기준"], [
    ["입력", "▲ ◀ ▼ ▶ 네 방향 회피 + 검 아이콘 공격. 앞 회피는 적을 통과하지 않는 짧은 진입 후 복귀"],
    ["공격 예고", "인물 주변 위험 위치의 궤적·점멸·형태. 상단 공격 설명과 상시 정답 화살표 제거"],
    ["상태 막대", "플레이어·일반 적 HP는 각 발밑. 중간 보스·보스 HP는 상단의 긴 막대. 기절은 종 아이콘과 막대·자세로 전달"],
    ["타격감", "픽셀 숫자 피해 팝업, 피격 지점의 사각 파편, 치명타의 짧은 월드 흔들림"],
    ["진입", "최초 실행 즉시1층 → 회피 → 공격 튜토리얼 → 연속 진행. 사망하면 로비"],
    ["성장", "캐릭터별 공격력·체력·행운·공명, 고정 별점, + 강화와10개 종. 10→11·20→21에 큰 성장"],
    ["진행", "50층, 10단위 보스 체크포인트. 50층 엔딩 후 해당 캐릭터의 다음 회차1층"],
    ["특별 재화", f"{e['gemName']}: 중간 보스1 / 보스3, 반복 처치도 지급. 골드 교환·장착 슬롯·현금 상품"],
]) + '<p><a class="button-link" href="Visuals/wireframes-v3.html">새 화면과 상호작용 시안 열기</a> <a class="button-link secondary" href="index-v2.html">이전 v2 보기</a></p>')

combat = section("combat", 2, "읽는 HUD에서 보는 전투로", paragraphs(c["display"]) +
    table(["위험 형태", "위험 위치", "대응 회피"], c["cueRows"]) +
    table(["연출", "초기 기준", "판정을 가리지 않는 조건"], c["feedbackRows"]) +
    table(["회피·기절 규칙", "초기값"], [
        ["회피", f"유효{c['dodgeActiveMs']}ms + 복귀{c['dodgeRecoveryMs']}ms, 다음 입력까지{c['dodgeReuseMs']}ms. 마지막{c['perfectWindowMs']}ms가 완벽"],
        ["기절 누적", f"일반+{c['normalStagger']} / 완벽+{c['perfectStagger']}, {c['staggerThreshold']}에서 발동. 피격−20. 해당 적 전투에서만 유지"],
        ["기절 상태", "일반1.5초 / 중간1.2초 / 보스1초. 남은 공격 취소, 종료 후2초 누적 면역"],
        ["공격", "간격500ms·준비100ms. 회피로 취소 가능하나 공격 간격 환급 없음. 출전 중 성장값 고정"],
    ]) + '<p class="mini">공격 예고는 위험이 오는 위치다. 눌러야 할 방향을 항상 표시하는 장치가 아니다. 소리·진동·화면 흔들림을 꺼도 궤적과 자세만으로 구분되어야 한다. 수치는 신작의 초기 가설이며 원작 실측값이 아니다.</p>')

flow = section("flow", 3, "처음에는 바로 싸운다", '<div class="loop">' + "".join(
    f'<div class="step"><span class="number">{i+1:02}</span><b>{escape(row[0])}</b><span>{escape(row[1])}</span></div>'
    for i, row in enumerate(d["firstRun"]["steps"])
) + "</div>" + paragraphs(d["firstRun"]["rules"]) +
    '<div class="callout">첫 출전 중에는 로비 강화가 없다. 처음부터 스테이지에 맞춰 레벨을 올린다고 가정하지 않는다. 첫 출전은 Lv.0 기본 능력으로 학습하고, 획득한 골드는 로비에서 투자한다. 실패를 강제하는 연출은 두지 않는다.</div>')

lobby = section("lobby", 4, "왼쪽에서 고르고, 오른쪽에서 키운다",
    figure("lobby-v3.svg", "가로 로비: 좌측 약25% 캐릭터 목록, 우측 Idle·4개 스탯·10개 종·+ 버튼, 우상단 지갑") +
    paragraphs(d["lobby"]["display"]) +
    table(["캐릭터", "공격", "체력", "행운", "공명", "특징"], [
        [x["name"]] + ["★"*x["stars"][k] + "☆"*(5-x["stars"][k]) for k in ["attack","hp","luck","resonance"]] + [x["identity"]]
        for x in d["characters"]
    ]) + '<p class="mini">별점은 캐릭터의 고정 성장 적성이다. 확률 뽑기·별 강화·유료 승급은 이번 범위에 없다. 캐릭터를 바꿔도 다른 캐릭터의 성장이나 체크포인트를 덮어쓰지 않는다.</p>')

growth = section("growth", 5, "별점은 증가 폭, 종은 지금까지의 강화",
    paragraphs(g["display"]) +
    table(["스탯", "전투 효과", "제한"], g["statRows"]) +
    table(["강화 레벨", "종 표시", "공격 ★1", "공격 ★3", "공격 ★5"], g["sampleRows"]) +
    '<div class="callout">Lv.10에서는 종10개가 가득 찬다. 다음 +를 누르면 돌파 효과와 수치 증가를 보여주고, Lv.11의 다음 구간 종1개로 바뀐다. 강화 레벨과 구간 표시는 유지해 성장이 사라진 것처럼 보이지 않게 한다.</div>' +
    '<p class="formula">'+escape(g["formulaDisplay"])+'</p>' +
    table(["강화 비용", "도달 레벨 기준"], e["upgradeRows"]))

zones = []
for z in range(5):
    cells = []
    for s in range(z*10+1,z*10+11):
        kind = "보스" if s%10==0 else "중간" if s%5==0 else "일반"
        cls = "boss" if s%10==0 else "mid" if s%5==0 else ""
        cells.append(f'<div class="stage-cell {cls}"><b>{s}</b><small>{kind}</small></div>')
    zones.append(f'<div class="panel"><h3>{escape(p["zoneNames"][z])} · {z*10+1}–{z*10+10}</h3><div class="stage-track">{"".join(cells)}</div></div>')
campaign = section("campaign", 6, "보스를 넘긴 기록만 출발점을 바꾼다", '<div class="stage-zones">'+"".join(zones)+"</div>" +
    paragraphs(p["display"]) +
    table(["상황", "다음 출전"], p["checkpointExamples"]) +
    table(["시점", "능력·행동", "저장"], p["endingRows"]) +
    '<p class="formula">'+escape(p["enemyFormulaDisplay"])+'</p>' +
    table(["층", "종류", "적 HP", "피격 피해"], p["enemySamples"]) +
    '<details><summary>출전 중 강화 없이, 10전을 버티는 초기 계산</summary>' +
    table(["고정 출전", "구간", "최대HP", "순수 전투량", "예상 피해", "보스 회복 전 잔여"], p["survivalExamples"]) +
    '<p class="mini">'+escape(p["survivalAssumptions"])+'</p></details>' +
    '<p class="mini">적은 캐릭터의 현재 레벨에 자동으로 맞춰지지 않는다. v2의30층·스테이지별 HP전회복·현재층 재도전·474초 가정은 폐기한다. 실제 완주 시간·사망 횟수·NG+도달 시간은 시제품 관찰로 확인한다.</p>')

economy = section("economy", 7, "종파편을 얻고 투자 방향을 고른다",
    table(["획득·사용", "초기값", "조건"], e["displayRows"]) +
    paragraphs(e["display"]) +
    table(["처치 위치", "유물", "효과"], d["relics"]["displayRows"]) +
    table(["소유 범위", "저장 항목"], d["saveOwnership"]) +
    '<p class="mini">현금 상품은 기획 후보이며 실제 결제 기능은 없다. 유물과 종파편은 남은 장착 공간과 무관하게 지급한다. 같은 보상/구매 거래를 중복 콜백으로 다시 지급하지 않는다.</p>')

production = section("production", 8, "첫10층에서 입력·생존·성장을 연결한다",
    table(["단계", "만드는 범위", "확인할 것"], d["production"]["phases"]) +
    table(["리소스", "수량·범위"], d["production"]["resources"]) +
    paragraphs(d["production"]["display"]) +
    '<p><a href="Reviews/combat-v3.md">전투·성장 계산</a> · <a href="Reviews/ui-v3.md">UI bounds·상태·문구 명세</a></p>' +
    '<div class="flow-code">Session: FirstRun → TutorialDodge → TutorialAttack → Combat → StageTransition → Combat\nDeath / Retreat → Lobby(characterId)\nBoss10/20/30/40 clear → checkpoint commit + heal → next floor\nBoss50 clear → reward + cycle completion commit → Ending → Lobby → next cycle floor1</div>' +
    paragraphs(d["production"]["architecture"]))

b=d["business"]
revenue = section("revenue", 9, "현금 재화 추가가 매출 상승을 보장하지 않는다",
    paragraphs(b["display"]) +
    table(["상품 후보", "가격", "제공"], b["products"]) +
    table(["30일 민감도", "MAU", "결제자 비율", "ARPPU", "모형상 수입"], b["scenarioRows"]) +
    '<p class="formula">'+escape(b["formulaDisplay"])+'</p>' +
    '<p class="mini">가격·전환율·파밍 시간은 출시 전에 검증해야 할 가정값이다. 노동비·유료 이용자 확보 비용을 제외한 금액을 사업 이익으로 표시하지 않는다. 원스토어 연동 조건은 담당 문서의 공식 출처와 확인일을 따른다.</p>' +
    '<p><a href="Reviews/market-v3.md">경제 계산·판매/거래 계약·공식 자료</a></p>')

review = section("review", 10, "담당별 교차 검토와 검증 기록",
    table(["담당", "검토", "상태"], [
        [r["label"],r["scope"],r["display"]] for r in d["reviews"].values()
    ]) + paragraphs([d["reviewDisplay"], "기획 검토 완료는 실제 재미·결제·출시 검증 완료를 뜻하지 않는다."]) +
    '<p><a href="Reviews/concept-v3.md">기획</a> · <a href="Reviews/combat-v3.md">전투</a> · <a href="Reviews/ui-v3.md">UI</a> · <a href="Reviews/market-v3.md">경제</a></p>' +
    '<p class="mini"><a href="decisions.json">현재 결정 기록</a> · <a href="Evidence/planning-verification-v3.json">수치·상태 전이 검사</a> · <a href="Evidence/document-render-check-v3.json">렌더·상호작용 검사</a> · <a href="index-v2.html">이전 v2</a></p>')
page = head + "".join([decision,combat,flow,lobby,growth,campaign,economy,production,revenue,review]) + '</main><footer class="wrap">2026-10-21 · planning-v3 · 기획과 상호작용 시안 · 실제 게임 빌드 아님<br><a href="#top">처음으로</a></footer></body></html>'
(ROOT / "index.html").write_text(page,encoding="utf-8")
print("Built Planning/index.html from planning-v3 decisions")
