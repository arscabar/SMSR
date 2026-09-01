using System.Text.Json;

namespace SMSR.App.Services;

internal static class HookJson
{
    public static string String(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty : string.Empty;

    public static JsonElement? Object(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : null;

    public static string NestedString(JsonElement root, string objectName, string name)
        => Object(root, objectName) is { } value ? String(value, name) : string.Empty;

    public static string FindString(JsonElement value, string name)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty(name, out var found) && found.ValueKind == JsonValueKind.String)
                return found.GetString() ?? string.Empty;
            foreach (var property in value.EnumerateObject())
                if (FindString(property.Value, name) is { Length: > 0 } nested) return nested;
        }
        if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray())
                if (FindString(item, name) is { Length: > 0 } nested) return nested;
        if (value.ValueKind == JsonValueKind.String)
            try
            {
                using var nested = JsonDocument.Parse(value.GetString() ?? "");
                return FindString(nested.RootElement, name);
            }
            catch { }
        return string.Empty;
    }
}
