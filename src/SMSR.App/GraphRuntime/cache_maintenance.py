"""Expire unused payloads after 30 days, then enforce a shared 1 GiB quota."""
import os
import time
from contextlib import contextmanager
from cache_paths import root, checked, regular
from cache_lock import locked, owned
from cache_inventory import inventory, erase

def touch(folder, now=None):
    marker = checked(folder / '.last-used')
    if not marker.exists():
        marker.touch()
    regular(marker)
    value = time.time() if now is None else now
    os.utime(marker, (value, value))

def trim(*, now=None, ttl=30 * 86400, quota=1024 ** 3):
    base = root()
    base.mkdir(parents=True, exist_ok=True)
    report = dict(removedFiles=0, removedBytes=0, activeSkipped=0, unsafeSkipped=0,
                  totalBytes=0, overLimit=False)
    with locked(base / '.maintenance.lock', wait=False) as acquired:
        if not acquired:
            report['maintenanceBusy'] = True
            return report
        rows, report['unsafeSkipped'] = inventory(base)
        report['totalBytes'] = sum(row[2] for row in rows)
        clock = time.time() if now is None else now
        for used, folder, size in rows:
            if not size or (clock - used < ttl and report['totalBytes'] <= quota):
                continue
            try:
                with locked(folder / '.lock', wait=False) as acquired:
                    if not acquired:
                        report['activeSkipped'] += 1
                        continue
                    # A process may have used this namespace since the inventory.
                    marker = checked(folder / '.last-used')
                    latest = marker.stat().st_mtime if marker.exists() else used
                    if latest != used:
                        continue
                    count, removed = erase(folder)
                    report['removedFiles'] += count
                    report['removedBytes'] += removed
                    report['totalBytes'] -= removed
            except (OSError, ValueError):
                report['unsafeSkipped'] += 1
        report['overLimit'] = report['totalBytes'] > quota
    return report

@contextmanager
def lease(folder):
    folder = checked(folder)
    nested = owned(folder / '.lock')
    with locked(folder / '.lock'):
        touch(folder)
        report = {}
        try:
            yield report
        finally:
            touch(folder)
    if not nested:
        report.update(trim())
