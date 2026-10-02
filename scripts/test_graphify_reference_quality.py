"""Offline extraction regression: real XAML properties and honest diagnostics."""
import hashlib
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'src/SMSR.App/GraphRuntime'))
from graphify_index import run
from graphify_issue_candidates import describe

def check():
    sources = {
        'Demo.csproj': '<Project><ItemGroup><PackageReference Include="Example.Core" Version="1.2.3" /></ItemGroup></Project>',
        'DemoViewModel.cs': '''namespace Demo;
public class DemoViewModel
{
    public string Title { get; set; }
    public object SaveCommand { get; set; }
}
''',
        'DemoView.xaml': '''<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="clr-namespace:Demo" x:Class="Demo.DemoView">
 <Window.DataContext><local:DemoViewModel /></Window.DataContext>
 <TextBlock Text="{Binding Title}" />
 <Button Command="{Binding SaveCommand}" />
</Window>'''
    }
    def extract(values):
        return run(dict(files=[dict(path=p, text=t, hash=hashlib.sha256(t.encode()).hexdigest().upper()) for p,t in values.items()]))
    result = extract(sources)
    nodes = {n['nodeId']: n for n in result['nodes']}
    packages = [n for n in nodes.values() if n['label']=='Example.Core (1.2.3)']
    assert len(packages)==1 and packages[0]['details']['entityKind']=='concept'
    for name, relation in [('Title','BINDS_TO'),('SaveCommand','COMMAND_BINDS_TO')]:
        edges = [e for e in result['edges'] if e['relation']==relation and nodes.get(e['targetId'],{}).get('details',{}).get('entityKind')=='property']
        assert len(edges)==1 and nodes[edges[0]['targetId']]['label']==name, (name, edges)
        assert nodes[edges[0]['targetId']]['details']['ownerNodeId'] in nodes
    sources['DemoView.xaml'] = sources['DemoView.xaml'].replace('{Binding Title}', '{Binding Title, ElementName=Other}')
    negative = extract(sources)
    lookup = {n['nodeId']: n for n in negative['nodes']}
    assert not any(e['relation']=='BINDS_TO' and lookup.get(e['targetId'],{}).get('details',{}).get('entityKind')=='property' for e in negative['edges'])
    sources['DemoView.xaml'] = sources['DemoView.xaml'].replace('{Binding Title, ElementName=Other}', '{Binding Title}').replace('<TextBlock Text="{Binding Title}" />', '<DataTemplate><TextBlock Text="{Binding Title}" /></DataTemplate>')
    template = extract(sources)
    lookup = {n['nodeId']: n for n in template['nodes']}
    assert not any(e['relation']=='BINDS_TO' and lookup.get(e['targetId'],{}).get('details',{}).get('entityKind')=='property' for e in template['edges'])
    reason, paths = describe('token=do-not-record-12345678', ['b.cs','a.cs'], True)
    assert reason.startswith('AMBIGUOUS_TARGET') and 'do-not-record' not in reason and paths==['a.cs','b.cs']
    assert describe('External',[])[0].startswith('EXTERNAL_OR_MISSING_SOURCE')
    print('PASS: project metadata, AST-owned XAML property/command, source override refusal, safe diagnostics')

if __name__=='__main__':
    check()
