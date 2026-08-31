---
name: smsr-tracking
description: Push a Codex task's hierarchical plan, per-agent heartbeat, retries, and artifacts to a connected local SMSR MCP dashboard.
---

SMSR is a passive receiver. It never starts or polls agents. Graph tracking is opt-in: use this skill only when the user explicitly asks to track or visualize work as a graph, flow, dashboard, or SMSR workflow, or asks to resume a previous graph. Ordinary work creates no SMSR records.

Use the repository folder name as `projectId`. For a new graph, omit `workflowId` in the first `save_plan`; SMSR returns a `projectName__yyyyMMdd-HHmmssfff` ID. Reuse that returned ID unchanged for every heartbeat and event until the explicitly requested graph scope ends. Use the current Codex task/session ID as `agentId`, not as the generated workflow ID.

- The coordinator calls `save_plan` when the concrete plan changes. Use `parentNodeId` for drilldown groups, `dependsOn` for execution order, and include `assignedAgentId`, `agentRole`, and `completionCriteria` when known.
- To resume a previous graph, call `list_workflows`, then load the selected workflow with `get_plan` and `get_state`. Keep that workflow ID. If several candidates exist and the user's choice is unclear, ask instead of creating a new graph.
- Each active agent calls `record_heartbeat` when it starts, after roughly 30 seconds of continued work, and before it stops. Send that agent's ID, role, active node, status, and retry count.
- Each agent calls `record_event` for its own node only when status, progress, retry count, next action, or artifacts meaningfully change. Use a unique `eventId`, the plan's exact `nodeId`, and `NODE_STATUS_CHANGED`.
- The coordinator must not fabricate a subagent heartbeat or mirror a subagent event when the subagent can send it directly.
- Do not send prompts, raw shell commands, secrets, or every tool call. Artifact entries should be paths or concise identifiers, not file contents.
- Continue the same graph across related follow-up turns until every node reaches `SUCCESS`, `FAILED`, or `BLOCKED`. Send the final terminal event and stop heartbeats for that graph.
- Never attach later unrelated requests to a completed graph. A later explicit graph request starts a new workflow scope.

Use `get_plan` and `get_state` to load or confirm a selected graph. No lifecycle hook writes activity records; heartbeat and progress exist only while an explicitly requested graph is active.
