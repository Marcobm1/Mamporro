"""Extrae H1b sin convertir el éxito NUnit en aceptación de calidad/presupuesto."""
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def extract(path, gate):
    root = ET.parse(path).getroot()
    if root.get('result') != 'Passed':
        raise ValueError('Suite fallida: exactitud no certificada')
    tags = ('PREP', 'GATE', 'QUERY', 'WORST', 'FAILPAIR')
    rows = {tag: [] for tag in tags}
    for case in root.iter('test-case'):
        for output in case.findall('output'):
            for line in (output.text or '').splitlines():
                fields = line.split()
                if not fields or fields[0] not in ['H1B-' + t for t in tags]:
                    continue
                row = {}
                for field in fields[1:]:
                    key, value = field.split('=', 1)
                    try:
                        row[key] = int(value)
                    except ValueError:
                        try:
                            row[key] = float(value)
                        except ValueError:
                            row[key] = value
                rows[fields[0][4:]].append(row)
    expected_maps = ({('Z9K2', 64)} | {(s, 16) for s in
        ('barrier', 'roofs', 'drop', 'climb', 'ramp', 'pass')}) if gate == 'A' else {
        (s, size) for s in ('A', 'B', 'P0QA', 'MAMPORRO', 'COLINAS7', 'Z9K2',
                          'MESETA', 'ACANTILADO', 'RAMPA42', 'HORDA750') for size in (16, 32, 64)}
    expected = {(s, size, c) for s, size in expected_maps for c in range(3)}
    key = lambda r: (r['case'], r['size'], r['class'])
    if len(rows['PREP']) != len(expected) or {key(r) for r in rows['PREP']} != expected:
        raise ValueError('Matriz de preparaciones incompleta/duplicada')
    quality = {(key(r), r['k']): r for r in rows['GATE']}
    if len(quality) != 4*len(expected) or len(rows['GATE']) != len(quality):
        raise ValueError('Faltan variantes o hay duplicados')
    for case in expected:
        for variant in range(4):
            row = quality[case, variant]
            queries = [q for q in rows['QUERY'] if key(q) == case and q['k'] == variant]
            assert len(queries) == row['goals']
            assert len({q['goal'] for q in queries}) == len(queries)
            assert sum(q['work'] for q in queries) == row['work']
            if variant:
                assert all(q['work'] == q['init'] + q['macro'] + q['physical'] for q in queries)
            assert bool(row['pass']) == (row['p95'] <= 1.5 and row['max'] <= 2)
    worst = {r['k']: r for r in rows['WORST']}
    assert set(worst) == set(range(4))
    assert worst[0]['cost'] == 8691 and all(r['optimal'] == 50 for r in worst.values())
    preparations = []
    for label, size in sorted(expected_maps):
        group = [r for r in rows['PREP'] if (r['case'], r['size']) == (label, size)]
        preparations.append({'case': label, 'size': size,
            'completeMs': group[0]['baseMs'] + sum(r['worldMs'] + r['setupMs'] for r in group),
            'probePayload': sum(r['payload'] for r in group),
            'countedArrays': sum(r['payload'] + r['physical'] + r['topology'] for r in group),
            'maxSetupScratch': max(r['scratch'] for r in group)})
    failures = [r for r in rows['GATE'] if r['k'] == 3 and not r['pass']]
    work_ratios = [{'case': c[0], 'size': c[1], 'class': c[2],
                    'ratio': quality[c, 3]['work'] / quality[c, 0]['work']} for c in sorted(expected)]
    budget_failures = [r for r in preparations if r['completeMs'] > 6000 or
                       r['probePayload'] > 20*1024**2 or r['countedArrays'] > 160*1024**2]
    observed_failures = [r for r in rows['PREP'] if r['managedObserved'] > 256*1024**2 or
                         r['unityObserved'] > 512*1024**2]
    favorable = not (failures or budget_failures or observed_failures or
                     any(r['ratio'] > 2 for r in work_ratios) or worst[3]['ratio'] > 2)
    return {'source': str(path), 'gate': gate, 'tests': root.get('passed'),
            'duration': root.get('duration'), **rows, 'preparations': preparations,
            'qualityFailures': failures, 'workRatios': work_ratios,
            'budgetFailures': budget_failures, 'observedMemoryFailures': observed_failures,
            'gateResult': 'FAVORABLE_NOT_CERTIFIED' if favorable else 'FAIL',
            'note': 'Offline; sin FPS ni latencia. Arrays/muestras no certifican residencia, picos ni p95 temporal.'}


if __name__ == '__main__':
    gate = sys.argv[3]
    if gate not in ('A', 'B'):
        raise ValueError('Puerta A o B requerida')
    result = extract(Path(sys.argv[1]), gate)
    Path(sys.argv[2]).write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: result[k] for k in ('tests', 'gate', 'gateResult')}))
    print('Fallos de calidad finales:', len(result['qualityFailures']))
    for variant in range(4):
        group = [r for r in result['GATE'] if r['k'] == variant]
        print(variant, 'failed', sum(not r['pass'] for r in group), 'p95', max(r['p95'] for r in group),
              'max', max(r['max'] for r in group))
