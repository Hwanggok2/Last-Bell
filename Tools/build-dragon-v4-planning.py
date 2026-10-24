"""Reflect the manual-skill / encounter contract on the existing local planning board."""
from pathlib import Path
import html, json

ROOT=Path(__file__).resolve().parents[1]
B=ROOT/'Planning/Story/Dragon-v1'
data=json.loads((ROOT/'GameData/mvp-catalog.json').read_text(encoding='utf-8'))
e=lambda v:html.escape(str(v))

def main():
    skills=data['skills']; chars={c['id']:c for c in data['characters']}
    records=[]; cards=[]
    for s in skills:
        name=chars[s['characterId']]['displayName'].split('·')[0].strip()
        records.append(dict(**s,characterName=name,activation='하단 중앙 스킬 버튼 / 키보드 E',artId=chars[s['characterId']]['artId']))
        cards.append(f'<article class="card"><h3>{e(name)} · {e(s["displayName"])}</h3><p><b>{e(s["description"])}</b></p><p>쿨타임 {s["cooldownMs"]//1000}초 · 직접 사용</p><p>'+({'on':'적이 기절했을 때 강한 한 번을 넣거나, 다음 공격을 기다리지 않고 처치를 노립니다.','dam':'다음 공격을 피하기 어려울 때 1회 보호막을 준비합니다. 적중해야 생기며, 3초가 지나거나 다음 피해를 흡수하면 사라집니다.','rin':'빈틈을 만들 기절 누적을 앞당깁니다. 일반 +50, 중간 보스 +30, 보스 +20. 100을 채우면 다음 타격으로 기절을 엽니다.'}[s['characterId']])+'</p></article>')
    contract=dict(version='dragon-skills-v2',dataVersion=data['dataVersion'],status='implemented-source-awaiting-integration-QA',skills=records,
        rules=dict(cooldownClock='combat-only',consume='accepted skill input',contact='one confirmed normal-attack contact',
            rejectedInput='no cooldown',cancelAfterAccepted='keep cooldown, no contact effect',stageTravel='retain cooldown; freeze outside combat',
            newRun='ready',resume='remaining cooldown/shield restored, pending contact cancelled',ordinaryAttack='does not consume skill',
            controls='up/back dodge + down parry + normal attack + separate skill'))
    (B/'character-skills-v2.json').write_text(json.dumps(contract,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    section='<section id="character-skills"><p class="eyebrow">MANUAL SKILLS · V2</p><h2>세 모험가, 직접 쓰는 한 가지 기술.</h2><p class="lead">하단 중앙의 전용 버튼을 눌러 사용합니다. 20초의 전투 시간이 지나면 다시 쓸 수 있습니다.</p><div class="grid3">'+''.join(cards)+'</div><p class="note">처음 출전할 때 준비 완료. 같은 도전에서 다음 적을 만나도 남은 대기시간을 유지합니다. 대화·이동·일시정지 중에는 시간이 흐르지 않습니다. 누를 수 없는 상태에서는 소모하지 않으며, 접수한 뒤 회피로 취소한 스킬은 효과 없이 대기시간만 남습니다.</p><p>기존 세 번 충전 방식은 사용하지 않습니다. 위·뒤 회피, 아래 쳐내기, 일반 공격은 그대로 유지합니다. <a href="character-skills-v2.json">현재 수치·상태 계약</a> · <a href="character-skills-v1.json">이전 충전 방식 보관본</a></p></section>'
    (B/'character-skills-v2-section.html').write_text(section,encoding='utf-8')
    identity='<section id="game-identity"><p class="eyebrow">READ · DEFEND · CHOOSE</p><h2>피한 뒤, 언제 한 방을 쓸까?</h2><p class="lead">방향 예고를 읽고 피한 다음, 평타와 20초 스킬 중 무엇을 지금 쓸지 고릅니다.</p><div class="grid3"><article class="card"><h3>눈으로 읽는 공격</h3><p>진한 외곽선과 노란 도트가 실제 위험 범위를 보여 줍니다. 예고 마지막까지 표시하고, 적중 직전에는 밝은 크림색으로 변합니다. 11스테이지 고블린은 정면으로 달려오며 뒤 회피로 대응합니다.</p></article><article class="card"><h3>기절 전후의 선택</h3><p>밝은 하늘색 누적이 가득 차 노란색이 되면 다음 타격으로 기절을 엽니다. 루카는 기절 뒤 큰 피해, 다렌은 다음 실수를 완충할 보호막, 미엘은 기절 준비를 앞당기는 누적이 강점입니다.</p></article><article class="card"><h3>작은 만남이 있는 여정</h3><p>적을 쓰러뜨리며 앞으로 달리고, 토리·리나·바룬·도토와 짧은 말을 나눕니다. 목표는 산길을 막은 용을 혼내주는 것. 이름·표정·말투로 동행하는 인물의 성격을 보여 줍니다.</p></article></div><p class="note">이 조합을 이 게임의 중심 경험으로 삼습니다. 시장 최초나 다른 게임에 없는 규칙이라고 주장하지 않습니다. 현재 플레이 구간은 1~20이며, 21~50의 지역과 마지막 용 전투는 후속 제작입니다.</p></section>'
    (B/'dragon-identity-v2-section.html').write_text(identity,encoding='utf-8')
    audio='<section id="attack-audio"><p class="eyebrow">PIXEL FEEDBACK · V4</p><h2>누르는 소리, 맞히는 소리.</h2><p class="lead">공격 입력에는 작은 도트 터치음, 실제 적중에는 짧은 8비트 타격음을 사용합니다.</p><div class="grid2"><article class="card"><h3>적중 · 114ms</h3><audio controls preload="none" src="Audio-v2/pixel-hit.wav"></audio><p>적중 이벤트에 한 번. 취소된 공격에는 타격음이 나지 않습니다.</p></article><article class="card"><h3>터치 · 39ms</h3><audio controls preload="none" src="Audio-v2/pixel-touch.wav"></audio><p>활성화된 공격 버튼을 누를 때 작은 입력음을 냅니다. 무음·일시정지 설정을 따릅니다.</p></article></div><p><a href="https://opengameart.org/content/512-sound-effects-8-bit-style">SubspaceAudio / Juhani Junkala · CC0</a> · <a href="Audio-v2/CREDITS.txt">배포 출처·편집 내역</a> · <a href="Audio-v1/index.html">이전 후보 비교</a></p><p class="small">원작과 동일한 음원이라는 뜻은 아닙니다. 최종 체감 음량은 실제 기기에서 추가로 확인합니다.</p></section>'
    (B/'attack-audio-v4-section.html').write_text(audio,encoding='utf-8')
    feedback='<section id="unity-build"><p class="eyebrow">UNITY · V0.9.3</p><h2>공격은 더 선명하게, 모험가는 셋으로.</h2><p class="lead">예고·기절 게이지, 고블린 돌진, 수동 스킬과 초상화 대화를 함께 연결하는 업데이트입니다.</p><p class="note" id="v4-status">통합 작업 중입니다. 아래 내용은 현재 제작 기준이며, 실행 검증 결과와 최신 실행판 링크는 완료 후 갱신합니다.</p><div class="grid3"><article class="card"><h3>예고·기절 가독성</h3><p>예고 판정 범위는 유지하고, 진한 외곽선과 밝은 경고 표시를 적중 직전까지 보여 줍니다. 기절바 본체는 가로·세로 20% 확대했습니다.</p></article><article class="card"><h3>플레이어 세 명</h3><p>루카·다렌·미엘을 선택해 출전합니다. 캐릭터별 모션과 전용 스킬, 독립 성장·진행을 연결합니다.</p></article><article class="card"><h3>이름과 얼굴이 있는 대화</h3><p>하단 대화창 왼쪽에 초상화, 본문 왼쪽 위에 화자를 표시합니다. 처음 만난 인물은 인사하고 자신의 이름을 알려 줍니다.</p></article></div><p><a href="../../QA/dragon-v4-acceptance-20261024.md">이번 완료 기준</a> · <a href="dialogue-v4.html">대화 페이지별 미리보기</a> · <a href="Audio-v2/index.html">새 공격음 듣기</a></p></section>'
    (B/'unity-v4-section.html').write_text(feedback,encoding='utf-8')
    board=B/'build-board.py';source=board.read_text(encoding='utf-8')
    source=source.replace('__UNITY_V3____CHARACTER_SKILLS____GAME_IDENTITY____STAGE_GROUND____ATTACK_AUDIO__', '__UNITY_V4____CHARACTER_SKILLS_V2____DIALOGUE_V4____GAME_IDENTITY_V2____STAGE_GROUND____ATTACK_AUDIO_V4__<details class="archive"><summary>이전 Unity 적용 기록 · v0.9.2</summary>__UNITY_V3__</details>')
    source=source.replace('tokens={','tokens={\'UNITY_V4\':(B/\'unity-v4-section.html\').read_text(encoding=\'utf-8\'),\'CHARACTER_SKILLS_V2\':(B/\'character-skills-v2-section.html\').read_text(encoding=\'utf-8\'),\'DIALOGUE_V4\':(B/\'dialogue-v4-section.html\').read_text(encoding=\'utf-8\'),\'GAME_IDENTITY_V2\':(B/\'dragon-identity-v2-section.html\').read_text(encoding=\'utf-8\'),\'ATTACK_AUDIO_V4\':(B/\'attack-audio-v4-section.html\').read_text(encoding=\'utf-8\'),',1) if "'UNITY_V4':" not in source else source
    source=source.replace('<a href="#character-skills">전용 스킬</a>','<a href="#character-skills">전용 스킬</a><a href="#dialogue-v4">만남·대화</a>') if 'href="#dialogue-v4"' not in source else source
    source=source.replace('다렌·미엘과 전용 스킬은 후속 제작입니다.','나머지 21~50 전투는 후속 제작입니다.')
    source=source.replace('<b>첫 제작의 플레이어는 검사 루카입니다.</b> 토리·리나·바룬은 이야기에서 먼저 만나고, 플레이어 직업 확장은 별도 후속 범위로 둡니다. 대사를 나눴다고 자동으로 파티에 합류하지 않습니다.','<b>플레이어는 루카·다렌·미엘입니다.</b> 토리·리나·바룬·도토는 길에서 만나는 이야기 동료입니다. 동행 대사는 함께 여행한다는 뜻이며, 전투에서는 선택한 모험가 한 명을 조작합니다.')
    board.write_text(source,encoding='utf-8')
    print('Prepared v4 sections; board remains generated by the existing build-board.py.')

if __name__=='__main__':main()
