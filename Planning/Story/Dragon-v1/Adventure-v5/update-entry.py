"""Publish the proposal into existing local planning entry points, not Unity."""
from pathlib import Path
import subprocess,sys

B=Path(__file__).resolve().parent
D=B.parent
section='''<section id="adventure-v5"><p class="eyebrow">LATEST PLAN · 2026.10.24 · UNITY 미적용</p><h2>새 대화창, 회복력, 그리고 함께 싸우는 동료.</h2><p class="lead">초상화9종을24px로 다시 그리고 대화창 오른쪽 위로 옮겼습니다. 5스테이지 연속 공격 연습, 적의 후퇴 준비, 클리어 회복과 여관 동료 기능을 한 기획으로 연결했습니다.</p><div class="actions"><a href="Adventure-v5/index.html">대화·동료 개정안 전체 보기 →</a><a href="Adventure-v5/index.html#recovery">체력 회복 규칙</a><a href="Adventure-v5/index.html#journey">NPC 만남 순서</a></div><figure class="wide"><a href="Adventure-v5/index.html#screens"><img src="Adventure-v5/dialogue-draft.png" width="1280" height="720" alt="24px 투명 토리 초상이 우측 상단, 이름표가 좌측 상단에 있는 새 대화 배치"></a><figcaption>기획 시안 · 초상 배경·프레임 제거 / 별도 화자 이름표 / 기존 실행판 v0.9.3은 아래에 그대로 보관</figcaption></figure><div class="grid3"><article class="card"><h3>공격 박자</h3><p>뒤로 물러나기 → 준비 → 타격. 현재 타격의 진행 표시를 실제 판정 시각과 맞춥니다.</p></article><article class="card"><h3>매 클리어 회복</h3><p>보스 처치 시 완전 회복을 없애고, 다섯 번째 스탯 회복력만큼 한 번 회복하는 안입니다.</p></article><article class="card"><h3>다섯 지역의 동료</h3><p>2토리 · 12바룬 · 22도토 · 32리나 · 42모모. 여관 서비스·강화와 주기적인 지원 공격을 설계했습니다.</p></article></div><p class="note">이 개정안은 대화 배치·첫 NPC 만남·보스 회복·동료 기능의 새 기준입니다. 아래 v0.9.3 적용 기록과 이전 스토리의 해당 부분보다 우선합니다. 아직 게임에 적용되지 않았습니다.</p></section>'''
(D/'adventure-v5-section.html').write_text(section+'\n',encoding='utf-8')
p=D/'build-board.py';s=p.read_text(encoding='utf-8')
if '__ADVENTURE_V5__' not in s:
    s=s.replace('<main><section class="hero"','<main>__ADVENTURE_V5__<section class="hero"',1)
    s=s.replace('tokens={','tokens={\'ADVENTURE_V5\':(B/\'adventure-v5-section.html\').read_text(encoding=\'utf-8\'),',1)
    s=s.replace('<nav aria-label="판타지 기획 목차">','<nav aria-label="판타지 기획 목차"><a href="#adventure-v5">동료·회복력 개정</a>',1)
    s=s.replace('__DIALOGUE_V4__','<details class="archive"><summary>현재 실행판의 대화 배치·만남 · v0.9.3</summary>__DIALOGUE_V4__</details>',1)
    s=s.replace('<section id="story"><p class="eyebrow">','<section id="story"><p class="note">NPC 첫 만남·동행·여관 서비스는 <a href="Adventure-v5/index.html#journey">최신 v5안</a>이 우선합니다. 아래는 기존 이야기의 목표·지역 배경입니다.</p><p class="eyebrow">',1)
    s=s.replace('토리·리나·바룬·도토는 길에서 만나는 이야기 동료입니다. 동행 대사는 함께 여행한다는 뜻이며, 전투에서는 선택한 모험가 한 명을 조작합니다.','현재 v0.9.3에서 토리·리나·바룬·도토는 이야기 동료입니다. 새 <a href="Adventure-v5/index.html#companions">v5 기획</a>에서는 이들을 여관에서 편성하고 자동 지원 공격을 받습니다.')
    p.write_text(s,encoding='utf-8')
# Direct #game-identity links still expose the new proposal.
p=D/'dragon-identity-v2-section.html';s=p.read_text(encoding='utf-8')
if 'Adventure-v5/index.html' not in s:
    s=s.replace('<section id="game-identity">','<section id="game-identity"><p class="note"><b>새 기획 v5:</b> 적의 후퇴 준비·Stage5 연속 공격 연습·회복력·여관 동료가 추가됩니다. <a href="Adventure-v5/index.html">화면 시안과 전체 기획 보기 →</a> (Unity 미적용)</p>',1)
    p.write_text(s,encoding='utf-8')
# Keep the established banner generator as the entry point's single source.
p=D/'link-board.py';s=p.read_text(encoding='utf-8')
marker='    # Adventure-v5 latest planning banner\n'
if marker not in s:
    injection=marker+'''    banner=banner.replace('세 모험가·전용 스킬 v0.9.3 · Unity 적용','대화·동료·회복력 v5 기획 · Unity 미적용').replace('공격 예고·기절바를 보강하고 세 캐릭터의 20초 스킬, 도트 효과음, 화자별 초상화 대화를 연결했습니다. 최신 실행판과 실제 화면을 상단에서 확인할 수 있습니다.','24px 초상과 새 대화창, 적 후퇴 동작·연속 공격 연습, 회복력과 여관 동료 기획을 상단에 정리했습니다. 현재 실행판은 v0.9.3입니다.').replace('게임 적용·기획 보기 →','최신 기획·시안 보기 →').replace('href="'+href+'"','href="'+href+'#adventure-v5"')
'''
    s=s.replace('    text=re.sub(r\'(<body',injection+'    text=re.sub(r\'(<body',1)
    p.write_text(s,encoding='utf-8')
subprocess.run([sys.executable,str(D/'build-board.py')],check=True)
subprocess.run([sys.executable,str(D/'link-board.py')],check=True)
print('Planning entry points updated.')
