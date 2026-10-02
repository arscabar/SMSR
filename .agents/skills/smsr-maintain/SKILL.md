---
name: smsr-maintain
description: Check and maintain an SMSR project's index freshness and analysis coverage when the user explicitly asks to update the graph.
---

Discover tool schemas and confirm `get_graph_health`. Use `check_graph_freshness` to identify actual additions, changes and deletions. Read `get_graph_coverage` and `get_graph_issues` where available; unavailable tools mean an app version limitation, not an empty result.

Read-only checks require no index write. When updating is authorized, use `index_project_graph` with the existing trusted project root, never an invented path. Respect the configured folder scope and large-reduction protection. Do not set allowLargeReduction to bypass a warning without user approval. Recheck freshness, coverage, issues and representative relationships after indexing.

Indexing changes file/structure relations automatically, but does not authorize external semantic analysis or local model downloads. Stored semantic evidence whose dependency hash changes is stale and must be explicitly regenerated; do not overwrite it with guesses. Use validated document/analysis tools only within their scope.

Report changed counts, remaining unsupported/unresolved relations and exact verification. Never claim every runtime call is known. A watch toggle must be explicitly requested, not silently enabled. Protect source files and previous graph data; do not clear caches or install global hooks without explicit authority. Do not retain raw questions.
