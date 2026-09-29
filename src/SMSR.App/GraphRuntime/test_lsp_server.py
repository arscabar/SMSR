"""Local self-check fixture only; not an installed language analysis server."""
import json
import subprocess
import sys
import time

mode = sys.argv[1]
opened = closed = False

def read():
    headers = {}
    while True:
        line = sys.stdin.buffer.readline()
        if line == b'\r\n':
            break
        if not line:
            raise EOFError()
        key, value = line.decode('ascii').split(':', 1)
        headers[key.lower()] = value.strip()
    return json.loads(sys.stdin.buffer.read(int(headers['content-length'])))

def send(value):
    body = json.dumps(dict(jsonrpc='2.0', **value), ensure_ascii=False).encode()
    frame = f'Content-Length: {len(body)}\r\n\r\n'.encode() + body
    for i in range(0, len(frame), 7):
        sys.stdout.buffer.write(frame[i:i+7])
        sys.stdout.buffer.flush()

while True:
    message = read()
    method = message.get('method')
    if method == 'initialize':
        if mode == 'profile':
            assert message['params']['initializationOptions'] == {'fixtureProfile': True}
        if mode == 'error':
            send(dict(id=message['id'], error=dict(code=-32603, message='FIXTURE_SECRET_MUST_NOT_LEAK')))
            continue
        if mode == 'wrong-id':
            send(dict(id=99999, result={}))
            continue
        if mode == 'oversize':
            sys.stdout.buffer.write(b'Content-Length: 999999999\r\n\r\n')
            sys.stdout.buffer.flush()
            time.sleep(120)
        send(dict(method='window/logMessage', params=dict(type=3, message='secret fixture not for logs')))
        encoding = 'utf-8' if mode == 'encoding' else 'utf-16'
        send(dict(id=message['id'], result=dict(capabilities=dict(positionEncoding=encoding,
            definitionProvider=True, textDocumentSync=1))))
    elif method == 'initialized':
        pass
    elif method == 'textDocument/didOpen':
        assert '😀' in message['params']['textDocument']['text']
        opened = True
    elif method == 'textDocument/definition':
        assert opened and message['params']['position'] == dict(line=1, character=0)
        for name, params in [('workspace/configuration', dict(items=[dict(section='fixture')])),
                             ('workspace/applyEdit', dict(edit={})), ('workspace/executeCommand', {})]:
            send(dict(id='server', method=name, params=params))
            reply = read()
            assert reply['id'] == 'server'
            if name.endswith('configuration'): assert reply['result'] == [None]
            elif name.endswith('applyEdit'): assert reply['result']['applied'] is False
            else: assert reply['error']['code'] == -32601
        uri = message['params']['textDocument']['uri']
        span = dict(start=dict(line=1, character=0), end=dict(line=1, character=6))
        send(dict(id=message['id'], result=[dict(uri=uri, range=span),
            dict(targetUri=uri, targetRange=span, targetSelectionRange=span),
            dict(uri='https://invalid.example/source.py', range=span)]))
    elif method == 'textDocument/didClose':
        closed = True
    elif method == 'smsr/testChild':
        child = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(120)'],
                                 creationflags=subprocess.CREATE_NO_WINDOW)
        send(dict(id=message['id'], result=child.pid))
    elif method == 'smsr/hang':
        time.sleep(120)
    elif method == 'shutdown':
        assert closed or mode == 'slow-exit'
        send(dict(id=message['id'], result=None))
    elif method == 'exit':
        if mode == 'slow-exit':
            time.sleep(120)
        sys.exit(0)
