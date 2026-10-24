"""Adventure-v5 configuration shape and semantic validation; no runtime state."""

def adventure_schema(int64):
    result={key:float for key in 'recoveryBase recoveryLinear recoveryBreakBase recoveryBreakPerStar'.split()}
    result.update({key:int for key in 'recoveryCostPercent companionMaxLevel companionGrowthPercent companionBreakPercent partyDpsCapPercent companionWindupMs newEnemyGraceMs slotPhaseMs guideGoldPercent blessingHpPercent teaRecoveryPercent servicePricePercent companionSkillChanceBasePercent companionSkillChancePerLevel companionSkillChanceCapPercent companionPeriodReductionPerLevel companionPeriodFloorPercent'.split()})
    result.update({key:[int] for key in 'slotGemCosts slotBossRequirements levelCapBosses levelCaps weaponAttackPercents weaponBossRequirements'.split()})
    result['weaponGoldCosts']=[int64]
    return result

COMPANION={**{key:str for key in 'id displayName speciesId jobId attackName serviceId returnLine skillId skillName'.split()}, **{key:int for key in 'joinStage periodMs damagePercent skillDamagePercent skillStaggerGain skillHealMaxHpPercent'.split()}}

def validate_adventure(data,need):
    a=data['adventure']
    expected=dict(recoveryCostPercent=50,companionMaxLevel=30,companionGrowthPercent=3,companionBreakPercent=25,partyDpsCapPercent=24,companionWindupMs=180,newEnemyGraceMs=350,slotPhaseMs=400,guideGoldPercent=3,blessingHpPercent=5,teaRecoveryPercent=15,servicePricePercent=10)
    expected.update(companionSkillChanceBasePercent=12,companionSkillChancePerLevel=1,companionSkillChanceCapPercent=45,companionPeriodReductionPerLevel=1,companionPeriodFloorPercent=70)
    for key,value in expected.items(): need(a[key]==value,'$.adventure.'+key,f'expected {value}')
    for key in 'recoveryBase recoveryLinear recoveryBreakBase recoveryBreakPerStar'.split(): need(a[key]>0,'$.adventure.'+key,'positive value required')
    for key,value in dict(slotGemCosts=[4,8],slotBossRequirements=[10,30],levelCapBosses=[0,10,20],levelCaps=[10,20,30],weaponAttackPercents=[4,7,10],weaponBossRequirements=[0,30,40]).items():need(a[key]==value,'$.adventure.'+key,'invalid tiers')
    need(len(a['weaponGoldCosts'])==3 and all(x>0 for x in a['weaponGoldCosts']),'$.adventure.weaponGoldCosts','three positive costs required')
    ids=['tori','barun','doto','rina','momo']; periods=[6000,10000,8000,7000,6000]; damages=[24,40,32,28,24];services=['route_guide','pack_cart','weapon_craft','travel_blessing','herbal_tea']
    skills=['companion_tori_double_shot','companion_barun_ground_slam','companion_doto_flame_hammer','companion_rina_lightning','companion_momo_seed_bomb'];skill_damage=[180,170,200,180,170];stagger=[0,8,0,10,0];heal=[0,0,0,0,2]
    need(len(data['companions'])==5 and {x['id'] for x in data['companions']}==set(ids),'$.companions','five distinct stable IDs required')
    for c in data['companions']:
        if c['id'] not in ids:continue
        i=ids.index(c['id'])
        need(c['joinStage']==2+10*i and c['periodMs']==periods[i] and c['damagePercent']==damages[i] and c['serviceId']==services[i],'$.companions.'+c['id'],'invalid timing, damage or service')
        need(all(c[x].strip() for x in ('displayName','speciesId','jobId','attackName','returnLine')),'$.companions.'+c['id'],'display text required')
        need(c['skillId']==skills[i] and bool(c['skillName'].strip()) and c['skillDamagePercent']==skill_damage[i] and c['skillStaggerGain']==stagger[i] and c['skillHealMaxHpPercent']==heal[i],'$.companions.'+c['id'],'invalid skill')
    characters={x['id'] for x in data['characters']}; portraits=characters|set(ids)|{'unknown','player','gib'}
    for table,stages in [('stories',[2,10,12,20]),('plannedStories',[22,32,42]),('legacyStories',[2,7,10,15,20])]:
        need(sorted(x['stage'] for x in data[table])==stages,'$.'+table,'invalid scene stages')
        need(len({x['id'] for x in data[table]})==len(data[table]),'$.'+table,'duplicate scene ID')
        for s in data[table]:
            path='$.'+table+'.'+s['id']
            need(s['kind'] in ('boss','encounter') and s['id']==f"story_{s['kind']}_{s['stage']}",path,'invalid identity')
            need(not s['companionId'] or s['companionId'] in ids,path,'unknown companion')
            boss=s['stage']%10==0
            need(s['kind']==('boss' if boss else 'encounter') and (s['bossId']==('bell_knight' if s['stage']==10 else 'chain_jailer') if boss else s['bossId']=='' and bool(s['companionId'])),path,'invalid boss/encounter binding')
            if not boss and table!='legacyStories':need(any(c['id']==s['companionId'] and c['joinStage']==s['stage'] for c in data['companions']),path,'encounter companion stage mismatch')
            need(2<=len(s['lines'])<=8,path,'two to eight lines required')
            for line in s['lines']:
                need(bool(line['speaker'].strip()) and bool(line['text'].strip()),path,'speaker/text required')
                need(line['portraitId'] in portraits and (line['speaker']=='player')==(line['portraitId']=='player'),path,'invalid portrait/player binding')
                seen=set()
                for r in line['responses']:
                    need(r['characterId'] in characters and r['characterId'] not in seen and bool(r['text'].strip()),path,'invalid response');seen.add(r['characterId'])
                need(line['speaker']!='player' or seen==characters,path,'all player responses required')
