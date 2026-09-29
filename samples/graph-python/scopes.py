"""Compiler evidence example. SMSR compiles this file but does not run it."""


def counter(initial):
    value = initial

    def increment():
        nonlocal value
        value += 1
        return value

    return increment


def choose(value):
    if value < 0:
        return 0
    while value < 3:
        value += 1
    return value


def protected(value):
    try:
        return 1 / value
    except ZeroDivisionError:
        return 0


def stream(items):
    for item in items:
        yield item


def maybe(flag):
    if flag:
        value = 1
    return value


def publish(value, target):
    adjusted = value + 1
    return target(adjusted, original=value)


def walk(items, receiver):
    for value in items:
        receiver.send(value)
    return receiver.result


def decision(flag):
    if flag:
        return 1
    return 0


def endless():
    while True:
        pass


def summarized(flag, left, right):
    return left if flag else right


def cleared(value):
    value = 0
    return value


def local_call(value):
    def inner(item):
        return item
    copied = inner
    return copied(value)


def local_choice(value, flag, external):
    def inner(item):
        return item
    chosen = inner if flag else external
    return chosen(value)


def local_erased(value):
    def inner(item):
        item = 0
        return item
    return inner(value)


def local_nested(value):
    def middle(item):
        def inner(last):
            return last
        return inner(item)
    return middle(value)


def keyword_reordered(left, right):
    def inner(first, second):
        return second
    return inner(second=left, first=right)


def keyword_only(left, right):
    def inner(first, /, *, second):
        return second
    return inner(right, second=left)
