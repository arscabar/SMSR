---
name: smsr-impact
description: Assess direct and indirect change-impact candidates from SMSR graph evidence, with affected paths, tests and uncertainty. Use before changing an indexed component.
---

Confirm the existing index with `get_graph_health`, resolve an actual node with `search_graph_question`, and disambiguate path/owner. A question does not authorize indexing or modification.

Call `get_graph_affected` in the incoming direction, depth 1–3, excluding inferred links initially. Inspect returned edges and paths, then use `get_graph_relations`, `trace_graph_path` and source evidence to confirm the important candidates. Compare stored revisions with `compare_graph_revisions` only when requested revisions exist. Requery if the revision changes; never merge mixed snapshots.

Return the proposed change boundary, direct dependencies, indirect impact candidates, likely tests (identified from real test files), and unresolved/dynamic/configuration risks. Distinguish structural candidates from proven behavior. State truncation and missing coverage. Candidate edges and heuristic taint cannot prove a vulnerability or safety.

Consult `get_graph_feedback_lessons` for each important source; stale lessons are excluded and corrections require source verification. Cite paths, locations, revision and analyzer. Do not save raw questions, prompts or results, install hooks, call external providers or execute target code. Implement only under a separate change request.
