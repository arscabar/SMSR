"""Forward Codex stdin lifecycle identifiers, never conversation text, to CUA."""
import argparse
import json
import subprocess
import sys
from pathlib import Path


def payload(event):
    if event.get("hook_event_name") != "Stop":
        return None
    identifiers = [event.get("session_id"), event.get("turn_id")]
    if any(not isinstance(x, str) or not x or len(x) > 256 for x in identifiers):
        raise ValueError("missing or invalid lifecycle identifiers")
    return json.dumps({"type": "agent-turn-complete", "thread-id": identifiers[0],
                       "turn-id": identifiers[1]}, ensure_ascii=True)


def forward(event, notifier, run=subprocess.run):
    data = payload(event)
    if data is None:
        return
    if not notifier.is_file():
        raise ValueError("computer-use notifier not found")
    run([str(notifier), "turn-ended", data], check=True, capture_output=True,
        timeout=5, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))


def self_test():
    for text in ["short", "한글" * 40000, '"\\' * 40000]:
        event = {"hook_event_name": "Stop", "session_id": "test-thread",
                 "turn_id": "test-turn", "last_assistant_message": text}
        result = payload(event)
        assert len(result) < 200 and text not in result
        calls = []
        forward(event, Path(sys.executable), lambda *a, **k: calls.append(a[0]))
        assert len(calls) == 1 and json.loads(calls[0][-1])["turn-id"] == "test-turn"
    assert payload({"hook_event_name": "PreToolUse"}) is None
    try:
        payload({"hook_event_name": "Stop"})
    except ValueError:
        pass
    else:
        raise AssertionError("invalid identifier accepted")
    print("notify self-test passed")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--notifier", type=Path)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return 0
    if args.notifier is None:
        parser.error("--notifier is required")
    try:
        forward(json.loads(sys.stdin.buffer.read().decode("utf-8-sig")), args.notifier)
    except (ValueError, OSError, subprocess.SubprocessError) as error:
        # Never expose stdin, conversation text, command arguments, or stderr.
        print("computer-use notification failed: " + type(error).__name__, file=sys.stderr)
        return 1
    print("{}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
