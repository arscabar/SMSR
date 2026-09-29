"""One request per isolated process. No request/source text is logged."""
import json
import sys

def main():
    raw = sys.stdin.buffer.read(32 * 1024 * 1024 + 1)
    if len(raw) > 32 * 1024 * 1024:
        raise ValueError('Request exceeds 32 MiB')
    request = json.loads(raw)
    operation = request['operation']
    if operation == 'cypher':
        from cypher_query import run
    elif operation == 'embed':
        from embedding import run
    elif operation == 'analyze':
        from symbols import run
    elif operation == 'java':
        from java_bundle import run
    elif operation == 'typescript':
        from typescript_bundle import run
    else:
        raise ValueError('Unknown operation')
    return run(request)

try:
    output = json.dumps(main(), ensure_ascii=True, default=str, allow_nan=False)
    if len(output) > 16 * 1024 * 1024:
        raise ValueError('Result exceeds 16 MiB')
    print(output)
except Exception as error:
    # Engine exception text may quote queries or source, so only expose a category.
    print(json.dumps({'error': type(error).__name__ + ': local graph operation failed; check input, runtime and limits'}))
    sys.exit(1)
