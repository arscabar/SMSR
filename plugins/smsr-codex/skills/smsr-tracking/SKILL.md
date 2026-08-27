---
name: smsr-tracking
description: Track a Codex task plan and meaningful node status changes in the local SMSR dashboard.
---

Use the SMSR IDs supplied by the SessionStart hook.

1. When a concrete plan is agreed, call `save_plan` once with nodes, dependencies, and integer weights.
2. Call `record_event` only when a plan node changes status. Use the same node ID, a unique event ID, and `NODE_STATUS_CHANGED`.
3. Do not log shell commands, prompt text, or every tool call. Use `get_plan` to confirm the applied status when needed.

`record_lifecycle` is called automatically by the plugin hooks and is separate from plan progress.
