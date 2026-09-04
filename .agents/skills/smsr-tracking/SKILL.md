---
name: smsr-tracking
description: Push a Codex task's hierarchical plan, per-agent heartbeat, retries, and artifacts to a connected local SMSR MCP dashboard.
---

SMSR is a passive receiver. It never starts or polls agents. Never record calculations, quick lookups, Q&A, read-only inspection, status checks, navigation, or commands that do not change project files. After a request actually changes project files, call `record_daily_activity` exactly once before the final reply with a unique activity ID for that request, a concise title, result, changed paths, verification, and artifacts. Reuse the activity ID only to correct the same card. Never store prompt or raw command text.

Graph tracking is reserved for an explicit user request or an automatically qualified complex project change. Automatic qualification requires a real project change and at least two of: three or more execution stages, multiple files/components, build-test-release work, parallel/subagent work, or over ten minutes expected duration. Ambiguous work and a single-location small edit use only daily activity.

Use the repository folder name as `projectId`. For a new graph, omit `workflowId` in the first `save_plan`; SMSR returns a readable `yyyyMMdd-HHmmssfff__project__task` ID. Reuse that returned ID unchanged for every heartbeat and event until the explicitly requested graph scope ends. Use the current Codex task/session ID as `agentId`, not as the generated workflow ID.

- The coordinator calls `save_plan` when the concrete plan changes. Reuse the active `workflowId`; the array order controls display order, existing node IDs retain their state, and new node IDs start pending. Use `parentNodeId` for drilldown groups, `dependsOn` for execution order, and include `assignedAgentId`, `agentRole`, and `completionCriteria` when known.
- To resume a previous graph, call `list_workflows`, then load the selected workflow with `get_plan` and `get_state`. Keep that workflow ID. If several candidates exist and the user's choice is unclear, ask instead of creating a new graph.
- Immediately after `save_plan`, the assigned agent records the first executable node as `IN_PROGRESS`. When moving between dependent nodes, record the predecessor as `SUCCESS` first; SMSR normalizes it to 100%, then immediately record the successor as `IN_PROGRESS`. Only independent nodes may be active in parallel.
- Each agent calls `record_event` for its own node immediately when the node starts or its implementation stage, validation, retry count, artifact, progress, next action, or terminal state meaningfully changes. Do not batch events at the end of the task. Use a unique `eventId`, the plan's exact `nodeId`, and `NODE_STATUS_CHANGED`.
- Each active agent calls `record_heartbeat` at startup and within 30 seconds only while work continues without a meaningful event, then once before it stops. Heartbeat supplements immediate progress events; it does not replace them.
- The coordinator must not fabricate a subagent heartbeat or mirror a subagent event when the subagent can send it directly.
- Do not send prompts, raw shell commands, secrets, or every tool call. Artifact entries should be paths or concise identifiers, not file contents.
- A `SUCCESS` node is complete and must not be reopened, edited, or used as the parent of later work. For implementation, fixes, validation, documentation, commit, or release work related to the same request, preserve the completed nodes and append a sibling/new root connected to a completed node with `dependsOn`. Do this `save_plan` update before starting the follow-up work, even when every existing node is already terminal.
- The graph scope closes when every planned node is terminal. A later request starts no graph unless it independently qualifies or the user explicitly requests one. Include all already-known remaining work before marking the last node `SUCCESS`; if more related work is discovered while the scope is active, append its node and immediately record it `IN_PROGRESS` before any other tool call.

Use `get_plan` and `get_state` to load or confirm a selected graph. Semantic heartbeat and progress exist only while a qualified graph is active. Link its final `record_daily_activity` to the graph workflow ID.
When the installed SMSR Codex hook is trusted, it separately writes normalized agent lifecycle and supported local tool completion metadata to the active workflow's `activity.jsonl`. This automatic activity trail never replaces semantic `record_event` updates and never stores prompts, raw commands, tool inputs, or tool outputs.
