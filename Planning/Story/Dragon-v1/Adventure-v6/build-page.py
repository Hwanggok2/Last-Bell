"""Build the v6 planning page from approved contracts; never edits shared boards."""
from pathlib import Path
import json,html
B=Path(__file__).resolve().parent
plan=json.loads((B/'plan.json').read_text(encoding='utf-8'))
e=lambda value:html.escape(str(value))
def bounds(rows):
 return '<table><thead><tr><th>영역</th><th>1280×720 x / y / w / h</th><th>640×360</th></tr></thead><tbody>'+''.join('<tr><td>'+e(name)+'</td><td>'+e(' / '.join(map(str,rect)))+'</td><td>'+e(' / '.join(f'{v/2:g}' for v in rect))+'</td></tr>' for name,rect in rows)+'</tbody></table>'
g=plan['ui']['growth'];c=plan['ui']['companions']
growth=bounds([(name,g[key]) for name,key in [('뒤로','back'),('제목','title'),('동료 진입','companionsButton'),('스탯 5행 패널','statsPanel'),('상세 패널','detailPanel'),('유물 / 교환','relicOrExchange'),('강화','upgrade'),('Stage N','stage')]])
party=bounds([('동료 목록 / 편성',c['listPanel']),('동료 상세',c['detailPanel']),('재화',c['wallet'])]+[(name,[c['detailPanel'][0]+c[key][0],c['detailPanel'][1]+c[key][1],*c[key][2:]]) for name,key in [('전용 기술','skillNameRelative'),('기술 효과','skillEffectRelative'),('발동률 현재→다음','skillChanceRelative'),('동료 강화','upgradeRelative')]])
players=''.join(f'<article class="card"><img class="portrait" src="../Adventure-v5/portraits/{p["id"]}.png" width="96" height="96" alt="{e(p["name"])}"><span class="pill">{e(p["name"])}</span><h3>{e(p["skill"])}</h3><p><b>공격력의 {p["damagePercent"]}%</b><br>{e(p["additional"])}</p><p class="small">{e(p["effect"])}</p></article>' for p in plan['playerSkill']['skills'])
companions=''.join(f'<tr><td>{e(n["name"])}</td><td><b>{e(n["skill"])}</b><br><span class="small">{e(n["visual"])}</span></td><td>{n["skillDamagePercent"]}%</td><td>{e(n["additional"])}</td><td>{n["basePeriodMs"]/1000:g}초</td></tr>' for n in plan['companions'])
initial=''.join(f'<tr><td>{e(n["name"])}</td><td>12% →13%</td><td>{n["basePeriodMs"]/1000:.2f}초 →{n["basePeriodMs"]*.99/1000:.2f}초</td><td>{n["skillDamagePercent"]}% · 고정 배율</td></tr>' for n in plan['companions'])
pixel='<p>1280 기준 플레이어208×44 / 일반 적224×44 / 중간보스·보스560×44. 보스 바 중심 (640,38), 이름 중심 (900,84) / 440×40. HP y16~60, 이름 x680~1120/y64~104로 중앙 방향 예고와 분리하고, 리듬바 y108 이후로 4px 여백을 둡니다. 11행4px 정수격자·하트·계단 윤곽·밝은 윗셀, 양수HP는 최소1칸을 남깁니다.</p><p>피해 숫자는5×7 bitmap. 일반·동료6px 셀, 치명8px 셀, 어두운1셀 외곽. 크림·하늘색·노랑으로 구분하며0.85초 동안 우상단으로 정수 이동합니다. 80ms 안의 동료 피해는 청록 숫자 하나로 합산하고 일반 공격·스킬과 최대3줄로 분리합니다. 각 피격 효과는 유지하며, 숫자의 전체 이동 영역은 상단 기절 게이지 아래에 둡니다. 640에서는 각 치수를 정확히 절반으로 줄입니다.</p>'
vfx='<a href="../../../Production/dragon-v6-effects-handoff.md">파티클 인계</a>'
effects=''.join(f'<tr><td>{e(x["who"])}</td><td>{e(x["shape"])}</td><td>{e(x["end"])}</td></tr>' for x in plan['effects']['visuals'])
template=(B/'page-template.html').read_text(encoding='utf-8')
for key,value in {'GROWTH_BOUNDS':growth,'COMPANION_BOUNDS':party,'PLAYER_SKILLS':players,'COMPANION_SKILLS':companions,'INITIAL_GROWTH':initial,'PIXEL_SPEC':pixel,'VFX_LINK':vfx,'EFFECTS':effects}.items():template=template.replace('__'+key+'__',value)
(B/'index.html').write_text(template,encoding='utf-8');(B/'plan-data.js').write_text('window.DRAGON_V6_PLAN='+json.dumps(plan,ensure_ascii=False,separators=(',',':'))+';\n',encoding='utf-8')
print('v6 page generated: '+plan['statusLabel'])
