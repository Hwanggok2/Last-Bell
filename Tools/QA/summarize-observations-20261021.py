"""Summarize already-recorded observations; no game launch or source edits."""
import csv
import json
from collections import defaultdict
from pathlib import Path

root = Path(__file__).resolve().parents[2]
evidence = root / 'Planning/Evidence/UnityPresentation-20261021'
groups = defaultdict(list)
for row in csv.DictReader((evidence / 'sequence-960x540/timeline.csv').open()):
    groups[row['scenario']].append(row)

summary = {'limits': [
    'Real frame clock; synthetic fixture save and PointerCommand, not OS input.',
    'PNG capture can affect frame intervals; this is not a performance benchmark.',
    'TravelPixels is logical distance, including reduced mode; not rendered background displacement.',
    'CSV motion and flash columns describe the player, not the enemy.'
], 'scenarios': {}}
for name, rows in groups.items():
    segments = []
    for row in rows:
        key = (row['phase'], row['kind'], row['stage'])
        if not segments or tuple(segments[-1]['state']) != key:
            segments.append({'state': key, 'firstRealtime': float(row['realtime']),
                             'firstTransitionMs': float(row['transitionMs']), 'frames': 0})
        segments[-1]['frames'] += 1
        segments[-1]['lastRealtime'] = float(row['realtime'])
        segments[-1]['lastTransitionMs'] = float(row['transitionMs'])
    for i, segment in enumerate(segments[:-1]):
        segment['observedUntilNextStateMs'] = round(
            (segments[i + 1]['firstRealtime'] - segment['firstRealtime']) * 1000, 3)
    travel = [r for r in rows if r['kind'] == 'Travel']
    data = {'frames': len(rows), 'segments': segments,
            'deltaMsRange': [min(float(r['delta']) for r in rows) * 1000,
                             max(float(r['delta']) for r in rows) * 1000],
            'zoomRange': [min(float(r['zoom']) for r in rows), max(float(r['zoom']) for r in rows)]}
    if travel:
        data['travelPlayerXRange'] = [min(float(r['playerX']) for r in travel), max(float(r['playerX']) for r in travel)]
        data['travelRunFramesObserved'] = sorted({int(r['clipFrame']) for r in travel if r['motion'] == 'Run'})
        data['travelCombatMsRange'] = [min(float(r['combatMs']) for r in travel), max(float(r['combatMs']) for r in travel)]
    summary['scenarios'][name] = data
(evidence / 'timeline-summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
for name, data in summary['scenarios'].items():
    print(name, [(s['state'], s.get('observedUntilNextStateMs')) for s in data['segments']])
