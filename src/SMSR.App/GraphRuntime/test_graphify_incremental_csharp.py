"""Ordinary typed C# is incremental; unsafe cross-file merges remain full."""
import os
from tempfile import TemporaryDirectory
from test_graphify_incremental import source, update, facts
from graphify_index import run

def check():
    old_local = os.environ['LOCALAPPDATA']
    with TemporaryDirectory(prefix='smsr-incremental-csharp-') as folder:
        os.environ['LOCALAPPDATA'] = folder
        try:
            files = [source('Base.cs', 'namespace Lib; public class Base { public void Run() {} }'),
                source('Child.cs', 'namespace App; using Lib; public class Child { public void Go() { Base worker = new Base(); worker.Run(); } }'),
                source('Alone.cs', 'namespace Other; public class Alone { public void Silent() {} }')]
            run(dict(files=files, cacheId='C' * 64, previousManifest={}))
            old = files[:]
            files[1] = source('Child.cs', 'namespace App; using Lib; public class Child { public void Go() { Base worker = new Base(); worker.Run(); int count = 2; } }')
            value = update(files, old, cache='C' * 64)
            assert value['reparsedPaths'] == ['Child.cs'], value['analysis']
            assert any(e['relation'] == 'CALLS' and e['ownerPath'] == 'Child.cs' for e in value['edges'])
            old = files[:]
            files[0] = source('Base.cs', 'namespace Lib; public class Base { public void Renamed() {} }')
            update(files, old, cache='C' * 64, partial=False)
            old = files[:]
            files[0] = source('Base.cs', 'namespace Lib; public class Base { public void Run() {} public void Renamed() {} }')
            restored = update(files, old, cache='C' * 64)
            assert any(e['relation'] == 'CALLS' and e['ownerPath'] == 'Child.cs' for e in restored['edges'])
            old = files[:]
            files[1] = source('Child.cs', 'namespace App; using Worker = Lib.Base; public class Child { public void Go() { Worker worker = new Worker(); worker.Run(); } }')
            update(files, old, cache='C' * 64, partial=False)
            old = files[:]
            files.append(source('Duplicate.cs', 'namespace Elsewhere; public class Base { public void Run() {} }'))
            duplicate = update(files, old, cache='C' * 64, partial=False)
            targets = {n['nodeId']: n['ownerPath'] for n in duplicate['nodes']}
            assert all(targets[e['targetId']] == 'Base.cs' for e in duplicate['edges']
                if e['relation'] == 'CALLS' and e['ownerPath'] == 'Child.cs')
            old = files[:]
            files.append(source('AFirst.cs', 'namespace Lib; public class Earlier {}'))
            relocated = update(files, old, cache='C' * 64, partial=False)
            assert relocated['analysis']['mode'] == 'FULL', 'Canonical namespace owner drift reused'
            old = files[:]
            files.append(source('Partial.cs', 'namespace Lib; public partial class Split { public void Piece() {} }'))
            value = update(files, old, cache='C' * 64, partial=False)
            assert value['analysis']['mode'] == 'FULL' and 'partial' in value['analysis']['fallbackReason']
            print('C# typed context/change/rename/new method/alias/duplicate/namespace/partial full equivalence OK')
        finally:
            os.environ['LOCALAPPDATA'] = old_local

if __name__ == '__main__':
    check()
