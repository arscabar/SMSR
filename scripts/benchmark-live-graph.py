"""Loopback latency."""
import argparse
import http.client
import json
import os
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from time import perf_counter
from urllib.parse import urlencode
from graph_benchmark_oracle import cases, stats, verify_impact, verify_path
from graph_benchmark_snapshot import snapshot


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=49783)
    parser.add_argument('--project', default='SMSR')
    parser.add_argument('--database', type=Path, default=Path(os.environ['LOCALAPPDATA'])/'SMSR/smsr.db')
    args = parser.parse_args()
    client = http.client.HTTPConnection('127.0.0.1', args.port, timeout=30)

    def get(endpoint, **params):
        url = '/api/graph/'+endpoint+'?'+urlencode(dict(projectId=args.project, **params))
        start = perf_counter()
        client.request('GET', url)
        response = client.getresponse()
        body = response.read()
        elapsed = (perf_counter()-start)*1000
        assert response.status == 200, (endpoint, response.status)
        return json.loads(body), elapsed

    try:
        health, _ = get('health')
        revision, nodes, rows = snapshot(args.database, args.project)
        assert health['info']['revision'] == revision
        assert health['info']['nodeCount'] == len(nodes)
        assert health['info']['edgeCount'] == len(rows)
        paths, impacts, _, reverse = cases(nodes, rows)
        edges, timings, truncated = set(rows), {'path': [], 'impact': []}, 0
        for i in range(-5, 40):
            source, target, distance = paths[i % 40]
            result, elapsed = get('path', fromId=source, toId=target, maxDepth=8)
            assert result['revision'] == revision
            verify_path(result, source, target, distance, edges)
            if i >= 0:
                timings['path'].append(elapsed)
            source = impacts[i % 40]
            result, elapsed = get('impact', nodeId=source, maxDepth=8)
            assert result['revision'] == revision
            verify_impact(result, source, reverse)
            if i >= 0:
                timings['impact'].append(elapsed)
                truncated += result['truncated']
        after, _ = get('health')
        assert after['info'] == health['info'], 'Revision changed'
        report = dict(time=datetime.now(timezone.utc).isoformat(), info=health['info'],
                      warmupPerKind=5, targetP95Ms=2000, impactTruncated=truncated,
                      pathLengths=dict(Counter(str(p[2]) for p in paths)),
                      healthIssues=health['issueCount'], **{k: stats(v) for k,v in timings.items()})
        print(json.dumps(report, ensure_ascii=False, indent=2))
        assert all(stats(v)['p95Ms'] <= 2000 for v in timings.values()), 'Latency gate'
    finally:
        client.close()


if __name__ == '__main__':
    if not __debug__:
        raise RuntimeError('Run without -O')
    main()
