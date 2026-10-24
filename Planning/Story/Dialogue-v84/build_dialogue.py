"""Create the reviewed v8.4 dialogue patch and a self-contained review page.

This writes planning artifacts only. The catalog is owned by the integrating agent.
"""
from pathlib import Path
from copy import deepcopy
import html
import json

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
catalog = json.loads((ROOT / 'GameData/mvp-catalog.json').read_text(encoding='utf-8-sig'))
# Keep the captured v8.3 baseline when the integrating agent has already updated the catalog.
baseline = OUT / 'before.json'
previous = json.loads(baseline.read_text(encoding='utf-8')) if baseline.exists() else {
    'onboarding': deepcopy(catalog['onboarding']), 'stories': deepcopy(catalog['stories'])}

onboarding = {
    'prologueTitle': '마지막 불씨',
    'prologueParagraphs': [
        '종이 멎은 뒤로,\n마을에는 아침이 오지 않았습니다.',
        '모두 잠든 마을의 성문 밖.\n등불 하나가 마지막 불씨를 지킵니다.',
        '당신은 그 불씨를 이어받은 종지기.\n등불을 밝히고 다섯 종을 깨우세요.',
        '첫 종은 닫힌 성문에 있습니다.\n잠든 이들에게 아침을 돌려주세요.',
    ],
}

def line(speaker, text, responses=None):
    return {'speaker': speaker, 'text': text,
            'responses': [{'characterId': key, 'text': value} for key, value in (responses or {}).items()]}

responses10 = {
    'on': '등불이 닿는 곳까지 가볼게.\n안에 있는 사람들을 찾아야 해.',
    'dam': '문은 내가 열겠다.\n안에 남은 사람들을 되찾아 오지.',
    'rin': '문 안에서 목소리가 들려.\n사슬 너머에서 누군가 기다리고 있어.',
}
responses20 = {
    'on': '그럼, 한 집도 빠뜨리지 않을게.\n다음 종까지 등불을 이어갈 거야.',
    'dam': '돌려받을 몫이 남아 있었군.\n사슬을 풀고, 모두의 아침을 되찾겠다.',
    'rin': '들려. 저마다 다른 목소리야.\n종을 울려서, 집으로 보내줄게.',
}
stories = [
    {'id': 'story_boss_10', 'stage': 10, 'bossId': 'bell_knight', 'lines': [
        line('종의 기사', '나는 마을을 지키는 줄 알았다.\n그런데 이 문은 주민들을 가두고 있었지.'),
        line('종의 기사', '첫 종을 울려, 문을 열어라.\n집마다 걸린 사슬을 따라가 보거라.'),
        line('선택한 종지기', responses10['on'], responses10),
    ]},
    {'id': 'story_boss_20', 'stage': 20, 'bossId': 'chain_jailer', 'lines': [
        line('매달린 집행관', '저 집의 사람들은 죽지 않았다.\n빼앗긴 내일 속에 잠들어 있지.'),
        line('매달린 집행관', '사슬로 거둔 시간이 탑에 남아 있다.\n이제 종을 울려, 집으로 돌려보내라.'),
        line('선택한 종지기', responses20['on'], responses20),
    ]},
]

notes = {
    'scope': '현재 구현된 프롤로그, 10층/20층 보스 처치 후 대사만 교체. 새로운 스토리 이벤트, 21층 이후 플레이, 별도 종 상호작용은 추가하지 않는다.',
    'sources': ['Planning/Story/story-world-v2.json', 'Planning/Story/route-onboarding-v5.json', 'GameData/mvp-catalog.json'],
    'preserve': [
        '불씨를 이어받아 등불을 밝히고 다섯 종을 울려 아침을 되찾는 큰 줄기.',
        '1~9 성 밖 길, 10 성문 문지기, 11 입구 왼쪽 성문, 12부터 침수된 사슬 주거구.',
        '주민은 죽거나 괴물이 된 것이 아니라 빼앗긴 시간 속에 잠들어 있다.',
        '프롤로그 4문단/18초/건너뛰기, 보스별 3페이지, 기존 다음/건너뛰기 및 최초 표시 정책.',
        '온·담·린은 동행 인물이 아닌 선택 주인공. 현재 온만 사용 가능하며 응답 변형이 새 캐릭터를 해금하지 않는다.',
        '20층은 프로토타입 끝. 다음 종 언급은 여정이 이어진다는 서사이며 다음 스테이지를 열지 않는다.',
    ],
    'continuityFixes': [
        {'issue': '현재 20층의 종이 울리면 다시 묶인다/추를 끝까지 멈춘다는 문구가 원작의 종을 깨워 반환한다는 목표를 뒤집는다.',
         'resolution': '사슬이 거둔 시간이 탑에 남았다는 사실을 드러내고, 종을 울려 집으로 돌려보내는 목표를 잇는다.'},
        {'issue': '현재 적 이름 익사한 주민은 주민이 죽지 않았다는 설정과 충돌한다.',
         'resolution': '같은 drowned_resident ID와 문짝 무기/젖은 외투를 유지하며 표시 이름 침수된 잔상을 권고. 사슬이 붙잡힌 기억을 흉내 낸 형상이며 주민의 시체가 아니다.',
         'catalogChange': '통합 담당자가 표시 이름을 별도로 반영해야 함. 이 패치는 enemies를 수정하지 않는다.'},
        {'issue': '성 밖 적이 야생 동물로 바뀌어 예전 초병/갑옷에 관한 초반 서술이 현재 적 구성과 맞지 않는다.',
         'resolution': '프롤로그는 길의 적 종류를 단정하지 않는다. 동물은 버려진 외곽 길의 주민이며 성문을 지키는 명령은 10층 기사에게 남긴다.'},
    ],
    'voices': {
        '종의 기사': '지키려다 가두었다는 책임을 인정하는 짧은 고백. 플레이어를 평가하거나 긴 역사를 설명하지 않는다.',
        '매달린 집행관': '명령에 묶인 관리자의 기억. 무엇을 거두었고 어디에 남았는지 담담하게 밝힌다.',
        '온': '눈앞의 등불에서 사람의 집으로 관심이 넓어진다. 해볼게/이어갈 거야처럼 직접 행동을 약속한다.',
        '담': '문을 열고 사람을 되찾는 책임과 결단. 하겠다 체.',
        '린': '실제로 들리는 사람마다 다른 목소리를 말한다. 설명을 대신하는 초능력적 만능 해설은 피한다.',
    },
    'copyRoles': {'prologue': '짧은 서사 + 여정 목적', 'boss10': '반전 + 다음 구역으로 이동할 이유', 'boss20': '진실의 일부 + 종을 계속 울릴 이유'},
    'display': {'referenceResolution': [960, 540], 'minimumVerificationResolution': [640, 360],
                'storyTextBounds': '기존 하단 패널: 가로 화면의 4~79%, 아래 22~99px 영역(77px 높이). 실제 레이아웃의 폰트 크기를 유지.',
                'prologueTextBounds': '기존 두루마리 ReadingWindow 504×286, 문단 영역 504×120(960×540 기준).',
                'linePolicy': '모든 본문은 강제 2행. 프롤로그 행당 최대 20자, 대사 최대 22자(공백 포함).',
                'verificationLimit': '첨부 이미지는 문구 배치 검수용 합성. 실제 Unity 렌더 검수는 통합 빌드에서 수행.'},
    'uiCopySuggestions': {
        'staggerReady': '공격하면 기절!',
        'staggerHint': '게이지가 가득 차면 공격을 명중시켜 기절시키세요.',
        'relicAutomatic': '획득한 유물은 자동으로 적용됩니다.',
        'relicFull': '유물 보관함이 가득 찼습니다.',
        'relicExpand': '보관함 확장',
        'relicExpandExample': '보관 한도 5 → 6 · 종 파편 3',
        'caution': '확장 예시의 숫자는 밸런스 제안이 아님. UI에서는 실제 한도·가격을 바인딩해야 함.',
    },
}

all_text = [('prologue', s) for s in onboarding['prologueParagraphs']]
for story in stories:
    for n, item in enumerate(story['lines']):
        all_text.append((f'{story["id"]}.{n}', item['text']))
        for response in item['responses']:
            all_text.append((f'{story["id"]}.{n}.{response["characterId"]}', response['text']))
assert [s['id'] for s in stories] == [s['id'] for s in previous['stories']]
assert all(len(s['lines']) == 3 for s in stories)
assert all(len(text.split('\n')) == 2 for _, text in all_text)
assert max(len(x) for _, text in all_text for x in text.split('\n')) <= 22
review = {
    'checks': {
        'same_story_ids_and_stage_boss_mapping': [(s['id'], s['stage'], s['bossId']) for s in stories] == [(s['id'], s['stage'], s['bossId']) for s in previous['stories']],
        'four_prologue_paragraphs': len(onboarding['prologueParagraphs']) == 4,
        'three_pages_per_boss': all(len(s['lines']) == 3 for s in stories),
        'all_text_two_lines': True,
        'prologue_max_line_chars_20': max(len(x) for p in onboarding['prologueParagraphs'] for x in p.split('\n')) <= 20,
        'dialogue_max_line_chars_22': True,
        'original_bell_and_lantern_goal_preserved': True,
        'resident_survival_preserved': True,
        'no_new_playable_stages_or_story_triggers': True,
    },
    'lineLengths': [{'id': key, 'lineCharacters': [len(x) for x in text.split('\n')]} for key, text in all_text],
    'reviewedBy': 'story_dialogue parallel agent; editorial self-review, not independent player testing',
    'runtimeRender': 'Pending integrating agent. Mockups are not Unity captures.',
}
# A separate verifier uses Pillow from the supplied runtime to measure the real
# font. Regenerating prose should not erase its evidence when text is unchanged.
prior_review = OUT / 'review.json'
if prior_review.exists():
    old_review = json.loads(prior_review.read_text(encoding='utf-8'))
    if old_review.get('lineLengths') == review['lineLengths'] and 'glyphWidthMeasurement' in old_review:
        review['glyphWidthMeasurement'] = old_review['glyphWidthMeasurement']
patch = {'version': 'dialogue-v8.4', 'date': '2026-10-23', 'status': 'reviewed-for-integration', 'onboarding': onboarding, 'stories': stories, 'notes': notes, 'review': review}
for name, value in [('dialogue-patch.json', patch), ('before.json', previous), ('review.json', review)]:
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def esc(text):
    return html.escape(text).replace('\n', '<br>')

def block(text):
    return '<p class="copy">' + esc(text) + '</p>'

def compare(before, after, title):
    return '<article class="compare"><h3>' + esc(title) + '</h3><div class="two"><div class="before"><small>현재 v8.3</small>' + block(before) + '</div><div class="after"><small>수정 문구</small>' + block(after) + '</div></div></article>'

parts = ['''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>마지막 종지기 · 대사 다듬기 v8.4</title>
<style>@font-face{font-family:neo;src:url('../../../Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf')}*{box-sizing:border-box}body{margin:0;background:#101b25;color:#e6e0cf;font:16px/1.8 system-ui,sans-serif}main{max-width:1140px;margin:auto;padding:36px 24px 90px}h1{font:36px/1.4 neo;color:#e9c67b}h2{font-size:25px;color:#e9c67b;margin:52px 0 18px}h3{margin:0 0 12px;font-size:18px}a{color:#9ed6cf}nav{display:flex;gap:20px;flex-wrap:wrap}small,.muted{color:#a1b3bf}.tag{padding:4px 12px;background:#29433e;color:#bddfc5}.callout{background:#18312f;border-left:4px solid #e4ba6c;padding:18px 24px}.compare{margin:20px 0;border:1px solid #364752;padding:18px}.two{display:grid;grid-template-columns:1fr 1fr;gap:20px}.before,.after{padding:16px;background:#172632}.after{background:#20332f}.copy{font:22px/1.7 neo;margin:10px 0;overflow-wrap:anywhere}.note{color:#b1c3cc;font-size:14px}table{width:100%;border-collapse:collapse}th,td{text-align:left;border-bottom:1px solid #364752;padding:12px;vertical-align:top}.screen{width:100%;image-rendering:pixelated;border:1px solid #62746f}.preview{position:relative;aspect-ratio:16/9;background:#142533;overflow:hidden}.preview>img{width:100%;height:100%;object-fit:cover;image-rendering:pixelated}.dialogue{position:absolute;left:0;right:0;bottom:0;height:26.852%;background:rgba(3,7,13,.97);padding:0 4%;display:flex;flex-direction:column;justify-content:center}.dialogue:before{content:'';position:absolute;left:2.5%;right:2.5%;top:0;height:2px;background:#b09966}.speaker{font:clamp(10px,1.6vw,18px) neo;color:#dec588;margin-bottom:7px}.dialogue .copy{font-size:clamp(12px,2vw,24px);line-height:1.55;margin:0}.buttons{display:flex;gap:8px;flex-wrap:wrap;margin:12px 0}button{font:16px neo;background:#263e45;color:#e7e1cf;border:1px solid #6e9a99;padding:9px 14px;cursor:pointer}button[aria-pressed=true]{background:#526f5a;border-color:#e9c67b}.legend{font-size:14px;color:#b0c4ca;margin:10px 0 24px}.voice{border-left:3px solid #697f83;padding-left:14px}.prologue{padding:28px;background:#d0b782;color:#352e23;text-align:center}.prologue .copy{font-size:22px}details{margin-top:16px}summary{cursor:pointer;color:#c1d9d4}@media(max-width:720px){main{padding:24px 16px 60px}.two{grid-template-columns:1fr}h1{font-size:30px}.copy{font-size:20px}.dialogue .copy{font-size:clamp(9px,2.4vw,17px)}.speaker{font-size:clamp(8px,1.8vw,14px)}} </style>
<main><span class="tag">v8.4 · 문구 검수본</span><h1>불씨를 잇고, 종을 울려<br>잠든 마을에 아침을 돌려준다</h1>
<p>기존 줄거리를 유지하면서 첫 목표, 성문의 반전, 사슬의 진실이 한 방향으로 이어지도록 다듬었다. 프롤로그 4문단과 현재 구현된 10·20층 대사 3페이지씩만 교체한다.</p>
<nav><a href="#prologue">프롤로그</a><a href="#gate">10층 성문</a><a href="#chain">20층 주거구</a><a href="#continuity">설정 정합성</a><a href="#review">검수</a></nav>
<div class="callout"><b>반전은 여정의 목적을 더 깊게 만든다.</b><br>처음에는 길을 밝히려 종을 울린다. 사슬의 진실을 알게 된 뒤에는 그 빛이 돌아갈 사람들의 집을 생각하며 종을 울린다. 지금까지 켠 등불이 사람을 해쳤다는 반전은 넣지 않는다.</div>
<h2 id="prologue">프롤로그 · 마지막 불씨</h2><p class="muted">기존 18초 자동 스크롤과 건너뛰기를 유지. 새로운 버튼이나 설명 페이지를 추가하지 않는다.</p>''']
for i, (before, after) in enumerate(zip(previous['onboarding']['prologueParagraphs'], onboarding['prologueParagraphs'])):
    parts.append(compare(before, after, f'문단 {i + 1}'))
parts.append('<details><summary>수정 프롤로그를 이어 읽기</summary><div class="prologue">' + ''.join(block(p) for p in onboarding['prologueParagraphs']) + '</div></details>')
for story, old in zip(stories, previous['stories']):
    section = 'gate' if story['stage'] == 10 else 'chain'
    subtitle = '지키던 문이 사실은 주민을 가둔 문이었다' if story['stage'] == 10 else '사라진 것이 아니라 빼앗긴 내일 속에 잠들어 있다'
    parts.append(f'<h2 id="{section}">{story["stage"]}층 · {subtitle}</h2>')
    for i, (new_line, old_line) in enumerate(zip(story['lines'], old['lines'])):
        parts.append(compare(old_line['text'], new_line['text'], f'{i + 1}. {new_line["speaker"]}'))
    parts.append(f'<div class="preview" data-story="{story["id"]}"><img alt="기존 {story["stage"]}층 게임 장면을 사용한 문구 배치 시안" src="../../Evidence/Feedback-v83/desktop/v83-stage-{story["stage"]}.png"><div class="dialogue"><div class="speaker"></div><p class="copy"></p></div></div><div class="buttons" data-buttons="{story["id"]}"></div><p class="legend">문구 배치 시안. 배경은 기존 v8.3 캡처이며, 새 대사와 합성한 화면이다. 실제 Unity 캡처가 아니다.</p>')
    parts.append('<div class="two">')
    for response in story['lines'][-1]['responses']:
        names = {'on': '온 · 사람들을 찾아가는 목소리', 'dam': '담 · 책임지는 목소리', 'rin': '린 · 사람들의 소리를 듣는 목소리'}
        parts.append('<div class="voice"><small>' + names[response['characterId']] + '</small>' + block(response['text']) + '</div>')
    parts.append('</div>')
parts.append('''<h2 id="continuity">스토리와 몬스터가 같은 이야기를 하도록</h2><table><thead><tr><th>구간</th><th>몬스터의 의미</th><th>유지할 단서</th></tr></thead><tbody>
<tr><td>1~9 · 성 밖</td><td>사람의 발길이 끊긴 외곽 길의 들개·늑대·불곰. 모두 갑옷이나 기계로 바꾸지 않는다.</td><td>꺼진 등불과 닫힌 성벽. 첫 큰 목표는 10층 성문의 종.</td></tr>
<tr><td>10 · 성문</td><td>기존 종의 기사. 보호 명령을 따르면서 사람을 가둔 문을 지켜 왔다.</td><td>문은 안에서 잠겼다. 패배 후 원래 주민의 편이었던 목소리가 돌아온다.</td></tr>
<tr><td>11~19 · 침수 주거구</td><td>하수구 쥐는 야생동물, 물안개 망령은 붙잡힌 시간의 형상. 기존 ‘익사한 주민’ 외형은 <b>침수된 잔상</b>으로 정리해 살아 있는 주민을 죽이는 인상을 피한다.</td><td>집 문패·사슬·등불. 젖은 외투와 문짝 무기는 잔상이 흉내 낸 생활의 흔적이다.</td></tr>
<tr><td>20 · 집행관</td><td>시간 징수 명령에 묶인 관리자의 기억. 기존 보스 외형을 유지한다.</td><td>주민은 죽지 않았다. 탑에 묶인 시간을 집으로 돌려보내야 한다.</td></tr></tbody></table>
<p class="note">‘침수된 잔상’은 표시 이름 변경 권고이며 ID·전투 역할·비주얼의 기본 재료는 유지한다. 이 문서의 패치는 적 데이터나 배경을 직접 수정하지 않는다.</p>
<h3>이번에 드러내지 않을 것</h3><p>불씨를 남긴 이전 종지기의 정체, 시왕의 상실에 대한 두려움, 마지막 종의 결말은 기존 후반 설정으로 남긴다. 20층에서 설명을 몰아 넣거나 동행 인물을 새로 만들지 않는다. 프로토타입의 실제 진행 범위는 20층까지다.</p>
<h2 id="review">검수와 반영 범위</h2><table><tbody>
<tr><th>목표</th><td>프롤로그에서 등불 → 다섯 종 → 아침, 첫 목적지 성문까지 모두 명시.</td></tr>
<tr><th>문장 길이</th><td>모든 본문 2행. 프롤로그 한 행 최대 20자, 대사 한 행 최대 22자. 공백 포함. 두 보스는 각각 3페이지를 유지.</td></tr>
<tr><th>같은 사건, 다른 말투</th><td>온은 작은 빛과 사람, 담은 책임과 행동, 린은 목소리를 말한다. 선택하지 않은 주인공이 장면에 동행하지 않는다.</td></tr>
<tr><th>현재 빌드와 연결</th><td>기존 story_boss_10 / story_boss_20 ID 및 bossId 유지. 새 스토리 트리거나 추가 입력 없음.</td></tr>
<tr><th>아직 필요한 확인</th><td>통합 빌드의 960×540 / 640×360 실제 대사·스크롤 렌더 검수. 여기의 합성 시안으로 기기 검증을 대신하지 않는다.</td></tr></tbody></table>
<p><a href="dialogue-patch.json">정확한 반영용 JSON</a> · <a href="review.json">자체 검수 기록</a> · <a href="before.json">변경 전 문구</a></p>
<p class="note">근거: <a href="../story-world-v2.json">기존 세계관</a>, <a href="../route-onboarding-v5.json">기존 구간·온보딩</a>, <a href="../../../GameData/mvp-catalog.json">현재 게임 데이터</a>. 새 이야기로 교체하지 않고 목표와 표현의 충돌을 정리했다.</p>
</main><script>''')
parts.append('const stories=' + json.dumps(stories, ensure_ascii=False) + ';')
parts.append('''for(const story of stories){const panel=document.querySelector(`[data-story="${story.id}"]`),controls=document.querySelector(`[data-buttons="${story.id}"]`);const options=story.lines.map((line,i)=>({name:`${i+1}페이지`,speaker:line.speaker==='선택한 종지기'?'온':line.speaker,text:line.text}));for(const r of story.lines.at(-1).responses.filter(x=>x.characterId!=='on'))options.push({name:r.characterId==='dam'?'담 응답':'린 응답',speaker:r.characterId==='dam'?'담':'린',text:r.text});options.forEach((line,i)=>{const b=document.createElement('button');b.textContent=line.name;b.type='button';b.setAttribute('aria-pressed',i===0?'true':'false');b.onclick=()=>{controls.querySelectorAll('button').forEach(x=>x.setAttribute('aria-pressed','false'));b.setAttribute('aria-pressed','true');panel.querySelector('.speaker').textContent=line.speaker;panel.querySelector('.copy').replaceChildren(...line.text.split('\\n').flatMap((text,n)=>n?[document.createElement('br'),document.createTextNode(text)]:[document.createTextNode(text)]));};controls.append(b);if(i===0)b.click();});}</script></html>''')
(OUT / 'index.html').write_text('\n'.join(parts), encoding='utf-8')
print('Wrote dialogue-patch.json, before.json, review.json, index.html')
print('Characters including whitespace:', sum(len(t) for t in onboarding['prologueParagraphs']))
