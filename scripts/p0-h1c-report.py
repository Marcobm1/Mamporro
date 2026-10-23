"""H1c: separa medidas reales, diagnósticos de calidad y oportunidades no implementadas."""
import json
import math
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def p95(values):
    return sorted(values)[math.ceil(.95*len(values))-1]


def extract(path):
    root = ET.parse(path).getroot()
    if root.get('result') != 'Passed':
        raise ValueError('Suite fallida: no certificar el diagnóstico')
    rows = {tag: [] for tag in ('PREP', 'CLASS', 'COLD', 'MOVE', 'REUSE', 'PAIRFAIL')}
    for case in root.iter('test-case'):
        for output in case.findall('output'):
            for line in (output.text or '').splitlines():
                fields = line.split()
                if not fields or fields[0] not in ['H1C-'+tag for tag in rows]:
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
    matrix = {(seed, size) for seed in ('P0QA', 'MAMPORRO', 'HORDA750', 'Z9K2', 'RAMPA42')
              for size in (16, 32, 64)}
    result = {'source': str(path), 'tests': root.get('passed'), 'duration': root.get('duration'), **rows,
              'note': 'Instrumentación offline; sin servicio ni FPS. Ninguna preparación optimizada implementada.'}
    if rows['PREP']:
        assert len(rows['PREP']) == 45 and len(rows['CLASS']) == 135
        assert {(r['case'], r['size'], r['trial']) for r in rows['PREP']} == {
            (seed, size, trial) for seed, size in matrix for trial in range(3)}
        summaries = []
        for seed, size in sorted(matrix):
            group = [r for r in rows['PREP'] if (r['case'], r['size']) == (seed, size)]
            summaries.append({'case': seed, 'size': size, 'samples': len(group),
                              'actualP95Ms': p95([r['actualMs'] for r in group]),
                              'actualMaxMs': max(r['actualMs'] for r in group),
                              'means': {k: sum(r[k] for r in group)/len(group) for k in
                                        ('generationMs', 'precheckMs', 'originalMapsMs', 'validationMs',
                                         'mapResidualMs', 'circuitAndWrapperMs', 'd1Ms', 'd2Ms', 'indexMs',
                                         'projectionMs', 'certificateMs', 'diagnosticWallMs')},
                              'measuredStartupSavingMs': 0})
        for row in rows['PREP']:
            assert abs(row['accountedMs'] - sum(row[k] for k in ('baseMs', 'd1Ms', 'd2Ms', 'indexMs'))) < .01
            assert row['diagnosticWallMs'] >= row['actualMs']
            assert row['builds'] >= 1
        result['preparationSummaries'] = summaries
        result['preparationStatus'] = 'UNOPTIMIZED_MEASURED'
        result['preparationOver4s'] = sum(r['actualMs'] > 4000 for r in rows['PREP'])
        result['preparationOver6s'] = sum(r['actualMs'] > 6000 for r in rows['PREP'])
        result['diagnosticOver6s'] = sum(r['diagnosticWallMs'] > 6000 for r in rows['PREP'])
        result['preparationGate'] = ('FAIL' if any(r['actualP95Ms'] > 4000 or r['actualMaxMs'] > 6000
                                                 for r in summaries) else 'NOT_CERTIFIED')
    if rows['REUSE']:
        assert len(rows['REUSE']) == 45
        assert {(r['case'], r['size'], r['class']) for r in rows['REUSE']} == {
            (seed, size, body) for seed, size in matrix for body in range(3)}
        for row in rows['REUSE']:
            group = [r for r in rows['MOVE'] if all(r[k] == row[k] for k in ('case', 'size', 'class'))]
            assert len(group) == row['pairs']
            assert sum(r['fullWork'] for r in group) == row['fullWork']
            assert sum(r['localWork'] for r in group) == row['localWork']
            assert all(r['localWork'] == r['localInit']+r['localPhysical'] for r in group)
            for move in group:
                if 'extractions' in move:
                    assert move['localPhysical'] == sum(move[k] for k in
                                                       ('extractions', 'examinedEdges', 'relaxationAttempts'))
                    assert move['improvements'] <= move['relaxationAttempts']
            assert all(0 < r['domain'] <= r['nodes'] for r in group)
            row['measuredSearchSavingFraction'] = 1-row['localWork']/row['fullWork'] if row['fullWork'] else 0
            row['coldAndIndexSavingFraction'] = 1-(row['coldWork']+row['localIndexWork']+row['localWork']) / (
                row['coldWork']+row['fullWork']) if row['coldWork']+row['fullWork'] else 0
        result['qualityFailures'] = [r for r in rows['REUSE'] if r['p95'] > 1.5 or r['max'] > 2]
        result['freshQualityFailures'] = [r for r in rows['REUSE'] if r['freshP95'] > 1.5 or r['freshMax'] > 2]
        result['queryQualityFailures'] = [r for r in rows['MOVE'] if r['p95'] > 1.5 or r['max'] > 2]
        result['reuseQuality'] = 'FAIL' if result['qualityFailures'] or result['queryQualityFailures'] else 'PASS_SAMPLED'
        result['reuseSizeSummaries'] = []
        for size in (16, 32, 64):
            group = [r for r in rows['REUSE'] if r['size'] == size]
            moves = [r for r in rows['MOVE'] if r['size'] == size]
            totals = {k: sum(r[k] for r in group) for k in
                      ('pairs', 'coldWork', 'fullWork', 'localWork', 'macroReusable', 'localIndexWork')}
            result['reuseSizeSummaries'].append({
                'size': size, **totals,
                'worstCaseP95': max(r['p95'] for r in group),
                'worstQueryP95': max(r['p95'] for r in moves),
                'maxRatio': max(r['max'] for r in group),
                'failedCases': sum(r['p95'] > 1.5 or r['max'] > 2 for r in group),
                'failedQueries': sum(r['p95'] > 1.5 or r['max'] > 2 for r in moves),
                'searchSavingFraction': 1-totals['localWork']/totals['fullWork'],
                'coldAndIndexSavingFraction': 1-(totals['coldWork']+totals['localIndexWork']+totals['localWork']) / (
                    totals['coldWork']+totals['fullWork']),
                'macroReusableFraction': totals['macroReusable']/totals['fullWork'],
                'note': 'Fracciones de contadores de búsqueda; heap, reconstrucción y CPU se informan aparte.'})
    result['nextStep'] = 'STOP_REVIEW_NO_H2'
    return result


if __name__ == '__main__':
    report = extract(Path(sys.argv[1]))
    Path(sys.argv[2]).write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({k: report[k] for k in ('tests', 'preparationStatus', 'reuseQuality', 'nextStep') if k in report}))
    if 'qualityFailures' in report:
        print('Fallos de calidad local:', len(report['qualityFailures']), 'de', len(report['REUSE']))
