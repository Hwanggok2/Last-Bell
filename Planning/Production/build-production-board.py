"""Rebuild the local production review page from the owned roster and verification artifacts."""
from pathlib import Path
import html
import json

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
roster = json.loads((HERE / 'world-art-roster.json').read_text(encoding='utf-8'))
e = html.escape
zones = ''
for zone in roster['zones']:
    monsters = [m for m in roster['roster'] if m['zoneId'] == zone['id']]
    rows = ''.join(f'<tr><td>{m["targetStage"]}</td><td>{e(m["displayName"])}</td><td>{e(m["silhouette"])}</td><td>{e(m["weapon"])}</td></tr>' for m in monsters)
    zones += f'''<article class="zone"><span class="eyebrow">STAGE {zone['firstStage']:02}—{zone['lastStage']:02}</span>
    <h3>{e(zone['displayName'])}</h3><p>{e(zone['landmark'])}</p><p class="muted">{e(zone['midground'])}<br>{e(zone['farBackground'])}</p>
    <details><summary>몬스터 10종 명단 · 제작 설계</summary><table><thead><tr><th>층</th><th>이름</th><th>실루엣</th><th>무기</th></tr></thead><tbody>{rows}</tbody></table></details></article>'''

captures = [
 ('03-stage-1','첫 전투'),('06-boss','10층 보스'),('16-critical-hit','치명타·도트 파편'),('07-boss-reward','3개 중 하나 선택'),
 ('08-stage-11-continuation','10층 이후 자동 진행'),('10-upgrade','캐릭터 강화'),
 ('12-relic-equipped','유물 장착'),('13-gold-exchange','부족한 골드 교환')]
screens = ''.join(f'<figure><a href="../Evidence/Unity/Smoke/{name}.png"><img src="../Evidence/Unity/Smoke/{name}.png" alt="{label}" loading="lazy"></a><figcaption>{label} · 실제 Unity 실행 화면</figcaption></figure>' for name,label in captures if (HERE.parent/'Evidence/Unity/Smoke'/f'{name}.png').exists())
smoke = HERE.parent/'Evidence/Unity/Smoke/smoke-result.json'
verified = smoke.exists() and json.loads(smoke.read_text(encoding='utf-8')).get('passed')
status = 'Windows 실행 검증 통과' if verified else '실행 검증 진행 중'
page = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>마지막 종지기 · Unity MVP 제작실</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#0b1423;color:#f0ddaf;font-family:system-ui,"Malgun Gothic",sans-serif;line-height:1.7}a{color:#f9c765}nav{padding:18px 5vw;border-bottom:1px solid #314157;display:flex;gap:24px;align-items:center;flex-wrap:wrap}nav strong{margin-right:auto}main{max-width:1240px;margin:auto;padding:36px 28px 70px}.hero{padding:58px 38px 42px;background:linear-gradient(90deg,#0b1423ee,#0b142377),url('../../Client/Assets/Game/Art/Source/gate-background-v1.png') center/cover;border:1px solid #314157;min-height:380px}.eyebrow{letter-spacing:.14em;color:#dfb564;font-size:12px;font-weight:700}h1{font-size:clamp(30px,4vw,58px);line-height:1.16;margin:14px 0}h2{font-size:26px;margin:42px 0 14px}h3{font-size:23px;margin:8px 0}.hero p{max-width:680px}.muted,figcaption{color:#acb9cb;font-size:14px}.pill{display:inline-block;border:1px solid #8e774d;border-radius:20px;padding:4px 14px;font-size:13px}.actions{display:flex;gap:14px;flex-wrap:wrap;margin-top:26px}.actions a{padding:11px 20px;border:1px solid #c3a068;text-decoration:none;background:#0c1728d9}.actions a:first-child{color:#111d2b;background:#f0d498}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}.zone{padding:23px;background:#122238;border:1px solid #2d405a}.zone:last-child{grid-column:1/-1}.zone p{margin:7px 0}figure{margin:0;background:#122238;border:1px solid #2d405a;overflow:hidden}figure img{display:block;width:100%;height:auto;background:#14263c;image-rendering:pixelated}figcaption{padding:12px 18px}.sheet{padding:22px;background:repeating-conic-gradient(#14263c 0% 25%,#192d46 0% 50%) 0 0/28px 28px}.sheet img{background:none}.full{margin:18px 0}.note{border-left:3px solid #dfb564;padding:12px 20px;background:#152337;margin:24px 0}.flow{display:flex;gap:10px;flex-wrap:wrap;margin:22px 0}.flow span{padding:10px 16px;background:#1a2e47;border-bottom:2px solid #d5ab5b}details{margin-top:18px}summary{cursor:pointer;color:#f5cf7d}table{width:100%;font-size:13px;border-collapse:collapse;margin-top:15px}td,th{border-bottom:1px solid #33445d;padding:8px;text-align:left;vertical-align:top}th{color:#c4d3e6}footer{margin-top:40px;color:#a8b5c8;font-size:13px}code{background:#22344d;padding:2px 6px}@media(max-width:760px){.grid{grid-template-columns:1fr}.hero{padding:28px 22px}main{padding:22px 14px}nav{gap:14px}.zone{overflow:auto}.zone:last-child{grid-column:auto}}
</style><nav><strong>마지막 종지기 · 제작실</strong><a href="../index.html">기획 v4</a><a href="#play">실행 화면</a><a href="#art">아트 초안</a><a href="#world">5구역·50종</a></nav><main>
<section class="hero"><span class="eyebrow">UNITY 6000.3.15f1 · MVP 0.1</span><h1>멎은 종을,<br>다시 울려라.</h1><p>방향을 읽고 피한 뒤 빈틈을 공격하는 가로형 도트 액션.<br>첫10층의 전투·성장 루프를 Unity 실행본으로 연결합니다.</p>
<span class="pill">STATUS_PLACEHOLDER</span><div class="actions"><a href="../../Builds/LastBellMvp-Windows.zip">Windows 실행본 ZIP</a><a href="../../Builds/Android/LastBellMvp.apk">Android 테스트 APK</a><a href="../../Client/README.md">Unity 열기·조작 방법</a><a href="world-art-plan.md">구역·모션 제작 계획</a></div></section>
<div class="flow"><span>첫 실행 → 회피·공격 튜토리얼</span><span>1~10층 전투</span><span>5·10층 유물 선택</span><span>11층 자동 진입</span><span>사망 → 강화·장착 → 재출전</span></div>
<div class="note">이번 범위: 온1명, 적5종, 배경1장, 첫10층 + 자동 진행 확인용11층. 12~50층·NG+·나머지 캐릭터·실제 결제는 후속 제작입니다. 11층 완료 알림은 MVP 범위 안내이며 최종 엔딩이 아닙니다.</div>
<h2 id="play">실제 실행 화면</h2><p class="muted">별도 테스트 저장으로 실제 버튼과 전투 명령을 실행해 캡처했습니다. 무적이나 강제 처치로 전투를 건너뛰지 않습니다.</p><div class="grid">SCREENS_PLACEHOLDER</div>
<h2 id="art">플레이어와 첫 구역 적</h2><p>제공한 레퍼런스의 남색 폐허와 황금 실루엣을 유지한 초안입니다. 온은 측면을 유지하고 점프·숙이기·후퇴·돌진으로 네 회피를 표현합니다.</p>
<figure class="full"><div class="sheet"><img src="../../Client/Assets/Game/Art/Source/on-poses-v1.png" alt="온의 대기, 공격, 네 회피 대표 자세"></div><figcaption>온 · 8개 대표 자세. 이동·복귀와 피격은 런타임 연출을 조합합니다.</figcaption></figure>
<figure class="full"><div class="sheet"><img src="../../Client/Assets/Game/Art/Source/gate-enemies-v1.png" alt="성문 파수병, 사슬 망령, 재의 불씨, 성문 수문장, 종의 기사"></div><figcaption>왼쪽부터 성문 파수병 · 사슬 망령 · 재의 불씨 · 성문 수문장 · 종의 기사. 위에서 대기 / 공격 / 기절. 총15개 대표 자세.</figcaption></figure>
<figure class="full"><img src="../../Client/Assets/Game/Art/Source/gate-background-v1.png" alt="첫 성문 구역 배경"><figcaption>성문 구역 배경 · 바닥 높이 약82%. 합성1장 초안.</figcaption></figure>
<div class="note">50종 모두 별도의 상·하·앞·뒤 회피를 만들지 않습니다. 적은 대기·공격 준비·공격·피격·기절·사망을 중심으로 제작하고 10개 동작 원형을 공유합니다. 색만 바꾼 적은 새 종으로 세지 않습니다. 아래50종은 제작 명단이며 완성 자산 수가 아닙니다.</div>
<h2 id="world">10층씩 다른 구역</h2><div class="grid">ZONES_PLACEHOLDER</div>
<footer>2026-10-21 · <a href="generated-asset-notes.md">아트 생성 기록·폰트 출처</a> · <a href="art-draft-review.md">아트 담당 검토</a> · <a href="ui-implementation-review.md">UI 담당 검토</a> · <a href="combat-implementation-review.md">전투 담당 검토</a> · <a href="data-implementation-review.md">데이터 담당 검토</a></footer></main></html>'''
page = page.replace('STATUS_PLACEHOLDER', status).replace('SCREENS_PLACEHOLDER', screens).replace('ZONES_PLACEHOLDER', zones)
(HERE/'index.html').write_text(page,encoding='utf-8')
print('Planning/Production/index.html rebuilt')
