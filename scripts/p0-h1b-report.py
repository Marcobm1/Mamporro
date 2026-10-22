"""Extrae H1b sin convertir el éxito NUnit en aceptación de calidad/presupuesto."""
import json
import math
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


def extract_load(path):
    root = ET.parse(path).getroot()
    if root.get('result') != 'Passed':
        raise ValueError('Medición incompleta o fallida')
    rows = []
    for case in root.iter('test-case'):
        for output in case.findall('output'):
            for line in (output.text or '').splitlines():
                fields = line.split()
                if not fields or fields[0] != 'H1B-LOAD':
                    continue
                row = {}
                for field in fields[1:]:
                    key, value = field.split('=', 1)
                    row[key] = value if key == 'case' else float(value)
                rows.append(row)
    expected = {(size, trial) for size in (16, 32, 64) for trial in range(10)}
    assert len(rows) == 30 and {(r['size'], r['trial']) for r in rows} == expected
    assert all(r['case'] == 'MAMPORRO' for r in rows)
    summaries = []
    for size in (16, 32, 64):
        values = sorted(r['completeMs'] for r in rows if r['size'] == size)
        p95 = values[math.ceil(.95*len(values))-1]
        summaries.append({'size': size, 'samples': len(values), 'p95Ms': p95, 'maxMs': max(values),
                          'pass': p95 <= 4000 and max(values) <= 6000})
    return {'source': str(path), 'gate': 'Preparation', 'tests': root.get('passed'),
            'duration': root.get('duration'), 'rows': rows, 'summaries': summaries,
            'gateResult': 'NOT_CERTIFIED' if all(s['pass'] for s in summaries) else 'FAIL',
            'note': 'Diez arranques por tamaño del caso limitante; no certifica todas las semillas ni picos de memoria.'}


if __name__ == '__main__':
    if sys.argv[1] == '--summary':
        inputs = [Path(p) for p in sys.argv[2:5]]
        a, b, preparation = [json.loads(p.read_text(encoding='utf-8')) for p in inputs]
        assert a['gate'] == 'A' and b['gate'] == 'B' and preparation['gate'] == 'Preparation'
        failed = any(r['gateResult'] == 'FAIL' for r in (a, b, preparation))
        summary = {'sources': [str(p) for p in inputs],
                   'overallGate': 'FAIL' if failed else 'NOT_CERTIFIED',
                   'qualityGate': 'FAIL' if a['qualityFailures'] or b['qualityFailures'] else 'PASS',
                   'preparationGate': preparation['gateResult'], 'memoryGate': 'NOT_CERTIFIED',
                   'nextStep': 'STOP_REVIEW_BEFORE_H2',
                   'note': 'Calidad offline favorable no acepta presupuesto, servicio ni movimiento.'}
        Path(sys.argv[5]).write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
        print(json.dumps(summary))
        sys.exit(0)
    gate = sys.argv[3]
    if gate not in ('A', 'B', 'L'):
        raise ValueError('Puerta A, B o L (preparación) requerida')
    result = extract_load(Path(sys.argv[1])) if gate == 'L' else extract(Path(sys.argv[1]), gate)
    Path(sys.argv[2]).write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: result[k] for k in ('tests', 'gate', 'gateResult')}))
    if gate == 'L':
        print(json.dumps(result['summaries']))
        sys.exit(0)
    print('Fallos de calidad finales:', len(result['qualityFailures']))
    for variant in range(4):
        group = [r for r in result['GATE'] if r['k'] == variant]
        print(variant, 'failed', sum(not r['pass'] for r in group), 'p95', max(r['p95'] for r in group),
              'max', max(r['max'] for r in group))
