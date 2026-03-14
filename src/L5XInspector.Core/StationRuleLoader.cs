using System.Text.Json;
using L5XInspector.Core.Models;

namespace L5XInspector.Core;

public static class StationRuleLoader
{
    public static IReadOnlyList<StationRule> Load(string path)
    {
        if (!File.Exists(path))
            return Array.Empty<StationRule>();

        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<StationRuleDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (doc?.Stations == null)
            return Array.Empty<StationRule>();

        return doc.Stations
            .Select(s => new StationRule(s.Name, s.Patterns ?? new List<string>()))
            .ToList();
    }

    private sealed record StationRuleDocument(List<StationRuleItem>? Stations);

    private sealed record StationRuleItem(string Name, List<string>? Patterns);
}
