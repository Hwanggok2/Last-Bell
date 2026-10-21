"""Build the current visual planning document. Gameplay is not implemented here."""
import json
from html import escape
from pathlib import Path

ROOT = Path(__file__).parent
d = json.loads((ROOT / "decisions.json").read_text(encoding="utf-8"))
story_data = json.loads((ROOT / "story.json").read_text(encoding="utf-8"))
idle_data = json.loads((ROOT / "idle-status.json").read_text(encoding="utf-8"))
assert d["version"] == "planning-v5.1"

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
<title>마지막 종지기 · 통합 기획 v5.1 · 타이밍·성장·버튼</title><link rel="stylesheet" href="style.css"><style>
.story-top{{margin:24px 0 0;border:1px solid var(--amber);padding:20px 24px;background:#1b2534}}.story-top p{{margin-bottom:8px}}.boss-story-grid{{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:24px;margin:24px 0}}.boss-story-card{{min-width:0;padding:24px;background:var(--surface);border:1px solid var(--line)}}.boss-story-card h3{{color:var(--amber);font-size:24px}}.boss-story-card h4{{margin:20px 0 8px;color:var(--blue)}}.boss-story-card:last-child{{grid-column:1/-1}}.story-strip{{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:6px;margin:16px 0}}.story-strip span{{display:block;text-align:center;font-size:13px;border-top:3px solid var(--amber);padding:10px 4px;background:#111722}}.story-dialogue{{padding:14px 18px;background:#0b1220;border-left:3px solid #edb75d;margin:10px 0}}.story-dialogue small{{color:var(--amber);display:block}}.story-dialogue p{{font-size:20px;line-height:1.55;margin:4px 0 0}}.story-boss-mark{{height:90px;display:flex;align-items:center;justify-content:center;gap:12px;background:linear-gradient(180deg,#223852,#0c172b);color:#f2d184;font-size:42px;font-family:monospace}}.story-boss-mark span{{font-size:14px;color:var(--muted)}}.story-role{{font-size:14px;color:var(--muted)}}.story-flow{{display:flex;flex-wrap:wrap;gap:8px;margin:18px 0}}.story-flow span{{border:1px solid var(--line);padding:8px 12px;background:var(--surface)}}@media(max-width:850px){{.boss-story-grid{{grid-template-columns:1fr}}.boss-story-card:last-child{{grid-column:auto}}}}@media(max-width:520px){{.boss-story-card{{padding:18px}}.story-dialogue p{{font-size:18px}}.story-top{{padding:18px}}.story-strip{{grid-template-columns:repeat(2,minmax(0,1fr))}}}}
</style></head><body>
<header class="wrap" id="top"><div class="hero-top"><div><p class="eyebrow">GAME DESIGN v5.1 · TIMING / GROWTH / UI / 2026.10.21</p>
<h1>마지막 종지기</h1><p class="pitch">위험을 보고 피한다.<br>열 개의 종을 채워 다음 힘을 깨운다.</p>
<div class="tags"><span class="tag">4방향 회피</span><span class="tag">위험 위치·타이밍 신호</span><span class="tag">캐릭터별 성장</span><span class="tag">50층 · 다음 회차</span></div><p><a class="button-link" href="Presentation/feedback-plan-v5.html">타이밍·성장·버튼 개정안</a> <a class="button-link secondary" href="#story">스토리·엔딩 보기</a></p></div>
<div class="status"><b>타이밍·성장·버튼 v5.1 · 게임 적용</b><span id="review-status">{status}</span>
<p class="mini" style="margin:12px 0 0">v5.1을 Windows MVP에 적용하고 화면·입력·저장을 검증했다. 12~50층·실물 터치·결제는 별도 범위다.</p></div></div>
<div class="story-top"><p class="eyebrow">STORY · 10층마다 드러나는 진실</p><p><b>{escape(story_data['logline'])}</b></p><p>보스가 무너진 뒤 화면 아래에 짧은 대사가 남습니다. 이야기와 유물 선택이 끝나면 곧바로 다음 층으로 이어집니다.</p><p class="mini">10층 종의 기사: “문은… 안에서 잠갔다.” · <a href="#story">다섯 보스의 장면과 결말 읽기</a></p></div>
<img class="hero-art" src="Visuals/landscape-battle-v4.svg" alt="삼각형 방향키, 인물 주변 공격 예고, 발밑 체력과 긴 보스 체력">
<p class="caption">이전 v4 배치 참고 · 지면 약82%. 최신 타이밍 표시·버튼 배치와 크기는 아래 v5 개정안이 우선한다. 연출10 적용 기록은 별도 보존한다.</p></header>
<nav class="nav" aria-label="기획서 목차"><div class="wrap"><a href="#feedback-v5">최신 v5 개정</a><a href="#story">스토리·세계관</a><a href="#idle-status">Idle 현황</a><a href="Production/index.html">Unity MVP 제작실</a><a href="Presentation/index.html">전투 연출 기획 10</a><a href="#decision">변경 요약</a><a href="#combat">전투 신호</a><a href="#flow">첫 시작</a><a href="#lobby">캐릭터 로비</a><a href="#growth">별점·종 강화</a><a href="#campaign">50층·체크포인트</a><a href="#economy">재화·유물</a><a href="#production">제작·검증</a><a href="#revenue">수익 가정</a><a href="#review">검토 기록</a></div></nav><main class="wrap">'''

revision = section("feedback-v5", 0, "최신 개정 · 타이밍·성장·버튼과 달리기 방향",
    '<div class="callout"><b>v5.1 게임 적용 · 실제 빌드 검증 완료</b><p>공격 예고와 완벽 회피, 강화 가격과 버튼에 관한 이전 설명은 이 개정안을 우선한다. 기존 연출10의 적용 기록과 v4 검토 기록은 과거 상태로 보존한다.</p></div>' +
    paragraphs([
        "공격 예고: 위험 영역과 일정한 속도의 타이밍 막대를 분리한다. 회피 유효160ms 중 마지막60ms 완벽 판정은 유지한다.",
        "완벽 회피: 성공 확정 시 ‘완벽’320ms·청록 마름모와 이중 종 파문180ms·전용 종소리를 묶는다. 기존 기절 누적20/40을 실제 증가량으로 강조한다.",
        "경제 재개정: 강화할 때마다 약25%, 10→11·20→21·30→31·40→41 돌파는 약2.5배. 목표L의 가격은 ceil(10×1.25^(L−1)×2^floor((L−1)/10))G. 스테이지S 기본 골드는 그 가격의2배, 중간×3·보스×5. 10층까지1,374G로 공격·HP11과 행운·공명7을 구매하는 예산을 맞춘다.",
        "버튼: 남색 철판·황동 모서리·크림색 기호. 960/640 화면에서 회피64/56, 공격104/80 크기로 재배치한다. 방향키 배경은 두지 않는다.",
        "달리기: 새8자세를20fps·400ms로 적용했다. 배경 이동을640 기획px/2초로 줄였고 전체 전환3250ms는 유지한다. 접지 오차2px 목표와 별도 출발/감속 자세는 추가 검토 대상이다.",
        "원작 조사: 공식 소개·패치·리뷰·커뮤니티 표본을 확인했다. 전체 내부 밸런스표·모든 강화 가격·정확한 Just ms는 확보하지 못했으며 제안값과 구분한다."
    ]) + '<p><a class="button-link" href="Presentation/feedback-plan-v5.html">타이밍·가격·버튼 시안 열기</a> <a href="Presentation/design-spec.md">상세 개정 기획</a> · <a href="Presentation/feedback-plan-v5.json">수치와 조사 출처</a></p>')

def boss_scene_card(scene):
    dialogue = ''.join('<div class="story-dialogue"><small>'+escape(beat['speaker'].replace('선택한 종지기','온 · 응답 예시'))+'</small><p>“'+escape(beat['text'])+'”</p></div>' for beat in scene['dialogue'])
    response = scene['dialogue'][-1]['characterText']
    variants = '<p class="story-role">담: “'+escape(response['dam'])+'”<br>린: “'+escape(response['rin'])+'”</p>'
    steps = '<div class="story-strip">'+''.join('<span>'+escape(label)+'</span>' for label in ['01 보스 붕괴','02 하단 대사','03 유물 선택','04 '+('엔딩·로비' if scene['stage']==50 else '다음 층')])+'</div>'
    return '<article class="boss-story-card" id="story-boss-'+str(scene['stage'])+'"><p class="eyebrow">STAGE '+str(scene['stage'])+' / '+escape(scene['zone'])+'</p><h3>'+escape(scene['title'])+'</h3><div class="story-boss-mark" aria-hidden="true">'+str(scene['stage'])+'<span>'+escape(scene['bossName'])+'</span></div><p class="story-role">'+escape(scene['personality'])+'</p>'+steps+'<h4>쓰러진 뒤 남는 장면</h4>'+paragraphs([scene['defeat']])+'<h4>게임 하단에 나오는 말 · 한 번에 한 줄</h4>'+dialogue+variants+'<h4>새로 알게 되는 진실</h4>'+paragraphs([scene['reveal']])+'<h4>화면에 남기는 증거</h4>'+paragraphs([scene['evidence']])+'<h4>다음 장면과 복선 회수</h4>'+paragraphs([scene['nextLink'],scene['payoff']])+'</article>'

story_cards = '<div class="boss-story-grid">'+''.join(boss_scene_card(x) for x in story_data['bossScenes'])+'</div>'
story = section("story", 0, "스토리 · 마을의 내일을 돌려주는 여정",
    '<p class="pitch">'+escape(story_data['logline'])+'</p>' + paragraphs([story_data['premise']]) +
    '<div class="callout"><b>이번에 제안하는 보스 서사</b><p>첫 화면은 바로 전투입니다. 이야기는10·20·30·40·50층 보스가 쓰러진 뒤 세 박자의 짧은 대사로 드러납니다. 아래 인물상과 숨겨진 진실은 이번 개정 기획안이며, 실제 컷신 구현 완료를 뜻하지 않습니다.</p></div>' +
    '<figure class="wide-figure"><img src="Presentation/mood-storyboard-v1.png" alt="푸른 밤의 종탑, 불씨를 든 작은 종지기, 귀환 등불과 거대한 종의 기사"><figcaption>기존 콘셉트 이미지 · 밤의 탑 → 돌아올 등불 → 작은 종지기와 거대한 수호자. 새 보스5종 컷신을 제작했다는 뜻은 아닙니다.</figcaption></figure>' +
    '<h3>종탑에 무슨 일이 있었나</h3>' + paragraphs(story_data['coreTruth']) +
    '<div class="grid3">' + ''.join('<article class="panel"><h3>'+escape(x[0])+'</h3><p>'+escape(x[1])+'</p></article>' for x in story_data['characters']) + '</div>' + paragraphs([story_data['characterContinuity']]) +
    '<h3>'+escape(story_data['opening']['title'])+'</h3>' + paragraphs([story_data['opening']['description'],story_data['opening']['rules']]) +
    '<h3>다섯 보스의 마지막 말</h3><p>보스의 몸이 무너진 다음 핵과 종에 남은 목소리로 말합니다. 진실·복선 설명은 이 기획서에만 남기고 게임 하단에는 말하는 이와 대사만 표시합니다.</p>' +
    '<p>'+ ' · '.join('<a href="#story-boss-'+str(x['stage'])+'">'+str(x['stage'])+'층 '+escape(x['bossName'])+'</a>' for x in story_data['bossScenes']) + '</p>' + story_cards +
    '<h3>읽은 뒤에도 출전은 이어진다</h3><div class="story-flow">'+''.join('<span>'+escape(x)+'</span>' for x in ['처치·보상 저장','보스 붕괴','하단 대사','유물3택1','선택·체크포인트 저장','자동 다음 층'])+'</div>' + paragraphs([story_data['chapterRules']]) +
    '<div class="grid2"><article class="panel"><h3>첫 처치 · 건너뛰기 · 다시 보기</h3>'+paragraphs([story_data['playback']['firstClearExplanation'],story_data['playback']['skip'],story_data['playback']['archiveReplay']])+'</article><article class="panel"><h3>도중에 앱을 꺼도 이어서</h3>'+paragraphs([story_data['playback']['resume'],story_data['playback']['endingResume']])+'</article></div>' +
    '<div class="grid2"><article class="panel"><h3>'+escape(story_data['return']['title'])+'</h3>'+paragraphs([story_data['return']['description'],story_data['return']['rule']])+'</article><article class="panel"><h3>아침을 다시 빼앗지 않는 다음 회차</h3>'+paragraphs([story_data['ending']['nextCycle']])+'</article></div>' +
    '<h3>50층 · 돌려보내는 약6초 엔딩</h3>' + table(['구간','화면','게임에 남길 문구'],story_data['ending']['cuts']) + paragraphs([story_data['ending']['rules']]) +
    '<div class="callout"><h3>1~10층 MVP의 서사 범위</h3>'+paragraphs([story_data['mvp']])+'</div>' +
    '<p class="mini">'+escape(story_data['sourceNotes'])+' <a href="Reviews/concept-v3.md">기존 서사 근거</a> · <a href="story.json">현재 스토리 원본</a></p>')

idle_status = '<div id="idle-status" style="scroll-margin-top:80px"><h3>Idle 제작 현황 · 전체 완료 아님</h3><p>계획53종 중 <b>연속 Idle 후보1종</b>, 정지 그림만5종, 미제작47종입니다.</p>' + table(['대상','수량','실제 자산','상태'],idle_data['rows']) + '<div class="grid2"><figure><img src="'+idle_data['previewGif']+'" alt="온의 기존8프레임 Idle 후보 반복 미리보기" style="display:block;max-width:100%;width:288px;image-rendering:pixelated"><figcaption>온 · 기존8프레임 후보 GIF. Unity 적용 전.</figcaption></figure><div>'+paragraphs(idle_data['displayNotes'])+'</div></div>'+paragraphs([idle_data['nextPriority']])+'<p class="mini"><a href="idle-status.json">자산 감사 원본</a> · <a href="Presentation/PixelLab/review.html">프레임 검토기</a></p></div>'

decision = section("decision", 1, "계속 싸우고, 유물을 골라 성장한다", table(["항목", "현재 기준"], [
    ["입력", "▲ ◀ ▼ ▶ 화살표만 표시, 배경·테두리 제거. 보이지 않는 넓은 입력 영역은 유지"],
    ["공격 예고", "플레이어 주변의 크고 선명한 위험 궤적. 외곽선·형태로 방향 구분, HP·손가림 회피"],
    ["상태 막대", "플레이어·일반 적 HP는 각 발밑. 중간 보스·보스 HP는 상단의 긴 막대. 기절은 종 아이콘과 막대·자세로 전달"],
    ["타격감", "피해 숫자650ms 우상향, 캐릭터 전체 백색 피격. 치명타80ms·막타120ms가 겹치면 막타1회만. 기획1280px와Unity월드진폭은 아래기준으로구분"],
    ["진입", "최초 실행 즉시1층 → 회피 → 공격 튜토리얼 → 연속 진행. 사망하면 로비"],
    ["성장", "캐릭터별 공격력·체력·행운·공명, 고정 별점, + 강화와10개 종. 10→11·20→21에 큰 성장"],
    ["진행", "죽을 때까지 연속 전투. 보스 붕괴·첫 하단 대사·유물 선택과 전환 연출 뒤 다음 층 자동 진행, 10층 단위 출발 확인 없음. 50층 엔딩 유지"],
    ["로비", "Stage 21처럼 재출발 스테이지만 표기, 지갑은 우측 끝. 골드 부족 시 필요한 파편 교환 확인"],
    ["유물", "좌측1/3 장착·보유 목록, 우측 효과 상세. 10단위 보스만3개 중1선택, 중복은 레벨업"],
    ["특별 재화", f"{e['gemName']}: 중간 보스1 / 보스3, 반복 처치도 지급. 골드 교환·장착 슬롯·현금 상품"],
]) + '<p><a class="button-link" href="Visuals/wireframes-v4.html">이전 v4 시안 · 가격·배치 구버전</a> <a class="button-link secondary" href="index-v3.html">이전 v3 보기</a></p>')

presentation = '<div class="callout"><h3>기존 적용 기록 · 전투 연출10</h3>' + paragraphs(d['presentation'][key] for key in ['runtimeUpdate','run','dodge','dodgeTiming','attack','slash','monsterHit','hitFlash','lethalHit','damagePopup','transition','stageBanner','boss','bossStory','telegraph']) + '<p><a class="button-link" href="Presentation/index.html#boss">이동·Stage·보스 등장</a> <a class="button-link secondary" href="Presentation/index.html#player">베기·전체 백색 피격</a> <a class="button-link secondary" href="Presentation/index.html#parallax">기존 달리기 검토</a></p></div>'
def latest_visual(file, caption, version="Presentation-v10"):
    return f'<figure><img style="width:100%;image-rendering:pixelated" src="Evidence/{version}/{file}" alt="{escape(caption)}"><figcaption>{escape(caption)}</figcaption></figure>'
presentation += '<div class="grid2"><figure><h3>참고 · SimpleGame 검술사</h3><img src="Presentation/PixelLab/v10/reference/reference.gif" alt="왼쪽을 향한 검술사 원본8프레임500ms 달리기" style="display:block;width:256px;height:256px;image-rendering:pixelated"><figcaption>원본64×64를4배 표시 · 왼방향 그대로 · 실제 Move8장·16fps·500ms.</figcaption></figure><figure><h3>새 후보 · 마지막 종지기 온</h3><img src="Presentation/PixelLab/clips/hero-v10-blademage-run/edited-preview.gif" alt="오른쪽을 향한 온의 새 전체 몸체 선택8자세24fps·주기약333.33ms 후보" style="display:block;width:256px;height:256px;image-rendering:pixelated"><figcaption>128×128을2배 표시 · 오른방향 · 반환9장 중00~07 선택8자세·24fps·주기약333.33ms. GIF는3주기·1초로 저장.</figcaption></figure></div><p class="mini">두 인물은 방향이 다르다. 검술사 이미지는 사용자 프로젝트의 동작 참고로 원형을 보존하고, 온은 별도 입력 자세에서 전체 몸체 시퀀스를 생성했다. <a href="Presentation/PixelLab/v10/reference/source-manifest.json">클립·컨트롤러·프레임 추적 근거</a></p>'
presentation += '<div class="grid2">' + ''.join(latest_visual(file,caption,version) for file,caption,version in [
    ('run.gif','선택8자세·24fps·약333.33ms · 달리기1.5배 · Unity 미적용','Presentation-v10'),
    ('transition.gif','분해 →2초1920px 이동(960px/s) → Stage 도트 조립 → 첫 예고 · 전체3250ms 유지','Presentation-v10'),
    ('run-cycle.png','선택8자세·약333.33ms 한 주기 · 작은 팔 움직임·다리 교대·지지발 비교','Presentation-v10'),
    ('attack-link.png','새 달리기 접근과 기존 베기 연결 · 명중200ms·재공격500ms 유지','Presentation-v10'),
    ('hit-flash.gif','기존 피격: 맞은 캐릭터만45ms 흰색 유지 ·120ms 원색 복귀','Presentation-v7')
]) + '</div><p class="mini"><a href="Reviews/run-v10-prompt.md">v10 검술사 참고·생성 기록</a> · <a href="Reviews/run-v10-art.md">v10 원형·동작 검토</a> · <a href="Presentation/index-v9.html">이전 v9 시안</a> · <a href="Reviews/hit-flash-v7-flow.md">기존 피격·v4 회피 복원 근거</a></p>'

combat = section("combat", 2, "읽는 HUD에서 보는 전투로", presentation + paragraphs(c["display"]) +
    table(["위험 형태", "위험 위치", "대응 회피"], c["cueRows"]) +
    table(["연출", "현재 기준", "판정을 가리지 않는 조건"], c["feedbackRows"]) +
    '<div class="grid2">' + figure("damage-normal-v4.png", "일반 피해 · 960×540 시안의 픽셀 숫자 높이35") +
    figure("damage-critical-v4.png", "치명타 · 숫자 높이50과 외곽 강조, 월드만 짧게 흔들림") + '</div>' +
    table(["회피·기절 규칙", "초기값"], [
        ["회피", f"유효{c['dodgeActiveMs']}ms + 복귀{c['dodgeRecoveryMs']}ms, 다음 입력까지{c['dodgeReuseMs']}ms. 마지막{c['perfectWindowMs']}ms와 정확한 타격T까지 완벽. 올바른 방향·회피 준비 조건, T 이후 유예 없음"],
        ["기절 누적", f"일반+{c['normalStagger']} / 완벽+{c['perfectStagger']}, {c['staggerThreshold']}에서 발동. 피격−20. 해당 적 전투에서만 유지"],
        ["기절 상태", "일반1.5초 / 중간1.2초 / 보스1초. 남은 공격 취소, 종료 후2초 누적 면역"],
        ["공격", "간격500ms·접근 후200ms 명중·460ms 원위치 복귀. 회피로 취소 가능하나 공격 간격 환급 없음. 캐릭터 강화·장착 구성은 출전 고정, 장착 유물의 레벨업만 다음 적부터 반영"],
    ]) + '<p class="mini"><a href="Reviews/telegraph-end-timing.md">예고 종료 수정·Unity 자동검사 결과</a> · <a href="Evidence/UnityCombatFeedback-20261021/verification.json">빌드·해시 통합 기록</a> · <a href="Evidence/UnityCombatFeedback-20261021/playmode.xml">PlayMode5/5</a> · <a href="Evidence/UnityCombatFeedback-20261021/editmode.xml">EditMode58/58</a> · <a href="Evidence/UnityCombatFeedback-20261021/smoke-result.json">새 실행 파일smoke</a> · <a href="Evidence/UnityCombatFeedback-20261021/16-critical-hit.png">GPU 치명타 팝업 한 시점</a> · <a href="Presentation/index.html#signal">입력 offset 기획 검토</a></p><p class="mini">공격 예고는 위험이 오는 위치다. 눌러야 할 방향을 항상 표시하는 장치가 아니다. 소리·진동·화면 흔들림을 꺼도 궤적과 자세만으로 구분되어야 한다. 수치는 신작의 초기 가설이며 원작 실측값이 아니다.</p>')

flow = section("flow", 3, "처음에는 바로 싸운다", '<div class="loop">' + "".join(
    f'<div class="step"><span class="number">{i+1:02}</span><b>{escape(row[0])}</b><span>{escape(row[1])}</span></div>'
    for i, row in enumerate(d["firstRun"]["steps"])
) + "</div>" + paragraphs(d["firstRun"]["rules"]) +
    '<div class="callout">첫 출전 중에는 로비 강화가 없다. 처음부터 스테이지에 맞춰 레벨을 올린다고 가정하지 않는다. 첫 출전은 Lv.0 기본 능력으로 학습하고, 획득한 골드는 로비에서 투자한다. 실패를 강제하는 연출은 두지 않는다.</div>')

lobby = section("lobby", 4, "왼쪽에서 고르고, 오른쪽에서 키운다",
    figure("lobby-v4.svg", "가로 로비: 좌측 약25% 캐릭터 목록, 우측 Idle·4개 스탯·10개 종·+ 버튼, 우상단 지갑") +
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
    '<p class="mini">위 표는 유물 없이 고정한 각 성장 상태의 계산 예시다. 서로 다른 레벨 구간을 한 번의 출전으로 합산하지 않는다. v4 유물 선택·레벨 성장은 별도 관찰 대상이며 위 생존량에 포함하지 않았다. 실제 완주 시간·사망 횟수·NG+도달 시간은 시제품에서 확인한다.</p>')

economy = section("economy", 7, "종파편을 얻고 투자 방향을 고른다",
    table(["획득·사용", "초기값", "조건"], e["displayRows"]) +
    paragraphs(e["display"]) +
    table(["스테이지", "기본값 시작→끝", "중간보스", "보스", "구간 총액"], e["stageGoldRows"]) +
    figure("exchange-v4.svg", "이전 v4 교환 흐름 · 그림의150G/2파편은 구가격. 최신 예시는187G·보유12G·초기 교환율100G에서2파편 교환이며 확인 후 골드 획득, 다시 +로 강화") +
    figure("relics-v4.svg", "좌측1/3 안의 장착 슬롯·보유 목록과 우측 선택 유물 상세") +
    paragraphs(d["relics"]["display"]) +
    figure("relic-choice-v4.svg", "유물3개 중1개 선택. 신규와 중복 성장 결과를 비교하고 선택 확정 뒤 다음 적으로 이어진다") +
    table(["유물", "효과", "Lv.1", "Lv.2", "Lv.3", "Lv.4", "Lv.5"], d["relics"]["displayRows"]) +
    table(["소유 범위", "저장 항목"], d["saveOwnership"]) +
    '<p class="mini">현금 상품은 기획 후보이며 실제 결제 기능은 없다. 유물과 종파편은 남은 장착 공간과 무관하게 지급한다. 같은 보상/구매 거래를 중복 콜백으로 다시 지급하지 않는다.</p>')

production = section("production", 8, "첫10층에서 입력·생존·성장을 연결한다", idle_status +
    table(["단계", "만드는 범위", "확인할 것"], d["production"]["phases"]) +
    table(["리소스", "수량·범위"], d["production"]["resources"]) +
    paragraphs(d["production"]["display"]) +
    '<p><a href="Reviews/combat-v4.md">전투·성장 계산</a> · <a href="Reviews/ui-v4.md">UI bounds·상태·문구 명세</a></p>' +
    '<div class="flow-code">FirstRun → TutorialDodge → TutorialAttack → Combat\n일반 처치 → 저장 → 분해·복귀 →2초 질주 → Stage 표시 → 온전한 첫 예고\n중간 보스 → 기본 골드·종 파편1개 저장 → 분해 → 유물 선택 없이 자동 전진\n10·20·30·40층 보스 → 처치·후보 저장 → 붕괴 → 캐릭터 첫 하단 대사 → 유물3택1 → 선택·체크포인트·회복 저장 → 자동 다음층\nDeath / Retreat → Lobby(characterId) → Stage N\n50층 보스 → 붕괴 → 캐릭터 첫 하단 대사 → 유물 선택·완주 저장 → 캐릭터 첫6초 엔딩 → 로비·해당 캐릭터 NG+1층</div>' +
    paragraphs(d["production"]["architecture"]))

b=d["business"]
revenue = section("revenue", 9, "현금 재화 추가가 매출 상승을 보장하지 않는다",
    paragraphs(b["display"]) +
    table(["상품 후보", "가격", "제공"], b["products"]) +
    table(["30일 민감도", "MAU", "결제자 비율", "ARPPU", "모형상 수입"], b["scenarioRows"]) +
    '<p class="formula">'+escape(b["formulaDisplay"])+'</p>' +
    '<p class="mini">가격·전환율·파밍 시간은 출시 전에 검증해야 할 가정값이다. 노동비·유료 이용자 확보 비용을 제외한 금액을 사업 이익으로 표시하지 않는다. 원스토어 연동 조건은 담당 문서의 공식 출처와 확인일을 따른다.</p>' +
    '<p><a href="Reviews/market-v4.md">경제 계산·판매/거래 계약·공식 자료</a></p>')

review = section("review", 10, "담당별 교차 검토와 검증 기록",
    table(["담당", "검토", "상태"], [
        [r["label"],r["scope"],r["display"]] for r in d["reviews"].values()
    ]) + paragraphs([d["reviewDisplay"], "기획 검토 완료는 실제 재미·결제·출시 검증 완료를 뜻하지 않는다."]) +
    '<p><a href="Reviews/concept-v4.md">기획</a> · <a href="Reviews/combat-v4.md">전투</a> · <a href="Reviews/ui-v4.md">UI</a> · <a href="Reviews/market-v4.md">경제</a></p>' +
    '<p class="mini"><a href="decisions.json">현재 결정 기록</a> · <a href="Evidence/planning-verification-v4.json">수치·상태 전이 검사</a> · <a href="Evidence/document-render-check-v4.json">렌더·상호작용 검사</a> · <a href="index-v3.html">이전 v3</a></p>')
page = head + "".join([revision,story,decision,combat,flow,lobby,growth,campaign,economy,production,revenue,review]) + '</main><footer class="wrap">2026-10-21 · planning-v5.1 · 타이밍·성장·버튼 v5.1 게임 적용 · 연출10 적용 기록 보존<br><a href="#top">처음으로</a></footer></body></html>'
(ROOT / "index.html").write_text(page,encoding="utf-8")
print("Built Planning/index.html from planning-v5 decisions; historical presentation-v10 preserved")
