"""Reuse encrypted raw resolver facts only against the committed input manifest."""
from contextlib import contextmanager
from graphify_incremental_context import load, save, identifiers

@contextmanager
def session(request):
    if not request.get('cacheId') or 'previousManifest' not in request:
        yield None, None, {}
        return
    from graphify_cache import namespace
    from cache_maintenance import lease
    folder = namespace(request['cacheId'])
    with lease(folder) as maintenance:
        previous = load(folder)
        if previous and previous['manifest'] != request['previousManifest']:
            previous = None  # A prior extraction did not commit, or this is a different scope.
        yield folder, previous, maintenance

def persist(folder, root, known, report, issues, cache_id):
    if folder is None:
        return False
    from graphify_cache import namespace
    if folder != namespace(cache_id):
        raise ValueError('Analysis helpers changed; retain previous index')
    return save(folder, known, identifiers(root, known), report, issues)

def analysis(paths, mode='FULL', reason=None, reused=0, stored=False):
    return dict(mode=mode, affectedFiles=len(paths), reusedFiles=reused,
        fallbackReason=reason, contextStored=stored)

def merge(module, previous, fresh, affected, known):
    if previous is None:
        return fresh
    current = {item['path'] for item in known.values()}
    def retain(item):
        return item.get('source_file') in current - affected
    report = dict(nodes=[dict(n) for n in previous['report']['nodes'] if retain(n)] + fresh['nodes'],
        edges=[dict(e) for e in previous['report']['edges'] if retain(e)] + fresh['edges'],
        hyperedges=[dict(h) for h in previous['report'].get('hyperedges', []) if retain(h)] + fresh.get('hyperedges', []),
        failed_sources=fresh.get('failed_sources', []))
    module._canonicalize_csharp_namespace_nodes(report['nodes'], report['edges'])
    # Upstream's early C# using pass precedes context injection; rerun its exact rules.
    from graphify.extractors.csharp import _resolve_cross_file_csharp_imports
    _resolve_cross_file_csharp_imports([], [], report['nodes'], report['edges'])
    owners = {}
    for node in report['nodes']:
        identity = str(node['id'])
        if identity in owners and owners[identity] != node.get('source_file'):
            raise ValueError('Partial symbol identity collision')
        owners[identity] = node.get('source_file')
    return report
