"""Extrae H1d sin ocultar suites fallidas ni confundir payload con residencia."""
import json
import hashlib
import math
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def percentile(values):
    return sorted(values)[math.ceil(.95 * len(values)) - 1]


def extract(path):
    root = ET.parse(path).getroot()
    rows = {tag: [] for tag in ('EXACT', 'QUALITY', 'LOAD', 'ORDER')}
    failures = []
    for case in root.iter('test-case'):
        if case.get('result') != 'Passed':
            failures.append({'name': case.get('fullname'), 'result': case.get('result'),
                             'message': case.findtext('failure/message')})
        for output in case.findall('output'):
            for line in (output.text or '').splitlines():
                fields = line.split()
                if not fields or fields[0] not in ['H1D-' + t for t in rows]:
                    continue
                row = {}
                for field in fields[1:]:
                    k, v = field.split('=', 1)
                    try:
                        v = int(v)
                    except ValueError:
                        try:
                            v = float(v)
                        except ValueError:
                            pass
                    row[k] = v
                rows[fields[0][4:]].append(row)
    result = {'source': str(path), 'result': root.get('result'), 'passed': root.get('passed'),
              'failed': root.get('failed'), 'duration': root.get('duration'), 'failures': failures, **rows,
              'nextStep': 'STOP_REVIEW_NO_PRODUCTION'}
    matrix = {(seed, size, body) for seed in ('A', 'B', 'P0QA', 'MAMPORRO', 'COLINAS7', 'Z9K2',
              'MESETA', 'ACANTILADO', 'RAMPA42', 'HORDA750', 'barrier', 'roofs', 'drop', 'climb', 'ramp', 'pass')
              for size in (16, 32, 64) for body in range(3)}
    for tag in ('EXACT', 'QUALITY'):
        if rows[tag]:
            complete = len(rows[tag]) == len(matrix) and {(r['case'], r['size'], r['class']) for r in rows[tag]} == matrix
            ok = complete and root.get('result') == 'Passed'
            if tag == 'QUALITY':
                ok = ok and all(r['p95'] <= 1.5 and r['max'] <= 2 for r in rows[tag])
            result[tag.lower() + 'Gate'] = 'PASS_SAMPLED' if ok else 'FAIL'
    if rows['LOAD']:
        expected = {(seed, size, trial, mode) for seed in ('MAMPORRO', 'Z9K2', 'RAMPA42')
                    for size in (16, 32, 64) for trial in range(10) for mode in ('original', 'reused')}
        complete = len(rows['LOAD']) == len(expected) and {(r['case'], r['size'], r['trial'], r['mode']) for r in rows['LOAD']} == expected
        result['loadsComplete'] = complete
        result['loadSummary'] = []
        for seed in ('MAMPORRO', 'Z9K2', 'RAMPA42'):
            for size in (16, 32, 64):
                for mode in ('original', 'reused'):
                    group = [r for r in rows['LOAD'] if (r['case'], r['size'], r['mode']) == (seed, size, mode)]
                    if not group:
                        continue
                    result['loadSummary'].append({'case': seed, 'size': size, 'mode': mode, 'n': len(group),
                        'p95Ms': percentile([r['totalMs'] for r in group]), 'maxMs': max(r['totalMs'] for r in group),
                        'means': {k: sum(r[k] for r in group)/len(group) for k in group[0]
                                  if isinstance(group[0][k], (int, float)) and k not in ('trial', 'size')},
                        'maxima': {k: max(r[k] for r in group) for k in ('allocated', 'payload', 'instrumentPayload',
                                   'managedPeak', 'managedRetained', 'unityObserved', 'transientObserved')}})
        result['preparationGate'] = 'PASS_SAMPLED' if complete and root.get('result') == 'Passed' and all(
            r['p95Ms'] <= 4000 and r['maxMs'] <= 6000 for r in result['loadSummary'] if r['mode'] == 'reused') else 'FAIL'
        result['memoryGate'] = 'NOT_CERTIFIED_SAMPLED_PEAKS'
    return result


if __name__ == '__main__':
    if sys.argv[1] == '--verify-solver':
        folder = Path(__file__).resolve().parent.parent / 'unity/Assets/Mamporro/Tests/Core'
        original = (folder / 'AdaptiveCorridorProbe.cs').read_text(encoding='utf-8')
        reused = (folder / 'ReusedCorridorProbe.cs').read_text(encoding='utf-8')
        assert original.replace('AdaptiveCorridorProbe', 'ReusedCorridorProbe').replace('NavPortalWorld', 'ReusedRegionalWorld') == reused
        print(json.dumps({'solverTransformation': 'EXACT_TWO_TYPE_NAMES_ONLY',
                          'originalNormalizedSha256': hashlib.sha256(original.encode()).hexdigest()}))
        sys.exit(0)
    report = extract(Path(sys.argv[1]))
    Path(sys.argv[2]).write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in report.items() if k in ('result', 'passed', 'failed', 'exactGate',
          'qualityGate', 'preparationGate', 'memoryGate', 'nextStep')}))
