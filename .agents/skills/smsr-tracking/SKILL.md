---
name: smsr-tracking
description: Push a Codex task's hierarchical plan, per-agent heartbeat, retries, and artifacts to a connected local SMSR MCP dashboard.
---

SMSR is a passive receiver. It never starts or polls agents. The coordinator and every subagent send only their own state through the connected `smsr` MCP server.

Use the repository folder name as `projectId` and the Codex task/session ID as `workflowId`. Keep those IDs stable for the task.

- The coordinator calls `save_plan` when the concrete plan changes. Use `parentNodeId` for drilldown groups, `dependsOn` for execution order, and include `assignedAgentId`, `agentRole`, and `completionCriteria` when known.
- Each active agent calls `record_heartbeat` when it starts, after roughly 30 seconds of continued work, and before it stops. Send that agent's ID, role, active node, status, and retry count.
- Each agent calls `record_event` for its own node only when status, progress, retry count, next action, or artifacts meaningfully change. Use a unique `eventId`, the plan's exact `nodeId`, and `NODE_STATUS_CHANGED`.
- The coordinator must not fabricate a subagent heartbeat or mirror a subagent event when the subagent can send it directly.
- Do not send prompts, raw shell commands, secrets, or every tool call. Artifact entries should be paths or concise identifiers, not file contents.

Use `get_plan` and `get_state` only when confirmation is needed. Lifecycle hooks provide best-effort start/stop heartbeat records and do not replace explicit progress events.
