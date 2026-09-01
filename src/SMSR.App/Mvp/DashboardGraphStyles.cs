namespace SMSR.App.Mvp;

internal static class DashboardGraphStyles
{
    public const string Css = """
        .flow-svg{display:block;min-width:100%;margin:auto;user-select:none}.flow-svg a{text-decoration:none}.flow-node{cursor:pointer}.flow-node rect{fill:var(--pending);stroke:var(--pending-stroke);stroke-width:2}.flow-node:hover rect{stroke:#62adff;stroke-width:4}.flow-node.SUCCESS rect{fill:var(--success);stroke:#39a66d}.flow-node.IN_PROGRESS rect{fill:var(--active);stroke:#62adff}.flow-node.VALIDATING rect{fill:var(--validating);stroke:#9d7bea}.flow-node.FAILED rect,.flow-node.RETRYING rect,.flow-node.BLOCKED rect{fill:var(--error);stroke:#dd5668;stroke-width:3}.flow-node.current{filter:drop-shadow(0 0 7px #62adff)}.flow-node.current rect{stroke-width:4}.flow-node.current.VALIDATING{filter:drop-shadow(0 0 7px #9d7bea)}.flow-node.current.RETRYING{filter:drop-shadow(0 0 7px #dd5668)}.flow-node text{text-anchor:middle;fill:var(--text);pointer-events:none}.node-title{font-weight:700;font-size:14px}.node-meta{font-size:10px;fill:var(--muted)}.node-drill{font-size:10px;font-weight:700;fill:#62adff}.edge{fill:none;stroke:var(--pending-stroke);stroke-width:2}.edge.IN_PROGRESS,.edge.VALIDATING{stroke:#62adff;stroke-width:3}.edge.FAILED,.edge.RETRYING,.edge.BLOCKED{stroke:#dd5668}#arrow path{fill:var(--pending-stroke)}
        """;
}
