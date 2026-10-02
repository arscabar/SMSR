namespace SMSR.App.Mvp;

internal static class DashboardDisclosure
{
    internal static string Render(string key, string title, string subtitle, string content,
        string icon, string badge, bool open = false)
        => $"""
            <details class="evidence-disclosure panel-disclosure" data-disclosure="{key}" data-default-open="{open.ToString().ToLowerInvariant()}"{(open ? " open" : "")}>
            <summary><span class="evidence-icon" aria-hidden="true">{icon}</span><span class="evidence-heading"><strong>{DashboardPanels.Encode(title)}</strong><small>{DashboardPanels.Encode(subtitle)}</small></span><span class="evidence-count">{DashboardPanels.Encode(badge)}</span></summary>
            <div class="panel-disclosure-body">{content}</div></details>
            """;
}
