"""Build the dialogue planning section and click-through review from catalog source."""
from pathlib import Path
import html
import json

ROOT = Path(__file__).resolve().parents[1]
B = ROOT / "Planning/Story/Dragon-v1"
catalog = json.loads((ROOT / "GameData/mvp-catalog.json").read_text(encoding="utf-8-sig"))
scenes = sorted(catalog["stories"], key=lambda x: x["stage"])
names = {x["id"]: x["displayName"] for x in catalog["characters"]}
e = html.escape
labels = {2: "토리 · 처음 만난 길잡이", 7: "리나 · 준비를 챙기는 마법사", 10: "토리 · 숲길로 출발", 15: "바룬 · 점심을 나르는 운반꾼", 20: "기블과 도토 · 광산으로 가는 길"}

rows = "".join(f'<tr><td>Stage {x["stage"]}</td><td>{labels[x["stage"]]}</td><td>{len(x["lines"])}페이지</td><td>{"보스 뒤" if x["kind"] == "boss" else "처치 후 만남"}</td></tr>' for x in scenes)
section = f'''<section id="dialogue-v4"><p class="eyebrow">DIALOGUE V4 · 캐릭터의 이름과 얼굴</p>
<h2>“어이! 잠깐만!” 길 위에서 친구를 만납니다.</h2>
<p>한 사람이 말할 때마다 이름과 초상화가 함께 바뀝니다. 루카·다렌·미엘은 같은 질문에도 각자의 말투로 답합니다. 아래는 현재 1~20스테이지에 연결하는 대사이며, 전체 여정의 30·40·50 보스 대사는 후속 기획입니다.</p>
<div class="grid2"><article class="card"><img src="Production/Portraits-v1/tori.png" width="144" height="144" style="image-rendering:pixelated;float:left;margin:0 20px 12px 0" alt="토리 초상화"><h3>토리</h3><p>“반가워! 난 토리야. 이 길에서 뭐 하고 있어?”</p><p>“용을 잡으러 간다고? 멋지다! 나도 같이 갈래!”</p></article>
<article class="card"><h3>같은 만남, 다른 대답</h3><p><b>루카</b> “나는 루카야. 잘 부탁해, 토리.”</p><p><b>다렌</b> “난 다렌이야. 내 뒤로 따라와. 길은 네가 알려 주고!”</p><p><b>미엘</b> “나는 미엘이야. 그럼 길 안내는 토리에게 맡길게!”</p></article></div>
<div class="scroll"><table><thead><tr><th>진행</th><th>만남</th><th>분량</th><th>표시 시점</th></tr></thead><tbody>{rows}</tbody></table></div>
<p>토리의 첫 호출은 <b>???</b>와 실루엣으로 보여 주고, 다음 줄에서 이름과 얼굴을 공개합니다. 리나·바룬·도토는 처음부터 얼굴을 보여 주되 소개 전 화자명은 <b>???</b>로 표시합니다. 이름은 본문 왼쪽 위, 초상화는 왼쪽에 고정합니다. 대화 중에는 전투가 멈추며 <b>다음</b>과 <b>건너뛰기</b>로 진행합니다.</p>
<p class="small">새 만남은 캐릭터별 최초 1회입니다. 대화 중 저장 후에는 같은 페이지부터 이어집니다. 만남 자체로 유물·보석·체크포인트를 더 주지 않습니다. 토리의 동행은 이야기 속 길 안내이며 동료 자동 공격은 없습니다.</p>
<div class="actions"><a href="dialogue-v4.html">전체 대사·선택 캐릭터 미리보기</a><a href="dialogue-v4.md">배치·말투·저장 동작</a><a href="dialogue-v4-wireframe.svg">1280×720 와이어프레임</a></div></section>'''
(B / "dialogue-v4-section.html").write_text(section, encoding="utf-8")

payload = json.dumps({"scenes": scenes, "names": names}, ensure_ascii=False).replace("<", "\\u003c")
page = '''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>길에서 만나는 친구들 · 대사 v4</title><style>
*{box-sizing:border-box}body{margin:0;background:#f4ecd9;color:#273d45;font-family:system-ui,sans-serif}main{max-width:1160px;margin:auto;padding:28px}a{color:#287780}h1{margin:16px 0}label{display:inline-flex;gap:8px;align-items:center;margin:8px 18px 14px 0}select,button{font:inherit;padding:12px;border:2px solid #71543f;background:#fff9e9;color:#273d45;min-height:48px;border-radius:0}button:disabled{opacity:.4}button{cursor:pointer}.screen{aspect-ratio:16/9;position:relative;background:#b2d4c3;border:4px solid #273d45;overflow:hidden}.backdrop{width:100%;height:100%;object-fit:cover;image-rendering:pixelated;opacity:.75}.dialogue{position:absolute;bottom:0;left:0;right:0;display:grid;grid-template-columns:144px 1fr 144px;gap:32px;align-items:start;background:#fff1d1;border-top:4px solid #71543f;padding:18px 28px;min-height:184px}.portrait{width:144px;height:144px;image-rendering:pixelated;background:#fff9e9;box-shadow:0 0 0 4px #71543f}.name{font-weight:800;font-size:28px;margin:0 0 10px}.line{font-size:28px;line-height:1.35;margin:0}.next{align-self:end;height:76px}.outside{display:flex;gap:16px;align-items:center;margin-top:16px}.scope{font-size:15px;color:#5c695f}.voice{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}.voice p{padding:16px;background:#fff9e9}.caption{font-size:14px;color:#5c695f;margin-top:12px}@media(max-width:800px){main{padding:16px}.screen{aspect-ratio:auto;height:400px}.dialogue{grid-template-columns:96px 1fr;gap:20px;padding:16px;min-height:210px}.portrait{width:96px;height:96px}.name{font-size:22px}.line{font-size:20px}.next{grid-column:2;height:48px}.voice{grid-template-columns:1fr}.outside{flex-wrap:wrap}}
</style></head><body><main><a href="index.html#dialogue-v4">← 기획서</a><h1>길에서 만나는 친구들</h1><p>단계와 주인공을 골라 이름·얼굴·말투가 함께 바뀌는 대사를 확인합니다.</p>
<label>장면 <select id="scene"></select></label><label>주인공 <select id="hero"></select></label>
<div class="screen"><img class="backdrop" src="images/meadow-pixel-v3.png" alt="밝은 초원 배경 시안"><div class="dialogue"><img class="portrait" id="portrait" alt=""><div><p class="name" id="speaker"></p><p class="line" id="line"></p></div><button class="next" id="next">다음</button></div></div>
<p class="caption">대사 배치 검토용 HTML입니다. Unity 실행 캡처는 아니며, 실제 게임은 저장 프리팹의 배치를 사용합니다.</p><div class="outside"><button id="prev">이전</button><span id="progress"></span><button id="restart">처음부터</button></div>
<h2>선택한 모험가의 말투</h2><div class="voice"><p><b>루카</b><br>담백하고 성실하게. 필요한 약속을 짧게 합니다.</p><p><b>다렌</b><br>든든하고 호탕하게. 상대를 챙기는 말이 앞섭니다.</p><p><b>미엘</b><br>호기심 많고 밝게. 관찰한 것을 반갑게 말합니다.</p></div>
<p class="scope">현재 1~20: 토리(2)·리나(7)·토리(10)·바룬(15)·기블/도토(20). 30·40·50의 재회와 용의 결말은 <a href="story.json">전체 스토리 기획</a>입니다. 웹 미리보기는 저장 데이터를 사용하지 않습니다.</p></main>
<script id="data" type="application/json">__PAYLOAD__</script><script>
const data=JSON.parse(document.getElementById('data').textContent),scene=document.getElementById('scene'),hero=document.getElementById('hero');let page=0;
for(const x of data.scenes){const o=document.createElement('option');o.value=x.stage;o.textContent='Stage '+x.stage+' · '+(x.kind==='boss'?'보스 뒤 대화':'길 위의 만남');scene.append(o)}
for(const [id,name] of Object.entries(data.names)){const o=document.createElement('option');o.value=id;o.textContent=name;hero.append(o)}
function render(){const s=data.scenes.find(x=>x.stage===Number(scene.value)),l=s.lines[page],player=l.speaker==='player',response=l.responses.find(x=>x.characterId===hero.value);document.getElementById('speaker').textContent=player?data.names[hero.value]:l.speaker;document.getElementById('line').textContent=response?response.text:l.text;const p=document.getElementById('portrait');p.src='Production/Portraits-v1/'+(player?hero.value:l.portraitId)+'.png';p.alt=(player?data.names[hero.value]:l.speaker)+' 초상화';document.getElementById('progress').textContent=(page+1)+' / '+s.lines.length;document.getElementById('prev').disabled=page===0;document.getElementById('next').disabled=page===s.lines.length-1;document.getElementById('next').textContent=page===s.lines.length-1?'대화 끝':'다음'}
scene.onchange=()=>{page=0;render()};hero.onchange=render;document.getElementById('prev').onclick=()=>{if(page>0)page--;render()};document.getElementById('next').onclick=()=>{const s=data.scenes.find(x=>x.stage===Number(scene.value));if(page<s.lines.length-1)page++;render()};document.getElementById('restart').onclick=()=>{page=0;render()};render();
</script></body></html>'''.replace("__PAYLOAD__", payload)
(B / "dialogue-v4.html").write_text(page, encoding="utf-8")

notes = """# 대사 v4 · 길에서 만나는 친구들

현재 제작 계약: 1~20. 21~50은 후속 기획이다. 실행 문구의 단일 원본은 `GameData/mvp-catalog.json`의 stories이며 이 문서는 그 원본에서 생성한다.

## 만남과 말투

- Stage 2 처치 후 토리: ???와 실루엣으로 부른 뒤, 이름과 얼굴을 밝힌다. 반갑고 능청스럽게 길잡이를 자청한다.
- Stage 7 처치 후 리나: 가려운 풀을 피하게 도와주고 물을 챙기라고 한다. 차분하고 구체적이다.
- Stage 10 큰엄니 뒤 토리: 함께 숲으로 간다. 길 안내를 맡으며 무거운 짐을 부탁한다.
- Stage 15 큰 버섯 뒤 바룬: 광부들의 점심을 나르던 운반꾼. 호탕하게 고마워하며 기블의 통행세를 알려준다.
- Stage 20 기블 뒤 도토: 기블은 허세를 내려놓고 물러난다. 도토는 퉁명스럽지만 친절하게 갱도 길을 알려준다.

루카는 담백하고 성실하다. 다렌은 호탕하게 상대를 챙긴다. 미엘은 호기심 많고 밝다. 주인공 줄은 speaker와 portraitId를 player로 두고, responses에 세 안정적 ID on/dam/rin의 문장을 모두 둔다. 런타임은 선택한 ID로 이름·초상화·문장 세 가지를 함께 해석한다.

## 저장 프리팹 배치

16:9 SafeRoot는 960×540 논리 좌표를 쓴다. 아래 표는 실제 1280×720 캡처의 화면 픽셀로 환산한 값이며, 원점은 bottom-left다. 640×360에서는 각 값의 절반이다. 대화는 전투 입력을 닫은 상태에서만 나타난다.

| 요소 | 위치/크기 | 표시 규칙 |
|---|---|---|
| 하단 패널 | x0, y0, w1280, h245.3 | 양피지색 + 짙은 위쪽 경계 |
| 초상화 | x37.3, y24, w192, h192 | 48×48 원본의 4배, Point, 밝은 배경·도트 테두리 |
| 화자명 | x272, y184, w762.7, h48 | 본문 왼쪽 위, 짙은 글씨 |
| 본문 | x272, y32, w762.7, h144 | 공통 도트 폰트 배율, 1~2줄, 자동 축소 없음 |
| 다음 | center(1157.3,101.3), 192×128 | 640×360에서 96×64, 다른 입력 영역과 겹치지 않음 |
| 건너뛰기 | top-right offset(-122.7,-85.3), 192×128 | 이 장면만 종료, 보상 선택 대신 수행하지 않음 |

실행 화면의 폰트는 FeedbackLayout의 공통 도트 폰트 배율을 적용한다. 위 수치는 데스크톱 가로 화면 기준이며 모바일 실제 Safe Area는 별도 검증 대상이다. HTML 미리보기는 문구·화자 교체 검토용이며 Unity 캡처가 아니다.

## 동작과 복구

- 기존 EnemyDefeat→StoryDialogue→Travel 흐름 사용. 일반 만남은 새 보상이나 체크포인트를 생성하지 않는다.
- 각 캐릭터의 seenStoryIds로 장면별 최초 완료/건너뛰기만 기록한다. 대화 중 저장은 sceneId/lineIndex를 보존한다.
- 소개 전 첫 발화는 ???로 표시한다. 토리만 화면 밖 호출의 unknown 실루엣을 쓰고, 리나·바룬·도토는 실제 얼굴을 먼저 보여 준다. unknown은 누락 초상화를 감추는 fallback이 아니다.
- 초상화는 on/dam/rin/tori/rina/barun/doto/gib/unknown 9개. 저장 프리팹의 Portraits 배열과 StoryPortrait Image에 연결한다. 런타임에서 UI 계층을 새로 만들지 않는다.
- 30 바룬 재회, 40 리나 재회, 50 브라크 패배와 열린 길 엔딩은 story.json의 미래 구역 기획이다. 브라크 초상화는 그 구역 제작 시 추가한다.

## 현재 대사 원문
"""
for scene in scenes:
    notes += f'\n### Stage {scene["stage"]} · {labels[scene["stage"]]}\n\n'
    for index, line in enumerate(scene["lines"], 1):
        if line["speaker"] == "player":
            for response in line["responses"]:
                notes += f'{index}. {names[response["characterId"]]}: {response["text"]}\n'
        else:
            notes += f'{index}. {line["speaker"]}: {line["text"]}\n'
(B / "dialogue-v4.md").write_text(notes, encoding="utf-8")
print("Built dialogue v4: 5 scenes, 25 pages, 3 hero response sets.")
