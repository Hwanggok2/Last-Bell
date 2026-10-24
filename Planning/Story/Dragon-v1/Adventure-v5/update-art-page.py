"""Reconcile confirmed portrait/NPC source art into the planning page only."""
from pathlib import Path
import json,hashlib
B=Path(__file__).resolve().parent
def replace(s,a,b):
 assert a in s,('Expected source text not found',a[:80])
 return s.replace(a,b)
plan=json.loads((B/'plan.json').read_text(encoding='utf-8'))
plan['version']='adventure-v5-plan-2-art';plan['status']='unity-implementation-in-progress'
d=plan['ui']['dialogue']
d.update(playerPortrait=[48,384,144,144],npcPortrait=[1056,384,144,144],playerNameplate=[216,440,176,56],npcNameplate=[48,440,176,56],nameAnchor='NPC: above panel left at48,440. Player: right of left portrait at216,440.',portraitAnchor='Player: panel upper-left at48,384. NPC: panel upper-right at1056,384. Both overlap panel40px.',speakerSideRule='on/dam/rin are player speakers: left portrait, name to its right. NPC/unknown speakers: right portrait, name at panel left. Move portrait and badge atomically on speaker change.')
d['portrait']=d['npcPortrait'];d['nameplate']=d['npcNameplate']
d['states']=[x.replace('speaker change swaps cutout and badge together','speaker change swaps cutout, side and badge together') for x in d['states']]
plan['futureAcceptance'][0]='1280×720 and640×360 dialogue: player portrait upper-left/name toitsright, NPC portrait upper-right/name upper-left, no portrait plate, no cut text, all hit areas at least48px at640'
plan['artProduction']={'status':'source-art-reviewed; Unity integration in progress','mielRework':'portraits/rework-rin/comparison.png','mielPortrait':'portraits/rin.png','companionManifest':'../Production/Companions-v5/manifest.json','companionReview':'../Production/Companions-v5/review.html','momoPortrait':'../Production/Companions-v5/portraits/momo.png','companions':5,'idleFramesPerActor':4,'attackFrames':{'tori':8,'barun':8,'doto':6,'rina':8,'momo':8},'nativeBodyHeight':32,'paletteMax':8,'groundY':29,'contactMs':180,'attackTotalMs':600,'runtimeQa':'pending root integration; source validation is not game QA'}
(B/'plan.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=B/'page-template.html';s=p.read_text(encoding='utf-8')
s=replace(s,'기획·시안 개정 v5 · Unity 미적용','기획·아트 개정 v5 · Unity 적용 진행 중')
s=replace(s,'<span>새 초상화9종</span>','<span>기존9종 + 모모 초상</span>')
s=replace(s,'얼굴은 오른쪽 위, 이름은 왼쪽 위.','말하는 쪽에 맞춰, 얼굴과 이름을 함께.')
s=replace(s,'참고 이미지의 이름표 위치를 가져오고, 밝은 양피지·녹색 도트 UI는 이어갑니다. 초상 뒤의 사각 배경과 테두리를 없애 얼굴 실루엣만 남깁니다.','플레이어가 말하면 초상은 왼쪽 위, 이름은 그 오른쪽에 둡니다. NPC가 말하면 초상은 오른쪽 위, 이름은 패널 왼쪽 위로 옮깁니다. 밝은 양피지·녹색 도트 UI를 유지하고 초상 뒤의 사각 배경과 테두리는 없앱니다.')
s=replace(s,'<button data-scene="dialogue" aria-pressed="true">대화</button>','<button data-scene="dialogue" aria-pressed="true">NPC 대화</button><button data-scene="dialogue-player" aria-pressed="false">플레이어 대화</button>')
s=replace(s,'1280×720 기준 · Unity 미적용','1280×720 기준 · Unity 적용 진행 중')
s=replace(s,'패널 왼쪽 위 바깥에 붙입니다. 화자가 바뀌는 순간 이름과 초상을 함께 바꿉니다.','NPC 이름표는 패널 왼쪽 위 바깥, 플레이어 이름표는 왼쪽 초상의 오른쪽에 붙입니다. 화자가 바뀌는 순간 이름·초상·좌우 배치를 함께 바꿉니다.')
s=replace(s,'모모는 새 캐릭터 정의 후 같은 규격으로 제작합니다.','모모도 같은24px 규격으로 제작했습니다. 미엘은 넓은 청록색 모자와 양눈이 보이는 얼굴로 다시 정돈했습니다.')
s=replace(s,'width="1440" height="360" alt="위 기존48px 초상9종','width="1440" height="376" alt="위 기존48px 초상9종')
s=replace(s,'위: 현재48px / 아래: 새24px.','위: 이전48px / 아래: 확정24px 원본.')
s=replace(s,'<a href="portraits/manifest.json">제작·검증 기록</a></figcaption></figure>','<a href="portraits/manifest.json">제작·검증 기록</a> · <a href="portraits/rework-rin/comparison.png">미엘 수정 전후</a></figcaption></figure><figure><img class="screen" src="dialogue-player-draft.png" width="1280" height="720" alt="왼쪽 위에 새 미엘 초상, 그 오른쪽에 미엘 이름표가 놓인 플레이어 대화 시안"><figcaption>플레이어 화자 예시 · 새 미엘24px 초상 · 기획용 합성 화면, Unity 적용 진행 중</figcaption></figure>')
s=replace(s,'<tr><td>이름표</td><td>48 / 440 / 176 / 56</td><td>패널 왼쪽 위 anchor. 긴 이름이면 폭을 확장합니다.</td></tr><tr><td>초상</td><td>1056 / 384 / 144 / 144</td><td>패널 오른쪽 위 anchor. 640화면72×72, 프레임·판 없음.</td></tr>','<tr><td>플레이어 이름표</td><td>216 / 440 / 176 / 56</td><td>왼쪽 초상의 오른쪽. 긴 이름이면 폭을 확장합니다.</td></tr><tr><td>NPC 이름표</td><td>48 / 440 / 176 / 56</td><td>패널 왼쪽 위 anchor.</td></tr><tr><td>플레이어 초상</td><td>48 / 384 / 144 / 144</td><td>패널 왼쪽 위. 640화면72×72, 프레임·판 없음.</td></tr><tr><td>NPC 초상</td><td>1056 / 384 / 144 / 144</td><td>패널 오른쪽 위. 640화면72×72, 프레임·판 없음.</td></tr>')
anchor='<div class="scroll"><table><thead><tr><th>동료</th><th>여관 상호작용</th>'
preview='<article class="card" id="companion-art"><h3>다섯 동료의 대기·공격 모션</h3><p>토리는 활, 바룬은 대검, 도토는 망치, 리나는 바람 마법, 모모는 씨앗 주머니를 사용합니다. 모두32px 높이 기준,8색 이하의 원본으로 제작했습니다. 플레이어보다 작은4배 표시를 기준으로 잡습니다.</p><p>대기는 각4프레임, 공격은 각8프레임이며 도토는 중복 회복 포즈를 뺀6프레임입니다. 공격 시작180ms에 한 번 타격하고600ms에 복귀합니다. 무기가 잘리지 않도록 바룬·도토 공격에는 투명 여백을 더했습니다.</p><p><a class="button primary" href="../Production/Companions-v5/review.html">동료5종 모션 재생</a> <a class="button" href="../Production/Companions-v5/review.png">전체58프레임 보기</a></p><p class="small"><a href="../Production/Companions-v5/manifest.json">프레임·피벗·타격시각 명세</a> · <a href="../Production/Companions-v5/validation.json">원본 검수</a> · <a href="../Production/Companions-v5/portraits/review.png">모모 초상</a><br>아트 원본 제작·검수 완료. Unity 연결과 실제 동행 전투 검증은 진행 중입니다.</p></article>'
s=replace(s,anchor,preview+anchor)
s=replace(s,'아트 담당은 새24px 초상9종을 제작했습니다.','아트 담당은24px 초상9종의 미엘을 재정돈하고 모모 초상을 추가했습니다. 동료5종의 대기·공격58개 고유 포즈를 검수했으며, Unity 연결은 진행 중입니다.')
s=replace(s,'이번 작업은 기획·아트 초안·정적 문서 검토입니다. Unity 소스·저장 프리팹·게임 데이터·실행 파일은 변경하지 않았습니다. 다음 구현 묶음이 완성되면 전용 QA를 한 번 진행하고, 발견된 문제에 한해 추가 검증합니다.','이 페이지는 확정 기획과 검수한 아트 원본을 보여 줍니다. Unity 적용은 별도 담당이 진행 중이며, 실행판 적용 완료나 플레이 QA 통과를 뜻하지 않습니다. 구현 묶음이 완성되면 전용 QA를 한 번 진행하고, 발견된 문제에 한해 추가 검증합니다.')
s=replace(s,'기존 Unity v0.9.3 실행판은 그대로 유지됩니다.','Unity 적용 진행 중이며, 완료 여부는 통합 실행 검증 후 갱신합니다.')
p.write_text(s,encoding='utf-8')
check=json.loads((B/'portraits/rework-rin/validation.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((B/'portraits'/name).read_bytes()).hexdigest()==v for name,v in check['otherEightUnchangedHashes'].items())
print('Speaker sides, final source-art previews and implementation-in-progress state reconciled;8other portraits preserved.')
