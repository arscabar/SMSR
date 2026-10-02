"""One request per isolated process. No request/source text is logged."""
import json
import sys

def main():
    raw = sys.stdin.buffer.read(64 * 1024 * 1024 + 1)
    if len(raw) > 64 * 1024 * 1024:
        raise ValueError('Request exceeds 64 MiB')
    request = json.loads(raw)
    operation = request['operation']
    if operation != 'overview' and len(raw) > 32 * 1024 * 1024:
        raise ValueError('Request exceeds operation limit')
    if operation == 'cypher':
        from cypher_query import run
    elif operation == 'embed':
        from embedding import run
    elif operation == 'analyze':
        from symbols import run
    elif operation == 'index-code':
        from graphify_index import run
    elif operation == 'document':
        from document_input import run
    elif operation == 'document-preview':
        from document_preview import run
    elif operation == 'media':
        from media_input import run
    elif operation == 'overview':
        from graph_overview import run
    elif operation == 'java':
        from java_bundle import run
    elif operation == 'typescript':
        from typescript_bundle import run
    else:
        raise ValueError('Unknown operation')
    return run(request)

try:
    data = main()
    output = json.dumps(data, ensure_ascii=True, default=str, allow_nan=False)
    limit = 64 if isinstance(data, dict) and data.get('engine') == 'Graphify' else 16
    if len(output) > limit * 1024 * 1024:
        raise ValueError('Result exceeds operation limit')
    print(output)
except Exception as error:
    # Engine exception text may quote queries or source, so only expose a category.
    print(json.dumps({'error': type(error).__name__ + ': local graph operation failed; check input, runtime and limits'}))
    sys.exit(1)
