"""Run one ephemeral Codex turn and assert the installed CUA Stop hook completes."""
import json
import queue
import subprocess
import sys
import tempfile
import threading
import time

MARKER = "Computer-use completion notification"


def main():
    messages = queue.Queue()
    with tempfile.TemporaryDirectory(prefix="smsr-notify-") as cwd:
        p = subprocess.Popen([sys.argv[1], "--no-daemon", "app-server", "--stdio"],
                             stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                             stderr=subprocess.DEVNULL, text=True, encoding="utf-8")
        def read():
            for line in p.stdout:
                messages.put(json.loads(line))
        threading.Thread(target=read, daemon=True).start()
        completed = []
        deadline = time.monotonic() + 90
        def receive():
            m = messages.get(timeout=max(0.1, deadline - time.monotonic()))
            if m.get("method") == "hook/completed":
                run = m["params"]["run"]
                if run.get("statusMessage") == MARKER:
                    completed.append(run)
            return m
        def send(method, params=None, request_id=None):
            data = {"method": method}
            if params is not None: data["params"] = params
            if request_id is not None: data["id"] = request_id
            p.stdin.write(json.dumps(data) + "\n")
            p.stdin.flush()
            if request_id is None: return
            while True:
                m = receive()
                if m.get("id") == request_id:
                    assert "error" not in m, "app-server request failed"
                    return m["result"]
        try:
            send("initialize", {"clientInfo": {"name": "smsr-notify-test", "version": "1"},
                                "capabilities": {"experimentalApi": True}}, 1)
            send("initialized")
            listed = send("hooks/list", {"cwds": [cwd]}, 2)
            owned = [h for e in listed["data"] for h in e["hooks"]
                     if h.get("statusMessage") == MARKER]
            assert len(owned) == 1 and owned[0]["trustStatus"] == "trusted"
            t = send("thread/start", {"cwd": cwd, "ephemeral": True,
                     "sandbox": "read-only", "approvalPolicy": "never"}, 3)
            send("turn/start", {"threadId": t["thread"]["id"],
                 "input": [{"type": "text", "text": "Reply OK only. Do not use tools."}]}, 4)
            while receive().get("method") != "turn/completed":
                pass
            assert len(completed) == 1 and completed[0]["status"] == "completed"
            print(json.dumps({"notifyHook": "completed", "ephemeral": True,
                              "durationMs": completed[0]["durationMs"]}))
        finally:
            p.stdin.close()
            p.terminate()
            p.wait(timeout=5)


if __name__ == "__main__":
    main()
