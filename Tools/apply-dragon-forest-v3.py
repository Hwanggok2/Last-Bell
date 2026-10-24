"""Update display/art/story bindings while retaining stable IDs and gameplay values."""
from pathlib import Path
import copy
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'GameData/mvp-catalog.json'
PLAN = ROOT / 'Planning/Story/Dragon-v1'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
write = lambda p, d: p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def main():
    data = read(SOURCE)
    story = read(PLAN / 'story.json')
    protected = ['schemaVersion', 'defaultCharacterId', 'mvpLastStage', 'boundaryStage',
                 'combat', 'growth', 'economy', 'relicRules', 'stages', 'transitions', 'relics']
    before = {key: copy.deepcopy(data[key]) for key in protected}
    characters = copy.deepcopy(data['characters'])
    names = {'on': '루카 · 검사', 'dam': '다렌 · 수호기사', 'rin': '미엘 · 마법사'}
    for character in data['characters']:
        character['displayName'] = names[character['id']]
    roster = {enemy['id']: enemy for enemy in data['enemies']}
    bindings = []
    for stage, planned in zip(data['stages'][10:20], story['stages'][10:20]):
        assert stage['number'] == planned['stage']
        enemy = roster[stage['enemyId']]
        enemy['displayName'] = planned['name']
        enemy['artId'] = 'dragon_' + planned['enemyId']
        bindings.append(dict(stage=stage['number'], stableEnemyId=enemy['id'], **{k: enemy[k] for k in ('displayName', 'artId')}))
    data['onboarding']['prologueParagraphs'] = story['opening']['sentences']
    for scene in data['stories']:
        if scene['stage'] == 10:
            scene['lines'] = [dict(speaker='토리', text='용은 저 봉우리에 있어.', responses=[]),
                              dict(speaker='토리', text='숲길로 가면 빨라.', responses=[]),
                              dict(speaker='루카', text='좋아. 아직 해도 높네.', responses=[])]
        elif scene['stage'] == 20:
            scene['lines'] = [dict(speaker='기블', text='알았어! 통행료는 안 받을게.', responses=[]),
                              dict(speaker='도토', text='이 갱도를 지나면 고개 아래야.', responses=[]),
                              dict(speaker='루카', text='불 켜진 길만 따라가면 되겠네.', responses=[])]
    data['dataVersion'] = 'mvp1.15.0'
    data['sourcePlanningVersion'] = 'dragon-v3-forest-names-grounding'
    assert all(data[key] == before[key] for key in protected)
    assert all({k: v for k, v in old.items() if k != 'displayName'} ==
               {k: v for k, v in new.items() if k != 'displayName'}
               for old, new in zip(characters, data['characters']))
    write(SOURCE, data)
    write(ROOT / 'Planning/Production/dragon-v3-data-bindings.json', {
        'scope': 'Stage11–20 forest art/display and adventurer names/story; unchanged gameplay and IDs',
        'mapping': bindings, 'characters': names, 'protectedDomains': protected,
        'protectedSha256': hashlib.sha256(json.dumps(before, sort_keys=True).encode()).hexdigest(),
        'skillsImplemented': False, 'playableStages': 20,
    })
    print('Updated forest bindings, three character names and Stage10/20 dialogue; stable IDs and gameplay preserved.')


if __name__ == '__main__':
    main()
