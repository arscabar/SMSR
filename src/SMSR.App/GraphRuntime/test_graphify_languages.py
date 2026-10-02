"""Exercise additional extractors and reject a falsely complete optional-engine result."""
import hashlib
from graphify_index import run
from language_extension_cases import CASES

def check():
    for extension, text in CASES.items():
        item = dict(path='src/sample' + extension, text=text,
            hash=hashlib.sha256(text.encode()).hexdigest().upper())
        result = run(dict(files=[item]))
        assert result['version'] == '0.9.73'
        assert result['nodes'] or result['edges'], (extension, result)
        assert result == run(dict(files=[item])), 'Unstable ' + extension
        print(extension, len(result['nodes']), len(result['edges']), len(result['issues']))
    text = '/datum/sample'
    result = run(dict(files=[dict(path='sample.dm', text=text,
        hash=hashlib.sha256(text.encode()).hexdigest().upper())]))
    assert not result['nodes']
    assert any(i['relation'] == 'STRUCTURE_UNSUPPORTED' for i in result['issues'])
    print('Additional structural dispatch and explicit missing-engine checks passed')

if __name__ == '__main__':
    check()
