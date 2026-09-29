from itertools import product
import os
import subprocess
import sys
from pathlib import Path
from python_compiler import analyze
from python_call_summaries import analyze as summarize


def verify():
    # Independently enumerate inner-body paths, then permute caller arguments.
    for mask in range(16):
        lines=['  result=0']
        for i in range(4):lines.append(f'  if p{i}: result='+('xy'[i%2] if mask&(1<<i) else '0'))
        source='def outer(a,b,p0,p1,p2,p3):\n def inner(x,y,p0,p1,p2,p3):\n'
        source+='\n'.join(lines)+'\n  return result\n return inner(b,a,p0,p1,p2,p3)\n'
        wanted=set()
        for flags in product((False,True),repeat=4):
            last=None
            for i,flag in enumerate(flags):
                if flag:last=(1,0)[i%2] if mask&(1<<i) else None
            if last is not None:wanted.add(last)
        fs=analyze(source,'oracle.py')['functions'];outer=fs[1]['valueSummary']
        assert {p for r in outer['returns'] for p in r['parameterIndices']}==wanted
        assert not any(r['unknownValueIds'] for r in outer['returns'])
    def reject(_):raise ValueError('call summary budget')
    for spend,work in ((reject,lambda _:None),(lambda _:None,reject)):
        try:summarize(fs,spend,work)
        except ValueError as e:assert str(e)=='call summary budget'
        else:raise AssertionError('Call summary limit missing')
    command='import json,hashlib;from test_python_call_summaries import SOURCE;from python_compiler import analyze;print(hashlib.sha256(json.dumps(analyze(SOURCE,"stable.py"),sort_keys=True).encode()).hexdigest())'
    hashes=[subprocess.check_output([sys.executable,'-c',command],cwd=Path(__file__).parent,
            env={**os.environ,'PYTHONHASHSEED':seed},timeout=20,text=True).strip() for seed in ('1','17','997')]
    assert len(set(hashes))==1,'Cross-process hash-seed instability'
