"""Extrae evidencia H1e y descompone el indicador histórico sin certificar presupuestos."""
import argparse
import json
import math
import xml.etree.ElementTree as ET
from pathlib import Path

def extract(path):
    root=ET.parse(path).getroot()
    records=[]
    for output in root.iter('output'):
        for line in (output.text or '').splitlines():
            if not line.startswith('H1E-'):continue
            parts=line.split();record={'kind':parts[0][4:]}
            for part in parts[1:]:
                if '=' not in part:continue
                key,value=part.split('=',1)
                try:value=int(value)
                except ValueError:
                    try:value=float(value)
                    except ValueError:pass
                record[key]=value
            records.append(record)
    return {'source':str(path),'tests':root.attrib,'records':records}

def summarize(data, historical):
    rows=[r for d in data for r in d['records']]
    phase=[r for r in rows if r['kind']=='PHASE']
    loads=[r for r in rows if r['kind']=='LOAD']
    report={'gateA':'VALIDATED_AGGREGATES_ATTRIBUTION_PARTIAL' if loads else 'SEE_CALIBRATION',
            'gateB':'MEASURED' if loads else 'NO_COMPARISON_IN_INPUT','overall':'NOT_ACCEPTED',
            'allocationScope':'all threads in synchronous frame window; not exclusively navigation',
            'navigationResident':None,'navigationTransientPeak':None,
            'phases':{}}
    for name in sorted({r['phase'] for r in phase}):
        subset=[r for r in phase if r['phase']==name]
        report['phases'][name]={'samples':len(subset),'heapPeakObserved':max(r['peak'] for r in subset),
            'largestEndMinusStart':max(r['end']-r['start'] for r in subset),
            'gc':sum(r['gc'] for r in subset),'allocationBytesMax':max((r['allocated'] for r in subset if isinstance(r['allocated'],int)),default=None),'attributableBytes':None}
    report['comparisons']=[]
    for seed,size,mode in sorted({(r['seed'],r['size'],r['mode']) for r in loads}):
        subset=[r for r in loads if (r['seed'],r['size'],r['mode'])==(seed,size,mode)]
        times=sorted(r['totalMs'] for r in subset)
        report['comparisons'].append({'seed':seed,'size':size,'mode':mode,'n':len(subset),
            'p95Ms':times[math.ceil(len(times)*.95)-1],
            **{k+'Max':max(r[k] for r in subset) for k in ('totalMs','diagnosticMs','peak','retained','excess','allocated','ownedArrayBytes','unityObserved','workingObserved','privateObserved','maxGapMs')},
            'gcTotal':sum(r['gc'] for r in subset),
            'excessOver96':sum(r['excess']>96*2**20 for r in subset),
            'observedManagedOver256':sum(r['peak']>256*2**20 for r in subset)})
    diag=[r for r in rows if r['kind']=='DIAG']
    report['diagnosticLoads']=len(diag)
    report['diagnosticInterventions']=[{'seed':r['seed'],'size':r['size'],'releasedBeforeProjection':r['mapEnd']-r['beforeProjection']} for r in diag if r['forcedBetween']=='True']
    if historical:
        old=json.loads(Path(historical).read_text(encoding='utf-8-sig'))['LOAD']
        maximum=max((r for r in old if r['mode']=='reused'),key=lambda r:r['transientObserved'])
        control=next(r for r in old if r['mode']=='original' and all(r[k]==maximum[k] for k in ('case','size','trial')))
        report['historicalMaximum']={'source':str(historical),'reused':maximum,'control':control,
            'excessDifference':maximum['transientObserved']-control['transientObserved'],
            'peakDifference':maximum['managedPeak']-control['managedPeak'],
            'retentionDifference':control['managedRetained']-maximum['managedRetained']}
        assert report['historicalMaximum']['excessDifference']==report['historicalMaximum']['peakDifference']+report['historicalMaximum']['retentionDifference']
    return report

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('output');p.add_argument('xml',nargs='+');p.add_argument('--historical');args=p.parse_args()
    data=[extract(Path(path)) for path in args.xml]
    result={'runs':data,'summary':summarize(data,args.historical)}
    Path(args.output).write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result['summary'],ensure_ascii=False,indent=2))
