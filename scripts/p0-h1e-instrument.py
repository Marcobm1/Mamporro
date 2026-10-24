"""Copias diagnósticas mecánicas; jamás escribe producción ni H1d original."""
import argparse
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TESTS = ROOT / 'unity/Assets/Mamporro/Tests/Core'

def once(text, old, new):
    assert text.count(old) == 1, (old, text.count(old))
    return text.replace(old, new)

def copies():
    text = (ROOT / 'unity/Assets/Mamporro/Core/World/ProceduralValidation.cs').read_text(encoding='utf-8-sig')
    start = text.index('    public sealed class ProceduralMap')
    text = 'using System;\nusing System.Collections.Generic;\nusing Mamporro.Core;\nnamespace Mamporro.Tests\n{\n' + text[start:]
    text = text.replace('ProceduralMap', 'H1eMeasuredMap')
    text = once(text, 'Layout=new ProceduralLayout(seed,settings,first);World=new ProceduralWorld(Layout);',
                'H1eMemoryTrace.Begin("generation");Layout=new ProceduralLayout(seed,settings,first);World=new ProceduralWorld(Layout);H1eMemoryTrace.End();')
    text = once(text, 'PreRepair();', 'H1eMemoryTrace.Begin("precheck");PreRepair();H1eMemoryTrace.End();')
    text = once(text, 'Build();Check=Validate();', 'H1eMemoryTrace.Begin("originalGraphs");Build();H1eMemoryTrace.End();H1eMemoryTrace.Begin("validation");Check=Validate();H1eMemoryTrace.End();')
    yield 'H1eMeasuredMap.cs', text
    text = (TESTS / 'ReusedRegionalWorld.cs').read_text(encoding='utf-8-sig').replace('ReusedRegionalWorld','H1eMeasuredWorld')
    text = once(text, 'var watch=System.Diagnostics.Stopwatch.StartNew();', 'H1eMemoryTrace.Begin("projectionAndPortals");var watch=System.Diagnostics.Stopwatch.StartNew();')
    text = once(text, 'Component=StrongComponents', 'H1eMemoryTrace.End();H1eMemoryTrace.Begin("SCC");Component=StrongComponents')
    text = once(text, 'TopologyMs=BuildMs-physicalMs;', 'TopologyMs=BuildMs-physicalMs;H1eMemoryTrace.End();')
    yield 'H1eMeasuredWorld.cs', text
    text = (TESTS / 'ReusedCorridorProbe.cs').read_text(encoding='utf-8-sig').replace('ReusedRegionalWorld','H1eMeasuredWorld').replace('ReusedCorridorProbe','H1eMeasuredIndex')
    yield 'H1eMeasuredIndex.cs', text

if __name__ == '__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--write',action='store_true');args=parser.parse_args()
    for name, text in copies():
        target=TESTS/name
        if args.write: target.write_text(text,encoding='utf-8')
        else: assert target.read_text(encoding='utf-8-sig') == text, name
        print(name, 'OK')
