"""Reuse the pinned Graphify extraction code, without its CLI, server or LLM."""
import sys
from contextlib import redirect_stdout
import tempfile
from pathlib import Path
from graphify_grammars import install
from graphify_snapshot import create
from graphify_normalize import normalize, line_of
from graphify_support import supported, missing
from graphify_cache import reuse
from graphify_batches import files
from graphify_diagnostics import collect, issues
from graphify_call_diagnostics import unmatched
from graphify_call_sites import augment
from graphify_incremental import session, persist, analysis
from graphify_incremental_extract import extract_scope
from graphify_declarations import annotate_report
from graphify_project_metadata import annotate as project_metadata
from graphify_xaml_links import augment as xaml_links

def run(request):
    install()
    sys.path.insert(0, str(Path(__file__).parent / 'vendor'))
    with redirect_stdout(sys.stderr):
        import graphify.extract as module
    with tempfile.TemporaryDirectory(prefix='smsr-code-index-') as folder:
        root = Path(folder).resolve()
        known = create(root, files(request))
        paths = [root / item['path'] for item in known.values()
                 if supported(module, item['path'])]
        # Context and AST files share a process-safe cache lease; legacy callers remain unchanged.
        with session(request) as (folder, previous, maintenance), reuse(module, root,
                request.get('cacheId'), retained_paths=paths, namespace_folder=folder) as stats, collect(module, root) as parsed, redirect_stdout(sys.stderr):
            report, result, selected, context, reason = extract_scope(module, root, known, paths, previous, request)
            augment(report, selected, parsed, module, root)
            annotate_report(report, selected, root)
            project_metadata(report, root)
            xaml_links(report, root)
            result = normalize(report, root, known)
            affected = {p.relative_to(root).as_posix() for p in selected}
            result['cache'] = stats if request.get('cacheId') else None
            extra = [i for i in previous['issues'] if i['ownerPath'] not in affected and i['ownerPath'].casefold() in known] if context else []
            extra.extend(issues(selected, parsed, module, root))
            extra.extend(unmatched(report, selected, parsed, module, root, known))
            extra.extend(missing(known))
            result['issues'].extend(extra)
            stored = persist(folder, root, known, report, extra, request.get('cacheId'))
            if 'previousManifest' in request:
                result['reparsedPaths'] = [p.relative_to(root).as_posix() for p in selected]
                result['analysis'] = analysis(selected, 'INCREMENTAL' if context else 'FULL',
                    reason=reason, reused=len(paths) - len(selected), stored=stored)
        if result['cache'] and folder is not None:
            result['cache']['maintenance'] = maintenance
        return result
