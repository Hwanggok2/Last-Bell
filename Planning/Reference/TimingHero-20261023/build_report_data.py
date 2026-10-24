from pathlib import Path
import json, math, hashlib, base64
ROOT=Path(__file__).resolve().parent
REPO=ROOT.parents[2]
catalog=json.loads((REPO/'GameData/mvp-catalog.json').read_text(encoding='utf-8'))
def stat(star,level,kind):
    b=max(0,(level-1)//10);q=[.5,.75,1,1.25,1.5][star-1]
    if kind=='attack': return math.floor((10+4*q*level)*(2.3+.05*(star-1))**b+.5)
    if kind=='hp': return math.floor((100+24*q*level)*(1.7+.05*(star-1))**b+.5)
    if kind=='luck': return {'crit':min(60,5+level*[2,3,4,5,6][star-1]//10+star*b),'gold':min(150,level*[1,1,1,2,2][star-1]+star*b)}
    return min(600,140+star*(level+10*b))

# Manually transcribed from the supplied recording. Not a reconstructed source-code formula.
atk=[18,20,23,27,31,35,39,43,48,52,80,89,99,110,123,136,152,169,188,209,356]
cost=[40,45,50,56,64,72,81,91,102,577,129,146,164,184,208,234,263,296,333,1874]
times=[697,697.5,698,698.5,698.7,699,699.5,700,700.5,701,702.5,703,703.5,703.8,704,704.5,705,705.5,706,706.5]
growth=[{'currentLevel':i+1,'nextLevel':i+2,'before':atk[i],'after':atk[i+1],'delta':atk[i+1]-atk[i],'price':cost[i],'t':times[i],'rating':4,'character':'성기사','status':'영상 표시값'} for i in range(20)]
starObservations=[
 {'stat':'공격력','rating':4,'character':'성기사 / 네크로맨서','from':18,'to':20,'delta':2,'cost':40,'t':150},
 {'stat':'공격력','rating':5,'character':'도적','from':21,'to':23,'delta':2,'cost':40,'t':134},
 {'stat':'공격력','rating':6,'character':'기사','from':24,'to':26,'delta':2,'cost':40,'t':103},
 {'stat':'체력','rating':4,'character':'도적','from':120,'to':133,'delta':13,'cost':30,'t':135},
 {'stat':'체력','rating':6,'character':'기사 / 네크로맨서','from':160,'to':177,'delta':17,'cost':30,'t':119},
 {'stat':'체력','rating':7,'character':'성기사','from':180,'to':200,'delta':20,'cost':30,'t':692},
 {'stat':'치명타율','rating':1,'character':'성기사 / 네크로맨서','from':0,'to':.6,'delta':.6,'cost':1000,'t':120},
 {'stat':'치명타율','rating':4,'character':'기사','from':0,'to':1.2,'delta':1.2,'cost':1000,'t':105},
 {'stat':'회복력','rating':1,'character':'도적','from':0,'to':1.2,'delta':1.2,'cost':2000,'t':136},
 {'stat':'회복력','rating':5,'character':'성기사','from':0,'to':2.8,'delta':2.8,'cost':2000,'t':693.5},
 {'stat':'회복력','rating':6,'character':'네크로맨서 계열 표시','from':0,'to':3.2,'delta':3.2,'cost':2000,'t':128},
]
relicRows=[
 ['용의 발톱','보스에게 일정 시간 추가 피해','MAX',81,'보스 지속 피해'],
 ['대마법사의 갑옷','보스에게 받는 피해 감소','6',83,'보스 방어'],
 ['용의 송곳니','보스를 공격하면 체력 회복','7',84,'보스 흡혈'],
 ['거인의 심장','최대 체력 증가','MAX',98,'생존'],
 ['키메라의 발톱','일반 적에게 일정 시간 추가 피해','2',85,'일반 적 지속 피해'],
 ['대마법사의 장갑','보스에게 치명타를 더 자주 가함','8 → 9',88,'보스 치명타'],
 ['탐욕의 상자','보스에게서 더 많은 현상금 획득','6',89,'경제'],
 ['파괴의 도끼','몬스터의 피해 감소를 무시','MAX',90,'방어 관통'],
 ['대마법사의 투구','보스전에서 반격 기회를 더 자주 제공','4',91,'반격'],
 ['대마법사의 검','보스에게 가하는 피해 증가','6',92,'보스 공격'],
 ['사도의 장갑','일반 적에게 치명타를 더 자주 가함','3 → 4',93,'일반 적 치명타'],
 ['신성한 반지','스킬 사용 시 체력 추가 회복','1',94,'스킬 연계'],
 ['키메라의 뿔','일반 적의 공격에 일정량 반사','보상 후보 Lv.1',671,'반사'],
 ['회복의 목걸이','싸움 후 더 많은 체력 회복','보상 후보 Lv.1',673,'전투 후 회복'],
]
relics=[dict(zip(['name','effect','level','t','category'],r)) for r in relicRows]
events=[
 {'id':'opening','span':[5,61],'title':'프롤로그','observation':'삽화 4장과 짧은 글을 순차적으로 표시. 우하단 건너뛰기가 유지된다. 녹화에서 약 56초 소요.','confidence':'관찰'},
 {'id':'version','span':[64,65],'title':'타이틀','observation':'클래식·콜로세움·레이드와 상점, 설정, Credits. 화면에 ver. 1.0.7 표시. 스토어의 iOS 2.3.0 설명과 같은 빌드로 간주하지 않는다.','confidence':'관찰'},
 {'id':'selection','span':[66,80],'title':'용사 선택','observation':'좌측 세로 목록, 우측 선택 용사의 평가·스킬·레벨·도달 단계·4능력치·개별 보유 현상금. 용사 변경 시 현상금도 달라진다. 평가 50·55·125 등 종합 점수는 개별 능력치 성장 기호 수와 다르다.','confidence':'관찰'},
 {'id':'relic-list','span':[81,99],'title':'유물 목록','observation':'보유 목록에는 12종이 보이고, 전 용사에게 적용된다는 안내가 반복된다. 하단 12/12와 다음 슬롯 200다이아 안내. 장착 방식과 슬롯 상한 전체는 영상만으로 확정 불가.','confidence':'관찰 / 일부 미확인'},
 {'id':'rating','span':[103,151],'title':'성장 적성','observation':'성장 기호가 1~7개까지 등장한다. 공격·체력·치명타율·회복력으로 구성. 캐릭터 종합 평가 점수로 증가량을 직접 계산하면 안 된다.','confidence':'관찰'},
 {'id':'normal','span':[294,301.4],'title':'일반 적 공격 표본','observation':'12층 나무요정은 약 294.1초부터 준비 동작, 294.9초 접촉. 다음 준비 약 299.7초, 접촉 약 300.6~300.7초. 두 접촉 간 약 5.7~5.8초. 피격 시 플레이어 쪽에 29가 표시된다.','confidence':'100ms 간격 표본 / ±0.1~0.2초'},
 {'id':'boss10','span':[234,241],'title':'보스 회피·반격','observation':'10층 거인 접촉 약 234.6초와 238.3초, 간격 약 3.7초. Just!와 반격 기회 표시, 약 239.3초부터 기절과 공격 유도 문구. 추가 피해 숫자가 섞여 순수 기본 공격력 역산은 불가.','confidence':'100ms 간격 표본 / ±0.1~0.2초'},
 {'id':'combo','span':[607.6,613],'title':'20층 2연속 공격','observation':'서로 다른 위치의 ! 묶음이 약 608.0~608.2초, 608.8~609.0초에 먼저 등장. 타격은 약 609.6초, 610.3초 순서. ! 한 묶음의 기호 개수는 공격 횟수가 아니다.','confidence':'100ms 간격 표본 / ±0.1~0.2초'},
 {'id':'counter','span':[605.8,607.6],'title':'반격 기회','observation':'Chance!·방사형 표시·반격 기회 문구가 약 1.5초 이상 지속되는 표본. 유물의 기회 발생 효과가 존재하므로 완벽 회피만의 고정 규칙으로 단정하지 않는다.','confidence':'관찰'},
 {'id':'feel','span':[234,241],'title':'타격감','observation':'영웅이 멀리 접근해 공격한 뒤 복귀한다. 밝은 피격 반전, 굵고 외곽선 있는 피해 숫자, 작은 파편, 치명타 시 짧은 월드 흔들림이 겹친다. 백색 변화는 공격 예고와 피격 효과를 구분해 읽어야 한다.','confidence':'프레임 관찰'},
 {'id':'rewards','span':[649,679],'title':'런 종료와 보상 정산','observation':'21층 전투 뒤 화면을 전환해 10층 돌파 유물, 20층 돌파 유물을 차례로 선택. 3후보 → 선택 설명 → 고르기 → 업그레이드 안내. 마지막 승리 화면은 획득 현상금 3,487과 계속 버튼. 종료 입력/사망 판정 원인은 확정하지 않는다.','confidence':'관찰'},
 {'id':'prices','span':[697,711],'title':'실제 강화 연속 표본','observation':'성기사 공격력 Lv.1~20, 다음 수치·비용을 확인. 10→11은 52→80 / 577, 11→12는 80→89 / 129. 20→21 견적 209→356 / 1,874는 자금 부족으로 실행하지 않음.','confidence':'표시값 / 20→21은 견적'},
 {'id':'economy','span':[708.5,733],'title':'재화 부족','observation':'성기사 20레벨 화면에서는 다이아 1개→현상금 6,000, 새 네크로맨서는 1개→1,000. 환율이 달라지지만 전체 결정식은 미확인. 확인·취소가 분리된다.','confidence':'관찰'},
 {'id':'extra','span':[738,746.6],'title':'기타 모드','observation':'비밀 상점의 보석 상품, 콜로세움 3인 팀과 명예 755, 잠긴 용사 목록이 보인다. 실제 구매·콜로세움 전투·레이드 플레이는 녹화되지 않았다.','confidence':'관찰 / 상세 미확인'},
]
names=['식인꽃','돌연변이거북','나무괴물','주시자','골렘','식인꽃','돌연변이거북','나무괴물','주시자','거인','늑대인간','나무요정','스콜피오','도마뱀인간','설인','늑대인간','나무요정','스콜피오','도마뱀인간','미노타우루스','마법고양이']
stageTimes=[160,166,171,178,184,190,198,208,222,234,276,294,312,330,354,378,402,420,450,480,636]
bounties=[12,11,15,16,220,19,19,18,21,707,23,34,28,28,427,38,51,51,67,1682,None]
referenceStages=[{'stage':i+1,'name':n,'t':stageTimes[i],'rewardObserved':bounties[i],'hp':None,'damage':None} for i,n in enumerate(names)]
referenceStages[0]['hp']='약 50 (픽셀 추정 45–55)'
referenceStages[11]['hp']='약 300 (픽셀 추정 250–340)'
referenceStages[11]['damage']='표시 피격 29 (300.7초, 기본 공격력 아님)'

future=[]
old=json.loads((REPO/'Planning/Balance/balance-v7.json').read_text(encoding='utf-8'))['stages']
for s in range(1,51):
    atk3=stat(3,s,'attack');hp3=stat(3,s,'hp')
    if s<=20:
        c=catalog['stages'][s-1];hp=c['maxHp'];damage=c['damage']
    else:
        z=(s-1)//10;k=(s-1)%10
        target=24+8*(z-2)+.75*k
        if k==4:target+=10
        if k==9:target={2:60,3:72,4:84}[z]
        hp=math.floor(atk3*target+.5)
        ratio=.51+.01*k+(.10 if k==4 else .15 if k==9 else 0)
        damage=math.ceil(hp3*ratio)
    future.append({'stage':s,'attack3':atk3,'hp3':hp3,'enemyHp':hp,'enemyDamage':damage,'normalHits':math.ceil(hp/atk3),'hitsToDie':math.ceil(hp3/damage),'oldHp':old[s-1]['hp'],'oldDamage':old[s-1]['damage'],'proposed':s>20})

data={'version':'reference-plan-v8','date':'2026-10-23','scope':'Planning only. No Unity, GameData or build modifications.','source':json.loads((ROOT/'source.json').read_text(encoding='utf-8')),'catalog':catalog,'observations':events,'referenceGrowth':growth,'referenceStars':starObservations,'referenceRelics':relics,'referenceStages':referenceStages,'futureBalance':future,'currentStarTable':[{'star':s,'attack':[stat(s,l,'attack') for l in [0,1,10,11,20,21,50]],'hp':[stat(s,l,'hp') for l in [0,1,10,11,20,21,50]]} for s in range(1,6)],'timing':{'single':{'flashes':[0,240,480],'impact':[720]},'double':{'flashes':[[0,240,480],[720,960,1200]],'impact':[1440,1920]},'triple':{'flashes':[[0,240,480],[720,960,1200],[1440,1680,1920]],'impact':[2160,2640,3120]},'normalWindowMs':160,'perfectWindowMs':60,'counterMs':500,'attackContactMs':200,'attackCycleMs':500,'currentWaveDurationMs':600,'proposedFollowupWaveDurationMs':480},'externalSources':[{'url':'https://apps.apple.com/vn/app/timing-hero-colosseum-and-raid/id1041854448','title':'Buff Studio App Store 설명 (iOS 2.3.0, 2016)','use':'자동 전투 없음, 회피/반격, 유물의 전 용사 적용, 콜로세움/레이드의 3인 구성 보조 확인. 영상 빌드 수치 검증용 아님.'}]}
data['timing']['proposedFirstWaveDurationMs']=480
(ROOT/'analysis.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'report-data.js').write_text('window.REPORT_DATA = '+json.dumps(data,ensure_ascii=False)+';\n',encoding='utf-8')
assetPaths={'hero':'Client/Assets/Game/Art/Presentation/Clips/hero-v5-idle/01.png','slash':'Client/Assets/Game/Art/Presentation/Clips/hero-v6-slash-down/03.png','enemy':'Client/Assets/Game/Art/Presentation/Clips/chain-idle/00.png','currentBattle':'Planning/Evidence/Audio-v72/desktop/01-mobile-controls.png','currentLobby':'Planning/Evidence/Audio-v72/desktop/02-lobby.png','currentPrologue':'Planning/Evidence/Audio-v72/desktop/03-prologue-scroll.png','currentRelic':'Planning/Evidence/Audio-v72/desktop/06-relic-selected.png'}
assets={k:'data:image/png;base64,'+base64.b64encode((REPO/p).read_bytes()).decode() for k,p in assetPaths.items()}
(ROOT/'assets.js').write_text('window.PREVIEW_ASSETS = '+json.dumps(assets)+';\n',encoding='utf-8')
(ROOT/'asset-provenance.json').write_text(json.dumps({'note':'Existing Last Bell art and actual v7.2 runtime captures only. No Timing Hero art is used in proposed game screens.','assets':{k:{'path':p,'sha256':hashlib.sha256((REPO/p).read_bytes()).hexdigest()} for k,p in assetPaths.items()}},indent=2),encoding='utf-8')
print('Analysis data, current source snapshot, growth observations and art bindings written.')
