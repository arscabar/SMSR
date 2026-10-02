"""One-byte OS locks coordinate app, hook and worker cache users."""
import os
import time
from contextlib import contextmanager
from threading import RLock, get_ident
from cache_paths import checked, regular

_gate, _held, _threads = RLock(), {}, {}

def owned(path):
    with _gate:
        return _held.get(str(checked(path))) == get_ident()

@contextmanager
def locked(path, wait=True):
    path = checked(path)
    key = str(path)
    with _gate:
        mutex = _threads.setdefault(key, RLock())
    if not mutex.acquire(blocking=wait):
        yield False
        return
    handle = None
    nested = False
    try:
        with _gate:
            nested = _held.get(key) == get_ident()
        if nested:
            yield wait  # Cleanup probes never reenter an active lease.
            return
        checked(path.parent).mkdir(parents=True, exist_ok=True)
        checked(path)
        handle = path.open('a+b')
        regular(path)
        if not handle.seek(0, os.SEEK_END):
            handle.write(b'0')
            handle.flush()
        deadline = time.monotonic() + 30
        while True:
            try:
                handle.seek(0)
                if os.name == 'nt':
                    import msvcrt
                    msvcrt.locking(handle.fileno(), msvcrt.LK_NBLCK, 1)
                else:
                    import fcntl
                    fcntl.flock(handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
                break
            except OSError:
                if not wait:
                    yield False
                    return
                if time.monotonic() >= deadline:
                    raise TimeoutError('Cache namespace is busy')
                time.sleep(.05)
        with _gate:
            _held[key] = get_ident()
        try:
            yield True
        finally:
            with _gate:
                _held.pop(key, None)
            handle.seek(0)
            if os.name == 'nt':
                msvcrt.locking(handle.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                fcntl.flock(handle, fcntl.LOCK_UN)
    finally:
        if handle is not None:
            handle.close()
        mutex.release()
