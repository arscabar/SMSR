"""Only the current user's exact extraction-cache tree is a cache target."""
import os
import stat
from pathlib import Path

def root():
    local = Path(os.environ['LOCALAPPDATA'])
    if not local.is_absolute():
        raise ValueError('LOCALAPPDATA must be absolute')
    return checked(local / 'SMSR/graph-extraction-cache')

def checked(path):
    local = Path(os.environ['LOCALAPPDATA'])
    base = Path(os.path.abspath(local / 'SMSR/graph-extraction-cache'))
    value = Path(os.path.abspath(path))
    if not local.is_absolute() or not value.is_relative_to(base):
        raise ValueError('Cache target outside extraction cache')
    # lstat, not resolve: a junction must not silently redirect the cache tree.
    for item in [value, *value.parents]:
        try:
            info = item.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & 0x400:
            raise ValueError('Reparse cache path')
        if item != value and not stat.S_ISDIR(info.st_mode):
            raise ValueError('Non-directory cache parent')
    return value

def regular(path):
    value = checked(path)
    info = value.lstat()
    if not stat.S_ISREG(info.st_mode) or info.st_nlink != 1:
        raise ValueError('Non-file cache entry')
    return value
