using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class EventPayload
{
    public static RecordEventRequest? Parse(string json)
    {
        try { return JsonSerializer.Deserialize<RecordEventRequest>(json); }
        catch (JsonException) { return null; }
    }
}
