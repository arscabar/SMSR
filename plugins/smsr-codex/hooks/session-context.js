const path = require("path");

let raw = "";
process.stdin.setEncoding("utf8");
process.stdin.on("data", chunk => raw += chunk);
process.stdin.on("end", () => {
  try {
    const event = JSON.parse(raw);
    const projectId = path.basename(path.resolve(event.cwd || "workspace"));
    const text = `SMSR 추적 ID: projectId=${projectId}, workflowId=${event.session_id}. 계획을 수립하면 save_plan을 한 번 호출하고, 실제 노드 상태가 바뀔 때만 record_event를 호출하세요.`;
    process.stdout.write(JSON.stringify({ hookSpecificOutput: { hookEventName: "SessionStart", additionalContext: text } }));
  } catch { process.exitCode = 0; }
});
