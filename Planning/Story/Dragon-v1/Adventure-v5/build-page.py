"""Build the review page from the companion planning contract."""
import html,json
from pathlib import Path
B=Path(__file__).resolve().parent
data=json.loads((B/'companions-plan.json').read_text(encoding='utf-8'))
e=lambda s:html.escape(str(s))
services={s['id']:s for s in data['services']}
jobs={'tori':'여우 길잡이','barun':'짐꾼 · 바바리안','doto':'드워프 대장장이','rina':'바람 마법사','momo':'인간 약초사'}
route=[];dialogues=[];rows=[]
for n in data['companions']:
    portrait_path=f'portraits/{n["id"]}.png' if (B/'portraits'/f'{n["id"]}.png').is_file() else f'../Production/Companions-v5/portraits/{n["id"]}.png'
    portrait=f'<img class="pixel" src="{portrait_path}" width="96" height="96" alt="{e(n["name"])} 새24px 초상">' if (B/portrait_path).is_file() else '<p class="tag" style="margin:24px 0">아트 제작 예정</p>'
    route.append(f'<article class="card"><span class="eyebrow">STAGE {n["joinAfterStage"]}</span>{portrait}<strong>{e(n["name"])}</strong><p>{e(jobs[n["id"]])}</p><p class="small">{e(n["zone"])} · {n["zoneRange"][0]}–{n["zoneRange"][1]}</p></article>')
    pages=''.join(f'<p><b>{e(p["speaker"])}</b>{e(p["text"])}</p>' for p in n['dialogue'])
    dialogues.append(f'<article class="dialogue-lines"><h3>Stage {n["joinAfterStage"]} · {e(n["name"])}</h3>{pages}</article>')
    s=services[n['serviceId']];atk=n['attack']
    details=s['effect']
    if s['id']=='weapon_craft':
        details+=' · '+ ' / '.join(f'{r["rank"]}단계 {r["attackBonusPercent"]}%·{r["goldCost"]:,}G' for r in s['ranks'])
    elif s['id']=='route_guide':details+=' · 무료'
    elif s['id']=='pack_cart':details+=' · 4 / 8보석'
    elif s['id'] in ['travel_blessing','herbal_tea']:details+=' · 출발 스테이지 일반 골드의10% 비용(올림)'
    rows.append(f'<tr><td>{e(n["name"])}</td><td><b>{e(s["name"])}</b><br>{e(details)}<br><span class="small">{e(s["duration"])}</span></td><td>{e(atk["name"])}<br>{atk["periodMs"]/1000:g}초 · Lv1 피해 {atk["baseDamageToPlayerBaseAttack"]*100:g}% A</td><td>골드로 Lv1~30<br>11·21 돌파<br><span class="small">A는 출전 시 기본 공격력</span></td></tr>')
s=(B/'page-template.html').read_text(encoding='utf-8')
for token,value in [('ROUTE',''.join(route)),('DIALOGUES',''.join(dialogues)),('SERVICES',''.join(rows))]:s=s.replace('__'+token+'__',value)
(B/'index.html').write_text(s,encoding='utf-8')
print('Adventure-v5 page generated from companion plan:5NPCs / 20 dialogue pages.')
